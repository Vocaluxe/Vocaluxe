using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using DiffieHellman;

namespace ClientServerLib
{
    internal class CConnection
    {
        private bool keySet;
        private bool encryption;
        private CDiffieHellman dh;
        private UTF8Encoding encoder;
        private int connectionID;

        public TcpClient TcpClient;

        public bool KeySet
        {
            get { return keySet; }
        }

        public int ConnectionID
        {
            get { return connectionID; }
        }

        public CConnection(TcpClient client, int ConnectionID, bool Encryption = false)
        {
            TcpClient = client;
            client.NoDelay = true;
            keySet = false;
            encryption = Encryption;
            connectionID = ConnectionID;
            encoder = new UTF8Encoding();
        }

        public byte[] GetKeyParams()
        {
            if (!encryption)
            {
                return encoder.GetBytes("NO ENCRYPTION");
            }

            if (dh == null)
                dh = new CDiffieHellman(256).GenerateRequest();

            return encoder.GetBytes(dh.ToString());
        }

        public byte[] CreateClientKey(byte[] serverParam)
        {
            if (serverParam == null)
                return null;

            string response = encoder.GetString(serverParam, 0, serverParam.Length);
            if (response == "NO ENCRYPTION")
            {
                encryption = false;
                keySet = true;
                return encoder.GetBytes(response);
            }

            dh = new CDiffieHellman(256).GenerateResponse(response);

            keySet = true;
            encryption = true;
            return encoder.GetBytes(dh.ToString());
        }

        public void CreateServerKey(byte[] clientResponse)
        {
            string response = encoder.GetString(clientResponse, 0, clientResponse.Length);

            if (response == "NO ENCRYPTION")
            {
                keySet = true;
                return;
            }

            if (dh == null)
                return;

            dh.HandleResponse(response);
            keySet = true;
        }

        public byte[] Encrypt(byte[] Data)
        {
            if (Data == null || (!keySet && encryption))
                return Data;

            byte[] dataLength = BitConverter.GetBytes(Data.Length);

            if (!encryption)
            {
                int len = 4 + dataLength.Length + Data.Length;
                byte[] messageLength = BitConverter.GetBytes(len);

                using (var stream = new MemoryStream())
                {
                    stream.Write(messageLength, 0, messageLength.Length);
                    stream.Write(dataLength, 0, dataLength.Length);
                    stream.Write(Data, 0, Data.Length);

                    return stream.ToArray();
                }
            }

            using (Aes aes = new AesManaged())
            {
                aes.Key = dh.Key;
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var memoryStream = new MemoryStream())
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(Data, 0, Data.Length);
                    cryptoStream.FlushFinalBlock();

                    byte[] encrypted = memoryStream.ToArray();

                    int len = 4 + aes.IV.Length + dataLength.Length + encrypted.Length;
                    byte[] messageLength = BitConverter.GetBytes(len);

                    using (var stream = new MemoryStream())
                    {
                        stream.Write(messageLength, 0, messageLength.Length);
                        stream.Write(aes.IV, 0, aes.IV.Length);
                        stream.Write(dataLength, 0, dataLength.Length);
                        stream.Write(encrypted, 0, encrypted.Length);

                        return stream.ToArray();
                    }
                }
            }
        }

        public byte[] Decrypt(byte[] Data)
        {
            if (Data == null || (!keySet && encryption))
                return Data;

            
            int messageLength = BitConverter.ToInt32(Data, 0);
            if (messageLength > Data.Length)
                return null;

            int dataLength;

            if (!encryption)
            {
                dataLength = BitConverter.ToInt32(Data, 4);

                byte[] message = new byte[dataLength];
                Array.Copy(Data, 8, message, 0, dataLength);
                return message;
            }
            
            if (Data.Length < 25)
                return null;

            byte[] IV = new byte[16];
            Array.Copy(Data, 4, IV, 0, 16);

            dataLength = BitConverter.ToInt32(Data, 20);

            using (Aes aes = new AesManaged())
            {
                aes.Key = dh.Key;
                aes.IV = IV;

                byte[] encrypted = new byte[messageLength - 24];
                Array.Copy(Data, 24, encrypted, 0, encrypted.Length);

                try
                {
                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var memoryStream = new MemoryStream())
                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(encrypted, 0, encrypted.Length);
                        cryptoStream.FlushFinalBlock();
                        cryptoStream.Close();

                        byte[] decrypted = memoryStream.ToArray();
                        byte[] message = new byte[dataLength];
                        Array.Copy(decrypted, message, dataLength);

                        return message;
                    }
                }
                catch 
                {
                    return null;
                }
            }
        }
    }
}
