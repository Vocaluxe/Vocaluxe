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
using System.Windows.Forms;
using System.Xml.Serialization;
using VocaluxeLib.Draw;
using VocaluxeLib.Menu.SingNotes;
using VocaluxeLib.Menu.SongMenu;
using VocaluxeLib.Xml;

namespace VocaluxeLib.Menu
{
    public struct SScreenInformation
    {
        public string ScreenName;
        public int ScreenVersion;
    }

    [XmlType("Screen")]
    public struct STheme
    {
        public SScreenInformation Informations;
        [XmlArray("Backgrounds")] public List<SThemeBackground> Backgrounds;
        [XmlArray("Statics")] public List<SThemeStatic> Statics;
        [XmlArray("Texts")] public List<SThemeText> Texts;
        [XmlArray("Buttons")] public List<SThemeButton> Buttons;
        [XmlArray("SongMenus")] public List<SThemeSongMenu> SongMenus;
        [XmlArray("Lyrics")] public List<SThemeLyrics> Lyrics;
        [XmlArray("SelectSlides")] public List<SThemeSelectSlide> SelectSlides;
        [XmlArray("SingNotes")] public List<SThemeSingBar> SingNotes;
        [XmlArray("NameSelections")] public List<SThemeNameSelection> NameSelections;
        [XmlArray("Equalizers")] public List<SThemeEqualizer> Equalizers;
        [XmlArray("Playlists")] public List<SThemePlaylist> Playlists;
        [XmlArray("ParticleEffects")] public List<SThemeParticleEffect> ParticleEffects;
        [XmlArray("ScreenSettings")] public List<SScreenSetting> ScreenSettings;
    }

    struct SZSort
    {
        public int ID;
        public float Z;
    }

    public abstract class CMenu : CObjectInteractions, IMenu
    {
        public string ThemePath { get; private set; }

        protected abstract int _ScreenVersion { get; }
        public int PartyModeID { get; protected set; }
        public string ThemeName { get; set; }
        public STheme Theme;

        // ReSharper disable MemberCanBePrivate.Global
        protected string[] _ThemeBackgrounds;
        protected string[] _ThemeStatics;
        protected string[] _ThemeTexts;
        protected string[] _ThemeButtons;
        protected string[] _ThemeSongMenus;
        protected string[] _ThemeLyrics;
        protected string[] _ThemeSelectSlides;
        protected string[] _ThemeSingNotes;
        protected string[] _ThemeNameSelections;
        protected string[] _ThemeEqualizers;
        protected string[] _ThemePlaylists;
        protected string[] _ThemeParticleEffects;
        protected string[] _ThemeScreenSettings;
        protected readonly Dictionary<string, CScreenSetting> _ScreenSettings = new Dictionary<string, CScreenSetting>();

        // ReSharper restore MemberCanBePrivate.Global

        protected CMenu()
        {
            PartyModeID = -1;
        }

        public override void Init()
        {
            base.Init();
            ThemeName = GetType().Name;
            if (ThemeName[0] == 'C' && Char.IsUpper(ThemeName[1]))
                ThemeName = ThemeName.Remove(0, 1);

            _ThemeBackgrounds = null;
            _ThemeStatics = null;
            _ThemeTexts = null;
            _ThemeButtons = null;
            _ThemeSongMenus = null;
            _ThemeLyrics = null;
            _ThemeSelectSlides = null;
            _ThemeSingNotes = null;
            _ThemeNameSelections = null;
            _ThemeEqualizers = null;
            _ThemePlaylists = null;
            _ThemeParticleEffects = null;
            _ThemeScreenSettings = null;
        }

        protected static void _FadeTo(EScreens nextScreen)
        {
            CBase.Graphics.FadeTo(nextScreen);
        }

        #region ThemeHandler
        protected override void _ClearElements()
        {
            base._ClearElements();
            _ScreenSettings.Clear();
        }

        private delegate void AddElementHandler<in T>(T element, String key);

        private void _LoadThemeElement<T>(IEnumerable<string> elements, AddElementHandler<T> addElementHandler, CXMLReader xmlReader) where T : IThemeable
        {
            if (elements != null)
            {
                foreach (string elName in elements)
                {
                    var element = (T)Activator.CreateInstance(typeof(T), PartyModeID);
                    if (element.LoadTheme("//root/" + ThemeName, elName, xmlReader))
                        addElementHandler(element, elName);
                    else
                        CBase.Log.LogError("Can't load " + typeof(T).Name.Substring(1) + " \"" + elName + "\" in screen " + ThemeName);
                }
            }
        }

