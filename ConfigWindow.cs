using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using MinionMirage.Localization;
using System.Numerics;

namespace MinionMirage;

public sealed class ConfigWindow : Window, IDisposable
{
    private static readonly Vector2 CardIconSize = new(48.0f, 48.0f);
    private static readonly Vector2 ListIconSize = new(36.0f, 36.0f);
    private static readonly Vector4 CardBackground = new(0.055f, 0.065f, 0.105f, 0.92f);
    private static readonly Vector4 UnownedCardBackground = new(0.065f, 0.068f, 0.078f, 0.88f);
    private static readonly Vector4 CardBorder = new(0.20f, 0.23f, 0.34f, 0.95f);
    private static readonly Vector4 MutedText = new(0.58f, 0.62f, 0.72f, 1.0f);
    private static readonly Vector4 UnownedAccent = new(0.43f, 0.45f, 0.50f, 1.0f);
    private static readonly Vector4 EnabledText = new(0.31f, 0.91f, 0.67f, 1.0f);

    private readonly Plugin plugin;
    private string searchText = string.Empty;
    private MappingFilter selectedFilter = MappingFilter.All;
    private MappingView selectedView = MappingView.Cards;

    public ConfigWindow(Plugin plugin)
        : base($"{Plugin.DisplayName} v{Plugin.DisplayVersion}###MinionMirageConfig")
    {
        this.plugin = plugin;
        Flags |= ImGuiWindowFlags.MenuBar;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(580, 420),
            MaximumSize = new Vector2(1000, 10000),
        };
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        DrawMenuBar();

        var mappings = PrototypeContract.Mappings
            .OrderBy(plugin.GetCompanionOrder)
            .ThenBy(mapping => mapping.SourceCompanionRowId)
            .Where(MatchesFilter)
            .Where(MatchesSearch)
            .ToArray();
        var activeCount = PrototypeContract.Mappings.Count(mapping =>
            plugin.Configuration.IsMappingEnabled(mapping.SourceCompanionRowId));

        DrawSearch(activeCount);
        DrawToolbar(mappings.Length);
        DrawCategoryFilters();
        DrawExperimentalControls();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (mappings.Length == 0)
        {
            ImGui.TextDisabled(plugin.Localizer.Get(UiTextKey.NoResults));
            return;
        }

