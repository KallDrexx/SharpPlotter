using Microsoft.Xna.Framework;
using SharpPlotter.Primitives;

namespace SharpPlotter.Rendering
{
    public readonly struct RenderedSegment
    {
        public readonly Point2d Start;
        public readonly Point2d End;
        public readonly Color Color;

        public RenderedSegment(Point2d start, Point2d end, Color color)
        {
            Start = start;
            End = end;
            Color = color;
        }
    }
}