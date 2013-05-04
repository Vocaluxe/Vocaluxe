using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Security.Cryptography;


namespace Vocaluxe.Base.Server
{
    public static class CCommands
    {
        public static SHA256Managed SHA256 = new SHA256Managed();

        public const int CommandLogin = 20;
        public const int ResponseLoginWrongPassword = 21;
        public const int ResponseLoginFailed = 22;
        public const int ResponseLoginOK = 23;

        public static byte[] CreateCommandWithoutParams(int Command)
        {
            return BitConverter.GetBytes(Command);
        }

        /*
        public static byte[] CreateResponseSendUserNames(SUsers Users)
        {
            return Serialize<SUsers>(ResponseSendUserNames, Users);
        }

        public static SUsers DecodeResponseSendUserNames(byte[] response)
        {
            SUsers users;
            TryDeserialize<SUsers>(response, out users);
            return users;
        }
        */

        public static byte[] CreateCommandLogin(string Password)
        {
            SLoginData data = new SLoginData();
            data.SHA256 = SHA256.ComputeHash(Encoding.UTF8.GetBytes(Password));

            return Serialize<SLoginData>(CommandLogin, data);
        }

        public static bool ResponseCommandLogin(byte[] Message, out SLoginData LoginData)
        {
            return TryDeserialize<SLoginData>(Message, out LoginData);
        }

        private static byte[] Serialize<T>(int Command, T obj)
        {
            byte[] command = BitConverter.GetBytes(Command);

            MemoryStream stream = new MemoryStream();
            stream.Write(command, 0, command.Length);

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                data = ms.ToArray();
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
                    BinaryFormatter formatter = new BinaryFormatter();
                    obj = (T)formatter.Deserialize(ms);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}
