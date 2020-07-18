using System;
using Esprima;
using Jint;
using Jint.Runtime;
using Microsoft.Xna.Framework;

namespace SharpPlotter.Scripting
{
    public class JavascriptRunner : IScriptRunner
    {
        public GraphedItems RunScript(string scriptContent)
        {
            var items = new GraphedItems();

            try
            {
                new Engine(cfg => { cfg.AllowClr(typeof(Color).Assembly); })

                    // Allow use of the `color` struct, so `color.Red` is accessible
                    .Execute(
                        @"var XnaFramework = importNamespace('Microsoft.Xna.Framework');var color = XnaFramework.Color;")

                    // Helper to make defining points easy
                    .SetValue("p", new Func<double, double, Point2d>((x, y) => new Point2d((float) x, (float) y)))
                    .SetValue("graph", items)
                    .Execute(scriptContent, new ParserOptions { });
            }
            catch (JavaScriptException exception)
            {
                throw new ScriptException("Error", false, exception);
            }

            return items;
        }
    }
}