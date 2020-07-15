using System.Collections.Concurrent;

namespace SharpPlotter
{
    public class OnScreenLogger
    {
        private readonly ConcurrentQueue<string> _messages = new ConcurrentQueue<string>();

        public string GetLatestMessage() => _messages.TryPeek(out var message) ? message : null;

        public void LogMessage(string message)
        {
            _messages.Enqueue(message);
        }

        public void Clear()
        {
            _messages.Clear();
        }

        public void RemoveMostRecentMessage()
        {
            _messages.TryDequeue(out _);
        }
    }
}