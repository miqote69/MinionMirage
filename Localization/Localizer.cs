using Dalamud.Game;
using Dalamud.Plugin.Services;

namespace MinionToNPC.Localization;

public sealed class Localizer(Configuration configuration, IClientState clientState)
{
    public UiLanguage EffectiveLanguage => configuration.UiLanguage == UiLanguage.Automatic
        ? FromClientLanguage(clientState.ClientLanguage)
        : configuration.UiLanguage;

    public string Get(UiTextKey key)
        => Strings.TryGetValue(EffectiveLanguage, out var language)
            && language.TryGetValue(key, out var localized)
                ? localized
                : English[key];

    public string GetLanguageName(UiLanguage language) => language switch
    {
        UiLanguage.Automatic => Get(UiTextKey.Automatic),
        UiLanguage.English => Get(UiTextKey.English),
        UiLanguage.Japanese => Get(UiTextKey.Japanese),
        UiLanguage.German => Get(UiTextKey.German),
        UiLanguage.French => Get(UiTextKey.French),
        _ => language.ToString(),
    };

    private static UiLanguage FromClientLanguage(ClientLanguage language) => language switch
    {
        ClientLanguage.Japanese => UiLanguage.Japanese,
        ClientLanguage.German => UiLanguage.German,
        ClientLanguage.French => UiLanguage.French,
        _ => UiLanguage.English,
    };

    private static readonly IReadOnlyDictionary<UiTextKey, string> English =
        new Dictionary<UiTextKey, string>
        {
            [UiTextKey.Automatic] = "Automatic",
            [UiTextKey.English] = "English",
            [UiTextKey.Japanese] = "Japanese",
            [UiTextKey.German] = "German",
            [UiTextKey.French] = "French",
            [UiTextKey.UiLanguage] = "UI language",
            [UiTextKey.EnableAll] = "Enable all",
            [UiTextKey.DisableAll] = "Disable all",
            [UiTextKey.Enabled] = "Enabled",
            [UiTextKey.Disabled] = "Disabled",
        };

    private static readonly IReadOnlyDictionary<UiLanguage, IReadOnlyDictionary<UiTextKey, string>> Strings =
        new Dictionary<UiLanguage, IReadOnlyDictionary<UiTextKey, string>>
        {
            [UiLanguage.English] = English,
            [UiLanguage.Japanese] = Translate(new Dictionary<UiTextKey, string>
            {
                [UiTextKey.Automatic] = "自動",
                [UiTextKey.English] = "英語",
                [UiTextKey.Japanese] = "日本語",
                [UiTextKey.German] = "ドイツ語",
                [UiTextKey.French] = "フランス語",
                [UiTextKey.UiLanguage] = "UI言語",
                [UiTextKey.EnableAll] = "一括ON",
                [UiTextKey.DisableAll] = "一括OFF",
                [UiTextKey.Enabled] = "有効",
                [UiTextKey.Disabled] = "無効",
            }),
            [UiLanguage.German] = Translate(new Dictionary<UiTextKey, string>
            {
                [UiTextKey.Automatic] = "Automatisch",
                [UiTextKey.English] = "Englisch",
                [UiTextKey.Japanese] = "Japanisch",
                [UiTextKey.German] = "Deutsch",
                [UiTextKey.French] = "Französisch",
                [UiTextKey.UiLanguage] = "UI-Sprache",
                [UiTextKey.EnableAll] = "Alle aktivieren",
                [UiTextKey.DisableAll] = "Alle deaktivieren",
                [UiTextKey.Enabled] = "Aktiviert",
                [UiTextKey.Disabled] = "Deaktiviert",
            }),
            [UiLanguage.French] = Translate(new Dictionary<UiTextKey, string>
            {
                [UiTextKey.Automatic] = "Automatique",
                [UiTextKey.English] = "Anglais",
                [UiTextKey.Japanese] = "Japonais",
                [UiTextKey.German] = "Allemand",
                [UiTextKey.French] = "Français",
                [UiTextKey.UiLanguage] = "Langue de l'interface",
                [UiTextKey.EnableAll] = "Tout activer",
                [UiTextKey.DisableAll] = "Tout désactiver",
                [UiTextKey.Enabled] = "Activé",
                [UiTextKey.Disabled] = "Désactivé",
            }),
        };

    private static IReadOnlyDictionary<UiTextKey, string> Translate(
        IReadOnlyDictionary<UiTextKey, string> overrides)
        => English.ToDictionary(
            pair => pair.Key,
            pair => overrides.TryGetValue(pair.Key, out var value) ? value : pair.Value);
}
