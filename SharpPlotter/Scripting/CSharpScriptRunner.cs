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
            var globals = new ScriptGlobals {Plot = items};
            CSharpScript.RunAsync(scriptContent, _scriptOptions, globals).GetAwaiter().GetResult();

            return items;
        }
        
        // Must be public due to roslyn constraints
        public class ScriptGlobals
        {
            public GraphedItems Plot { get; set; }
        }
    }
}