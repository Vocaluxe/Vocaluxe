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
using VocaluxeLib;
using VocaluxeLib.Profile;
using VocaluxeLib.Draw;

namespace Vocaluxe.Base
{
    [Flags]
    public enum EProfileChangedFlags
    {
        None = 1,
        Avatar = 2,
        Profile = 4
    }

    public delegate void ProfileChangedCallback(EProfileChangedFlags typeChanged);

    static class CProfiles
    {
        #region enums and structs
        private enum EAction
        {
            LoadProfiles,
            LoadAvatars,
            AddProfile,
            EditProfile,
            DeleteProfile,
            AddAvatar,
            EditAvatar
        }

        private struct SChange
        {
            public CProfile Profile;
            public CAvatar Avatar;
            public EAction Action;
            public int ProfileID;
        }
        #endregion enums and structs

        #region private vars
        private static Dictionary<int, CProfile> _Profiles;
        private static Queue<int> _ProfileIDs;

        private static Dictionary<int, CAvatar> _Avatars;
        private static Queue<int> _AvatarIDs;

        private static readonly Queue<SChange> _Queue = new Queue<SChange>();
        private static readonly Object _QueueMutex = new Object();

        private static readonly List<ProfileChangedCallback> _ProfileChangedCallbacks = new List<ProfileChangedCallback>();
        private static bool _ProfilesChanged;
        private static bool _AvatarsChanged;
        #endregion private vars

        #region properties
        public static int NumProfiles
        {
            get { return _Profiles.Count; }
        }

        public static int NumAvatars
        {
            get { return _Avatars.Count; }
        }
        #endregion properties

        #region public methods
        public static void Init()
        {
            _Avatars = new Dictionary<int, CAvatar>();
            _AvatarIDs = new Queue<int>(1000);
            for (int i = 0; i < 1000; i++)
                _AvatarIDs.Enqueue(i);

            _Profiles = new Dictionary<int, CProfile>();
            _ProfileIDs = new Queue<int>(100);
            for (int i = 0; i < 100; i++)
                _ProfileIDs.Enqueue(i);

            LoadProfiles();
        }

        public static void Update()
        {
            lock (_QueueMutex)
            {
                while (_Queue.Count > 0)
                {
                    SChange change = _Queue.Dequeue();
                    switch (change.Action)
                    {
                        case EAction.LoadProfiles:
                            _LoadProfiles();
                            _ProfilesChanged = true;
                            _AvatarsChanged = true;
                            break;

                        case EAction.LoadAvatars:
                            _LoadAvatars();
                            _AvatarsChanged = true;
                            break;

                        case EAction.AddProfile:
                            if (change.Profile == null)
                                break;

                            change.Profile.ID = _ProfileIDs.Dequeue();
                            change.Profile.Avatar = _GetAvatar(change.Profile.AvatarFileName);
                            change.Profile.SaveProfile();
                            _Profiles.Add(change.Profile.ID, change.Profile);

                            _ProfilesChanged = true;
                            break;

                        case EAction.EditProfile:
                            if (change.Profile == null)
                                break;

                            if (!IsProfileIDValid(change.Profile.ID))
                                return;

                            _Profiles[change.Profile.ID] = change.Profile;
                            _ProfilesChanged = true;
                            break;

                        case EAction.DeleteProfile:
                            if (!IsProfileIDValid(change.ProfileID))
                                break;

                            _DeleteProfile(change.ProfileID);
                            _ProfilesChanged = true;
                            break;

                        case EAction.AddAvatar:
                            if (change.Avatar == null)
                                break;

                            change.Avatar.ID = _AvatarIDs.Dequeue();
                            _Avatars.Add(change.Avatar.ID, change.Avatar);
                            _AvatarsChanged = true;
                            break;

                        case EAction.EditAvatar:
                            if (change.Avatar == null)
                                break;

                            if (!IsAvatarIDValid(change.Avatar.ID))
                                return;

                            _Avatars[change.Avatar.ID] = change.Avatar;
                            _AvatarsChanged = true;
                            break;
                    }
                }
            }

            if (_ProfileChangedCallbacks.Count == 0)
                return;

            EProfileChangedFlags flags = EProfileChangedFlags.None;

            if (_AvatarsChanged)
                flags = EProfileChangedFlags.Avatar;

            if (_ProfilesChanged)
                flags |= EProfileChangedFlags.Profile;

            if (flags != EProfileChangedFlags.None)
            {
                int index = 0;
                while (index < _ProfileChangedCallbacks.Count)
                {
                    try
                    {
                        _ProfileChangedCallbacks[index](flags);
                    }
                    catch (Exception)
                    {
                        _ProfileChangedCallbacks.RemoveAt(index);
                    }
                    index++;
                }
            }
            _AvatarsChanged = false;
            _ProfilesChanged = false;
        }

        public static void AddProfileChangedCallback(ProfileChangedCallback notification)
        {
            _ProfileChangedCallbacks.Add(notification);
        }

