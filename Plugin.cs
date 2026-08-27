using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using MinionMirage.Localization;
using System.Reflection;
using CompanionSheet = Lumina.Excel.Sheets.Companion;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using NativeVisibilityFlags = FFXIVClientStructs.FFXIV.Client.Game.Object.VisibilityFlags;

namespace MinionMirage;

public sealed unsafe class Plugin : IDalamudPlugin
{
    public const string DisplayName = "Minion Mirage";
    private const int CharacterBaseModelScaleOffset = 0x2A4;
    private const uint MinionHiddenActionStatus = 1325;

    [PluginService] private static IObjectTable ObjectTable { get; set; } = null!;
    [PluginService] private static IFramework Framework { get; set; } = null!;
    [PluginService] private static IDataManager DataManager { get; set; } = null!;
    [PluginService] private static IClientState ClientState { get; set; } = null!;
    [PluginService] private static ITextureProvider TextureProvider { get; set; } = null!;
    [PluginService] private static IGameInteropProvider GameInteropProvider { get; set; } = null!;
    [PluginService] private static IPluginLog Log { get; set; } = null!;

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IReadOnlyDictionary<PrototypeTargetKey, AppearancePayload> desiredByTarget;
    private readonly IReadOnlyDictionary<uint, uint> sourceModelCharaBySource;
    private readonly IReadOnlyDictionary<uint, ushort> sourceOrderBySource;
    private readonly RuntimeStateReporter stateReporter;
    private readonly Hook<GetActionStatusDelegate>? getActionStatusHook;
    private readonly Hook<UseActionDelegate>? useActionHook;
    private readonly ConfigWindow configWindow;
    private readonly WindowSystem windowSystem = new("MinionMirage");
    private TrackedActor? tracked;
    private FailedActorState? failedActor;
    private TargetScanResult lastScan = TargetScanResult.NotStarted;
    private string lastTransition = "loaded";
    private DateTimeOffset lastTransitionAtUtc = DateTimeOffset.UtcNow;
    private string? lastError;
    private int normalSummonUnlockEnabled;
    private long normalSummonBypassCount;
    private int normalSummonLastBypassedRowId;
    private int normalSummonLastOriginalStatus;
    private long normalSummonLastBypassedUtcTicks;
    private long normalSummonLastReportedBypassCount;
    private long normalSummonIconClickCount;
    private int normalSummonLastIconClickRowId;
    private string? normalSummonLastIconClickResult;
    private bool disposed;

    public Configuration Configuration { get; }

    public Localizer Localizer { get; }

    public static string DisplayVersion =>
        typeof(Plugin).Assembly
            .GetCustomAttributes(false)
            .OfType<AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()
            ?.InformationalVersion
            .Split('+')[0]
        ?? typeof(Plugin).Assembly.GetName().Version?.ToString(3)
        ?? "0.0.0";

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        var configurationChanged = NormalizeConfiguration();
        if (configurationChanged)
            SaveConfiguration();
        Localizer = new Localizer(Configuration, ClientState);
        configWindow = new ConfigWindow(this);
        windowSystem.AddWindow(configWindow);
        pluginInterface.UiBuilder.Draw += windowSystem.Draw;
        pluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        stateReporter = new RuntimeStateReporter(pluginInterface.ConfigDirectory.FullName, Log);
        desiredByTarget = PrototypeContract.Mappings
            .SelectMany(mapping => PrototypeContract.GetTargetCandidates(mapping.SourceCompanionRowId))
            .ToDictionary(
                PrototypeContract.GetTargetKey,
                mapping => TargetAppearanceResolver.Resolve(
                    DataManager,
                    PrototypeContract.GetAppearanceMapping(mapping)));
        var companionSheet = DataManager.GetExcelSheet<CompanionSheet>();
        sourceModelCharaBySource = PrototypeContract.Mappings.ToDictionary(
            mapping => mapping.SourceCompanionRowId,
            mapping => companionSheet.TryGetRow(mapping.SourceCompanionRowId, out var companion)
                && companion.Model.RowId is not 0
                    ? companion.Model.RowId
                    : throw new InvalidOperationException(
                        $"Companion {mapping.SourceCompanionRowId} has no source ModelChara."));
        sourceOrderBySource = PrototypeContract.Mappings.ToDictionary(
            mapping => mapping.SourceCompanionRowId,
            mapping => companionSheet.TryGetRow(mapping.SourceCompanionRowId, out var companion)
                ? companion.Order
                : ushort.MaxValue);
        Hook<GetActionStatusDelegate>? createdStatusHook = null;
        Hook<UseActionDelegate>? createdActionHook = null;
        try
        {
            createdStatusHook = GameInteropProvider.HookFromAddress<GetActionStatusDelegate>(
                (nint)ActionManager.MemberFunctionPointers.GetActionStatus,
                GetActionStatusDetour);
            createdActionHook = GameInteropProvider.HookFromAddress<UseActionDelegate>(
                (nint)ActionManager.MemberFunctionPointers.UseAction,
                UseActionDetour);
            getActionStatusHook = createdStatusHook;
            useActionHook = createdActionHook;
            if (Configuration.ExperimentalEnableNormalCompanionSummon)
            {
                Volatile.Write(ref normalSummonUnlockEnabled, 1);
                useActionHook.Enable();
                getActionStatusHook.Enable();
            }
        }
        catch (Exception exception)
        {
            if (createdActionHook is not null)
            {
                if (createdActionHook.IsEnabled)
                    createdActionHook.Disable();
                createdActionHook.Dispose();
            }
            if (createdStatusHook is not null)
            {
                if (createdStatusHook.IsEnabled)
                    createdStatusHook.Disable();
                createdStatusHook.Dispose();
            }
            getActionStatusHook = null;
            useActionHook = null;
            Volatile.Write(ref normalSummonUnlockEnabled, 0);
            if (Configuration.ExperimentalEnableNormalCompanionSummon)
            {
                Configuration.ExperimentalEnableNormalCompanionSummon = false;
                SaveConfiguration();
            }
            Log.Error(exception, "Normal Companion summon unlock hook is unavailable.");
        }
        Framework.Update += OnFrameworkUpdate;

