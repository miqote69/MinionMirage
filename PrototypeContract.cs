namespace MinionMirage;

internal static class PrototypeContract
{
    private static readonly PrototypeMapping KhloeOptionB = new(
        SourceCompanionRowId: 260,
        SourceName: "Khloe",
        TargetKind: PrototypeTargetKind.EventNpc,
        TargetRowId: 1058181,
        TargetModelCharaRowId: 0,
        TargetName: "Khloe Aliapoh",
        IsHuman: true,
        TargetModelScale: 0.7f,
        AppearanceCategory: PrototypeAppearanceCategory.Human);

    private static readonly PrototypeMapping RyneOptionB = new(
        SourceCompanionRowId: 332,
        SourceName: "Ryne",
        TargetKind: PrototypeTargetKind.BattleNpc,
        TargetRowId: 10069,
        TargetModelCharaRowId: 2720,
        TargetName: "Minfilia",
        IsHuman: true,
        TargetModelScale: 0.86f,
        AppearanceCategory: PrototypeAppearanceCategory.Human);

    private static readonly PrototypeMapping ZhloeOptionB = new(
        SourceCompanionRowId: 298,
        SourceName: "Zhloe",
        TargetKind: PrototypeTargetKind.EventNpc,
        TargetRowId: 1015912,
        TargetModelCharaRowId: 0,
        TargetName: "Zhloe Aliapoh",
        IsHuman: true,
        TargetModelScale: null,
        AppearanceCategory: PrototypeAppearanceCategory.Human);

    private static readonly PrototypeMapping CirinaOptionB = new(
        SourceCompanionRowId: 293,
        SourceName: "Cirina",
        TargetKind: PrototypeTargetKind.EventNpc,
        TargetRowId: 1044730,
        TargetModelCharaRowId: 0,
        TargetName: "Cirina",
        IsHuman: true,
        TargetModelScale: null,
        AppearanceCategory: PrototypeAppearanceCategory.Human);

    private static readonly PrototypeMapping SaduOptionB = new(
        SourceCompanionRowId: 294,
        SourceName: "Sadu",
        TargetKind: PrototypeTargetKind.EventNpc,
        TargetRowId: 1044731,
        TargetModelCharaRowId: 0,
        TargetName: "Sadu",
        IsHuman: true,
        TargetModelScale: null,
        AppearanceCategory: PrototypeAppearanceCategory.Human);

    private static readonly PrototypeMapping MinfiliaOptionB = new(
        SourceCompanionRowId: 98,
        SourceName: "Minfilia",
        TargetKind: PrototypeTargetKind.BattleNpc,
        TargetRowId: 13753,
        TargetModelCharaRowId: 0,
        TargetName: "Minfilia's Soul",
        IsHuman: true,
        TargetModelScale: null,
        AppearanceCategory: PrototypeAppearanceCategory.Human);

    private static readonly PrototypeMapping AthenaOptionB = new(
        SourceCompanionRowId: 487,
        SourceName: "Athena",
        TargetKind: PrototypeTargetKind.EventNpc,
        TargetRowId: 1045553,
        TargetModelCharaRowId: 0,
        TargetName: "Athena",
        IsHuman: true,
        TargetModelScale: null,
        AppearanceCategory: PrototypeAppearanceCategory.Human);

    private static readonly PrototypeMapping WindUpPixieOptionB = new(
        SourceCompanionRowId: 354,
        SourceName: "Wind-up Pixie",
        TargetKind: PrototypeTargetKind.EventNpc,
        TargetRowId: 1031890,
        TargetModelCharaRowId: 2520,
        TargetName: "アン＝ラド",
        IsHuman: false,
        TargetModelScale: 0.62f,
        AppearanceCategory: PrototypeAppearanceCategory.DemiHuman);

