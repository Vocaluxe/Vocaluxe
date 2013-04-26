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
using VocaluxeLib.Menu;

namespace Vocaluxe.Base
{
    static class CProfiles
    {
        private static readonly XmlWriterSettings _Settings = new XmlWriterSettings();
        private static List<SProfile> _Profiles;
        private static readonly List<SAvatar> _Avatars = new List<SAvatar>();

        public static SProfile[] Profiles
        {
            get { return _Profiles.ToArray(); }
        }

        public static int NumProfiles
        {
            get { return _Profiles.Count; }
        }

        public static SAvatar[] Avatars
        {
            get { return _Avatars.ToArray(); }
        }

        public static int NumAvatars
        {
            get { return _Avatars.Count; }
        }

        public static void Init()
        {
            _Settings.Indent = true;
            _Settings.Encoding = Encoding.UTF8;
            _Settings.ConformanceLevel = ConformanceLevel.Document;

            LoadAvatars();
            LoadProfiles();
        }

        public static string GetPlayerName(int profileID, int playerNum = 0)
        {
            if (IsProfileIDValid(profileID))
                return _Profiles[profileID].PlayerName;
            string playerName = CLanguage.Translate("TR_SCREENNAMES_PLAYER");
            if (playerNum > 0)
                playerName += " " + playerNum;
            return playerName;
        }

        public static int NewProfile(string fileName = "")
        {
            SProfile profile = new SProfile
                {
                    PlayerName = String.Empty,
                    Difficulty = EGameDifficulty.TR_CONFIG_EASY,
                    Avatar = new SAvatar(-1),
                    GuestProfile = EOffOn.TR_CONFIG_OFF,
                    Active = EOffOn.TR_CONFIG_ON,
                    ProfileFile = fileName != "" ? Path.Combine(CSettings.FolderProfiles, fileName) : String.Empty
                };

            if (File.Exists(profile.ProfileFile))
                return -1;

            _Profiles.Add(profile);
            return _Profiles.Count - 1;
        }

        public static void SaveProfiles()
        {
            for (int i = 0; i < _Profiles.Count; i++)
                _SaveProfile(i);
            LoadProfiles();
        }

        public static void LoadProfiles()
        {
            _Profiles = new List<SProfile>();
            List<string> files = new List<string>();
            files.AddRange(CHelper.ListFiles(CSettings.FolderProfiles, "*.xml", true, true));

            foreach (string file in files)
                _LoadProfile(file);

            _SortProfilesByName();
        }

        public static void LoadAvatars()
        {
            for (int i = 0; i < _Avatars.Count; i++)
            {
                STexture texture = _Avatars[i].Texture;
                CDraw.RemoveTexture(ref texture);
            }
            _Avatars.Clear();

            List<string> files = new List<string>();
            files.AddRange(CHelper.ListFiles(CSettings.FolderProfiles, "*.png", true, true));
            files.AddRange(CHelper.ListFiles(CSettings.FolderProfiles, "*.jpg", true, true));
            files.AddRange(CHelper.ListFiles(CSettings.FolderProfiles, "*.jpeg", true, true));
            files.AddRange(CHelper.ListFiles(CSettings.FolderProfiles, "*.bmp", true, true));

            foreach (string file in files)
            {
                STexture tex = CDraw.AddTexture(file);

                if (tex.Index != -1)
                {
                    SAvatar avatar = new SAvatar {Texture = tex, FileName = Path.GetFileName(file)};
                    _Avatars.Add(avatar);
                }
            }
        }

        public static string AddGetPlayerName(int profileID, char chr)
        {
            if (!IsProfileIDValid(profileID))
                return String.Empty;

            SProfile profile = _Profiles[profileID];
            profile.PlayerName += chr;
            _Profiles[profileID] = profile;

            return profile.PlayerName;
        }

        public static string GetDeleteCharInPlayerName(int profileID)
        {
            if (!IsProfileIDValid(profileID))
                return String.Empty;

            SProfile profile = _Profiles[profileID];
            if (profile.PlayerName != "")
                profile.PlayerName = profile.PlayerName.Remove(profile.PlayerName.Length - 1);
            _Profiles[profileID] = profile;

            return profile.PlayerName;
        }

        public static bool IsProfileIDValid(int profileID)
        {
            return profileID >= 0 && profileID < _Profiles.Count;
        }

        public static EGameDifficulty GetDifficulty(int profileID)
        {
            return IsProfileIDValid(profileID) ? _Profiles[profileID].Difficulty : EGameDifficulty.TR_CONFIG_NORMAL;
        }

        public static void SetDifficulty(int profileID, EGameDifficulty difficulty)
        {
            if (!IsProfileIDValid(profileID))
                return;

            SProfile profile = _Profiles[profileID];
            profile.Difficulty = difficulty;
            _Profiles[profileID] = profile;
        }

        public static EOffOn GetGuestProfile(int profileID)
        {
            return IsProfileIDValid(profileID) ? _Profiles[profileID].GuestProfile : EOffOn.TR_CONFIG_OFF;
        }

        public static EOffOn GetActive(int profileID)
        {
            return IsProfileIDValid(profileID) ? _Profiles[profileID].Active : EOffOn.TR_CONFIG_OFF;
        }

        public static void SetGuestProfile(int profileID, EOffOn option)
        {
            if (!IsProfileIDValid(profileID))
                return;

            SProfile profile = _Profiles[profileID];
            profile.GuestProfile = option;
            _Profiles[profileID] = profile;
        }

