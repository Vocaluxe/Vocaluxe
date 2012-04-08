﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;

using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;
using Vocaluxe.Menu.SingNotes;
using Vocaluxe.Menu.SongMenu;
using Vocaluxe.Screens;

namespace Vocaluxe.Base
{
    #region Structs
    struct Theme
    {
        public string Name;
        public string Author;
        public string SkinFolder;
        public int ThemeVersionMajor;
        public int ThemeVersionMinor;

        public string Path;
        public string FileName;
    }

    struct SkinElement
    {
        public string Name;
        public string Value;

        public STexture Texture;
        public int VideoIndex;
    }

    struct Skin
    {
        public string Name;
        public string Author;
        public int SkinVersionMajor;
        public int SkinVersionMinor;

        public string Path;
        public string FileName;

        public Dictionary<string, SkinElement> SkinList;
        public List<SkinElement> VideoList;

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

        public float w;
        public float h;

        public float r;
        public float g;
        public float b;
        public float a;

        public string color;
    }
    #endregion Structs

    static class CTheme
    {
        // Version number for main theme and skin files. Increment it, if you've changed something on the theme files!
        const int ThemeSystemVersion = 5;
        const int SkinSystemVersion = 3;

        #region Vars
        private static XmlWriterSettings _settings = new XmlWriterSettings();
        private static List<Theme> _Themes = new List<Theme>();
        public static string[] ThemeNames
        {
            get 
            {
                List<string> names = new List<string>();
                foreach (Theme th in _Themes)
                {
                    names.Add(th.Name);
                }
                return names.ToArray();
            }
        }

        private static List<Skin> _Skins = new List<Skin>();
        public static string[] SkinNames
        {
            get
            {
                List<string> names = new List<string>();
                foreach (Skin sk in _Skins)
                {
                    names.Add(sk.Name);
                }
                return names.ToArray();
            }
        }

        public static SCursor Cursor = new SCursor();
        #endregion Vars

        #region Theme and Skin loading and writing
        public static void InitTheme()
        {
            _settings.Indent = true; 
            _settings.Encoding = Encoding.UTF8;
            _settings.ConformanceLevel = ConformanceLevel.Document;

            //ThemeColors.Player = new SColorF[6];

            ListThemes();
            ListSkins();

            LoadSkins();
            LoadTheme();
        }

