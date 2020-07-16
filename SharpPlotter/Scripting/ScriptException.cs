using System;

namespace SharpPlotter.Scripting
{
    public class ScriptException : Exception
    {
        public bool ShowStackTrace { get; set; }

        public ScriptException(string message, bool showStackTrace, Exception inner)
            : base(message, inner)
        {
            if (inner == null) throw new ArgumentNullException(nameof(inner));
            
            ShowStackTrace = showStackTrace;
        }
    }
}