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
        public bool Update(GameTime gameTime)
        {
            _previousKeyState = _currentKeyState;
            _previousMouseState = _currentMouseState;
            _currentKeyState = Keyboard.GetState();
            _currentMouseState = Mouse.GetState();

            var keyboardInputMade = HandleKeyboardInput(gameTime);
            var mouseInputMade = HandleMouseInput();

            return keyboardInputMade || mouseInputMade;
        }

        private bool HandleKeyboardInput(GameTime gameTime)
        {
            var inputHandled = false;

            if (HasBeenPressed(Keys.PageDown))
            {
                inputHandled = true;
                _camera.ZoomFactor *= 2;
            }

            if (HasBeenPressed(Keys.PageUp))
            {
                inputHandled = true;
                _camera.ZoomFactor /= 2;
            }

            if (_currentKeyState.IsKeyDown(Keys.Up))
            {
                inputHandled = true;
                var changeInY = (int)(PixelsPannedPerSecond * gameTime.ElapsedGameTime.TotalSeconds);
                _camera.MoveByPixelAmount(0, changeInY);
            }

            if (_currentKeyState.IsKeyDown(Keys.Down))
            {
                inputHandled = true;
                var changeInY = (int)(PixelsPannedPerSecond * gameTime.ElapsedGameTime.TotalSeconds);
                _camera.MoveByPixelAmount(0, -changeInY);
            }

            if (_currentKeyState.IsKeyDown(Keys.Left))
            {
                inputHandled = true;
                var changeInX = (int)(PixelsPannedPerSecond * gameTime.ElapsedGameTime.TotalSeconds);
                _camera.MoveByPixelAmount(-changeInX, 0);
            }

            if (_currentKeyState.IsKeyDown(Keys.Right))
            {
                inputHandled = true;
                var changeInX = (int)(PixelsPannedPerSecond * gameTime.ElapsedGameTime.TotalSeconds);
                _camera.MoveByPixelAmount(changeInX, 0);
            }

            if (HasBeenPressed(Keys.Back))
            {
                inputHandled = true;
                _camera.Origin = new Point2d(0, 0);
                _camera.ZoomFactor = 1f;
            }

            return inputHandled;
        }

        private bool HandleMouseInput()
        {
            const int scrollValueDivider = 10;
            
            var inputHandled = false;
            var scrollChange = _currentMouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
            scrollChange /= scrollValueDivider;
            
            if (scrollChange != 0)
            {
                inputHandled = true;
            }

            var positionChange = _currentMouseState.Position - _previousMouseState.Position;
            if (_previousMouseState.LeftButton == ButtonState.Pressed && 
                _currentMouseState.LeftButton == ButtonState.Pressed && 
                positionChange != Point.Zero)
            {
                inputHandled = true;
                
                _camera.MoveByPixelAmount(-positionChange.X, positionChange.Y);
            }

            return inputHandled;
        }

        private bool HasBeenPressed(Keys key)
        {
            return _currentKeyState.IsKeyDown(key) && !_previousKeyState.IsKeyDown(key);
        }
    }
}