        public virtual void LoadTheme(string xmlPath)
        {
            if (CBase.Config.GetLoadOldThemeFiles())
            {
                LoadThemeOld(xmlPath);
                return;
            }

            ThemePath = xmlPath;

            string file = Path.Combine(xmlPath, ThemeName + ".xml");

            Theme.Informations = new SScreenInformation();
            Theme.Backgrounds = new List<SThemeBackground>();
            Theme.Statics = new List<SThemeStatic>();
            Theme.Texts = new List<SThemeText>();
            Theme.Buttons = new List<SThemeButton>();
            Theme.SongMenus = new List<SThemeSongMenu>();
            Theme.Lyrics = new List<SThemeLyrics>();
            Theme.SelectSlides = new List<SThemeSelectSlide>();
            Theme.SingNotes = new List<SThemeSingBar>();
            Theme.NameSelections = new List<SThemeNameSelection>();
            Theme.Equalizers = new List<SThemeEqualizer>();
            Theme.Playlists = new List<SThemePlaylist>();
            Theme.ParticleEffects = new List<SThemeParticleEffect>();
            Theme.ScreenSettings = new List<SScreenSetting>();

            try
            {
                TextReader textReader = new StreamReader(file);
                XmlSerializer deserializer = new XmlSerializer(typeof(STheme));
                Theme = (STheme)deserializer.Deserialize(textReader);

                foreach (SThemeBackground bg in Theme.Backgrounds)
                    _AddBackground(new CBackground(bg, PartyModeID), bg.Name);

                foreach (SThemeButton bt in Theme.Buttons)
                    _AddButton(new CButton(bt, PartyModeID), bt.Name);

                foreach (SThemeEqualizer eq in Theme.Equalizers)
                    _AddEqualizer(new CEqualizer(eq, PartyModeID), eq.Name);

                foreach (SThemeLyrics ly in Theme.Lyrics)
                    _AddLyric(new CLyric(ly, PartyModeID), ly.Name);

                foreach (SThemeNameSelection ns in Theme.NameSelections)
                    _AddNameSelection(new CNameSelection(ns, PartyModeID), ns.Name);

                foreach (SThemeParticleEffect pe in Theme.ParticleEffects)
                    _AddParticleEffect(new CParticleEffect(pe, PartyModeID), pe.Name);

                foreach (SThemePlaylist pl in Theme.Playlists)
                    _AddPlaylist(new CPlaylist(pl, PartyModeID), pl.Name);

                foreach (SScreenSetting ss in Theme.ScreenSettings)
                    _AddScreenSetting(new CScreenSetting(ss, PartyModeID), ss.Name);

                foreach (SThemeSelectSlide sl in Theme.SelectSlides)
                    _AddSelectSlide(new CSelectSlide(sl, PartyModeID), sl.Name);

                foreach (SThemeSingBar sb in Theme.SingNotes)
                    _AddSingNote(new CSingNotesClassic(sb, PartyModeID), sb.Name);

                foreach (SThemeSongMenu sm in Theme.SongMenus)
                    _AddSongMenu(CSongMenuFactory.GetSongMenu(sm, PartyModeID), sm.Name);

                foreach (SThemeStatic st in Theme.Statics)
                    _AddStatic(new CStatic(st, PartyModeID), st.Name);

                foreach (SThemeText te in Theme.Texts)
                    _AddText(new CText(te, PartyModeID), te.Name);
            }
            catch (InvalidOperationException)
            {
                CBase.Log.LogError("Error while reading " + ThemeName + ".xml", true, true);
            }
            catch (Exception e)
            {
                CBase.Log.LogError(e.Message + e.StackTrace, true, true);
            }
            if (_ScreenVersion != Theme.Informations.ScreenVersion)
            {
                string msg = "Can't load screen file of screen \"" + ThemeName + "\", ";
                if (Theme.Informations.ScreenVersion < _ScreenVersion)
                    msg += "the file ist outdated! ";
                else
                    msg += "the file is for newer program versions! ";

                msg += "Current screen version is " + _ScreenVersion;
                CBase.Log.LogError(msg);
            }
        }

        private void _AddScreenSetting(CScreenSetting screenSetting, string name)
        {
            _ScreenSettings.Add(name, screenSetting);
        }

