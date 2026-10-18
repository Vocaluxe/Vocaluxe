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
using System.Windows.Forms;
using System.Xml.Serialization;
using VocaluxeLib.Draw;
using VocaluxeLib.Log;
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
    public struct SThemeScreen
    {
        public SScreenInformation Informations;
        [XmlArray("Backgrounds")]
        public List<SThemeBackground> Backgrounds;
        [XmlArray("Statics")]
        public List<SThemeStatic> Statics;
        [XmlArray("Texts")]
        public List<SThemeText> Texts;
        [XmlArray("Buttons")]
        public List<SThemeButton> Buttons;
        [XmlArray("SongMenus")]
        public List<SThemeSongMenu> SongMenus;
        [XmlArray("Lyrics")]
        public List<SThemeLyrics> Lyrics;
        [XmlArray("SelectSlides")]
        public List<SThemeSelectSlide> SelectSlides;
        [XmlArray("SingNotes")]
        public List<SThemeSingBar> SingNotes;
        [XmlArray("NameSelections")]
        public List<SThemeNameSelection> NameSelections;
        [XmlArray("Equalizers")]
        public List<SThemeEqualizer> Equalizers;
        [XmlArray("Playlists")]
        public List<SThemePlaylist> Playlists;
        [XmlArray("ParticleEffects")]
        public List<SThemeParticleEffect> ParticleEffects;
        [XmlArray("ScreenSettings")]
        public List<SThemeScreenSetting> ScreenSettings;
        [XmlArray("ProgressBars")]
        public List<SThemeProgressBar> ProgressBars;
        [XmlArray("RatingPopups")]
        public List<SThemeRatingPopup> RatingPopups;
    }

    struct SZSort
    {
        public int Id;
        public float Z;
    }

    public abstract class CMenu : CObjectInteractions, IMenu
    {
        public string ThemePath { get; private set; }

        protected abstract int _ScreenVersion { get; }
        public int PartyModeId { get; protected set; }
        public string ThemeName { get; private set; }
        public SThemeScreen Theme;

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
        protected string[] _ThemeProgressBars;
        protected string[] _ThemeRatingPopups;
        protected readonly Dictionary<string, CScreenSetting> _ScreenSettings = new Dictionary<string, CScreenSetting>();

        // ReSharper restore MemberCanBePrivate.Global

        protected CMenu()
        {
            PartyModeId = -1;
            CBase.Config.AddSongMenuListener(_OnSongMenuChanged);
        }

        ~CMenu()
        {
            CBase.Config.RemoveSongMenuListener(_OnSongMenuChanged);
        }

        public override void Init()
        {
            base.Init();
            ThemeName = GetType().Name;
            if (ThemeName[0] == 'C' && Char.IsUpper(ThemeName[1]))
            {
                ThemeName = ThemeName.Remove(0, 1);
            }

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
            _ThemeProgressBars = null;
            _ThemeRatingPopups = null;
        }

        protected virtual void _OnSongMenuChanged()
        {
            for (var i = 0; i < _SongMenus.Count; i++)
            {
                var theme = (SThemeSongMenu)_SongMenus[i].GetTheme();
                _SongMenus[i] = CSongMenuFactory.CreateSongMenu(theme, PartyModeId);
                _SongMenus[i].LoadSkin();
            }
        }

        #region ThemeHandler
        protected override void _ClearElements()
        {
            base._ClearElements();
            _ScreenSettings.Clear();
        }

        private delegate void AddElementHandler<in T>(T element, String key);

        private class CLoadThemeErrorHandler : CXmlDeserializer.CXmlDefaultErrorHandler
        {
            private static readonly string[] _AllowedMissing = new string[]
            {
                "Backgrounds", "Statics", "Texts", "Buttons", "SongMenus", "Lyrics", "SelectSlides", "SingNotes",
                "NameSelections", "Equalizers", "Playlists", "ParticleEffects", "ScreenSettings", "ProgressBars",
                "RatingPopups"
            };

            public override void HandleError(CXmlException e)
            {
                var missingEx = e as CXmlMissingElementException;
                if (missingEx != null)
                {
                    if (_AllowedMissing.Contains(missingEx.Field.Name))
                    {
                        return;
                    }
                }

                base.HandleError(e);
            }
        }

        public virtual void LoadTheme(string xmlPath)
        {
            ThemePath = xmlPath;

            var file = Path.Combine(xmlPath, ThemeName + ".xml");

            try
            {
                var deserializer = new CXmlDeserializer(new CLoadThemeErrorHandler());
                Theme = deserializer.Deserialize<SThemeScreen>(file);

                foreach (var bg in Theme.Backgrounds)
                {
                    _AddBackground(new CBackground(bg, PartyModeId), bg.Name);
                }

                foreach (var bt in Theme.Buttons)
                {
                    _AddButton(new CButton(bt, PartyModeId), bt.Name);
                }

                foreach (var eq in Theme.Equalizers)
                {
                    _AddEqualizer(new CEqualizer(eq, PartyModeId), eq.Name);
                }

                foreach (var ly in Theme.Lyrics)
                {
                    _AddLyric(new CLyric(ly, PartyModeId), ly.Name);
                }

                foreach (var ns in Theme.NameSelections)
                {
                    _AddNameSelection(new CNameSelection(ns, PartyModeId), ns.Name);
                }

                foreach (var pe in Theme.ParticleEffects)
                {
                    _AddParticleEffect(new CParticleEffect(pe, PartyModeId), pe.Name);
                }

                foreach (var pl in Theme.Playlists)
                {
                    _AddPlaylist(new CPlaylist(pl, PartyModeId), pl.Name);
                }

                foreach (var pb in Theme.ProgressBars)
                {
                    _AddProgressBar(new CProgressBar(pb, PartyModeId), pb.Name);
                }

                foreach (var rp in Theme.RatingPopups)
                {
                    _AddRatingPopup(new CRatingPopup(rp, PartyModeId), rp.Name);
                }

                foreach (var ss in Theme.ScreenSettings)
                {
                    _AddScreenSetting(new CScreenSetting(ss, PartyModeId), ss.Name);
                }

                foreach (var sl in Theme.SelectSlides)
                {
                    _AddSelectSlide(new CSelectSlide(sl, PartyModeId), sl.Name);
                }

                foreach (var sb in Theme.SingNotes)
                {
                    _AddSingNote(new CSingNotes(sb, PartyModeId), sb.Name);
                }

                foreach (var sm in Theme.SongMenus)
                {
                    _AddSongMenu(CSongMenuFactory.CreateSongMenu(sm, PartyModeId), sm.Name);
                }

                foreach (var st in Theme.Statics)
                {
                    _AddStatic(new CStatic(st, PartyModeId), st.Name);
                }

                foreach (var te in Theme.Texts)
                {
                    _AddText(new CText(te, PartyModeId), te.Name);
                }

                if (_ScreenVersion != Theme.Informations.ScreenVersion)
                {
                    var msg = "Can't load screen file of screen \"" + ThemeName + "\", ";
                    if (Theme.Informations.ScreenVersion < _ScreenVersion)
                    {
                        msg += "the file ist outdated! ";
                    }
                    else
                    {
                        msg += "the file is for newer program versions! ";
                    }

                    msg += "Current screen version is " + _ScreenVersion;
                    CLog.Error(msg);
                }

                foreach (var el in _Elements.Select(_GetElement).OfType<IThemeable>())
                {
                    el.LoadSkin();
                }
            }
            catch (Exception e)
            {
                CLog.Fatal(e, "Error while reading {ThemeName}.xml", CLog.Params(ThemeName), true);
            }
        }

        private void _AddScreenSetting(CScreenSetting screenSetting, string name)
        {
            _ScreenSettings.Add(name, screenSetting);
        }

        private static void _AddThemeablesToList<T, TT>(ICollection<TT> themeList, IEnumerable<T> objects) where T : IThemeable
        {
            themeList.Clear();
            foreach (var el in objects.Where(el => el.ThemeLoaded))
            {
                themeList.Add((TT)el.GetTheme());
            }
        }

        public virtual void SaveTheme()
        {
            if (string.IsNullOrEmpty(ThemePath))
            {
                return;
            }

            _ReadThemeSubElements();

            try
            {
                var serializer = new CXmlSerializer();
                serializer.Serialize(Path.Combine(ThemePath, ThemeName + ".xml"), Theme);
            }
            catch (Exception e)
            {
                CLog.Error("Error while saving theme-file: " + ThemeName + " " + e.Message, true);
            }
        }

        private void _ReadThemeSubElements()
        {
            // Load changed/added theme elements into theme struct
            _AddThemeablesToList(Theme.Backgrounds, _Backgrounds);
            _AddThemeablesToList(Theme.Buttons, _Buttons);
            _AddThemeablesToList(Theme.Equalizers, _Equalizers);
            _AddThemeablesToList(Theme.Lyrics, _Lyrics);
            _AddThemeablesToList(Theme.NameSelections, _NameSelections);
            _AddThemeablesToList(Theme.ParticleEffects, _ParticleEffects);
            _AddThemeablesToList(Theme.Playlists, _Playlists);
            _AddThemeablesToList(Theme.ProgressBars, _ProgressBars);
            _AddThemeablesToList(Theme.RatingPopups, _RatingPopups);
            _AddThemeablesToList(Theme.ScreenSettings, _ScreenSettings.Values);
            _AddThemeablesToList(Theme.SelectSlides, _SelectSlides);
            _AddThemeablesToList(Theme.Statics, _Statics);
            _AddThemeablesToList(Theme.Texts, _Texts);
            _AddThemeablesToList(Theme.SongMenus, _SongMenus);
            _AddThemeablesToList(Theme.SingNotes, _SingNotes);
        }

        public virtual void ReloadSkin()
        {
            foreach (var el in _Elements.Select(_GetElement).OfType<IThemeable>())
            {
                el.ReloadSkin();
            }
        }

        public virtual void UnloadSkin()
        {
            foreach (var el in _Elements.Select(_GetElement).OfType<IThemeable>())
            {
                el.UnloadSkin();
            }
        }

        public virtual void ReloadTheme(string xmlPath)
        {
            if (ThemePath == "")
            {
                return;
            }

            _ReadThemeSubElements();
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
            return new CButton(PartyModeId);
        }

        public static CButton GetNewButton(CButton button)
        {
            return new CButton(button);
        }

        public CText GetNewText()
        {
            return new CText(PartyModeId);
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
            return new CBackground(PartyModeId);
        }

        public CStatic GetNewStatic()
        {
            return new CStatic(PartyModeId);
        }

        public static CStatic GetNewStatic(CStatic oldStatic)
        {
            return new CStatic(oldStatic);
        }

        public CStatic GetNewStatic(CTextureRef texture, SColorF color, SRectF rect)
        {
            return new CStatic(PartyModeId, texture, color, rect);
        }

        public CSelectSlide GetNewSelectSlide()
        {
            return new CSelectSlide(PartyModeId);
        }

        public static CSelectSlide GetNewSelectSlide(CSelectSlide slide)
        {
            return new CSelectSlide(slide);
        }

        public CLyric GetNewLyric()
        {
            return new CLyric(PartyModeId);
        }

        public CSingNotes GetNewSingNotes()
        {
            return new CSingNotes(PartyModeId);
        }

        public CNameSelection GetNewNameSelection()
        {
            return new CNameSelection(PartyModeId);
        }

        public CEqualizer GetNewEqualizer()
        {
            return new CEqualizer(PartyModeId);
        }

        public CPlaylist GetNewPlaylist()
        {
            return new CPlaylist(PartyModeId);
        }

        public CParticleEffect GetNewParticleEffect(int maxNumber, SColorF color, SRectF area, CTextureRef texture, float size, EParticleType type)
        {
            return new CParticleEffect(PartyModeId, maxNumber, color, area, texture, size, type);
        }

        public CProgressBar GetNewProgressBar()
        {
            return new CProgressBar(PartyModeId);
        }

        public CProgressBar GetNewProgressBar(CProgressBar pb)
        {
            return new CProgressBar(pb);
        }

        public CRatingPopup GetNewRatingPopup()
        {
            return new CRatingPopup(PartyModeId);
        }

        public static CRatingPopup GetNewRatingPopup(CRatingPopup rp)
        {
            return new CRatingPopup(rp);
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

        public virtual void ApplyVolume() { }

        public virtual void OnShow()
        {
            _ResumeBG();
            _Active = true;
        }

        public virtual void OnShowFinish()
        {
            _ResumeBG();
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

        public virtual EMusicType CurrentMusicType
        {
            get { return EMusicType.Background; }
        }

        protected void _ResumeBG()
        {
            foreach (var bg in _Backgrounds)
            {
                bg.Resume();
            }
        }

        protected void _PauseBG()
        {
            foreach (var bg in _Backgrounds)
            {
                bg.Pause();
            }
        }

        #region Theme Handling
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