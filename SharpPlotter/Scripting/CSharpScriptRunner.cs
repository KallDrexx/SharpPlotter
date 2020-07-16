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
            var globals = new ScriptGlobals {Graph = items};

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
        
        // Must be public due to roslyn constraints
        public class ScriptGlobals
        {
            public GraphedItems Graph { get; set; }
        }
    }
}