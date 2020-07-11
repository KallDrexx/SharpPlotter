using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SharpPlotter.Rendering;

namespace SharpPlotter.MonoGame
{
    public class InputHandler
    {
        private const int PixelsPannedPerSecond = 360;
        
        private readonly Camera _camera;
        private KeyboardState _previousKeyState, _currentKeyState;
        private MouseState _previousMouseState, _currentMouseState;

        public InputHandler(Camera camera)
        {
            _camera = camera;
            
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
                var changeInY = (int)(PixelsPannedPerSecond * gameTime.ElapsedGameTime.TotalSeconds);
                _camera.MoveByPixelAmount(0, changeInY);
            }

            if (_currentKeyState.IsKeyDown(Keys.Down))
            {
                var changeInY = (int)(PixelsPannedPerSecond * gameTime.ElapsedGameTime.TotalSeconds);
                _camera.MoveByPixelAmount(0, -changeInY);
            }

            if (_currentKeyState.IsKeyDown(Keys.Left))
            {
                var changeInX = (int)(PixelsPannedPerSecond * gameTime.ElapsedGameTime.TotalSeconds);
                _camera.MoveByPixelAmount(-changeInX, 0);
            }

            if (_currentKeyState.IsKeyDown(Keys.Right))
            {
                var changeInX = (int)(PixelsPannedPerSecond * gameTime.ElapsedGameTime.TotalSeconds);
                _camera.MoveByPixelAmount(changeInX, 0);
            }

            if (HasBeenPressed(Keys.Back))
            {
                _camera.Origin = new Point2d(0, 0);
                _camera.ZoomFactor = 1f;
            }
        }

        private void HandleMouseInput()
        {
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

        private bool HasBeenPressed(Keys key)
        {
            return _currentKeyState.IsKeyDown(key) && !_previousKeyState.IsKeyDown(key);
        }
    }
}