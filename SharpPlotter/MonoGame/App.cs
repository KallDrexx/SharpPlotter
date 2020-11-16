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
        private const int DefaultWidth = 1024;
        private const int DefaultHeight = 768;
        
        private readonly ScriptManager _scriptManager;
        private readonly AppSettings _appSettings;
        private readonly OnScreenLogger _onScreenLogger;
        private Camera _camera;
        private byte[] _rawCanvasPixels;
        private GraphedItems _graphedItems;
        private SpriteBatch _spriteBatch;
        private Texture2D _graphTexture;
        private InputHandler _inputHandler;
        private PlotterUi _plotterUi;
        private bool _resetCameraRequested;

        public App()
        {
            _appSettings = SettingsIo.Load() ?? new AppSettings
            {
                ScriptFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "SharpPlotter"),
                TextEditorExecutable = "code",
            };
            
            // ReSharper disable once ObjectCreationAsStatement
            new GraphicsDeviceManager(this)
            {
                PreferMultiSampling = true,
                PreferredBackBufferWidth = DefaultWidth,
                PreferredBackBufferHeight = DefaultHeight,
            };
            
            IsMouseVisible = true;
            _onScreenLogger = new OnScreenLogger();
            _onScreenLogger.LogMessage(OnLoadText());

            _graphedItems = new GraphedItems(); 
            _scriptManager = new ScriptManager(_appSettings, _onScreenLogger);
            
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += WindowOnClientSizeChanged;
        }

        protected override void Initialize()
        {
            _camera = new Camera(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, _onScreenLogger);
            WindowOnClientSizeChanged(null, EventArgs.Empty);

            _plotterUi = new PlotterUi(this, _appSettings, _scriptManager, _onScreenLogger, _camera);
            _plotterUi.AppToolbar.UpdateCameraOriginRequested += AppToolbarOnUpdateCameraOriginRequested;
            _plotterUi.AppToolbar.UpdateCameraBoundsRequested += AppToolbarOnUpdateCameraBoundsRequested;
            _plotterUi.AppToolbar.ResetCameraRequested += (sender, args) => SetCameraToSizeOfGraphedItems();

            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _inputHandler = new InputHandler(_camera, _plotterUi);
            _inputHandler.ResetCameraRequested += (sender, args) => _resetCameraRequested = true;

            UpdateToolbarWithCameraProperties();

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

            if (_camera.CameraHasMoved)
            {
                UpdateToolbarWithCameraProperties();
            }

            var newGraphedItems = _scriptManager.CheckForNewGraphedItems();
            if (newGraphedItems != null)
            {
                _graphedItems = newGraphedItems;

                while (_graphedItems.Messages.TryDequeue(out var message))
                {
                    _onScreenLogger.LogMessage(message);
                }
            }

            _graphedItems?.Update(gameTime.ElapsedGameTime.TotalSeconds);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            if (_camera.CameraHasMoved || _graphedItems.ItemsChangedSinceLastRender)
            {
                var itemsToRender = _graphedItems.GetItemsToRender();
                using var image = _camera.Render(itemsToRender);
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

            _rawCanvasPixels ??= new byte[image.Height * image.PeekPixels().RowBytes];
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
            else if (minCoords.Value == maxCoords.Value)
            {
                // Only one point exists, so instead of binding to it we just want to center it
                _camera.Origin = minCoords.Value;
            }
            else
            {
                var x = (minCoords.Value.X, maxCoords.Value.X);
                var y = (minCoords.Value.Y, maxCoords.Value.Y);
                _camera.SetGraphBounds(x, y);
            }
        }

        private void UpdateToolbarWithCameraProperties()
        {
            _plotterUi.AppToolbar.CameraOrigin = _camera.Origin;
            _plotterUi.AppToolbar.CameraMinBounds = _camera.MinimumGraphBounds;
            _plotterUi.AppToolbar.CameraMaxBounds = _camera.MaximumGraphBounds;
        }

        private void AppToolbarOnUpdateCameraOriginRequested(object sender, EventArgs e)
        {
            _camera.Origin = _plotterUi.AppToolbar.CameraOrigin;
        }

        private void AppToolbarOnUpdateCameraBoundsRequested(object sender, EventArgs e)
        {
            var x = ((int) _plotterUi.AppToolbar.CameraMinBounds.X, (int) _plotterUi.AppToolbar.CameraMaxBounds.X);
            var y = ((int) _plotterUi.AppToolbar.CameraMinBounds.Y, (int) _plotterUi.AppToolbar.CameraMaxBounds.Y);

            _camera.SetGraphBounds(x, y);
        }

        private string OnLoadText()
        {
            return $"Welcome to SharpPlotter!{Environment.NewLine}{Environment.NewLine}" +
                   $"SharpPlotter allows drawing points and lines in real-time using standard programming languages, using " +
                   $"any code editor you prefer.{Environment.NewLine}{Environment.NewLine}" +
                   $"To start, use the File menu above to create a new script, or to open an existing one. {Environment.NewLine}{Environment.NewLine}" +
                   $"Scripts Directory: {_appSettings.ScriptFolderPath}{Environment.NewLine}" +
                   $"Text Editor Executable: {_appSettings.TextEditorExecutable}{Environment.NewLine}";
        }

        private void WindowOnClientSizeChanged(object? sender, EventArgs e)
        {
            var width = GraphicsDevice.Viewport.Width;
            var height = GraphicsDevice.Viewport.Height;
            _camera.ResizeViewport(width, height);
            
            // Raw pixel array and graph texture no longer have the proper dimensions, so clear them out so they are
            // regenerated next render call.
            _rawCanvasPixels = null;
            _graphTexture = null;
        }
    }
}