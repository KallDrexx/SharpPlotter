using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
        private readonly byte[] _rawCanvasPixels;
        private SpriteBatch _spriteBatch;
        private Texture2D _graphTexture;
        private ScriptRunner _scriptRunner;
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
            
            // Do a first render to get pixel data from the image for initial byte data allocation
            using var image = _canvas.Render();
            _rawCanvasPixels = new byte[image.Height * image.PeekPixels().RowBytes];
        }

        protected override void Initialize()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _currentKeyState = Keyboard.GetState();

            const string filename = @"c:\temp\test.cs";
            _scriptRunner = new ScriptRunner(_canvas, filename);
            Process.Start("cmd", $"/C code {filename}");

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            _previousKeyState = _currentKeyState;
            _currentKeyState = Keyboard.GetState();
            
            var requireReRender = HandleKeyboardInput();

            if (_scriptRunner.CheckForChanges())
            {
                requireReRender = true;
            }

            if (_graphTexture == null || requireReRender)
            {
                using var image = _canvas.Render();
                RenderImageToTexture2D(image, GraphicsDevice);
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

        private void RenderImageToTexture2D(SKImage image, GraphicsDevice graphicsDevice)
        {
            var pixelMap = image.PeekPixels();
            var pointer = pixelMap.GetPixels();

            Marshal.Copy(pointer, _rawCanvasPixels, 0, _rawCanvasPixels.Length);

            if (_graphTexture == null)
            {
                _graphTexture = new Texture2D(graphicsDevice, image.Width, image.Height);
            }
            
            _graphTexture.SetData(_rawCanvasPixels);
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
    }
}