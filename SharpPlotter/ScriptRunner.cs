using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Xna.Framework;

namespace SharpPlotter
{
    public class ScriptRunner
    {
        private readonly ConcurrentQueue<string> _scriptChanges = new ConcurrentQueue<string>();
        private readonly FileSystemWatcher _watcher;
        private readonly ScriptOptions _scriptOptions;
        private readonly ScriptGlobals _scriptGlobals;
        private readonly Canvas _canvas;

        public ScriptRunner(Canvas canvas, string scriptFile)
        {
            if (!Directory.Exists(Path.GetDirectoryName(scriptFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(scriptFile));
            }

            if (!File.Exists(scriptFile))
            {
                using (File.Create(scriptFile))
                {
                }
            }

            _canvas = canvas;
            _watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(scriptFile), 
                Filter = Path.GetFileName(scriptFile)
            };
            
            _watcher.Changed += ScriptFileChanged;
            _watcher.EnableRaisingEvents = true;
            
            _scriptOptions = ScriptOptions.Default
                .WithImports("System")
                .WithImports("System.Collections")
                .WithImports("System.Collections.Generic")
                .WithImports("System.Linq")
                .WithReferences(typeof(Enumerable).Assembly)
                .WithReferences(typeof(GraphItems).Assembly)
                .WithImports("SharpPlotter")
                .WithImports("SharpPlotter.Primitives")
                .WithReferences(typeof(Color).Assembly)
                .WithImports("Microsoft.Xna.Framework");

            _scriptGlobals = new ScriptGlobals
            {
                Canvas = canvas,
            };
        }

        public bool CheckForChanges()
        {
            var newScript = (string) null;
            while (_scriptChanges.TryDequeue(out var script))
            {
                // We only care about the latest change since last check
                newScript = script;
            }

            if (string.IsNullOrWhiteSpace(newScript))
            {
                return false;
            }

            try
            {
                _canvas.Clear();
                CSharpScript.RunAsync(newScript, _scriptOptions, _scriptGlobals).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error executing script: " + exception.Message);
                return false;
            }
            
            return true;
        }

        private void ScriptFileChanged(object sender, FileSystemEventArgs e)
        {
            var attemptCount = 0;
            while (true)
            {
                try
                {
                    using var file = File.Open(e.FullPath, FileMode.Open, FileAccess.Read);
                    using var reader = new StreamReader(file);

                    var content = reader.ReadToEnd();
                    _scriptChanges.Enqueue(content);
                    
                    break;
                }
                catch (IOException)
                {
                    if (attemptCount < 5)
                    {
                        attemptCount++;
                        Thread.Sleep(10);
                    }
                    else
                    {
                        // Failed too many times
                        throw;
                    }
                }
            }
            
            
        }
        
        // Must be public due to roslyn constraints
        public class ScriptGlobals
        {
            public Canvas Canvas { get; set; }
        }
    }
}