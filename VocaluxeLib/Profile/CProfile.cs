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
using System.IO;
using System.Text;
using System.Xml;

namespace VocaluxeLib.Profile
{
    public class CProfile
    {
        public int ID;

        public string PlayerName;
        public string FilePath;
        public string FileName;
        public string AvatarFileName;

        public EGameDifficulty Difficulty;

        private CAvatar _Avatar;
        public CAvatar Avatar
        {
            get { return _Avatar; }
            set
            {
                _Avatar = value;
                AvatarFileName = _Avatar.FileName;
            }
        }

        public EOffOn GuestProfile;
        public EOffOn Active;

        public CProfile()
        {
            PlayerName = String.Empty;
            Difficulty = EGameDifficulty.TR_CONFIG_EASY;
            Avatar = new CAvatar(-1);
            GuestProfile = EOffOn.TR_CONFIG_OFF;
            Active = EOffOn.TR_CONFIG_ON;

            AvatarFileName = String.Empty;
            FileName = String.Empty;
        }

        public bool LoadProfile()
        {
            return LoadProfile(FilePath, FileName);
        }

        public bool LoadProfile(string file)
        {
            FileName = Path.GetFileName(file);
            FilePath = Path.GetDirectoryName(file);

            return LoadProfile(FilePath, FileName);
        }

        public bool LoadProfile(string pathName, string fileName)
        {
            CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(FilePath, FileName));
            if (xmlReader == null)
                return false;

            string value = String.Empty;
            if (xmlReader.GetValue("//root/Info/PlayerName", out value, value))
            {
                PlayerName = value;

                xmlReader.TryGetEnumValue("//root/Info/Difficulty", ref Difficulty);
                xmlReader.GetValue("//root/Info/Avatar", out AvatarFileName, String.Empty);
                xmlReader.TryGetEnumValue("//root/Info/GuestProfile", ref GuestProfile);
                xmlReader.TryGetEnumValue("//root/Info/Active", ref Active);

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

                int i = 0;
                while (File.Exists(Path.Combine(FilePath, filename + ".xml")))
                {
                    i++;
                    if (!File.Exists(Path.Combine(FilePath, filename + i + ".xml")))
                        filename += i;
                }

                FileName = filename + ".xml";
            }

            if (FileName == String.Empty)
                return;

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
                writer.WriteElementString("GuestProfile", Enum.GetName(typeof(EOffOn), GuestProfile));
                writer.WriteElementString("Active", Enum.GetName(typeof(EOffOn), Active));
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