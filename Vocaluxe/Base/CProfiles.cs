using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;

namespace Vocaluxe.Base
{    
    static class CProfiles
    {
        private static XmlWriterSettings _settings = new XmlWriterSettings();
        private static List<SProfile> _Profiles;
        private static List<SAvatar> _Avatars = new List<SAvatar>();

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
            _settings.Indent = true;
            _settings.Encoding = System.Text.Encoding.UTF8;
            _settings.ConformanceLevel = ConformanceLevel.Document;

            LoadAvatars();
            LoadProfiles();
        }

        public static int NewProfile()
        {
            return NewProfile(String.Empty);
        }

        public static int NewProfile(string FileName)
        {
            SProfile profile = new SProfile();

            profile.PlayerName = String.Empty;
            profile.Difficulty = EGameDifficulty.TR_CONFIG_EASY;
            profile.Avatar = new SAvatar(-1);
            profile.GuestProfile = EOffOn.TR_CONFIG_OFF;
            profile.Active = EOffOn.TR_CONFIG_ON;
            
            if (FileName != String.Empty)
                profile.ProfileFile = Path.Combine(CSettings.sFolderProfiles, FileName);
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
            {
                SaveProfile(i);
            }
            LoadProfiles();
        }

        public static void LoadProfiles()
        {
            _Profiles = new List<SProfile>();
            List<string> files = new List<string>();
            files.AddRange(CHelper.ListFiles(CSettings.sFolderProfiles, "*.xml", true, true));

            foreach (string file in files)
            {
                LoadProfile(file);
            }

            SortProfilesByName();
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
            files.AddRange(CHelper.ListFiles(CSettings.sFolderProfiles, "*.png", true, true));
            files.AddRange(CHelper.ListFiles(CSettings.sFolderProfiles, "*.jpg", true, true));
            files.AddRange(CHelper.ListFiles(CSettings.sFolderProfiles, "*.jpeg", true, true));
            files.AddRange(CHelper.ListFiles(CSettings.sFolderProfiles, "*.bmp", true, true));

            foreach (string file in files)
            {
                STexture tex = CDraw.AddTexture(file);

                if (tex.index != -1)
                {
                    SAvatar avatar = new SAvatar();
                    avatar.Texture = tex;
                    avatar.FileName = Path.GetFileName(file);
                    _Avatars.Add(avatar);
                }
            }
        }

        public static string AddGetPlayerName(int ProfileNr, char Chr)
        {
            if (ProfileNr < 0 || ProfileNr >= _Profiles.Count)
                return String.Empty;

            SProfile profile = _Profiles[ProfileNr];
            profile.PlayerName += Chr;
            _Profiles[ProfileNr] = profile;

            return profile.PlayerName;
        }

        public static string GetPlayerName(int ProfileNr)
        {
            if (ProfileNr < 0 || ProfileNr >= _Profiles.Count)
                return String.Empty;

            return _Profiles[ProfileNr].PlayerName;
        }

        public static string GetDeleteCharInPlayerName(int ProfileNr)
        {
            if (ProfileNr < 0 || ProfileNr >= _Profiles.Count)
                return String.Empty;

            SProfile profile = _Profiles[ProfileNr];
            if (profile.PlayerName.Length > 0)
                profile.PlayerName = profile.PlayerName.Remove(profile.PlayerName.Length - 1);
            _Profiles[ProfileNr] = profile;

            return profile.PlayerName;
        }

        public static EGameDifficulty GetDifficulty(int ProfileNr)
        {
            if (ProfileNr < 0 || ProfileNr >= _Profiles.Count)
                return EGameDifficulty.TR_CONFIG_NORMAL;

            return _Profiles[ProfileNr].Difficulty;
        }

        public static void SetDifficulty(int ProfileNr, EGameDifficulty Difficulty)
        {
            if (ProfileNr < 0 || ProfileNr >= _Profiles.Count)
                return;

            SProfile profile = _Profiles[ProfileNr];
            profile.Difficulty = Difficulty;
            _Profiles[ProfileNr] = profile;
        }

        public static EOffOn GetGuestProfile(int ProfileNr)
        {
            if (ProfileNr < 0 || ProfileNr >= _Profiles.Count)
                return EOffOn.TR_CONFIG_OFF;

            return _Profiles[ProfileNr].GuestProfile;
        }

        public static EOffOn GetActive(int ProfileNr)
        {
            if (ProfileNr < 0 || ProfileNr >= _Profiles.Count)
                return EOffOn.TR_CONFIG_OFF;

            return _Profiles[ProfileNr].Active;
        }

        public static void SetGuestProfile(int ProfileNr, EOffOn Option)
        {
            if (ProfileNr < 0 || ProfileNr >= _Profiles.Count)
                return;

            SProfile profile = _Profiles[ProfileNr];
            profile.GuestProfile = Option;
            _Profiles[ProfileNr] = profile;
        }

        public static void SetActive(int ProfileNr, EOffOn Option)
        {
            if (ProfileNr < 0 || ProfileNr >= _Profiles.Count)
                return;
            SProfile profile = _Profiles[ProfileNr];
            profile.Active = Option;
            _Profiles[ProfileNr] = profile;
        }

        public static bool IsGuestProfile(int ProfileNr)
        {
            if (ProfileNr < 0 || ProfileNr >= _Profiles.Count)
                return true;    // this will prevent from saving dummy profiles to highscore db

            return _Profiles[ProfileNr].GuestProfile == EOffOn.TR_CONFIG_ON;
        }

        public static bool IsActive(int ProfileNr)
        {
            if (ProfileNr < 0 || ProfileNr >= _Profiles.Count)
                return false;
            return _Profiles[ProfileNr].Active == EOffOn.TR_CONFIG_ON;
        }