    private static readonly PrototypeMapping WindUpPixieOptionC = new(
        SourceCompanionRowId: 354,
        SourceName: "Wind-up Pixie",
        TargetKind: PrototypeTargetKind.EventNpc,
        TargetRowId: 1031806,
        TargetModelCharaRowId: 2520,
        TargetName: "ティル＝ベーク",
        IsHuman: false,
        TargetModelScale: 0.62f,
        AppearanceCategory: PrototypeAppearanceCategory.DemiHuman);

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
            TargetModelScale: 1.0f,
            AppearanceCategory: PrototypeAppearanceCategory.Human),
        new(
            SourceCompanionRowId: 232,
            SourceName: "Scathach",
            TargetKind: PrototypeTargetKind.BattleNpc,
            TargetRowId: 6479,
            TargetModelCharaRowId: 1689,
            TargetName: "Scathach",
            IsHuman: false,
            TargetModelScale: 0.5f,
            AppearanceCategory: PrototypeAppearanceCategory.Monster),
        new(
            SourceCompanionRowId: 218,
            SourceName: "Alisaie",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1017687,
            TargetModelCharaRowId: 0,
            TargetName: "Alisaie",
            IsHuman: true,
            TargetModelScale: 0.97f,
            AppearanceCategory: PrototypeAppearanceCategory.Human),
        new(
            SourceCompanionRowId: 398,
            SourceName: "Gaia",
            TargetKind: PrototypeTargetKind.BattleNpc,
            TargetRowId: 17830,
            TargetModelCharaRowId: 4436,
            TargetName: "Gaia",
            IsHuman: false,
            TargetModelScale: null,
            AppearanceCategory: PrototypeAppearanceCategory.DemiHuman),
        new(
            SourceCompanionRowId: 534,
            SourceName: "Pelupelu",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1046564,
            TargetModelCharaRowId: 0,
            TargetName: "Quiet Pelupelu",
            IsHuman: true,
            TargetModelScale: null,
            AppearanceCategory: PrototypeAppearanceCategory.Human),
        new(
            SourceCompanionRowId: 325,
            SourceName: "Fran",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1025589,
            TargetModelCharaRowId: 2382,
            TargetName: "Fran",
            IsHuman: false,
            TargetModelScale: null,
            AppearanceCategory: PrototypeAppearanceCategory.DemiHuman),
        new(
            SourceCompanionRowId: 298,
            SourceName: "Zhloe",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1044638,
            TargetModelCharaRowId: 0,
            TargetName: "Zhloe Aliapoh",
            IsHuman: true,
            TargetModelScale: null,
            AppearanceCategory: PrototypeAppearanceCategory.Human),
        new(
            SourceCompanionRowId: 394,
            SourceName: "Automaton 2B",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1033925,
            TargetModelCharaRowId: 2810,
            TargetName: "2B",
            IsHuman: true,
            TargetModelScale: null,
            AppearanceCategory: PrototypeAppearanceCategory.Human),
        new(
            SourceCompanionRowId: 395,
            SourceName: "Automaton 2P",
            TargetKind: PrototypeTargetKind.BattleNpc,
            TargetRowId: 11366,
            TargetModelCharaRowId: 2810,
            TargetName: "2P",
            IsHuman: true,
            TargetModelScale: null,
            AppearanceCategory: PrototypeAppearanceCategory.Human),
        new(
            SourceCompanionRowId: 98,
            SourceName: "Minfilia",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1006573,
            TargetModelCharaRowId: 0,
            TargetName: "Minfilia",
            IsHuman: true,
            TargetModelScale: null,
            AppearanceCategory: PrototypeAppearanceCategory.Human),
        new(
            SourceCompanionRowId: 260,
            SourceName: "Khloe",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1012445,
            TargetModelCharaRowId: 0,
            TargetName: "Khloe Aliapoh",
            IsHuman: true,
            TargetModelScale: 0.7f,
            AppearanceCategory: PrototypeAppearanceCategory.Human),
        new(
            SourceCompanionRowId: 332,
            SourceName: "Ryne",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1033894,
            TargetModelCharaRowId: 0,
            TargetName: "Ryne",
            IsHuman: true,
            TargetModelScale: 0.86f,
            AppearanceCategory: PrototypeAppearanceCategory.Human),
        new(
            SourceCompanionRowId: 451,
            SourceName: "Azeyma",
            TargetKind: PrototypeTargetKind.BattleNpc,
            TargetRowId: 14545,
            TargetModelCharaRowId: 3645,
            TargetName: "Azeyma",
            IsHuman: false,
            TargetModelScale: null,
            AppearanceCategory: PrototypeAppearanceCategory.Monster),
        new(
            SourceCompanionRowId: 354,
            SourceName: "Wind-up Pixie",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1031809,
            TargetModelCharaRowId: 2520,
            TargetName: "ウィンニミイ",
            IsHuman: false,
            TargetModelScale: 0.62f,
            AppearanceCategory: PrototypeAppearanceCategory.DemiHuman),
        new(
            SourceCompanionRowId: 293,
            SourceName: "Cirina",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1018978,
            TargetModelCharaRowId: 0,
            TargetName: "Cirina",
            IsHuman: true,
            TargetModelScale: null,
            AppearanceCategory: PrototypeAppearanceCategory.Human),
        new(
            SourceCompanionRowId: 294,
            SourceName: "Sadu",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1018980,
            TargetModelCharaRowId: 0,
            TargetName: "Sadu",
            IsHuman: true,
            TargetModelScale: null,
            AppearanceCategory: PrototypeAppearanceCategory.Human),
        new(
            SourceCompanionRowId: 487,
            SourceName: "Athena",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1043513,
            TargetModelCharaRowId: 0,
            TargetName: "Athena",
            IsHuman: true,
            TargetModelScale: null,
            AppearanceCategory: PrototypeAppearanceCategory.Human),
        new(
            SourceCompanionRowId: 441,
            SourceName: "Heloise",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1036935,
            TargetModelCharaRowId: 3439,
            TargetName: "Venat",
            IsHuman: true,
            TargetModelScale: 0.97f,
            AppearanceCategory: PrototypeAppearanceCategory.Human),
        new(
            SourceCompanionRowId: 73,
            SourceName: "Kan-E-Senna",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1026816,
            TargetModelCharaRowId: 0,
            TargetName: "Kan-E-Senna",
            IsHuman: true,
            TargetModelScale: null,
            AppearanceCategory: PrototypeAppearanceCategory.Human),
        new(
            SourceCompanionRowId: 145,
            SourceName: "Ysayle",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1014847,
            TargetModelCharaRowId: 0,
            TargetName: "Ysayle",
            IsHuman: true,
            TargetModelScale: null,
            AppearanceCategory: PrototypeAppearanceCategory.Human),
        new(
            SourceCompanionRowId: 286,
            SourceName: "Mithra",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1051960,
            TargetModelCharaRowId: 0,
            TargetName: "Mithran Adventurer",
            IsHuman: true,
            TargetModelScale: null,
            AppearanceCategory: PrototypeAppearanceCategory.Human),
        new(
            SourceCompanionRowId: 248,
            SourceName: "Lyse",
            TargetKind: PrototypeTargetKind.EventNpc,
            TargetRowId: 1038813,
            TargetModelCharaRowId: 0,
            TargetName: "Lyse",
            IsHuman: true,
            TargetModelScale: null,
            AppearanceCategory: PrototypeAppearanceCategory.Human),
    ];

    private static readonly IReadOnlyDictionary<uint, IReadOnlyList<PrototypeMapping>> TargetCandidatesBySource =
        new Dictionary<uint, IReadOnlyList<PrototypeMapping>>
        {
            [98] =
            [
                Mappings.Single(mapping => mapping.SourceCompanionRowId == 98),
                MinfiliaOptionB,
            ],
            [260] =
            [
                Mappings.Single(mapping => mapping.SourceCompanionRowId == 260),
                KhloeOptionB,
            ],
            [293] =
            [
                Mappings.Single(mapping => mapping.SourceCompanionRowId == 293),
                CirinaOptionB,
            ],
            [294] =
            [
                Mappings.Single(mapping => mapping.SourceCompanionRowId == 294),
                SaduOptionB,
            ],
            [298] =
            [
                ZhloeOptionB,
                Mappings.Single(mapping => mapping.SourceCompanionRowId == 298),
            ],
            [332] =
            [
                Mappings.Single(mapping => mapping.SourceCompanionRowId == 332),
                RyneOptionB,
            ],
            [354] =
            [
                Mappings.Single(mapping => mapping.SourceCompanionRowId == 354),
                WindUpPixieOptionB,
                WindUpPixieOptionC,
            ],
            [487] =
            [
                Mappings.Single(mapping => mapping.SourceCompanionRowId == 487),
                AthenaOptionB,
            ],
        };

    private static readonly IReadOnlyDictionary<uint, uint> DefaultTargetRowIdsBySource =
        new Dictionary<uint, uint>
        {
            [260] = KhloeOptionB.TargetRowId,
            [354] = 1031809,
        };

    public static IReadOnlyList<PrototypeMapping> GetTargetCandidates(uint sourceCompanionRowId)
    {
        if (!TryGetMapping(sourceCompanionRowId, out var sourceMapping))
            return [];

        return TargetCandidatesBySource.TryGetValue(sourceCompanionRowId, out var candidates)
            ? candidates
            : [sourceMapping];
    }

    public static bool HasMultipleTargetCandidates(uint sourceCompanionRowId)
        => GetTargetCandidates(sourceCompanionRowId).Count > 1;

    public static bool TryGetTargetCandidate(
        uint sourceCompanionRowId,
        uint targetRowId,
        out PrototypeMapping mapping)
    {
        foreach (var candidate in GetTargetCandidates(sourceCompanionRowId))
        {
            if (candidate.TargetRowId == targetRowId)
            {
                mapping = candidate;
                return true;
            }
        }

        mapping = null!;
        return false;
    }

    public static PrototypeMapping GetDefaultTargetMapping(uint sourceCompanionRowId)
    {
        if (!TryGetMapping(sourceCompanionRowId, out var sourceMapping))
            throw new InvalidOperationException($"Unknown source Companion row: {sourceCompanionRowId}.");

        return DefaultTargetRowIdsBySource.TryGetValue(sourceCompanionRowId, out var targetRowId)
            && TryGetTargetCandidate(sourceCompanionRowId, targetRowId, out var defaultMapping)
                ? defaultMapping
                : sourceMapping;
    }

    public static bool TryGetSelectedMapping(
        uint sourceCompanionRowId,
        IReadOnlyDictionary<uint, uint>? selectedTargetRowIds,
        out PrototypeMapping mapping)
    {
        if (!TryGetMapping(sourceCompanionRowId, out _))
        {
            mapping = null!;
            return false;
        }

        if (selectedTargetRowIds is not null
            && selectedTargetRowIds.TryGetValue(sourceCompanionRowId, out var targetRowId)
            && TryGetTargetCandidate(sourceCompanionRowId, targetRowId, out mapping))
        {
            return true;
        }

        mapping = GetDefaultTargetMapping(sourceCompanionRowId);
        return true;
    }

    public static PrototypeMapping GetSelectedMapping(
        uint sourceCompanionRowId,
        IReadOnlyDictionary<uint, uint>? selectedTargetRowIds)
    {
        if (TryGetSelectedMapping(sourceCompanionRowId, selectedTargetRowIds, out var mapping))
            return mapping;

        throw new InvalidOperationException($"Unknown source Companion row: {sourceCompanionRowId}.");
    }

    public static PrototypeMapping GetAppearanceMapping(PrototypeMapping selectedMapping)
        => selectedMapping;

    public static PrototypeTargetKey GetTargetKey(PrototypeMapping mapping)
        => new(mapping.SourceCompanionRowId, mapping.TargetKind, mapping.TargetRowId);

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

internal enum PrototypeAppearanceCategory
{
    Human,
    DemiHuman,
    Monster,
}

internal sealed record PrototypeMapping(
    uint SourceCompanionRowId,
    string SourceName,
    PrototypeTargetKind TargetKind,
    uint TargetRowId,
    uint TargetModelCharaRowId,
    string TargetName,
    bool IsHuman,
    float? TargetModelScale,
    PrototypeAppearanceCategory AppearanceCategory);

internal readonly record struct PrototypeTargetKey(
    uint SourceCompanionRowId,
    PrototypeTargetKind TargetKind,
    uint TargetRowId);
