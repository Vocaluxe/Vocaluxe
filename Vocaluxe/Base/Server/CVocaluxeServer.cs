using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ClientServerLib;
using Vocaluxe.Base;
using Vocaluxe.Lib.Input;
using VocaluxeLib.Menu;

namespace Vocaluxe.Base.Server
{
    static class CVocaluxeServer
    {
        private static CServer _Server;
        private static Dictionary<int, CClientHandler> _Clients;

        public static CControllerFramework Controller = new CControllerFramework();

        public static void Init()
        {
            _Clients = new Dictionary<int, CClientHandler>();
            _Server = new CServer(RequestHandler, CConfig.ServerPort);
            Controller.Init();
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

            bool loggedIn = false;
            lock (_Clients)
            {
                if (!_Clients.ContainsKey(ConnectionID))
                {
                    CClientHandler client = new CClientHandler(ConnectionID);
                    _Clients.Add(ConnectionID, client);
                }

                loggedIn = _Clients[ConnectionID].LoggedIn;
            }

            int command = BitConverter.ToInt32(Message, 0);
            byte[] answer = null;

            switch (command)
            {
                case CCommands.CommandLogin:
                    SLoginData data;

                    if (!CCommands.DecodeCommandLogin(Message, out data))
                        answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseLoginFailed);
                    else
                    {
                        byte[] serverPW = CCommands.SHA256.ComputeHash(Encoding.UTF8.GetBytes(CConfig.ServerPassword));

                        if (!serverPW.SequenceEqual(data.SHA256))
                            answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseLoginWrongPassword);
                        else
                        {
                            answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseLoginOK);
                            _Clients[ConnectionID].LoggedIn = true;
                        }
                    }
                    break;
            }

            if (!loggedIn)
                return answer;

            switch (command)
            {
                case CCommands.CommandSendKeyEvent:
                    Keys key;
                    if (!CCommands.DecodeCommandSendKeyEvent(Message, out key))
                        answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseNOK);
                    else
                    {
                        answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseOK);
                    }

                    Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, key));
                    break;

                default:
                    break;
            }

            return answer;
        }
    }
}
