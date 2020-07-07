using System.IO;
using MathPlayground.Primitives;
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
            canvas.SetGraphBounds(-10, 10, -10, 10);
            canvas.DrawPoints(Color.Green, (6,4), (3,1), (1,2), (-1,5), (-3,4), (-4,4), (-5,3), (-5,2), (-2,2),
                (-5,1), (-4,0), (-2,1), (-1,0), (0, -3), (-1,-4), (1,-4), (2,-3), (1,-2), (3,-1), (5,1));
            
            canvas.DrawPolygon(Color.Red, (6,4), (3,1), (1,2), (-1,5), (-3,4), (-4,4), (-5,3), (-5,2), (-2,2),
                (-5,1), (-4,0), (-2,1), (-1,0), (0, -3), (-1,-4), (1,-4), (2,-3), (1,-2), (3,-1), (5,1));
            
            canvas.DrawSegments((-1, -9), (-2, -8), (-3, -5));

            using var file = File.OpenWrite(Path.Combine(directory, filename));
            using var image = canvas.Render();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            data.SaveTo(file);
        }
    }
}