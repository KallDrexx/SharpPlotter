// ReSharper disable UnusedMember.Global
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Xna.Framework;

namespace SharpPlotter.Scripting
{
    public class CSharpScriptRunner : IScriptRunner
    {
        private readonly ScriptOptions _scriptOptions;

        public string NewFileHeaderContent => @"
// C# script for SharpPlotter
//
// Points on the graph can be specified via a 2 item numeric tuple, e.g. `(1,2)` refers to x=1, y=2 on the graph.
// Colors are specified via the XNA Color structure, so a predefined set of colors can be utilized directly, for 
//    example `Color.Red`, or `Color.CornflowerBlue`.  Custom colors can be specified via the `new Color(r,g,b)`
//    constructor.
//
// The graph can be drawn on via the following methods:
//    * `Graph.Points()` draws one or more isolated points at the specified positions (e.g. `Graph.Points((1,2), (3,4))`)
//    * `Graph.Segments()` draws line segments from one point to the next (e.g. `Graph.Segments((1,2), (3,4), (4,0))`)
//    * `Graph.Function()` draws an unbounded function for each X graph value visible (e.g. `Graph.Function(x => x * x)`)
//    * `Graph.Arrow()` draws an arrow for a starting and ending point (e.g. `Graph.Arrow((1,1), (2,2))`)
//    * `Graph.Polygon()` draws a filled in polygon between 3 or more points (e.g. `Graph.Polygon((1,1), (2,2), (3,0))`)
//    * `Graph.Log()` will show a text message on the screen (can be helpful for debugging or non-graph values)
//
// All graphing functions except `Log()` can have the first parameter as a color value to change what color they are
//    drawn as.  The `Points()` and `Segments()` methods can be given an `Enumerable<(float, float)>` to be given
//    any number of points.
//
using System;
using System.Linq;

".TrimStart();

        public CSharpScriptRunner()
        {
            _scriptOptions = ScriptOptions.Default
                .WithImports("System")
                .WithImports("SharpPlotter")
                .WithImports("SharpPlotter.Primitives")
                .WithReferences(typeof(Color).Assembly)
                .WithImports("Microsoft.Xna.Framework");
        }
        
