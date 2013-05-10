using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;

namespace ClientServerLib
{
    public delegate void OnServerDiscovered(string IPAddress, string Hostname);

    public class CDiscover
    {
        private string _Keyword;
        private string _BroadcastAddress;
        private int _Port;

        private Thread _DiscoverThread;
        private bool _DiscoverRunning;
        private OnServerDiscovered _OnDiscovered;


        private System.Timers.Timer _BroadcastTimer;

        public CDiscover(int Port, string Keyword, string BroadcastAddress = "255.255.255.255")
        {
            _BroadcastAddress = BroadcastAddress;
            _Port = Port;
            _Keyword = Keyword;

            _DiscoverRunning = false;
            _BroadcastTimer = new System.Timers.Timer();
            _BroadcastTimer.Elapsed += new ElapsedEventHandler(OnBroadcastEvent);
            _BroadcastTimer.Interval = 2500;
        }

        public void StartBroadcasting()
        {
            _BroadcastTimer.Start();
        }

        public void Stop()
        {
            _BroadcastTimer.Stop();
            _DiscoverRunning = false;          
        }

        public void Discover(OnServerDiscovered OnDiscovered, int Timeout = 5000)
        {
            if (_DiscoverRunning)
                return;

            _DiscoverRunning = true;
            _DiscoverThread = new Thread(() => _Discover(Timeout));
            _OnDiscovered = OnDiscovered;
            _DiscoverThread.Start();
        }

        private void OnBroadcastEvent(object source, ElapsedEventArgs e)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.EnableBroadcast = true;
            IPAddress broadcast = IPAddress.Parse(_BroadcastAddress);
            byte[] sendbuf = Encoding.UTF8.GetBytes(_Keyword);
            IPEndPoint ep = new IPEndPoint(broadcast, _Port);

            s.SendTo(sendbuf, ep);
            s.Close();
        }

        private void _Discover(int Timeout)
        {
            UdpClient listener = null;
            Stopwatch timer = new Stopwatch();

            List<string> knownServer = new List<string>();
            bool foundSomething = false;
            timer.Start();
            while (_DiscoverRunning && timer.ElapsedMilliseconds < Timeout * 2)
            {
                try
                {
                    listener = new UdpClient(_Port);
                    IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, _Port);
                    listener.Client.ReceiveTimeout = Timeout;

                    byte[] bytes = listener.Receive(ref groupEP);
                    string message = Encoding.UTF8.GetString(bytes, 0, bytes.Length);

                    foundSomething = true;
                    if (!knownServer.Contains(groupEP.Address.ToString()))
                    {
                        knownServer.Add(groupEP.Address.ToString());
                        if (_OnDiscovered != null)
                        {
                            IPHostEntry e = Dns.GetHostEntry(groupEP.Address);
                            string hostname = e.HostName;

                            _OnDiscovered(groupEP.Address.ToString(), hostname);
                        }
                    }

                    listener.Close();
                }
                catch (Exception)
                {
                    if (listener != null)
                        listener.Close();
                }
            }
            _DiscoverRunning = false;

            if (!foundSomething)
                _OnDiscovered("Timeout", "unknown");
        }
    }
}
