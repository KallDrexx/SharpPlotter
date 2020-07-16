using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SharpPlotter.Scripting
{
    public class ScriptManager
    {
        private readonly ConcurrentQueue<string> _waitingScriptContent = new ConcurrentQueue<string>();
        private readonly AppSettings _appSettings;
        private readonly OnScreenLogger _onScreenLogger;
        private readonly FileSystemWatcher _fileSystemWatcher;

        private IScriptRunner _scriptRunner;

        public string CurrentFileName { get; private set; }
        public ScriptLanguage? CurrentLanguage { get; private set; }

        public ScriptManager(AppSettings appSettings, OnScreenLogger onScreenLogger)
        {
            _appSettings = appSettings;
            _onScreenLogger = onScreenLogger;
            _fileSystemWatcher = new FileSystemWatcher();
            
            _fileSystemWatcher.Changed += FileSystemWatcherOnChanged;
        }

        /// <summary>
        /// Creates a new script file. 
        /// </summary>
        public void CreateNewScript(string fileName, ScriptLanguage? language)
        {
            if (!Directory.Exists(_appSettings.ScriptFolderPath))
            {
                Directory.CreateDirectory(_appSettings.ScriptFolderPath);
            }

            var expectedExtension = language switch
            {
                ScriptLanguage.CSharp => ".cs",
                null => throw new InvalidOperationException($"A language is required"),
                _ => throw new NotSupportedException($"Language '{language}' is not supported")
            };

            if (Path.GetExtension(fileName) != expectedExtension)
            {
                fileName += expectedExtension;
            }

            var fullPath = Path.Combine(_appSettings.ScriptFolderPath, fileName);
            if (File.Exists(fullPath))
            {
                throw new InvalidOperationException($"Cannot create file `{fullPath}' as it already exists");
            }
            
            File.WriteAllText(fullPath, "");

            CurrentFileName = fileName;
            CurrentLanguage = language;
            
            _appSettings.AddOpenedFileName(fileName);
            OpenTextEditorCurrentFile();
            SetupScriptExecution();
        }

        /// <summary>
        /// Open the specified file.  This assumes the file exists in the scripts folder.
        /// </summary>
        public void OpenExistingScript(string fileName)
        {
            var fullPath = Path.Combine(_appSettings.ScriptFolderPath, fileName);
            var extension = Path.GetExtension(fileName);

            var language = extension?.ToLower() switch
            {
                ".cs" => ScriptLanguage.CSharp,
                _ => throw new NotSupportedException($"No scripting language could be found for extension '{extension}'")
            };

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"The file '{fileName}' does not exist in the script directory");
            }
            
            CurrentLanguage = language;
            CurrentFileName = fileName;
            
            _appSettings.AddOpenedFileName(fileName);
            OpenTextEditorCurrentFile();
            SetupScriptExecution();
        }

        public GraphedItems CheckForNewGraphedItems()
        {
            if (!_waitingScriptContent.Any())
            {
                return null;
            }
            
            // We only care about the latest changes waiting to be run
            var scriptContent = (string) null;
            while (_waitingScriptContent.TryDequeue(out var content))
            {
                // We want to make sure only to rewrite `scriptContent` if the dequeue was successful, if we try
                // to use `content` below it will always be null, since the last time this gets called it will
                // always return null.
                scriptContent = content;
            }

            try
            {
                return _scriptRunner.RunScript(scriptContent);
            }
            catch (ScriptException exception)
            {
                var content = "Failed to run script:\n\n";
                
                if (exception.ShowStackTrace)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    content += exception.InnerException.ToString();
                }
                else
                {
                    // ReSharper disable once PossibleNullReferenceException
                    content += $"{exception.Message}: {exception.InnerException.Message}";
                }
                
                _onScreenLogger.LogMessage(content);
            }
            catch (Exception exception)
            {
                _onScreenLogger.LogMessage($"Failed to run script:\n\n{exception}");
            }
            
            return null;
        }

        private void OpenTextEditorCurrentFile()
        {
            if (string.IsNullOrWhiteSpace(CurrentFileName) || 
                string.IsNullOrWhiteSpace(_appSettings.TextEditorExecutable))
            {
                return;
            }
            
            var fullPath = Path.Combine(_appSettings.ScriptFolderPath, CurrentFileName);
            Process.Start("cmd", $"/C {_appSettings.TextEditorExecutable} \"{fullPath}\"");
        }

        private void SetupScriptExecution()
        {
            _scriptRunner = CurrentLanguage switch
            {
                ScriptLanguage.CSharp => new CSharpScriptRunner(),
                _ => throw new NotSupportedException($"No script runner for script of type '{CurrentLanguage}'")
            };

            _fileSystemWatcher.EnableRaisingEvents = false;
            _fileSystemWatcher.Path = _appSettings.ScriptFolderPath;
            _fileSystemWatcher.Filter = CurrentFileName;
            _fileSystemWatcher.EnableRaisingEvents = true;

            var script = File.ReadAllText(Path.Combine(_appSettings.ScriptFolderPath, CurrentFileName));
            _waitingScriptContent.Enqueue(script);
        }

        private void FileSystemWatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            Task.Run(async () =>
            {
                // This sometimes triggers too soon while the other application is still writing, and thus the other
                // application will have the file locked.  So we need to retry a few times until we are allowed
                // to open it.
                const int maxRetry = 10;
                var retryCount = 0;
                while (true)
                {
                    try
                    {
                        var script = await File.ReadAllTextAsync(e.FullPath);
                        _waitingScriptContent.Enqueue(script);
                        return;
                    }
                    catch (IOException exception)
                    {
                        if (retryCount >= maxRetry)
                        {
                            _onScreenLogger.LogMessage(
                                $"Failed to load changes from current file {maxRetry} times:\n{exception}");

                            return;
                        }
                    }
                    catch (Exception exception)
                    {
                        _onScreenLogger.LogMessage($"Failed to read from current file:\n{exception}");
                        return;
                    }

                    await Task.Delay(10);
                    retryCount++;
                }
            });
        }
    }
}