        public virtual void LoadThemeOld(string xmlPath)
        {
            string file = Path.Combine(xmlPath, ThemeName + ".xml");

            CXMLReader xmlReader = CXMLReader.OpenFile(file);
            if (xmlReader == null)
                return;

            bool versionCheck = _CheckVersion(_ScreenVersion, xmlReader);

            if (versionCheck)
            {
                ThemePath = xmlPath;
                _LoadThemeBasics(xmlReader);

                _LoadThemeElement<CBackground>(_ThemeBackgrounds, _AddBackground, xmlReader);
                _LoadThemeElement<CStatic>(_ThemeStatics, _AddStatic, xmlReader);
                _LoadThemeElement<CText>(_ThemeTexts, _AddText, xmlReader);
                _LoadThemeElement<CButton>(_ThemeButtons, _AddButton, xmlReader);
                _LoadThemeElement<CSelectSlide>(_ThemeSelectSlides, _AddSelectSlide, xmlReader);
                foreach (string elName in _ThemeSongMenus)
                {
                    ISongMenu element = CSongMenuFactory.GetSongMenu(PartyModeID);
                    if (element.LoadTheme("//root/" + ThemeName, elName, xmlReader))
                        _AddSongMenu(element, elName);
                    else
                        CBase.Log.LogError("Can't load songmenu \"" + elName + "\" in screen " + ThemeName);
                }
                _LoadThemeElement<CSongMenuFramework>(_ThemeSongMenus, _AddSongMenu, xmlReader);
                _LoadThemeElement<CLyric>(_ThemeLyrics, _AddLyric, xmlReader);
                _LoadThemeElement<CSingNotesClassic>(_ThemeSingNotes, _AddSingNote, xmlReader);
                _LoadThemeElement<CNameSelection>(_ThemeNameSelections, _AddNameSelection, xmlReader);
                _LoadThemeElement<CEqualizer>(_ThemeEqualizers, _AddEqualizer, xmlReader);
                _LoadThemeElement<CPlaylist>(_ThemePlaylists, _AddPlaylist, xmlReader);
                _LoadThemeElement<CParticleEffect>(_ThemeParticleEffects, _AddParticleEffect, xmlReader);
                _LoadThemeElement<CScreenSetting>(_ThemeScreenSettings, _AddScreenSetting, xmlReader);
            }

            Theme.Informations.ScreenName = ThemeName;
            Theme.Informations.ScreenVersion = _ScreenVersion;
            Theme.Backgrounds = new List<SThemeBackground>();
            Theme.Statics = new List<SThemeStatic>();
            Theme.Texts = new List<SThemeText>();
            Theme.Buttons = new List<SThemeButton>();
            Theme.SongMenus = new List<SThemeSongMenu>();
            Theme.Lyrics = new List<SThemeLyrics>();
            Theme.SelectSlides = new List<SThemeSelectSlide>();
            Theme.SingNotes = new List<SThemeSingBar>();
            Theme.NameSelections = new List<SThemeNameSelection>();
            Theme.Equalizers = new List<SThemeEqualizer>();
            Theme.Playlists = new List<SThemePlaylist>();
            Theme.ParticleEffects = new List<SThemeParticleEffect>();
            Theme.ScreenSettings = new List<SScreenSetting>();
        }

        public virtual void SaveTheme()
        {
            if (CBase.Config.GetLoadOldThemeFiles())
            {
                //Have to add theme-stuff manually, later it will be done while de-serialization
                foreach (CBackground el in _Backgrounds)
                    Theme.Backgrounds.Add(el.GetTheme());
                foreach (CButton el in _Buttons)
                    Theme.Buttons.Add(el.GetTheme());
                foreach (CEqualizer el in _Equalizers)
                    Theme.Equalizers.Add(el.GetTheme());
                foreach (CLyric el in _Lyrics)
                    Theme.Lyrics.Add(el.GetTheme());
                foreach (CNameSelection el in _NameSelections)
                    Theme.NameSelections.Add(el.GetTheme());
                foreach (CParticleEffect el in _ParticleEffects)
                    Theme.ParticleEffects.Add(el.GetTheme());
                foreach (CPlaylist el in _Playlists)
                    Theme.Playlists.Add(el.GetTheme());
                foreach (CScreenSetting el in _ScreenSettings.Values)
                    Theme.ScreenSettings.Add(el.GetTheme());
                foreach (CSelectSlide el in _SelectSlides)
                    Theme.SelectSlides.Add(el.GetTheme());
                foreach (CStatic el in _Statics)
                    Theme.Statics.Add(el.GetTheme());
                foreach (CText el in _Texts)
                    Theme.Texts.Add(el.GetTheme());
                foreach (CSongMenuFramework el in _SongMenus)
                    Theme.SongMenus.Add(el.GetTheme());
                foreach (CSingNotes el in _SingNotes)
                    Theme.SingNotes.Add(el.GetTheme());
            }

            if (string.IsNullOrEmpty(ThemePath))
                return;

            try
            {
                TextWriter textWriter = new StreamWriter(Path.Combine(ThemePath, ThemeName + ".xml"));

                XmlSerializer serializer = new XmlSerializer(typeof(STheme));
                serializer.Serialize(textWriter, Theme);

                textWriter.Close();
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Error while saving theme-file: " + ThemeName + " " + e.Message, true);
            }
        }

