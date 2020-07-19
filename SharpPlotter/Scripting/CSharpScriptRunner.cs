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
// The graph can be drawn to by calling the `Graph.Points()` to draw isolated points, or `Graph.Segments()` to draw
//    a set of line segments.  Both functions take in one or more point tuples (specified individually or contained
//    within an `IEnumerable<(x,y)>`), and the first parameter may be a `Color` to specify the color for each call.
//
// Examples:
//    * `Graph.Points((1,2))` draws a single white point at x=1, y=2
//    * `Graph.Points(Color.Red, (3,2), (4,1))` draws 2 red points at x=3, y=2, and x=4, y=1
//    * `Graph.Segments((1,1), (2,2), (3,3))` draws 2 white line segments, one from 1,1 to 2,2, and a 2nd from 2,2 to 3,3
//    * `Graph.Segments(Color.Green, anArrayOfPoints)` draws green lines from each point in the array to the next.
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
        }
    }
}