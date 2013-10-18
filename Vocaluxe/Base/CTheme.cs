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
using System.Windows.Forms;
using System.Xml;
using Vocaluxe.Base.Fonts;
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

    class CSkinElement
    {
        public string Value;
        public CTexture Texture;
    }

    class CVideoSkinElement : CSkinElement
    {
        public int VideoIndex = -1;
    }

    struct SSkin
    {
        public string Name;
        public string Author;
        public int SkinVersionMajor;
        public int SkinVersionMinor;

        public string Path;
        public string FileName;
        public int PartyModeID;

        public Dictionary<string, CSkinElement> SkinList;
        public Dictionary<string, CVideoSkinElement> VideoList;

        public SColors ThemeColors;
    }

    struct SColors
    {
        public SColorF[] Player;
        public SColorScheme[] ColorSchemes;
    }

    struct SColorScheme
    {
        public string Name;
        public SColorF Color;
    }

    struct SCursor
    {
        public string SkinName;

        public float W;
        public float H;

        public float R;
        public float G;
        public float B;
        public float A;

        public string Color;
    }
    #endregion Structs

    static class CTheme
    {
        // Version number for main theme and skin files. Increment it, if you've changed something on the theme files!
        private const int _ThemeSystemVersion = 5;
        private const int _SkinSystemVersion = 3;

        #region Vars
        private static readonly List<STheme> _Themes = new List<STheme>();
        public static string[] ThemeNames
        {
            get
            {
                var names = new List<string>();
                // ReSharper disable LoopCanBeConvertedToQuery
                foreach (var th in _Themes)
                    // ReSharper restore LoopCanBeConvertedToQuery
                {
                    if (th.PartyModeID == -1)
                        names.Add(th.Name);
                }
                return names.ToArray();
            }
        }

        private static readonly List<SSkin> _Skins = new List<SSkin>();
        public static string[] SkinNames
        {
            get
            {
                var names = new List<string>();
                // ReSharper disable LoopCanBeConvertedToQuery
                foreach (var sk in _Skins)
                    // ReSharper restore LoopCanBeConvertedToQuery
                {
                    if (sk.PartyModeID == -1)
                        names.Add(sk.Name);
                }
                return names.ToArray();
            }
        }

        public static SCursor Cursor;
        #endregion Vars

        #region Theme and Skin loading and writing
        public static void InitTheme()
        {
            _ListThemes();
            ListSkins();

            LoadSkins();
            LoadTheme();
        }

        public static void LoadSkins()
        {
            for (int index = 0; index < _Skins.Count; index++)
            {
                if (_Skins[index].PartyModeID == -1)
                    LoadSkin(index);
            }
        }

        public static bool LoadSkin(int skinIndex)
        {
            CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(_Skins[skinIndex].Path, _Skins[skinIndex].FileName));
            if (xmlReader == null)
                return false;

            // load skins/textures
            foreach (var valuePair in _Skins[skinIndex].SkinList)
            {
                CSkinElement sk = valuePair.Value;
                try
                {
                    xmlReader.GetValue("//root/Skins/" + valuePair.Key, out sk.Value, String.Empty);
                    sk.Texture = CDraw.AddTexture(Path.Combine(_Skins[skinIndex].Path, sk.Value));
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error on loading texture \"" + valuePair.Key + "\": " + e.Message + e.StackTrace);
                    CLog.LogError("Error on loading texture \"" + valuePair.Key + "\": " + e.Message + e.StackTrace);
                }
            }


            // load videos
            foreach (var valuePair in _Skins[skinIndex].VideoList)
            {
                try
                {
                    xmlReader.GetValue("//root/Videos/" + valuePair.Key, out valuePair.Value.Value, String.Empty);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error on loading video \"" + valuePair.Key + "\": " + e.Message + e.StackTrace);
                    CLog.LogError("Error on loading video \"" + valuePair.Key + "\": " + e.Message + e.StackTrace);
                }
            }

            // load colors
            _LoadColors(xmlReader, skinIndex);
            return true;
        }

        public static void UnloadSkins()
        {
            for (int i = 0; i < _Skins.Count; i++)
            {
                foreach (var sk in _Skins[i].SkinList.Values)
                    CDraw.RemoveTexture(ref sk.Texture);

                foreach (var vsk in _Skins[i].VideoList.Values)
                {
                    CVideo.Close(vsk.VideoIndex);
                    CDraw.RemoveTexture(ref vsk.Texture);
                }
            }

            for (int i = 0; i < _Skins.Count; i++)
            {
                if (_Skins[i].PartyModeID != -1)
                    continue;
                _Skins.RemoveAt(i);
                i--;
            }
        }

        public static void LoadTheme()
        {
            for (int i = 0; i < _Themes.Count; i++)
            {
                if (_Themes[i].Name == CConfig.Theme && _Themes[i].PartyModeID == -1)
                    LoadTheme(i);
            }
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

            int skinIndex = GetSkinIndex(_Themes[themeIndex].PartyModeID);

            // Load Cursor
            if (_Themes[themeIndex].PartyModeID == -1)
                _LoadCursor(xmlReader, skinIndex);

            // load fonts
            if (_Themes[themeIndex].PartyModeID == -1)
            {
                CFonts.LoadThemeFonts(
                    _Themes[themeIndex].Name,
                    Path.Combine(Path.Combine(_Themes[themeIndex].Path, _Themes[themeIndex].SkinFolder), CSettings.FolderThemeFonts),
                    xmlReader);
            }
            else
            {
                CFonts.LoadPartyModeFonts(_Themes[themeIndex].PartyModeID,
                                          Path.Combine(_Themes[themeIndex].Path, CSettings.FolderPartyModeFonts),
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
            for (int skinIndex = 0; skinIndex < _Skins.Count; skinIndex++)
            {
                using (XmlWriter writer = XmlWriter.Create(Path.Combine(_Skins[skinIndex].Path, _Skins[skinIndex].FileName), CConfig.XMLSettings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("root");

                    // ThemeSystemVersion
                    writer.WriteElementString("SkinSystemVersion", _SkinSystemVersion.ToString());

                    #region Info
                    writer.WriteStartElement("Info");

                    writer.WriteElementString("Name", _Skins[skinIndex].Name);
                    writer.WriteElementString("Author", _Skins[skinIndex].Author);
                    writer.WriteElementString("SkinVersionMajor", _Skins[skinIndex].SkinVersionMajor.ToString());
                    writer.WriteElementString("SkinVersionMinor", _Skins[skinIndex].SkinVersionMinor.ToString());

                    writer.WriteEndElement();
                    #endregion Info

                    // save colors
                    _SaveColors(writer, skinIndex);

                    #region Skins
                    writer.WriteStartElement("Skins");

                    foreach (var element in _Skins[skinIndex].SkinList)
                        writer.WriteElementString(element.Key, element.Value.Value);
                    writer.WriteEndElement();
                    #endregion Skins

                    #region Videos
                    writer.WriteStartElement("Videos");

                    foreach (var element in _Skins[skinIndex].VideoList)
                        writer.WriteElementString(element.Key, element.Value.Value);
                    writer.WriteEndElement();
                    #endregion Videos

                    // End of File
                    writer.WriteEndElement(); //end of root
                    writer.WriteEndDocument();

                    writer.Flush();
                }
            }
        }

        private static void _ListThemes()
        {
            _Themes.Clear();

            string path = Path.Combine(Directory.GetCurrentDirectory(), CSettings.FolderThemes);
            List<string> files = CHelper.ListFiles(path, "*.xml");

            foreach (string file in files)
                AddTheme(Path.Combine(path, file), -1);
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
                CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(path, file));
                if (xmlReader == null)
                    continue;

                var skin = new SSkin();

                int version = 0;
                xmlReader.TryGetIntValue("//root/SkinSystemVersion", ref version);

                if (version == _SkinSystemVersion)
                {
                    xmlReader.GetValue("//root/Info/Name", out skin.Name, String.Empty);
                    if (skin.Name != "")
                    {
                        xmlReader.GetValue("//root/Info/Author", out skin.Author, String.Empty);
                        xmlReader.TryGetIntValue("//root/Info/SkinVersionMajor", ref skin.SkinVersionMajor);
                        xmlReader.TryGetIntValue("//root/Info/SkinVersionMinor", ref skin.SkinVersionMinor);


                        skin.Path = path;
                        skin.FileName = file;
                        skin.PartyModeID = theme.PartyModeID;

                        skin.SkinList = new Dictionary<string, CSkinElement>();
                        List<string> names = xmlReader.GetValues("Skins");
                        foreach (string str in names)
                            skin.SkinList[str] = new CSkinElement();

                        skin.VideoList = new Dictionary<string, CVideoSkinElement>();
                        names = xmlReader.GetValues("Videos");
                        foreach (string str in names)
                            skin.VideoList[str] = new CVideoSkinElement();
                        _Skins.Add(skin);
                    }
                }
                else
                {
                    string msg = "Can't load Skin \"" + file + "\", ";
                    if (version < _SkinSystemVersion)
                        msg += "the file ist outdated! ";
                    else
                        msg += "the file is for newer program versions! ";

                    msg += "Current SkinSystemVersion is " + _SkinSystemVersion;
                    CLog.LogError(msg);
                }
            }
        }
        #endregion Theme and Skin loading and writing

        #region Theme and Skin index handling
        public static int GetSkinIndex(int partyModeID)
        {
            for (int i = 0; i < _Skins.Count; i++)
            {
                if (partyModeID != -1 && _Skins[i].PartyModeID == partyModeID)
                    return i;
                if (partyModeID == -1 && _Skins[i].Name == CConfig.Skin)
                    return i;
            }

            return -1;
        }

        public static int GetThemeIndex(int partyModeID)
        {
            for (int i = 0; i < _Themes.Count; i++)
            {
                if (partyModeID != -1 && _Themes[i].PartyModeID == partyModeID)
                    return i;
                if (partyModeID == -1 && _Themes[i].Name == CConfig.Theme)
                    return i;
            }

            return -1;
        }

        public static string GetThemeScreensPath(int partyModeID)
        {
            string path = String.Empty;

            int themeIndex = GetThemeIndex(partyModeID);
            if (themeIndex != -1)
            {
                path = Path.Combine(_Themes[themeIndex].Path, _Themes[themeIndex].SkinFolder);
                path = Path.Combine(path, CSettings.FolderScreens);
            }
            else
                CLog.LogError("Can't find current Theme");

            return path;
        }

        public static CTexture GetSkinTexture(string textureName, int partyModeID)
        {
            int skinIndex = GetSkinIndex(partyModeID);
            CSkinElement sk;
            if (skinIndex != -1 && textureName != null && _Skins[skinIndex].SkinList.TryGetValue(textureName, out sk))
                return sk.Texture;
            return null;
        }

        public static string GetSkinFilePath(string skinName, int partyModeID)
        {
            return _GetSkinFileName(skinName, GetSkinIndex(partyModeID), true);
        }

        private static string _GetSkinFileName(string skinName, int skinIndex, bool returnPath = false)
        {
            CSkinElement sk;
            if (_Skins[skinIndex].SkinList.TryGetValue(skinName, out sk))
                return !returnPath ? sk.Value : Path.Combine(_Skins[skinIndex].Path, sk.Value);

            CLog.LogError("Can't find Skin Element \"" + skinName);
            return skinName;
        }

        public static CVideoSkinElement GetSkinVideo(string videoName, int partyModeID, bool load = true)
        {
            int skinIndex = GetSkinIndex(partyModeID);
            if (skinIndex != -1)
            {
                CVideoSkinElement sk;
                if (_Skins[skinIndex].VideoList.TryGetValue(videoName, out sk))
                {
                    if (sk.VideoIndex == -1 && load)
                    {
                        sk.VideoIndex = CVideo.Load(GetVideoFilePath(videoName, partyModeID));
                        CVideo.SetLoop(sk.VideoIndex, true);
                    }
                    return sk;
                }
            }
            return null;
        }

        public static CTexture GetSkinVideoTexture(string videoName, int partyModeID)
        {
            CVideoSkinElement sk = GetSkinVideo(videoName, partyModeID);
            if (sk == null)
                return null;
            float time = 0f;
            CVideo.GetFrame(sk.VideoIndex, ref sk.Texture, time, out time);
            return sk.Texture;
        }

        public static void SkinVideoPause(string videoName, int partyModeID)
        {
            CVideoSkinElement sk = GetSkinVideo(videoName, partyModeID, false);
            if (sk == null)
                return;
            CVideo.Pause(sk.VideoIndex);
        }

        public static void SkinVideoResume(string videoName, int partyModeID)
        {
            CVideoSkinElement sk = GetSkinVideo(videoName, partyModeID);
            if (sk == null)
                return;
            CVideo.Resume(sk.VideoIndex);
        }

        public static string GetVideoFilePath(string videoName, int partyModeID)
        {
            return _GetVideoFileName(videoName, GetSkinIndex(partyModeID), true);
        }

        private static string _GetVideoFileName(string videoName, int skinIndex, bool returnPath = false)
        {
            CVideoSkinElement sk;
            if (_Skins[skinIndex].VideoList.TryGetValue(videoName, out sk))
                return !returnPath ? sk.Value : Path.Combine(_Skins[skinIndex].Path, sk.Value);

            CLog.LogError("Can't find Video Element \"" + videoName);
            return videoName;
        }
        #endregion Theme and Skin index handling

        #region Element loading
        private static void _LoadColors(CXMLReader xmlReader, int skinIndex)
        {
            SSkin skin = _Skins[skinIndex];

            if (_Skins[skinIndex].PartyModeID == -1)
            {
                var playerColors = new List<SColorF>();
                float value = 0f;

                int i = 1;
                while (xmlReader.TryGetFloatValue("//root/Colors/Player" + i + "/R", ref value))
                {
                    var color = new SColorF {R = value};

                    xmlReader.TryGetFloatValue("//root/Colors/Player" + i + "/G", ref color.G);
                    xmlReader.TryGetFloatValue("//root/Colors/Player" + i + "/B", ref color.B);
                    xmlReader.TryGetFloatValue("//root/Colors/Player" + i + "/A", ref color.A);

                    playerColors.Add(color);
                    i++;
                }
                skin.ThemeColors.Player = playerColors.ToArray();
            }

            var colorScheme = new List<SColorScheme>();
            List<string> names = xmlReader.GetValues("ColorSchemes");
            foreach (string str in names)
            {
                var scheme = new SColorScheme {Name = str};

                xmlReader.TryGetFloatValue("//root/ColorSchemes/" + str + "/R", ref scheme.Color.R);
                xmlReader.TryGetFloatValue("//root/ColorSchemes/" + str + "/G", ref scheme.Color.G);
                xmlReader.TryGetFloatValue("//root/ColorSchemes/" + str + "/B", ref scheme.Color.B);
                xmlReader.TryGetFloatValue("//root/ColorSchemes/" + str + "/A", ref scheme.Color.A);

                colorScheme.Add(scheme);
            }
            skin.ThemeColors.ColorSchemes = colorScheme.ToArray();

            _Skins[skinIndex] = skin;
        }

        private static void _LoadCursor(CXMLReader xmlReader, int skinIndex)
        {
            string value = String.Empty;
            xmlReader.GetValue("//root/Cursor/Skin", out Cursor.SkinName, value);

            xmlReader.TryGetFloatValue("//root/Cursor/W", ref Cursor.W);
            xmlReader.TryGetFloatValue("//root/Cursor/H", ref Cursor.H);

            if (xmlReader.GetValue("//root/Cursor/Color", out value, value))
            {
                SColorF color = GetColor(value, _Skins[skinIndex].PartyModeID);
                Cursor.R = color.R;
                Cursor.G = color.G;
                Cursor.B = color.B;
                Cursor.A = color.A;
                Cursor.Color = value;
            }
            else
            {
                Cursor.Color = String.Empty;
                xmlReader.TryGetFloatValue("//root/Cursor/R", ref Cursor.R);
                xmlReader.TryGetFloatValue("//root/Cursor/G", ref Cursor.G);
                xmlReader.TryGetFloatValue("//root/Cursor/B", ref Cursor.B);
                xmlReader.TryGetFloatValue("//root/Cursor/A", ref Cursor.A);
            }
        }
        #endregion Element loading

        #region Element Writing
        private static void _SaveColors(XmlWriter writer, int skinIndex)
        {
            if (_Skins[skinIndex].PartyModeID == -1)
            {
                writer.WriteStartElement("Colors");
                for (int i = 0; i < _Skins[skinIndex].ThemeColors.Player.Length; i++)
                {
                    writer.WriteStartElement("Player" + (i + 1));

                    writer.WriteElementString("R", _Skins[skinIndex].ThemeColors.Player[i].R.ToString("#0.000"));
                    writer.WriteElementString("G", _Skins[skinIndex].ThemeColors.Player[i].G.ToString("#0.000"));
                    writer.WriteElementString("B", _Skins[skinIndex].ThemeColors.Player[i].B.ToString("#0.000"));
                    writer.WriteElementString("A", _Skins[skinIndex].ThemeColors.Player[i].A.ToString("#0.000"));

                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }

            writer.WriteStartElement("ColorSchemes");
            foreach (var scheme in _Skins[skinIndex].ThemeColors.ColorSchemes)
            {
                writer.WriteStartElement(scheme.Name);

                writer.WriteElementString("R", scheme.Color.R.ToString("#0.000"));
                writer.WriteElementString("G", scheme.Color.G.ToString("#0.000"));
                writer.WriteElementString("B", scheme.Color.B.ToString("#0.000"));
                writer.WriteElementString("A", scheme.Color.A.ToString("#0.000"));

                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        private static void _SaveCursor(XmlWriter writer)
        {
            writer.WriteStartElement("Cursor");

            writer.WriteElementString("Skin", Cursor.SkinName);

            writer.WriteElementString("W", Cursor.W.ToString("#0.000"));
            writer.WriteElementString("H", Cursor.H.ToString("#0.000"));

            if (!String.IsNullOrEmpty(Cursor.Color))
                writer.WriteElementString("Color", Cursor.Color);
            else
            {
                writer.WriteElementString("R", Cursor.R.ToString("#0.000"));
                writer.WriteElementString("G", Cursor.G.ToString("#0.000"));
                writer.WriteElementString("B", Cursor.B.ToString("#0.000"));
                writer.WriteElementString("A", Cursor.A.ToString("#0.000"));
            }

            writer.WriteEndElement();
        }
        #endregion ElementWriting

        #region Color Handling
        public static SColorF GetColor(string colorName, int partyModeID)
        {
            SColorF color;

            int skinIndex = GetSkinIndex(partyModeID);

            GetColor(colorName, skinIndex, out color);
            return color;
        }

        public static bool GetColor(string colorName, int skinIndex, out SColorF color)
        {
            foreach (var scheme in _Skins[skinIndex].ThemeColors.ColorSchemes)
            {
                if (scheme.Name == colorName)
                {
                    color = new SColorF(scheme.Color);
                    return true;
                }
            }

            bool success;
            color = GetPlayerColor(colorName, GetSkinIndex(-1), out success);
            return success;
        }

        public static SColorF GetPlayerColor(int playerNr)
        {
            var color = new SColorF(1f, 1f, 1f, 1f);

            int skinIndex = GetSkinIndex(-1);

            if (_Skins[skinIndex].ThemeColors.Player.Length < playerNr)
                return color;

            return _Skins[skinIndex].ThemeColors.Player[playerNr - 1];
        }

        public static SColorF GetPlayerColor(string playerNrString)
        {
            bool dummy;
            return GetPlayerColor(playerNrString, GetSkinIndex(-1), out dummy);
        }

        public static SColorF GetPlayerColor(string playerNrString, int skinIndex, out bool success)
        {
            int selection = 0;
            if (playerNrString != null && playerNrString.StartsWith("Player"))
                int.TryParse(playerNrString.Substring(6), out selection);

            return GetPlayerColor(selection, skinIndex, out success);
        }

        public static SColorF GetPlayerColor(int playerNr, out bool success)
        {
            return GetPlayerColor(playerNr, GetSkinIndex(-1), out success);
        }

        public static SColorF GetPlayerColor(int playerNr, int skinIndex, out bool success)
        {
            success = false;

            if (_Skins[skinIndex].PartyModeID != -1)
                skinIndex = GetSkinIndex(-1);

            if (_Skins[skinIndex].ThemeColors.Player.Length < playerNr || playerNr < 1)
                return new SColorF(1f, 1f, 1f, 1f);

            success = true;
            return _Skins[skinIndex].ThemeColors.Player[playerNr - 1];
        }
        #endregion Color Handling
    }
}