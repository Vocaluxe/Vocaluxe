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
using System.IO;
using System.Linq;
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
            public Guid ProfileId;
        }
        #endregion enums and structs

        #region private vars
        private static Dictionary<Guid, CProfile> _Profiles;

        private static Dictionary<int, CAvatar> _Avatars;
        private static Queue<int> _AvatarIds;

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
            _AvatarIds = new Queue<int>(1000);
            for (var i = 0; i < 1000; i++)
            {
                _AvatarIds.Enqueue(i);
            }

            _Profiles = new Dictionary<Guid, CProfile>();

            LoadProfiles();
        }

        public static void Update()
        {
            lock (_QueueMutex)
            {
                while (_Queue.Count > 0)
                {
                    var change = _Queue.Dequeue();
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
                            var newProf = change.Profile;
                            if (newProf == null)
                            {
                                break;
                            }

                            newProf.Id = Guid.NewGuid();
                            if (newProf.Avatar == null)
                            {
                                newProf.Avatar = _Avatars.Values.First();
                            }
                            else if (newProf.Avatar.Id < 0)
                            {
                                newProf.Avatar.Id = _AvatarIds.Dequeue();
                                _Avatars.Add(newProf.Avatar.Id, newProf.Avatar);
                                _AvatarsChanged = true;
                            }

                            newProf.SaveProfile();
                            _Profiles.Add(newProf.Id, newProf);

                            _ProfilesChanged = true;
                            break;

                        case EAction.EditProfile:
                            if (change.Profile == null)
                            {
                                break;
                            }

                            if (!IsProfileIdValid(change.Profile.Id))
                            {
                                return;
                            }

                            _Profiles[change.Profile.Id] = change.Profile;
                            _ProfilesChanged = true;
                            break;

                        case EAction.DeleteProfile:
                            if (!IsProfileIdValid(change.ProfileId))
                            {
                                break;
                            }

                            _DeleteProfile(change.ProfileId);
                            _ProfilesChanged = true;
                            break;

                        case EAction.AddAvatar:
                            if (change.Avatar == null)
                            {
                                break;
                            }

                            change.Avatar.Id = _AvatarIds.Dequeue();
                            _Avatars.Add(change.Avatar.Id, change.Avatar);
                            _AvatarsChanged = true;
                            break;

                        case EAction.EditAvatar:
                            if (change.Avatar == null)
                            {
                                break;
                            }

                            if (!IsAvatarIdValid(change.Avatar.Id))
                            {
                                return;
                            }

                            _Avatars[change.Avatar.Id] = change.Avatar;
                            _AvatarsChanged = true;
                            break;
                    }
                }
            }

            if (_ProfileChangedCallbacks.Count == 0)
            {
                return;
            }

            var flags = EProfileChangedFlags.None;

            if (_AvatarsChanged)
            {
                flags = EProfileChangedFlags.Avatar;
            }

            if (_ProfilesChanged)
            {
                flags |= EProfileChangedFlags.Profile;
            }

            if (flags != EProfileChangedFlags.None)
            {
                var index = 0;
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
            var change = new SChange { Action = EAction.LoadProfiles };

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static void LoadAvatars()
        {
            var change = new SChange { Action = EAction.LoadAvatars };

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static void AddProfile(CProfile newProfile)
        {
            if (newProfile == null)
            {
                return;
            }

            var change = new SChange { Action = EAction.AddProfile, Profile = newProfile };

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static void EditProfile(CProfile editProfile)
        {
            if (editProfile == null)
            {
                return;
            }

            var change = new SChange { Action = EAction.EditProfile, Profile = editProfile };

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static void DeleteProfile(Guid profileId)
        {
            if (!IsProfileIdValid(profileId))
            {
                return;
            }

            var change = new SChange { Action = EAction.DeleteProfile, ProfileId = profileId };

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static void AddAvatar(CAvatar newAvatar)
        {
            if (newAvatar == null)
            {
                return;
            }

            var change = new SChange { Action = EAction.AddAvatar, Avatar = newAvatar };

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static void EditAvatar(CAvatar editAvatar)
        {
            if (editAvatar == null)
            {
                return;
            }

            var change = new SChange { Action = EAction.EditAvatar, Avatar = editAvatar };

            lock (_QueueMutex)
            {
                _Queue.Enqueue(change);
            }
        }

        public static CProfile[] GetProfiles()
        {
            if (_Profiles.Count == 0)
            {
                return new CProfile[0];
            }

            var list = new List<CProfile>(_Profiles.Values);
            list.Sort(_AlphaNumericCompareByPlayerName);
            return list.ToArray();
        }

        public static CProfile GetProfile(Guid profileId)
        {
            if (!IsProfileIdValid(profileId))
            {
                return null;
            }

            return _Profiles[profileId];
        }

        public static IEnumerable<CAvatar> GetAvatars()
        {
            if (_Avatars.Count == 0)
            {
                return null;
            }

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
            {
                return Guid.Empty;
            }

            profile.Id = Guid.NewGuid();
            _Profiles.Add(profile.Id, profile);
            _ProfilesChanged = true;
            return profile.Id;
        }

        public static int NewAvatar(string fileName)
        {
            var avatar = CAvatar.GetAvatar(fileName);
            if (avatar == null)
            {
                return -1;
            }

            avatar.Id = _AvatarIds.Dequeue();
            _Avatars.Add(avatar.Id, avatar);
            _AvatarsChanged = true;
            return avatar.Id;
        }

        public static void SaveProfiles()
        {
            foreach (var id in _Profiles.Keys)
            {
                _Profiles[id].SaveProfile();
            }
        }

        public static bool IsProfileIdValid(Guid profileId)
        {
            return profileId != Guid.Empty && _Profiles.ContainsKey(profileId);
        }

        public static bool IsAvatarIdValid(int avatarId)
        {
            return _Avatars.ContainsKey(avatarId);
        }
        #endregion public methods

        #region profile properties
        public static string GetPlayerName(Guid profileId, int playerNum = 0)
        {
            if (IsProfileIdValid(profileId))
            {
                return _Profiles[profileId].PlayerName;
            }

            var playerName = CLanguage.Translate("TR_SCREENNAMES_PLAYER");
            if (playerNum > 0)
            {
                playerName += " " + playerNum;
            }

            return playerName;
        }

        public static void SetPlayerName(Guid profileId, string playerName)
        {
            if (!IsProfileIdValid(profileId))
            {
                return;
            }

            _Profiles[profileId].PlayerName = playerName;
        }

        public static string GetProfileFileName(Guid profileId)
        {
            if (!IsProfileIdValid(profileId))
            {
                return String.Empty;
            }

            return Path.GetFileName(_Profiles[profileId].FilePath);
        }

        public static string AddGetPlayerName(Guid profileId, char chr)
        {
            if (!IsProfileIdValid(profileId))
            {
                return String.Empty;
            }

            _Profiles[profileId].PlayerName += chr;
            return _Profiles[profileId].PlayerName;
        }

        public static string GetDeleteCharInPlayerName(Guid profileId)
        {
            if (!IsProfileIdValid(profileId))
            {
                return String.Empty;
            }

            var profile = _Profiles[profileId];
            if (!String.IsNullOrEmpty(profile.PlayerName))
            {
                profile.PlayerName = profile.PlayerName.Remove(profile.PlayerName.Length - 1);
            }

            return profile.PlayerName;
        }

        public static EGameDifficulty GetDifficulty(Guid profileId)
        {
            return IsProfileIdValid(profileId) ? _Profiles[profileId].Difficulty : EGameDifficulty.TR_CONFIG_NORMAL;
        }

        public static void SetDifficulty(Guid profileId, EGameDifficulty difficulty)
        {
            if (!IsProfileIdValid(profileId))
            {
                return;
            }

            _Profiles[profileId].Difficulty = difficulty;
        }

        public static EUserRole GetUserRoleProfile(Guid profileId)
        {
            return IsProfileIdValid(profileId) ? _Profiles[profileId].UserRole : EUserRole.TR_USERROLE_GUEST;
        }

        public static void SetUserRoleProfile(Guid profileId, EUserRole option)
        {
            if (!IsProfileIdValid(profileId))
            {
                return;
            }

            //Only allow the change of TR_USERROLE_GUEST, TR_USERROLE_NORMAL and TR_USERROLE_ADMIN
            const EUserRole mask = EUserRole.TR_USERROLE_GUEST | EUserRole.TR_USERROLE_NORMAL | EUserRole.TR_USERROLE_ADMIN;
            option &= mask;
            _Profiles[profileId].UserRole = (_Profiles[profileId].UserRole & ~mask) | option;
        }

        public static EOffOn GetActive(Guid profileId)
        {
            return IsProfileIdValid(profileId) ? _Profiles[profileId].Active : EOffOn.TR_CONFIG_OFF;
        }

        public static void SetActive(Guid profileId, EOffOn option)
        {
            if (!IsProfileIdValid(profileId))
            {
                return;
            }

            _Profiles[profileId].Active = option;
        }

        public static bool IsGuestProfile(Guid profileId)
        {
            if (!IsProfileIdValid(profileId))
            {
                return true; // this will prevent from saving dummy profiles to highscore db
            }

            return _Profiles[profileId].UserRole <= EUserRole.TR_USERROLE_GUEST;
        }

        public static bool IsActive(Guid profileId)
        {
            if (!IsProfileIdValid(profileId))
            {
                return false;
            }

            return _Profiles[profileId].Active == EOffOn.TR_CONFIG_ON;
        }

        public static void SetAvatar(Guid profileId, int avatarId)
        {
            if (!IsProfileIdValid(profileId) || !IsAvatarIdValid(avatarId))
            {
                return;
            }

            _Profiles[profileId].Avatar = _Avatars[avatarId];
        }

        public static int GetAvatarId(Guid profileId)
        {
            if (!IsProfileIdValid(profileId) || _Profiles[profileId].Avatar == null)
            {
                return -1;
            }

            return _Profiles[profileId].Avatar.Id;
        }

        public static CAvatar GetAvatar(Guid profileId)
        {
            if (!IsProfileIdValid(profileId))
            {
                return null;
            }

            return _Profiles[profileId].Avatar;
        }

        //TODO: Remove this?
        public static Guid GetProfileId(Guid num)
        {
            return _Profiles[num].Id;
        }
        #endregion profile properties

        #region avatar texture
        public static CTextureRef GetAvatarTexture(int avatarId)
        {
            if (!IsAvatarIdValid(avatarId))
            {
                return null;
            }

            return _Avatars[avatarId].Texture;
        }

        public static CTextureRef GetAvatarTextureFromProfile(Guid profileId)
        {
            if (!IsProfileIdValid(profileId) || _Profiles[profileId].Avatar == null)
            {
                return null;
            }

            return _Profiles[profileId].Avatar.Texture;
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
                foreach (var id in ids)
                {
                    if (_Profiles[id].LoadProfile())
                    {
                        knownFiles.Add(Path.GetFileName(_Profiles[id].FilePath));
                    }
                    else
                    {
                        _Profiles.Remove(id);
                    }
                }
            }


            var files = new List<string>();
            foreach (var path in CConfig.ProfileFolders)
            {
                files.AddRange(CHelper.ListFiles(path, "*.xml", true, true));
            }

            foreach (var file in files)
            {
                if (knownFiles.Contains(Path.GetFileName(file)))
                {
                    continue;
                }

                var profile = new CProfile();

                if (profile.LoadProfile(file))
                {
                    _Profiles.Add(profile.Id, profile);
                }
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
                foreach (var id in ids)
                {
                    if (_Avatars[id].Reload())
                    {
                        knownFiles.Add(Path.GetFileName(_Avatars[id].FileName));
                    }
                    else
                    {
                        _Avatars.Remove(id);
                    }
                }
            }

            var files = new List<string>();
            foreach (var path in CConfig.ProfileFolders)
            {
                files.AddRange(CHelper.ListImageFiles(path, true, true));
            }

            foreach (var file in files)
            {
                if (knownFiles.Contains(Path.GetFileName(file)))
                {
                    continue;
                }

                var avatar = CAvatar.GetAvatar(file);
                if (avatar != null)
                {
                    avatar.Id = _AvatarIds.Dequeue();
                    _Avatars.Add(avatar.Id, avatar);
                }
            }

            _ProfilesChanged = true;
        }

        private static void _DeleteProfile(Guid profileId)
        {
            if (!IsProfileIdValid(profileId))
            {
                return;
            }

            if (string.IsNullOrEmpty(_Profiles[profileId].FilePath))
            {
                _RemoveProfile(profileId);
                return;
            }

            try
            {
                //Check if profile saved in config
                for (var i = 0; i < CSettings.MaxNumPlayer; i++)
                {
                    if (CConfig.Config.Game.Players[i] == GetProfileFileName(profileId))
                    {
                        CConfig.Config.Game.Players[i] = string.Empty;
                        CConfig.SaveConfig();
                    }
                }

                File.Delete(_Profiles[profileId].FilePath);
                _RemoveProfile(profileId);

                //Check if profile is selected in game
                for (var i = 0; i < CGame.Players.Length; i++)
                {
                    if (CGame.Players[i].ProfileId == profileId)
                    {
                        CGame.Players[i].ProfileId = Guid.Empty;
                    }
                }
            }
            catch (Exception)
            {
                CLog.Error("Can't delete Profile File " + _Profiles[profileId].FilePath);
            }

            _ProfilesChanged = true;
        }

        private static void _RemoveProfile(Guid profileId)
        {
            if (!IsProfileIdValid(profileId))
            {
                return;
            }

            _Profiles.Remove(profileId);
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
            var name = Path.GetFileName(fileName);

            foreach (var id in _Avatars.Keys)
            {
                if (Path.GetFileName(_Avatars[id].FileName) == name)
                {
                    return _Avatars[id];
                }
            }

            return null;
        }
        #endregion private methods
    }
}