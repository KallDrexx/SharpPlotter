using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpPlotter.Primitives;
using SharpPlotter.Rendering;
using SkiaSharp;

namespace SharpPlotter.MonoGame
{
    public class App : Game
    {
        private const int Width = 1024;
        private const int Height = 768;

        private readonly GraphicsDeviceManager _graphics;
        private readonly Canvas _canvas;
        private readonly Camera _camera;
        private readonly byte[] _rawCanvasPixels;
        private SpriteBatch _spriteBatch;
        private Texture2D _graphTexture;
        private ScriptRunner _scriptRunner;
        private InputHandler _inputHandler;

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
            _camera = new Camera(Width, Height);

            // Do a first render to get pixel data from the image for initial byte data allocation
            using var image = _camera.Render(null, null);
            _rawCanvasPixels = new byte[image.Height * image.PeekPixels().RowBytes];
        }

        protected override void Initialize()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _inputHandler = new InputHandler(_camera);

            // const string filename = @"c:\temp\test.cs";
            // _scriptRunner = new ScriptRunner(_canvas, filename);
            // Process.Start("cmd", $"/C code {filename}");

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            var requireReRender = _inputHandler.Update(gameTime);

            // if (_scriptRunner.CheckForChanges())
            // {
            //     requireReRender = true;
            // }
            //
            if (_graphTexture == null || requireReRender)
            {
                var points = new[]
                {
                    new RenderedPoint(new Point2d(1, 1), Color.Red),
                    new RenderedPoint(new Point2d(-1, 1), Color.Blue),
                    new RenderedPoint(new Point2d(-1, -1), Color.Green),
                    new RenderedPoint(new Point2d(1, -1), Color.White),
                };

                var segments = new[]
                {
                    new RenderedSegment(new Point2d(1, 1), new Point2d(-1, 1), Color.Red),
                    new RenderedSegment(new Point2d(-1, 1), new Point2d(-1, -1), Color.Blue),
                    new RenderedSegment(new Point2d(-1, -1), new Point2d(1, -1), Color.Green),
                    new RenderedSegment(new Point2d(1, -1), new Point2d(1, 1), Color.White),
                };
                
                using var image = _camera.Render(points, segments);
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
    }
}