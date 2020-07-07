using System;
using System.Collections.Generic;
using System.Linq;
using MathPlayground.Primitives;

namespace MathPlayground
{
    public class GraphItems
    {
        private readonly List<GraphPoint2d> _points  = new List<GraphPoint2d>();
        private readonly List<Path2d> _paths = new List<Path2d>();

        public IEnumerable<GraphPoint2d> Points => _points;
        public IEnumerable<Path2d> Paths => _paths;
        public bool HasAnyItems => _points.Any() || _paths.Any();
        public float MinX { get; private set; }
        public float MinY { get; private set; }
        public float MaxX { get; private set; }
        public float MaxY { get; private set; }

        public GraphItems()
        {
            Clear();
        }

        public void AddPoints(params GraphPoint2d[] points)
        {
            points ??= Array.Empty<GraphPoint2d>();
            
            foreach (var point in points)
            {
                RecalculateMinMax(point.X, point.Y);
                _points.Add(point);
            }
        }

        public void AddPaths(params Path2d[] paths)
        {
            paths ??= Array.Empty<Path2d>();
            
            foreach (var path in paths)
            {
                foreach (var point in path.Points)
                {
                    RecalculateMinMax(point.X, point.Y);
                }
                
                _paths.Add(path);
            }
        }

        public void Clear()
        {
            _points.Clear();
            _paths.Clear();
            MinX = float.MaxValue;
            MinY = float.MaxValue;
            MaxX = float.MinValue;
            MaxY = float.MinValue;
        }

        private void RecalculateMinMax(float x, float y)
        {
            if (x < MinX) MinX = x;
            if (x > MaxX) MaxX = x;
            if (y < MinY) MinY = y;
            if (y > MaxY) MaxY = y;
        }
    }
}