namespace MinionMirage;

internal sealed record AppearancePayload(
    uint ModelCharaId,
    byte[] Customize,
    ulong[] Equipment,
    bool IsHuman);
