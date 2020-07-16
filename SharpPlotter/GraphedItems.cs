using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using SharpPlotter.Rendering;

namespace SharpPlotter
{
    public class GraphedItems
    {
        private readonly List<RenderedPoint> _points = new List<RenderedPoint>();
        private readonly List<RenderedSegment> _segments = new List<RenderedSegment>();
        
        /// <summary>
        /// The smallest X and Y values across all graphed items
        /// </summary>
        public Point2d? MinCoordinates { get; private set; }
        
        /// <summary>
        /// The largest X and Y values across all graphed items
        /// </summary>
        public Point2d? MaxCoordinates { get; private set; }

        public GraphedItems()
        {
            // Always start with true, as if this is a new object then obviously something has changed.
            ItemsChangedSinceLastRender = true;
        }
        
        /// <summary>
        /// Returns true if any changes have been made to any collection of graph-able items.  This resets any time
        /// the list of items to render is retrieved.
        /// </summary>
        public bool ItemsChangedSinceLastRender { get; private set; }

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
                _points.Add(new RenderedPoint(new Point2d(x, y), color));
            }
            
            GraphItemsUpdated();
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
            
            GraphItemsUpdated();
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
            
            GraphItemsUpdated();
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
            
            GraphItemsUpdated();
        }
        
        /// <summary>
        /// Provides the list of items that should be rendered.  This will reset `ItemsChangedSinceLastRender`
        /// </summary>
        internal ItemsToRender GetItemsToRender()
        {
            ItemsChangedSinceLastRender = false;
            return new ItemsToRender(_points, _segments);
        }

        private void GraphItemsUpdated()
        {
            ItemsChangedSinceLastRender = true;
            
            var allCoordinates = _points.Select(x => x.Point)
                .Union(_segments.Select(x => x.End))
                .Union(_segments.Select(x => x.End))
                .Distinct()
                .ToArray();

            if (allCoordinates.Any())
            {
                float minX = float.MaxValue,
                    minY = float.MaxValue,
                    maxX = float.MinValue,
                    maxY = float.MinValue;

                foreach (var coordinate in allCoordinates)
                {
                    if (minX > coordinate.X) minX = coordinate.X;
                    if (minY > coordinate.Y) minY = coordinate.Y;
                    if (maxX < coordinate.X) maxX = coordinate.X;
                    if (maxY < coordinate.Y) maxY = coordinate.Y;
                }
                
                MinCoordinates = new Point2d(minX, minY);
                MaxCoordinates = new Point2d(maxX, maxY);
            }
            else
            {
                MinCoordinates = null;
                MaxCoordinates = null;
            }
        }
    }
}