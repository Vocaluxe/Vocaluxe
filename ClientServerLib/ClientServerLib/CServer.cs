using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Diagnostics;
using System.Net;

namespace ServerLib
{
    public class CServer
    {
        private ServiceHost _Host;
        private Uri _BaseAddress;
        private bool _Encrypted = false;

        private static SendKeyEventDelegate _SendKeyEvent;
        public static SendKeyEventDelegate SendKeyEvent
        {
            internal get { return CServer._SendKeyEvent; }
            set { CServer._SendKeyEvent = value; }
        }

        #region profile

        private static GetProfileDataDelegate _GetProfileData;
        public static GetProfileDataDelegate GetProfileData
        {
            internal get { return CServer._GetProfileData; }
            set { CServer._GetProfileData = value; }
        }

        private static SendProfileDataDelegate _SendProfileData;
        public static SendProfileDataDelegate SendProfileData
        {
            internal get { return CServer._SendProfileData; }
            set { CServer._SendProfileData = value; }
        }

        private static GetProfileListDelegate _GetProfileList;
        public static GetProfileListDelegate GetProfileList
        {
            internal get { return CServer._GetProfileList; }
            set { CServer._GetProfileList = value; }
        }

        #endregion

        #region photo
        private static SendPhotoDelegate _SendPhoto;
        public static SendPhotoDelegate SendPhoto
        {
            get { return CServer._SendPhoto; }
            set { CServer._SendPhoto = value; }
        }

        #endregion

        #region website

        private static GetSiteFileDelegate _GetSiteFile;
        public static GetSiteFileDelegate GetSiteFile
        {
            internal get { return CServer._GetSiteFile; }
            set { CServer._GetSiteFile = value; }
        }

        private static GetDelayedImageDelegate _GetDelayedImage;
        public static GetDelayedImageDelegate GetDelayedImage
        {
            internal get { return CServer._GetDelayedImage; }
            set { CServer._GetDelayedImage = value; }
        }

        #endregion

        #region songs

        private static GetSongDelegate _GetSong;
        public static GetSongDelegate GetSong
        {
            internal get { return CServer._GetSong; }
            set { CServer._GetSong = value; }
        }

        private static GetAllSongsDelegate _GetAllSongs;
        public static GetAllSongsDelegate GetAllSongs
        {
            get { return CServer._GetAllSongs; }
            set { CServer._GetAllSongs = value; }
        }

        private static GetCurrentSongIdDelegate _GetCurrentSongId;
        public static GetCurrentSongIdDelegate GetCurrentSongId
        {
            get { return CServer._GetCurrentSongId; }
            set { CServer._GetCurrentSongId = value; }
        }


        #endregion

        #region user management

        private static SetPasswordDelegate _SetPassword;
        public static SetPasswordDelegate SetPassword
        {
            internal get { return CServer._SetPassword; }
            set { CServer._SetPassword = value; }
        }

        private static ValidatePasswordDelegate _ValidatePassword;
        public static ValidatePasswordDelegate ValidatePassword
        {
            internal get { return CServer._ValidatePassword; }
            set { CServer._ValidatePassword = value; }
        }

        private static GetUserRoleDelegate _GetUserRole;
        public static GetUserRoleDelegate GetUserRole
        {
            internal get { return CServer._GetUserRole; }
            set { CServer._GetUserRole = value; }
        }

        private static SetUserRoleDelegate _SetUserRole;
        public static SetUserRoleDelegate SetUserRole
        {
            internal get { return CServer._SetUserRole; }
            set { CServer._SetUserRole = value; }
        }

        private static GetUserIdFromUsernameDelegate _GetUserIdFromUsername;
        public static GetUserIdFromUsernameDelegate GetUserIdFromUsername
        {
            internal get { return CServer._GetUserIdFromUsername; }
            set { CServer._GetUserIdFromUsername = value; }
        }

        #endregion

        public CServer(int port, bool encrypted)
        {
            _Init(port, encrypted);
        }

        #region server control

        private void _Init(int port, bool encrypted)
        {
            string hostname = Dns.GetHostName();
            if (encrypted)
            {
                _BaseAddress = new Uri("https://" + hostname + ":" + port + "/");
            }
            else
            {
                _BaseAddress = new Uri("http://" + hostname + ":" + port + "/");
            }
            this._Encrypted = encrypted;
            _Host = new WebServiceHost(typeof(CWebservice), _BaseAddress);

            WebHttpBinding wb = new WebHttpBinding
            {
                MaxReceivedMessageSize = 10485760,
                MaxBufferSize = 10485760,
                MaxBufferPoolSize = 10485760,
                ReaderQuotas = {MaxStringContentLength = 10485760, MaxArrayLength = 10485760, MaxBytesPerRead = 10485760}
            };
            if (encrypted)
            {
                wb.Security.Mode = WebHttpSecurityMode.Transport;
                wb.Security.Transport = new HttpTransportSecurity { ClientCredentialType = HttpClientCredentialType.None };
            }
            _Host.AddServiceEndpoint(typeof(ICWebservice), wb, "");
        }

        public void Start()
        {
            try
            {
                /*ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
                smb.HttpGetEnabled = true;
                host.Description.Behaviors.Add(smb);*/

                _Host.Open();
            }
            catch (CommunicationException e)
            {
                if (e is AddressAccessDeniedException || e is AddressAlreadyInUseException)
                {
                    _RegisterUrlAndCert(_BaseAddress.Port);
                    try
                    {
                        _Host.Abort();
                        _Init(_BaseAddress.Port, _Encrypted);
                        _Host.Open();
                    }
                    catch (CommunicationException e2)
                    {
                        _Host.Abort();
                    }
                }
                else
                {
                    _Host.Abort();
                }
            }
        }

        public void Stop()
        {
            try
            {
                _Host.Close();
            }
            catch (CommunicationException e)
            {
                _Host.Abort();
            }
        }

        private void _RegisterUrlAndCert(int port)
        {
#if WIN

            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = "WebserverInitalConfig.exe",
                Arguments = port.ToString() + " " + _Encrypted.ToString(),
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas"
            };
            Process p = Process.Start(info);
            if (p != null)
            {
                p.WaitForExit();
                p.Close();
            }

#else

            //Required?

#endif
        }

        #endregion

    }
}
