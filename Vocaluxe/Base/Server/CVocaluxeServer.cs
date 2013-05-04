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

        public static void Init()
        {
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
        }

        private static byte[] RequestHandler(int ConnectionID, byte[] Message)
        {
            return null;
        }
    }
}
