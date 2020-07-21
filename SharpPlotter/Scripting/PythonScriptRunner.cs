using System;
using System.Collections.Generic;
using System.Linq;
using IronPython.Hosting;
using IronPython.Runtime;
using IronPython.Runtime.Exceptions;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace SharpPlotter.Scripting
{
    public class PythonScriptRunner : IScriptRunner
    {
        private readonly ScriptEngine _scriptEngine;

        public string NewFileHeaderContent => @"
# Python script for SharpPlotter
#
# Points on the graph can be defined as a tuple value of x and y coordinates, such as `(1,2)`.  
#
# Colors can be defined by calling the `Color(r, g, b)` function with integer values between 0 and 255.  A set of 
#    predefined colors exist as properties on the `Color` object, such as `Color.Green`, `Color.CornflowerBlue`, etc..
#
# The graph can be drawn on by calling any of the following functions:
#    * `graph.Points()` can draw one or more isolated points (e.g. `graph.Points((1,2), (3,4), (4,0))`)
#    * `graph.Segments()` can draw line segments from one point to the next (e.g. `graph.Segments((1,2), (3,4), (4,0))`)
#    * `graph.Function()` can draw an unbounded function for each visible X value (e.g. `graph.Function(lambda x: x * x)`)
#    * `graph.Arrow()` can draw an arrow from a start point to an end point (e.g. `graph.Arrow((1,1), (2,2))`
#    * `graph.Log()` displays a text message to the screen (useful for debugging and non-graph output)
#
# All functions except `Log()` can optionally take a color value as the first parameter to change what color each
#    drawing is rendered as.
#
  
".TrimStart();

        public PythonScriptRunner()
        {
            _scriptEngine = Python.CreateEngine();
            _scriptEngine.Runtime.LoadAssembly(typeof(Color).Assembly);
        }
        
        public GraphedItems RunScript(string scriptContent)
        {
            var scope = _scriptEngine.CreateScope();
            var items = new GraphedItems();
            var drawMethods = new DrawMethods(items);
            scope.SetVariable("graph", drawMethods);

            scriptContent = $"from Microsoft.Xna.Framework import Color{Environment.NewLine}{scriptContent}";
            var script = _scriptEngine.CreateScriptSourceFromString(scriptContent);

            try
            {
                script.Execute(scope);
            }
            catch (Exception exception)
            {
                if (exception is SyntaxErrorException ||
                    exception is AttributeErrorException ||
                    exception is MissingMemberException)
                {
                    throw new ScriptException("Syntax Error", false, exception);
                }

                throw;
            }

            return items;
        }
        
        // ReSharper disable once MemberCanBePrivate.Global
        // If this is not public, then the scripting engine won't see the methods and a MissingMemberException
        // will occur when calling them.
        public class DrawMethods
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
            
            public void Arrow(Color color, object start, object end)
            {
                var (_, startPoints) = ParseObject(start);
                var (_, endPoints) = ParseObject(end);

                if (startPoints?.Any() != true || endPoints?.Any() != true)
                {
                    const string message = "Both start and end points need to be provided for arrows";
                    throw new InvalidOperationException(message);
                }
                
                _graphedItems.AddArrow(color, startPoints[0], endPoints[0]);
            }
            
            public void Arrow(object start, object end)
            {
                var (_, startPoints) = ParseObject(start);
                var (_, endPoints) = ParseObject(end);

                if (startPoints?.Any() != true || endPoints?.Any() != true)
                {
                    const string message = "Both start and end points need to be provided for arrows";
                    throw new InvalidOperationException(message);
                }
                
                _graphedItems.AddArrow(Color.White, startPoints[0], endPoints[0]);
            }

            private static (Color color, Point2d[] points) ParseObjects(params object[] objects)
            {
                // Due to python's dynamic nature, the incoming objects can be one of several type of objects
                var color = Color.White;
                var points = new List<Point2d>();
                foreach (var obj in objects)
                {
                    var (parsedColor, parsedPoints) = ParseObject(obj);
                    if (parsedColor != null)
                    {
                        color = parsedColor.Value;
                    }
                    else if (parsedPoints != null && parsedPoints.Any())
                    {
                        points.AddRange(parsedPoints);
                    }
                    else
                    {
                        const string message = "Invalid objects passed in, was not a color or point";
                        throw new InvalidOperationException(message);
                    }
                }

                return (color, points.ToArray());
            }

            private static (Color? color, Point2d[] points) ParseObject(object obj)
            {
                switch (obj)
                {
                    case null:
                        throw new PointConversionException("Cannot convert `null` to a point");

                    case Color passedInColor:
                        return (passedInColor, null);

                    case PythonTuple pythonTuple when pythonTuple.Count == 2 &&
                                                      (pythonTuple[0] is double || pythonTuple[0] is int ||
                                                       pythonTuple[0] is float) &&
                                                      (pythonTuple[1] is double || pythonTuple[1] is int ||
                                                       pythonTuple[1] is float):
                    {
                        var x = (float) Convert.ToDouble(pythonTuple[0]);
                        var y = (float) Convert.ToDouble(pythonTuple[1]);
                        
                        return (null, new[] {new Point2d(x, y)});
                    }

                    // Iron python passes in arrays as a generic List
                    case List list:
                        var (_, innerPoints) = ParseObjects(list.ToArray());
                        return (null, innerPoints);

                    // No known way to parse out the point
                    default:
                        var json = JsonConvert.SerializeObject(obj);
                        throw new PointConversionException($"No known way to convert '{json}' to a point");
                }
            }
        }
    }
}