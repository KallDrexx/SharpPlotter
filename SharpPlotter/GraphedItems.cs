using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
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
        /// Add points to the graph. This allows for multiple combinations of items to be passed in.  This is needed to
        /// facilitate some scripting engines, such as Javascript.  Each point may be represented via
        /// * Float, double, or int 2 item tuples - [(1,2)]
        /// * 2 element array of int, double, or float where the first item is X and the second is Y - [[1,2]]
        /// * Point2d
        /// * An object with an X and Y property - [{x:1,y:2}]
        ///
        /// If the first object in the collection is an XNA Color value, than that color will be applied to every
        /// point that gets rendered, otherwise all points will be white.
        ///
        /// Any other value will thrown an exception
        /// </summary>
        public void Points(params object[] points)
        {
            points ??= Array.Empty<object>();
            if (!points.Any())
            {
                return;
            }

            var color = Color.White;
            if (points[0] is Color passedInColor)
            {
                color = passedInColor;
                
                // With some scripting engines, like JInt, if both a color and an array is passed in than the array
                // will be passed in as it's own `object[]` in `points[1]`.  So we need to disambiguate that 
                // if possible.  However, we do need to be careful that `points[1]` isn't an `object[]` due to 
                // a function call like `Points(Color.Red, [1, 2])` which is valid.
                if (points.Length > 1 && points[1] is object[] objArray)
                {
                    if (objArray.Length == 2 && (objArray[0] is double || objArray[0] is int || objArray[0] is float))
                    {
                        // Do not expand this
                        points = points.Skip(1).ToArray();
                    }
                    else
                    {
                        points = objArray;
                    }
                }
                else
                {
                    points = points.Skip(1).ToArray();
                }
            }

            foreach (var pointObject in points)
            {
                var point = ConvertObjectToPoint2d(pointObject);
                _points.Add(new RenderedPoint(point, color));
            }
            
            GraphItemsUpdated();
        }

        /// <summary>
        /// Add segments to the graph.  This allows for multiple combinations of items to be passed in.  This is
        /// needed to facilitate some scripting engines, such as Javascript.  Each point may be represented via
        /// * Float, double, or int 2 item tuples - [(1,2)]
        /// * 2 element array of int, double, or float where the first item is X and the second is Y - [[1,2]]
        /// * Point2d
        /// * An object with an X and Y property - [{x:1,y:2}]
        ///
        /// Any other value will thrown an exception
        /// </summary>
        public void Segments(params object[] points)
        {
            points ??= Array.Empty<object>();
            if (!points.Any())
            {
                return;
            }

            var color = Color.White;
            if (points[0] is Color passedInColor)
            {
                color = passedInColor;
                
                // With some scripting engines, like JInt, if both a color and an array is passed in than the array
                // will be passed in as it's own `object[]` in `points[1]`.  So we need to disambiguate that 
                // if possible.  However, we do need to be careful that `points[1]` isn't an `object[]` due to 
                // a function call like `Points(Color.Red, [1, 2])` which is valid.
                if (points.Length > 1 && points[1] is object[] objArray)
                {
                    if (objArray.Length == 2 && (objArray[0] is double || objArray[0] is int || objArray[0] is float))
                    {
                        // Do not expand this
                        points = points.Skip(1).ToArray();
                    }
                    else
                    {
                        points = objArray;
                    }
                }
                else
                {
                    points = points.Skip(1).ToArray();
                }
            }
            
            var lastPoint = (Point2d?) null;
            for (var index = 0; index < points.Length; index++)
            {
                var point = ConvertObjectToPoint2d(points[index]);

                if (lastPoint != null)
                {
                    var start = lastPoint.Value;
                    var end = point;
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

        private Point2d ConvertObjectToPoint2d(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentException("Cannot convert `null` to a valid point");
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
                            _ => null
                        };
                    }
                    
                    if (key.Equals("y", StringComparison.OrdinalIgnoreCase))
                    {
                        y = value switch
                        {
                            int intVal => intVal,
                            float floatVal => floatVal,
                            double doubleVal => (float) doubleVal,
                            _ => null
                        };
                    }
                }

                if (x != null && y != null)
                {
                    return new Point2d(x.Value, y.Value);
                }
            }

            var json = JsonConvert.SerializeObject(obj);
            throw new ArgumentException($"Cannot convert object to a point: '{json}'");
        }
    }
}