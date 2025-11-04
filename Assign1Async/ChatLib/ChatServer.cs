using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatLib
{
    public class ChatServer
    {
        private TcpListener _listener; // Listens for clients
        private Thread _acceptThread; // Runs the accept loop
        private readonly ConcurrentDictionary<int, ClientHandler> _clients = new ConcurrentDictionary<int, ClientHandler>(); // Active clients
        private int _nextId; // Unique client IDs
        public int Port { get; }

        public ChatServer(int port)
        {
            Port = port;
        }

        public void Start()
        {
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start(); // Begin listening for clients
            _acceptThread = new Thread(AcceptLoop) { IsBackground = true };
            _acceptThread.Start();
        }

        private void AcceptLoop()
        {
            while (true)
            {
                try
                {
                    // Wait for a new client to connect
                    var tcp = _listener.AcceptTcpClient();
                    int id = System.Threading.Interlocked.Increment(ref _nextId);
                    var handler = new ClientHandler(id, tcp, this);

                    // Add client to the list
                    if (_clients.TryAdd(id, handler))
                    {
                        handler.Start(); // Start handling messages
                    }
                }
                catch (SocketException)
                {
                    // listener stopped
                    break;
                }
                catch { }
            }
        }

        // Send a message to all clients
        public void Broadcast(string message)
        {
            foreach (var kv in _clients)
            {
                kv.Value.Send(message);
            }
        }

        // Remove a disconnected client
        public void RemoveClient(int id)
        {
            if (_clients.TryRemove(id, out var handler))
            {
                handler.Stop();
            }
        }

        // Stop the server and disconnect everyone
        public void Stop()
        {
            try { _listener.Stop(); } catch { }
            foreach (var kv in _clients)
            {
                kv.Value.Stop();
            }
            _clients.Clear();
        }
    }
}
