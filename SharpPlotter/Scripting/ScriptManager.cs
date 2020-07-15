using System;
using System.Diagnostics;
using System.IO;

namespace SharpPlotter.Scripting
{
    public class ScriptManager
    {
        private readonly AppSettings _appSettings;

        public string CurrentFileName { get; private set; }
        public ScriptLanguage? CurrentLanguage { get; private set; }

        public ScriptManager(AppSettings appSettings)
        {
            _appSettings = appSettings;
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
    }
}