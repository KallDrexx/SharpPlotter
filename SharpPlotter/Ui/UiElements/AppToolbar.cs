using System;
using System.Diagnostics;
using ImGuiHandler;
using ImGuiNET;

namespace SharpPlotter.Ui.UiElements
{
    public class AppToolbar : ImGuiElement
    {
        private readonly ScriptManager _scriptManager;

        public Point2d? MousePointerGraphLocation { get; set; }
        
        public event EventHandler NewClicked;
        public event EventHandler OpenClicked;
        public event EventHandler SettingsClicked;

        public AppToolbar(ScriptManager scriptManager)
        {
            _scriptManager = scriptManager;
        }
        
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

                if (!string.IsNullOrWhiteSpace(_scriptManager.CurrentFileName) && _scriptManager.CurrentLanguage != null)
                {
                    var languageName = _scriptManager.CurrentLanguage.Value switch
                    {
                        ScriptLanguage.CSharp => "C#",
                        _ => ""
                    };
                    
                    ImGui.Text($"- {_scriptManager.CurrentFileName} ({languageName})");
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