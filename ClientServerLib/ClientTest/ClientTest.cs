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
            client.Connect(tbServerIP.Text, port, OnConnectionChanged);
        }

        private void Disconnect()
        {
            client.Disconnect();
        }

        private void OnConnectionChanged(bool Connected)
        {
            if (Connected)
                lbConnectionStatusText.Text = "Connected";
            else
            {
                client.Disconnect();
                lbConnectionStatusText.Text = "Disconnected";
            }
        }
    }
}
