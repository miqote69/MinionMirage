namespace MinionToNPC;

internal sealed record AppearancePayload(
    uint ModelCharaId,
    byte[] Customize,
    ulong[] Equipment);
