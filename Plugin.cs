using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using NativeVisibilityFlags = FFXIVClientStructs.FFXIV.Client.Game.Object.VisibilityFlags;

namespace MinionToNPC;

public sealed unsafe class Plugin : IDalamudPlugin
{
    public const string DisplayName = "Minion To NPC";

    [PluginService] private static IObjectTable ObjectTable { get; set; } = null!;
    [PluginService] private static IFramework Framework { get; set; } = null!;
    [PluginService] private static IDataManager DataManager { get; set; } = null!;
    [PluginService] private static IGameInteropProvider Interop { get; set; } = null!;
    [PluginService] private static IPluginLog Log { get; set; } = null!;

    private readonly NativeDrawObjectInjector injector;
    private readonly IReadOnlyDictionary<uint, AppearancePayload> desiredBySource;
    private readonly RuntimeStateReporter stateReporter;
    private TrackedActor? tracked;
    private ActorIdentity? failedIdentity;
    private TargetScanResult lastScan = TargetScanResult.NotStarted;
    private string lastTransition = "loaded";
    private DateTimeOffset lastTransitionAtUtc = DateTimeOffset.UtcNow;
    private string? lastError;
    private bool disposed;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        stateReporter = new RuntimeStateReporter(pluginInterface.ConfigDirectory.FullName, Log);
        desiredBySource = PrototypeContract.Mappings.ToDictionary(
            mapping => mapping.SourceCompanionRowId,
            mapping => TargetAppearanceResolver.Resolve(DataManager, mapping));
        injector = new NativeDrawObjectInjector(Interop);
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

        if (tracked is not null && TryResolve(tracked.Identity, out var current))
        {
            if (!TryImmediateRedraw(current, tracked.Original))
            {
                Log.Error("Failed to restore the tracked Companion while unloading.");
                lastError = "unload_restore_failed";
            }
        }

