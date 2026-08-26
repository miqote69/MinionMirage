using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.IoC;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using MinionToNPC.Localization;
using System.Reflection;
using CompanionSheet = Lumina.Excel.Sheets.Companion;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using NativeVisibilityFlags = FFXIVClientStructs.FFXIV.Client.Game.Object.VisibilityFlags;

namespace MinionToNPC;

public sealed unsafe class Plugin : IDalamudPlugin
{
    public const string DisplayName = "Minion To NPC";
    private const int CharacterBaseModelScaleOffset = 0x2A4;

    [PluginService] private static IObjectTable ObjectTable { get; set; } = null!;
    [PluginService] private static IFramework Framework { get; set; } = null!;
    [PluginService] private static IDataManager DataManager { get; set; } = null!;
    [PluginService] private static IClientState ClientState { get; set; } = null!;
    [PluginService] private static ITextureProvider TextureProvider { get; set; } = null!;
    [PluginService] private static IPluginLog Log { get; set; } = null!;

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IReadOnlyDictionary<uint, AppearancePayload> desiredBySource;
    private readonly IReadOnlyDictionary<uint, uint> sourceModelCharaBySource;
    private readonly RuntimeStateReporter stateReporter;
    private readonly ConfigWindow configWindow;
    private readonly WindowSystem windowSystem = new("MinionToNPC");
    private TrackedActor? tracked;
    private ActorIdentity? failedIdentity;
    private TargetScanResult lastScan = TargetScanResult.NotStarted;
    private string lastTransition = "loaded";
    private DateTimeOffset lastTransitionAtUtc = DateTimeOffset.UtcNow;
    private string? lastError;
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
        Configuration.DisabledCompanionRowIds ??= [];
        Localizer = new Localizer(Configuration, ClientState);
        configWindow = new ConfigWindow(this);
        windowSystem.AddWindow(configWindow);
        pluginInterface.UiBuilder.Draw += windowSystem.Draw;
        pluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        stateReporter = new RuntimeStateReporter(pluginInterface.ConfigDirectory.FullName, Log);
        desiredBySource = PrototypeContract.Mappings.ToDictionary(
            mapping => mapping.SourceCompanionRowId,
            mapping => TargetAppearanceResolver.Resolve(DataManager, mapping));
        var companionSheet = DataManager.GetExcelSheet<CompanionSheet>();
        sourceModelCharaBySource = PrototypeContract.Mappings.ToDictionary(
            mapping => mapping.SourceCompanionRowId,
            mapping => companionSheet.TryGetRow(mapping.SourceCompanionRowId, out var companion)
                && companion.Model.RowId is not 0
                    ? companion.Model.RowId
                    : throw new InvalidOperationException(
                        $"Companion {mapping.SourceCompanionRowId} has no source ModelChara."));
        Framework.Update += OnFrameworkUpdate;

        Log.Information(
            "MinionToNPC prototype loaded. MappingCount={MappingCount}.",
            PrototypeContract.Mappings.Count);
        Log.Information("MinionToNPC runtime state: {StateFilePath}", stateReporter.StateFilePath);
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

    private void SaveConfiguration()
        => pluginInterface.SavePluginConfig(Configuration);

