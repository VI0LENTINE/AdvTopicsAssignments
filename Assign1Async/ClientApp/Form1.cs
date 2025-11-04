using ChatLib;
using System;
using System.Windows.Forms;

namespace ClientApp
{
    public partial class Form1 : Form
    {
        private ChatClient _client;
        private const int DefaultPort = 5000;
        private const string DefaultHost = "127.0.0.1";

        public Form1()
        {
            InitializeComponent();
            richTxtChatDisplay.ReadOnly = true;
            richTxtChatDisplay.TabStop = false;
        }

        private void OnMessageReceived(object sender, ChatMessageEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnMessageReceived(sender, e)));
                return;
            }
            richTxtChatDisplay.AppendText($"{e.Message}\r\n");
        }

        private async void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                _client = new ChatClient();
                _client.MessageReceived += OnMessageReceived;
                await _client.ConnectAsync(DefaultHost, DefaultPort);
                richTxtChatDisplay.AppendText("[System] Connected to server.\r\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}", "Connection Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_client != null)
            {
                _client.Disconnect();
                _client = null;
                richTxtChatDisplay.AppendText("[System] Disconnected.\r\n");
            }
            else
            {
                MessageBox.Show("Not connected.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            if (_client == null)
            {
                MessageBox.Show("Connect first.", "Not connected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string msg = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            await _client.SendMessageAsync(msg);
            richTxtChatDisplay.AppendText($">> {msg}\r\n");
            txtMessage.Clear();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _client?.Disconnect();
            Application.Exit();
        }
    }
}
