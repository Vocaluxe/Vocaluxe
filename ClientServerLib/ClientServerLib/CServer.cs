using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Net;

namespace ClientServerLib
{
    public class CServer
    {
        private ServiceHost host;
        private Uri baseAddress;
        private bool encrypted = false;

        private static SendKeyEventDelegate sendKeyEvent;
        public static SendKeyEventDelegate SendKeyEvent
        {
            internal get { return CServer.sendKeyEvent; }
            set { CServer.sendKeyEvent = value; }
        }

        #region profile

        private static GetProfileDataDelegate getProfileData;
        public static GetProfileDataDelegate GetProfileData
        {
            internal get { return CServer.getProfileData; }
            set { CServer.getProfileData = value; }
        }

        private static SendProfileDataDelegate sendProfileData;
        public static SendProfileDataDelegate SendProfileData
        {
            internal get { return CServer.sendProfileData; }
            set { CServer.sendProfileData = value; }
        }

        private static GetProfileListDelegate getProfileList;
        public static GetProfileListDelegate GetProfileList
        {
            internal get { return CServer.getProfileList; }
            set { CServer.getProfileList = value; }
        }

        #endregion

        #region photo
        private static SendPhotoDelegate sendPhoto;
        public static SendPhotoDelegate SendPhoto
        {
            get { return CServer.sendPhoto; }
            set { CServer.sendPhoto = value; }
        }

        #endregion

        #region website

        private static GetSiteFileDelegate getSiteFile;
        public static GetSiteFileDelegate GetSiteFile
        {
            internal get { return CServer.getSiteFile; }
            set { CServer.getSiteFile = value; }
        }

        #endregion

        #region songs

        private static GetSongDelegate getSong;
        public static GetSongDelegate GetSong
        {
            internal get { return CServer.getSong; }
            set { CServer.getSong = value; }
        }

        private static GetAllSongsDelegate getAllSongs;
        public static GetAllSongsDelegate GetAllSongs
        {
            get { return CServer.getAllSongs; }
            set { CServer.getAllSongs = value; }
        }

        private static GetCurrentSongIdDelegate getCurrentSongId;
        public static GetCurrentSongIdDelegate GetCurrentSongId
        {
            get { return CServer.getCurrentSongId; }
            set { CServer.getCurrentSongId = value; }
        }


        #endregion

        public CServer(int port, bool encrypted)
        {
            init(port, encrypted);
        }

        private void init(int port, bool encrypted)
        {
            string hostname = Dns.GetHostName();
            if (encrypted)
            {
                baseAddress = new Uri("https://" + hostname + ":" + port + "/");
            }
            else
            {
                baseAddress = new Uri("http://" + hostname + ":" + port + "/");
            }
            this.encrypted = encrypted;
            host = new WebServiceHost(typeof(CWebservice), baseAddress);

            WebHttpBinding wb = new WebHttpBinding();
            wb.MaxReceivedMessageSize = 10485760;
            wb.MaxBufferSize = 10485760;
            wb.MaxBufferPoolSize = 10485760;
            wb.ReaderQuotas.MaxStringContentLength = 10485760;
            wb.ReaderQuotas.MaxArrayLength = 10485760;
            wb.ReaderQuotas.MaxBytesPerRead = 10485760;
            if (encrypted)
            {
                wb.Security.Mode = WebHttpSecurityMode.Transport;
                wb.Security.Transport = new HttpTransportSecurity { ClientCredentialType = HttpClientCredentialType.None };
            }
            host.AddServiceEndpoint(typeof(ICWebservice), wb, "");
        }

        public void Start()
        {
            try
            {
                /*ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
                smb.HttpGetEnabled = true;
                host.Description.Behaviors.Add(smb);*/

                host.Open();
            }            
            catch (CommunicationException e)
            {
                if (e is AddressAccessDeniedException || e is AddressAlreadyInUseException)
                {
                    registerUrlAndCert(baseAddress.Port);
                    try
                    {
                        host.Abort();
                        init(baseAddress.Port, encrypted);
                        host.Open();
                    }
                    catch (CommunicationException e2)
                    {
                        host.Abort();
                    }
                }
                else
                {
                    host.Abort();
                }
            }
        }

        public void Stop()
        {
            try
            {
                host.Close();
            }
            catch (CommunicationException e)
            {
                host.Abort();
            }
        }

        private void registerUrlAndCert(int port)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "WebserverInitalConfig.exe";
            info.Arguments = port.ToString() + " " + encrypted.ToString();
            info.UseShellExecute = true;
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;
            info.Verb = "runas";
            Process p = Process.Start(info);
            if (p != null)
            {
                p.WaitForExit();
                p.Close();
            }
        }
    }
}
