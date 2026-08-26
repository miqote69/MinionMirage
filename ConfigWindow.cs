using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using MinionToNPC.Localization;
using System.Numerics;

namespace MinionToNPC;

public sealed class ConfigWindow(Plugin plugin)
    : Window($"{Plugin.DisplayName} v{Plugin.DisplayVersion}###MinionToNPCConfig"), IDisposable
{
    private static readonly Vector2 IconSize = new(40.0f, 40.0f);

    public override void PreDraw()
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(430, 310),
            MaximumSize = new Vector2(720, 640),
        };
    }

    public override void Draw()
    {
        DrawUiLanguageSelector();
        ImGui.Separator();
        DrawBulkControls();
        ImGui.Separator();
        DrawMappings();
    }

    public void Dispose()
    {
    }

    private void DrawUiLanguageSelector()
    {
        var current = plugin.Configuration.UiLanguage;
        ImGui.SetNextItemWidth(220.0f);
        if (!ImGui.BeginCombo(
                $"{plugin.Localizer.Get(UiTextKey.UiLanguage)}###ui-language-combo",
                plugin.Localizer.GetLanguageName(current)))
        {
            return;
        }

        foreach (var language in Enum.GetValues<UiLanguage>())
        {
            if (ImGui.Selectable(
                    $"{plugin.Localizer.GetLanguageName(language)}###ui-language-{language}",
                    current == language))
            {
                plugin.SetUiLanguage(language);
            }
        }

        ImGui.EndCombo();
    }

    private void DrawBulkControls()
    {
        if (ImGui.Button($"{plugin.Localizer.Get(UiTextKey.EnableAll)}###enable-all"))
            plugin.SetAllMappingsEnabled(true);

        ImGui.SameLine();
        if (ImGui.Button($"{plugin.Localizer.Get(UiTextKey.DisableAll)}###disable-all"))
            plugin.SetAllMappingsEnabled(false);
    }

    private void DrawMappings()
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.RowBg
            | ImGuiTableFlags.BordersInnerH
            | ImGuiTableFlags.SizingStretchProp;
        if (!ImGui.BeginTable("##mapping-rows", 3, flags))
            return;

        ImGui.TableSetupColumn("##icon", ImGuiTableColumnFlags.WidthFixed, 48.0f);
        ImGui.TableSetupColumn("##name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##enabled", ImGuiTableColumnFlags.WidthFixed, 120.0f);

        foreach (var mapping in PrototypeContract.Mappings)
        {
            ImGui.PushID(checked((int)mapping.SourceCompanionRowId));
            ImGui.TableNextRow(ImGuiTableRowFlags.None, 48.0f);

            ImGui.TableSetColumnIndex(0);
            if (plugin.TryGetCompanionIcon(mapping.SourceCompanionRowId, out var icon))
                ImGui.Image(icon!.Handle, IconSize);
            else
                ImGui.Dummy(IconSize);

            ImGui.TableSetColumnIndex(1);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(plugin.GetCompanionName(mapping));

            ImGui.TableSetColumnIndex(2);
            var enabled = plugin.Configuration.IsMappingEnabled(mapping.SourceCompanionRowId);
            var label = plugin.Localizer.Get(enabled ? UiTextKey.Enabled : UiTextKey.Disabled);
            if (ImGui.Checkbox($"{label}###mapping-enabled", ref enabled))
                plugin.SetMappingEnabled(mapping.SourceCompanionRowId, enabled);

            ImGui.PopID();
        }

        ImGui.EndTable();
    }
}
