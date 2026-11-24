using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using MinionMirage.Localization;
using System.Numerics;

namespace MinionMirage;

internal sealed class ReleaseNotesWindow : Window
{
    private readonly Localizer localizer;

    internal ReleaseNotesWindow(Localizer localizer)
        : base($"{Plugin.DisplayName} — {localizer.Get(UiTextKey.ReleaseNotes)}###MinionMirageReleaseNotes")
    {
        this.localizer = localizer;
        Size = new Vector2(560, 360);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(360, 240),
            MaximumSize = new Vector2(1000, 10000),
        };
    }

    public override void PreDraw()
        => WindowName = $"{Plugin.DisplayName} — {localizer.Get(UiTextKey.ReleaseNotes)}###MinionMirageReleaseNotes";

    public override void Draw()
    {
        ImGui.TextUnformatted(Plugin.DisplayVersion);
        ImGui.Separator();
        ImGui.TextWrapped(localizer.Get(UiTextKey.ReleaseNotesPrishe));
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.TextUnformatted("0.1.0-beta.10");
        ImGui.Separator();
        ImGui.TextWrapped(localizer.Get(UiTextKey.ReleaseNotesFootsteps));
        ImGui.Spacing();
        ImGui.TextWrapped(localizer.Get(UiTextKey.ReleaseNotesMenu));
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.TextUnformatted("0.1.0-beta.9");
        ImGui.Separator();
        ImGui.TextWrapped(localizer.Get(UiTextKey.ReleaseNotesBeta9));
    }
}
