using ImGuiHandler;
using ImGuiNET;

namespace SharpPlotter.MonoGame.UiElements
{
    public class ImGuiDemoWindow : ImGuiElement
    {
        protected override void CustomRender()
        {
            ImGui.ShowDemoWindow();
        }
    }
}