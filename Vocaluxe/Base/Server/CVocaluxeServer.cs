#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

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
using Vocaluxe.Lib.Input;
using VocaluxeLib;
using VocaluxeLib.Profile;

namespace Vocaluxe.Base.Server
{
    static class CVocaluxeServer
    {
        private static CServer _Server;
        private static CDiscover _Discover;
        private static Dictionary<int, CClientHandler> _Clients;

        public static readonly CControllerFramework Controller = new CControllerFramework();

        public static void Init()
        {
            _Clients = new Dictionary<int, CClientHandler>();
            if (CConfig.ServerEncryption == EOffOn.TR_CONFIG_ON)
                _Server = new CServer(RequestHandler, CConfig.ServerPort, CConfig.ServerPassword);
            else
                _Server = new CServer(RequestHandler, CConfig.ServerPort, String.Empty);

            _Discover = new CDiscover(CConfig.ServerPort, CCommands.BroadcastKeyword);
            Controller.Init();
        }

        public static void Start()
        {
            if (CConfig.ServerActive == EOffOn.TR_CONFIG_ON)
            {
                _Server.Start();
                _Discover.StartBroadcasting();
            }
        }

        public static void Close()
        {
            _Server.Stop();
            _Discover.Stop();
            _Clients = new Dictionary<int, CClientHandler>();
        }

        private static byte[] RequestHandler(int connectionID, byte[] message)
        {
            byte[] answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseNOK);

            if (message == null)
                return answer;

            if (message.Length < 4)
                return answer;

            bool loggedIn;
            lock (_Clients)
            {
                if (!_Clients.ContainsKey(connectionID))
                {
                    CClientHandler client = new CClientHandler(connectionID);
                    _Clients.Add(connectionID, client);
                }

                loggedIn = _Clients[connectionID].LoggedIn;
            }

            int command = BitConverter.ToInt32(message, 0);

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
                    if (CCommands.DecodeCommandSendAvatarPicture(message, out avatarPicture))
                    {
                        using (Bitmap bmp = new Bitmap(avatarPicture.Width, avatarPicture.Height))
                        {
                            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                            Marshal.Copy(avatarPicture.data, 0, bmpData.Scan0, avatarPicture.data.Length);
                            bmp.UnlockBits(bmpData);

                            const string filename = "snapshot";
                            int i = 0;
                            while (File.Exists(Path.Combine(CSettings.FolderProfiles, filename + i + ".png")))
                                i++;
                            bmp.Save(Path.Combine(CSettings.FolderProfiles, filename + i + ".png"), ImageFormat.Png);
                        }

                        answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseOK);
                    }
                    break;

                case CCommands.CommandSendAvatarPictureJpg:

                    SAvatarPicture avatarPictureJpg;
                    if (CCommands.DecodeCommandSendAvatarPicture(message, out avatarPictureJpg))
                    {
                        if (_AddAvatar(avatarPictureJpg.data) != String.Empty)
                            answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseOK);
                    }
                    break;

                case CCommands.CommandSendProfile:
                    SProfile profile;
                    if (CCommands.DecodeCommandSendProfile(message, out profile))
                    {
                        try
                        {
                            string avatarFilename = _AddAvatar(profile.Avatar.data);
                            if (avatarFilename != String.Empty)
                            {
                                CProfile p = new CProfile
                                    {
                                        Active = EOffOn.TR_CONFIG_ON,
                                        AvatarFileName = avatarFilename,
                                        Difficulty = (EGameDifficulty)profile.Difficulty,
                                        GuestProfile = EOffOn.TR_CONFIG_OFF,
                                        PlayerName = profile.PlayerName
                                    };
                                CProfiles.AddProfile(p);
                                answer = CCommands.CreateCommandWithoutParams(CCommands.ResponseOK);
                            }
                        }
                        catch (Exception) {}
                    }

                    break;
            }

            return answer;
        }

        private static string _AddAvatar(byte[] jpgData)
        {
            string result = String.Empty;
            try
            {
                string filename = Path.Combine(CSettings.FolderProfiles, "snapshot");
                int i = 0;
                while (File.Exists(filename + i + ".jpg"))
                    i++;

                filename = filename + i + ".jpg";
                FileStream fs = File.Create(filename);
                fs.Write(jpgData, 0, jpgData.Length);
                fs.Flush();
                fs.Close();

                CAvatar avatar = new CAvatar(-1);
                if (avatar.LoadFromFile(filename))
                {
                    CProfiles.AddAvatar(avatar);
                    result = filename;
                }
            }
            catch {}

            return result;
        }
    }
}