        public static void SetAvatar(int ProfileNr, int AvatarNr)
        {
            if (ProfileNr < 0 || ProfileNr >= _Profiles.Count || AvatarNr < 0 || AvatarNr >= _Avatars.Count)
                return;

            SProfile profile = _Profiles[ProfileNr];
            profile.Avatar = _Avatars[AvatarNr];
            _Profiles[ProfileNr] = profile;
        }

        public static int GetAvatarNr(int ProfileNr)
        {
            if (ProfileNr < 0 || ProfileNr >= _Profiles.Count)
                return 0;

            for (int i = 0; i < _Avatars.Count; i++)
            {
                if (_Profiles[ProfileNr].Avatar.FileName == _Avatars[i].FileName)
                    return i;
            }

            return 0;
        }

        public static void DeleteProfile(int ProfileID)
        {
            if (ProfileID < 0 || ProfileID >= _Profiles.Count)
                return;
            if (_Profiles[ProfileID].ProfileFile != String.Empty)
            {
                try
                {
                    File.Delete(_Profiles[ProfileID].ProfileFile);
                    _Profiles.RemoveAt(ProfileID);
                }
                catch (Exception)
                {
                    CLog.LogError("Can't delete Profile File " + _Profiles[ProfileID].ProfileFile + ".xml");
                }
            }
        }

        private static void SortProfilesByName()
        {
            _Profiles.Sort(CompareByPlayerName);
        }

        private static int CompareByPlayerName(SProfile a, SProfile b)
        {
            return String.Compare(a.PlayerName, b.PlayerName);
        }

        #region private methods
        private static SAvatar GetAvatar(string FileName)
        {
            foreach (SAvatar avatar in _Avatars)
            {
                if (FileName == avatar.FileName)
                    return avatar;
            }
            return new SAvatar(-1);
        }

        private static void SaveProfile(int ProfileID)
        {
            if (ProfileID < 0 || ProfileID >= _Profiles.Count)
                return;

            if (_Profiles[ProfileID].ProfileFile == String.Empty)
            {
                string filename = string.Empty;
                foreach (char chr in _Profiles[ProfileID].PlayerName)
                {
                    if (char.IsLetter(chr))
                        filename += chr.ToString();
                }

                if (filename == String.Empty)
                    filename = "1";

                int i = 0;
                while(File.Exists(Path.Combine(CSettings.sFolderProfiles, filename + ".xml")))
                {
                    i++;
                    if(!File.Exists(Path.Combine(CSettings.sFolderProfiles, filename + i + ".xml")))
                    {
                        filename += i;
                    }
                }

                SProfile profile = _Profiles[ProfileID];
                profile.ProfileFile = Path.Combine(CSettings.sFolderProfiles, filename + ".xml");
                _Profiles[ProfileID] = profile;
            }

            XmlWriter writer;
            try
            {
                writer = XmlWriter.Create(_Profiles[ProfileID].ProfileFile, _settings);
            }
            catch (Exception e)
            {
                CLog.LogError("Error creating/opening Profile File " + _Profiles[ProfileID].ProfileFile + ": " + e.Message);
                return;
            }

            if (writer == null)
            {
                CLog.LogError("Error creating/opening Profile File " + _Profiles[ProfileID].ProfileFile);
                return;
            }

            writer.WriteStartDocument();
            writer.WriteStartElement("root");

            writer.WriteStartElement("Info");
            writer.WriteElementString("PlayerName", _Profiles[ProfileID].PlayerName);
            writer.WriteElementString("Difficulty", Enum.GetName(typeof(EGameDifficulty), _Profiles[ProfileID].Difficulty));
            writer.WriteElementString("Avatar", _Profiles[ProfileID].Avatar.FileName);
            writer.WriteElementString("GuestProfile", Enum.GetName(typeof(EOffOn), _Profiles[ProfileID].GuestProfile));
            writer.WriteElementString("Active", Enum.GetName(typeof(EOffOn), _Profiles[ProfileID].Active));
            writer.WriteEndElement();

            writer.WriteEndElement(); //end of root
            writer.WriteEndDocument();

            writer.Flush();
            writer.Close();
        }

        private static void LoadProfile(string FileName)
        {
            SProfile profile = new SProfile();
            profile.ProfileFile = Path.Combine(CSettings.sFolderProfiles, FileName);

            CXMLReader xmlReader = CXMLReader.OpenFile(profile.ProfileFile);
            if (xmlReader == null)
                return;

            string value = String.Empty;
            if (xmlReader.GetValue("//root/Info/PlayerName", ref value, value))
            {
                profile.PlayerName = value;

                profile.Difficulty = EGameDifficulty.TR_CONFIG_EASY;
                xmlReader.TryGetEnumValue<EGameDifficulty>("//root/Info/Difficulty", ref profile.Difficulty);

                profile.Avatar = new SAvatar(-1);
                if (xmlReader.GetValue("//root/Info/Avatar", ref value, value))
                {
                    profile.Avatar = GetAvatar(value);
                }

                profile.GuestProfile = EOffOn.TR_CONFIG_OFF;
                xmlReader.TryGetEnumValue<EOffOn>("//root/Info/GuestProfile", ref profile.GuestProfile);

                profile.Active = EOffOn.TR_CONFIG_ON;
                xmlReader.TryGetEnumValue<EOffOn>("//root/Info/Active", ref profile.Active);

                _Profiles.Add(profile);
            }
            else
            {
                CLog.LogError("Can't find PlayerName in Profile File: " + FileName);
            }
        }
        #endregion private methods
    }
}
