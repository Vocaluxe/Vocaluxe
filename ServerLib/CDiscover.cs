#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;

namespace ServerLib
{
    public delegate void OnServerDiscovered(string ipAddress, string hostname);

    public class CDiscover
    {
        public const string Timeout = "Timeout";
        public const string Finished = "Finished";

        private readonly string _Keyword;
        private readonly string _BroadcastAddress;
        private readonly int _Port;

        private Thread _DiscoverThread;
        private bool _DiscoverRunning;
        private OnServerDiscovered _OnDiscovered;

        private readonly System.Timers.Timer _BroadcastTimer;

        public CDiscover(int port, string keyword, string broadcastAddress = "255.255.255.255")
        {
            _BroadcastAddress = broadcastAddress;
            _Port = port;
            _Keyword = keyword;

            _DiscoverRunning = false;
            _BroadcastTimer = new System.Timers.Timer();
            _BroadcastTimer.Elapsed += _OnBroadcastEvent;
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

        public void Discover(OnServerDiscovered onDiscovered, int timeout = 5000)
        {
            if (_DiscoverRunning)
                return;

            _DiscoverRunning = true;
            _DiscoverThread = new Thread(() => _Discover(timeout)) {Name = "Client discover"};
            _OnDiscovered = onDiscovered;
            _DiscoverThread.Start();
        }

        private void _OnBroadcastEvent(object source, ElapsedEventArgs e)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) {EnableBroadcast = true};
            IPAddress broadcast = IPAddress.Parse(_BroadcastAddress);
            byte[] sendbuf = Encoding.UTF8.GetBytes(_Keyword);
            IPEndPoint ep = new IPEndPoint(broadcast, _Port);

            s.SendTo(sendbuf, ep);
            s.Close();
        }

        private void _Discover(int timeout)
        {
            UdpClient listener = null;
            Stopwatch timer = new Stopwatch();

            List<string> knownServer = new List<string>();
            bool foundSomething = false;
            timer.Start();
            while (_DiscoverRunning && timer.ElapsedMilliseconds < timeout * 2)
            {
                try
                {
                    listener = new UdpClient(_Port);
                    IPEndPoint groupEp = new IPEndPoint(IPAddress.Any, _Port);
                    listener.Client.ReceiveTimeout = timeout;

                    //byte[] bytes = 
                    listener.Receive(ref groupEp);
                    //string message = Encoding.UTF8.GetString(bytes, 0, bytes.Length);

                    foundSomething = true;
                    if (!knownServer.Contains(groupEp.Address.ToString()))
                    {
                        knownServer.Add(groupEp.Address.ToString());
                        if (_OnDiscovered != null)
                        {
                            IPHostEntry e = Dns.GetHostEntry(groupEp.Address);
                            string hostname = e.HostName;

                            _OnDiscovered(groupEp.Address.ToString(), hostname);
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

            if (_OnDiscovered != null)
                _OnDiscovered(!foundSomething ? Timeout : Finished, String.Empty);
        }
    }
}