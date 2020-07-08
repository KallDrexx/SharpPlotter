using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SkiaSharp;

namespace SharpPlotter
{
    public class App : Game
    {
        private const int Width = 1024;
        private const int Height = 768;

        private readonly GraphicsDeviceManager _graphics;
        private readonly Canvas _canvas;
        private SpriteBatch _spriteBatch;
        private Texture2D _graphTexture;
        private KeyboardState _currentKeyState;
        private KeyboardState _previousKeyState;

        public App()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = Width, 
                PreferredBackBufferHeight = Height,
                PreferMultiSampling = true
            };

            IsMouseVisible = true;
            _canvas = new Canvas(Width, Height);
        }

        protected override void Initialize()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _currentKeyState = Keyboard.GetState();

            var options = ScriptOptions.Default
                .WithImports("System")
                .WithReferences(typeof(GraphItems).Assembly)
                .WithImports("SharpPlotter")
                .WithImports("SharpPlotter.Primitives")
                .WithReferences(typeof(Color).Assembly)
                .WithImports("Microsoft.Xna.Framework");

            var globals = new ScriptGlobals {Canvas = _canvas};

            CSharpScript.RunAsync(@"
            Canvas.SetGraphBounds(-10, 10, -10, 10);
            Canvas.DrawPoints(Color.Green, (6,4), (3,1), (1,2), (-1,5), (-3,4), (-4,4), (-5,3), (-5,2), (-2,2),
                (-5,1), (-4,0), (-2,1), (-1,0), (0, -3), (-1,-4), (1,-4), (2,-3), (1,-2), (3,-1), (5,1));
            
            Canvas.DrawPolygon(Color.Red, (6,4), (3,1), (1,2), (-1,5), (-3,4), (-4,4), (-5,3), (-5,2), (-2,2),
                (-5,1), (-4,0), (-2,1), (-1,0), (0, -3), (-1,-4), (1,-4), (2,-3), (1,-2), (3,-1), (5,1));
            
            Canvas.DrawSegments((-1, -9), (-2, -8), (-3, -5));
", options, globals).GetAwaiter().GetResult();

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            _previousKeyState = _currentKeyState;
            _currentKeyState = Keyboard.GetState();
            
            var requireReRender = HandleKeyboardInput();

            if (_graphTexture == null || requireReRender)
            {
                using var image = _canvas.Render();
                _graphTexture = RenderImageToTexture2D(image, GraphicsDevice);
            }
            
            
            
            

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            
            _spriteBatch.Begin();
            _spriteBatch.Draw(_graphTexture, Vector2.Zero, Color.White);
            _spriteBatch.End();
            
            base.Draw(gameTime);
        }

        private static Texture2D RenderImageToTexture2D(SKImage image, GraphicsDevice graphicsDevice)
        {
            var pixelMap = image.PeekPixels();
            var pointer = pixelMap.GetPixels();
            var pixels = new byte[image.Height * pixelMap.RowBytes];

            Marshal.Copy(pointer, pixels, 0, pixels.Length);
            var texture = new Texture2D(graphicsDevice, image.Width, image.Height);
            texture.SetData(pixels);

            return texture;
        }

        private bool HasBeenPressed(Keys key)
        {
            return _currentKeyState.IsKeyDown(key) && !_previousKeyState.IsKeyDown(key);
        }

        private bool HandleKeyboardInput()
        {
            var requireReRender = false;
            if (HasBeenPressed(Keys.PageUp))
            {
                requireReRender = true;
                _canvas.SetGraphBounds(_canvas.MinX - 1,
                    _canvas.MaxX + 1,
                    _canvas.MinY - 1,
                    _canvas.MaxY + 1);
            }

            if (HasBeenPressed(Keys.PageDown))
            {
                if (_canvas.MaxX - _canvas.MinX > 2 &&
                    _canvas.MaxY - _canvas.MinY > 2)
                {
                    requireReRender = true;
                    _canvas.SetGraphBounds(_canvas.MinX + 1,
                        _canvas.MaxX - 1,
                        _canvas.MinY + 1,
                        _canvas.MaxY - 1);
                }
            }

            if (HasBeenPressed(Keys.Up))
            {
                requireReRender = true;
                _canvas.SetGraphBounds(_canvas.MinX,
                    _canvas.MaxX,
                    _canvas.MinY - 1,
                    _canvas.MaxY - 1);
            }

            if (HasBeenPressed(Keys.Down))
            {
                requireReRender = true;
                _canvas.SetGraphBounds(_canvas.MinX,
                    _canvas.MaxX,
                    _canvas.MinY + 1,
                    _canvas.MaxY + 1);
            }

            if (HasBeenPressed(Keys.Left))
            {
                requireReRender = true;
                _canvas.SetGraphBounds(_canvas.MinX + 1,
                    _canvas.MaxX + 1,
                    _canvas.MinY,
                    _canvas.MaxY);
            }

            if (HasBeenPressed(Keys.Right))
            {
                requireReRender = true;
                _canvas.SetGraphBounds(_canvas.MinX - 1,
                    _canvas.MaxX - 1,
                    _canvas.MinY,
                    _canvas.MaxY);
            }

            if (HasBeenPressed(Keys.Back))
            {
                requireReRender = true;
                _canvas.EnableDynamicGraphBounds();
            }

            return requireReRender;
        }

        public class ScriptGlobals
        {
            public Canvas Canvas { get; set; }
        }
    }
}