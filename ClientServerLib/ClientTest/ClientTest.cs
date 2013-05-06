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
using Vocaluxe.Base.Server;

namespace ClientTest
{
    public partial class ClientTest : Form
    {
        CClient client;
        private bool loggedIn;

        public ClientTest()
        {
            client = new CClient();
            loggedIn = false;

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
            if (!client.Connected)
                return;

            client.SendMessage(CCommands.CreateCommandLogin(tbPassword.Text), OnResponse);
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

        private void OnResponse(byte[] Message)
        {
            if (Message == null)
                return;

            if (Message.Length < 4)
                return;

            int command = BitConverter.ToInt32(Message, 0);
            switch (command)
            {
                case CCommands.ResponseLoginFailed:
                    MessageBox.Show("Unknown Error on login", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;

                case CCommands.ResponseLoginWrongPassword:
                    MessageBox.Show("Wrong Password", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;

                case CCommands.ResponseLoginOK:
                    loggedIn = true;
                    this.Invoke((MethodInvoker)delegate
                    {
                        lbConnectionStatusText.Text = "Logged in";
                    });
                    break;

                default:
                    break;
            }
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
                    btUp.Enabled = true;
                    btDown.Enabled = true;
                    btLeft.Enabled = true;
                    btRight.Enabled = true;
                    btSendAvatar.Enabled = true;
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
                    loggedIn = false;
                    btUp.Enabled = false;
                    btDown.Enabled = false;
                    btLeft.Enabled = false;
                    btRight.Enabled = false;
                    btSendAvatar.Enabled = false;
                });
            }
        }

        private void OnSend(byte[] Message)
        {
            if (Message == null)
                return;

            string text = String.Empty;
            int max = Message.Length;
            if (max > 1000)
                max = 1000;
            for (int i = 0; i < max; i++)
            {
                text += Message[i].ToString() + " ";
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
            int max = Message.Length;
            if (max > 1000)
                max = 1000;
            for (int i = 0; i < max; i++)
            {
                text += Message[i].ToString() + " ";
            }

            this.Invoke((MethodInvoker)delegate
            {
                tbDataReceiving.Text = text;
            });
        }

        private void btUp_Click(object sender, EventArgs e)
        {
            if (!client.Connected || !loggedIn)
                return;

            client.SendMessage(CCommands.CreateCommandWithoutParams(CCommands.CommandSendKeyUp), null);
        }

        private void btLeft_Click(object sender, EventArgs e)
        {
            if (!client.Connected || !loggedIn)
                return;

            client.SendMessage(CCommands.CreateCommandWithoutParams(CCommands.CommandSendKeyLeft), null);
        }

        private void btDown_Click(object sender, EventArgs e)
        {
            if (!client.Connected || !loggedIn)
                return;

            client.SendMessage(CCommands.CreateCommandWithoutParams(CCommands.CommandSendKeyDown), null);
        }

        private void brRight_Click(object sender, EventArgs e)
        {
            if (!client.Connected || !loggedIn)
                return;

            client.SendMessage(CCommands.CreateCommandWithoutParams(CCommands.CommandSendKeyRight), null);
        }

        private void btSendAvatar_Click(object sender, EventArgs e)
        {
            if (!client.Connected || !loggedIn)
                return;

            byte[] data = new byte[512 * 512 * 4];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte) (i % 255);
            }
            client.SendMessage(CCommands.CreateCommandSendAvatarPicture(512, 512, data), null);
        }
    }
}
