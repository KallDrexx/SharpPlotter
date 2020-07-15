using System.Collections.Generic;
using System.Linq;

namespace SharpPlotter
{
    public class OnScreenLogger
    {
        private const int MaxMessageCount = 10;
        
        private readonly List<string> _messages = new List<string>(MaxMessageCount + 1);

        public IReadOnlyList<string> Messages => _messages;

        public void LogMessage(string message)
        {
            _messages.Insert(0, message);
            while (_messages.Count > MaxMessageCount)
            {
                _messages.RemoveAt(_messages.Count - 1);
            }
        }

        public void Clear()
        {
            _messages.Clear();
        }

        public void RemoveMostRecentMessage()
        {
            if (_messages.Any())
            {
                _messages.RemoveAt(0);
            }
        }
    }
}