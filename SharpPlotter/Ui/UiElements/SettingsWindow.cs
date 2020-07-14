using System;
using ImGuiHandler;
using ImGuiNET;
using SharpPlotter.MonoGame;

namespace SharpPlotter.Ui.UiElements
{
    public class SettingsWindow : ImGuiElement
    {
        public event EventHandler SaveChangesRequested;
        
        [HasTextBuffer(1000)]
        public string ScriptFolderPath
        {
            get => Get<string>();
            set => Set(value);
        }

        [HasTextBuffer(256)]
        public string TextEditorExecutable
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool CloseRequested
        {
            get => Get<bool>();
            set => Set(value);
        }

        public SettingsWindow(AppSettings settings)
        {
            ResetSettings(settings);
        }

        public void ResetSettings(AppSettings settings)
        {
            using (DisablePropertyChangedNotifications())
            {
                ScriptFolderPath = settings.ScriptFolderPath;
                TextEditorExecutable = settings.TextEditorExecutable;
            }
        }

        protected override void CustomRender()
        {
            ImGui.OpenPopup("Settings");
            
            var isOpen = true;
            if (ImGui.BeginPopupModal("Settings", ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.PushItemWidth(500f);
                InputText(nameof(ScriptFolderPath), "Script Folder");
                InputText(nameof(TextEditorExecutable), "Text Editor Executable");
                ImGui.PopItemWidth();
                
                ImGui.NewLine();
                ImGui.NewLine();

                if (ImGui.Button("Save"))
                {
                    SaveChangesRequested?.Invoke(this, EventArgs.Empty);
                }
                
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                    CloseRequested = true;
                }
                
                ImGui.EndPopup();
            }

            if (!isOpen)
            {
                // X in the top right was pressed.  Treat it as a cancel
                CloseRequested = true;
            }
        }
    }
}