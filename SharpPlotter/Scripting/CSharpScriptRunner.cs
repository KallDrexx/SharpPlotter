using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Xna.Framework;

namespace SharpPlotter.Scripting
{
    public class CSharpScriptRunner : IScriptRunner
    {
        private readonly ScriptOptions _scriptOptions;

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

            public void Points(params (double x, double y)[] points)
            {
                points ??= Array.Empty<(double, double)>();
                _graphedItems.AddPoints(Color.White, points.Select(p => new Point2d((float) p.x, (float) p.y)));
            }

            public void Points(Color color, (double x, double y)[] points)
            {
                points ??= Array.Empty<(double, double)>();
                _graphedItems.AddPoints(color, points.Select(p => new Point2d((float) p.x, (float) p.y)));
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