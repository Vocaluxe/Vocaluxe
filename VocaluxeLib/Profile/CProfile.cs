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
using System.IO;
using System.Xml;

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
        public int ID;

        public string PlayerName;
        public string FilePath;
        public string FileName;
        public byte[] PasswordHash;
        public byte[] PasswordSalt;

        public EGameDifficulty Difficulty;

        public CAvatar Avatar { get; set; }

        public EUserRole UserRole;
        public EOffOn Active;

        public CProfile()
        {
            PlayerName = String.Empty;
            Difficulty = EGameDifficulty.TR_CONFIG_EASY;
            UserRole = EUserRole.TR_USERROLE_NORMAL;
            Active = EOffOn.TR_CONFIG_ON;

            FileName = String.Empty;
        }

        public bool LoadProfile(string pathName, string fileName)
        {
            FilePath = pathName;
            FileName = fileName;
            return LoadProfile();
        }

        public bool LoadProfile(string file)
        {
            FileName = Path.GetFileName(file);
            FilePath = Path.GetDirectoryName(file);

            return LoadProfile();
        }

        public bool LoadProfile()
        {
            CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(FilePath, FileName));
            if (xmlReader == null)
                return false;

            string value = String.Empty;
            if (xmlReader.GetValue("//root/Info/PlayerName", out value, value))
            {
                PlayerName = value;
                string avatarFileName;
                xmlReader.TryGetEnumValue("//root/Info/Difficulty", ref Difficulty);
                xmlReader.GetValue("//root/Info/Avatar", out avatarFileName, String.Empty);
                Avatar = CBase.Profiles.GetAvatarByFilename(avatarFileName);
                xmlReader.TryGetEnumValue("//root/Info/UserRole", ref UserRole);
                xmlReader.TryGetEnumValue("//root/Info/Active", ref Active);
                string passwordHash;
                xmlReader.GetValue("//root/Info/PasswordHash", out passwordHash, "");
                PasswordHash = !string.IsNullOrEmpty(passwordHash) ? Convert.FromBase64String(passwordHash) : null;
                string passwordSalt;
                xmlReader.GetValue("//root/Info/PasswordSalt", out passwordSalt, "");
                PasswordSalt = !string.IsNullOrEmpty(passwordSalt) ? Convert.FromBase64String(passwordSalt) : null;
                return true;
            }

            CBase.Log.LogError("Can't find PlayerName in Profile File: " + FileName);
            return false;
        }

        public void SaveProfile()
        {
            if (String.IsNullOrEmpty(FilePath))
                FilePath = Path.Combine(CBase.Settings.GetDataPath(), CBase.Settings.GetFolderProfiles());
            if (FileName == String.Empty)
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

                FileName = CHelper.GetUniqueFileName(FilePath, filename + ".xml", false);
            }

            XmlWriter writer;
            try
            {
                writer = XmlWriter.Create(Path.Combine(FilePath, FileName), CBase.Config.GetXMLSettings());
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Error creating/opening Profile File " + FileName + ": " + e.Message);
                return;
            }
            try
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("root");

                writer.WriteStartElement("Info");
                writer.WriteElementString("PlayerName", PlayerName);
                writer.WriteElementString("Difficulty", Enum.GetName(typeof(EGameDifficulty), Difficulty));
                writer.WriteElementString("Avatar", Path.GetFileName(Avatar.FileName));
                writer.WriteElementString("UserRole", Enum.GetName(typeof(EUserRole), UserRole));
                writer.WriteElementString("Active", Enum.GetName(typeof(EOffOn), Active));
                if (PasswordHash != null)
                    writer.WriteElementString("PasswordHash", Convert.ToBase64String(PasswordHash));
                if (PasswordSalt != null)
                    writer.WriteElementString("PasswordSalt", Convert.ToBase64String(PasswordSalt));
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
    }
}