        public static void LoadProfiles()
        {
            SChange change = new SChange {Action = EAction.LoadProfiles};

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static void LoadAvatars()
        {
            SChange change = new SChange {Action = EAction.LoadAvatars};

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static void AddProfile(CProfile newProfile)
        {
            if (newProfile == null)
                return;

            SChange change = new SChange {Action = EAction.AddProfile, Profile = newProfile};

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static void EditProfile(CProfile editProfile)
        {
            if (editProfile == null)
                return;

            SChange change = new SChange {Action = EAction.EditProfile, Profile = editProfile};

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static void DeleteProfile(int profileID)
        {
            if (!IsProfileIDValid(profileID))
                return;

            SChange change = new SChange {Action = EAction.DeleteProfile, ProfileID = profileID};

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static void AddAvatar(CAvatar newAvatar)
        {
            if (newAvatar == null)
                return;

            SChange change = new SChange {Action = EAction.AddAvatar, Avatar = newAvatar};

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static void EditAvatar(CAvatar editAvatar)
        {
            if (editAvatar == null)
                return;

            SChange change = new SChange {Action = EAction.EditAvatar, Avatar = editAvatar};

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static CProfile[] GetProfiles()
        {
            if (_Profiles.Count == 0)
                return null;

            List<CProfile> list = new List<CProfile>(_Profiles.Values);
            list.Sort(_CompareByPlayerName);
            return list.ToArray();
        }

        public static IEnumerable<CAvatar> GetAvatars()
        {
            if (_Avatars.Count == 0)
                return null;

            CAvatar[] result = new CAvatar[_Avatars.Count];
            _Avatars.Values.CopyTo(result, 0);

            return result;
        }

        public static int NewProfile(string fileName = "")
        {
            CProfile profile = new CProfile
                {
                    FileName = fileName != "" ? Path.Combine(CSettings.FolderProfiles, fileName) : String.Empty
                };

            if (File.Exists(profile.FileName))
                return -1;

            profile.ID = _ProfileIDs.Dequeue();
            _Profiles.Add(profile.ID, profile);
            _ProfilesChanged = true;
            return profile.ID;
        }

        public static int NewAvatar(string fileName)
        {
            CAvatar avatar = new CAvatar(-1);
            if (!avatar.LoadFromFile(fileName))
                return -1;

            avatar.ID = _AvatarIDs.Dequeue();
            _Avatars.Add(avatar.ID, avatar);
            _AvatarsChanged = true;
            return avatar.ID;
        }

        public static void SaveProfiles()
        {
            foreach (int id in _Profiles.Keys)
                _Profiles[id].SaveProfile();
        }

        public static bool IsProfileIDValid(int profileID)
        {
            return _Profiles.ContainsKey(profileID);
        }

        public static bool IsAvatarIDValid(int avatarID)
        {
            return _Avatars.ContainsKey(avatarID);
        }
        #endregion public methods

        #region profile properties
        public static string GetPlayerName(int profileID, int playerNum = 0)
        {
            if (IsProfileIDValid(profileID))
                return _Profiles[profileID].PlayerName;

            string playerName = CLanguage.Translate("TR_SCREENNAMES_PLAYER");
            if (playerNum > 0)
                playerName += " " + playerNum;
            return playerName;
        }

        public static void SetPlayerName(int profileID, string playerName)
        {
            if (!IsProfileIDValid(profileID))
                return;

            _Profiles[profileID].PlayerName = playerName;
        }

        public static string GetProfileFileName(int profileID)
        {
            if (!IsProfileIDValid(profileID))
                return String.Empty;

            return Path.GetFileName(_Profiles[profileID].FileName);
        }

        public static string AddGetPlayerName(int profileID, char chr)
        {
            if (!IsProfileIDValid(profileID))
                return String.Empty;

            _Profiles[profileID].PlayerName += chr;
            return _Profiles[profileID].PlayerName;
        }

        public static string GetDeleteCharInPlayerName(int profileID)
        {
            if (!IsProfileIDValid(profileID))
                return String.Empty;

            CProfile profile = _Profiles[profileID];
            if (!String.IsNullOrEmpty(profile.PlayerName))
                profile.PlayerName = profile.PlayerName.Remove(profile.PlayerName.Length - 1);

            return profile.PlayerName;
        }

        public static EGameDifficulty GetDifficulty(int profileID)
        {
            return IsProfileIDValid(profileID) ? _Profiles[profileID].Difficulty : EGameDifficulty.TR_CONFIG_NORMAL;
        }

        public static void SetDifficulty(int profileID, EGameDifficulty difficulty)
        {
            if (!IsProfileIDValid(profileID))
                return;

            _Profiles[profileID].Difficulty = difficulty;
        }

        public static EOffOn GetGuestProfile(int profileID)
        {
            return IsProfileIDValid(profileID) ? _Profiles[profileID].GuestProfile : EOffOn.TR_CONFIG_OFF;
        }

        public static void SetGuestProfile(int profileID, EOffOn option)
        {
            if (!IsProfileIDValid(profileID))
                return;

            _Profiles[profileID].GuestProfile = option;
        }

        public static EOffOn GetActive(int profileID)
        {
            return IsProfileIDValid(profileID) ? _Profiles[profileID].Active : EOffOn.TR_CONFIG_OFF;
        }

        public static void SetActive(int profileID, EOffOn option)
        {
            if (!IsProfileIDValid(profileID))
                return;

            _Profiles[profileID].Active = option;
        }

        public static bool IsGuestProfile(int profileID)
        {
            if (!IsProfileIDValid(profileID))
                return true; // this will prevent from saving dummy profiles to highscore db

            return _Profiles[profileID].GuestProfile == EOffOn.TR_CONFIG_ON;
        }

        public static bool IsActive(int profileID)
        {
            if (!IsProfileIDValid(profileID))
                return false;
            return _Profiles[profileID].Active == EOffOn.TR_CONFIG_ON;
        }

        public static void SetAvatar(int profileID, int avatarID)
        {
            if (!IsProfileIDValid(profileID) || !IsAvatarIDValid(avatarID))
                return;

            _Profiles[profileID].Avatar = _Avatars[avatarID];
        }

        public static int GetAvatarID(int profileID)
        {
            if (!IsProfileIDValid(profileID))
                return -1;

            return _Profiles[profileID].Avatar.ID;
        }
        #endregion profile properties

        #region avatar texture
        public static CTexture GetAvatarTexture(int avatarID)
        {
            if (!IsAvatarIDValid(avatarID))
                return null;

            return _Avatars[avatarID].Texture;
        }

        public static CTexture GetAvatarTextureFromProfile(int profileID)
        {
            if (!IsProfileIDValid(profileID))
                return null;

            return _Profiles[profileID].Avatar.Texture;
        }
        #endregion avatar texture

        #region private methods
        private static void _LoadProfiles()
        {
            _LoadAvatars();

            List<string> knownFiles = new List<string>();
            if (_Profiles.Count > 0)
            {
                int[] ids = new int[_Profiles.Keys.Count];
                _Profiles.Keys.CopyTo(ids, 0);
                foreach (int id in ids)
                {
                    if (_Profiles[id].LoadProfile())
                    {
                        _Profiles[id].Avatar = _GetAvatar(_Profiles[id].AvatarFileName);
                        knownFiles.Add(Path.GetFileName(_Profiles[id].FileName));
                    }
                    else
                        _Profiles.Remove(id);
                }
            }

            List<string> files = new List<string>();
            files.AddRange(CHelper.ListFiles(CSettings.FolderProfiles, "*.xml", true, true));

            foreach (string file in files)
            {
                if (knownFiles.Contains(Path.GetFileName(file)))
                    continue;

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
            _ProfilesChanged = true;
        }

        private static void _LoadAvatars()
        {
            List<string> knownFiles = new List<string>();
            if (_Avatars.Count > 0)
            {
                int[] ids = new int[_Avatars.Keys.Count];
                _Avatars.Keys.CopyTo(ids, 0);
                foreach (int id in ids)
                {
                    if (_Avatars[id].Reload())
                        knownFiles.Add(Path.GetFileName(_Avatars[id].FileName));
                    else
                        _Avatars.Remove(id);
                }
            }

            List<string> files = new List<string>();
            files.AddRange(CHelper.ListFiles(CSettings.FolderProfiles, "*.png", true, true));
            files.AddRange(CHelper.ListFiles(CSettings.FolderProfiles, "*.jpg", true, true));
            files.AddRange(CHelper.ListFiles(CSettings.FolderProfiles, "*.jpeg", true, true));
            files.AddRange(CHelper.ListFiles(CSettings.FolderProfiles, "*.bmp", true, true));

            foreach (string file in files)
            {
                if (knownFiles.Contains(Path.GetFileName(file)))
                    continue;

                CAvatar avatar = new CAvatar(-1);
                if (avatar.LoadFromFile(file))
                {
                    avatar.ID = _AvatarIDs.Dequeue();
                    _Avatars.Add(avatar.ID, avatar);
                }
            }
            _ProfilesChanged = true;
        }

        private static void _DeleteProfile(int profileID)
        {
            if (!IsProfileIDValid(profileID))
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
            _ProfilesChanged = true;
        }

        private static void _RemoveProfile(int profileID)
        {
            if (!IsProfileIDValid(profileID))
                return;

            _Profiles.Remove(profileID);
            _ProfilesChanged = true;
        }

        private static int _CompareByPlayerName(CProfile a, CProfile b)
        {
            return String.CompareOrdinal(a.PlayerName, b.PlayerName);
        }

        private static CAvatar _GetAvatar(string fileName)
        {
            string name = Path.GetFileName(fileName);

            foreach (int id in _Avatars.Keys)
            {
                if (Path.GetFileName(_Avatars[id].FileName) == name)
                    return _Avatars[id];
            }
            return new CAvatar(-1);
        }
        #endregion private methods
    }
}