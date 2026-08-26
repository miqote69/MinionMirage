namespace MinionToNPC;

internal static class PrototypeContract
{
    public static IReadOnlyList<PrototypeMapping> Mappings { get; } =
    [
        new(
            SourceCompanionRowId: 331,
            SourceName: "Y'shtola",
            TargetKind: PrototypeTargetKind.BattleNpc,
            TargetRowId: 13910,
            TargetModelCharaRowId: 0,
            TargetName: "Y'shtola",
            IsHuman: true,
            TargetModelScale: 1.0f),
        new(
            SourceCompanionRowId: 232,
            SourceName: "Scathach",
            TargetKind: PrototypeTargetKind.BattleNpc,
            TargetRowId: 6479,
            TargetModelCharaRowId: 1689,
            TargetName: "Scathach",
            IsHuman: false,
            TargetModelScale: null),
        new(
            SourceCompanionRowId: 218,
            SourceName: "Alisaie",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1017687,
            TargetModelCharaRowId: 0,
            TargetName: "Alisaie",
            IsHuman: true,
            TargetModelScale: 0.97f),
        new(
            SourceCompanionRowId: 398,
            SourceName: "Gaia",
            TargetKind: PrototypeTargetKind.BattleNpc,
            TargetRowId: 17830,
            TargetModelCharaRowId: 4436,
            TargetName: "Gaia",
            IsHuman: false,
            TargetModelScale: null),
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
    bool IsHuman,
    float? TargetModelScale);