        SetTransition("disposed");
        tracked = null;
        PublishRuntimeState("disposed", force: true);
        injector.Dispose();
    }

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
                && !TryImmediateRedraw(oldActor, tracked.Original))
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
            if (!TryCapture(candidate, out var original))
            {
                SetTransition("capture_not_ready");
                return;
            }

            tracked = new TrackedActor(identity, original, mapping, ApplyStage.WriteVisible);
            lastError = null;
            SetTransition("target_acquired");
            Log.Information(
                "Target Companion acquired. ObjectIndex={ObjectIndex}, GameObjectId={GameObjectId:X16}.",
                identity.ObjectIndex,
                identity.GameObjectId);
        }

        AdvanceTrackedActor();
    }

    private void AdvanceTrackedActor()
    {
        var state = tracked;
        if (state is null || state.Stage == ApplyStage.Applied)
            return;

        if (!TryResolve(state.Identity, out var current))
        {
            tracked = null;
            return;
        }

        var desired = desiredBySource[state.Mapping.SourceCompanionRowId];
        var next = state.Stage switch
        {
            ApplyStage.WriteVisible when TryWrite(current, desired) => ApplyStage.Disable,
            ApplyStage.Disable when TryDisable(current) => ApplyStage.WriteHidden,
            ApplyStage.WriteHidden when TryWrite(current, desired) => ApplyStage.Enable,
            ApplyStage.Enable when TryEnable(current, desired) => ApplyStage.Finalize,
            ApplyStage.Finalize when TryFinalizeAppearance(current, desired) => ApplyStage.Verify,
            ApplyStage.Verify when IsApplied(current, desired) => ApplyStage.Applied,
            _ => ApplyStage.Failed,
        };

        if (next == ApplyStage.Failed)
        {
            Log.Error(
                "{TargetName} appearance apply failed at stage {Stage}; attempting one rollback.",
                state.Mapping.TargetName,
                state.Stage);
            lastError = $"appearance_apply_failed_at_{state.Stage}";
            SetTransition($"apply_failed_at_{state.Stage}");
            FailCurrentActor();
            return;
        }

        tracked = state with { Stage = next };
        SetTransition($"apply_{state.Stage}_to_{next}");
        if (next == ApplyStage.Applied)
        {
            lastError = null;
            Log.Information(
                "{TargetName} appearance applied. Companion={CompanionRowId}, {TargetKind}={TargetRowId}, ModelChara={ModelCharaRowId}.",
                state.Mapping.TargetName,
                state.Mapping.SourceCompanionRowId,
                state.Mapping.TargetKind,
                state.Mapping.TargetRowId,
                desired.ModelCharaId);
        }
    }

    private void FailCurrentActor()
    {
        var state = tracked;
        if (state is null)
            return;

        if (TryResolve(state.Identity, out var current)
            && !TryImmediateRedraw(current, state.Original))
        {
            Log.Error("Rollback failed for the current mapped minion.");
            lastError = "rollback_failed";
            SetTransition("rollback_failed");
        }

        failedIdentity = state.Identity;
        tracked = null;
    }

    private static TargetScanResult ScanTargets()
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

            if (!valid || !PrototypeContract.TryGetMapping(candidate.BaseId, out var mapping))
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

    private static bool TryDisable(IGameObject actor)
    {
        var gameObject = (GameObject*)actor.Address;
        if (gameObject == null)
            return false;

        gameObject->RenderFlags |= NativeVisibilityFlags.Model;
        gameObject->DisableDraw();
        return true;
    }

    private bool TryEnable(IGameObject actor, AppearancePayload appearance)
    {
        var gameObject = (GameObject*)actor.Address;
        if (gameObject == null)
            return false;

        gameObject->RenderFlags &= ~NativeVisibilityFlags.Model;
        injector.Invoke(gameObject, appearance);
        return gameObject->DrawObject != null;
    }

    private static bool TryFinalizeAppearance(IGameObject actor, AppearancePayload appearance)
    {
        var gameObject = (GameObject*)actor.Address;
        var characterBase = gameObject == null ? null : gameObject->GetCharacterBase();
        var character = (Character*)actor.Address;
        if (characterBase == null || character == null)
            return false;

        if (!appearance.IsHuman)
            return characterBase->GetModelType() != CharacterBase.ModelType.Human;

        if (characterBase->GetModelType() != CharacterBase.ModelType.Human
            || appearance.Equipment.Length != character->DrawData.EquipmentModelIds.Length)
        {
            return false;
        }

        for (var index = 0; index < appearance.Equipment.Length; ++index)
        {
            var model = new EquipmentModelId { Value = appearance.Equipment[index] };
            character->DrawData.LoadEquipment((DrawDataContainer.EquipmentSlot)index, &model, true);
        }

        return true;
    }

    private static bool IsApplied(IGameObject actor, AppearancePayload appearance)
    {
        var gameObject = (GameObject*)actor.Address;
        var characterBase = gameObject == null ? null : gameObject->GetCharacterBase();
        var character = (Character*)actor.Address;
        if (characterBase == null
            || character == null
            || character->ModelContainer.ModelCharaId != appearance.ModelCharaId)
        {
            return false;
        }

        if (!appearance.IsHuman)
            return characterBase->GetModelType() != CharacterBase.ModelType.Human;

        if (characterBase->GetModelType() != CharacterBase.ModelType.Human
            || !appearance.Customize.AsSpan().SequenceEqual(character->DrawData.CustomizeData.Data))
        {
            return false;
        }

        var equipment = character->DrawData.EquipmentModelIds;
        for (var index = 0; index < equipment.Length; ++index)
            if (equipment[index].Value != appearance.Equipment[index])
                return false;

        return true;
    }

    private bool TryImmediateRedraw(IGameObject actor, AppearancePayload appearance)
    {
        try
        {
            var gameObject = (GameObject*)actor.Address;
            if (gameObject == null)
                return false;

            var redrawn = TryWrite(actor, appearance)
                && TryDisable(actor)
                && TryWrite(actor, appearance)
                && TryEnable(actor, appearance);
            if (!redrawn)
                return false;

            var characterBase = gameObject->GetCharacterBase();
            return characterBase != null
                && TryFinalizeAppearance(actor, appearance)
                && IsBackingApplied(actor, appearance);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Immediate redraw failed.");
            return false;
        }
    }

    private static bool IsBackingApplied(IGameObject actor, AppearancePayload appearance)
    {
        var character = (Character*)actor.Address;
        if (character == null || character->ModelContainer.ModelCharaId != appearance.ModelCharaId)
            return false;
        if (!appearance.IsHuman)
            return true;
        if (!appearance.Customize.AsSpan().SequenceEqual(character->DrawData.CustomizeData.Data))
            return false;

        var equipment = character->DrawData.EquipmentModelIds;
        for (var index = 0; index < equipment.Length; ++index)
            if (equipment[index].Value != appearance.Equipment[index])
                return false;
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
            mapping.IsHuman);

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
        WriteVisible,
        Disable,
        WriteHidden,
        Enable,
        Finalize,
        Verify,
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