        public static void SetActive(int profileID, EOffOn option)
        {
            if (!IsProfileIDValid(profileID))
                return;
            SProfile profile = _Profiles[profileID];
            profile.Active = option;
            _Profiles[profileID] = profile;
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

        public static void SetAvatar(int profileID, int avatarNr)
        {
            if (!IsProfileIDValid(profileID) || avatarNr < 0 || avatarNr >= _Avatars.Count)
                return;

            SProfile profile = _Profiles[profileID];
            profile.Avatar = _Avatars[avatarNr];
            _Profiles[profileID] = profile;
        }

        public static int GetAvatarNr(int profileID)
        {
            if (!IsProfileIDValid(profileID))
                return 0;

            for (int i = 0; i < _Avatars.Count; i++)
            {
                if (_Profiles[profileID].Avatar.FileName == _Avatars[i].FileName)
                    return i;
            }

            return 0;
        }

        public static void DeleteProfile(int profileID)
        {
            if (!IsProfileIDValid(profileID))
                return;
            if (_Profiles[profileID].ProfileFile == "")
                return;
            try
            {
                //Check if profile saved in config
                for (int i = 0; i < CConfig.Players.Length; i++)
                {
                    if (CConfig.Players[i] == _Profiles[profileID].ProfileFile)
                    {
                        CConfig.Players[i] = string.Empty;
                        CConfig.SaveConfig();
                    }
                }
                File.Delete(_Profiles[profileID].ProfileFile);
                _Profiles.RemoveAt(profileID);
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
                CLog.LogError("Can't delete Profile File " + _Profiles[profileID].ProfileFile + ".xml");
            }
        }

        private static void _SortProfilesByName()
        {
            _Profiles.Sort(_CompareByPlayerName);
        }

        private static int _CompareByPlayerName(SProfile a, SProfile b)
        {
            return String.CompareOrdinal(a.PlayerName, b.PlayerName);
        }

        #region private methods
        private static SAvatar _GetAvatar(string fileName)
        {
            foreach (SAvatar avatar in _Avatars)
            {
                if (fileName == avatar.FileName)
                    return avatar;
            }
            return new SAvatar(-1);
        }

        private static void _SaveProfile(int profileID)
        {
            if (!IsProfileIDValid(profileID))
                return;

            if (_Profiles[profileID].ProfileFile == "")
            {
                string filename = string.Empty;
                foreach (char chr in _Profiles[profileID].PlayerName)
                {
                    if (char.IsLetter(chr))
                        filename += chr.ToString();
                }

                if (filename == "")
                    filename = "1";

                int i = 0;
                while (File.Exists(Path.Combine(CSettings.FolderProfiles, filename + ".xml")))
                {
                    i++;
                    if (!File.Exists(Path.Combine(CSettings.FolderProfiles, filename + i + ".xml")))
                        filename += i;
                }

                SProfile profile = _Profiles[profileID];
                profile.ProfileFile = Path.Combine(CSettings.FolderProfiles, filename + ".xml");
                _Profiles[profileID] = profile;
            }

            XmlWriter writer;
            try
            {
                writer = XmlWriter.Create(_Profiles[profileID].ProfileFile, _Settings);
            }
            catch (Exception e)
            {
                CLog.LogError("Error creating/opening Profile File " + _Profiles[profileID].ProfileFile + ": " + e.Message);
                return;
            }
            try
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("root");

                writer.WriteStartElement("Info");
                writer.WriteElementString("PlayerName", _Profiles[profileID].PlayerName);
                writer.WriteElementString("Difficulty", Enum.GetName(typeof(EGameDifficulty), _Profiles[profileID].Difficulty));
                writer.WriteElementString("Avatar", _Profiles[profileID].Avatar.FileName);
                writer.WriteElementString("GuestProfile", Enum.GetName(typeof(EOffOn), _Profiles[profileID].GuestProfile));
                writer.WriteElementString("Active", Enum.GetName(typeof(EOffOn), _Profiles[profileID].Active));
                writer.WriteEndElement();

                writer.WriteEndElement(); //end of root
                writer.WriteEndDocument();

                writer.Flush();
            }
            finally
            {
                writer.Close();
            }
        }

        private static void _LoadProfile(string fileName)
        {
            SProfile profile = new SProfile {ProfileFile = Path.Combine(CSettings.FolderProfiles, fileName)};

            CXMLReader xmlReader = CXMLReader.OpenFile(profile.ProfileFile);
            if (xmlReader == null)
                return;

            string value = String.Empty;
            if (xmlReader.GetValue("//root/Info/PlayerName", out value, value))
            {
                profile.PlayerName = value;

                profile.Difficulty = EGameDifficulty.TR_CONFIG_EASY;
                xmlReader.TryGetEnumValue("//root/Info/Difficulty", ref profile.Difficulty);

                profile.Avatar = new SAvatar(-1);
                if (xmlReader.GetValue("//root/Info/Avatar", out value, value))
                    profile.Avatar = _GetAvatar(value);

                profile.GuestProfile = EOffOn.TR_CONFIG_OFF;
                xmlReader.TryGetEnumValue("//root/Info/GuestProfile", ref profile.GuestProfile);

                profile.Active = EOffOn.TR_CONFIG_ON;
                xmlReader.TryGetEnumValue("//root/Info/Active", ref profile.Active);

                _Profiles.Add(profile);
            }
            else
                CLog.LogError("Can't find PlayerName in Profile File: " + fileName);
        }
        #endregion private methods
    }
}