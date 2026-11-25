using Dalamud.Configuration;

namespace MinionMirage;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 5;

    public UiLanguage UiLanguage { get; set; } = UiLanguage.Automatic;

    public HashSet<uint> DisabledCompanionRowIds { get; set; } = [];

    public Dictionary<uint, uint> SelectedTargetRowIds { get; set; } = [];

    public bool ExperimentalEnableNormalCompanionSummon { get; set; }

    public bool UseMinionFootsteps { get; set; } = true;

    public uint SelectedLeaderCompanionRowId { get; set; } = 73;

    public bool IsMappingEnabled(uint companionRowId)
        => !DisabledCompanionRowIds.Contains(companionRowId is 72 or 74 ? 73 : companionRowId);

    internal uint ResolveCompanionAction(uint actionId)
        => actionId == 71 && IsMappingEnabled(73) ? SelectedLeaderCompanionRowId : actionId;

    internal bool UseLocalLeaderTransition(uint originalStatus)
        => originalStatus == 0 || (ExperimentalEnableNormalCompanionSummon && originalStatus == 1325);

    internal static uint GetLeaderTransition(uint selectedRowId, uint activeRowId, bool hasCompanionObject)
        => hasCompanionObject && activeRowId == selectedRowId ? 0u : selectedRowId;
}

public enum UiLanguage
{
    Automatic,
    English,
    Japanese,
    German,
    French,
}
