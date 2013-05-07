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
using System.IO;
using System.Text;
using System.Xml;
using VocaluxeLib;
using VocaluxeLib.Game;
using VocaluxeLib.Profile;

namespace Vocaluxe.Base
{
    

    static class CProfiles
    {
        private static Dictionary<int, CProfile> _Profiles;
        private static Queue<int> _ProfileIDs;
        private static Object _ProfileMutex = new Object();

        private static Dictionary<int, CAvatar> _Avatars;
        private static Queue<int> _AvatarIDs;
        private static Object _AvatarMutex = new Object();

        public delegate void ProfilesChangedNotification();
        private static List<ProfilesChangedNotification> _Callbacks;

        public static int NumProfiles
        {
            get { return _Profiles.Count; }
        }

        public static int NumAvatars
        {
            get { return _Avatars.Count; }
        }

        public static void Init()
        {
            _Avatars = new Dictionary<int, CAvatar>();
            _AvatarIDs = new Queue<int>(1000);
            for (int i = 0; i < 1000; i++)
            {
                _AvatarIDs.Enqueue(i);
            }

            _Profiles = new Dictionary<int, CProfile>();
            _ProfileIDs = new Queue<int>(100);
            for (int i = 0; i < 100; i++)
            {
                _ProfileIDs.Enqueue(i);
            }

            _Callbacks = new List<ProfilesChangedNotification>();
            
            LoadProfiles();
        }

        public static void AddNotificationCallback(ProfilesChangedNotification Callback)
        {
            if (Callback == null)
                return;

            lock (_Callbacks)
            {
                _Callbacks.Add(Callback);
            }
        }

        public static CProfile[] GetProfiles()
        {
            List<CProfile> list = new List<CProfile>();

            lock (_ProfileMutex)
            {
                if (_Profiles.Count == 0)
                    return null;          

                CProfile[] result = new CProfile[_Profiles.Count];
                _Profiles.Values.CopyTo(result, 0);
                for (int i = 0; i < result.Length; i++)
                {
                    list.Add(result[i]);
                }
                list.Sort(_CompareByPlayerName);
            }
            return list.ToArray();
        }

        public static CAvatar[] GetAvatars()
        {
            CAvatar[] result = null;
            lock (_AvatarMutex)
            {
                if (_Avatars.Count == 0)
                    return null;
            
                result = new CAvatar[_Avatars.Count];
                _Avatars.Values.CopyTo(result, 0);
            }
            return result;
        }

        public static string GetPlayerName(int profileID, int playerNum = 0)
        {
            if (ICProfileIDValid(profileID))
                return _Profiles[profileID].PlayerName;

            string playerName = CLanguage.Translate("TR_SCREENNAMES_PLAYER");
            if (playerNum > 0)
                playerName += " " + playerNum;
            return playerName;
        }

        public static string GetProfileFileName(int profileID)
        {
            if (!ICProfileIDValid(profileID))
                return String.Empty;

            return Path.GetFileName(_Profiles[profileID].FileName);
        }

        public static int NewProfile(string fileName = "")
        {
            CProfile profile = new CProfile
                {
                    FileName = fileName != "" ? Path.Combine(CSettings.FolderProfiles, fileName) : String.Empty
                };

            if (File.Exists(profile.FileName))
                return -1;

            lock (_ProfileMutex)
            {
                profile.ID = _ProfileIDs.Dequeue();
                _Profiles.Add(profile.ID, profile);
            }
            _Notification();
            return profile.ID;
        }

        public static void SaveProfiles()
        {
            foreach (int id in _Profiles.Keys)
            {
                _Profiles[id].SaveProfile();
            }

            LoadProfiles();
        }

        public static void LoadProfiles()
        {
            LoadAvatars();

            lock (_ProfileMutex)
            {
                List<string> knownFiles = new List<string>();
                foreach (int id in _Profiles.Keys)
                {
                    _Profiles[id].LoadProfile();
                    knownFiles.Add(_Profiles[id].FileName);
                }

                List<string> files = new List<string>();
                files.AddRange(CHelper.ListFiles(CSettings.FolderProfiles, "*.xml", true, true));

                foreach (string file in knownFiles)
                {
                    files.Remove(file);
                }

                foreach (string file in files)
                {
                    CProfile profile = new CProfile
                        {
                            FileName = Path.Combine(CSettings.FolderProfiles, file)
                        };

                    if (profile.LoadProfile())
                    {
                        profile.Avatar = _GetAvatar(profile.AvatarFileName);
                        profile.ID = _ProfileIDs.Dequeue();
                        _Profiles.Add(profile.ID, profile);
                    }
                }
            }
            _Notification();
        }

        public static void LoadAvatars()
        {
            lock (_AvatarMutex)
            {
                List<string> knownFiles = new List<string>();
                foreach (int id in _Avatars.Keys)
                {
                    _Avatars[id].Reload();
                    knownFiles.Add(_Avatars[id].FileName);
                }

                List<string> files = new List<string>();
                files.AddRange(CHelper.ListFiles(CSettings.FolderProfiles, "*.png", true, true));
                files.AddRange(CHelper.ListFiles(CSettings.FolderProfiles, "*.jpg", true, true));
                files.AddRange(CHelper.ListFiles(CSettings.FolderProfiles, "*.jpeg", true, true));
                files.AddRange(CHelper.ListFiles(CSettings.FolderProfiles, "*.bmp", true, true));

                foreach (string file in knownFiles)
                {
                    files.Remove(file);
                }

                foreach (string file in files)
                {
                    CAvatar avatar = new CAvatar(-1);
                    if (avatar.LoadFromFile(file))
                    {
                        avatar.ID = _AvatarIDs.Dequeue();
                        _Avatars.Add(avatar.ID, avatar);
                    }
                }
            }
            _Notification();
        }

