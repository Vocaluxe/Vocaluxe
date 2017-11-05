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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vocaluxe.Lib.Input;
using Vocaluxe.Lib.Playlist;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.Profile;
using VocaluxeLib.Songs;

namespace Vocaluxe.Base.Server
{
    static class CVocaluxeServer
    {
        private static ServiceHost _Host;
        private static Uri _BaseAddress;
        private static bool _Encrypted;
        private static readonly Queue<Task> _ServerTaskQueue = new Queue<Task>();

        private class CServerController : CControllerFramework
        {
            public override string GetName()
            {
                return "App controller";
            }

            public override void Connect() {}

            public override void Disconnect() {}

            public override bool IsConnected()
            {
                return true;
            }

            public override void SetRumble(float duration) {}
        }

        public static readonly CControllerFramework Controller = new CServerController();


        #region server control

        public static void Init()
        {
            int port = CConfig.Config.Server.ServerPort;
            bool encrypted = CConfig.Config.Server.ServerEncryption == EOffOn.TR_CONFIG_ON;
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
                ReaderQuotas = { MaxStringContentLength = 10485760, MaxArrayLength = 10485760, MaxBytesPerRead = 10485760 }
            };
            if (encrypted)
            {
                wb.Security.Mode = WebHttpSecurityMode.Transport;
                wb.Security.Transport = new HttpTransportSecurity { ClientCredentialType = HttpClientCredentialType.None };
            }
            _Host.AddServiceEndpoint(typeof(ICWebservice), wb, "");

            Start();

            //_Discover = new CDiscover(CConfig.ServerPort, CCommands.BroadcastKeyword);
        }

        public static void Start()
        {
            if (CConfig.Config.Server.ServerActive == EOffOn.TR_CONFIG_ON)
            {
                try
                {
                    _RegisterUrlAndCert(_BaseAddress.Port, false);
                    _Host.Open();
                }
                catch (CommunicationException e)
                {
                    if (e is AddressAccessDeniedException || e is AddressAlreadyInUseException)
                    {
                        _RegisterUrlAndCert(_BaseAddress.Port, true);
                        try
                        {
                            _Host.Abort();
                            Init();
                            _Host.Open();
                        }
                        catch (CommunicationException)
                        {
                            _Host.Abort();
                            MessageBox.Show("Problem while initialization of webserver. You may try a different port (Change it in config.xml)");
                        }
                    }
                    else
                        _Host.Abort();
                }
            }
        }

        public static void Close()
        {
            if (_Host != null)
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
        }

