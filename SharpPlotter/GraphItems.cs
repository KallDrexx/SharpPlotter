using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using SharpPlotter.Primitives;

namespace SharpPlotter
{
    public class GraphItems
    {
        private readonly List<GraphPoint2d> _points  = new List<GraphPoint2d>();
        private readonly List<Path2d> _paths = new List<Path2d>();
        private readonly Dictionary<object, Color> _colorMap = new Dictionary<object, Color>();

        public IEnumerable<GraphPoint2d> Points => _points;
        public IEnumerable<Path2d> Paths => _paths;
        public Dictionary<object, Color> ColorMap => _colorMap;
        public bool HasAnyItems => _points.Any() || _paths.Any();
        
        public float MinX { get; private set; }
        public float MinY { get; private set; }
        public float MaxX { get; private set; }
        public float MaxY { get; private set; }

        public GraphItems()
        {
            Clear();
        }

        public void AddPoints((float x, float y)[] points, Color? color = null)
        {
            points ??= Array.Empty<(float, float)>();
            
            foreach (var point in points)
            {
                RecalculateMinMax(point.x, point.y);
                var graphPoint = new GraphPoint2d(point.x, point.y);
                _points.Add(graphPoint);
                
                if (color != null)
                {
                    _colorMap.Add(graphPoint, color.Value);
                }
            }
        }

        public void AddPath(Path2d path, Color? color = null)
        {
            foreach (var point in path.Points)
            {
                RecalculateMinMax(point.X, point.Y);
            }
                
            _paths.Add(path);
            if (color != null)
            {
                _colorMap.Add(path, color.Value);
            }
        }

        public void Clear()
        {
            _points.Clear();
            _paths.Clear();
            _colorMap.Clear();
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