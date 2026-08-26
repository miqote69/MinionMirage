namespace MinionToNPC;

internal static class PrototypeContract
{
    public static IReadOnlyList<PrototypeMapping> Mappings { get; } =
    [
        new(
            SourceCompanionRowId: 331,
            SourceName: "Y'shtola",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1003782,
            TargetModelCharaRowId: 0,
            TargetName: "Y'shtola",
            IsHuman: true),
        new(
            SourceCompanionRowId: 232,
            SourceName: "Scathach",
            TargetKind: PrototypeTargetKind.BattleNpc,
            TargetRowId: 6479,
            TargetModelCharaRowId: 1689,
            TargetName: "Scathach",
            IsHuman: false),
    ];

    public static bool TryGetMapping(uint sourceCompanionRowId, out PrototypeMapping mapping)
    {
        mapping = Mappings.FirstOrDefault(item => item.SourceCompanionRowId == sourceCompanionRowId)!;
        return mapping is not null;
    }
}

internal enum PrototypeTargetKind
{
    EventNpc,
    BattleNpc,
}

internal sealed record PrototypeMapping(
    uint SourceCompanionRowId,
    string SourceName,
    PrototypeTargetKind TargetKind,
    uint TargetRowId,
    uint TargetModelCharaRowId,
    string TargetName,
    bool IsHuman);