        public virtual void ReloadSkin()
        {
            foreach (CBackground background in _Backgrounds)
                background.ReloadSkin();

            foreach (CButton button in _Buttons)
                button.ReloadSkin();

            foreach (CText text in _Texts)
                text.ReloadSkin();

            foreach (CStatic stat in _Statics)
                stat.ReloadSkin();

            foreach (CSelectSlide slide in _SelectSlides)
                slide.ReloadSkin();

            foreach (CSongMenuFramework sm in _SongMenus)
                sm.ReloadSkin();

            foreach (CLyric lyric in _Lyrics)
                lyric.ReloadSkin();

            foreach (CSingNotes sn in _SingNotes)
                sn.ReloadSkin();

            foreach (CNameSelection ns in _NameSelections)
                ns.ReloadSkin();

            foreach (CEqualizer eq in _Equalizers)
                eq.ReloadSkin();

            foreach (CPlaylist pls in _Playlists)
                pls.ReloadSkin();

            foreach (CParticleEffect pe in _ParticleEffects)
                pe.ReloadSkin();
        }

        public virtual void UnloadSkin()
        {
            foreach (CBackground background in _Backgrounds)
                background.UnloadSkin();

            foreach (CButton button in _Buttons)
                button.UnloadSkin();

            foreach (CText text in _Texts)
                text.UnloadSkin();

            foreach (CStatic stat in _Statics)
                stat.UnloadSkin();

            foreach (CSelectSlide slide in _SelectSlides)
                slide.UnloadSkin();

            foreach (CSongMenuFramework sm in _SongMenus)
                sm.UnloadSkin();

            foreach (CLyric lyric in _Lyrics)
                lyric.UnloadSkin();

            foreach (CSingNotes sn in _SingNotes)
                sn.UnloadSkin();

            foreach (CNameSelection ns in _NameSelections)
                ns.UnloadSkin();

            foreach (CPlaylist pls in _Playlists)
                pls.UnloadSkin();

            foreach (CEqualizer eq in _Equalizers)
                eq.UnloadSkin();

            foreach (CParticleEffect pe in _ParticleEffects)
                pe.UnloadSkin();
        }

        public virtual void ReloadTheme(string xmlPath)
        {
            if (ThemePath == "")
                return;

            UnloadSkin();
            _ClearElements();
            LoadTheme(xmlPath);
        }
        #endregion ThemeHandler

        #region Create Elements
        // ReSharper disable UnusedMember.Global
        // ReSharper disable MemberCanBeProtected.Global
        public CButton GetNewButton()
        {
            return new CButton(PartyModeID);
        }

        public static CButton GetNewButton(CButton button)
        {
            return new CButton(button);
        }

        public CText GetNewText()
        {
            return new CText(PartyModeID);
        }

        public static CText GetNewText(CText text)
        {
            return new CText(text);
        }

        public static CText GetNewText(float x, float y, float z, float h, float mw, EAlignment align, EStyle style, string font, SColorF col, string text)
        {
            return new CText(x, y, z, h, mw, align, style, font, col, text);
        }

        public CBackground GetNewBackground()
        {
            return new CBackground(PartyModeID);
        }

        public CStatic GetNewStatic()
        {
            return new CStatic(PartyModeID);
        }

        public static CStatic GetNewStatic(CStatic oldStatic)
        {
            return new CStatic(oldStatic);
        }

        public CStatic GetNewStatic(CTextureRef texture, SColorF color, SRectF rect)
        {
            return new CStatic(PartyModeID, texture, color, rect);
        }