        if (selectedView == MappingView.Cards)
            DrawCards(mappings);
        else
            DrawList(mappings);
    }

    private void DrawMenuBar()
    {
        if (!ImGui.BeginMenuBar())
            return;

        if (ImGui.BeginMenu($"{plugin.Localizer.Get(UiTextKey.Settings)}###settings-menu"))
        {
            if (ImGui.BeginMenu($"{plugin.Localizer.Get(UiTextKey.UiLanguage)}###language-menu"))
            {
                var current = plugin.Configuration.UiLanguage;
                foreach (var language in Enum.GetValues<UiLanguage>())
                {
                    if (ImGui.MenuItem(
                            $"{plugin.Localizer.GetLanguageName(language)}###ui-language-{language}",
                            string.Empty,
                            current == language))
                    {
                        plugin.SetUiLanguage(language);
                    }
                }

                ImGui.EndMenu();
            }

            ImGui.EndMenu();
        }

        ImGui.EndMenuBar();
    }

    private void DrawExperimentalControls()
    {
        ImGui.Spacing();
        ImGui.TextColored(
            new Vector4(0.94f, 0.48f, 0.24f, 1.0f),
            plugin.Localizer.Get(UiTextKey.Experimental));
        ImGui.Separator();

        const ImGuiTableFlags flags = ImGuiTableFlags.SizingStretchProp;
        if (ImGui.BeginTable("##experimental-normal-summon", 2, flags))
        {
            ImGui.TableSetupColumn("##normal-summon-label", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("##normal-summon-toggle", ImGuiTableColumnFlags.WidthFixed, 52.0f);
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(plugin.Localizer.Get(UiTextKey.EnableMinionSummonExperiment));

            ImGui.TableSetColumnIndex(1);
            var enabled = plugin.Configuration.ExperimentalEnableNormalCompanionSummon;
            if (DrawToggle("##normal-summon-enabled", ref enabled, new Vector4(0.94f, 0.48f, 0.24f, 1.0f)))
                plugin.SetExperimentalEnableNormalCompanionSummon(enabled);

            ImGui.EndTable();
        }

        ImGui.SetWindowFontScale(0.86f);
        ImGui.TextWrapped(plugin.Localizer.Get(UiTextKey.EnableMinionSummonExperimentHelp));
        ImGui.SetWindowFontScale(1.0f);
    }

    private void DrawSearch(int activeCount)
    {
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var countText = plugin.Localizer.Get(
            UiTextKey.ActiveMappings,
            activeCount,
            PrototypeContract.Mappings.Count);
        var countWidth = ImGui.CalcTextSize(countText).X;
        var searchWidth = Math.Max(220.0f, availableWidth - countWidth - ImGui.GetStyle().ItemSpacing.X);

        ImGui.SetNextItemWidth(searchWidth);
        ImGui.InputTextWithHint(
            "##mapping-search",
            plugin.Localizer.Get(UiTextKey.Search),
            ref searchText,
            128);
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(EnabledText, countText);
    }

    private void DrawToolbar(int visibleCount)
    {
        ImGui.Spacing();
        DrawViewButton(MappingView.Cards, UiTextKey.CardView);
        ImGui.SameLine();
        DrawViewButton(MappingView.List, UiTextKey.ListView);
        ImGui.SameLine();
        ImGui.TextDisabled(plugin.Localizer.Get(
            UiTextKey.VisibleMappings,
            visibleCount,
            PrototypeContract.Mappings.Count));

        ImGui.Spacing();
        if (ImGui.Button($"{plugin.Localizer.Get(UiTextKey.DisableAll)}###disable-all"))
            plugin.SetAllMappingsEnabled(false);

        ImGui.SameLine();
        PushAccentButtonColors(new Vector4(0.50f, 0.63f, 1.0f, 1.0f));
        if (ImGui.Button($"{plugin.Localizer.Get(UiTextKey.EnableAll)}###enable-all"))
            plugin.SetAllMappingsEnabled(true);
        ImGui.PopStyleColor(3);
    }

    private void DrawCategoryFilters()
    {
        ImGui.Spacing();
        var filters = new[]
        {
            (MappingFilter.All, UiTextKey.All, PrototypeContract.Mappings.Count),
            (MappingFilter.AdultHuman, UiTextKey.AdultHuman, CountGroup(MappingGroup.AdultHuman)),
            (MappingFilter.YoungHuman, UiTextKey.YoungHuman, CountGroup(MappingGroup.YoungHuman)),
            (MappingFilter.DemiHuman, UiTextKey.DemiHuman, CountGroup(MappingGroup.DemiHuman)),
            (MappingFilter.Monster, UiTextKey.Monster, CountGroup(MappingGroup.Monster)),
        };

        for (var index = 0; index < filters.Length; index++)
        {
            var (filter, labelKey, count) = filters[index];
            DrawFilterButton(filter, labelKey, count);
            if (index + 1 < filters.Length)
            {
                var next = filters[index + 1];
                var nextLabel = $"{plugin.Localizer.Get(next.Item2)}  {next.Item3}";
                if (CanFitOnCurrentLine(nextLabel))
                    ImGui.SameLine();
            }
        }
    }

    private void DrawFilterButton(MappingFilter filter, UiTextKey labelKey, int count)
    {
        var selected = selectedFilter == filter;
        if (selected)
            PushAccentButtonColors(new Vector4(0.46f, 0.39f, 0.91f, 1.0f));

        if (ImGui.Button($"{plugin.Localizer.Get(labelKey)}  {count}###filter-{filter}"))
            selectedFilter = filter;

        if (selected)
            ImGui.PopStyleColor(3);
    }

    private void DrawViewButton(MappingView view, UiTextKey labelKey)
    {
        var selected = selectedView == view;
        if (selected)
            PushAccentButtonColors(new Vector4(0.38f, 0.32f, 0.73f, 1.0f));

        var icon = view == MappingView.Cards
            ? FontAwesomeIcon.ThLarge.ToIconString()
            : FontAwesomeIcon.List.ToIconString();
        using (plugin.PushIconFont())
        {
            if (ImGui.Button($"{icon}###view-{view}"))
                selectedView = view;
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(plugin.Localizer.Get(labelKey));

        if (selected)
            ImGui.PopStyleColor(3);
    }

    private void DrawCards(IReadOnlyList<PrototypeMapping> mappings)
    {
        const float minimumCardWidth = 300.0f;
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var columnCount = Math.Max(1, (int)(availableWidth / minimumCardWidth));
        const ImGuiTableFlags flags = ImGuiTableFlags.SizingStretchSame
            | ImGuiTableFlags.PadOuterX;

        if (!ImGui.BeginTable("##mapping-cards", columnCount, flags))
            return;

        foreach (var mapping in mappings)
        {
            ImGui.TableNextColumn();
            DrawCard(mapping);
        }

        ImGui.EndTable();
    }

    private void DrawCard(PrototypeMapping mapping)
    {
        var selectedMapping = plugin.GetSelectedMapping(mapping);
        var group = GetMappingGroup(selectedMapping);
        var isUnlocked = plugin.IsCompanionUnlocked(mapping.SourceCompanionRowId);
        var accent = isUnlocked ? GetAccentColor(group) : UnownedAccent;
        ImGui.PushID(checked((int)mapping.SourceCompanionRowId));
        ImGui.PushStyleColor(ImGuiCol.ChildBg, isUnlocked ? CardBackground : UnownedCardBackground);
        ImGui.PushStyleColor(ImGuiCol.Border, Vector4.Lerp(CardBorder, accent, 0.32f));
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 10.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12.0f, 11.0f));

        if (ImGui.BeginChild("##mapping-card", new Vector2(0, 104.0f), true, ImGuiWindowFlags.NoScrollbar))
        {
            var contentStart = ImGui.GetCursorPos();
            var contentWidth = ImGui.GetContentRegionAvail().X;
            var toggleWidth = ImGui.GetFrameHeight() * 1.85f;
            DrawIcon(mapping, CardIconSize, isUnlocked);
            ImGui.SameLine();
            ImGui.BeginGroup();
            if (isUnlocked)
                ImGui.TextUnformatted(plugin.GetCompanionName(mapping));
            else
                ImGui.TextDisabled(plugin.GetCompanionName(mapping));

            ImGui.BeginDisabled(!isUnlocked);
            DrawTargetSelection(mapping, selectedMapping, 150.0f);
            ImGui.EndDisabled();
            DrawGroup(group, accent);
            if (!isUnlocked)
            {
                ImGui.SameLine(0, 6.0f);
                ImGui.TextDisabled($"· {plugin.Localizer.Get(UiTextKey.NotOwned)}");
            }
            ImGui.EndGroup();

            ImGui.SetCursorPos(new Vector2(
                contentStart.X + contentWidth - toggleWidth,
                contentStart.Y));
            ImGui.BeginDisabled(!isUnlocked);
            DrawMappingToggle(mapping, accent);
            ImGui.EndDisabled();
        }

        ImGui.EndChild();
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(2);
        ImGui.PopID();
    }

    private void DrawList(IReadOnlyList<PrototypeMapping> mappings)
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.RowBg
            | ImGuiTableFlags.BordersInnerH
            | ImGuiTableFlags.SizingStretchProp;
        if (!ImGui.BeginTable("##mapping-list", 4, flags))
            return;

        ImGui.TableSetupColumn("##icon", ImGuiTableColumnFlags.WidthFixed, 44.0f);
        ImGui.TableSetupColumn("##identity", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##category", ImGuiTableColumnFlags.WidthFixed, 120.0f);
        ImGui.TableSetupColumn("##enabled", ImGuiTableColumnFlags.WidthFixed, 64.0f);

        foreach (var mapping in mappings)
        {
            var selectedMapping = plugin.GetSelectedMapping(mapping);
            var group = GetMappingGroup(selectedMapping);
            var isUnlocked = plugin.IsCompanionUnlocked(mapping.SourceCompanionRowId);
            var accent = isUnlocked ? GetAccentColor(group) : UnownedAccent;
            ImGui.PushID(checked((int)mapping.SourceCompanionRowId));
            ImGui.TableNextRow(ImGuiTableRowFlags.None, 48.0f);

            ImGui.TableSetColumnIndex(0);
            DrawIcon(mapping, ListIconSize, isUnlocked);

            ImGui.TableSetColumnIndex(1);
            if (isUnlocked)
                ImGui.TextUnformatted(plugin.GetCompanionName(mapping));
            else
                ImGui.TextDisabled($"{plugin.GetCompanionName(mapping)}  [{plugin.Localizer.Get(UiTextKey.NotOwned)}]");

            ImGui.BeginDisabled(!isUnlocked);
            DrawTargetSelection(mapping, selectedMapping, 180.0f);
            ImGui.EndDisabled();

            ImGui.TableSetColumnIndex(2);
            ImGui.AlignTextToFramePadding();
            DrawGroup(group, accent);

            ImGui.TableSetColumnIndex(3);
            ImGui.BeginDisabled(!isUnlocked);
            DrawMappingToggle(mapping, accent);
            ImGui.EndDisabled();

            ImGui.PopID();
        }

        ImGui.EndTable();
    }

    private void DrawIcon(PrototypeMapping mapping, Vector2 size, bool isUnlocked)
    {
        if (!isUnlocked)
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.42f);

        if (plugin.TryGetCompanionIcon(mapping.SourceCompanionRowId, out var icon))
            ImGui.Image(icon!.Handle, size);
        else
            ImGui.Dummy(size);

        if (!isUnlocked)
            ImGui.PopStyleVar();
    }

    private void DrawTargetSelection(
        PrototypeMapping sourceMapping,
        PrototypeMapping selectedMapping,
        float comboWidth)
    {
        var candidates = plugin.GetTargetCandidates(sourceMapping);
        if (candidates.Count < 2)
        {
            ImGui.TextColored(
                MutedText,
                $"{plugin.Localizer.Get(UiTextKey.Target)}: {selectedMapping.TargetName}");
            return;
        }

        ImGui.TextDisabled($"{plugin.Localizer.Get(UiTextKey.Target)}:");
        ImGui.SameLine(0, 4.0f);
        ImGui.SetNextItemWidth(comboWidth);
        if (!ImGui.BeginCombo("##target-selection", selectedMapping.TargetName))
            return;

        foreach (var candidate in candidates)
        {
            var isSelected = PrototypeContract.GetTargetKey(candidate)
                == PrototypeContract.GetTargetKey(selectedMapping);
            if (ImGui.Selectable($"{candidate.TargetName} ({candidate.TargetRowId})", isSelected))
                plugin.SetSelectedTarget(sourceMapping.SourceCompanionRowId, candidate.TargetRowId);

            if (isSelected)
                ImGui.SetItemDefaultFocus();
        }

        ImGui.EndCombo();
    }

    private void DrawGroup(MappingGroup group, Vector4 accent)
    {
        ImGui.TextColored(accent, "●");
        ImGui.SameLine(0, 5.0f);
        ImGui.TextDisabled(plugin.Localizer.Get(GetGroupTextKey(group)));
    }

    private void DrawMappingToggle(PrototypeMapping mapping, Vector4 accent)
    {
        var enabled = plugin.Configuration.IsMappingEnabled(mapping.SourceCompanionRowId);
        if (DrawToggle("##mapping-enabled", ref enabled, accent))
            plugin.SetMappingEnabled(mapping.SourceCompanionRowId, enabled);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(plugin.Localizer.Get(
                enabled ? UiTextKey.Enabled : UiTextKey.Disabled));
        }
    }

    private static bool DrawToggle(string id, ref bool value, Vector4 accent)
    {
        var height = ImGui.GetFrameHeight();
        var width = height * 1.85f;
        var position = ImGui.GetCursorScreenPos();
        var changed = ImGui.InvisibleButton(id, new Vector2(width, height));
        if (changed)
            value = !value;

        var background = value
            ? new Vector4(accent.X, accent.Y, accent.Z, 0.88f)
            : new Vector4(0.25f, 0.27f, 0.34f, 1.0f);
        if (ImGui.IsItemHovered())
            background = Vector4.Lerp(background, Vector4.One, 0.12f);

        var radius = height * 0.5f;
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(
            position,
            position + new Vector2(width, height),
            ImGui.GetColorU32(background),
            radius);
        var knobCenter = new Vector2(
            value ? position.X + width - radius : position.X + radius,
            position.Y + radius);
        drawList.AddCircleFilled(
            knobCenter,
            radius - 3.0f,
            ImGui.GetColorU32(new Vector4(0.98f, 0.99f, 1.0f, 1.0f)));
        return changed;
    }

    private bool MatchesSearch(PrototypeMapping mapping)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return true;

        return plugin.GetCompanionName(mapping).Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || mapping.SourceName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || plugin.GetTargetCandidates(mapping).Any(candidate =>
                candidate.TargetName.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    private bool MatchesFilter(PrototypeMapping mapping)
        => selectedFilter switch
        {
            MappingFilter.All => true,
            MappingFilter.AdultHuman => GetMappingGroup(plugin.GetSelectedMapping(mapping)) == MappingGroup.AdultHuman,
            MappingFilter.YoungHuman => GetMappingGroup(plugin.GetSelectedMapping(mapping)) == MappingGroup.YoungHuman,
            MappingFilter.DemiHuman => GetMappingGroup(plugin.GetSelectedMapping(mapping)) == MappingGroup.DemiHuman,
            MappingFilter.Monster => GetMappingGroup(plugin.GetSelectedMapping(mapping)) == MappingGroup.Monster,
            _ => true,
        };

    private int CountGroup(MappingGroup group)
        => PrototypeContract.Mappings.Count(mapping =>
            GetMappingGroup(plugin.GetSelectedMapping(mapping)) == group);

    private MappingGroup GetMappingGroup(PrototypeMapping mapping)
        => mapping.AppearanceCategory switch
        {
            PrototypeAppearanceCategory.Human when plugin.IsYoungHuman(mapping) => MappingGroup.YoungHuman,
            PrototypeAppearanceCategory.Human => MappingGroup.AdultHuman,
            PrototypeAppearanceCategory.DemiHuman => MappingGroup.DemiHuman,
            PrototypeAppearanceCategory.Monster => MappingGroup.Monster,
            _ => MappingGroup.AdultHuman,
        };

    private static UiTextKey GetGroupTextKey(MappingGroup group)
        => group switch
        {
            MappingGroup.AdultHuman => UiTextKey.AdultHuman,
            MappingGroup.YoungHuman => UiTextKey.YoungHuman,
            MappingGroup.DemiHuman => UiTextKey.DemiHuman,
            MappingGroup.Monster => UiTextKey.Monster,
            _ => UiTextKey.All,
        };

    private static Vector4 GetAccentColor(MappingGroup group)
        => group switch
        {
            MappingGroup.AdultHuman => new Vector4(0.57f, 0.48f, 1.00f, 1.0f),
            MappingGroup.YoungHuman => new Vector4(1.00f, 0.58f, 0.36f, 1.0f),
            MappingGroup.DemiHuman => new Vector4(0.36f, 0.88f, 0.82f, 1.0f),
            MappingGroup.Monster => new Vector4(0.31f, 0.76f, 1.00f, 1.0f),
            _ => new Vector4(0.50f, 0.63f, 1.00f, 1.0f),
        };

    private static void PushAccentButtonColors(Vector4 accent)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(accent.X * 0.45f, accent.Y * 0.45f, accent.Z * 0.45f, 0.95f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(accent.X * 0.62f, accent.Y * 0.62f, accent.Z * 0.62f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(accent.X * 0.80f, accent.Y * 0.80f, accent.Z * 0.80f, 1.0f));
    }

    private static bool CanFitOnCurrentLine(string nextLabel)
    {
        var style = ImGui.GetStyle();
        var nextWidth = ImGui.CalcTextSize(nextLabel).X + (style.FramePadding.X * 2.0f);
        return ImGui.GetContentRegionAvail().X >= nextWidth + style.ItemSpacing.X;
    }

    private enum MappingFilter
    {
        All,
        AdultHuman,
        YoungHuman,
        DemiHuman,
        Monster,
    }

    private enum MappingGroup
    {
        AdultHuman,
        YoungHuman,
        DemiHuman,
        Monster,
    }

    private enum MappingView
    {
        Cards,
        List,
    }
}