        public static void LoadSkins()
        {
            for (int index = 0; index < _Skins.Count; index++)
            {
                bool loaded = false;
                XPathDocument xPathDoc = null;
                XPathNavigator navigator = null;

                try
                {
                    xPathDoc = new XPathDocument(Path.Combine(_Skins[index].Path, _Skins[index].FileName));
                    navigator = xPathDoc.CreateNavigator();
                    loaded = true;
                }
                catch (Exception e)
                {
                    CLog.LogError("Error loading skin " + _Skins[index].FileName + ": " + e.Message);
                    loaded = false;
                    if (navigator != null)
                        navigator = null;

                    if (xPathDoc != null)
                        xPathDoc = null;
                }

                if (loaded)
                {
                    string value = String.Empty;


                    // load skins/textures
                    List<string> keys = new List<string>(_Skins[index].SkinList.Keys);

                    foreach (string name in keys)
                    {
                        try
                        {
                            CHelper.GetValueFromXML("//root/Skins/" + name, navigator, ref value, String.Empty);
                            SkinElement sk = _Skins[index].SkinList[name];
                            sk.Value = value;
                            sk.VideoIndex = -1;
                            sk.Texture = CDraw.AddTexture(Path.Combine(_Skins[index].Path, sk.Value));
                            _Skins[index].SkinList[name] = sk;
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Error on loading texture \"" + name + "\": " + e.Message + e.StackTrace);
                            CLog.LogError("Error on loading texture \"" + name + "\": " + e.Message + e.StackTrace);
                        }
                    }
                    

                    // load videos
                    for (int i = 0; i < _Skins[index].VideoList.Count; i++)
                    {
                        try
                        {
                            CHelper.GetValueFromXML("//root/Videos/" + _Skins[index].VideoList[i].Name, navigator, ref value, String.Empty);
                            SkinElement sk = new SkinElement();
                            sk.Name = _Skins[index].VideoList[i].Name;
                            sk.Value = value;
                            sk.VideoIndex = CVideo.VdLoad(Path.Combine(_Skins[index].Path, sk.Value));
                            CVideo.VdSetLoop(sk.VideoIndex, true);
                            CVideo.VdPause(sk.VideoIndex);
                            sk.Texture = new STexture(-1);
                            _Skins[index].VideoList[i] = sk;
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Error on loading video \"" + _Skins[index].VideoList[i].Name + "\": " + e.Message + e.StackTrace);
                            CLog.LogError("Error on loading video \"" + _Skins[index].VideoList[i].Name + "\": " + e.Message + e.StackTrace);
                        }
                    }

                    // load colors
                    LoadColors(navigator, index);
                }
            }
        }

        public static void UnloadSkins()
        {
            for (int i = 0; i < _Skins.Count; i++)
            {
                foreach (SkinElement sk in _Skins[i].SkinList.Values)
                {
                    STexture Texture = sk.Texture;
                    CDraw.RemoveTexture(ref Texture);
                }

                for (int j = 0; j < _Skins[i].VideoList.Count; j++)
                {
                    CVideo.VdClose(_Skins[i].VideoList[j].VideoIndex);
                    STexture VideoTexture = _Skins[i].VideoList[j].Texture;
                    CDraw.RemoveTexture(ref VideoTexture);
                }
            }
            _Skins.Clear();
        }

        public static void LoadTheme()
        {
            bool loaded = false;
            XPathDocument xPathDoc = null;
            XPathNavigator navigator = null;
            int index = 0;

            for (int i = 0; i < _Themes.Count; i++)
            {
                if (_Themes[i].Name == CConfig.Theme)
                    index = i;
            }

            try
            {
                xPathDoc = new XPathDocument(Path.Combine(_Themes[index].Path, _Themes[index].FileName));
                navigator = xPathDoc.CreateNavigator();
                loaded = true;
            }
            catch (Exception e)
            {
                CLog.LogError("Error loading theme " + _Themes[index].FileName + ": " + e.Message);
                loaded = false;
                if (navigator != null)
                    navigator = null;

                if (xPathDoc != null)
                    xPathDoc = null;
            }
            
            if (loaded)
            {
                int skinIndex = GetSkinIndex();

                // Load Cursor
                LoadCursor(navigator, skinIndex);

                // load fonts
                CFonts.LoadThemeFonts(
                    _Themes[index].Name,
                    Path.Combine(Path.Combine(_Themes[index].Path, _Themes[index].SkinFolder), CSettings.sFolderThemeFonts),
                    navigator);
            }   
        }

        public static void SaveTheme()
        {
            SaveSkin();

            int ThemeIndex = 0;
            for (int i = 0; i < _Themes.Count; i++)
            {
                if (_Themes[i].Name == CConfig.Theme)
                    ThemeIndex = i;
            }

            #region ThemeMainFile
            XmlWriter writer = XmlWriter.Create(Path.Combine(_Themes[ThemeIndex].Path,  _Themes[ThemeIndex].FileName), _settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("root");

            // ThemeSystemVersion
            writer.WriteElementString("ThemeSystemVersion", ThemeSystemVersion.ToString()); 

            #region Info
            writer.WriteStartElement("Info");

            writer.WriteElementString("Name", _Themes[ThemeIndex].Name);
            writer.WriteElementString("Author", _Themes[ThemeIndex].Author);
            writer.WriteElementString("SkinFolder", _Themes[ThemeIndex].SkinFolder);
            writer.WriteElementString("ThemeVersionMajor", _Themes[ThemeIndex].ThemeVersionMajor.ToString());
            writer.WriteElementString("ThemeVersionMinor", _Themes[ThemeIndex].ThemeVersionMinor.ToString());
            
            writer.WriteEndElement();
            #endregion Info

            // Cursor
            SaveCursor(writer);

            // save fonts
            CFonts.SaveThemeFonts(_Themes[ThemeIndex].Name, writer);


            // End of File
            writer.WriteEndElement(); //end of root
            writer.WriteEndDocument();

            writer.Flush();
            writer.Close();
            #endregion ThemeMainFile
        }

        public static void SaveSkin()
        {
            for (int SkinIndex = 0; SkinIndex < _Skins.Count; SkinIndex++)
            {
                XmlWriter writer = XmlWriter.Create(Path.Combine(_Skins[SkinIndex].Path, _Skins[SkinIndex].FileName), _settings);
                writer.WriteStartDocument();
                writer.WriteStartElement("root");

                // ThemeSystemVersion
                writer.WriteElementString("SkinSystemVersion", SkinSystemVersion.ToString());

                #region Info
                writer.WriteStartElement("Info");

                writer.WriteElementString("Name", _Skins[SkinIndex].Name);
                writer.WriteElementString("Author", _Skins[SkinIndex].Author);
                writer.WriteElementString("SkinVersionMajor", _Skins[SkinIndex].SkinVersionMajor.ToString());
                writer.WriteElementString("SkinVersionMinor", _Skins[SkinIndex].SkinVersionMinor.ToString());

                writer.WriteEndElement();
                #endregion Info

                // save colors
                SaveColors(writer, SkinIndex);

                #region Skins
                writer.WriteStartElement("Skins");

                foreach (SkinElement element in _Skins[SkinIndex].SkinList.Values)
                {
                    writer.WriteElementString(element.Name, element.Value);
                }
                writer.WriteEndElement();
                #endregion Skins

                #region Videos
                writer.WriteStartElement("Videos");

                foreach (SkinElement element in _Skins[SkinIndex].VideoList)
                {
                    writer.WriteElementString(element.Name, element.Value);
                }
                writer.WriteEndElement();
                #endregion Videos

                // End of File
                writer.WriteEndElement(); //end of root
                writer.WriteEndDocument();

                writer.Flush();
                writer.Close();
            }
        }

        private static void ListThemes()
        {
            CHelper Helper = new CHelper();
            _Themes.Clear();

            string path = Path.Combine(Directory.GetCurrentDirectory(), CSettings.sFolderThemes);
            List<string> files = Helper.ListFiles(path, "*.xml", false);

            foreach (string file in files)
            {
                bool loaded = false;
                XPathDocument xPathDoc = null;
                XPathNavigator navigator = null;

                try
                {
                    xPathDoc = new XPathDocument(Path.Combine(path, file));
                    navigator = xPathDoc.CreateNavigator();
                    loaded = true;
                }
                catch (Exception e)
                {
                    CLog.LogError("Error loading theme " + file + ": " + e.Message);
                    loaded = false;
                    if (navigator != null)
                        navigator = null;

                    if (xPathDoc != null)
                        xPathDoc = null;
                }

                if (loaded)
                {
                    Theme theme = new Theme();

                    int version = 0;
                    CHelper.TryGetIntValueFromXML("//root/ThemeSystemVersion", navigator, ref version);

                    if (version == ThemeSystemVersion)
                    {
                        CHelper.GetValueFromXML("//root/Info/Name", navigator, ref theme.Name, String.Empty);
                        if (theme.Name != String.Empty)
                        {
                            CHelper.GetValueFromXML("//root/Info/Author", navigator, ref theme.Author, String.Empty);
                            CHelper.GetValueFromXML("//root/Info/SkinFolder", navigator, ref theme.SkinFolder, String.Empty);
                            CHelper.TryGetIntValueFromXML("//root/Info/ThemeVersionMajor", navigator, ref theme.ThemeVersionMajor);
                            CHelper.TryGetIntValueFromXML("//root/Info/ThemeVersionMinor", navigator, ref theme.ThemeVersionMinor);
                            theme.Path = path;
                            theme.FileName = file;

                            _Themes.Add(theme);
                        }
                    }
                    else
                    {
                        string msg = "Can't load Theme \"" + file + "\", ";
                        if (version < ThemeSystemVersion)
                            msg += "the file ist outdated! ";
                        else
                            msg += "the file is for newer program versions! ";

                        msg += "Current ThemeSystemVersion is " + ThemeSystemVersion.ToString();
                        CLog.LogError(msg);
                    }
                }
            }          
        }

        public static void ListSkins()
        {
            CHelper Helper = new CHelper();
            int themeIndex = GetThemeIndex();
            _Skins.Clear();

            if (themeIndex < 0)
            {
                CLog.LogError("Error List Skins. Can't find Theme: " + CConfig.Theme);
                return;
            }

            Theme theme = _Themes[themeIndex];
           
            string path = Path.Combine(theme.Path, theme.SkinFolder);
            List<string> files = Helper.ListFiles(path, "*.xml", false);

            foreach (string file in files)
            {
                bool loaded = false;
                XPathDocument xPathDoc = null;
                XPathNavigator navigator = null;

                try
                {
                    xPathDoc = new XPathDocument(Path.Combine(path, file));
                    navigator = xPathDoc.CreateNavigator();
                    loaded = true;
                }
                catch (Exception e)
                {
                    CLog.LogError("Error loading skin " + file + ": " + e.Message);
                    loaded = false;
                    if (navigator != null)
                        navigator = null;

                    if (xPathDoc != null)
                        xPathDoc = null;
                }

                if (loaded)
                {
                    Skin skin = new Skin();

                    int version = 0;
                    CHelper.TryGetIntValueFromXML("//root/SkinSystemVersion", navigator, ref version);

                    if (version == SkinSystemVersion)
                    {
                        CHelper.GetValueFromXML("//root/Info/Name", navigator, ref skin.Name, String.Empty);
                        if (skin.Name != String.Empty)
                        {
                            CHelper.GetValueFromXML("//root/Info/Author", navigator, ref skin.Author, String.Empty);
                            CHelper.TryGetIntValueFromXML("//root/Info/SkinVersionMajor", navigator, ref skin.SkinVersionMajor);
                            CHelper.TryGetIntValueFromXML("//root/Info/SkinVersionMinor", navigator, ref skin.SkinVersionMinor);


                            skin.Path = path;
                            skin.FileName = file;

                            skin.SkinList = new Dictionary<string, SkinElement>();
                            List<string> names = CHelper.GetValuesFromXML("Skins", navigator);
                            foreach (string str in names)
                            {
                                SkinElement sk = new SkinElement();
                                sk.Name = str;
                                sk.Value = String.Empty;
                                skin.SkinList[str] = sk;
                            }

                            skin.VideoList = new List<SkinElement>();
                            names = CHelper.GetValuesFromXML("Videos", navigator);
                            foreach (string str in names)
                            {
                                SkinElement sk = new SkinElement();
                                sk.Name = str;
                                sk.Value = String.Empty;
                                skin.VideoList.Add(sk);
                            }
                            _Skins.Add(skin);
                        }
                    }
                    else
                    {
                        string msg = "Can't load Skin \"" + file + "\", ";
                        if (version < SkinSystemVersion)
                            msg += "the file ist outdated! ";
                        else
                            msg += "the file is for newer program versions! ";

                        msg += "Current SkinSystemVersion is " + SkinSystemVersion.ToString();
                        CLog.LogError(msg);
                    }
                }    
            }
        }
        #endregion Theme and Skin loading and writing

        #region Theme and Skin index handling
        public static int GetSkinIndex()
        {
            for (int i = 0; i < _Skins.Count; i++)
            {
                if (_Skins[i].Name == CConfig.Skin)
                {
                    return i;
                }
            }
            
            return -1;
        }

        public static int GetThemeIndex()
        {
            for (int i = 0; i < _Themes.Count; i++)
            {
                if (_Themes[i].Name == CConfig.Theme)
                {
                    return i;
                }
            }

            return -1;
        }

        public static string GetThemeScreensPath()
        {
            string path = String.Empty;

            int themeIndex = GetThemeIndex();
            if (themeIndex != -1)
            {
                path = Path.Combine(_Themes[themeIndex].Path, _Themes[themeIndex].SkinFolder);
                path = Path.Combine(path, CSettings.sFolderScreens);
            }
            else
                CLog.LogError("Can't find current Theme");

            return path;
        }

        public static STexture GetSkinTexture(string TextureName)
        {
            int SkinIndex = GetSkinIndex();
            if (SkinIndex != -1 && TextureName != null && _Skins[SkinIndex].SkinList.ContainsKey(TextureName))
            {
                return _Skins[SkinIndex].SkinList[TextureName].Texture;
            }
            return new STexture(-1);
        }

        public static string GetSkinFilePath(string SkinName)
        {
            return GetSkinFileName(SkinName, GetSkinIndex(), true);
        }

        private static string GetSkinFileName(string SkinName, int SkinIndex)
        {
            return GetSkinFileName(SkinName, SkinIndex, false);
        }

        private static string GetSkinFileName(string SkinName, int SkinIndex, bool ReturnPath)
        {
            foreach (SkinElement sk in _Skins[SkinIndex].SkinList.Values)
            {
                if (sk.Name == SkinName)
                {
                    if (!ReturnPath)
                        return sk.Value;
                    else
                        return Path.Combine(_Skins[SkinIndex].Path, sk.Value);
                }
            }

            CLog.LogError("Can't find Skin Element \"" + SkinName);
            return SkinName;
        }

        public static STexture GetSkinVideoTexture(string VideoName)
        {
            int SkinIndex = GetSkinIndex();
            if (SkinIndex != -1)
            {
                for (int i = 0; i < _Skins[SkinIndex].VideoList.Count; i++)
                {
                    SkinElement sk = _Skins[SkinIndex].VideoList[i];
                    if (sk.Name == VideoName)
                    {
                        float Time = 0f;
                        if (sk.VideoIndex == -1)
                        {
                            sk.VideoIndex = CVideo.VdLoad(GetVideoFilePath(sk.Name));
                            CVideo.VdSetLoop(sk.VideoIndex, true);
                        }
                        CVideo.VdGetFrame(sk.VideoIndex, ref sk.Texture, Time, ref Time);
                        _Skins[SkinIndex].VideoList[i] = sk;
                        return sk.Texture;
                    }
                }
            }
            return new STexture(-1);
        }

        public static void SkinVideoPause(string VideoName)
        {
            int SkinIndex = GetSkinIndex();
            if (SkinIndex != -1)
            {
                for (int i = 0; i < _Skins[SkinIndex].VideoList.Count; i++)
                {
                    SkinElement sk = _Skins[SkinIndex].VideoList[i];
                    if (sk.Name == VideoName)
                    {
                        if (sk.VideoIndex == -1)
                        {
                            sk.VideoIndex = CVideo.VdLoad(GetVideoFilePath(sk.Name));
                            CVideo.VdSetLoop(sk.VideoIndex, true);
                        }
                        CVideo.VdPause(sk.VideoIndex);
                        _Skins[SkinIndex].VideoList[i] = sk;
                        return;
                    }
                }
            }
        }

        public static void SkinVideoResume(string VideoName)
        {
            int SkinIndex = GetSkinIndex();
            if (SkinIndex != -1)
            {
                for (int i = 0; i < _Skins[SkinIndex].VideoList.Count; i++)
                {
                    SkinElement sk = _Skins[SkinIndex].VideoList[i];
                    if (sk.Name == VideoName)
                    {
                        if (sk.VideoIndex == -1)
                        {
                            sk.VideoIndex = CVideo.VdLoad(GetVideoFilePath(sk.Name));
                            CVideo.VdSetLoop(sk.VideoIndex, true);
                        }
                        CVideo.VdResume(sk.VideoIndex);
                        _Skins[SkinIndex].VideoList[i] = sk;
                        return;
                    }
                }
            }
        }

        public static string GetVideoFilePath(string VideoName)
        {
            return GetVideoFileName(VideoName, GetSkinIndex(), true);
        }

        private static string GetVideoFileName(string VideoName, int SkinIndex)
        {
            return GetVideoFileName(VideoName, SkinIndex, false);
        }

        private static string GetVideoFileName(string VideoName, int SkinIndex, bool ReturnPath)
        {
            foreach (SkinElement sk in _Skins[SkinIndex].VideoList)
            {
                if (sk.Name == VideoName)
                {
                    if (!ReturnPath)
                        return sk.Value;
                    else
                        return Path.Combine(_Skins[SkinIndex].Path, sk.Value);
                }
            }

            CLog.LogError("Can't find Video Element \"" + VideoName);
            return VideoName;
        }
        #endregion Theme and Skin index handling

        #region Element loading
        private static void LoadColors(XPathNavigator navigator, int SkinIndex)
        {
            Skin skin = _Skins[SkinIndex];
            
            List<SColorF> PlayerColors = new List<SColorF>();
            float value = 0f;

            int i = 1;
            while (CHelper.TryGetFloatValueFromXML("//root/Colors/Player" + i.ToString() + "/R", navigator, ref value))
            {
                SColorF color = new SColorF();

                color.R = value;
                CHelper.TryGetFloatValueFromXML("//root/Colors/Player" + i.ToString() + "/G", navigator, ref color.G);
                CHelper.TryGetFloatValueFromXML("//root/Colors/Player" + i.ToString() + "/B", navigator, ref color.B);
                CHelper.TryGetFloatValueFromXML("//root/Colors/Player" + i.ToString() + "/A", navigator, ref color.A);

                PlayerColors.Add(color);
                i++;
            }
            skin.ThemeColors.Player = PlayerColors.ToArray();

            List<SColorScheme> ColorScheme = new List<SColorScheme>();
            List<string> names = CHelper.GetValuesFromXML("ColorSchemes", navigator);
            foreach (string str in names)
            {
                SColorScheme scheme = new SColorScheme();
                scheme.Name = str;

                CHelper.TryGetFloatValueFromXML("//root/ColorSchemes/" + str + "/R", navigator, ref scheme.Color.R);
                CHelper.TryGetFloatValueFromXML("//root/ColorSchemes/" + str + "/G", navigator, ref scheme.Color.G);
                CHelper.TryGetFloatValueFromXML("//root/ColorSchemes/" + str + "/B", navigator, ref scheme.Color.B);
                CHelper.TryGetFloatValueFromXML("//root/ColorSchemes/" + str + "/A", navigator, ref scheme.Color.A);

                ColorScheme.Add(scheme);
            }
            skin.ThemeColors.ColorSchemes = ColorScheme.ToArray();

            _Skins[SkinIndex] = skin;
        }

        private static void LoadCursor(XPathNavigator navigator, int SkinIndex)
        {
            string value = String.Empty;
            CHelper.GetValueFromXML("//root/Cursor/Skin", navigator, ref Cursor.SkinName, value);
            
            CHelper.TryGetFloatValueFromXML("//root/Cursor/W", navigator, ref Cursor.w);
            CHelper.TryGetFloatValueFromXML("//root/Cursor/H", navigator, ref Cursor.h);

            if (CHelper.GetValueFromXML("//root/Cursor/Color", navigator, ref value, value))
            {
                SColorF color = GetColor(value);
                Cursor.r = color.R;
                Cursor.g = color.G;
                Cursor.b = color.B;
                Cursor.a = color.A;
                Cursor.color = value;
            }
            else
            {
                Cursor.color = String.Empty;
                CHelper.TryGetFloatValueFromXML("//root/Cursor/R", navigator, ref Cursor.r);
                CHelper.TryGetFloatValueFromXML("//root/Cursor/G", navigator, ref Cursor.g);
                CHelper.TryGetFloatValueFromXML("//root/Cursor/B", navigator, ref Cursor.b);
                CHelper.TryGetFloatValueFromXML("//root/Cursor/A", navigator, ref Cursor.a);
            }
        }
        #endregion Element loading

        #region Element Writing
        private static void SaveColors(XmlWriter writer, int SkinIndex)
        {
            writer.WriteStartElement("Colors");
            for (int i = 0; i < _Skins[SkinIndex].ThemeColors.Player.Length; i++)
            {
                writer.WriteStartElement("Player" + (i + 1).ToString());

                writer.WriteElementString("R", _Skins[SkinIndex].ThemeColors.Player[i].R.ToString("#0.000"));
                writer.WriteElementString("G", _Skins[SkinIndex].ThemeColors.Player[i].G.ToString("#0.000"));
                writer.WriteElementString("B", _Skins[SkinIndex].ThemeColors.Player[i].B.ToString("#0.000"));
                writer.WriteElementString("A", _Skins[SkinIndex].ThemeColors.Player[i].A.ToString("#0.000"));
                
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("ColorSchemes");
            foreach (SColorScheme scheme in _Skins[SkinIndex].ThemeColors.ColorSchemes)
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

        private static void SaveCursor(XmlWriter writer)
        {
            writer.WriteStartElement("Cursor");

            writer.WriteElementString("Skin", Cursor.SkinName);

            writer.WriteElementString("W", Cursor.w.ToString("#0.000"));
            writer.WriteElementString("H", Cursor.h.ToString("#0.000"));

            if (Cursor.color != String.Empty)
            {
                writer.WriteElementString("Color", Cursor.color);
            }
            else
            {
                writer.WriteElementString("R", Cursor.r.ToString("#0.000"));
                writer.WriteElementString("G", Cursor.g.ToString("#0.000"));
                writer.WriteElementString("B", Cursor.b.ToString("#0.000"));
                writer.WriteElementString("A", Cursor.a.ToString("#0.000"));
            }

            writer.WriteEndElement();
        }
        #endregion ElementWriting

        #region Color Handling
        public static SColorF GetColor(string ColorName)
        {
            SColorF color = new SColorF(1f, 1f, 1f, 1f);

            int SkinIndex = GetSkinIndex();
            foreach (SColorScheme scheme in _Skins[SkinIndex].ThemeColors.ColorSchemes)
            {
                if (scheme.Name == ColorName)
                {
                    color.R = scheme.Color.R;
                    color.G = scheme.Color.G;
                    color.B = scheme.Color.B;
                    color.A = scheme.Color.A;

                    return color;
                }
            }
            return GetPlayerColor(ColorName);
        }

        public static bool GetColor(string ColorName, int SkinIndex, ref SColorF Color)
        {
            foreach (SColorScheme scheme in _Skins[SkinIndex].ThemeColors.ColorSchemes)
            {
                if (scheme.Name == ColorName)
                {
                    Color = new SColorF(scheme.Color);
                    return true;
                }
            }

            bool success;
            GetPlayerColor(ColorName, SkinIndex, out success);
            return success;
        }

        public static SColorF GetPlayerColor(int PlayerNr)
        {
            SColorF color = new SColorF(1f, 1f, 1f, 1f);

            int SkinIndex = GetSkinIndex();

            if (_Skins[SkinIndex].ThemeColors.Player.Length < PlayerNr)
                return color;

            return _Skins[SkinIndex].ThemeColors.Player[PlayerNr - 1];
        }

        public static SColorF GetPlayerColor(string PlayerNrString)
        {
            bool dummy;
            return GetPlayerColor(PlayerNrString, GetSkinIndex(), out dummy);
        }

        public static SColorF GetPlayerColor(string PlayerNrString, int SkinIndex, out bool success)
        {
            SColorF color = new SColorF(1f, 1f, 1f, 1f);

            int selection = -1;
            for (int i = 0; i < _Skins[SkinIndex].ThemeColors.Player.Length; i++)
            {
                if (PlayerNrString == "Player" + (i + 1).ToString())
                {
                    selection = i + 1;
                    break;
                }
            }

            return GetPlayerColor(selection, SkinIndex, out success);
        }

        public static SColorF GetPlayerColor(int PlayerNr, out bool success)
        {
            return GetPlayerColor(PlayerNr, GetSkinIndex(), out success);
        }

        public static SColorF GetPlayerColor(int PlayerNr, int SkinIndex, out bool success)
        {
            success = false;
            SColorF color = new SColorF(1f, 1f, 1f, 1f);

            if (_Skins[SkinIndex].ThemeColors.Player.Length < PlayerNr || PlayerNr < 1)
                return color;

            success = true;
            return _Skins[SkinIndex].ThemeColors.Player[PlayerNr - 1];
        }
        #endregion Color Handling
    }
}
