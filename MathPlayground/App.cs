using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;

namespace MathPlayground
{
    public class App : Game
    {
        private const int Width = 1024;
        private const int Height = 768;
        
        private readonly GraphicsDeviceManager _graphics;
        private readonly Canvas _canvas;
        private SpriteBatch _spriteBatch;
        private Texture2D _graphTexture;

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
            
            _canvas.SetGraphBounds(-10, 10, -10, 10);
            _canvas.DrawPoints(Color.Green, (6,4), (3,1), (1,2), (-1,5), (-3,4), (-4,4), (-5,3), (-5,2), (-2,2),
                (-5,1), (-4,0), (-2,1), (-1,0), (0, -3), (-1,-4), (1,-4), (2,-3), (1,-2), (3,-1), (5,1));
            
            _canvas.DrawPolygon(Color.Red, (6,4), (3,1), (1,2), (-1,5), (-3,4), (-4,4), (-5,3), (-5,2), (-2,2),
                (-5,1), (-4,0), (-2,1), (-1,0), (0, -3), (-1,-4), (1,-4), (2,-3), (1,-2), (3,-1), (5,1));
            
            _canvas.DrawSegments((-1, -9), (-2, -8), (-3, -5));

            using var image = _canvas.Render();
            _graphTexture = RenderImageToTexture2D(image, GraphicsDevice);
            
            base.Initialize();
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
    }
}