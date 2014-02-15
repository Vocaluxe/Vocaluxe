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
        private bool _Encrypted;

        public static SendKeyEventDelegate SendKeyEvent { internal get; set; }

        #region profile
        public static GetProfileDataDelegate GetProfileData { internal get; set; }

        public static SendProfileDataDelegate SendProfileData { internal get; set; }

        public static GetProfileListDelegate GetProfileList { internal get; set; }
        #endregion

        #region photo
        public static SendPhotoDelegate SendPhoto { get; set; }
        #endregion

        #region website
        public static GetSiteFileDelegate GetSiteFile { internal get; set; }

        public static GetDelayedImageDelegate GetDelayedImage { internal get; set; }
        #endregion

        #region songs
        public static GetSongDelegate GetSong { internal get; set; }

        public static GetAllSongsDelegate GetAllSongs { internal get; set; }

        public static GetCurrentSongIdDelegate GetCurrentSongId { internal get; set; }
        #endregion

        #region playlist
        public static GetPlaylistsDelegate GetPlaylists { internal get; set; }

        public static GetPlaylistDelegate GetPlaylist { internal get; set; }

        public static AddSongToPlaylistDelegate AddSongToPlaylist { internal get; set; }

        public static RemoveSongFromPlaylistDelegate RemoveSongFromPlaylist { internal get; set; }

        public static MoveSongInPlaylistDelegate MoveSongInPlaylist { internal get; set; }

        public static PlaylistContainsSongDelegate PlaylistContainsSong { internal get; set; }

        public static GetPlaylistSongsDelegate GetPlaylistSongs { internal get; set; }

        public static RemovePlaylistDelegate RemovePlaylist { internal get; set; }

        public static AddPlaylistDelegate AddPlaylist { internal get; set; }
        #endregion

        #region user management
        public static ValidatePasswordDelegate ValidatePassword { internal get; set; }

        public static GetUserRoleDelegate GetUserRole { internal get; set; }

        public static SetUserRoleDelegate SetUserRole { internal get; set; }

        public static GetUserIdFromUsernameDelegate GetUserIdFromUsername { internal get; set; }
        #endregion

        public CServer(int port, bool encrypted)
        {
            _Init(port, encrypted);
        }

        public static string GetLocalAddress()
        {
            return Dns.GetHostName();

            /*IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress ip in ips)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            }
            return "";*/
        }

        #region server control
        private void _Init(int port, bool encrypted)
        {
            string hostname = Dns.GetHostName();
            string protocol = (encrypted) ? "https" : "http";
            _BaseAddress = new Uri(protocol + "://" + hostname + ":" + port + "/");
            _Encrypted = encrypted;
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
                wb.Security.Transport = new HttpTransportSecurity {ClientCredentialType = HttpClientCredentialType.None};
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
                    catch (CommunicationException)
                    {
                        _Host.Abort();
                    }
                }
                else
                    _Host.Abort();
            }
        }

        public void Stop()
        {
            try
            {
                _Host.Close();
            }
            catch (CommunicationException)
            {
                _Host.Abort();
            }
        }

        private void _RegisterUrlAndCert(int port)
        {
#if WIN

            ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = "VocaluxeServerConfig.exe",
                    Arguments = port + " " + (_Encrypted ? "true" : "false"),
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Verb = "runas"
                };
            Process p = Process.Start(info);
            p.WaitForExit();
            p.Close();

#else

    //Required?

#endif
        }
        #endregion
    }
}