        private static void _RegisterUrlAndCert(int port, bool reserve)
        {
#if WIN

            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = "VocaluxeServerConfig.exe",
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                Arguments = AppDomain.CurrentDomain.FriendlyName + " " + port + " " + (_Encrypted ? "true" : "false") + (reserve ? " true" : ""),
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            try
            {
                using (Process p = Process.Start(info))
                {
                    p.WaitForExit();
                    if (p.ExitCode != 0)
                        MessageBox.Show("Registering the Server failed (Code " + p.ExitCode + ")!\r\nThe Server might not work correctly.");
                    p.Close();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Registering the Server failed (" + e + ")!\r\nThe Server might not work correctly.");
            }
#else

    //Required?

#endif
        }

        public static string GetServerAddress()
        {
            return _BaseAddress == null ? "" : _BaseAddress.AbsoluteUri;
        }

        public static bool IsServerRunning()
        {
            if (_Host == null)
                return false;

            return _Host.State == CommunicationState.Opened;
        }

        #endregion

        #region task control

        public static void ProcessServerTasks()
        {
            //Serial processing - one by one
            while (_ServerTaskQueue.Count > 0)
            {
                //Get a task from the queue
                Task task = _ServerTaskQueue.Dequeue();
                //Start the task
                task.RunSynchronously(TaskScheduler.FromCurrentSynchronizationContext());
            }
        }


        public static TReturnType DoTask<TReturnType>(Func<TReturnType> action)
        {
            var task = new Task<TReturnType>(action);
            _ServerTaskQueue.Enqueue(task);
            task.Wait(); //wait until the task is completed
            return task.Result;
        }

        public static TReturnType DoTask<TReturnType, TParameterType>(Func<TParameterType, TReturnType> action, TParameterType parameter)
        {
            var task = new Task<TReturnType>(() => action(parameter));
            _ServerTaskQueue.Enqueue(task);
            task.Wait(); //wait until the task is completed
            return task.Result;
        }

        public static TReturnType DoTask<TReturnType, TParameterType1, TParameterType2>(Func<TParameterType1, TParameterType2, TReturnType> action, TParameterType1 parameter1, TParameterType2 parameter2)
        {
            var task = new Task<TReturnType>(() => action(parameter1, parameter2));
            _ServerTaskQueue.Enqueue(task);
            task.Wait(); //wait until the task is completed
            return task.Result;
        }

        public static TReturnType DoTask<TReturnType, TParameterType1, TParameterType2, TParameterType3>(Func<TParameterType1, TParameterType2, TParameterType3, TReturnType> action, TParameterType1 parameter1, TParameterType2 parameter2, TParameterType3 parameter3)
        {
            var task = new Task<TReturnType>(() => action(parameter1, parameter2, parameter3));
            _ServerTaskQueue.Enqueue(task);
            task.Wait(); //wait until the task is completed
            return task.Result;
        }

        public static TReturnType DoTask<TReturnType, TParameterType1, TParameterType2, TParameterType3, TParameterType4>(Func<TParameterType1, TParameterType2, TParameterType3, TParameterType4, TReturnType> action, TParameterType1 parameter1, TParameterType2 parameter2, TParameterType3 parameter3, TParameterType4 parameter4)
        {
            var task = new Task<TReturnType>(() => action(parameter1, parameter2, parameter3, parameter4));
            _ServerTaskQueue.Enqueue(task);
            task.Wait(); //wait until the task is completed
            return task.Result;
        }

        
        public static void DoTaskWithoutReturn(Action action)
        {
            var task = new Task(action);
            _ServerTaskQueue.Enqueue(task);
            task.Wait(); //wait until the task is completed
        }

        public static void DoTaskWithoutReturn<TParameterType>(Action<TParameterType> action, TParameterType parameter)
        {
            var task = new Task(() => action(parameter));
            _ServerTaskQueue.Enqueue(task);
            task.Wait(); //wait until the task is completed
        }

        public static void DoTaskWithoutReturn<TParameterType1, TParameterType2>(Action<TParameterType1, TParameterType2> action, TParameterType1 parameter1, TParameterType2 parameter2)
        {
            var task = new Task(() => action(parameter1, parameter2));
            _ServerTaskQueue.Enqueue(task);
            task.Wait(); //wait until the task is completed
        }

        public static void DoTaskWithoutReturn<TParameterType1, TParameterType2, TParameterType3>(Action<TParameterType1, TParameterType2, TParameterType3> action, TParameterType1 parameter1, TParameterType2 parameter2, TParameterType3 parameter3)
        {
            var task = new Task(() => action(parameter1, parameter2, parameter3));
            _ServerTaskQueue.Enqueue(task);
            task.Wait(); //wait until the task is completed
        }

        public static void DoTaskWithoutReturn<TParameterType1, TParameterType2, TParameterType3, TParameterType4>(Action<TParameterType1, TParameterType2, TParameterType3, TParameterType4> action, TParameterType1 parameter1, TParameterType2 parameter2, TParameterType3 parameter3, TParameterType4 parameter4)
        {
            var task = new Task(() => action(parameter1, parameter2, parameter3, parameter4));
            _ServerTaskQueue.Enqueue(task);
            task.Wait(); //wait until the task is completed
        }

        #endregion

        public static bool SendKeyEvent(string key)
        {
            bool result = false;
            string lowerKey = key.ToLower();

            if (!string.IsNullOrEmpty(lowerKey))
            {
                switch (lowerKey)
                {
                    case "up":
                        Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Up));
                        result = true;
                        break;
                    case "down":
                        Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Down));
                        result = true;
                        break;
                    case "left":
                        Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Left));
                        result = true;
                        break;
                    case "right":
                        Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Right));
                        result = true;
                        break;
                    case "escape":
                        Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Escape));
                        result = true;
                        break;
                    case "return":
                        Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Return));
                        result = true;
                        break;
                    case "tab":
                        Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Tab));
                        result = true;
                        break;
                    case "backspace":
                        Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, Keys.Back));
                        result = true;
                        break;
                    default:
                        if (lowerKey.StartsWith("f"))
                        {
                            string numberString = lowerKey.Substring(1);
                            int number;
                            Keys fKey;

                            if (Int32.TryParse(numberString, out number) && number >= 1
                                && number <= 12
                                && Enum.TryParse("F" + number, true, out fKey))
                            {
                                Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, Char.MinValue, fKey));
                                result = true;
                            }
                        }
                        break;
                }
            }

            return result;
        }

        public static bool SendKeyStringEvent(string keyString, bool isShiftPressed, bool isAltPressed, bool isCtrlPressed)
        {
            bool result = false;

            foreach (char key in keyString)
            {
                Controller.AddKeyEvent(new SKeyEvent(ESender.Keyboard, isAltPressed,
                                                     Char.IsUpper(key) || isShiftPressed,
                                                     isCtrlPressed, true,
                                                     isShiftPressed ? Char.ToUpper(key) : key,
                                                     _ParseKeys(key)));
                result = true;
            }

            return result;
        }

        private static Keys _ParseKeys(char keyText)
        {
            Keys key;

            if (!Enum.TryParse(keyText.ToString(), true, out key))
            {
                switch (keyText)
                {
                    case ' ':
                        key = Keys.Space;
                        break;
                    default:
                        key = Keys.None;
                        break;
                }
            }

            return key;
        }

        #region profile
        public static SProfileData GetProfileData(Guid profileId, bool isReadonly)
        {
            CProfile profile = CProfiles.GetProfile(profileId);
            if (profile == null)
                return new SProfileData();
            return _CreateProfileData(profile, isReadonly);
        }

        public static bool SendProfileData(SProfileData profile)
        {
            CProfile newProfile;
            CProfile existingProfile = CProfiles.GetProfile(profile.ProfileId);

            if (existingProfile != null)
            {
                newProfile = new CProfile
                    {
                        ID = existingProfile.ID,
                        FilePath = existingProfile.FilePath,
                        Active = existingProfile.Active,
                        Avatar = existingProfile.Avatar,
                        Difficulty = existingProfile.Difficulty,
                        UserRole = existingProfile.UserRole,
                        PlayerName = existingProfile.PlayerName
                    };
            }
            else
            {
                newProfile = new CProfile
                    {
                        Active = EOffOn.TR_CONFIG_ON,
                        UserRole = EUserRole.TR_USERROLE_NORMAL
                    };
            }

            if (profile.Avatar != null)
                newProfile.Avatar = _AddAvatar(profile.Avatar);
            else if (newProfile.Avatar == null || newProfile.Avatar.ID == -1)
            {
                newProfile.Avatar = CProfiles.GetAvatars().First();

                /*CAvatar avatar = new CAvatar(-1);
                avatar.LoadFromFile("Profiles\\Avatar_f.png");
                CProfiles.AddAvatar(avatar);
                newProfile.Avatar = avatar;*/
            }

            if (!string.IsNullOrEmpty(profile.PlayerName))
                newProfile.PlayerName = profile.PlayerName;
            else if (!string.IsNullOrEmpty(newProfile.PlayerName))
                newProfile.PlayerName = "DummyName";

            if (profile.Difficulty >= 0 && profile.Difficulty <= 2)
                newProfile.Difficulty = (EGameDifficulty)profile.Difficulty;

            if (profile.Type >= 0 && profile.Type <= 1)
            {
                EUserRole option = profile.Type == 0 ? EUserRole.TR_USERROLE_GUEST : EUserRole.TR_USERROLE_NORMAL;
                //Only allow the change of TR_USERROLE_GUEST and TR_USERROLE_NORMAL
                const EUserRole mask = EUserRole.TR_USERROLE_NORMAL;
                newProfile.UserRole = (newProfile.UserRole & mask) | option;
            }

            if (!string.IsNullOrEmpty(profile.Password))
            {
                if (profile.Password == "***__CLEAR_PASSWORD__***")
                {
                    newProfile.PasswordSalt = null;
                    newProfile.PasswordHash = null;
                }
                else
                {
                    RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                    byte[] buffer = new byte[32];
                    rng.GetNonZeroBytes(buffer);
                    byte[] salt = buffer;
                    byte[] hashedPassword = _Hash((new UTF8Encoding()).GetBytes(profile.Password), salt);

                    newProfile.PasswordSalt = salt;
                    newProfile.PasswordHash = hashedPassword;
                }
            }

            if (existingProfile != null)
            {
                CProfiles.EditProfile(newProfile);
                CProfiles.Update();
                CProfiles.SaveProfiles();
            }
            else
                CProfiles.AddProfile(newProfile);

            return true;
        }

        public static SProfileData[] GetProfileList()
        {
            List<SProfileData> result = new List<SProfileData>(CProfiles.NumProfiles);

            result.AddRange(CProfiles.GetProfiles().Select(profile => _CreateProfileData(profile, true)));

            return result.ToArray();
        }

        private static SProfileData _CreateProfileData(CProfile profile, bool isReadonly)
        {
            SProfileData profileData = new SProfileData
                {
                    IsEditable = !isReadonly,
                    ProfileId = profile.ID,
                    PlayerName = profile.PlayerName,
                    //Is TR_USERROLE_GUEST or TR_USERROLE_NORMAL?
                    Type = (profile.UserRole.HasFlag(EUserRole.TR_USERROLE_NORMAL) ? 1 : 0),
                    Difficulty = (int)profile.Difficulty
                };

            CAvatar avatar = profile.Avatar;
            if (avatar != null)
            {
                if (File.Exists(avatar.FileName))
                    profileData.Avatar = new CBase64Image(_CreateDelayedImage(avatar.FileName));
            }
            return profileData;
        }

        private static CAvatar _AddAvatar(CBase64Image avatarData)
        {
            try
            {
                string filename = _SaveImage(avatarData, "snapshot", CConfig.ProfileFolders[0]);

                CAvatar avatar = CAvatar.GetAvatar(filename);
                if (avatar != null)
                {
                    CProfiles.AddAvatar(avatar);
                    return avatar;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
        #endregion

        #region photo
        private static readonly List<string> _PhotosOfThisRound = new List<string>();

        public static bool SendPhoto(SPhotoData photoData)
        {
            if (photoData.Photo == null)
                return false;

            string name = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filePath = _SaveImage(photoData.Photo, name, CSettings.FolderNamePhotos);
            if (!string.IsNullOrEmpty(filePath))
            {
                _PhotosOfThisRound.Add(filePath);
                return true;
            }

            return false;
        }

        internal static string[] GetPhotosOfThisRound()
        {
            string[] result = _PhotosOfThisRound.ToArray();
            _PhotosOfThisRound.Clear();
            return result;
        }
        #endregion

        #region website
        private static readonly Dictionary<string, string> _DelayedImagePath = new Dictionary<string, string>();

        public static byte[] GetSiteFile(string filename)
        {
            string path = "Website/" + filename;
            path = path.Replace("..", "");

            if (!File.Exists(path))
                return null;

            /*string content = File.ReadAllText(path);

            content = content.Replace("%SERVER%", System.Net.Dns.GetHostName() + ":" + CConfig.ServerPort);


            return Encoding.UTF8.GetBytes(content);*/

            return File.ReadAllBytes(path);
        }

        private static string _CreateDelayedImage(string filename)
        {
            byte[] by = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(filename));
            var sb = new StringBuilder();
            foreach (byte b in by)
                sb.Append(b.ToString("x2"));

            string hashedFilename = sb.ToString();

            if (!_DelayedImagePath.ContainsKey(hashedFilename))
                _DelayedImagePath.Add(hashedFilename, filename);
            return hashedFilename;
        }

        public static string GetServerVersion()
        {
            return Application.ProductVersion;
        }

        public static CBase64Image GetDelayedImage(string hashedFilename)
        {
            if (!_DelayedImagePath.ContainsKey(hashedFilename))
                throw new FileNotFoundException("Image not found");

            string fileName = _DelayedImagePath[hashedFilename];

            if (File.Exists(fileName))
            {
                Image image = Image.FromFile(fileName);
                return new CBase64Image(image, image.RawFormat);
            }
            throw new FileNotFoundException("Image not found");
        }
        #endregion

        #region songs
        public static SSongInfo GetSong(int songId)
        {
            CSong song = CSongs.GetSong(songId);
            return _GetSongInfo(song, true);
        }


        private static SSongInfo[] _SongInfoCache = null;
        public static SSongInfo[] GetAllSongs()
        {
            bool sendCovers = CConfig.Config.Server.SongCountCoverThreshold == -1 || CConfig.Config.Server.SongCountCoverThreshold > CSongs.Songs.Count;

            if (_SongInfoCache == null)
            {
                List<CSong> songs = CSongs.Songs;
                _SongInfoCache = (from s in songs
                    select _GetSongInfo(s, sendCovers)).AsParallel().ToArray<SSongInfo>();
            }
            
            return _SongInfoCache;
        }

        public static string GetMp3Path(int songId)
        {
            CSong song = CSongs.GetSong(songId);
            return song.GetMP3();
        }

        public static int GetCurrentSongId()
        {
            CSong song = CGame.GetSong();
            if (song == null)
                return -1;
            return song.ID;
        }

        private static SSongInfo _GetSongInfo(CSong song, bool includeCover)
        {
            SSongInfo result = new SSongInfo();
            if (song != null)
            {
                result.Title = song.Title;
                result.Artist = song.Artist;
                result.Genre = song.Genres.FirstOrDefault();
                result.Language = song.Languages.FirstOrDefault();
                result.Year = song.Year;
                result.IsDuet = song.IsDuet;
                result.SongId = song.ID;
                if (includeCover)
                {
                    if (song.CoverFileName == "")
                    {
                        result.Cover = new CBase64Image(_CreateDelayedImage("Website\\img\\noCover.png"));
                    }
                    else
                    {
                        result.Cover = new CBase64Image(_CreateDelayedImage(song.Folder + "\\" + song.CoverFileName));
                    }
                }
                    
            }
            return result;
        }
        #endregion

        #region playlist
        public static SPlaylistData[] GetPlaylists()
        {
            return (from p in CPlaylists.Playlists
                    select _GetPlaylistInfo(p)).ToArray();
        }

        public static SPlaylistData GetPlaylist(int playlistId)
        {
            if (CPlaylists.Get(playlistId) == null)
                throw new ArgumentException("invalid playlistId");
            return _GetPlaylistInfo(CPlaylists.Get(playlistId));
        }

        public static void AddSongToPlaylist(int songId, int playlistId, bool allowDuplicates)
        {
            if (CPlaylists.Get(playlistId) == null)
                throw new ArgumentException("invalid playlistId");

            if (allowDuplicates || !PlaylistContainsSong(songId, playlistId))
            {
                CPlaylists.AddSong(playlistId, songId);
                CPlaylists.Save(playlistId);
            }
            else
                throw new ArgumentException("song exists in this playlist");
        }

        public static void RemoveSongFromPlaylist(int position, int playlistId, int songId)
        {
            CPlaylistFile pl = CPlaylists.Get(playlistId);
            if (pl == null)
                throw new ArgumentException("invalid playlistId");
            if (!PlaylistContainsSong(songId, playlistId))
                throw new ArgumentException("invalid songId");
            if (position < 0 || pl.Songs.Count <= position
                || pl.Songs[position].SongID != songId)
                throw new ArgumentException("invalid position");
            pl.DeleteSong(position);
            pl.Save();
        }

        public static void MoveSongInPlaylist(int newPosition, int playlistId, int songId)
        {
            CPlaylistFile pl = CPlaylists.Get(playlistId);
            if (pl == null)
                throw new ArgumentException("invalid playlistId");
            if (!PlaylistContainsSong(songId, playlistId))
                throw new ArgumentException("invalid songId");

            if (pl.Songs.Count < newPosition)
                throw new ArgumentException("invalid newPosition");

            int oldPosition = pl.Songs.FindIndex(s => s.SongID == songId);
            pl.MoveSong(oldPosition, newPosition);
            pl.Save();
        }

        public static bool PlaylistContainsSong(int songId, int playlistId)
        {
            CPlaylistFile pl = CPlaylists.Get(playlistId);
            if (pl == null)
                throw new ArgumentException("invalid playlistId");
            return pl.Songs.Any(s => s.SongID == songId);
        }

        public static SPlaylistSongInfo[] GetPlaylistSongs(int playlistId)
        {
            CPlaylistFile pl = CPlaylists.Get(playlistId);
            if (pl == null)
                throw new ArgumentException("invalid playlistId");

            return _GetPlaylistSongInfos(pl);
        }

        private static SPlaylistSongInfo _GetPlaylistSongInfo(CPlaylistSong playlistSong, int playlistId, int playlistPos)
        {
            SPlaylistSongInfo result = new SPlaylistSongInfo();
            if (playlistSong != null)
            {
                result.PlaylistId = playlistId;
                result.GameMode = (int)playlistSong.GameMode;
                result.PlaylistPosition = playlistPos;
                result.Song = _GetSongInfo(CSongs.GetSong(playlistSong.SongID), true);
            }
            return result;
        }

        private static SPlaylistSongInfo[] _GetPlaylistSongInfos(CPlaylistFile playlist)
        {
            SPlaylistSongInfo[] result = new SPlaylistSongInfo[playlist.Songs.Count];
            for (int i = 0; i < playlist.Songs.Count; i++)
                result[i] = _GetPlaylistSongInfo(playlist.Songs[i], playlist.Id, i);
            return result;
        }

        private static SPlaylistData _GetPlaylistInfo(CPlaylistFile playlist)
        {
            return new SPlaylistData
                {
                    PlaylistId = playlist.Id,
                    PlaylistName = playlist.Name,
                    SongCount = playlist.Songs.Count,
                    LastChanged = DateTime.Now.ToLongDateString()
                };
        }

        public static void RemovePlaylist(int playlistId)
        {
            if (CPlaylists.Get(playlistId) == null)
                throw new ArgumentException("invalid playlistId");
            CPlaylists.Delete(playlistId);
        }

        public static int AddPlaylist(string playlistName)
        {
            int newPlaylistId = CPlaylists.NewPlaylist(playlistName);
            CPlaylists.Save(newPlaylistId);

            return newPlaylistId;
        }
        #endregion

        #region user management
        public static bool ValidatePassword(Guid profileId, string password)
        {
            CProfile profile = CProfiles.GetProfile(profileId);
            if (profile == null)
                throw new ArgumentException("Invalid profileId");

            if (profile.PasswordHash == null)
            {
                if (string.IsNullOrEmpty(password))
                    return true; //Allow emty passwords
                return false;
            }

            byte[] salt = profile.PasswordSalt;
            return _Hash((new UTF8Encoding()).GetBytes(password), salt).SequenceEqual(profile.PasswordHash);
        }

        public static bool ValidatePassword(Guid profileId, byte[] hashedPassword)
        {
            CProfile profile = CProfiles.GetProfile(profileId);
            if (profile == null)
                throw new ArgumentException("Invalid profileId");

            if (profile.PasswordHash == null)
            {
                if (hashedPassword == null)
                    return true; //Allow emty passwords
                return false;
            }

            //byte[] salt = profile.PasswordSalt;
            return hashedPassword.SequenceEqual(profile.PasswordHash);
        }

        private static byte[] _GetPasswordSalt(Guid profileId)
        {
            CProfile profile = CProfiles.GetProfile(profileId);
            if (profile == null)
                throw new ArgumentException("Invalid profileId");

            if (profile.PasswordHash == null)
                throw new ArgumentException("Emty password");

            return profile.PasswordSalt;
        }

        public static int GetUserRole(Guid profileId)
        {
            CProfile profile = CProfiles.GetProfile(profileId);
            if (profile == null)
                throw new ArgumentException("Invalid profileId");

            //Hide TR_USERROLE_GUEST and TR_USERROLE_NORMAL
            //const EUserRole mask = (EUserRole.TR_USERROLE_GUEST | EUserRole.TR_USERROLE_NORMAL);

            return (int)(profile.UserRole);
        }

        public static void SetUserRole(Guid profileId, int userRole)
        {
            CProfile profile = CProfiles.GetProfile(profileId);
            if (profile == null)
                throw new ArgumentException("Invalid profileId");

            var option = (EUserRole)userRole;

            //Only allow the change of all options exept TR_USERROLE_GUEST and TR_USERROLE_NORMAL
            const EUserRole mask = (EUserRole.TR_USERROLE_GUEST | EUserRole.TR_USERROLE_NORMAL);
            option &= ~mask;

            profile.UserRole = (profile.UserRole & mask) | option;

            CProfiles.EditProfile(profile);
            CProfiles.Update();
            CProfiles.SaveProfiles();
        }

        public static Guid GetUserIdFromUsername(string username)
        {
            IEnumerable<Guid> playerIds = (from p in CProfiles.GetProfiles()
                                          where String.Equals(p.PlayerName, username, StringComparison.OrdinalIgnoreCase)
                                          select p.ID);
            try
            {
                return playerIds.First();
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException("Invalid playername");
            }
        }

        private static byte[] _Hash(byte[] password, byte[] salt)
        {
            HashAlgorithm hashAlgo = new SHA256Managed();

            byte[] data = new byte[password.Length + salt.Length];

            password.CopyTo(data, 0);
            salt.CopyTo(data, password.Length);

            return hashAlgo.ComputeHash(data);
        }
        #endregion

        private static string _SaveImage(CBase64Image imageDate, string name, string folder)
        {
            Image avatarImage = imageDate.GetImage();
            string extension = imageDate.GetImageType();

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string file = CHelper.GetUniqueFileName(folder, name + "." + extension);

            avatarImage.Save(file);
            return file;
        }
    }
}