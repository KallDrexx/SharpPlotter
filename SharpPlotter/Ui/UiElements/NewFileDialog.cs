using System;
using ImGuiHandler;
using ImGuiNET;
using SharpPlotter.Scripting;

namespace SharpPlotter.Ui.UiElements
{
    public class NewFileDialog : ImGuiElement
    {
        private static readonly string[] LanguageOptions = {"C#", "Javascript"};
        private int _selectedLanguageIndex;
        
        public event EventHandler CreateFileRequested;
        
        [HasTextBuffer(100)]
        public string FileName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ErrorText
        {
            get => Get<string>();
            set => Set(value);
        }

        public ScriptLanguage? SelectedLanguage
        {
            get => Get<ScriptLanguage>();
            set => Set(value);
        }

        public bool CloseRequested
        {
            get => Get<bool>();
            set => Set(value);
        }
        
        protected override void CustomRender()
        {
            const string title = "Create Script";
            ImGui.OpenPopup(title);

            var isOpen = true;
            if (ImGui.BeginPopupModal(title, ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.PushItemWidth(500f);
                InputText(nameof(FileName), "File Name");

                
                ImGui.Combo("Scripting Language", ref _selectedLanguageIndex, LanguageOptions, LanguageOptions.Length);
                SelectedLanguage = _selectedLanguageIndex switch
                {
                    0 => ScriptLanguage.CSharp,
                    1 => ScriptLanguage.Javascript,
                    _ => null
                };

                if (!string.IsNullOrWhiteSpace(ErrorText))
                {
                    ImGui.NewLine();
                    ImGui.TextWrapped(ErrorText);
                }

                ImGui.PopItemWidth();
                
                if (ImGui.Button("Create"))
                {
                    CreateFileRequested?.Invoke(this, EventArgs.Empty);
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