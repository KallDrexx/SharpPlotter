using System;
using ImGuiHandler;
using ImGuiNET;

namespace SharpPlotter.MonoGame.UiElements
{
    public class AppToolbar : ImGuiElement
    {
        public Point2d? MousePointerGraphLocation { get; set; }
        
        public event EventHandler NewClicked;
        public event EventHandler OpenClicked;
        public event EventHandler SettingsClicked;
        
        protected override void CustomRender()
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    ImGui.MenuItem("New");
                    ImGui.MenuItem("Open");
                    ImGui.Separator();
                    ImGui.MenuItem("Settings");
                    
                    ImGui.EndMenu();
                }
                
                if (MousePointerGraphLocation != null)
                {
                    ImGui.SameLine(ImGui.GetWindowWidth() - 150);
                    ImGui.Text($"({MousePointerGraphLocation.Value.X:F1},{MousePointerGraphLocation.Value.Y:F1})");
                }

                ImGui.EndMainMenuBar();
            }
        }
    }
}