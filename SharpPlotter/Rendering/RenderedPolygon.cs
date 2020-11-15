using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SharpPlotter.Rendering
{
    public class RenderedPolygon
    {
        public Color FillColor { get; }
        public IReadOnlyList<Point2d> Points { get; }

        public RenderedPolygon(Color color, IEnumerable<Point2d> points)
        {
            FillColor = color;
            Points = new List<Point2d>(points);

            if (Points.Count < 3)
            {
                throw new InvalidOperationException("At least 3 points must be provided to draw a polygon");
            }
        }
    }
}