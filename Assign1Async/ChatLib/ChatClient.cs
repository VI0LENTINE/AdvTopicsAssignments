using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatLib
{
    public class ChatClient : IChatClient
    {
        private TcpClient _tcpClient; // Connects to the chat server
        private StreamReader _reader; // Reads messages from server
        private StreamWriter _writer; // Sends messages to server
        private CancellationTokenSource _cts; // Used to stop the listening loop
        private readonly object _sendLock = new object(); // Prevents send conflicts

        public event EventHandler<ChatMessageEventArgs> MessageReceived; // Triggered when a message arrives

        public bool IsConnected => _tcpClient?.Connected ?? false; // True if connected

        public async Task ConnectAsync(string host, int port, string username = null)
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(host, port); // Connect to server
            var ns = _tcpClient.GetStream();
            //No strange characters in cmd prompt: https://learn.microsoft.com/en-us/dotnet/api/system.text.utf8encoding.-ctor?view=net-9.0
            _reader = new StreamReader(ns, new UTF8Encoding(false));
            _writer = new StreamWriter(ns, new UTF8Encoding(false)) { AutoFlush = true };
            _cts = new CancellationTokenSource();

            // Send username to server if provided
            if (!string.IsNullOrEmpty(username))
                await _writer.WriteLineAsync(username);

            // Start listening for messages in the background
            _ = Task.Run(() => ListenLoopAsync(_cts.Token));
        }

        private async Task ListenLoopAsync(CancellationToken token)
        {
            try
            {
                // Keep reading messages until stopped
                while (!token.IsCancellationRequested)
                {
                    string line = await _reader.ReadLineAsync().ConfigureAwait(false);
                    if (line == null) break; // Server closed connection
                    OnMessageReceived(new ChatMessageEventArgs(line)); // Notify UI
                }
            }
            catch (OperationCanceledException) { }
            catch (IOException) { }  // Connection lost
            catch (Exception ex)
            {
                // Show any unexpected errors
                OnMessageReceived(new ChatMessageEventArgs($"[System] Receive error: {ex.Message}"));
            }
        }

        protected virtual void OnMessageReceived(ChatMessageEventArgs e)
        {
            MessageReceived?.Invoke(this, e); // Raise event
        }

        public async Task SendMessageAsync(string message)
        {
            if (_writer == null) throw new InvalidOperationException("Not connected.");
            lock (_sendLock)
            {
                _writer.WriteLine(message); // Send message to server
            }
            await Task.CompletedTask;
        }

        public void Disconnect()
        {
            try
            {
                _cts?.Cancel();
                _reader?.Close();
                _writer?.Close();
                _tcpClient?.Close();
            }
            catch { }
        }
    }
}
