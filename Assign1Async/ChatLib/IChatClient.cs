using System;
using System.Threading.Tasks;

namespace ChatLib
{
    public interface IChatClient
    {
        event EventHandler<ChatMessageEventArgs> MessageReceived;
        Task ConnectAsync(string host, int port, string username = null);
        Task SendMessageAsync(string message);
        void Disconnect();
        bool IsConnected { get; }
    }
}
