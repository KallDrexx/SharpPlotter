using ImGuiHandler;
using ImGuiNET;

namespace SharpPlotter.Ui.UiElements
{
    public class ImGuiSettings : ImGuiElement
    {
        protected override void CustomRender()
        {
            ImGui.GetIO().FontGlobalScale = 1.10f;
        }
    }
}