﻿using Microsoft.Xna.Framework;
using SharpPlotter.Primitives;

namespace SharpPlotter.Rendering
{
    public readonly struct RenderedPoint
    {
        public readonly Point2d Point;
        public readonly Color Color;

        public RenderedPoint(Point2d point, Color color)
        {
            Point = point;
            Color = color;
        }
    }
}