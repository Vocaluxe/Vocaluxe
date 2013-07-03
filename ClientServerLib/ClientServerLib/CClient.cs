using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


namespace ClientServerLib
{
    public class CClient
    {
        private bool doConnect;
        private bool connected;
        private bool running;
        private int bufferLength = 8192;

        private Thread clientThread;
        private IPEndPoint serverEndPoint;
        private string ip;
        private int port;

        private CConnection connection;
        private List<SRequest> requests;
        private List<SRequest> responses;
        private Object mutexRequests = new Object();
        private OnConnectionChanged onConnectionChanged;
        private OnSend onSend;
        private OnReceived onReceived;

        private byte[] data;

        public bool Connected
        {
            get { return connected; }
        }

        public CClient()
        {
            doConnect = false;
            connected = false;
            running = true;
            requests = new List<SRequest>();
            responses = new List<SRequest>();
            data = new byte[bufferLength];

            connection = new CConnection(new TcpClient(), -1);
        }

        public void Connect(string IP = "127.0.0.1", int Port = 3000, string Password = null,
            OnConnectionChanged OnConnectionChanged = null,
            OnSend OnSend = null,
            OnReceived OnReceived = null)
        {
            if (!doConnect)
            {
                onConnectionChanged = OnConnectionChanged;
                onSend = OnSend;
                onReceived = OnReceived;

                ip = IP;
                port = Port;
                connection.Password = Password;
                doConnect = true;

                if (clientThread == null)
                {
                    running = true;
                    clientThread = new Thread(new ThreadStart(Runner));
                    clientThread.Start();
                }
            }
        }

        public void Disconnect()
        {
            if (doConnect)
            {
                doConnect = false;

                lock (mutexRequests)
                {
                    requests.Clear();
                }

                lock (responses)
                {
                    responses.Clear();
                }
            }
        }

        public void Close()
        {
            running = false;

            if (clientThread != null)
                clientThread.Join(100);

            clientThread = null;
            DoDisconnect();
        }

        public void SendMessage(byte[] Message, ResponseCallback Callback)
        {
            SRequest req = new SRequest();
            req.Command = Message;
            req.Callback = Callback;
            req.Response = null;

            lock (mutexRequests)
            {
                requests.Add(req);
            }
        }

        private void Runner()
        {
            while (running)
            {
                if (!connected && doConnect)
                {
                    try
                    {
                        IPAddress resolvedIP;
                        if (TryResolveIPAddress(ip, out resolvedIP))
                        {
                            serverEndPoint = new IPEndPoint(resolvedIP, port);
                            connection.TcpClient.Connect(serverEndPoint);
                            connected = true;
                            RaiseOnConnectionChanged();
                        }
                    }
                    catch
                    {
                        DoDisconnect();

                        for (int i = 0; i < 500; i++)
                        {
                            if (running && doConnect)
                                Thread.Sleep(10);
                            else
                                break;
                        }
                        
                    }
                }

                if (connected && !doConnect)
                {
                    DoDisconnect();
                }

                if (doConnect && connected)
                {
                    try
                    {
                        Communicate();
                        Thread.Sleep(10);
                    }
                    catch
                    {
                        DoDisconnect();
                    }

                }

                if (!doConnect && !connected)
                {
                    for (int i = 0; i < 500; i++)
                    {
                        if (running && !doConnect)
                            Thread.Sleep(10);
                        else
                            break;
                    }
                }
            }
        }

