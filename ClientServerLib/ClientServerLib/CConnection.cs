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

        public CConnection(TcpClient client, int ConnectionID)
        {
            TcpClient = client;
            keySet = false;
            connectionID = ConnectionID;
            encoder = new UTF8Encoding();
        }

        public byte[] GetKeyParams()
        {
            if (dh == null)
                dh = new CDiffieHellman(256).GenerateRequest();

            return encoder.GetBytes(dh.ToString());
        }

        public byte[] CreateClientKey(byte[] serverParam)
        {
            if (serverParam == null)
                return null;

            dh = new CDiffieHellman(256).GenerateResponse(
                encoder.GetString(serverParam, 0, serverParam.Length));

            keySet = true;
            return encoder.GetBytes(dh.ToString());
        }

        public void CreateServerKey(byte[] clientResponse)
        {
            if (dh == null)
                return;

            dh.HandleResponse(encoder.GetString(clientResponse, 0, clientResponse.Length));
            keySet = true;
        }

        public byte[] Encrypt(byte[] Data)
        {
            if (Data == null || !keySet)
                return Data;

            using (Aes aes = new AesManaged())
            {
                aes.Key = dh.Key;
                aes.GenerateIV();

                byte[] dataLength = BitConverter.GetBytes(Data.Length);

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var memoryStream = new MemoryStream())
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(Data, 0, Data.Length);
                    cryptoStream.FlushFinalBlock();

                    byte[] encrypted = memoryStream.ToArray();

                    int len = 4 + aes.IV.Length + dataLength.Length + encrypted.Length;
                    byte[] messageLength = BitConverter.GetBytes(len);

                    MemoryStream stream = new MemoryStream();
                    stream.Write(messageLength, 0, messageLength.Length);
                    stream.Write(aes.IV, 0, aes.IV.Length);
                    stream.Write(dataLength, 0, dataLength.Length);
                    stream.Write(encrypted, 0, encrypted.Length);

                    return stream.ToArray();
                }
            }
        }

        public byte[] Decrypt(byte[] Data)
        {
            if (Data == null || !keySet)
                return Data;

            if (Data.Length < 25)
                return null;

            int messageLength = BitConverter.ToInt32(Data, 0);
            if (messageLength > Data.Length)
                return null;

            byte[] IV = new byte[16];
            Array.Copy(Data, 4, IV, 0, 16);
            int dataLength = BitConverter.ToInt32(Data, 20);

            using (Aes aes = new AesManaged())
            {
                aes.Key = dh.Key;
                aes.IV = IV;

                byte[] encrypted = new byte[messageLength - 24];
                Array.Copy(Data, 24, encrypted, 0, encrypted.Length);

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
        }
    }
}
