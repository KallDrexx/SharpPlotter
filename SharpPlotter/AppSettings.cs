using System.Collections.Generic;

namespace SharpPlotter
{
    public class AppSettings
    {
        private const int RecentListSize = 5;
        
        public string ScriptFolderPath { get; set; }
        public string TextEditorExecutable { get; set; }

        // ReSharper disable once MemberCanBePrivate.Global
        public List<string> RecentlyOpenedFiles { get; set; }

        public void AddOpenedFileName(string fileName)
        {
            RecentlyOpenedFiles ??= new List<string>();
            RecentlyOpenedFiles.Remove(fileName);
            
            RecentlyOpenedFiles.Insert(0, fileName);
            for (var x = RecentlyOpenedFiles.Count - 1; x > RecentListSize; x--)
            {
                RecentlyOpenedFiles.RemoveAt(x);
            }
        }
    }
}