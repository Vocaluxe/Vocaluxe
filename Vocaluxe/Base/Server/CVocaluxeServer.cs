using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Linq;
using System.IO;
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
            byte[] answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseNOK);

            if (Message == null)
                return answer;

            if (Message.Length < 4)
                return answer;

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
                case CCommands.CommandSendKeyUp:
                    answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseOK);
                    Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Up));
                    break;

                case CCommands.CommandSendKeyDown:
                    answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseOK);
                    Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Down));
                    break;

                case CCommands.CommandSendKeyLeft:
                    answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseOK);
                    Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Left));
                    break;

                case CCommands.CommandSendKeyRight:
                    answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseOK);
                    Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Right));
                    break;

                case CCommands.CommandSendAvatarPicture:

                    SAvatarPicture avatarPicture;
                    if (CCommands.DecodeCommandSendAvatarPicture(Message, out avatarPicture))
                    {
                        Bitmap bmp = null;
                        bool success = false;
                        try
                        {
                            bmp = new Bitmap(avatarPicture.Width, avatarPicture.Height);
                            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                            Marshal.Copy(avatarPicture.data, 0, bmpData.Scan0, avatarPicture.data.Length);
                            bmp.UnlockBits(bmpData);

                            const string filename = "snapshot";
                            int i = 0;
                            while (File.Exists(Path.Combine(CSettings.FolderProfiles, filename + i + ".png")))
                                i++;
                            bmp.Save(Path.Combine(CSettings.FolderProfiles, filename + i + ".png"), ImageFormat.Png);
                            success = true;
                        }
                        finally
                        {
                            if (bmp != null)
                                bmp.Dispose();
                        }

                        if (success)
                            answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseOK);
                    }
                    break;

                case CCommands.CommandSendAvatarPictureJpg:

                    SAvatarPicture avatarPictureJpg;
                    if (CCommands.DecodeCommandSendAvatarPicture(Message, out avatarPictureJpg))
                    {
                        bool success = false;
                        try
                        {
                            string filename = Path.Combine(CSettings.FolderProfiles, "snapshot");
                            int i = 0;
                            while (File.Exists(filename + i + ".jpg"))
                                i++;

                            FileStream fs = File.Create(filename + i + ".jpg");
                            fs.Write(avatarPictureJpg.data, 0, avatarPictureJpg.data.Length);
                            fs.Flush();
                            fs.Close();
                            success = true;
                        }
                        catch {}

                        if (success)
                            answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseOK);
                    }
                    break;

                default:
                    break;
            }

            return answer;
        }
    }
}