        public CSelectSlide GetNewSelectSlide()
        {
            return new CSelectSlide(PartyModeID);
        }

        public static CSelectSlide GetNewSelectSlide(CSelectSlide slide)
        {
            return new CSelectSlide(slide);
        }

        public CLyric GetNewLyric()
        {
            return new CLyric(PartyModeID);
        }

        public CSingNotes GetNewSingNotes()
        {
            return new CSingNotesClassic(PartyModeID);
        }

        public CNameSelection GetNewNameSelection()
        {
            return new CNameSelection(PartyModeID);
        }

        public CEqualizer GetNewEqualizer()
        {
            return new CEqualizer(PartyModeID);
        }

        public CPlaylist GetNewPlaylist()
        {
            return new CPlaylist(PartyModeID);
        }

        public CParticleEffect GetNewParticleEffect(int maxNumber, SColorF color, SRectF area, CTextureRef texture, float size, EParticleType type)
        {
            return new CParticleEffect(PartyModeID, maxNumber, color, area, texture, size, type);
        }

        // ReSharper restore MemberCanBeProtected.Global
        // ReSharper restore UnusedMember.Global
        #endregion Create Elements

        public override bool HandleInputThemeEditor(SKeyEvent keyEvent)
        {
            if (!keyEvent.KeyPressed)
            {
                switch (keyEvent.Key)
                {
                    case Keys.S:
                        CBase.Graphics.SaveTheme();
                        return true;
                    case Keys.R:
                        _ReloadThemeEditMode();
                        return true;
                }
            }
            return base.HandleInputThemeEditor(keyEvent);
        }

        public abstract bool UpdateGame();

        public virtual void ApplyVolume() {}

        public virtual void OnShow()
        {
            _ResumeBG();
        }

        public virtual void OnShowFinish()
        {
            _ResumeBG();
            _Active = true;
        }

        public virtual void OnClose()
        {
            _PauseBG();
            _Active = false;
        }

        public virtual SRectF ScreenArea
        {
            get { return CBase.Settings.GetRenderRect(); }
        }

        protected void _ResumeBG()
        {
            foreach (CBackground bg in _Backgrounds)
                bg.Resume();
        }

        protected void _PauseBG()
        {
            foreach (CBackground bg in _Backgrounds)
                bg.Pause();
        }

        #region Theme Handling
        private bool _CheckVersion(int reqVersion, CXMLReader xmlReader)
        {
            int actualVersion = 0;
            xmlReader.TryGetIntValue("//root/" + ThemeName + "/ScreenVersion", ref actualVersion);

            if (actualVersion == reqVersion)
                return true;
            string msg = "Can't load screen file of screen \"" + ThemeName + "\", ";
            if (actualVersion < reqVersion)
                msg += "the file ist outdated! ";
            else
                msg += "the file is for newer program versions! ";

            msg += "Current screen version is " + reqVersion;
            CBase.Log.LogError(msg);
            return false;
        }

        private void _LoadThemeBasics(CXMLReader xmlReader)
        {
            // Backgrounds
            var background = new CBackground(PartyModeID);
            int i = 1;
            while (background.LoadTheme("//root/" + ThemeName, "Background" + i, xmlReader))
            {
                _AddBackground(background);
                background = new CBackground(PartyModeID);
                i++;
            }

            // Statics
            var stat = new CStatic(PartyModeID);
            i = 1;
            while (stat.LoadTheme("//root/" + ThemeName, "Static" + i, xmlReader))
            {
                _AddStatic(stat);
                stat = new CStatic(PartyModeID);
                i++;
            }

            // Texts
            var text = new CText(PartyModeID);
            i = 1;
            while (text.LoadTheme("//root/" + ThemeName, "Text" + i, xmlReader))
            {
                _AddText(text);
                text = new CText(PartyModeID);
                i++;
            }

            // ParticleEffects
            var partef = new CParticleEffect(PartyModeID);
            i = 1;
            while (partef.LoadTheme("//root/" + ThemeName, "ParticleEffect" + i, xmlReader))
            {
                _AddParticleEffect(partef);
                partef = new CParticleEffect(PartyModeID);
                i++;
            }
        }

        private void _ReloadThemeEditMode()
        {
            CBase.Themes.Reload();
            CBase.Graphics.ReloadTheme();

            OnShow();
            OnShowFinish();
        }
        #endregion Theme Handling
    }
}