        public bool TryResolveIPAddress(string serverNameOrURL, out IPAddress resolvedIPAddress)
        {
            bool isResolved = false;
            IPHostEntry hostEntry = null;
            IPAddress resolvIP = null;
            try
            {
                if (!IPAddress.TryParse(serverNameOrURL, out resolvIP))
                {
                    hostEntry = Dns.GetHostEntry(serverNameOrURL);
                    if (hostEntry != null && hostEntry.AddressList != null && hostEntry.AddressList.Length > 0)
                    {
                        if (hostEntry.AddressList.Length == 1)
                        {
                            resolvIP = hostEntry.AddressList[0];
                            isResolved = true;
                        }
                        else
                        {
                            foreach (IPAddress var in hostEntry.AddressList)
                            {
                                if (var.AddressFamily == AddressFamily.InterNetwork)
                                {
                                    resolvIP = var;
                                    isResolved = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                    isResolved = true;
            }
            catch (Exception) {}
            finally
            {
                resolvedIPAddress = resolvIP;
            }
            return isResolved;
        }

        private void DoDisconnect()
        {
            try
            {
                connection.TcpClient.Close();
            }
            catch { }

            connection = new CConnection(new TcpClient(), -1);
            connected = false;
            doConnect = false;
            RaiseOnConnectionChanged();
        }

        private void Communicate()
        {
            if (connection.TcpClient == null || !connection.TcpClient.Connected)
                DoDisconnect();

            NetworkStream clientStream = connection.TcpClient.GetStream();
                        
            int bytesRead;

            if (!connection.KeySet)
            {
                bytesRead = 0;

                try
                {
                    bytesRead = clientStream.Read(data, 0, bufferLength);
                }
                catch
                {
                    return;
                }

                if (bytesRead == 0)
                    return;

                byte[] serverParam = new byte[bytesRead];
                Array.Copy(data, serverParam, bytesRead);
                RaiseOnReceived(serverParam);

                byte[] clientResponse = connection.CreateClientKey(serverParam);
                RaiseOnSend(clientResponse);
                clientStream.Write(clientResponse, 0, clientResponse.Length);
                clientStream.Flush();
            }
            else
            {
                try
                {
                    while (requests.Count > 0)
                    {
                        SRequest res = HandleRequest(requests[0]);
                        lock (responses)
                        {
                            responses.Add(res);
                        }

                        lock (mutexRequests)
                        {
                            requests.RemoveAt(0);
                        }
                    }
                }
                catch { };

                lock (responses)
                {
                    while (responses.Count > 0)
                    {
                        try
                        {
                            if (responses[0].Callback != null)
                                responses[0].Callback(responses[0].Response);
                        }
                        finally
                        {
                            responses.RemoveAt(0);
                        }                       
                    }
                }

            }
        }

        private SRequest HandleRequest(SRequest request)
        {
            SRequest response = new SRequest();
            response.Command = request.Command;
            response.Callback = request.Callback;
            response.Response = null;

            NetworkStream clientStream = connection.TcpClient.GetStream();

            RaiseOnSend(request.Command);
            byte[] encrypted = connection.Encrypt(request.Command);

			int sendBytes = 0;
			while(sendBytes < encrypted.Length)
			{
				int bytesToSend = encrypted.Length - sendBytes;
				if (bytesToSend > bufferLength)
					bytesToSend = bufferLength;

				clientStream.Write(encrypted, sendBytes, bytesToSend);
				sendBytes += bytesToSend;
			}
            clientStream.Flush();

            byte[] data = new byte[bufferLength];
            int bytesRead = 0;

            try
            {
                bytesRead = clientStream.Read(data, 0, bufferLength);
            }
            catch
            {
                return response;
            }

            if (bytesRead == 0)
                return response;

            int messageLength = BitConverter.ToInt32(data, 0);
            while (messageLength > bytesRead)
            {
                if (data.Length < messageLength + bufferLength)
                    Array.Resize<byte>(ref data, messageLength + bufferLength);
                bytesRead += clientStream.Read(data, bytesRead, bufferLength);
            }

            encrypted = new byte[bytesRead];
            Array.Copy(data, encrypted, bytesRead);
            response.Response = connection.Decrypt(encrypted);
            RaiseOnReceived(response.Response);

            return response;
        }

        private void RaiseOnConnectionChanged()
        {
            if (onConnectionChanged == null)
                return;

            try
            {
                onConnectionChanged(connected);
            }
            catch (Exception e)
			{
				Console.WriteLine (e.Message);
			}
        }

        private void RaiseOnSend(byte[] Message)
        {
            if (onSend == null)
                return;

            try
            {
                onSend(Message);
            }
            catch { }
        }

        private void RaiseOnReceived(byte[] Message)
        {
            if (onReceived == null)
                return;

            try
            {
                onReceived(Message);
            }
            catch { }
        }
    }
}
