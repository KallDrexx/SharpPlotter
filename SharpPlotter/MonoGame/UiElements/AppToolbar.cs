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
                    if (ImGui.MenuItem("New")) NewClicked?.Invoke(this, EventArgs.Empty);
                    if (ImGui.MenuItem("Open")) OpenClicked?.Invoke(this, EventArgs.Empty);
                    
                    ImGui.Separator();
                    
                    if (ImGui.MenuItem("Settings")) SettingsClicked?.Invoke(this, EventArgs.Empty);

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