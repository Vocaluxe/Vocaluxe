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

            connection = new CConnection(new TcpClient(), -1);

            clientThread = new Thread(new ThreadStart(Runner));
            clientThread.Start();
        }

        public void Connect(string IP = "127.0.0.1", int Port = 3000,
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
                doConnect = true;
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
            Disconnect();
            running = false;
            clientThread.Join(100);
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
                        serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                        connection.TcpClient.Connect(serverEndPoint);
                        connected = true;
                        RaiseOnConnectionChanged();
                    }
                    catch
                    {
                        connected = false;
                        RaiseOnConnectionChanged();
                        for (int i = 0; i < 500; i++)
                        {
                            if (doConnect)
                                Thread.Sleep(10);
                            else
                                break;
                        }
                        
                    }
                }

                if (connected && !doConnect)
                {
                    try
                    {
                        connection.TcpClient.Close();
                        connection = new CConnection(new TcpClient(), -1);
                    }
                    catch
                    {
                    }

                    connected = false;
                    RaiseOnConnectionChanged();
                }

                if (doConnect && connected)
                {
                    try
                    {
                        Communicate();
                    }
                    catch
                    {
                        connected = false;
                        RaiseOnConnectionChanged();
                    }

                }
            }
        }

        private void Communicate()
        {
            NetworkStream clientStream = connection.TcpClient.GetStream();

            byte[] data = new byte[bufferLength];
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
            catch {}
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
