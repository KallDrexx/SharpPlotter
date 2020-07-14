using System.Collections.Generic;

namespace SharpPlotter
{
    public class AppSettings
    {
        private const int RecentListSize = 5;

        private readonly List<string> _recentlyOpenedFiles = new List<string>();
        
        public string ScriptFolderPath { get; set; }
        public string TextEditorExecutable { get; set; }

        public IReadOnlyList<string> RecentlyOpenedFiles
        {
            get => _recentlyOpenedFiles;
            set
            {
                if (value != null)
                {
                    _recentlyOpenedFiles.Clear();
                    _recentlyOpenedFiles.AddRange(value);
                }
            }
        }

        public void AddOpenedFileName(string fileName)
        {
            _recentlyOpenedFiles.Insert(0, fileName);
            for (var x = _recentlyOpenedFiles.Count - 1; x > RecentListSize; x--)
            {
                _recentlyOpenedFiles.RemoveAt(x);
            }
        }
    }
}