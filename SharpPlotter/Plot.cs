using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using SharpPlotter.Rendering;

namespace SharpPlotter
{
    public class Plot
    {
        private readonly List<RenderedPoint> _points = new List<RenderedPoint>();
        private readonly List<RenderedSegment> _segments = new List<RenderedSegment>();

        public IReadOnlyList<RenderedPoint> PointsToRender => _points;
        public IReadOnlyList<RenderedSegment> SegmentsToRender => _segments;

        /// <summary>
        /// Clear all items from the graph
        /// </summary>
        public void Clear()
        {
            _points.Clear();
            _segments.Clear();
        }

        /// <summary>
        /// Add white points via tuples
        /// </summary>
        public void Points(params (float x, float y)[] points)
        {
            Points(Color.White, points);
        }

        /// <summary>
        /// Add colored points via tuples
        /// </summary>
        public void Points(Color color, params (float x, float y)[] points)
        {
            points ??= Array.Empty<(float, float)>();
            foreach (var (x, y) in points)
            {
                _points.Add(new RenderedPoint(new Point2d(x, y), Color.White));
            }
        }

        /// <summary>
        /// Add white points via a 2 dimensional array.  Each inner array is expected to be 2 floats.  For example,
        /// adding the points 1,1 and 2,2 would be [[1,1], [2,2]].  This should make interacting via javascript easier.
        /// </summary>
        public void Points(params float[][] points)
        {
            Points(Color.White, points);
        }
        
        /// <summary>
        /// Add white points via a 2 dimensional array.  Each inner array is expected to be 2 floats.  For example,
        /// adding the points 1,1 and 2,2 would be [[1,1], [2,2]].  This should make interacting via javascript easier.
        /// </summary>
        public void Points(Color color, params float[][] points)
        {
            points ??= Array.Empty<float[]>();
            for (var index = 0; index < points.Length; index++)
            {
                var point = points[index] ?? Array.Empty<float>();
                if (point.Length != 2)
                {
                    var message = $"Expected point {index + 1} to have 2 items, but {point.Length} were found";
                    throw new ArgumentException(message);
                }
                
                _points.Add(new RenderedPoint(new Point2d(point[0], point[1]), color));
            }
        }
        
        /// <summary>
        /// Add continuous white line segments via tuples of points
        /// </summary>
        public void Segments(params (float x, float y)[] points)
        {
            Segments(Color.White, points);
        }

        /// <summary>
        /// Add continuous colored line segments via tuples of points
        /// </summary>
        public void Segments(Color color, params (float x, float y)[] points)
        {
            points ??= Array.Empty<(float, float)>();
            if (points.Length < 2)
            {
                return; // Can't draw a line segment without at least two points
            }
            
            var lastPoint = ((float x, float y)?) null;
            foreach (var point in points)
            {
                if (lastPoint != null)
                {
                    _segments.Add(new RenderedSegment(lastPoint.Value, point, color));
                }

                lastPoint = point;
            }
        }

        /// <summary>
        /// Add white segments via a 2 dimensional array of points.  Each inner array is expected to be 2 floats.
        /// For example,  adding the points 1,1 and 2,2 would be [[1,1], [2,2]].  This should make method calls from
        /// javascript easier
        /// </summary>
        public void Segments(params float[][] points)
        {
            Segments(Color.White, points);
        }
        
        /// <summary>
        /// Add colored segments via a 2 dimensional array of points.  Each inner array is expected to be 2 floats.
        /// For example,  adding the points 1,1 and 2,2 would be [[1,1], [2,2]].  This should make method calls from
        /// javascript easier
        /// </summary>
        public void Segments(Color color, params float[][] points)
        {
            points ??= Array.Empty<float[]>();

            var lastPoint = (float[]) null;
            for (var index = 0; index < points.Length; index++)
            {
                var point = points[index] ?? Array.Empty<float>();
                if (point.Length != 2)
                {
                    var message = $"Expected point {index + 1} to have 2 items, but {point.Length} were found";
                    throw new ArgumentException(message);
                }

                if (lastPoint != null)
                {
                    var start = new Point2d(lastPoint[0], lastPoint[1]);
                    var end = new Point2d(point[0], point[1]);
                    _segments.Add(new RenderedSegment(start, end, color));
                }
                
                lastPoint = point;
            }
        }
    }
}