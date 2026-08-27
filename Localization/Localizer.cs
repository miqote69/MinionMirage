using Dalamud.Game;
using Dalamud.Plugin.Services;
using System.Globalization;

namespace MinionMirage.Localization;

public sealed class Localizer(Configuration configuration, IClientState clientState)
{
    public UiLanguage EffectiveLanguage => configuration.UiLanguage == UiLanguage.Automatic
        ? FromClientLanguage(clientState.ClientLanguage)
        : configuration.UiLanguage;

    public string Get(UiTextKey key, params object[] arguments)
    {
        var value = Strings.TryGetValue(EffectiveLanguage, out var language)
            && language.TryGetValue(key, out var localized)
                ? localized
                : English[key];

        return arguments.Length == 0
            ? value
            : string.Format(CultureInfo.CurrentCulture, value, arguments);
    }

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
            [UiTextKey.Settings] = "Settings",
            [UiTextKey.UiLanguage] = "UI language",
            [UiTextKey.Experimental] = "Experimental",
            [UiTextKey.EnableMinionSummonExperiment] = "Enable minion summon (experimental)",
            [UiTextKey.EnableMinionSummonExperimentHelp] = "Enables every game minion icon and normal summon in prohibited areas. This may crash the game.",
            [UiTextKey.Search] = "Search minions or NPCs",
            [UiTextKey.All] = "All",
            [UiTextKey.Human] = "Human",
            [UiTextKey.AdultHuman] = "Adult human",
            [UiTextKey.YoungHuman] = "Young human",
            [UiTextKey.DemiHuman] = "Demi-human",
            [UiTextKey.Monster] = "Monster",
            [UiTextKey.CardView] = "Cards",
            [UiTextKey.ListView] = "List",
            [UiTextKey.Target] = "Target",
            [UiTextKey.ActiveMappings] = "{0} / {1} active",
            [UiTextKey.VisibleMappings] = "{0} / {1} shown",
            [UiTextKey.NoResults] = "No mappings match the current search and filter.",
            [UiTextKey.EnableAll] = "Enable all",
            [UiTextKey.DisableAll] = "Disable all",
            [UiTextKey.Enabled] = "Enabled",
            [UiTextKey.Disabled] = "Disabled",
            [UiTextKey.NotOwned] = "Not owned",
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
                [UiTextKey.Settings] = "設定",
                [UiTextKey.UiLanguage] = "UI言語",
                [UiTextKey.Experimental] = "実験的機能",
                [UiTextKey.EnableMinionSummonExperiment] = "ミニオン召喚を有効化（実験）",
                [UiTextKey.EnableMinionSummonExperimentHelp] = "召喚禁止エリアで全ミニオンのゲーム内アイコンと通常召喚を有効化します。ゲームがクラッシュする可能性があります。",
                [UiTextKey.Search] = "ミニオン名・NPC名で検索",
                [UiTextKey.All] = "すべて",
                [UiTextKey.Human] = "ヒューマン",
                [UiTextKey.AdultHuman] = "Adultヒューマン",
                [UiTextKey.YoungHuman] = "Youngヒューマン",
                [UiTextKey.DemiHuman] = "デミヒューマン",
                [UiTextKey.Monster] = "モンスター",
                [UiTextKey.CardView] = "カード",
                [UiTextKey.ListView] = "リスト",
                [UiTextKey.Target] = "変換先",
                [UiTextKey.ActiveMappings] = "{0} / {1} 有効",
                [UiTextKey.VisibleMappings] = "{0} / {1} 件",
                [UiTextKey.NoResults] = "検索・フィルター条件に一致する項目はありません。",
                [UiTextKey.EnableAll] = "一括ON",
                [UiTextKey.DisableAll] = "一括OFF",
                [UiTextKey.Enabled] = "有効",
                [UiTextKey.Disabled] = "無効",
                [UiTextKey.NotOwned] = "未所持",
            }),
            [UiLanguage.German] = Translate(new Dictionary<UiTextKey, string>
            {
                [UiTextKey.Automatic] = "Automatisch",
                [UiTextKey.English] = "Englisch",
                [UiTextKey.Japanese] = "Japanisch",
                [UiTextKey.German] = "Deutsch",
                [UiTextKey.French] = "Französisch",
                [UiTextKey.Settings] = "Einstellungen",
                [UiTextKey.UiLanguage] = "UI-Sprache",
                [UiTextKey.Experimental] = "Experimentell",
                [UiTextKey.EnableMinionSummonExperiment] = "Begleiterbeschwörung aktivieren (experimentell)",
                [UiTextKey.EnableMinionSummonExperimentHelp] = "Aktiviert alle Begleitersymbole und die normale Beschwörung in gesperrten Bereichen. Das Spiel kann abstürzen.",
                [UiTextKey.Search] = "Begleiter oder NPC suchen",
                [UiTextKey.All] = "Alle",
                [UiTextKey.Human] = "Humanoid",
                [UiTextKey.AdultHuman] = "Erwachsener Humanoid",
                [UiTextKey.YoungHuman] = "Junger Humanoid",
                [UiTextKey.DemiHuman] = "Demi-Humanoid",
                [UiTextKey.Monster] = "Monster",
                [UiTextKey.CardView] = "Karten",
                [UiTextKey.ListView] = "Liste",
                [UiTextKey.Target] = "Ziel",
                [UiTextKey.ActiveMappings] = "{0} / {1} aktiv",
                [UiTextKey.VisibleMappings] = "{0} / {1} angezeigt",
                [UiTextKey.NoResults] = "Keine Zuordnungen entsprechen Suche und Filter.",
                [UiTextKey.EnableAll] = "Alle aktivieren",
                [UiTextKey.DisableAll] = "Alle deaktivieren",
                [UiTextKey.Enabled] = "Aktiviert",
                [UiTextKey.Disabled] = "Deaktiviert",
                [UiTextKey.NotOwned] = "Nicht im Besitz",
            }),
            [UiLanguage.French] = Translate(new Dictionary<UiTextKey, string>
            {
                [UiTextKey.Automatic] = "Automatique",
                [UiTextKey.English] = "Anglais",
                [UiTextKey.Japanese] = "Japonais",
                [UiTextKey.German] = "Allemand",
                [UiTextKey.French] = "Français",
                [UiTextKey.Settings] = "Paramètres",
                [UiTextKey.UiLanguage] = "Langue de l'interface",
                [UiTextKey.Experimental] = "Expérimental",
                [UiTextKey.EnableMinionSummonExperiment] = "Activer l'invocation de mascotte (expérimental)",
                [UiTextKey.EnableMinionSummonExperimentHelp] = "Active toutes les icônes de mascotte et l'invocation normale dans les zones interdites. Le jeu peut planter.",
                [UiTextKey.Search] = "Rechercher une mascotte ou un PNJ",
                [UiTextKey.All] = "Tout",
                [UiTextKey.Human] = "Humanoïde",
                [UiTextKey.AdultHuman] = "Humanoïde adulte",
                [UiTextKey.YoungHuman] = "Jeune humanoïde",
                [UiTextKey.DemiHuman] = "Demi-humain",
                [UiTextKey.Monster] = "Monstre",
                [UiTextKey.CardView] = "Cartes",
                [UiTextKey.ListView] = "Liste",
                [UiTextKey.Target] = "Cible",
                [UiTextKey.ActiveMappings] = "{0} / {1} actifs",
                [UiTextKey.VisibleMappings] = "{0} / {1} affichés",
                [UiTextKey.NoResults] = "Aucune association ne correspond à la recherche et au filtre.",
                [UiTextKey.EnableAll] = "Tout activer",
                [UiTextKey.DisableAll] = "Tout désactiver",
                [UiTextKey.Enabled] = "Activé",
                [UiTextKey.Disabled] = "Désactivé",
                [UiTextKey.NotOwned] = "Non obtenue",
            }),
        };

    private static IReadOnlyDictionary<UiTextKey, string> Translate(
        IReadOnlyDictionary<UiTextKey, string> overrides)
        => English.ToDictionary(
            pair => pair.Key,
            pair => overrides.TryGetValue(pair.Key, out var value) ? value : pair.Value);
}
