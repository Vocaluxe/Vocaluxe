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
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Vocaluxe.Base.Server
{
    public static class CCommands
    {
        private static readonly UTF8Encoding _Encoder = new UTF8Encoding();
        private static readonly SHA256Managed _Sha256 = new SHA256Managed();

        public const string BroadcastKeyword = "I am a Vocaluxe Server";

        #region Commands
        // ReSharper disable UnusedMember.Global
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
        // ReSharper restore UnusedMember.Global
        #endregion Commands

        #region General
        public static byte[] CreateCommandWithoutParams(int command)
        {
            return BitConverter.GetBytes(command);
        }
        #endregion General

        #region Login
        public static byte[] CreateCommandLogin(string password)
        {
            var data = new SLoginData {Sha256 = _Sha256.ComputeHash(Encoding.UTF8.GetBytes(password))};

            return _Serialize(CommandLogin, data);
        }

        public static bool DecodeCommandLogin(byte[] message, out SLoginData loginData)
        {
            return _TryDeserialize(message, out loginData);
        }
        #endregion Login

        #region Serializing
        private static byte[] _Serialize<T>(T obj)
        {
            return _Serialize(0, obj);
        }

        private static byte[] _Serialize<T>(int command, T obj)
        {
            byte[] cmd = BitConverter.GetBytes(command);

            var stream = new MemoryStream();
            stream.Write(cmd, 0, cmd.Length);

            string json = JsonConvert.SerializeObject(obj);
            byte[] data = _Encoder.GetBytes(json);
            stream.Write(data, 0, data.Length);
            return stream.ToArray();
        }

        private static bool _TryDeserialize<T>(byte[] message, out T obj)
        {
            obj = default(T);

            if (message == null)
                return false;

            if (message.Length < 5)
                return false;

            var data = new byte[message.Length - 4];
            Array.Copy(message, 4, data, 0, data.Length);
            try
            {
                obj = JsonConvert.DeserializeObject<T>(_Encoder.GetString(data));
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion Serializing
    }
}