using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Security.Cryptography;

using Newtonsoft.Json;

namespace Vocaluxe.Base.Server
{
    public static class CCommands
    {
        private static UTF8Encoding encoder = new UTF8Encoding();
        public static SHA256Managed SHA256 = new SHA256Managed();

        public const string BroadcastKeyword = "I am a Vocaluxe Server";

        #region Commands
        public const int ResponseOK = 1;
        public const int ResponseNOK = 2;

        public const int CommandLogin = 20;
        public const int ResponseLoginWrongPassword = 21;
        public const int ResponseLoginFailed = 22;
        public const int ResponseLoginOK = 23;


        public const int CommandSendKeyStroke = 100;
        public const int CommandSendKeyUp = 110;
        public const int CommandSendKeyDown = 111;
        public const int CommandSendKeyLeft = 112;
        public const int CommandSendKeyRight = 113;

        public const int CommandSendMouseMoveEvent = 200;
        public const int CommandSendMouseLBDownEvent = 220;
        public const int CommandSendMouseLBUpEvent = 221;
        public const int CommandSendMouseRBDownEvent = 230;
        public const int CommandSendMouseRBUpEvent = 231;
        public const int ComamndSendMouseMBDownEvent = 240;
        public const int ComamndSendMouseMBUpEvent = 241;
        public const int CommandSendMouseWheelEvent = 250;

        public const int CommandSendAvatarPicture = 500;
        public const int CommandSendAvatarPictureJpg = 501;

        public const int CommandSendProfile = 510;

        #endregion Commands

        #region General
        public static byte[] CreateCommandWithoutParams(int Command)
        {
            return BitConverter.GetBytes(Command);
        }
        #endregion General

        #region Login
        public static byte[] CreateCommandLogin(string Password)
        {
            SLoginData data = new SLoginData();
            data.SHA256 = SHA256.ComputeHash(Encoding.UTF8.GetBytes(Password));

            return Serialize<SLoginData>(CommandLogin, data);
        }

        public static bool DecodeCommandLogin(byte[] Message, out SLoginData LoginData)
        {
            return TryDeserialize<SLoginData>(Message, out LoginData);
        }
        #endregion Login

        #region Keyboard

        #endregion Keyboard

        #region Profiles
        public static byte[] CreateCommandSendAvatarPicture(int Width, int Height, byte[] data)
        {
            SAvatarPicture ap = new SAvatarPicture();
            ap.Height = Height;
            ap.Width = Width;
            ap.data = new byte[data.Length];
            Array.Copy(data, ap.data, data.Length);

            return Serialize<SAvatarPicture>(CommandSendAvatarPicture, ap);
        }

        public static bool DecodeCommandSendAvatarPicture(byte[] Message, out SAvatarPicture AvatarPicture)
        {
            return TryDeserialize<SAvatarPicture>(Message, out AvatarPicture);
        }

        public static byte[] CreateCommandSendAvatarPictureJpg(byte[] AvatarJpgData)
        {
            SAvatarPicture ap = new SAvatarPicture();
            ap.Height = 0;
            ap.Width = 0;
            ap.data = new byte[AvatarJpgData.Length];
            Array.Copy(AvatarJpgData, ap.data, AvatarJpgData.Length);

            return Serialize<SAvatarPicture>(CommandSendAvatarPictureJpg, ap);
        }

        public static byte[] CreateCommandSendProfile(byte[] AvatarJpgData, string PlayerName, int Difficulty)
        {
            SAvatarPicture ap = new SAvatarPicture();
            ap.Height = 0;
            ap.Width = 0;
            ap.data = new byte[AvatarJpgData.Length];
            Array.Copy(AvatarJpgData, ap.data, AvatarJpgData.Length);

            SProfile profile = new SProfile();
            profile.Avatar = ap;
            profile.PlayerName = PlayerName;
            profile.Difficulty = Difficulty;

            return Serialize<SProfile>(CommandSendProfile, profile);
        }

        public static bool DecodeCommandSendProfile(byte[] Message, out SProfile Profile)
        {
            return TryDeserialize<SProfile>(Message, out Profile);
        }

        #endregion Profiles

        #region Serializing
        private static byte[] Serialize<T>(int Command, T obj)
        {
            byte[] command = BitConverter.GetBytes(Command);

            MemoryStream stream = new MemoryStream();
            stream.Write(command, 0, command.Length);

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                string json = JsonConvert.SerializeObject(obj);
                data = encoder.GetBytes(json);
            }
            stream.Write(data, 0, data.Length);
            return stream.ToArray();
        }

        private static bool TryDeserialize<T>(byte[] message, out T obj)
        {
            obj = default(T);

            if (message == null)
                return false;

            if (message.Length < 5)
                return false;

            byte[] data = new byte[message.Length - 4];
            Array.Copy(message, 4, data, 0, data.Length);
            using (MemoryStream ms = new MemoryStream(data))
            {
                try
                {
                    obj = JsonConvert.DeserializeObject<T>(encoder.GetString(data));
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
        #endregion Serializing
    }
}
