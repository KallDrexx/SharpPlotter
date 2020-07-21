using Microsoft.Xna.Framework;

namespace SharpPlotter.Rendering
{
    public readonly struct RenderedArrow
    {
        public readonly Point2d Start;
        public readonly Point2d End;
        public readonly Color Color;

        public RenderedArrow(Point2d start, Point2d end, Color color)
        {
            Start = start;
            End = end;
            Color = color;
        }
    }
}