using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Esprima;
using IronPython.Runtime;
using Jint;
using Jint.Runtime;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace SharpPlotter.Scripting
{
    public class JavascriptRunner : IScriptRunner
    {
        private delegate void CallPoints(params object[] values);

        public string NewFileHeaderContent => @"
// Javascript script for SharpPlotter
// Points on the graph can be specified in several ways:
//    * An object with `x` and `y` properties (e.g. `{x:1,y:2}`)
//    * An array with exactly 2 numeric values representing x and y coordinates (e.g. `[1,2]`)
//    * Calling the `p()` function with 2 values (e.g. `p(1,2)`)
//
// Color values can be used by calling the constructor on the `Color` type with r, g, and b values passed in as values
//    between 0 and 255.  Some ready made defaults are available to use off the `Color` type, such as `Color.Red`, 
//    `Color.Magenta`, etc...
//
// The graph can be drawn on with the following functions
//    * `graph.Points()` allows drawing one or more isolated points (e.g. `graph.Points(p(1,2), p(3,4));`)
//    * `graph.Segments()` allows drawing line segments from one point to the next (e.g. `graph.Segments(p(1,2), p(3,4), p(4,0));`)
//    * `graph.Function()` allows drawing an unbounded function for each x value (e.g. `graph.Function(function(x) { return x*x;});`)
//    * `graph.Log()` allows displaying a text message on the screen (can be used for debugging).
//
// All graph functions except `Log()` can have a first parameter being a color value to change the color of the 
//    drawn data.    

".TrimStart();
        
        public GraphedItems RunScript(string scriptContent)
        {
            var items = new GraphedItems();
            var drawMethods = new DrawMethods(items);

            try
            {
                new Engine(cfg => { cfg.AllowClr(typeof(Color).Assembly); })

                    // Allow use of the `color` struct, so `color.Red` is accessible
                    .Execute(
                        @"var SharpPlotterHelpers = importNamespace('Microsoft.Xna.Framework');var Color = SharpPlotterHelpers.Color;")

                    // Helper to make defining points easy
                    .SetValue("p", new Func<double, double, Point2d>((x, y) => new Point2d((float) x, (float) y)))
                    .SetValue("graph", drawMethods)
                    .Execute(scriptContent, new ParserOptions { });
            }
            catch (JavaScriptException exception)
            {
                throw new ScriptException("Error", false, exception);
            }

            return items;
        }

        private class DrawMethods
        {
            private readonly GraphedItems _graphedItems;

            public DrawMethods(GraphedItems graphedItems)
            {
                _graphedItems = graphedItems;
            }

            public void Points(params object[] objects)
            {
                objects ??= Array.Empty<object>();
                var (color, points) = ParseObjects(objects);
                
                _graphedItems.AddPoints(color, points);
            }

            public void Segments(params object[] objects)
            {
                objects ??= Array.Empty<object>();
                var (color, points) = ParseObjects(objects);
                
                _graphedItems.AddSegments(color, points);
            }

            public void Log(string message)
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    _graphedItems.Messages.Enqueue(message);
                }
            }

            public void Function(Color color, Func<float, float> function)
            {
                _graphedItems.AddFunction(color, function);
            }
            
            public void Function(Func<float, float> function)
            {
                _graphedItems.AddFunction(Color.White, function);
            }

            private static (Color color, Point2d[] points) ParseObjects(params object[] objects)
            {
                // JInt will always call this as an array of objects, so we'll have to manually parse each argument out
                
                // If only 2 objects were passed in, and each is a number, assume that a single [x,y] pair was passed in
                if (objects.Length == 2 &&
                    (objects[0] is float || objects[0] is int || objects[0] is double) &&
                    (objects[1] is float || objects[1] is int || objects[1] is double))
                {
                    var x = (float) Convert.ToDouble(objects[0]);
                    var y = (float) Convert.ToDouble(objects[1]);

                    return (Color.White, new[] {new Point2d(x, y)});
                }
                
                var color = Color.White;
                var points = new List<Point2d>();
                foreach (var obj in objects)
                {
                    switch (obj)
                    {
                        case null:
                            throw new PointConversionException("Cannot convert `null` to a point");
                        
                        case Color passedInColor:
                            color = passedInColor;
                            break;
                        
                        // Check if an array of 2 points were passed in, i.e. [1,2] for x=1,y=2
                        case object[] objArray when objArray.Length == 2 && 
                                                    (objArray[0] is float || objArray[0] is int || objArray[0] is double) &&
                                                    (objArray[1] is float || objArray[1] is int || objArray[1] is double):
                        {
                            var x = (float) Convert.ToDouble(objArray[0]);
                            var y = (float) Convert.ToDouble(objArray[1]);

                            points.Add(new Point2d(x, y));
                            break;
                        }
                        
                        // If a `Color` was passed in before the array of points, then the array of points will be its own
                        // object array.  Thus we need to parse the inner array 
                        case object[] objArray2:
                            var (_, innerPoints) = ParseObjects(objArray2);
                            points.AddRange(innerPoints);
                            break;
                        
                        // If using the custom `p()` function then we will have actual Point2d objects
                        case Point2d point:
                            points.Add(point);
                            break;
                        
                        // Check if this is a javascript object with an x and y property, i.e. {x:1,y:2}
                        case ExpandoObject expandoObject when expandoObject.Any(x => x.Key.Equals("x", StringComparison.OrdinalIgnoreCase)) && expandoObject.Any(x => x.Key.Equals("y", StringComparison.OrdinalIgnoreCase)):
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

                            points.Add(new Point2d(x.Value, y.Value));
                            break;
                        }
                        
                        // No known way to parse out the point
                        default:
                            var json = JsonConvert.SerializeObject(obj);
                            throw new PointConversionException($"No known way to convert '{json}' to a point");
                    }
                }

                return (color, points.ToArray());
            }
        }
    }
}