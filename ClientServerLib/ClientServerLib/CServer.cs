using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ClientServerLib
{
    public class CServer
    {
        private TcpListener tcpListener;
        private Thread listenThread;
        private int port;
        private bool encryption;
        private string password;
        private int bufferLength = 8192;
        private bool running;

        private HandleRequest requestCallback;

        private Dictionary<int, CConnection> clients;
        private  Queue<int> ids;

        public CServer(HandleRequest RequestCallback, int Port = 3000, string Password = null)
        {
            port = Port;
            running = false;
            requestCallback = RequestCallback;
            encryption = Password != null;
            password = Password;

            clients = new Dictionary<int, CConnection>();
            ids = new Queue<int>(1000);

            for (int i = 0; i < 1000; i++)
                ids.Enqueue(i);
        }

        public void Start()
        {
            if (!running)
            {
                running = true;

                tcpListener = new TcpListener(IPAddress.Any, port);

                listenThread = new Thread(new ThreadStart(ListenForClients));
                listenThread.Start();
            }
        }

        public void Stop()
        {
            if (running)
            {
                running = false;
                tcpListener.Stop();
                Thread.Sleep(100);
                listenThread.Join();

                lock (clients)
                {
                    int[] cIDs = new int[clients.Count];
                    clients.Keys.CopyTo(cIDs, 0);

                    foreach (int id in cIDs)
                    {
                        clients.Remove(id);
                        ids.Enqueue(id);
                    }
                }
            }
        }

        private void ListenForClients()
        {
            tcpListener.Start();
            TcpClient client;

            while (running)
            {
                //wait for new client
                try
                {
                    client = tcpListener.AcceptTcpClient();
                }
                catch
                {
                    client = null;
                }

                if (client != null)
                {
                    int id = ids.Dequeue();
                    CConnection connection = new CConnection(client, id, password);

                    lock (clients)
                    {
                        clients.Add(id, connection);
                    }

                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientCom));

                    NetworkStream clientStream = client.GetStream();
                    byte[] serverParams = connection.GetKeyParams();

                    try
                    {
                        clientStream.Write(serverParams, 0, serverParams.Length);
                        clientStream.Flush();
                    }
                    catch
                    {
                        client = null;
                    }

                    if (client != null)
                        clientThread.Start(connection);
                }
            }
        }

        private void HandleClientCom(object client)
        {
            CConnection connection = (CConnection)client;
            TcpClient tcpClient = (TcpClient)connection.TcpClient;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] data = new byte[bufferLength];
            int bytesRead;

            while (running)
            {
                bytesRead = 0;

                try
                {
                    bytesRead = clientStream.Read(data, 0, bufferLength);
                }
                catch
                {
                    break;
                }

                if (bytesRead == 0)
                    break;

                if (!connection.KeySet)
                {
                    byte[] clientResponse = new byte[bytesRead];
                    Array.Copy(data, clientResponse, bytesRead);
                    connection.CreateServerKey(clientResponse);
                }
                else
                {
                    //receive message
                    int messageLength = BitConverter.ToInt32(data, 0);
                    while (messageLength > bytesRead)
                    {
                        if (data.Length < messageLength + bufferLength)
                            Array.Resize<byte>(ref data, messageLength + bufferLength);
                        bytesRead += clientStream.Read(data, bytesRead, bufferLength);
                    }

                    byte[] message = new byte[bytesRead];
                    Array.Copy(data, message, bytesRead);
                    byte[] decrypted = connection.Decrypt(message);

                    byte[] answer = HandleClientMessage(decrypted, connection);

                    //send message
                    byte[] encrypted = connection.Encrypt(answer);

                    try
                    {
                        clientStream.Write(encrypted, 0, encrypted.Length);
                        clientStream.Flush();
                    }
                    catch
                    {
                        break;
                    }
                }
            }
        }

        private byte[] HandleClientMessage(byte[] Message, CConnection connection)
        {
            return requestCallback(connection.ConnectionID, Message);
        }
    }
}