        public static string AddGetPlayerName(int profileID, char chr)
        {
            if (!ICProfileIDValid(profileID))
                return String.Empty;

            _Profiles[profileID].PlayerName += chr;
            return _Profiles[profileID].PlayerName;
        }

        public static string GetDeleteCharInPlayerName(int profileID)
        {
            if (!ICProfileIDValid(profileID))
                return String.Empty;

            CProfile profile = _Profiles[profileID];
            if (profile.PlayerName != "")
                profile.PlayerName = profile.PlayerName.Remove(profile.PlayerName.Length - 1);

            return profile.PlayerName;
        }

        public static bool ICProfileIDValid(int profileID)
        {
            return _Profiles.ContainsKey(profileID);
        }

        public static bool ICAvatarIDValid(int avatarID)
        {
            return _Avatars.ContainsKey(avatarID);
        }

        public static EGameDifficulty GetDifficulty(int profileID)
        {
            return ICProfileIDValid(profileID) ? _Profiles[profileID].Difficulty : EGameDifficulty.TR_CONFIG_NORMAL;
        }

        public static void SetDifficulty(int profileID, EGameDifficulty difficulty)
        {
            if (!ICProfileIDValid(profileID))
                return;

            _Profiles[profileID].Difficulty = difficulty;
        }

        public static EOffOn GetGuestProfile(int profileID)
        {
            return ICProfileIDValid(profileID) ? _Profiles[profileID].GuestProfile : EOffOn.TR_CONFIG_OFF;
        }

        public static EOffOn GetActive(int profileID)
        {
            return ICProfileIDValid(profileID) ? _Profiles[profileID].Active : EOffOn.TR_CONFIG_OFF;
        }

        public static void SetGuestProfile(int profileID, EOffOn option)
        {
            if (!ICProfileIDValid(profileID))
                return;

            _Profiles[profileID].GuestProfile = option;
        }

        public static void SetActive(int profileID, EOffOn option)
        {
            if (!ICProfileIDValid(profileID))
                return;
            _Profiles[profileID].Active = option;
        }

        public static bool IsGuestProfile(int profileID)
        {
            if (!ICProfileIDValid(profileID))
                return true; // this will prevent from saving dummy profiles to highscore db

            return _Profiles[profileID].GuestProfile == EOffOn.TR_CONFIG_ON;
        }

        public static bool IsActive(int profileID)
        {
            if (!ICProfileIDValid(profileID))
                return false;
            return _Profiles[profileID].Active == EOffOn.TR_CONFIG_ON;
        }

        public static void SetAvatar(int profileID, int avatarID)
        {
            if (!ICProfileIDValid(profileID) || !ICAvatarIDValid(avatarID))
                return;

            _Profiles[profileID].Avatar = _Avatars[avatarID];
        }

        public static int GetAvatarID(int profileID)
        {
            if (!ICProfileIDValid(profileID))
                return -1;

            return _Profiles[profileID].Avatar.ID;
        }

        public static STexture GetAvatarTexture(int avatarID)
        {
            if (!ICAvatarIDValid(avatarID))
                return new STexture(-1);

            return _Avatars[avatarID].Texture;
        }

        public static STexture GetAvatarTextureFromProfile(int profileID)
        {
            if (!ICProfileIDValid(profileID))
                return new STexture(-1);

            return _Profiles[profileID].Avatar.Texture;
        }

        public static void DeleteProfile(int profileID)
        {
            lock (_ProfileMutex)
            {
                if (!ICProfileIDValid(profileID))
                    return;

                if (_Profiles[profileID].FileName == "")
                {
                    _RemoveProfile(profileID);
                    return;
                }

                try
                {
                    //Check if profile saved in config
                    for (int i = 0; i < CConfig.Players.Length; i++)
                    {
                        if (CConfig.Players[i] == _Profiles[profileID].FileName)
                        {
                            CConfig.Players[i] = string.Empty;
                            CConfig.SaveConfig();
                        }
                    }
                    File.Delete(_Profiles[profileID].FileName);
                    _RemoveProfile(profileID);

                    //Check if profile is selected in game
                    for (int i = 0; i < CGame.Players.Length; i++)
                    {
                        if (CGame.Players[i].ProfileID > profileID)
                            CGame.Players[i].ProfileID--;
                        else if (CGame.Players[i].ProfileID == profileID)
                            CGame.Players[i].ProfileID = -1;
                    }
                }
                catch (Exception)
                {
                    CLog.LogError("Can't delete Profile File " + _Profiles[profileID].FileName + ".xml");
                }
            }
        }

        #region private methods
        private static void _Notification()
        {
            lock (_Callbacks)
            {
                for (int i = 0; i < _Callbacks.Count; i++)
                {
                    try
                    {
                        _Callbacks[i]();
                    }
                    catch (Exception)
                    {
                        _Callbacks.RemoveAt(i);
                    }
                }
                
            }
        }

        private static int _CompareByPlayerName(CProfile a, CProfile b)
        {
            return String.CompareOrdinal(a.PlayerName, b.PlayerName);
        }

        private static CAvatar _GetAvatar(string fileName)
        {
            lock (_AvatarMutex)
            {
                foreach (int id in _Avatars.Keys)
                {
                    if (Path.GetFileName(_Avatars[id].FileName) == fileName)
                        return _Avatars[id];
                }
            }
            return new CAvatar(-1);
        }

        private static void _RemoveProfile(int profileID)
        {
            if (!ICProfileIDValid(profileID))
                return;

            _Profiles.Remove(profileID);
            _Notification();
        }
        #endregion private methods
    }
}