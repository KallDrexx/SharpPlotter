using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SharpPlotter.Rendering;
using SharpPlotter.Ui;

namespace SharpPlotter.MonoGame
{
    public class InputHandler
    {
        private const int PixelsPannedPerSecond = 360;
        private const int FovChangePerSecond = 100;
        
        private readonly Camera _camera;
        private readonly PlotterUi _plotterUi;
        private KeyboardState _previousKeyState, _currentKeyState;
        private MouseState _previousMouseState, _currentMouseState;

        public event EventHandler ResetCameraRequested;

        public InputHandler(Camera camera, PlotterUi plotterUi)
        {
            _camera = camera;
            _plotterUi = plotterUi;
            
            _currentKeyState = Keyboard.GetState();
            _currentMouseState = Mouse.GetState();
        }

        /// <summary>
        /// Checks for any input that happened since the last frame, and adjust the graph accordingly
        /// </summary>
        public void Update(GameTime gameTime)
        {
            _previousKeyState = _currentKeyState;
            _previousMouseState = _currentMouseState;
            _currentKeyState = Keyboard.GetState();
            _currentMouseState = Mouse.GetState();

            HandleKeyboardInput(gameTime);
            HandleMouseInput();
        }

        private void HandleKeyboardInput(GameTime gameTime)
        {
            if (HasBeenPressed(Keys.F12))
            {
                _plotterUi.ToggleImGuiDemoWindow();
            }

            // Camera actions should only be taken if UI elements do not have keyboard focus
            if (!_plotterUi.AcceptingKeyboardInput)
            {
                if (HasBeenPressed(Keys.PageDown))
                {
                    _camera.ZoomFactor *= 2;
                }

                if (HasBeenPressed(Keys.PageUp))
                {
                    _camera.ZoomFactor /= 2;
                }

                if (_currentKeyState.IsKeyDown(Keys.Up))
                {
                    var changeInY = (int) (PixelsPannedPerSecond * gameTime.ElapsedGameTime.TotalSeconds);
                    _camera.MoveByPixelAmount(0, changeInY);
                }

                if (_currentKeyState.IsKeyDown(Keys.Down))
                {
                    var changeInY = (int) (PixelsPannedPerSecond * gameTime.ElapsedGameTime.TotalSeconds);
                    _camera.MoveByPixelAmount(0, -changeInY);
                }

                if (_currentKeyState.IsKeyDown(Keys.Left))
                {
                    var changeInX = (int) (PixelsPannedPerSecond * gameTime.ElapsedGameTime.TotalSeconds);
                    _camera.MoveByPixelAmount(-changeInX, 0);
                }

                if (_currentKeyState.IsKeyDown(Keys.Right))
                {
                    var changeInX = (int) (PixelsPannedPerSecond * gameTime.ElapsedGameTime.TotalSeconds);
                    _camera.MoveByPixelAmount(changeInX, 0);
                }

                if (_currentKeyState.IsKeyDown(Keys.Insert))
                {
                    var changeInX = (int) (FovChangePerSecond * gameTime.ElapsedGameTime.TotalSeconds);
                    _camera.ChangeFieldOfView(changeInX, 0);
                }

                if (_currentKeyState.IsKeyDown(Keys.Delete))
                {
                    var changeInX = (int) (FovChangePerSecond * gameTime.ElapsedGameTime.TotalSeconds);
                    _camera.ChangeFieldOfView(-changeInX, 0);
                }

                if (_currentKeyState.IsKeyDown(Keys.Home))
                {
                    var changeInY = (int) (FovChangePerSecond * gameTime.ElapsedGameTime.TotalSeconds);
                    _camera.ChangeFieldOfView(0, changeInY);
                }

                if (_currentKeyState.IsKeyDown(Keys.End))
                {
                    var changeInY = (int) (FovChangePerSecond * gameTime.ElapsedGameTime.TotalSeconds);
                    _camera.ChangeFieldOfView(0, -changeInY);
                }

                if (HasBeenPressed(Keys.Back))
                {
                    ResetCameraRequested?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void HandleMouseInput()
        {
            // Only react to mouse movements if the UI elements do not have mouse focus
            if (_plotterUi.AcceptingMouseInput)
            {
                // Mouse is on ImGui, so no graph coordinates required
                _plotterUi.AppToolbar.MousePointerGraphLocation = null;
            }
            else
            {
                var (mouseX, mouseY) = _currentMouseState.Position;
                var graphCoordsAtMouse = _camera.GetGraphPointForPixelCoordinates(mouseX, mouseY);
                _plotterUi.AppToolbar.MousePointerGraphLocation = graphCoordsAtMouse;
                
                var scrollChange = _currentMouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
                if (scrollChange != 0)
                {
                    const float scrollZoomModifier = 1.2f;
                    _camera.ZoomFactor = scrollChange > 0
                        ? _camera.ZoomFactor * scrollZoomModifier
                        : _camera.ZoomFactor / scrollZoomModifier;
                }

                var positionChange = _currentMouseState.Position - _previousMouseState.Position;
                if (_previousMouseState.LeftButton == ButtonState.Pressed && 
                    _currentMouseState.LeftButton == ButtonState.Pressed && 
                    positionChange != Point.Zero)
                {
                    _camera.MoveByPixelAmount(-positionChange.X, positionChange.Y);
                }
            }
        }

        private bool HasBeenPressed(Keys key)
        {
            return _currentKeyState.IsKeyDown(key) && !_previousKeyState.IsKeyDown(key);
        }
    }
}