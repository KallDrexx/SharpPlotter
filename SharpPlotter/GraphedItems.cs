using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using IronPython.Runtime;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using SharpPlotter.Rendering;

namespace SharpPlotter
{
    public class GraphedItems
    {
        private readonly List<RenderedPoint> _points = new List<RenderedPoint>();
        private readonly List<RenderedSegment> _segments = new List<RenderedSegment>();
        private readonly List<RenderedFunction> _functions = new List<RenderedFunction>();
        private readonly List<RenderedArrow> _arrows = new List<RenderedArrow>();
        private readonly List<RenderedPolygon> _polygons = new List<RenderedPolygon>();

        public Queue<string> Messages { get; } = new Queue<string>();
        
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
        /// Adds points to the graph set to the specified color
        /// </summary>
        public void AddPoints(Color color, IEnumerable<Point2d> points)
        {
            points ??= Array.Empty<Point2d>();
            
            _points.AddRange(points.Select(x => new RenderedPoint(x, color)));
            GraphItemsUpdated();
        }

        /// <summary>
        /// Adds line segments to the graph from each point specified to the next point.  
        /// </summary>
        public void AddSegments(Color color, IEnumerable<Point2d> points)
        {
            points ??= Array.Empty<Point2d>();

            var lastPoint = (Point2d?) null;
            foreach (var point in points)
            {
                if (lastPoint != null)
                {
                    _segments.Add(new RenderedSegment(lastPoint.Value, point, color));
                }

                lastPoint = point;
            };
            
            GraphItemsUpdated();
        }

        /// <summary>
        /// Adds an unbounded function that will be rendered, with values calculated on demand
        /// </summary>
        public void AddFunction(Color color, Func<float, float> function)
        {
            if (function == null) throw new ArgumentNullException(nameof(function));
            
            _functions.Add(new RenderedFunction(color, function));
        }

        /// <summary>
        /// Adds a line segment with a pointer at the end from the starting point to and ending point
        /// </summary>
        public void AddArrow(Color color, Point2d start, Point2d end)
        {
            _arrows.Add(new RenderedArrow(start, end, color));
            GraphItemsUpdated();
        }

        public void AddPolygon(Color color, IEnumerable<Point2d> points)
        {
            points ??= Array.Empty<Point2d>();
            
            var polygon = new RenderedPolygon(color, points);
            _polygons.Add(polygon);
            GraphItemsUpdated();
        }
        
        /// <summary>
        /// Provides the list of items that should be rendered.  This will reset `ItemsChangedSinceLastRender`
        /// </summary>
        internal ItemsToRender GetItemsToRender()
        {
            ItemsChangedSinceLastRender = false;
            return new ItemsToRender(_points, _segments, _functions, _arrows, _polygons);
        }

        private void GraphItemsUpdated()
        {
            ItemsChangedSinceLastRender = true;
            
            var allCoordinates = _points.Select(x => x.Point)
                .Union(_segments.Select(x => x.Start))
                .Union(_segments.Select(x => x.End))
                .Union(_arrows.Select(x => x.Start))
                .Union(_arrows.Select(x => x.End))
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