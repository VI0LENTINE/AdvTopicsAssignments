using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace ChatLib
{
    // Handles one connected client
    internal class ClientHandler
    {
        private readonly TcpClient _tcp;
        private readonly ChatServer _server;
        private StreamReader _reader;
        private StreamWriter _writer;
        private Thread _thread;
        public string Username { get; private set; }
        public int Id { get; }

        public ClientHandler(int id, TcpClient tcp, ChatServer server)
        {
            Id = id;
            _tcp = tcp;
            _server = server;

            var ns = _tcp.GetStream();
            //No strange characters in cmd prompt: https://learn.microsoft.com/en-us/dotnet/api/system.text.utf8encoding.-ctor?view=net-9.0
            _reader = new StreamReader(ns, new UTF8Encoding(false));
            _writer = new StreamWriter(ns, new UTF8Encoding(false)) { AutoFlush = true };
        }

        public void Start()
        {
            // Run client logic in its own thread
            _thread = new Thread(Run) { IsBackground = true };
            _thread.Start();
        }

        private void Run()
        {
            try
            {
                // First message from client is their username
                Username = _reader.ReadLine() ?? $"User{Id}";
                _server.Broadcast($"{Username} has joined the chat.");

                string line;

                // Keep reading and broadcasting messages
                while ((line = _reader.ReadLine()) != null)
                {
                    _server.Broadcast($"{Username}: {line}");
                }
            }
            catch (IOException) { /* client disconnected */ }
            catch (Exception ex)
            {
                _server.Broadcast($"System: Error with {Username}: {ex.Message}");
            }
            finally
            {
                _server.RemoveClient(Id); // Remove when done
                try { _tcp.Close(); } catch { }
                _server.Broadcast($"{Username} has left the chat.");
            }
        }

        public void Send(string message)
        {
            try
            {
                _writer.WriteLine(message); // Send message to this client
            }
            catch { /* ignore send errors; server will clean up later */ }
        }

        public void Stop()
        {
            try { _tcp.Close(); } catch { }
        }
    }
}
