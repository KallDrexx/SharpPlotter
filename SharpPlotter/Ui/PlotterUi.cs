using System;
using System.ComponentModel;
using System.IO;
using ImGuiHandler;
using ImGuiHandler.MonoGame;
using Microsoft.Xna.Framework;
using SharpPlotter.Rendering;
using SharpPlotter.Scripting;
using SharpPlotter.Ui.UiElements;

namespace SharpPlotter.Ui
{
    public class PlotterUi
    {
        private readonly ImGuiManager _imGuiManager;
        private readonly ImGuiDemoWindow _imGuiDemoWindow;
        private readonly AppSettings _appSettings;
        private readonly ScriptManager _scriptManager;
        private readonly OnScreenLogger _onScreenLogger;
        private readonly Camera _camera;

        public AppToolbar AppToolbar { get; }

        public bool AcceptingKeyboardInput => _imGuiManager.AcceptingKeyboardInput;
        public bool AcceptingMouseInput => _imGuiManager.AcceptingMouseInput;

        public PlotterUi(Game game, 
            AppSettings appSettings, 
            ScriptManager scriptManager, 
            OnScreenLogger onScreenLogger, 
            Camera camera)
        {
            _appSettings = appSettings;
            _scriptManager = scriptManager;
            _onScreenLogger = onScreenLogger;
            _camera = camera;

            var renderer = new MonoGameImGuiRenderer(game);
            renderer.Initialize();
            
            _imGuiManager = new ImGuiManager(renderer);
            
            _imGuiDemoWindow = new ImGuiDemoWindow();
            _imGuiManager.AddElement(_imGuiDemoWindow);
            
            var messageOverlay = new MessageOverlay(onScreenLogger){IsVisible = true};
            messageOverlay.DismissMostRecentMessageClicked +=
                (sender, args) => _onScreenLogger.RemoveMostRecentMessage();
            
            _imGuiManager.AddElement(messageOverlay);
            
            AppToolbar = new AppToolbar(_scriptManager, _appSettings){IsVisible = true};
            _imGuiManager.AddElement(AppToolbar);
            
            _imGuiManager.AddElement(new ImGuiSettings{IsVisible = true});
        
            AppToolbar.PropertyChanged += AppToolbarOnPropertyChanged;
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
                    _scriptManager.CreateNewScript(dialog.FileName?.Trim(), dialog.SelectedLanguage);
                }
                catch (InvalidOperationException exception)
                {
                    dialog.ErrorText = exception.Message;
                    return;
                }
                catch (Exception exception)
                {
                    dialog.ErrorText = $"Exception: {exception}";
                    return;
                }
                
                _imGuiManager.RemoveElement(dialog);
                SettingsIo.Save(_appSettings);
                _onScreenLogger.Clear();
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
            _onScreenLogger.Clear();
            
            try
            {
                _scriptManager.OpenExistingScript(fileName?.Trim());
            }
            catch (FileNotFoundException exception)
            {
                _onScreenLogger.LogMessage($"The file '{exception.FileName}' does not exist");
                
                // If this file is a recently opened, remove it from the list
                _appSettings.RecentlyOpenedFiles.Remove(fileName);
            }
            catch (Exception exception)
            {
                _onScreenLogger.LogMessage($"Exception opening file: {exception.Message}");
            }
            
            SettingsIo.Save(_appSettings);
        }

        private void AppToolbarOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AppToolbar.HideGridLines):
                    _camera.HideGridLines = AppToolbar.HideGridLines;
                    break;
            }
        }
    }
}