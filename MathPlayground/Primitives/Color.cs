using SkiaSharp;

namespace MathPlayground.Primitives
{
    public readonly struct Color
    {
        public static readonly Color Red = new Color(255, 0, 0);
        public static readonly Color Green = new Color(0, 255, 0);
        public static readonly Color Blue = new Color(0, 0, 255);
        public static readonly Color Black = new Color(0, 0, 0);
        public static readonly Color White = new Color(255, 255, 255);
        
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;

        public Color(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }
        
        public static implicit operator Color((byte r, byte g, byte b) color) 
            => new Color(color.r, color.g, color.b); 
        
        public static implicit operator SKColor(Color color)
            => new SKColor(color.R, color.G, color.B);
    }
}