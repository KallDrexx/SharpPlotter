using System;
using ImGuiHandler;
using ImGuiHandler.MonoGame;
using Microsoft.Xna.Framework;
using SharpPlotter.MonoGame.UiElements;

namespace SharpPlotter.MonoGame
{
    public class PlotterUi
    {
        private readonly ImGuiManager _imGuiManager;
        private readonly ImGuiDemoWindow _imGuiDemoWindow;

        public AppToolbar AppToolbar { get; }

        public bool AcceptingKeyboardInput => _imGuiManager.AcceptingKeyboardInput;
        public bool AcceptingMouseInput => _imGuiManager.AcceptingMouseInput;

        public PlotterUi(Game game, AppSettings appSettings)
        {
            var renderer = new MonoGameImGuiRenderer(game);
            renderer.Initialize();
            
            _imGuiManager = new ImGuiManager(renderer);
            
            _imGuiDemoWindow = new ImGuiDemoWindow();
            _imGuiManager.AddElement(_imGuiDemoWindow);
            
            AppToolbar = new AppToolbar{IsVisible = true};
            _imGuiManager.AddElement(AppToolbar);
            
            var settingsWindow = new SettingsWindow(appSettings);
            _imGuiManager.AddElement(settingsWindow);
            settingsWindow.SaveChangesRequested += (sender, e) =>
            {
                appSettings.ScriptFolderPath = settingsWindow.ScriptFolderPath;
                appSettings.TextEditorExecutable = settingsWindow.TextEditorExecutable;
                SettingsIo.Save(appSettings);

                settingsWindow.IsVisible = false;
            };

            AppToolbar.SettingsClicked += (sender, e) =>
            {
                settingsWindow.ResetSettings(appSettings);
                settingsWindow.IsVisible = true;
            };
        }

        public void Draw(TimeSpan timeSinceLastFrame)
        {
            _imGuiManager.RenderElements(timeSinceLastFrame);
        }

        public void ToggleImGuiDemoWindow() => _imGuiDemoWindow.IsVisible = !_imGuiDemoWindow.IsVisible;
    }
}