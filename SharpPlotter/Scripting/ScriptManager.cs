using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpPlotter.Scripting
{
    public class ScriptManager
    {
        private readonly AppSettings _appSettings;
        private readonly OnScreenLogger _onScreenLogger;
        private readonly FileSystemWatcher _fileSystemWatcher;
        private IScriptRunner _scriptRunner;
        
        private long _lastScriptChangedEventFiredAt;
        private long _lastProcessedScriptChangedEvent;

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
                ScriptLanguage.Javascript => ".js",
                ScriptLanguage.Python => ".py",
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

            CurrentFileName = fileName;
            CurrentLanguage = language;
            SetupScriptRunner();
            
            File.WriteAllText(fullPath, _scriptRunner.NewFileHeaderContent);
            
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
                ".js" => ScriptLanguage.Javascript,
                ".py" => ScriptLanguage.Python,
                _ => throw new NotSupportedException($"No scripting language could be found for extension '{extension}'")
            };

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"The file '{fileName}' does not exist in the script directory", fullPath);
            }
            
            CurrentLanguage = language;
            CurrentFileName = fileName;
            SetupScriptRunner();
            
            _appSettings.AddOpenedFileName(fileName);
            OpenTextEditorCurrentFile();
            SetupScriptExecution();
        }

        public GraphedItems CheckForNewGraphedItems()
        {
            if (_lastScriptChangedEventFiredAt > _lastProcessedScriptChangedEvent)
            {
                // Only actually read the latest script file contents after a period of time since the
                // last change event fired.  This prevents reading while it's still being written, and should
                // help with file lock contention.
                var lastFiredAtTicks = _lastScriptChangedEventFiredAt;
                var lastFiredAt = new DateTime(lastFiredAtTicks);
                if ((DateTime.Now - lastFiredAt).TotalMilliseconds > 200)
                {
                    var contents = TryLoadFileContents();
                    if (contents != null)
                    {
                        Interlocked.Exchange(ref _lastProcessedScriptChangedEvent, lastFiredAtTicks);
                        
                        try
                        {
                            _onScreenLogger.Clear();
                            return _scriptRunner.RunScript(contents);
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
                        catch (PointConversionException exception)
                        {
                            _onScreenLogger.LogMessage(exception.Message);
                        }
                        catch (Exception exception)
                        {
                            _onScreenLogger.LogMessage($"Failed to run script:\n\n{exception}");
                        }
                    }
                }
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

        private void SetupScriptRunner()
        {
            _scriptRunner = CurrentLanguage switch
            {
                ScriptLanguage.CSharp => new CSharpScriptRunner(),
                ScriptLanguage.Javascript => new JavascriptRunner(),
                ScriptLanguage.Python => new PythonScriptRunner(),
                _ => throw new NotSupportedException($"No script runner for script of type '{CurrentLanguage}'")
            };
        }

        private void SetupScriptExecution()
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
            _fileSystemWatcher.Path = _appSettings.ScriptFolderPath;
            _fileSystemWatcher.Filter = CurrentFileName;
            _fileSystemWatcher.EnableRaisingEvents = true;

            Interlocked.Exchange(ref _lastScriptChangedEventFiredAt, DateTime.Now.Ticks);
        }

        private string TryLoadFileContents()
        {
            try
            {
                var fullPath = Path.Combine(_appSettings.ScriptFolderPath, CurrentFileName);
                return File.ReadAllText(fullPath);
            }
            catch (Exception exception)
            {
                _onScreenLogger.LogMessage($"Failed to read current script file:\n{exception}");
                return null;
            }
        }

        private void FileSystemWatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            // We don't want to read the script immediately after this event triggers, as this event will 
            // trigger multiple times while the file is being written to.  What we really want is to keep
            // track when this event triggers and only read the script after a period of time where this 
            // event has stopped triggering.
            Interlocked.Exchange(ref _lastScriptChangedEventFiredAt, DateTime.Now.Ticks);
        }
    }
}