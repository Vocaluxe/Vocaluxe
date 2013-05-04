using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ClientServerLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Base.Server
{
    static class CVocaluxeServer
    {
        private static CServer _Server;
        private static Dictionary<int, CClientHandler> _Clients;

        public static void Init()
        {
            _Clients = new Dictionary<int, CClientHandler>();
            _Server = new CServer(RequestHandler, CConfig.ServerPort);
        }

        public static void Start()
        {
            if (CConfig.Server == EOffOn.TR_CONFIG_ON)
                _Server.Start();
        }

        public static void Close()
        {
            _Server.Stop();
            _Clients = new Dictionary<int, CClientHandler>();
        }

        private static byte[] RequestHandler(int ConnectionID, byte[] Message)
        {
            if (Message == null)
                return null;

            if (Message.Length < 4)
                return null;

            lock (_Clients)
            {
                if (!_Clients.ContainsKey(ConnectionID))
                {
                    CClientHandler client = new CClientHandler(ConnectionID);
                    _Clients.Add(ConnectionID, client);
                }
            }

            int command = BitConverter.ToInt32(Message, 0);
            byte[] answer = null;

            switch (command)
            {
                case CCommands.CommandLogin:
                    SLoginData data;

                    if (!CCommands.ResponseCommandLogin(Message, out data))
                        answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseLoginFailed);
                    else
                    {
                        byte[] serverPW = CCommands.SHA256.ComputeHash(Encoding.UTF8.GetBytes(CConfig.ServerPassword));

                        bool ok = true;
                        for (int i = 0; i < 32; i++)
                        {
                            if (serverPW[i] != data.SHA256[i])
                            {
                                ok = false;
                                break;
                            }
                        }

                        if (!ok)
                            answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseLoginWrongPassword);
                        else
                            answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseLoginOK);
                    }
                    break;

                default:
                    break;
            }

            return answer;
        }
    }
}
