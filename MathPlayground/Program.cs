using System.IO;
using SkiaSharp;

namespace MathPlayground
{
    class Program
    {
        static void Main(string[] args)
        {
            const string directory = @"c:\temp";
            const string filename = "test.png";
            
            using var canvas = new Canvas(800, 600);
            
            using var file = File.OpenWrite(Path.Combine(directory, filename));
            using var image = canvas.Render();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            data.SaveTo(file);
        }
    }
}