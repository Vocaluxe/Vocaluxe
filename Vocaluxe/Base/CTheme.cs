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
using System.Xml;
using Vocaluxe.Base.Fonts;
using Vocaluxe.Lib.Video;
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Base
{

    #region Structs
    struct STheme
    {
        public string Name;
        public string Author;
        public string SkinFolder;
        public int ThemeVersionMajor;
        public int ThemeVersionMinor;

        public string Path;
        public string FileName;
        public int PartyModeID;
    }

    struct SCursor
    {
        public string SkinName;

        public float W;
        public float H;

        public SThemeColor Color;
    }
    #endregion Structs

    static class CTheme
    {
        // Version number for main theme and skin files. Increment it, if you've changed something on the theme files!
        private const int _ThemeSystemVersion = 5;

        #region Vars
        private static readonly List<STheme> _Themes = new List<STheme>();
        public static string[] ThemeNames
        {
            get { return _Themes.Where(th => th.PartyModeID == -1).Select(th => th.Name).ToArray(); }
        }

        private static readonly List<CSkin> _Skins = new List<CSkin>();
        public static string[] SkinNames
        {
            get { return _Skins.Where(ak => ak.PartyModeID == -1).Select(ak => ak.Name).ToArray(); }
        }

        public static SCursor Cursor;
        #endregion Vars

        #region Theme and Skin loading and writing
        public static bool Init()
        {
            _ListThemes();
            ListSkins();

            return LoadSkins() && LoadTheme();
        }

        public static void Close()
        {
            UnloadSkins();
        }

        public static bool LoadSkins()
        {
            //Fail if loading of any skin failed or no skin was loaded
            //TODO: implement handlers that failing skins are logged and ignored
            bool result = false;
            for (int index = 0; index < _Skins.Count; index++)
            {
                if (_Skins[index].PartyModeID == -1)
                {
                    if (LoadSkin(index))
                        result = true;
                    else
                        return false;
                }
            }
            return result;
        }

        public static bool LoadSkin(int skinIndex)
        {
            return _Skins[skinIndex].Load();
        }

        public static void UnloadSkins()
        {
            for (int i = 0; i < _Skins.Count; i++)
            {
                if (_Skins[i].PartyModeID != -1)
                    continue;
                _Skins[i].Unload();
                _Skins.RemoveAt(i);
                i--;
            }
        }

        public static bool LoadTheme()
        {
            for (int i = 0; i < _Themes.Count; i++)
            {
                if (_Themes[i].Name == CConfig.Theme && _Themes[i].PartyModeID == -1)
                    return LoadTheme(i);
            }
            return false;
        }

        public static bool LoadTheme(int themeIndex)
        {
            if (themeIndex < 0 || themeIndex >= _Themes.Count)
            {
                CLog.LogError("Can't find Theme Index " + themeIndex);
                return false;
            }

            CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(_Themes[themeIndex].Path, _Themes[themeIndex].FileName));
            if (xmlReader == null)
                return false;

            // Load Cursor
            if (_Themes[themeIndex].PartyModeID == -1)
                _LoadCursor(xmlReader);

            // load fonts
            if (_Themes[themeIndex].PartyModeID == -1)
            {
                CFonts.LoadThemeFonts(
                    _Themes[themeIndex].Name,
                    Path.Combine(_Themes[themeIndex].Path, _Themes[themeIndex].SkinFolder, CSettings.FolderNameThemeFonts),
                    xmlReader);
            }
            else
            {
                CFonts.LoadPartyModeFonts(_Themes[themeIndex].PartyModeID,
                                          Path.Combine(_Themes[themeIndex].Path, CSettings.FolderNamePartyModeFonts),
                                          xmlReader);
            }
            return true;
        }

        public static void SaveTheme()
        {
            SaveSkin();

            for (int i = 0; i < _Themes.Count; i++)
            {
                if (_Themes[i].Name == CConfig.Theme && _Themes[i].PartyModeID == -1 || _Themes[i].PartyModeID != -1)
                    _SaveTheme(i);
            }
        }

        private static void _SaveTheme(int themeIndex)
        {
            #region ThemeMainFile
            using (XmlWriter writer = XmlWriter.Create(Path.Combine(_Themes[themeIndex].Path, _Themes[themeIndex].FileName), CConfig.XMLSettings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("root");

                // ThemeSystemVersion
                writer.WriteElementString("ThemeSystemVersion", _ThemeSystemVersion.ToString());

                #region Info
                writer.WriteStartElement("Info");

                writer.WriteElementString("Name", _Themes[themeIndex].Name);
                writer.WriteElementString("Author", _Themes[themeIndex].Author);
                writer.WriteElementString("SkinFolder", _Themes[themeIndex].SkinFolder);
                writer.WriteElementString("ThemeVersionMajor", _Themes[themeIndex].ThemeVersionMajor.ToString());
                writer.WriteElementString("ThemeVersionMinor", _Themes[themeIndex].ThemeVersionMinor.ToString());

                writer.WriteEndElement();
                #endregion Info

                // Cursor
                if (_Themes[themeIndex].PartyModeID == -1)
                    _SaveCursor(writer);

                // save fonts
                if (_Themes[themeIndex].PartyModeID == -1)
                    CFonts.SaveThemeFonts(_Themes[themeIndex].Name, writer);


                // End of File
                writer.WriteEndElement(); //end of root
                writer.WriteEndDocument();

                writer.Flush();
            }
            #endregion ThemeMainFile
        }

        public static void SaveSkin()
        {
            foreach (CSkin skin in _Skins)
                skin.SaveSkin();
        }

        private static void _ListThemes()
        {
            _Themes.Clear();

            string path = Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameThemes);
            List<string> files = CHelper.ListFiles(path, "*.xml", false, true);

            foreach (string file in files)
                AddTheme(file, -1);
        }

        public static bool AddTheme(string filePath, int partyModeID)
        {
            CXMLReader xmlReader = CXMLReader.OpenFile(filePath);
            if (xmlReader == null)
                return false;

            var theme = new STheme();

            int version = 0;
            xmlReader.TryGetIntValue("//root/ThemeSystemVersion", ref version);

            if (version == _ThemeSystemVersion)
            {
                xmlReader.GetValue("//root/Info/Name", out theme.Name, String.Empty);
                if (theme.Name != "")
                {
                    xmlReader.GetValue("//root/Info/Author", out theme.Author, String.Empty);
                    xmlReader.GetValue("//root/Info/SkinFolder", out theme.SkinFolder, String.Empty);
                    xmlReader.TryGetIntValue("//root/Info/ThemeVersionMajor", ref theme.ThemeVersionMajor);
                    xmlReader.TryGetIntValue("//root/Info/ThemeVersionMinor", ref theme.ThemeVersionMinor);
                    theme.Path = Path.GetDirectoryName(filePath);
                    theme.FileName = Path.GetFileName(filePath);
                    theme.PartyModeID = partyModeID;

                    _Themes.Add(theme);
                }
            }
            else
            {
                string msg = "Can't load Theme \"" + filePath + "\", ";
                if (version < _ThemeSystemVersion)
                    msg += "the file is outdated! ";
                else
                    msg += "the file is for newer program versions! ";

                msg += "Current ThemeSystemVersion is " + _ThemeSystemVersion + " but found " + version;
                CLog.LogError(msg);
            }
            return true;
        }

        public static void ListSkins()
        {
            int themeIndex = GetThemeIndex(-1);
            _Skins.Clear();

            if (themeIndex < 0 || themeIndex >= _Themes.Count)
            {
                CLog.LogError("Error List Skins. Can't find Theme: " + CConfig.Theme);
                return;
            }
            ListSkins(themeIndex);
        }

        public static void ListSkins(int themeIndex)
        {
            if (themeIndex < 0 || themeIndex >= _Themes.Count)
            {
                CLog.LogError("Error List Skins. Can't find Theme index: " + themeIndex);
                return;
            }

            STheme theme = _Themes[themeIndex];

            string path = Path.Combine(theme.Path, theme.SkinFolder);
            List<string> files = CHelper.ListFiles(path, "*.xml");

            foreach (string file in files)
            {
                CSkin skin = new CSkin(path, file, theme.PartyModeID);
                if (skin.Init())
                    _Skins.Add(skin);
            }
        }
        #endregion Theme and Skin loading and writing

        #region Theme and Skin index handling
        public static int GetSkinIndex(int partyModeID)
        {
            for (int i = 0; i < _Skins.Count; i++)
            {
                if (_Skins[i].PartyModeID == partyModeID && _Skins[i].Name == CConfig.Skin)
                    return i;
            }
            if (partyModeID >= 0)
            {
                for (int i = 0; i < _Skins.Count; i++)
                {
                    if (_Skins[i].PartyModeID == partyModeID)
                        return i;
                }
            }
            for (int i = 0; i < _Skins.Count; i++)
            {
                if (_Skins[i].PartyModeID < 0 && _Skins[i].Name == CConfig.Skin)
                    return i;
            }
            return -1;
        }

        private static CSkin _GetSkin(int partyModeID)
        {
            int index = GetSkinIndex(partyModeID);
            return index < 0 ? null : _Skins[index];
        }

        public static int GetThemeIndex(int partyModeID)
        {
            for (int i = 0; i < _Themes.Count; i++)
            {
                if (_Themes[i].PartyModeID == partyModeID && _Themes[i].Name == CConfig.Theme)
                    return i;
            }
            if (partyModeID >= 0)
            {
                for (int i = 0; i < _Themes.Count; i++)
                {
                    if (_Themes[i].PartyModeID == partyModeID)
                        return i;
                }
            }
            for (int i = 0; i < _Themes.Count; i++)
            {
                if (_Themes[i].PartyModeID < 0 && _Themes[i].Name == CConfig.Theme)
                    return i;
            }
            return -1;
        }

        public static string GetThemeScreensPath(int partyModeID)
        {
            string path = String.Empty;

            int themeIndex = GetThemeIndex(partyModeID);
            if (themeIndex != -1)
                path = Path.Combine(_Themes[themeIndex].Path, _Themes[themeIndex].SkinFolder, CSettings.FolderNameScreens);
            else
                CLog.LogError("Can't find current Theme");

            return path;
        }

        public static CTextureRef GetSkinTexture(string textureName, int partyModeID)
        {
            int skinIndex = GetSkinIndex(partyModeID);
            CSkinElement sk;
            if (skinIndex != -1 && textureName != null && _Skins[skinIndex].SkinList.TryGetValue(textureName, out sk))
                return sk.Texture;
            return null;
        }

        public static CVideoStream GetSkinVideo(string videoName, int partyModeID, bool loop = true)
        {
            CSkin skin = _GetSkin(partyModeID);
            return skin == null ? null : skin.GetVideo(videoName, loop);
        }
        #endregion Theme and Skin index handling

        #region Cursor save/load
        private static void _LoadCursor(CXMLReader xmlReader)
        {
            string value = String.Empty;
            xmlReader.GetValue("//root/Cursor/Skin", out Cursor.SkinName, value);

            xmlReader.TryGetFloatValue("//root/Cursor/W", ref Cursor.W);
            xmlReader.TryGetFloatValue("//root/Cursor/H", ref Cursor.H);

            if (xmlReader.GetValue("//root/Cursor/Color", out value, value))
                Cursor.Color.Name = value;
            else
            {
                Cursor.Color.Name = null;
                xmlReader.TryGetColorFromRGBA("//root/Cursor", out Cursor.Color.Color);
            }
        }

        private static void _SaveCursor(XmlWriter writer)
        {
            writer.WriteStartElement("Cursor");

            writer.WriteElementString("Skin", Cursor.SkinName);

            writer.WriteElementString("W", Cursor.W.ToString("#0.000"));
            writer.WriteElementString("H", Cursor.H.ToString("#0.000"));

            if (!String.IsNullOrEmpty(Cursor.Color.Name))
                writer.WriteElementString("Color", Cursor.Color.Name);
            else
            {
                writer.WriteElementString("R", Cursor.Color.Color.R.ToString("#0.000"));
                writer.WriteElementString("G", Cursor.Color.Color.G.ToString("#0.000"));
                writer.WriteElementString("B", Cursor.Color.Color.B.ToString("#0.000"));
                writer.WriteElementString("A", Cursor.Color.Color.A.ToString("#0.000"));
            }

            writer.WriteEndElement();
        }
        #endregion Cursor save/load

        #region Color Handling
        public static bool GetColor(string colorName, int partyModeID, out SColorF color)
        {
            int skinIndex = GetSkinIndex(partyModeID);

            if (_Skins[skinIndex].GetColor(colorName, out color))
                return true;

            if (partyModeID < 0)
                return false;
            return GetColor(colorName, -1, out color);
        }

        public static SColorF GetPlayerColor(int playerNr)
        {
            bool dummy;
            return GetPlayerColor(playerNr, out dummy);
        }

        public static SColorF GetPlayerColor(int playerNr, out bool success)
        {
            return GetPlayerColor(playerNr, GetSkinIndex(-1), out success);
        }

        public static SColorF GetPlayerColor(int playerNr, int skinIndex, out bool success)
        {
            SColorF color;
            success = _Skins[skinIndex].GetPlayerColor(playerNr, out color);
            return color;
        }
        #endregion Color Handling
    }
}