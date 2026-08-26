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
    private readonly AppearancePayload desired;
    private TrackedActor? tracked;
    private ActorIdentity? failedIdentity;
    private bool disposed;

    public Plugin()
    {
        desired = YshtolaAppearanceResolver.Resolve(DataManager);
        injector = new NativeDrawObjectInjector(Interop);
        Framework.Update += OnFrameworkUpdate;

        Log.Information(
            "MinionToNPC prototype loaded. Companion={CompanionRowId}, ENpcBase={EventNpcRowId}.",
            PrototypeContract.SourceCompanionRowId,
            PrototypeContract.TargetEventNpcRowId);
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
                Log.Error("Failed to restore the tracked Y'shtola minion while unloading.");
        }

        tracked = null;
        injector.Dispose();
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        try
        {
            UpdateTarget();
        }
        catch (Exception exception)
        {
            Log.Error(exception, "MinionToNPC update failed; stopping writes for the current actor.");
            FailCurrentActor();
        }
    }

    private void UpdateTarget()
    {
        var candidate = FindTarget();

        if (tracked is not null
            && (candidate is null || !tracked.Identity.Matches(candidate)))
        {
            if (TryResolve(tracked.Identity, out var oldActor)
                && !TryImmediateRedraw(oldActor, tracked.Original))
            {
                Log.Error("Failed to restore the previous tracked Companion before identity changed.");
            }

            tracked = null;
        }

        if (candidate is null)
        {
            failedIdentity = null;
            return;
        }

        var identity = ActorIdentity.From(candidate);
        if (failedIdentity is { } failed && failed.Equals(identity))
            return;

        if (tracked is null)
        {
            if (!TryCapture(candidate, out var original))
                return;

            tracked = new TrackedActor(identity, original, ApplyStage.WriteVisible);
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

        var next = state.Stage switch
        {
            ApplyStage.WriteVisible when TryWrite(current, desired) => ApplyStage.Disable,
            ApplyStage.Disable when TryDisable(current) => ApplyStage.WriteHidden,
            ApplyStage.WriteHidden when TryWrite(current, desired) => ApplyStage.Enable,
            ApplyStage.Enable when TryEnable(current, desired) => ApplyStage.Finalize,
            ApplyStage.Finalize when TryFinalizeHumanEquipment(current, desired) => ApplyStage.Verify,
            ApplyStage.Verify when IsApplied(current, desired) => ApplyStage.Applied,
            _ => ApplyStage.Failed,
        };

        if (next == ApplyStage.Failed)
        {
            Log.Error("Y'shtola appearance apply failed at stage {Stage}; attempting one rollback.", state.Stage);
            FailCurrentActor();
            return;
        }

        tracked = state with { Stage = next };
        if (next == ApplyStage.Applied)
        {
            Log.Information(
                "Y'shtola appearance applied. Companion={CompanionRowId}, ENpcBase={EventNpcRowId}.",
                PrototypeContract.SourceCompanionRowId,
                PrototypeContract.TargetEventNpcRowId);
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
            Log.Error("Rollback failed for the current Y'shtola minion.");
        }

        failedIdentity = state.Identity;
        tracked = null;
    }

    private static IGameObject? FindTarget()
    {
        var localPlayer = ObjectTable.LocalPlayer;
        if (localPlayer is null || localPlayer.Address == nint.Zero)
            return null;

        IGameObject? result = null;
        foreach (var candidate in ObjectTable)
        {
            if (candidate.ObjectKind != ObjectKind.Companion
                || candidate.BaseId != PrototypeContract.SourceCompanionRowId
                || candidate.Address == nint.Zero
                || !candidate.IsValid()
                || !IsOwnedByLocalPlayer(candidate, localPlayer))
            {
                continue;
            }

            if (result is not null)
                return null;
            result = candidate;
        }

        return result;
    }

    private static bool IsOwnedByLocalPlayer(IGameObject candidate, IGameObject localPlayer)
    {
        var companion = (Companion*)candidate.Address;
        if (companion->Owner == (BattleChara*)localPlayer.Address)
            return true;

        var localEntityId = GetObjectIdPart(localPlayer.EntityId);
        var localGameObjectId = GetObjectIdPart(localPlayer.GameObjectId);
        var ownerId = GetObjectIdPart(candidate.OwnerId);
        return IsSameObjectId(ownerId, localEntityId)
            || IsSameObjectId(ownerId, localGameObjectId);
    }

    private static bool TryCapture(IGameObject actor, out AppearancePayload appearance)
    {
        var character = (Character*)actor.Address;
        var gameObject = (GameObject*)actor.Address;
        if (character == null || gameObject == null || gameObject->DrawObject == null)
        {
            appearance = null!;
            return false;
        }

        appearance = new AppearancePayload(
            checked((uint)character->ModelContainer.ModelCharaId),
            character->DrawData.CustomizeData.Data.ToArray(),
            character->DrawData.EquipmentModelIds.ToArray().Select(static item => item.Value).ToArray());
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

    private static bool TryFinalizeHumanEquipment(IGameObject actor, AppearancePayload appearance)
    {
        var gameObject = (GameObject*)actor.Address;
        var characterBase = gameObject == null ? null : gameObject->GetCharacterBase();
        var character = (Character*)actor.Address;
        if (characterBase == null
            || characterBase->GetModelType() != CharacterBase.ModelType.Human
            || character == null
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
            || characterBase->GetModelType() != CharacterBase.ModelType.Human
            || character == null
            || character->ModelContainer.ModelCharaId != appearance.ModelCharaId
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
                && (characterBase->GetModelType() != CharacterBase.ModelType.Human
                    || TryFinalizeHumanEquipment(actor, appearance))
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
            || current.BaseId != PrototypeContract.SourceCompanionRowId
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
        uint EntityId)
    {
        public static ActorIdentity From(IGameObject actor)
            => new(actor.ObjectIndex, actor.GameObjectId, actor.EntityId);

        public bool Matches(IGameObject actor)
            => actor.ObjectIndex == ObjectIndex
                && actor.GameObjectId == GameObjectId
                && actor.EntityId == EntityId;
    }

    private sealed record TrackedActor(
        ActorIdentity Identity,
        AppearancePayload Original,
        ApplyStage Stage);
}
