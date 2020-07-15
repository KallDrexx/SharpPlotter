using System;
using ImGuiHandler;
using ImGuiHandler.MonoGame;
using Microsoft.Xna.Framework;
using SharpPlotter.MonoGame;
using SharpPlotter.Ui.UiElements;

namespace SharpPlotter.Ui
{
    public class PlotterUi
    {
        private readonly ImGuiManager _imGuiManager;
        private readonly ImGuiDemoWindow _imGuiDemoWindow;
        private readonly AppSettings _appSettings;
        private readonly ScriptManager _scriptManager;

        public AppToolbar AppToolbar { get; }

        public bool AcceptingKeyboardInput => _imGuiManager.AcceptingKeyboardInput;
        public bool AcceptingMouseInput => _imGuiManager.AcceptingMouseInput;

        public PlotterUi(Game game, AppSettings appSettings, ScriptManager scriptManager)
        {
            _appSettings = appSettings;
            _scriptManager = scriptManager;

            var renderer = new MonoGameImGuiRenderer(game);
            renderer.Initialize();
            
            _imGuiManager = new ImGuiManager(renderer);
            
            _imGuiDemoWindow = new ImGuiDemoWindow();
            _imGuiManager.AddElement(_imGuiDemoWindow);
            
            AppToolbar = new AppToolbar(_scriptManager, _appSettings){IsVisible = true};
            _imGuiManager.AddElement(AppToolbar);

            AppToolbar.SettingsClicked += (sender, args) => CreateSettingsWindow();
            AppToolbar.NewClicked += (sender, args) => CreateNewFileDialog();
            AppToolbar.OpenClicked += (sender, args) => OpenScriptFile(args);
        }

        public void Draw(TimeSpan timeSinceLastFrame)
        {
            _imGuiManager.RenderElements(timeSinceLastFrame);
        }

        public void ToggleImGuiDemoWindow() => _imGuiDemoWindow.IsVisible = !_imGuiDemoWindow.IsVisible;

        private void CreateSettingsWindow()
        {
            var settingsWindow = new SettingsWindow(_appSettings) {IsVisible = true};
            _imGuiManager.AddElement(settingsWindow);
            
            settingsWindow.SaveChangesRequested += (sender, args) =>
            {
                _appSettings.ScriptFolderPath = settingsWindow.ScriptFolderPath;
                _appSettings.TextEditorExecutable = settingsWindow.TextEditorExecutable;
                SettingsIo.Save(_appSettings);
                
                _imGuiManager.RemoveElement(settingsWindow);
            };

            settingsWindow.PropertyChanged += (sender, args) =>
            {
                switch (args.PropertyName)
                {
                    case nameof(SettingsWindow.CloseRequested):
                        _imGuiManager.RemoveElement(settingsWindow);
                        break;
                }
            };
        }

        private void CreateNewFileDialog()
        {
            var dialog = new NewFileDialog {IsVisible = true};
            _imGuiManager.AddElement(dialog);

            dialog.CreateFileRequested += (sender, args) =>
            {
                try
                {
                    _scriptManager.CreateNewScript(dialog.FileName, dialog.SelectedLanguage);
                }
                catch (Exception exception)
                {
                    dialog.ErrorText = $"Exception: {exception.Message}";
                    return;
                }
                
                _imGuiManager.RemoveElement(dialog);
                SettingsIo.Save(_appSettings);
            };

            dialog.PropertyChanged += (sender, args) =>
            {
                switch (args.PropertyName)
                {
                    case nameof(NewFileDialog.CloseRequested):
                        _imGuiManager.RemoveElement(dialog);
                        break;
                }
            };
        }

        private void OpenScriptFile(string fileName)
        {
            try
            {
                _scriptManager.OpenExistingScript(fileName);
            }
            catch (Exception exception)
            {
                // Todo: need a good way to display generic errors
                Console.WriteLine($"Exception opening file: {exception.Message}");
            }
            
            SettingsIo.Save(_appSettings);
        }
    }
}