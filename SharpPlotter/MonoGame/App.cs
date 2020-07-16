using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpPlotter.Rendering;
using SharpPlotter.Scripting;
using SharpPlotter.Ui;
using SkiaSharp;

namespace SharpPlotter.MonoGame
{
    public class App : Game
    {
        private const int Width = 1024;
        private const int Height = 768;
        
        private readonly Camera _camera;
        private readonly byte[] _rawCanvasPixels;
        private readonly ScriptManager _scriptManager;
        private readonly AppSettings _appSettings;
        private readonly OnScreenLogger _onScreenLogger;
        private GraphedItems _graphedItems;
        private SpriteBatch _spriteBatch;
        private Texture2D _graphTexture;
        private InputHandler _inputHandler;
        private PlotterUi _plotterUi;
        private bool _resetCameraRequested;

        public App()
        {
            // ReSharper disable once ObjectCreationAsStatement
            new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = Width, 
                PreferredBackBufferHeight = Height,
                PreferMultiSampling = true
            };

            IsMouseVisible = true;
            _camera = new Camera(Width, Height);
            _graphedItems = new GraphedItems();

            // Do a first render to get pixel data from the image for initial byte data allocation
            using var image = _camera.Render(null, null);
            _rawCanvasPixels = new byte[image.Height * image.PeekPixels().RowBytes];
            
            _appSettings = SettingsIo.Load() ?? new AppSettings
            {
                ScriptFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "SharpPlotter"),
                TextEditorExecutable = "code",
            };

            _onScreenLogger = new OnScreenLogger();
            _onScreenLogger.LogMessage("Use the file menu above to create a new script, or to open an existing one");
            
            _scriptManager = new ScriptManager(_appSettings, _onScreenLogger);
        }

        protected override void Initialize()
        {
            _plotterUi = new PlotterUi(this, _appSettings, _scriptManager, _onScreenLogger);
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _inputHandler = new InputHandler(_camera, _plotterUi);
            _inputHandler.ResetCameraRequested += (sender, args) => _resetCameraRequested = true;

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            _inputHandler.Update(gameTime);

            if (_resetCameraRequested)
            {
                SetCameraToSizeOfGraphedItems();
                _resetCameraRequested = false;
            }

            var newGraphedItems = _scriptManager.CheckForNewGraphedItems();
            if (newGraphedItems != null)
            {
                _graphedItems = newGraphedItems;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            
            if (_graphTexture == null || _camera.CameraHasMoved || _graphedItems.ItemsChangedSinceLastRender)
            {
                var itemsToRender = _graphedItems.GetItemsToRender();
                using var image = _camera.Render(itemsToRender.Points, itemsToRender.Segments);
                RenderImageToTexture2D(image, GraphicsDevice);
            }
            
            _spriteBatch.Begin();
            _spriteBatch.Draw(_graphTexture, Vector2.Zero, Color.White);
            _spriteBatch.End();
            
            _plotterUi.Draw(gameTime.ElapsedGameTime);
            
            base.Draw(gameTime);
        }

        private void RenderImageToTexture2D(SKImage image, GraphicsDevice graphicsDevice)
        {
            var pixelMap = image.PeekPixels();
            var pointer = pixelMap.GetPixels();

            Marshal.Copy(pointer, _rawCanvasPixels, 0, _rawCanvasPixels.Length);

            _graphTexture ??= new Texture2D(graphicsDevice, image.Width, image.Height);
            _graphTexture.SetData(_rawCanvasPixels);
        }

        private void SetCameraToSizeOfGraphedItems()
        {
            var minCoords = _graphedItems?.MinCoordinates;
            var maxCoords = _graphedItems?.MaxCoordinates;

            if (minCoords == null || maxCoords == null)
            {
                _camera.Origin = new Point2d(0, 0);
                _camera.ZoomFactor = 1f;
                _camera.ResetFieldOfView();
            }
            else
            {
                var x = ((int) minCoords.Value.X, (int) maxCoords.Value.X);
                var y = ((int) minCoords.Value.Y, (int) maxCoords.Value.Y);
                _camera.SetGraphBounds(x, y);
            }
        }
    }
}