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

        private Point2d ConvertObjectToPoint2d(object obj)
        {
            if (obj == null)
            {
                throw new PointConversionException("Cannot convert `null` to a valid point");
            }

            if (obj is Point2d point)
            {
                return point;
            }

            if (obj is ValueTuple<int, int> intTuple)
            {
                return new Point2d(intTuple.Item1, intTuple.Item2);
            }

            if (obj is ValueTuple<double, double> doubleTuple)
            {
                return new Point2d((float) doubleTuple.Item1, (float) doubleTuple.Item2);
            }

            if (obj is ValueTuple<float, float> floatTuple)
            {
                return new Point2d(floatTuple.Item1, floatTuple.Item2);
            }

            if (obj is object[] objArray && 
                objArray.Length == 2 &&
                (objArray[0] is float || objArray[0] is int || objArray[0] is double) &&
                (objArray[1] is float || objArray[1] is int || objArray[1] is double))
            {
                var x = (float) Convert.ToDouble(objArray[0]);
                var y = (float) Convert.ToDouble(objArray[1]);
                
                return new Point2d(x, y);
            }

            if (obj is PythonTuple pythonTuple &&
                pythonTuple.Count == 2 &&
                (pythonTuple[0] is float || pythonTuple[0] is int || pythonTuple[0] is double) &&
                (pythonTuple[1] is float || pythonTuple[1] is int || pythonTuple[1] is double))
            {
                var x = (float) Convert.ToDouble(pythonTuple[0]);
                var y = (float) Convert.ToDouble(pythonTuple[1]);
                
                return new Point2d(x, y);
            }

            if (obj is ExpandoObject expandoObject)
            {
                var x = (float?) null;
                var y = (float?) null;
                
                foreach (var (key, value) in expandoObject)
                {
                    if (key.Equals("x", StringComparison.OrdinalIgnoreCase))
                    {
                        x = value switch
                        {
                            int intVal => intVal,
                            float floatVal => floatVal,
                            double doubleVal => (float) doubleVal,
                            _ => (float?) null
                        };
                    }
                    
                    if (key.Equals("y", StringComparison.OrdinalIgnoreCase))
                    {
                        y = value switch
                        {
                            int intVal => intVal,
                            float floatVal => floatVal,
                            double doubleVal => (float) doubleVal,
                            _ => (float?) null
                        };
                    }
                }

                if (x != null && y != null)
                {
                    return new Point2d(x.Value, y.Value);
                }
            }

            var json = JsonConvert.SerializeObject(obj);
            throw new PointConversionException($"Cannot convert object to a point: '{json}'");
        }
    }
}