    private void OnFrameworkUpdate(IFramework framework)
    {
        try
        {
            lastScan = ScanTargets();
            UpdateTarget(lastScan.Candidate, lastScan.Mapping);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "MinionToNPC update failed; stopping writes for the current actor.");
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
        if (tracked is not null
            && (candidate is null || !tracked.Identity.Matches(candidate)))
        {
            if (TryResolve(tracked.Identity, out var oldActor)
                && !TryApplyAppearance(oldActor, tracked.Original, tracked.OriginalModelScale))
            {
                Log.Error("Failed to restore the previous tracked Companion before identity changed.");
                lastError = "previous_actor_restore_failed";
            }

            tracked = null;
            SetTransition("tracked_actor_changed_or_disappeared");
        }

        if (candidate is null || mapping is null)
        {
            failedIdentity = null;
            lastError = null;
            return;
        }

        var identity = ActorIdentity.From(candidate);
        if (failedIdentity is { } failed && failed.Equals(identity))
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

            tracked = new TrackedActor(identity, original, originalModelScale, mapping, ApplyStage.Pending);
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
        if (state is null || state.Stage == ApplyStage.Redrawn)
            return;

        if (!TryResolve(state.Identity, out var current))
        {
            tracked = null;
            return;
        }

        var desired = desiredBySource[state.Mapping.SourceCompanionRowId];
        if (!TryApplyAppearance(current, desired, state.Mapping.TargetModelScale))
        {
            Log.Error(
                "{TargetName} backing write or full redraw call failed; stopping for the current actor without rollback.",
                state.Mapping.TargetName);
            lastError = "appearance_redraw_failed";
            SetTransition("appearance_redraw_failed");
            FailCurrentActor();
            return;
        }

        tracked = state with { Stage = ApplyStage.Redrawn };
        lastError = null;
        SetTransition("appearance_redrawn");
        Log.Information(
            "{TargetName} backing written and full redraw requested. Companion={CompanionRowId}, {TargetKind}={TargetRowId}, ModelChara={ModelCharaRowId}.",
            state.Mapping.TargetName,
            state.Mapping.SourceCompanionRowId,
            state.Mapping.TargetKind,
            state.Mapping.TargetRowId,
            desired.ModelCharaId);
        if (state.Mapping.TargetModelScale is { } modelScale)
            Log.Information("{TargetName} draw-model scale set to {ModelScale}.", state.Mapping.TargetName, modelScale);
    }

    private void FailCurrentActor()
    {
        var state = tracked;
        if (state is null)
            return;

        failedIdentity = state.Identity;
        tracked = null;
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
                || !PrototypeContract.TryGetMapping(candidate.BaseId, out var mapping)
                || !Configuration.IsMappingEnabled(candidate.BaseId))
                continue;

            validSourceCount++;
            if (ownership.IsOwned)
                ownedTargets.Add(new TargetCandidate(candidate, mapping));
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
            ? new LocalPlayerRuntimeObservation(
                localPlayer!.ObjectIndex,
                Hex(localPlayer.GameObjectId),
                Hex(localPlayer.EntityId))
            : null;

        return new TargetScanResult(
            ownedTargets.Count == 1 ? ownedTargets[0].Actor : null,
            ownedTargets.Count == 1 ? ownedTargets[0].Mapping : null,
            localPlayerObservation,
            selectionState,
            observations);
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
        var drawObjectPresent = false;
        string? modelType = null;

        if (valid)
        {
            var character = (Character*)candidate.Address;
            var gameObject = (GameObject*)candidate.Address;
            modelCharaId = character->ModelContainer.ModelCharaId;
            drawObjectPresent = gameObject->DrawObject != null;
            var characterBase = gameObject->GetCharacterBase();
            modelType = characterBase == null ? null : characterBase->GetModelType().ToString();
        }

        return new CompanionRuntimeObservation(
            candidate.ObjectIndex,
            candidate.BaseId,
            candidate.Name.ToString(),
            Hex(candidate.GameObjectId),
            Hex(candidate.EntityId),
            Hex(candidate.OwnerId),
            valid,
            ownership.IsOwned,
            ownership.Evidence,
            modelCharaId,
            drawObjectPresent,
            modelType);
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
            PrototypeContract.Mappings.Select(ObserveMapping).ToArray(),
            lastScan.LocalPlayer,
            lastScan.SelectionState,
            lastScan.Companions,
            tracked is null ? null : ObserveTracked(tracked.Identity, tracked.Mapping, tracked.Stage.ToString()),
            failedIdentity is null ? null : ObserveFailed(failedIdentity.Value),
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

    private static TrackedRuntimeObservation? ObserveFailed(ActorIdentity identity)
        => PrototypeContract.TryGetMapping(identity.BaseId, out var mapping)
            ? ObserveTracked(identity, mapping, "failed")
            : null;

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

    private enum ApplyStage
    {
        Pending,
        Redrawn,
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
        ApplyStage Stage);

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
