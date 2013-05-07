using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace VocaluxeLib.Profile
{
    public class CProfile
    {
        private static readonly XmlWriterSettings _Settings = new XmlWriterSettings()
            { 
                Indent = true,
                Encoding = Encoding.UTF8,
                ConformanceLevel = ConformanceLevel.Document
            };

        public int ID;
        
        public string PlayerName;
        public string FileName;
        public string AvatarFileName;

        public EGameDifficulty Difficulty;
        public CAvatar Avatar;

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
            return LoadProfile(FileName);
        }

        public bool LoadProfile(string NewFileName)
        {
            FileName = NewFileName;

            CXMLReader xmlReader = CXMLReader.OpenFile(FileName);
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
            if (FileName == String.Empty)
            {
                string filename = string.Empty;
                foreach (char chr in PlayerName)
                {
                    if (char.IsLetter(chr))
                        filename += chr.ToString();
                }

                if (filename == "")
                    filename = "1";

                int i = 0;
                while (File.Exists(Path.Combine(CBase.Settings.GetFolderProfiles(), filename + ".xml")))
                {
                    i++;
                    if (!File.Exists(Path.Combine(CBase.Settings.GetFolderProfiles(), filename + i + ".xml")))
                        filename += i;
                }

                FileName = Path.Combine(Environment.CurrentDirectory, CBase.Settings.GetFolderProfiles(), filename + ".xml");
            }

            if (FileName == String.Empty)
                return;

            XmlWriter writer;
            try
            {
                writer = XmlWriter.Create(FileName, _Settings);
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
