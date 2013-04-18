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

        public static int NewProfile()
        {
            return NewProfile(String.Empty);
        }

        public static int NewProfile(string fileName)
        {
            SProfile profile = new SProfile();

            profile.PlayerName = String.Empty;
            profile.Difficulty = EGameDifficulty.TR_CONFIG_EASY;
            profile.Avatar = new SAvatar(-1);
            profile.GuestProfile = EOffOn.TR_CONFIG_OFF;
            profile.Active = EOffOn.TR_CONFIG_ON;

            if (fileName.Length > 0)
                profile.ProfileFile = Path.Combine(CSettings.FolderProfiles, fileName);
            else
                profile.ProfileFile = String.Empty;

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
            List<SProfile> _OldProfiles = null;
            if (_Profiles != null)
                _OldProfiles = _Profiles;
            _Profiles = new List<SProfile>();
            List<string> files = new List<string>();
            files.AddRange(CHelper.ListFiles(CSettings.FolderProfiles, "*.xml", true, true));

            foreach (string file in files)
                _LoadProfile(file);

            _SortProfilesByName();

            //Find profile-id if new profile is created
            if (_OldProfiles != null)
            {
                int _NewProfileId = -1;
                for (int i = 0; i < _OldProfiles.Count; i++)
                {
                    if (_Profiles[i].ProfileFile != _OldProfiles[i].ProfileFile)
                    {
                        _NewProfileId = i;
                        break;
                    }
                }
                if (_NewProfileId != -1)
                {
                    for (int i = 0; i < CGame.Player.Length; i++)
                    {
                        if (CGame.Player[i].ProfileID >= _NewProfileId)
                            CGame.Player[i].ProfileID++;
                    }
                }
            }
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
                    SAvatar avatar = new SAvatar();
                    avatar.Texture = tex;
                    avatar.FileName = Path.GetFileName(file);
                    _Avatars.Add(avatar);
                }
            }
        }

        public static string AddGetPlayerName(int profileNr, char chr)
        {
            if (profileNr < 0 || profileNr >= _Profiles.Count)
                return String.Empty;

            SProfile profile = _Profiles[profileNr];
            profile.PlayerName += chr;
            _Profiles[profileNr] = profile;

            return profile.PlayerName;
        }

        public static string GetPlayerName(int profileNr)
        {
            if (profileNr < 0 || profileNr >= _Profiles.Count)
                return String.Empty;

            return _Profiles[profileNr].PlayerName;
        }

        public static string GetDeleteCharInPlayerName(int profileNr)
        {
            if (profileNr < 0 || profileNr >= _Profiles.Count)
                return String.Empty;

            SProfile profile = _Profiles[profileNr];
            if (profile.PlayerName.Length > 0)
                profile.PlayerName = profile.PlayerName.Remove(profile.PlayerName.Length - 1);
            _Profiles[profileNr] = profile;

            return profile.PlayerName;
        }

        public static EGameDifficulty GetDifficulty(int profileNr)
        {
            if (profileNr < 0 || profileNr >= _Profiles.Count)
                return EGameDifficulty.TR_CONFIG_NORMAL;

            return _Profiles[profileNr].Difficulty;
        }

        public static void SetDifficulty(int profileNr, EGameDifficulty difficulty)
        {
            if (profileNr < 0 || profileNr >= _Profiles.Count)
                return;

            SProfile profile = _Profiles[profileNr];
            profile.Difficulty = difficulty;
            _Profiles[profileNr] = profile;
        }

        public static EOffOn GetGuestProfile(int profileNr)
        {
            if (profileNr < 0 || profileNr >= _Profiles.Count)
                return EOffOn.TR_CONFIG_OFF;

            return _Profiles[profileNr].GuestProfile;
        }

        public static EOffOn GetActive(int profileNr)
        {
            if (profileNr < 0 || profileNr >= _Profiles.Count)
                return EOffOn.TR_CONFIG_OFF;

            return _Profiles[profileNr].Active;
        }

        public static void SetGuestProfile(int profileNr, EOffOn option)
        {
            if (profileNr < 0 || profileNr >= _Profiles.Count)
                return;

            SProfile profile = _Profiles[profileNr];
            profile.GuestProfile = option;
            _Profiles[profileNr] = profile;
        }

        public static void SetActive(int profileNr, EOffOn option)
        {
            if (profileNr < 0 || profileNr >= _Profiles.Count)
                return;
            SProfile profile = _Profiles[profileNr];
            profile.Active = option;
            _Profiles[profileNr] = profile;
        }

        public static bool IsGuestProfile(int profileNr)
        {
            if (profileNr < 0 || profileNr >= _Profiles.Count)
                return true; // this will prevent from saving dummy profiles to highscore db

            return _Profiles[profileNr].GuestProfile == EOffOn.TR_CONFIG_ON;
        }

        public static bool IsActive(int profileNr)
        {
            if (profileNr < 0 || profileNr >= _Profiles.Count)
                return false;
            return _Profiles[profileNr].Active == EOffOn.TR_CONFIG_ON;
        }

        public static void SetAvatar(int profileNr, int avatarNr)
        {
            if (profileNr < 0 || profileNr >= _Profiles.Count || avatarNr < 0 || avatarNr >= _Avatars.Count)
                return;

            SProfile profile = _Profiles[profileNr];
            profile.Avatar = _Avatars[avatarNr];
            _Profiles[profileNr] = profile;
        }

        public static int GetAvatarNr(int profileNr)
        {
            if (profileNr < 0 || profileNr >= _Profiles.Count)
                return 0;

            for (int i = 0; i < _Avatars.Count; i++)
            {
                if (_Profiles[profileNr].Avatar.FileName == _Avatars[i].FileName)
                    return i;
            }

            return 0;
        }

        public static void DeleteProfile(int profileID)
        {
            if (profileID < 0 || profileID >= _Profiles.Count)
                return;
            if (_Profiles[profileID].ProfileFile.Length > 0)
            {
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
                    for (int i = 0; i < CGame.Player.Length; i++)
                    {
                        if (CGame.Player[i].ProfileID > profileID)
                            CGame.Player[i].ProfileID--;
                        else if (CGame.Player[i].ProfileID == profileID)
                            CGame.Player[i].ProfileID = -1;
                    }
                }
                catch (Exception)
                {
                    CLog.LogError("Can't delete Profile File " + _Profiles[profileID].ProfileFile + ".xml");
                }
            }
        }

        private static void _SortProfilesByName()
        {
            _Profiles.Sort(_CompareByPlayerName);
        }

        private static int _CompareByPlayerName(SProfile a, SProfile b)
        {
            return String.Compare(a.PlayerName, b.PlayerName);
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
            if (profileID < 0 || profileID >= _Profiles.Count)
                return;

            if (_Profiles[profileID].ProfileFile.Length == 0)
            {
                string filename = string.Empty;
                foreach (char chr in _Profiles[profileID].PlayerName)
                {
                    if (char.IsLetter(chr))
                        filename += chr.ToString();
                }

                if (filename.Length == 0)
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
                writer = null;
            }
        }

        private static void _LoadProfile(string fileName)
        {
            SProfile profile = new SProfile();
            profile.ProfileFile = Path.Combine(CSettings.FolderProfiles, fileName);

            CXMLReader xmlReader = CXMLReader.OpenFile(profile.ProfileFile);
            if (xmlReader == null)
                return;

            string value = String.Empty;
            if (xmlReader.GetValue("//root/Info/PlayerName", ref value, value))
            {
                profile.PlayerName = value;

                profile.Difficulty = EGameDifficulty.TR_CONFIG_EASY;
                xmlReader.TryGetEnumValue("//root/Info/Difficulty", ref profile.Difficulty);

                profile.Avatar = new SAvatar(-1);
                if (xmlReader.GetValue("//root/Info/Avatar", ref value, value))
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