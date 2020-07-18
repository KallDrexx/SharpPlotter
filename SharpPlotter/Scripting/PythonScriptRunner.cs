using System;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Microsoft.Xna.Framework;

namespace SharpPlotter.Scripting
{
    public class PythonScriptRunner : IScriptRunner
    {
        private readonly ScriptEngine _scriptEngine;

        public PythonScriptRunner()
        {
            _scriptEngine = Python.CreateEngine();
            _scriptEngine.Runtime.LoadAssembly(typeof(Color).Assembly);
        }
        
        public GraphedItems RunScript(string scriptContent)
        {
            var items = new GraphedItems();
            var scope = _scriptEngine.CreateScope();
            scope.SetVariable("graph", items);

            scriptContent = $"from Microsoft.Xna.Framework import Color{Environment.NewLine}{scriptContent}";
            var script = _scriptEngine.CreateScriptSourceFromString(scriptContent);

            try
            {
                script.Execute(scope);
            }
            catch (SyntaxErrorException exception)
            {
                throw new ScriptException("Syntax Error", false, exception);
            }

            return items;
        }
    }
}