        public GraphedItems RunScript(string scriptContent)
        {
            var items = new GraphedItems();
            var globals = new ScriptGlobals {Graph = new DrawMethods(items)};

            try
            {
                CSharpScript.RunAsync(scriptContent, _scriptOptions, globals).GetAwaiter().GetResult();
            }
            catch (CompilationErrorException exception)
            {
                // Since this is a compiler error then we don't need the stack trace shown, as that's just
                // noise that won't help end users
                throw new ScriptException("Compile Error", false, exception);
            }

            return items;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        // Must be public for Roslyn to not throw inaccessible exceptions
        public class ScriptGlobals
        {
            public DrawMethods Graph { get; set; }
        }
        
        // ReSharper disable once MemberCanBePrivate.Global
        // Must be public for Roslyn to not throw inaccessible exceptions
        public class DrawMethods
        {
            private readonly GraphedItems _graphedItems;

            public DrawMethods(GraphedItems graphedItems)
            {
                _graphedItems = graphedItems;
            }

            public void Points(IEnumerable<(int x, int y)> points)
            {
                points ??= Array.Empty<(int, int)>();
                _graphedItems.AddPoints(Color.White, points.Select(p => new Point2d(p.x, p.y)));
            }
            
            public void Points(Color color, IEnumerable<(int x, int y)> points)
            {
                points ??= Array.Empty<(int, int)>();
                _graphedItems.AddPoints(color, points.Select(p => new Point2d(p.x, p.y)));
            }

            public void Points(params (int x, int y)[] points)
            {
                points ??= Array.Empty<(int, int)>();
                _graphedItems.AddPoints(Color.White, points.Select(p => new Point2d(p.x, p.y)));
            }
            
            public void Points(Color color, params (int x, int y)[] points)
            {
                points ??= Array.Empty<(int, int)>();
                _graphedItems.AddPoints(color, points.Select(p => new Point2d(p.x, p.y)));
            }

            public void Points(IEnumerable<(double x, double y)> points)
            {
                points ??= Array.Empty<(double, double)>();
                _graphedItems.AddPoints(Color.White, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }

            public void Points(Color color, IEnumerable<(double x, double y)> points)
            {
                points ??= Array.Empty<(double, double)>();
                _graphedItems.AddPoints(color, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }

            public void Points(params (double x, double y)[] points)
            {
                points ??= Array.Empty<(double, double)>();
                _graphedItems.AddPoints(Color.White, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }

            public void Points(Color color, params (double x, double y)[] points)
            {
                points ??= Array.Empty<(double, double)>();
                _graphedItems.AddPoints(color, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }

            public void Points(IEnumerable<(float x, float y)> points)
            {
                points ??= Array.Empty<(float, float)>();
                _graphedItems.AddPoints(Color.White, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }

            public void Points(Color color, IEnumerable<(float x, float y)> points)
            {
                points ??= Array.Empty<(float, float)>();
                _graphedItems.AddPoints(color, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }

            public void Points(params (float x, float y)[] points)
            {
                points ??= Array.Empty<(float, float)>();
                _graphedItems.AddPoints(Color.White, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }

            public void Points(Color color, params (float x, float y)[] points)
            {
                points ??= Array.Empty<(float, float)>();
                _graphedItems.AddPoints(color, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }
            
            public void Segments(IEnumerable<(int x, int y)> points)
            {
                points ??= Array.Empty<(int, int)>();
                _graphedItems.AddSegments(Color.White, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }
            
            public void Segments(Color color, IEnumerable<(int x, int y)> points)
            {
                points ??= Array.Empty<(int, int)>();
                _graphedItems.AddSegments(color, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }

            public void Segments(params (int x, int y)[] points)
            {
                points ??= Array.Empty<(int, int)>();
                _graphedItems.AddSegments(Color.White, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }
            
            public void Segments(Color color, params (int x, int y)[] points)
            {
                points ??= Array.Empty<(int, int)>();
                _graphedItems.AddSegments(color, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }

            public void Segments(IEnumerable<(double x, double y)> points)
            {
                points ??= Array.Empty<(double, double)>();
                _graphedItems.AddSegments(Color.White, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }
            
            public void Segments(Color color, IEnumerable<(double x, double y)> points)
            {
                points ??= Array.Empty<(double, double)>();
                _graphedItems.AddSegments(color, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }

            public void Segments(params (double x, double y)[] points)
            {
                points ??= Array.Empty<(double, double)>();
                _graphedItems.AddSegments(Color.White, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }
            
            public void Segments(Color color, params (double x, double y)[] points)
            {
                points ??= Array.Empty<(double, double)>();
                _graphedItems.AddSegments(color, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }

            public void Segments(IEnumerable<(float x, float y)> points)
            {
                points ??= Array.Empty<(float, float)>();
                _graphedItems.AddSegments(Color.White, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }
            
            public void Segments(Color color, IEnumerable<(float x, float y)> points)
            {
                points ??= Array.Empty<(float, float)>();
                _graphedItems.AddSegments(color, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }

            public void Segments(params (float x, float y)[] points)
            {
                points ??= Array.Empty<(float, float)>();
                _graphedItems.AddSegments(Color.White, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }
            
            public void Segments(Color color, params (float x, float y)[] points)
            {
                points ??= Array.Empty<(float, float)>();
                _graphedItems.AddSegments(color, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }

            public void Polygon(IEnumerable<(int x, int y)> points)
            {
                points ??= Array.Empty<(int, int)>();
                _graphedItems.AddPolygon(Color.White, points.Select(p => new Point2d(p.x, p.y)));
            }

            public void Polygon(Color color, IEnumerable<(int x, int y)> points)
            {
                points ??= Array.Empty<(int, int)>();
                _graphedItems.AddPolygon(color, points.Select(p => new Point2d(p.x, p.y)));
            }

            public void Polygon(params (int x, int y)[] points)
            {
                points ??= Array.Empty<(int, int)>();
                _graphedItems.AddPolygon(Color.White, points.Select(p => new Point2d(p.x, p.y)));
            }

            public void Polygon(Color color, params (int x, int y)[] points)
            {
                points ??= Array.Empty<(int, int)>();
                _graphedItems.AddPolygon(color, points.Select(p => new Point2d(p.x, p.y)));
            }

            public void Polygon(IEnumerable<(float x, float y)> points)
            {
                points ??= Array.Empty<(float, float)>();
                _graphedItems.AddPolygon(Color.White, points.Select(p => new Point2d(p.x, p.y)));
            }

            public void Polygon(Color color, IEnumerable<(float x, float y)> points)
            {
                points ??= Array.Empty<(float, float)>();
                _graphedItems.AddPolygon(color, points.Select(p => new Point2d(p.x, p.y)));
            }

            public void Polygon(params (float x, float y)[] points)
            {
                points ??= Array.Empty<(float, float)>();
                _graphedItems.AddPolygon(Color.White, points.Select(p => new Point2d(p.x, p.y)));
            }

            public void Polygon(Color color, params (float x, float y)[] points)
            {
                points ??= Array.Empty<(float, float)>();
                _graphedItems.AddPolygon(color, points.Select(p => new Point2d(p.x, p.y)));
            }

            public void Polygon(IEnumerable<(double x, double y)> points)
            {
                points ??= Array.Empty<(double, double)>();
                _graphedItems.AddPolygon(Color.White, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }

            public void Polygon(Color color, IEnumerable<(double x, double y)> points)
            {
                points ??= Array.Empty<(double, double)>();
                _graphedItems.AddPolygon(color, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }

            public void Polygon(params (double x, double y)[] points)
            {
                points ??= Array.Empty<(double, double)>();
                _graphedItems.AddPolygon(Color.White, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }

            public void Polygon(Color color, params (double x, double y)[] points)
            {
                points ??= Array.Empty<(double, double)>();
                _graphedItems.AddPolygon(color, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }

            public void Log(string message)
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    _graphedItems.Messages.Enqueue(message);
                }
            }

            public void Function(Func<float, float> function)
            {
                _graphedItems.AddFunction(Color.White, function);
            }

            public void Function(Color color, Func<float, float> function)
            {
                _graphedItems.AddFunction(color, function);
            }

            public void Arrow(Color color, (double x, double y) start, (double x, double y) end)
            {
                _graphedItems.AddArrow(color, 
                    new Point2d((float) start.x, (float) start.y),
                    new Point2d((float) end.x, (float) end.y));
            }
            
            public void Arrow((double x, double y) start, (double x, double y) end)
            {
                _graphedItems.AddArrow(Color.White, 
                    new Point2d((float) start.x, (float) start.y),
                    new Point2d((float) end.x, (float) end.y));
            }
        }
    }
}