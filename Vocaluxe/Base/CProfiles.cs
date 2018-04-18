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
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Log;
using VocaluxeLib.Profile;

namespace Vocaluxe.Base
{
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
            public Guid ProfileID;
        }
        #endregion enums and structs

        #region private vars
        private static Dictionary<Guid, CProfile> _Profiles;

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

            _Profiles = new Dictionary<Guid, CProfile>();

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
                            CProfile newProf = change.Profile;
                            if (newProf == null)
                                break;

                            newProf.ID = Guid.NewGuid();
                            if (newProf.Avatar == null)
                                newProf.Avatar = _Avatars.Values.First();
                            else if (newProf.Avatar.ID < 0)
                            {
                                newProf.Avatar.ID = _AvatarIDs.Dequeue();
                                _Avatars.Add(newProf.Avatar.ID, newProf.Avatar);
                                _AvatarsChanged = true;
                            }
                            newProf.SaveProfile();
                            _Profiles.Add(newProf.ID, newProf);

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

            var flags = EProfileChangedFlags.None;

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
            var change = new SChange {Action = EAction.LoadProfiles};

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static void LoadAvatars()
        {
            var change = new SChange {Action = EAction.LoadAvatars};

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static void AddProfile(CProfile newProfile)
        {
            if (newProfile == null)
                return;

            var change = new SChange {Action = EAction.AddProfile, Profile = newProfile};

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static void EditProfile(CProfile editProfile)
        {
            if (editProfile == null)
                return;

            var change = new SChange {Action = EAction.EditProfile, Profile = editProfile};

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static void DeleteProfile(Guid profileID)
        {
            if (!IsProfileIDValid(profileID))
                return;

            var change = new SChange {Action = EAction.DeleteProfile, ProfileID = profileID};

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static void AddAvatar(CAvatar newAvatar)
        {
            if (newAvatar == null)
                return;

            var change = new SChange {Action = EAction.AddAvatar, Avatar = newAvatar};

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static void EditAvatar(CAvatar editAvatar)
        {
            if (editAvatar == null)
                return;

            var change = new SChange {Action = EAction.EditAvatar, Avatar = editAvatar};

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static CProfile[] GetProfiles()
        {
            if (_Profiles.Count == 0)
                return new CProfile[0];

            var list = new List<CProfile>(_Profiles.Values);
            list.Sort(_AlphaNumericCompareByPlayerName);
            return list.ToArray();
        }

        public static CProfile GetProfile(Guid profileID)
        {
            if (!IsProfileIDValid(profileID))
                return null;

            return _Profiles[profileID];
        }

        public static IEnumerable<CAvatar> GetAvatars()
        {
            if (_Avatars.Count == 0)
                return null;

            var result = new CAvatar[_Avatars.Count];
            _Avatars.Values.CopyTo(result, 0);

            return result;
        }

        public static Guid NewProfile(string fileName = "")
        {
            var profile = new CProfile
                {
                    FilePath = fileName != "" ? Path.Combine(CConfig.ProfileFolders[0], fileName) : String.Empty
                };

            if (File.Exists(profile.FilePath))
                return Guid.Empty;

            profile.ID = Guid.NewGuid();
            _Profiles.Add(profile.ID, profile);
            _ProfilesChanged = true;
            return profile.ID;
        }

        public static int NewAvatar(string fileName)
        {
            CAvatar avatar = CAvatar.GetAvatar(fileName);
            if (avatar == null)
                return -1;

            avatar.ID = _AvatarIDs.Dequeue();
            _Avatars.Add(avatar.ID, avatar);
            _AvatarsChanged = true;
            return avatar.ID;
        }

        public static void SaveProfiles()
        {
            foreach (Guid id in _Profiles.Keys)
                _Profiles[id].SaveProfile();
        }

        public static bool IsProfileIDValid(Guid profileID)
        {
            return _Profiles.ContainsKey(profileID);
        }

        public static bool IsAvatarIDValid(int avatarID)
        {
            return _Avatars.ContainsKey(avatarID);
        }
        #endregion public methods

        #region profile properties
        public static string GetPlayerName(Guid profileID, int playerNum = 0)
        {
            if (IsProfileIDValid(profileID))
                return _Profiles[profileID].PlayerName;

            string playerName = CLanguage.Translate("TR_SCREENNAMES_PLAYER");
            if (playerNum > 0)
                playerName += " " + playerNum;
            return playerName;
        }

        public static void SetPlayerName(Guid profileID, string playerName)
        {
            if (!IsProfileIDValid(profileID))
                return;

            _Profiles[profileID].PlayerName = playerName;
        }

        public static string GetProfileFileName(Guid profileID)
        {
            if (!IsProfileIDValid(profileID))
                return String.Empty;

            return Path.GetFileName(_Profiles[profileID].FilePath);
        }

        public static string AddGetPlayerName(Guid profileID, char chr)
        {
            if (!IsProfileIDValid(profileID))
                return String.Empty;

            _Profiles[profileID].PlayerName += chr;
            return _Profiles[profileID].PlayerName;
        }

        public static string GetDeleteCharInPlayerName(Guid profileID)
        {
            if (!IsProfileIDValid(profileID))
                return String.Empty;

            CProfile profile = _Profiles[profileID];
            if (!String.IsNullOrEmpty(profile.PlayerName))
                profile.PlayerName = profile.PlayerName.Remove(profile.PlayerName.Length - 1);

            return profile.PlayerName;
        }

        public static EGameDifficulty GetDifficulty(Guid profileID)
        {
            return IsProfileIDValid(profileID) ? _Profiles[profileID].Difficulty : EGameDifficulty.TR_CONFIG_NORMAL;
        }

        public static void SetDifficulty(Guid profileID, EGameDifficulty difficulty)
        {
            if (!IsProfileIDValid(profileID))
                return;

            _Profiles[profileID].Difficulty = difficulty;
        }

        public static EUserRole GetUserRoleProfile(Guid profileID)
        {
            return IsProfileIDValid(profileID) ? _Profiles[profileID].UserRole : EUserRole.TR_USERROLE_GUEST;
        }

        public static void SetUserRoleProfile(Guid profileID, EUserRole option)
        {
            if (!IsProfileIDValid(profileID))
                return;
            //Only allow the change of TR_USERROLE_GUEST, TR_USERROLE_NORMAL and TR_USERROLE_ADMIN
            const EUserRole mask = (EUserRole.TR_USERROLE_GUEST | EUserRole.TR_USERROLE_NORMAL | EUserRole.TR_USERROLE_ADMIN);
            option &= mask;
            _Profiles[profileID].UserRole = (_Profiles[profileID].UserRole & ~mask) | option;
        }

        public static EOffOn GetActive(Guid profileID)
        {
            return IsProfileIDValid(profileID) ? _Profiles[profileID].Active : EOffOn.TR_CONFIG_OFF;
        }

        public static void SetActive(Guid profileID, EOffOn option)
        {
            if (!IsProfileIDValid(profileID))
                return;

            _Profiles[profileID].Active = option;
        }

        public static bool IsGuestProfile(Guid profileID)
        {
            if (!IsProfileIDValid(profileID))
                return true; // this will prevent from saving dummy profiles to highscore db

            return _Profiles[profileID].UserRole <= EUserRole.TR_USERROLE_GUEST;
        }

        public static bool IsActive(Guid profileID)
        {
            if (!IsProfileIDValid(profileID))
                return false;
            return _Profiles[profileID].Active == EOffOn.TR_CONFIG_ON;
        }

        public static void SetAvatar(Guid profileID, int avatarID)
        {
            if (!IsProfileIDValid(profileID) || !IsAvatarIDValid(avatarID))
                return;

            _Profiles[profileID].Avatar = _Avatars[avatarID];
        }

        public static int GetAvatarID(Guid profileID)
        {
            if (!IsProfileIDValid(profileID) || _Profiles[profileID].Avatar == null)
                return -1;

            return _Profiles[profileID].Avatar.ID;
        }

        public static CAvatar GetAvatar(Guid profileID)
        {
            if (!IsProfileIDValid(profileID))
                return null;

            return _Profiles[profileID].Avatar;
        }

        //TODO: Remove this?
        public static Guid GetProfileID(Guid num)
        {
            return _Profiles[num].ID;
        }
        #endregion profile properties

        #region avatar texture
        public static CTextureRef GetAvatarTexture(int avatarID)
        {
            if (!IsAvatarIDValid(avatarID))
                return null;

            return _Avatars[avatarID].Texture;
        }

        public static CTextureRef GetAvatarTextureFromProfile(Guid profileID)
        {
            if (!IsProfileIDValid(profileID) || _Profiles[profileID].Avatar == null)
                return null;

            return _Profiles[profileID].Avatar.Texture;
        }
        #endregion avatar texture

        #region private methods
        private static void _LoadProfiles()
        {
            _LoadAvatars();

            var knownFiles = new List<string>();
            if (_Profiles.Count > 0)
            {
                var ids = new Guid[_Profiles.Keys.Count];
                _Profiles.Keys.CopyTo(ids, 0);
                foreach (Guid id in ids)
                {
                    if (_Profiles[id].LoadProfile())
                        knownFiles.Add(Path.GetFileName(_Profiles[id].FilePath));
                    else
                        _Profiles.Remove(id);
                }
            }


            var files = new List<string>();
            foreach (string path in CConfig.ProfileFolders)
                files.AddRange(CHelper.ListFiles(path, "*.xml", true, true));

            foreach (string file in files)
            {
                if (knownFiles.Contains(Path.GetFileName(file)))
                    continue;

                var profile = new CProfile();

                if (profile.LoadProfile(file))
                    _Profiles.Add(profile.ID, profile);
            }
            _ProfilesChanged = true;
        }

        private static void _LoadAvatars()
        {
            var knownFiles = new List<string>();
            if (_Avatars.Count > 0)
            {
                var ids = new int[_Avatars.Keys.Count];
                _Avatars.Keys.CopyTo(ids, 0);
                foreach (int id in ids)
                {
                    if (_Avatars[id].Reload())
                        knownFiles.Add(Path.GetFileName(_Avatars[id].FileName));
                    else
                        _Avatars.Remove(id);
                }
            }

            var files = new List<string>();
            foreach (string path in CConfig.ProfileFolders)
                files.AddRange(CHelper.ListImageFiles(path, true, true));

            foreach (string file in files)
            {
                if (knownFiles.Contains(Path.GetFileName(file)))
                    continue;

                CAvatar avatar = CAvatar.GetAvatar(file);
                if (avatar != null)
                {
                    avatar.ID = _AvatarIDs.Dequeue();
                    _Avatars.Add(avatar.ID, avatar);
                }
            }
            _ProfilesChanged = true;
        }

        private static void _DeleteProfile(Guid profileID)
        {
            if (!IsProfileIDValid(profileID))
                return;

            if (string.IsNullOrEmpty(_Profiles[profileID].FilePath))
            {
                _RemoveProfile(profileID);
                return;
            }

            try
            {
                //Check if profile saved in config
                for (int i = 0; i < CSettings.MaxNumPlayer; i++)
                {
                    if (CConfig.Config.Game.Players[i] == GetProfileFileName(profileID))
                    {
                        CConfig.Config.Game.Players[i] = string.Empty;
                        CConfig.SaveConfig();
                    }
                }
                File.Delete(_Profiles[profileID].FilePath);
                _RemoveProfile(profileID);

                //Check if profile is selected in game
                for (int i = 0; i < CGame.Players.Length; i++)
                {
                    if (CGame.Players[i].ProfileID == profileID)
                        CGame.Players[i].ProfileID = Guid.Empty;
                }
            }
            catch (Exception)
            {
                CLog.Error("Can't delete Profile File " + _Profiles[profileID].FilePath);
            }
            _ProfilesChanged = true;
        }

        private static void _RemoveProfile(Guid profileID)
        {
            if (!IsProfileIDValid(profileID))
                return;

            _Profiles.Remove(profileID);
            _ProfilesChanged = true;
        }

        private static int _AlphaNumericCompareByPlayerName(CProfile a, CProfile b)
        {
            return _PadNumbersInString(a.PlayerName).CompareTo(_PadNumbersInString(b.PlayerName));
        }

        private static string _PadNumbersInString(string text)
        {
            return Regex.Replace(text, "[0-9]+", match => match.Value.PadLeft(4, '0'));
        }

        public static CAvatar GetAvatarByFilename(string fileName)
        {
            string name = Path.GetFileName(fileName);

            foreach (int id in _Avatars.Keys)
            {
                if (Path.GetFileName(_Avatars[id].FileName) == name)
                    return _Avatars[id];
            }
            return null;
        }
        #endregion private methods
    }
}