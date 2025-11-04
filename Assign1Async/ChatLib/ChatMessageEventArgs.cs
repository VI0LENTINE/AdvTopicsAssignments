using System;

namespace ChatLib
{
    // Used to hold message data for events
    public class ChatMessageEventArgs : EventArgs
    {
        public string Message { get; }

        public ChatMessageEventArgs(string message)
        {
            Message = message;
        }
    }
}
