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
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using VocaluxeLib.Log;
using VocaluxeLib.Xml;

namespace VocaluxeLib.Profile
{
    [Flags]
    public enum EProfileChangedFlags
    {
        None = 1,
        Avatar = 2,
        Profile = 4
    }

    public delegate void ProfileChangedCallback(EProfileChangedFlags typeChanged);

    public class CProfile
    {
        private struct SOldXmlProfile
        {
#pragma warning disable 649
            public CProfile Info;
#pragma warning restore 649
        }

        [DefaultValue(null)] public Guid ID;

        public string PlayerName;
        [XmlIgnore] public string FilePath;
        [DefaultValue(null)] public byte[] PasswordHash;
        [DefaultValue(null)] public byte[] PasswordSalt;

        public EGameDifficulty Difficulty;

        [XmlIgnore]
        public CAvatar Avatar { get; set; }

        public EUserRole UserRole;
        public EOffOn Active;

        [XmlElement("Avatar")]
        // ReSharper disable UnusedMember.Global
        public string AvatarFileName
            // ReSharper restore UnusedMember.Global
        {
            get { return Path.GetFileName(Avatar.FileName); }
            set
            {
                Avatar = CBase.Profiles.GetAvatarByFilename(value);
                if (Avatar == null)
                    CLog.Error("Avatar '" + value + "' not found");
            }
        }

        public CProfile()
        {
            ID = Guid.NewGuid();
            PlayerName = String.Empty;
            Difficulty = EGameDifficulty.TR_CONFIG_EASY;
            UserRole = EUserRole.TR_USERROLE_NORMAL;
            Active = EOffOn.TR_CONFIG_ON;
        }

        public bool LoadProfile(string filePath)
        {
            FilePath = filePath;

            return LoadProfile();
        }

        public bool LoadProfile()
        {
            var xml = new CXmlDeserializer();
            try
            {
                xml.Deserialize(FilePath, this);
                //If ID couldn't be loaded, generate a new one and save it
                if (ID == Guid.Empty)
                {
                    ID = Guid.NewGuid();
                    SaveProfile();
                }
            }
            catch (Exception e)
            {
                if (_ConvertProfile(ref e))
                    return true;
                CLog.Error("Error loading profile file " + Path.GetFileName(FilePath) + ": " + e.Message);
                return false;
            }
            return true;
        }

        private bool _ConvertProfile(ref Exception e)
        {
            var xml = new CXmlDeserializer();
            var ser = new CXmlSerializer();
            try
            {
                var old = xml.Deserialize<SOldXmlProfile>(FilePath);
                string newXml = ser.Serialize(old.Info);
                xml.DeserializeString(newXml, this);
                if (ID == null)
                    ID = Guid.NewGuid();
                ser.Serialize(FilePath, this);

            }
            catch (Exception e2)
            {
                if (!(e2 is CXmlException))
                    e = e2;
                return false;
            }
            return true;
        }

        public void SaveProfile()
        {
            if (String.IsNullOrEmpty(FilePath))
            {
                string filename = string.Empty;
                // ReSharper disable LoopCanBeConvertedToQuery
                foreach (char chr in PlayerName)
                    // ReSharper restore LoopCanBeConvertedToQuery
                {
                    if (char.IsLetter(chr))
                        filename += chr.ToString();
                }

                if (filename == "")
                    filename = "1";

                FilePath = CHelper.GetUniqueFileName(Path.Combine(CBase.Settings.GetDataPath(), CBase.Settings.GetFolderProfiles()), filename + ".xml");
            }

            var xml = new CXmlSerializer();
            xml.Serialize(FilePath, this);
        }
    }
}