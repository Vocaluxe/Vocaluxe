using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

using ClientServerLib;

namespace ClientTest
{
    public partial class ClientTest : Form
    {
        CClient client;

        public ClientTest()
        {
            client = new CClient();

            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            client.Close();
            base.OnClosing(e);
        }

        private void btConnect_Click(object sender, EventArgs e)
        {
            if (!client.Connected)
                Connect();
            else
                Disconnect();
        }

        private void btLogin_Click(object sender, EventArgs e)
        {

        }

        private void Connect()
        {
            int port = 3000;
            if (!int.TryParse(tbPort.Text, out port))
            {
                MessageBox.Show("Error parsing port number", "Port Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            IPAddress ip;
            if (!IPAddress.TryParse(tbServerIP.Text, out ip))
            {
                MessageBox.Show("Error parsing ip address", "IP Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            lbConnectionStatusText.Text = "Connecting...";
            client.Connect(tbServerIP.Text, port, OnConnectionChanged, OnSend, OnReceived);
        }

        private void Disconnect()
        {
            client.Disconnect();
        }

        private void OnConnectionChanged(bool Connected)
        {
            if (Connected)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    lbConnectionStatusText.Text = "Connected";
                    btConnect.Text = "Disconnect";
                    btLogin.Enabled = true;
                });
            }
            else
            {
                this.Invoke((MethodInvoker)delegate
                {
                    client.Disconnect();
                    lbConnectionStatusText.Text = "Disconnected";
                    btConnect.Text = "Connect";
                    btLogin.Text = "Login";
                    btLogin.Enabled = false;
                });
            }
        }

        private void OnSend(byte[] Message)
        {
            if (Message == null)
                return;

            string text = String.Empty;
            foreach (byte b in Message)
            {
                text += b.ToString() + " ";
            }

            this.Invoke((MethodInvoker)delegate
            {
                tbDataSending.Text = text;
            });
        }

        private void OnReceived(byte[] Message)
        {
            if (Message == null)
                return;

            string text = String.Empty;
            foreach (byte b in Message)
            {
                text += b.ToString() + " ";
            }

            this.Invoke((MethodInvoker)delegate
            {
                tbDataReceiving.Text = text;
            });
        }
    }
}
