using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace MinionToNPC;

internal sealed unsafe class NativeDrawObjectInjector : IDisposable
{
    private readonly Hook<CreateCharacterBaseDelegate> hook;
    private AppearancePayload? active;
    private bool injectedDuringInvoke;
    private bool disposed;

    public NativeDrawObjectInjector(IGameInteropProvider interop)
    {
        hook = interop.HookFromAddress<CreateCharacterBaseDelegate>(
            (nint)CharacterBase.MemberFunctionPointers.Create,
            CreateCharacterBaseDetour);
    }

    public bool Invoke(GameObject* gameObject, AppearancePayload appearance)
    {
        if (disposed || active is not null)
            throw new InvalidOperationException("Draw object injection is unavailable or already active.");

        hook.Enable();
        active = appearance;
        injectedDuringInvoke = false;
        try
        {
            gameObject->EnableDraw();
            return injectedDuringInvoke;
        }
        finally
        {
            active = null;
            hook.Disable();
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        active = null;
        hook.Dispose();
    }

    private CharacterBase* CreateCharacterBaseDetour(
        uint modelId,
        CustomizeData* customize,
        EquipmentModelId* equipment,
        byte unknown)
    {
        var appearance = active;
        if (appearance is null)
            return hook.Original(modelId, customize, equipment, unknown);

        var injectedCustomize = default(CustomizeData);
        if (appearance.Customize.Length != injectedCustomize.Data.Length)
            return hook.Original(modelId, customize, equipment, unknown);
        appearance.Customize.AsSpan().CopyTo(injectedCustomize.Data);

        const int equipmentSlotCount = 10;
        if (appearance.Equipment.Length != equipmentSlotCount)
            return hook.Original(modelId, customize, equipment, unknown);

        EquipmentModelId* injectedEquipment = stackalloc EquipmentModelId[equipmentSlotCount];
        for (var index = 0; index < equipmentSlotCount; ++index)
            injectedEquipment[index].Value = appearance.Equipment[index];

        injectedDuringInvoke = true;
        return hook.Original(
            appearance.ModelCharaId,
            &injectedCustomize,
            injectedEquipment,
            unknown);
    }

    private delegate CharacterBase* CreateCharacterBaseDelegate(
        uint modelId,
        CustomizeData* customize,
        EquipmentModelId* equipment,
        byte unknown);
}