        Log.Information(
            "Minion Mirage loaded. MappingCount={MappingCount}.",
            PrototypeContract.Mappings.Count);
        Log.Information("Minion Mirage runtime state: {StateFilePath}", stateReporter.StateFilePath);
        PublishRuntimeState("running", force: true);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        Framework.Update -= OnFrameworkUpdate;
        pluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        pluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        Volatile.Write(ref normalSummonUnlockEnabled, 0);
        if (getActionStatusHook is not null)
        {
            if (getActionStatusHook.IsEnabled)
                getActionStatusHook.Disable();
            getActionStatusHook.Dispose();
        }
        if (useActionHook is not null)
        {
            if (useActionHook.IsEnabled)
                useActionHook.Disable();
            useActionHook.Dispose();
        }

        if (tracked is not null && TryResolve(tracked.Identity, out var current))
        {
            if (!TryApplyAppearance(current, tracked.Original, tracked.OriginalModelScale))
            {
                Log.Error("Failed to restore the tracked Companion while unloading.");
                lastError = "unload_restore_failed";
            }
        }

        SetTransition("disposed");
        tracked = null;
        PublishRuntimeState("disposed", force: true);
        windowSystem.RemoveAllWindows();
        configWindow.Dispose();
    }

    public void SetUiLanguage(UiLanguage language)
    {
        if (Configuration.UiLanguage == language)
            return;

        Configuration.UiLanguage = language;
        SaveConfiguration();
    }

    public void SetMappingEnabled(uint companionRowId, bool enabled)
    {
        if (!PrototypeContract.TryGetMapping(companionRowId, out _))
            return;

        var changed = enabled
            ? Configuration.DisabledCompanionRowIds.Remove(companionRowId)
            : Configuration.DisabledCompanionRowIds.Add(companionRowId);
        if (changed)
            SaveConfiguration();
    }

    public void SetSelectedTarget(uint sourceCompanionRowId, uint targetRowId)
    {
        if (!PrototypeContract.HasMultipleTargetCandidates(sourceCompanionRowId)
            || !PrototypeContract.TryGetTargetCandidate(sourceCompanionRowId, targetRowId, out _)
            || (Configuration.SelectedTargetRowIds.TryGetValue(sourceCompanionRowId, out var current)
                && current == targetRowId))
        {
            return;
        }

        Configuration.SelectedTargetRowIds[sourceCompanionRowId] = targetRowId;
        SaveConfiguration();
    }

    public void SetExperimentalEnableNormalCompanionSummon(bool enabled)
    {
        if (Configuration.ExperimentalEnableNormalCompanionSummon == enabled)
            return;

        if (getActionStatusHook is null || useActionHook is null)
        {
            SetTransition("normal_summon_unlock_hook_unavailable");
            Log.Error("Normal Companion summon unlock could not be enabled because its native hook is unavailable.");
            PublishRuntimeState(lastError is null ? "running" : "error", force: true);
            return;
        }

        try
        {
            if (enabled)
            {
                Volatile.Write(ref normalSummonUnlockEnabled, 1);
                useActionHook.Enable();
                getActionStatusHook.Enable();
            }
            else
            {
                Volatile.Write(ref normalSummonUnlockEnabled, 0);
                getActionStatusHook.Disable();
                useActionHook.Disable();
            }

            Configuration.ExperimentalEnableNormalCompanionSummon = enabled;
            SaveConfiguration();
            SetTransition(enabled
                ? "normal_summon_unlock_enabled"
                : "normal_summon_unlock_disabled");
            Log.Warning(
                "Experimental normal Companion summon unlock {State}. Status {OriginalStatus} is bypassed; rejected native actions use the local Companion transition with parameter 1. These native hooks may crash the game.",
                enabled ? "enabled" : "disabled",
                MinionHiddenActionStatus);
        }
        catch (Exception exception)
        {
            Volatile.Write(ref normalSummonUnlockEnabled, 0);
            if (getActionStatusHook.IsEnabled)
                getActionStatusHook.Disable();
            if (useActionHook.IsEnabled)
                useActionHook.Disable();
            Configuration.ExperimentalEnableNormalCompanionSummon = false;
            SaveConfiguration();
            SetTransition("normal_summon_unlock_toggle_failed");
            Log.Error(exception, "Normal Companion summon unlock toggle failed and was disabled.");
        }

        PublishRuntimeState(lastError is null ? "running" : "error", force: true);
    }

    public void SetAllMappingsEnabled(bool enabled)
    {
        var changed = false;
        foreach (var mapping in PrototypeContract.Mappings)
        {
            changed |= enabled
                ? Configuration.DisabledCompanionRowIds.Remove(mapping.SourceCompanionRowId)
                : Configuration.DisabledCompanionRowIds.Add(mapping.SourceCompanionRowId);
        }

        if (changed)
            SaveConfiguration();
    }

    internal string GetCompanionName(PrototypeMapping mapping)
    {
        try
        {
            if (DataManager.GetExcelSheet<CompanionSheet>(ClientState.ClientLanguage)
                .TryGetRow(mapping.SourceCompanionRowId, out var companion))
            {
                var name = companion.Singular.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
            }
        }
        catch (Exception exception)
        {
            Log.Debug(
                exception,
                "Failed to read Companion name for row {CompanionRowId}.",
                mapping.SourceCompanionRowId);
        }

        return mapping.SourceName;
    }

    internal ushort GetCompanionOrder(PrototypeMapping mapping)
        => sourceOrderBySource.GetValueOrDefault(mapping.SourceCompanionRowId, ushort.MaxValue);

    internal bool IsCompanionUnlocked(uint companionRowId)
    {
        var uiState = UIState.Instance();
        return uiState == null || uiState->IsCompanionUnlocked(companionRowId);
    }

    internal PrototypeMapping GetSelectedMapping(PrototypeMapping sourceMapping)
        => PrototypeContract.GetSelectedMapping(
            sourceMapping.SourceCompanionRowId,
            Configuration.SelectedTargetRowIds);

    internal IReadOnlyList<PrototypeMapping> GetTargetCandidates(PrototypeMapping sourceMapping)
        => PrototypeContract.GetTargetCandidates(sourceMapping.SourceCompanionRowId);

    internal bool IsYoungHuman(PrototypeMapping mapping)
    {
        var appearanceMapping = PrototypeContract.GetAppearanceMapping(mapping);
        return desiredByTarget.TryGetValue(PrototypeContract.GetTargetKey(appearanceMapping), out var appearance)
            && appearance.IsHuman
            && appearance.Customize.Length > 2
            && appearance.Customize[2] == 4;
    }

    internal IDisposable PushIconFont()
        => pluginInterface.UiBuilder.IconFontFixedWidthHandle.Push();

    public bool TryGetCompanionIcon(uint companionRowId, out IDalamudTextureWrap? texture)
    {
        texture = null;
        try
        {
            if (!DataManager.GetExcelSheet<CompanionSheet>().TryGetRow(companionRowId, out var companion))
                return false;

            var iconId = (uint)companion.Icon;
            if (iconId == 0)
                return false;

            texture = TextureProvider.GetFromGameIcon(new GameIconLookup(iconId)).GetWrapOrEmpty();
            return true;
        }
        catch (Exception exception)
        {
            Log.Debug(exception, "Failed to load Companion icon for row {CompanionRowId}.", companionRowId);
            return false;
        }
    }

    private void ToggleConfigUi()
        => configWindow.Toggle();

    private bool NormalizeConfiguration()
    {
        var changed = false;
        var migrateV1 = Configuration.Version < 2;

        if (Configuration.DisabledCompanionRowIds is null)
        {
            Configuration.DisabledCompanionRowIds = [];
            changed = true;
        }

        if (Configuration.SelectedTargetRowIds is null)
        {
            Configuration.SelectedTargetRowIds = [];
            changed = true;
        }

        foreach (var selection in Configuration.SelectedTargetRowIds.ToArray())
        {
            if (PrototypeContract.HasMultipleTargetCandidates(selection.Key)
                && PrototypeContract.TryGetTargetCandidate(selection.Key, selection.Value, out _))
            {
                continue;
            }

            Configuration.SelectedTargetRowIds.Remove(selection.Key);
            changed = true;
        }

        foreach (var sourceMapping in PrototypeContract.Mappings)
        {
            var sourceCompanionRowId = sourceMapping.SourceCompanionRowId;
            var hasValidSelection = Configuration.SelectedTargetRowIds.TryGetValue(
                    sourceCompanionRowId,
                    out var targetRowId)
                && PrototypeContract.TryGetTargetCandidate(sourceCompanionRowId, targetRowId, out _);
            if (!PrototypeContract.HasMultipleTargetCandidates(sourceCompanionRowId)
                || (!migrateV1 && hasValidSelection))
            {
                continue;
            }

            var defaultTargetRowId =
                PrototypeContract.GetDefaultTargetMapping(sourceCompanionRowId).TargetRowId;
            if (!hasValidSelection || targetRowId != defaultTargetRowId)
            {
                Configuration.SelectedTargetRowIds[sourceCompanionRowId] = defaultTargetRowId;
                changed = true;
            }
        }

        if (Configuration.Version != 5)
        {
            Configuration.Version = 5;
            changed = true;
        }

        return changed;
    }

    private void SaveConfiguration()
        => pluginInterface.SavePluginConfig(Configuration);

    private void OnFrameworkUpdate(IFramework framework)
    {
        try
        {
            lastScan = ScanTargets();
            UpdateNormalSummonUnlockState();
            UpdateTarget(lastScan.Candidate, lastScan.Mapping);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Minion Mirage update failed; stopping writes for the current actor.");
            lastError = exception.Message;
            SetTransition("update_failed");
            FailCurrentActor();
        }
        finally
        {
            PublishRuntimeState(lastError is null ? "running" : "error");
        }
    }

    private void UpdateTarget(IGameObject? candidate, PrototypeMapping? mapping)
    {
        ClearFailureWhenIdentityChanges(candidate);

        if (tracked is not null
            && (candidate is null || !tracked.Identity.Matches(candidate)))
        {
            var trackedIdentityStillPresent = IsIdentityPresent(tracked.Identity);
            var trackedMappingStillEnabled = Configuration.IsMappingEnabled(
                tracked.Mapping.SourceCompanionRowId);

            if (candidate is null
                && trackedIdentityStillPresent
                && trackedMappingStillEnabled)
            {
                if (tracked.Stage == ApplyStage.Redrawn
                    && TryResolve(tracked.Identity, out var retainedActor))
                {
                    PreserveModelScaleIfAvailable(retainedActor, tracked.Mapping.TargetModelScale);
                }

                return;
            }

            var restoreDisabledMapping = candidate is null
                && trackedIdentityStillPresent
                && !trackedMappingStillEnabled;
            if (restoreDisabledMapping
                && TryResolve(tracked.Identity, out var oldActor)
                && !TryApplyAppearance(oldActor, tracked.Original, tracked.OriginalModelScale))
            {
                Log.Error("Failed to restore the tracked Companion after its mapping was disabled.");
                lastError = "previous_actor_restore_failed";
            }

            tracked = null;
            SetTransition(restoreDisabledMapping
                ? "tracked_actor_restored_after_mapping_disabled"
                : "tracked_actor_released");
        }

        if (tracked is not null
            && candidate is not null
            && mapping is not null
            && tracked.Identity.Matches(candidate)
            && PrototypeContract.GetTargetKey(tracked.Mapping) != PrototypeContract.GetTargetKey(mapping))
        {
            var previous = tracked;
            if (!TryResolve(previous.Identity, out var oldActor))
            {
                Log.Error("Failed to restore the tracked Companion after its target selection changed.");
                lastError = "target_selection_restore_failed";
                failedActor = new FailedActorState(previous.Identity, previous.Mapping);
                tracked = null;
                SetTransition("target_selection_restore_failed");
                return;
            }

            tracked = previous with
            {
                Mapping = mapping,
                Stage = ApplyStage.Pending,
                AppliedDrawObject = nint.Zero,
            };
            lastError = null;
            SetTransition("target_selection_changed");
        }

        if (candidate is null || mapping is null)
        {
            if (failedActor is null)
                lastError = null;
            return;
        }

        var identity = ActorIdentity.From(candidate);
        if (failedActor is { } failed && failed.Identity.Equals(identity))
            return;

        if (tracked is null)
        {
            if (!IsSourceModelReady(candidate, mapping))
            {
                SetTransition("source_model_pending");
                return;
            }

            if (!TryCapture(candidate, out var original)
                || !TryCaptureModelScale(candidate, mapping.TargetModelScale, out var originalModelScale))
            {
                SetTransition("capture_not_ready");
                return;
            }

            tracked = new TrackedActor(
                identity,
                original,
                originalModelScale,
                mapping,
                ApplyStage.Pending,
                nint.Zero);
            lastError = null;
            SetTransition("target_acquired");
            Log.Information(
                "Target Companion acquired. ObjectIndex={ObjectIndex}, GameObjectId={GameObjectId:X16}.",
                identity.ObjectIndex,
                identity.GameObjectId);
        }

        AdvanceTrackedActor();
    }

    private bool IsSourceModelReady(IGameObject actor, PrototypeMapping mapping)
    {
        var character = (Character*)actor.Address;
        return character != null
            && sourceModelCharaBySource.TryGetValue(mapping.SourceCompanionRowId, out var expectedModelCharaId)
            && character->ModelContainer.ModelCharaId == checked((int)expectedModelCharaId);
    }

    private void AdvanceTrackedActor()
    {
        var state = tracked;
        if (state is null)
            return;

        if (!TryResolve(state.Identity, out var current))
        {
            tracked = null;
            return;
        }

        if (state.Stage == ApplyStage.Redrawn)
        {
            PreserveModelScaleIfAvailable(current, state.Mapping.TargetModelScale);

            var gameObject = (GameObject*)current.Address;
            var currentDrawObject = gameObject == null ? nint.Zero : (nint)gameObject->DrawObject;
            if (currentDrawObject == nint.Zero || currentDrawObject == state.AppliedDrawObject)
                return;

            tracked = state with
            {
                Stage = ApplyStage.Pending,
                AppliedDrawObject = nint.Zero,
            };
            SetTransition("draw_object_recreated");
            Log.Information(
                "Companion DrawObject recreated; reapplying configured NPC. CompanionRowId={CompanionRowId}, OldDrawObject=0x{OldDrawObject:X16}, NewDrawObject=0x{NewDrawObject:X16}.",
                state.Mapping.SourceCompanionRowId,
                state.AppliedDrawObject,
                currentDrawObject);
            return;
        }

        var appliedMapping = PrototypeContract.GetAppearanceMapping(state.Mapping);
        var desired = desiredByTarget[PrototypeContract.GetTargetKey(state.Mapping)];
        if (state.Stage == ApplyStage.Pending)
        {
            if (!TryWrite(current, desired))
            {
                Log.Error("{TargetName} backing write failed.", state.Mapping.TargetName);
                lastError = "appearance_write_failed";
                SetTransition("appearance_write_failed");
                FailCurrentActor();
                return;
            }

            tracked = state with { Stage = ApplyStage.BackingWritten };
            SetTransition("appearance_backing_written");
            return;
        }

        if (state.Stage == ApplyStage.BackingWritten)
        {
            if (!TryDisableAppearanceDraw(current))
            {
                lastError = "appearance_disable_failed";
                SetTransition("appearance_disable_failed");
                FailCurrentActor();
                return;
            }

            tracked = state with { Stage = ApplyStage.Disabled };
            SetTransition("appearance_disabled");
            return;
        }

        if (state.Stage == ApplyStage.Disabled)
        {
            if (!TryWrite(current, desired))
            {
                Log.Error("{TargetName} hidden backing write failed.", state.Mapping.TargetName);
                lastError = "appearance_hidden_write_failed";
                SetTransition("appearance_hidden_write_failed");
                FailCurrentActor();
                return;
            }

            tracked = state with { Stage = ApplyStage.HiddenBackingWritten };
            SetTransition("appearance_hidden_backing_written");
            return;
        }

        if (state.Stage == ApplyStage.HiddenBackingWritten)
        {
            if (!TryEnableAppearanceDraw(current, state.Mapping.TargetModelScale))
            {
                lastError = "appearance_enable_failed";
                SetTransition("appearance_enable_failed");
                FailCurrentActor();
                return;
            }

            tracked = state with { Stage = ApplyStage.Enabled };
            SetTransition("appearance_enabled");
            return;
        }

        if (state.Stage == ApplyStage.Enabled)
        {
            var finalizeResult = TryFinalizeAppearance(current, desired, state.Mapping.TargetModelScale);
            if (finalizeResult == AppearanceFinalizeResult.Pending)
            {
                SetTransition("appearance_finalize_pending");
                return;
            }

            if (finalizeResult == AppearanceFinalizeResult.Failed)
            {
                Log.Error(
                    "{TargetName} equipment finalization failed; stopping for the current actor without rollback.",
                    state.Mapping.TargetName);
                lastError = "appearance_finalize_failed";
                SetTransition("appearance_finalize_failed");
                FailCurrentActor();
                return;
            }

            tracked = state with { Stage = ApplyStage.Verify };
            SetTransition("appearance_verify_pending");
            return;
        }

        if (!IsAppearanceApplied(current, desired, state.Mapping.TargetModelScale))
        {
            SetTransition("appearance_verify_pending");
            return;
        }

        var verifiedGameObject = (GameObject*)current.Address;
        var appliedDrawObject = verifiedGameObject == null
            ? nint.Zero
            : (nint)verifiedGameObject->DrawObject;
        if (appliedDrawObject == nint.Zero)
        {
            SetTransition("appearance_verify_pending");
            return;
        }

        tracked = state with
        {
            Stage = ApplyStage.Redrawn,
            AppliedDrawObject = appliedDrawObject,
        };
        lastError = null;
        SetTransition("appearance_redrawn");
        Log.Information(
            "NPC appearance applied. Companion={CompanionRowId}, Selected={SelectedKind}#{SelectedRowId}, Applied={AppliedKind}#{AppliedRowId} ({AppliedName}), ModelChara={ModelCharaRowId}, Body=0x{Body:X16}, Legs=0x{Legs:X16}, Feet=0x{Feet:X16}.",
            state.Mapping.SourceCompanionRowId,
            state.Mapping.TargetKind,
            state.Mapping.TargetRowId,
            appliedMapping.TargetKind,
            appliedMapping.TargetRowId,
            appliedMapping.TargetName,
            desired.ModelCharaId,
            desired.Equipment[1],
            desired.Equipment[3],
            desired.Equipment[4]);
        if (state.Mapping.TargetModelScale is { } modelScale)
            Log.Information("{TargetName} draw-model scale set to {ModelScale}.", state.Mapping.TargetName, modelScale);
    }

    private void FailCurrentActor()
    {
        var state = tracked;
        if (state is null)
            return;

        failedActor = new FailedActorState(state.Identity, state.Mapping);
        tracked = null;
    }

    private void ClearFailureWhenIdentityChanges(IGameObject? candidate)
    {
        if (failedActor is not { } failure)
            return;

        if (IsIdentityPresent(failure.Identity)
            && (candidate is null || failure.Identity.Matches(candidate)))
        {
            return;
        }

        failedActor = null;
        lastError = null;
    }

    private static bool IsIdentityPresent(ActorIdentity identity)
    {
        var current = ObjectTable[identity.ObjectIndex];
        return current is not null
            && current.Address != nint.Zero
            && current.IsValid()
            && current.ObjectKind == ObjectKind.Companion
            && identity.Matches(current);
    }

    private TargetScanResult ScanTargets()
    {
        var localPlayer = ObjectTable.LocalPlayer;
        var localPlayerAvailable = localPlayer is not null && localPlayer.Address != nint.Zero;
        var observations = new List<CompanionRuntimeObservation>();
        var ownedTargets = new List<TargetCandidate>();
        var validSourceCount = 0;

        foreach (var candidate in ObjectTable)
        {
            if (candidate.ObjectKind != ObjectKind.Companion)
                continue;

            var valid = candidate.Address != nint.Zero && candidate.IsValid();
            var ownership = ObserveOwnership(candidate, localPlayerAvailable ? localPlayer : null, valid);
            observations.Add(ObserveCompanion(candidate, valid, ownership));

            if (!valid
                || !PrototypeContract.TryGetMapping(candidate.BaseId, out _)
                || !Configuration.IsMappingEnabled(candidate.BaseId))
            {
                continue;
            }

            validSourceCount++;
            if (ownership.IsOwned
                && PrototypeContract.TryGetSelectedMapping(
                    candidate.BaseId,
                    Configuration.SelectedTargetRowIds,
                    out var selectedMapping))
            {
                ownedTargets.Add(new TargetCandidate(candidate, selectedMapping));
            }
        }

        var selectionState = !localPlayerAvailable
            ? "local_player_unavailable"
            : observations.Count == 0
                ? "no_companion"
                : validSourceCount == 0
                    ? "source_not_present"
                    : ownedTargets.Count == 0
                        ? "source_not_owned"
                        : ownedTargets.Count > 1
                            ? "multiple_owned_sources"
                            : "target_ready";

        var localPlayerObservation = localPlayerAvailable
            ? ObserveLocalPlayer(localPlayer!)
            : null;

        return new TargetScanResult(
            ownedTargets.Count == 1 ? ownedTargets[0].Actor : null,
            ownedTargets.Count == 1 ? ownedTargets[0].Mapping : null,
            localPlayerObservation,
            selectionState,
            observations);
    }

    private void UpdateNormalSummonUnlockState()
    {
        var bypassCount = Interlocked.Read(ref normalSummonBypassCount);
        if (bypassCount == normalSummonLastReportedBypassCount)
            return;

        var firstBypass = normalSummonLastReportedBypassCount == 0;
        normalSummonLastReportedBypassCount = bypassCount;
        if (!firstBypass)
            return;

        SetTransition("normal_summon_status_bypassed");
        Log.Warning(
            "Normal Companion summon status bypassed. CompanionRowId={CompanionRowId}, OriginalStatus={OriginalStatus}, BypassCount={BypassCount}.",
            Volatile.Read(ref normalSummonLastBypassedRowId),
            Volatile.Read(ref normalSummonLastOriginalStatus),
            bypassCount);
    }

    private uint GetActionStatusDetour(
        ActionManager* actionManager,
        ActionType actionType,
        uint actionId,
        ulong targetId,
        bool checkRecastActive,
        bool checkCastingActive,
        uint* outOptExtraInfo)
    {
        var originalStatus = getActionStatusHook!.Original(
            actionManager,
            actionType,
            actionId,
            targetId,
            checkRecastActive,
            checkCastingActive,
            outOptExtraInfo);
        if (Volatile.Read(ref normalSummonUnlockEnabled) != 1
            || actionType != ActionType.Companion
            || originalStatus != MinionHiddenActionStatus)
        {
            return originalStatus;
        }

        Volatile.Write(ref normalSummonLastBypassedRowId, unchecked((int)actionId));
        Volatile.Write(ref normalSummonLastOriginalStatus, unchecked((int)originalStatus));
        Interlocked.Exchange(ref normalSummonLastBypassedUtcTicks, DateTime.UtcNow.Ticks);
        Interlocked.Increment(ref normalSummonBypassCount);
        return 0;
    }

    private bool UseActionDetour(
        ActionManager* actionManager,
        ActionType actionType,
        uint actionId,
        ulong targetId,
        uint extraParam,
        ActionManager.UseActionMode mode,
        uint comboRouteId,
        bool* outOptAreaTargeted)
    {
        var unlockCompanionAction = Volatile.Read(ref normalSummonUnlockEnabled) == 1
            && actionType == ActionType.Companion;
        var originalStatus = unlockCompanionAction
            ? getActionStatusHook!.Original(
                actionManager,
                actionType,
                actionId,
                targetId,
                true,
                true,
                null)
            : 0;
        var originalResult = useActionHook!.Original(
            actionManager,
            actionType,
            actionId,
            targetId,
            extraParam,
            mode,
            comboRouteId,
            outOptAreaTargeted);
        if (!unlockCompanionAction)
        {
            return originalResult;
        }

        Volatile.Write(ref normalSummonLastIconClickRowId, unchecked((int)actionId));
        Interlocked.Increment(ref normalSummonIconClickCount);
        if (originalStatus != MinionHiddenActionStatus)
        {
            Volatile.Write(
                ref normalSummonLastIconClickResult,
                originalResult ? "original_action_executed" : "original_action_rejected");
            return originalResult;
        }

        var nativeResult = actionManager->UseActionLocation(
            actionType,
            actionId,
            targetId,
            null,
            extraParam);
        Volatile.Write(
            ref normalSummonLastIconClickResult,
            nativeResult ? "native_action_executed" : "native_action_rejected");
        Log.Information(
            "Companion click native action result. CompanionRowId={CompanionRowId}, OriginalStatus={OriginalStatus}, OriginalResult={OriginalResult}, UseActionLocationResult={NativeResult}.",
            actionId,
            originalStatus,
            originalResult,
            nativeResult);
        if (nativeResult)
            return true;

        if (ObjectTable.LocalPlayer is not { } localPlayer || localPlayer.Address == nint.Zero)
        {
            Volatile.Write(ref normalSummonLastIconClickResult, "local_companion_unavailable");
            return false;
        }

        try
        {
            var character = (Character*)localPlayer.Address;
            var activeCompanionRowId = character->CompanionData.CompanionId;
            var requestedCompanionRowId = activeCompanionRowId == actionId
                ? 0u
                : actionId;
            character->CompanionData.SetupCompanion(
                unchecked((short)requestedCompanionRowId),
                1);

            var result = requestedCompanionRowId == 0
                ? "local_companion_dismiss_invoked"
                : activeCompanionRowId == 0
                    ? "local_companion_summon_invoked"
                    : "local_companion_replace_invoked";
            Volatile.Write(ref normalSummonLastIconClickResult, result);
            Log.Information(
                "Companion click local transition invoked. ClickedCompanionRowId={ClickedCompanionRowId}, ActiveCompanionRowId={ActiveCompanionRowId}, RequestedCompanionRowId={RequestedCompanionRowId}, TransitionParameter=1, Result={Result}.",
                actionId,
                activeCompanionRowId,
                requestedCompanionRowId,
                result);
            return true;
        }
        catch (Exception exception)
        {
            Volatile.Write(ref normalSummonLastIconClickResult, "local_companion_invoke_failed");
            Log.Error(
                exception,
                "Companion click local transition failed. CompanionRowId={CompanionRowId}.",
                actionId);
            return false;
        }
    }

    private NormalCompanionSummonUnlockObservation ObserveNormalCompanionSummonUnlock()
    {
        var bypassCount = Interlocked.Read(ref normalSummonBypassCount);
        var lastBypassedRowId = Volatile.Read(ref normalSummonLastBypassedRowId);
        var lastOriginalStatus = Volatile.Read(ref normalSummonLastOriginalStatus);
        var lastBypassedUtcTicks = Interlocked.Read(ref normalSummonLastBypassedUtcTicks);
        var companionObserved = lastBypassedRowId > 0
            && lastScan.Companions.Any(companion =>
                companion.IsValid
                && companion.IsOwnedByLocalPlayer
                && companion.BaseId == (uint)lastBypassedRowId);
        var hookAvailable = getActionStatusHook is not null;
        var hookEnabled = getActionStatusHook?.IsEnabled == true;
        var actionHookAvailable = useActionHook is not null;
        var actionHookEnabled = useActionHook?.IsEnabled == true;
        var iconClickCount = Interlocked.Read(ref normalSummonIconClickCount);
        var lastIconClickRowId = Volatile.Read(ref normalSummonLastIconClickRowId);
        var lastIconClickResult = Volatile.Read(ref normalSummonLastIconClickResult);
        var result = !Configuration.ExperimentalEnableNormalCompanionSummon
            ? "disabled"
            : !hookAvailable || !actionHookAvailable
                ? "hook_unavailable"
                : companionObserved
                    ? "companion_observed"
                    : iconClickCount > 0
                        ? lastIconClickResult ?? "icon_click_observed"
                    : bypassCount > 0
                        ? "status_bypassed"
                        : "ready";

        return new NormalCompanionSummonUnlockObservation(
            Configuration.ExperimentalEnableNormalCompanionSummon,
            hookAvailable,
            hookEnabled,
            actionHookAvailable,
            actionHookEnabled,
            "all_companion_rows",
            bypassCount,
            lastBypassedRowId > 0 ? unchecked((uint)lastBypassedRowId) : null,
            lastOriginalStatus > 0 ? unchecked((uint)lastOriginalStatus) : null,
            lastBypassedUtcTicks > 0
                ? new DateTimeOffset(new DateTime(lastBypassedUtcTicks, DateTimeKind.Utc))
                : null,
            iconClickCount,
            lastIconClickRowId > 0 ? unchecked((uint)lastIconClickRowId) : null,
            lastIconClickResult,
            result,
            companionObserved);
    }

    private static bool IsOwnedByLocalPlayer(IGameObject candidate, IGameObject localPlayer)
        => ObserveOwnership(candidate, localPlayer, candidate.Address != nint.Zero && candidate.IsValid()).IsOwned;

    private static OwnershipObservation ObserveOwnership(
        IGameObject candidate,
        IGameObject? localPlayer,
        bool candidateValid)
    {
        if (!candidateValid)
            return new OwnershipObservation(false, "candidate_invalid");
        if (localPlayer is null || localPlayer.Address == nint.Zero)
            return new OwnershipObservation(false, "local_player_unavailable");

        var companion = (Companion*)candidate.Address;
        if (companion->Owner == (BattleChara*)localPlayer.Address)
            return new OwnershipObservation(true, "native_owner_pointer");

        var localEntityId = GetObjectIdPart(localPlayer.EntityId);
        var localGameObjectId = GetObjectIdPart(localPlayer.GameObjectId);
        var ownerId = GetObjectIdPart(candidate.OwnerId);
        if (IsSameObjectId(ownerId, localEntityId))
            return new OwnershipObservation(true, "owner_id_matches_entity_id");
        if (IsSameObjectId(ownerId, localGameObjectId))
            return new OwnershipObservation(true, "owner_id_matches_game_object_id");
        return new OwnershipObservation(false, "owner_mismatch");
    }

    private static CompanionRuntimeObservation ObserveCompanion(
        IGameObject candidate,
        bool valid,
        OwnershipObservation ownership)
    {
        int? modelCharaId = null;
        var drawObject = Hex(0);
        var renderFlags = Hex(0);
        string? modelType = null;

        if (valid)
        {
            var character = (Character*)candidate.Address;
            var gameObject = (GameObject*)candidate.Address;
            modelCharaId = character->ModelContainer.ModelCharaId;
            drawObject = Hex((ulong)(nuint)gameObject->DrawObject);
            renderFlags = Hex((ulong)gameObject->RenderFlags);
            var characterBase = gameObject->GetCharacterBase();
            modelType = characterBase == null ? null : characterBase->GetModelType().ToString();
        }

        return new CompanionRuntimeObservation(
            candidate.ObjectIndex,
            Hex((ulong)(nuint)candidate.Address),
            candidate.BaseId,
            candidate.Name.ToString(),
            Hex(candidate.GameObjectId),
            Hex(candidate.EntityId),
            Hex(candidate.OwnerId),
            valid,
            ownership.IsOwned,
            ownership.Evidence,
            modelCharaId,
            drawObject,
            renderFlags,
            modelType);
    }

    private static LocalPlayerRuntimeObservation ObserveLocalPlayer(IGameObject localPlayer)
    {
        var character = (Character*)localPlayer.Address;
        return new LocalPlayerRuntimeObservation(
            localPlayer.ObjectIndex,
            Hex((ulong)(nuint)localPlayer.Address),
            Hex(localPlayer.GameObjectId),
            Hex(localPlayer.EntityId),
            character->CompanionData.CompanionId,
            Hex((ulong)(nuint)character->CompanionData.CompanionObject),
            Hex((ulong)(nuint)character->ChildObject));
    }

    private static bool TryCapture(IGameObject actor, out AppearancePayload appearance)
    {
        var character = (Character*)actor.Address;
        var gameObject = (GameObject*)actor.Address;
        var characterBase = gameObject == null ? null : gameObject->GetCharacterBase();
        if (character == null || gameObject == null || gameObject->DrawObject == null || characterBase == null)
        {
            appearance = null!;
            return false;
        }

        appearance = new AppearancePayload(
            checked((uint)character->ModelContainer.ModelCharaId),
            character->DrawData.CustomizeData.Data.ToArray(),
            character->DrawData.EquipmentModelIds.ToArray().Select(static item => item.Value).ToArray(),
            characterBase->GetModelType() == CharacterBase.ModelType.Human);
        return appearance.Customize.Length == 26 && appearance.Equipment.Length == 10;
    }

    private static bool TryWrite(IGameObject actor, AppearancePayload appearance)
    {
        var character = (Character*)actor.Address;
        if (character == null
            || appearance.Customize.Length != character->DrawData.CustomizeData.Data.Length
            || appearance.Equipment.Length != character->DrawData.EquipmentModelIds.Length)
        {
            return false;
        }

        character->ModelContainer.ModelCharaId = checked((int)appearance.ModelCharaId);
        appearance.Customize.AsSpan().CopyTo(character->DrawData.CustomizeData.Data);
        for (var index = 0; index < appearance.Equipment.Length; ++index)
            character->DrawData.EquipmentModelIds[index].Value = appearance.Equipment[index];
        return true;
    }

    private static bool TryCaptureModelScale(
        IGameObject actor,
        float? targetModelScale,
        out float? originalModelScale)
    {
        originalModelScale = null;
        if (targetModelScale is null)
            return true;

        var gameObject = (GameObject*)actor.Address;
        var characterBase = gameObject == null ? null : gameObject->GetCharacterBase();
        if (characterBase == null)
            return false;

        originalModelScale = *(float*)((byte*)characterBase + CharacterBaseModelScaleOffset);
        return true;
    }

    private static bool TryApplyAppearance(
        IGameObject actor,
        AppearancePayload appearance,
        float? modelScale)
    {
        try
        {
            var gameObject = (GameObject*)actor.Address;
            if (gameObject == null || !TryWrite(actor, appearance))
                return false;

            gameObject->RenderFlags |= NativeVisibilityFlags.Model;
            gameObject->DisableDraw();
            gameObject->RenderFlags &= ~NativeVisibilityFlags.Model;
            gameObject->EnableDraw();

            var character = (Character*)actor.Address;
            if (appearance.IsHuman && !TryLoadHumanEquipment(character, appearance.Equipment))
                return false;

            if (modelScale is { } requestedScale)
            {
                var characterBase = gameObject->GetCharacterBase();
                if (characterBase == null)
                    return false;

                *(float*)((byte*)characterBase + CharacterBaseModelScaleOffset) = requestedScale;
            }

            return true;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Backing write or full redraw call failed.");
            return false;
        }
    }

    private static bool TryDisableAppearanceDraw(IGameObject actor)
    {
        try
        {
            var gameObject = (GameObject*)actor.Address;
            if (gameObject == null)
                return false;

            gameObject->RenderFlags |= NativeVisibilityFlags.Model;
            gameObject->DisableDraw();
            return true;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Appearance draw disable failed.");
            return false;
        }
    }

    private static bool TryEnableAppearanceDraw(IGameObject actor, float? modelScale)
    {
        try
        {
            var gameObject = (GameObject*)actor.Address;
            if (gameObject == null)
                return false;

            gameObject->RenderFlags &= ~NativeVisibilityFlags.Model;
            gameObject->EnableDraw();
            PreserveModelScaleIfAvailable(actor, modelScale);
            return true;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Appearance draw enable failed.");
            return false;
        }
    }

    private static void PreserveModelScaleIfAvailable(IGameObject actor, float? modelScale)
    {
        if (modelScale is not { } requestedScale)
            return;

        var gameObject = (GameObject*)actor.Address;
        var characterBase = gameObject == null ? null : gameObject->GetCharacterBase();
        if (characterBase != null
            && MathF.Abs(*(float*)((byte*)characterBase + CharacterBaseModelScaleOffset) - requestedScale) >= 0.0001f)
        {
            *(float*)((byte*)characterBase + CharacterBaseModelScaleOffset) = requestedScale;
        }
    }

    private static AppearanceFinalizeResult TryFinalizeAppearance(
        IGameObject actor,
        AppearancePayload appearance,
        float? modelScale)
    {
        try
        {
            var gameObject = (GameObject*)actor.Address;
            var character = (Character*)actor.Address;
            var characterBase = gameObject == null ? null : gameObject->GetCharacterBase();
            if (gameObject == null || character == null)
                return AppearanceFinalizeResult.Failed;
            if (gameObject->DrawObject == null || characterBase == null)
                return AppearanceFinalizeResult.Pending;
            if (appearance.IsHuman && characterBase->GetModelType() != CharacterBase.ModelType.Human)
                return AppearanceFinalizeResult.Pending;

            if (appearance.IsHuman && !TryLoadHumanEquipment(character, appearance.Equipment))
                return AppearanceFinalizeResult.Failed;

            if (modelScale is { } requestedScale)
                *(float*)((byte*)characterBase + CharacterBaseModelScaleOffset) = requestedScale;

            return AppearanceFinalizeResult.Applied;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Appearance equipment finalization failed.");
            return AppearanceFinalizeResult.Failed;
        }
    }

    private static bool IsAppearanceApplied(
        IGameObject actor,
        AppearancePayload appearance,
        float? modelScale)
    {
        var gameObject = (GameObject*)actor.Address;
        var character = (Character*)actor.Address;
        if (gameObject == null
            || character == null
            || character->ModelContainer.ModelCharaId != checked((int)appearance.ModelCharaId)
            || !appearance.Customize.AsSpan().SequenceEqual(character->DrawData.CustomizeData.Data))
        {
            return false;
        }

        for (var index = 0; index < appearance.Equipment.Length; ++index)
            if (character->DrawData.EquipmentModelIds[index].Value != appearance.Equipment[index])
                return false;

        if (modelScale is not { } expectedScale)
            return true;

        var characterBase = gameObject->GetCharacterBase();
        return characterBase != null
            && MathF.Abs(*(float*)((byte*)characterBase + CharacterBaseModelScaleOffset) - expectedScale) < 0.0001f;
    }

    private static bool TryLoadHumanEquipment(Character* character, IReadOnlyList<ulong> equipment)
    {
        if (character == null || equipment.Count != character->DrawData.EquipmentModelIds.Length)
            return false;

        for (var index = 0; index < equipment.Count; ++index)
        {
            var model = new EquipmentModelId { Value = equipment[index] };
            character->DrawData.LoadEquipment((DrawDataContainer.EquipmentSlot)index, &model, true);
        }

        return true;
    }

    private static bool TryResolve(ActorIdentity identity, out IGameObject actor)
    {
        var current = ObjectTable[identity.ObjectIndex];
        if (current is null
            || current.Address == nint.Zero
            || !current.IsValid()
            || current.ObjectKind != ObjectKind.Companion
            || current.BaseId != identity.BaseId
            || !PrototypeContract.TryGetMapping(current.BaseId, out _)
            || current.GameObjectId != identity.GameObjectId
            || current.EntityId != identity.EntityId
            || ObjectTable.LocalPlayer is not { } localPlayer
            || !IsOwnedByLocalPlayer(current, localPlayer))
        {
            actor = null!;
            return false;
        }

        actor = current;
        return true;
    }

    private static bool IsSameObjectId(uint left, uint right)
        => left != 0 && right != 0 && left == right;

    private static uint GetObjectIdPart(ulong value)
        => (uint)(value & uint.MaxValue);

    private static string Hex(ulong value)
        => $"0x{value:X16}";

    private void SetTransition(string transition)
    {
        if (string.Equals(lastTransition, transition, StringComparison.Ordinal))
            return;

        lastTransition = transition;
        lastTransitionAtUtc = DateTimeOffset.UtcNow;
    }

    private void PublishRuntimeState(string pluginState, bool force = false)
    {
        var snapshot = new RuntimeStateSnapshot(
            RuntimeStateReporter.CurrentSchemaVersion,
            DateTimeOffset.UtcNow,
            pluginState,
            PrototypeContract.Mappings.Select(GetSelectedMapping).Select(ObserveMapping).ToArray(),
            lastScan.LocalPlayer,
            ObserveNormalCompanionSummonUnlock(),
            lastScan.SelectionState,
            lastScan.Companions,
            tracked is null ? null : ObserveTracked(tracked.Identity, tracked.Mapping, tracked.Stage.ToString()),
            failedActor is null ? null : ObserveFailed(failedActor),
            lastTransition,
            lastTransitionAtUtc,
            lastError);
        stateReporter.TryWrite(snapshot, force);
    }

    private static RuntimeMappingObservation ObserveMapping(PrototypeMapping mapping)
        => new(
            mapping.SourceCompanionRowId,
            mapping.SourceName,
            mapping.TargetKind.ToString(),
            mapping.TargetRowId,
            mapping.TargetModelCharaRowId,
            mapping.TargetName,
            mapping.IsHuman,
            mapping.TargetModelScale);

    private static TrackedRuntimeObservation ObserveFailed(FailedActorState failure)
        => ObserveTracked(failure.Identity, failure.Mapping, "failed");

    private static TrackedRuntimeObservation ObserveTracked(
        ActorIdentity identity,
        PrototypeMapping mapping,
        string stage)
        => new(
            identity.ObjectIndex,
            Hex(identity.GameObjectId),
            Hex(identity.EntityId),
            mapping.SourceCompanionRowId,
            mapping.TargetKind.ToString(),
            mapping.TargetRowId,
            mapping.TargetModelCharaRowId,
            stage);

    private delegate uint GetActionStatusDelegate(
        ActionManager* actionManager,
        ActionType actionType,
        uint actionId,
        ulong targetId,
        bool checkRecastActive,
        bool checkCastingActive,
        uint* outOptExtraInfo);

    private delegate bool UseActionDelegate(
        ActionManager* actionManager,
        ActionType actionType,
        uint actionId,
        ulong targetId,
        uint extraParam,
        ActionManager.UseActionMode mode,
        uint comboRouteId,
        bool* outOptAreaTargeted);

    private enum ApplyStage
    {
        Pending,
        BackingWritten,
        Disabled,
        HiddenBackingWritten,
        Enabled,
        Verify,
        Redrawn,
    }

    private enum AppearanceFinalizeResult
    {
        Pending,
        Applied,
        Failed,
    }

    private readonly record struct ActorIdentity(
        ushort ObjectIndex,
        ulong GameObjectId,
        uint EntityId,
        uint BaseId)
    {
        public static ActorIdentity From(IGameObject actor)
            => new(actor.ObjectIndex, actor.GameObjectId, actor.EntityId, actor.BaseId);

        public bool Matches(IGameObject actor)
            => actor.ObjectIndex == ObjectIndex
                && actor.GameObjectId == GameObjectId
                && actor.EntityId == EntityId
                && actor.BaseId == BaseId;
    }

    private sealed record TrackedActor(
        ActorIdentity Identity,
        AppearancePayload Original,
        float? OriginalModelScale,
        PrototypeMapping Mapping,
        ApplyStage Stage,
        nint AppliedDrawObject);

    private sealed record FailedActorState(
        ActorIdentity Identity,
        PrototypeMapping Mapping);

    private readonly record struct OwnershipObservation(bool IsOwned, string Evidence);

    private sealed record TargetScanResult(
        IGameObject? Candidate,
        PrototypeMapping? Mapping,
        LocalPlayerRuntimeObservation? LocalPlayer,
        string SelectionState,
        IReadOnlyList<CompanionRuntimeObservation> Companions)
    {
        public static TargetScanResult NotStarted { get; } =
            new(null, null, null, "not_started", Array.Empty<CompanionRuntimeObservation>());
    }

    private sealed record TargetCandidate(IGameObject Actor, PrototypeMapping Mapping);
}
