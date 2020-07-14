using ImGuiHandler;
using ImGuiNET;

namespace SharpPlotter.Ui.UiElements
{
    public class ImGuiDemoWindow : ImGuiElement
    {
        protected override void CustomRender()
        {
            ImGui.ShowDemoWindow();
        }
    }
}