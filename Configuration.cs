using Dalamud.Configuration;

namespace MinionToNPC;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    public UiLanguage UiLanguage { get; set; } = UiLanguage.Automatic;

    public HashSet<uint> DisabledCompanionRowIds { get; set; } = [];

    public Dictionary<uint, uint> SelectedTargetRowIds { get; set; } = [];

    public bool IsMappingEnabled(uint companionRowId)
        => !DisabledCompanionRowIds.Contains(companionRowId);
}

public enum UiLanguage
{
    Automatic,
    English,
    Japanese,
    German,
    French,
}
