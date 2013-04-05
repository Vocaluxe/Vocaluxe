using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using VocaluxeLib.Menu.SingNotes;
using VocaluxeLib.Menu.SongMenu;

namespace VocaluxeLib.Menu
{
    struct ZSort
    {
        public int ID;
        public float z;
    }

    public abstract class CMenu : IMenu
    {
        private List<CInteraction> _Interactions;
        private int _Selection;
        private string _ThemePath = String.Empty;
        protected int _PartyModeID = -1;

        private COrderedDictionaryLite<CBackground> _Backgrounds;
        private COrderedDictionaryLite<CButton> _Buttons;
        private COrderedDictionaryLite<CText> _Texts;
        private COrderedDictionaryLite<CStatic> _Statics;
        private COrderedDictionaryLite<CSelectSlide> _SelectSlides;
        private COrderedDictionaryLite<CSongMenu> _SongMenus;
        private COrderedDictionaryLite<CLyric> _Lyrics;
        private COrderedDictionaryLite<CSingNotes> _SingNotes;
        private COrderedDictionaryLite<CNameSelection> _NameSelections;
        private COrderedDictionaryLite<CEqualizer> _Equalizers;
        private COrderedDictionaryLite<CPlaylist> _Playlists;
        private COrderedDictionaryLite<CParticleEffect> _ParticleEffects;
        private COrderedDictionaryLite<CScreenSetting> _ScreenSettings;

        private int _PrevMouseX;
        private int _PrevMouseY;

        protected int _MouseDX;
        protected int _MouseDY;

        protected bool _Active;

        protected abstract int _ScreenVersion { get; }
        private string _ThemeName;
        public string ThemeName
        {
            get { return _ThemeName; }
        }

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

        protected SRectF _ScreenArea;
        public SRectF ScreenArea
        {
            get { return _ScreenArea; }
        }

        public int ThemeScreenVersion
        {
            get { return _ScreenVersion; }
        }

        public virtual void Init()
        {
            _ThemeName = GetType().Name;
            if (_ThemeName[0] == 'C' && Char.IsUpper(_ThemeName[1]))
                _ThemeName = _ThemeName.Remove(0, 1);

            _Interactions = new List<CInteraction>();
            _Selection = 0;

            _Backgrounds = new COrderedDictionaryLite<CBackground>(this);
            _Buttons = new COrderedDictionaryLite<CButton>(this);
            _Texts = new COrderedDictionaryLite<CText>(this);
            _Statics = new COrderedDictionaryLite<CStatic>(this);
            _SelectSlides = new COrderedDictionaryLite<CSelectSlide>(this);
            _SongMenus = new COrderedDictionaryLite<CSongMenu>(this);
            _Lyrics = new COrderedDictionaryLite<CLyric>(this);
            _SingNotes = new COrderedDictionaryLite<CSingNotes>(this);
            _NameSelections = new COrderedDictionaryLite<CNameSelection>(this);
            _Equalizers = new COrderedDictionaryLite<CEqualizer>(this);
            _Playlists = new COrderedDictionaryLite<CPlaylist>(this);
            _ParticleEffects = new COrderedDictionaryLite<CParticleEffect>(this);
            _ScreenSettings = new COrderedDictionaryLite<CScreenSetting>(this);

            _PrevMouseX = 0;
            _PrevMouseY = 0;

            _MouseDX = 0;
            _MouseDY = 0;

            _Active = false;
            _ScreenArea = new SRectF(0f, 0f, CBase.Settings.GetRenderW(), CBase.Settings.GetRenderH(), 0f);

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

        protected void FadeTo(EScreens NextScreen)
        {
            CBase.Graphics.FadeTo(NextScreen);
        }

        #region ThemeHandler
        protected delegate void DAddElement<T>(T Element, String key);

        private void LoadThemeElement<T>(string[] Elements, DAddElement<T> MAddElement, CXMLReader xmlReader, int SkinIndex) where T : IMenuElement
        {
            if (Elements != null)
            {
                foreach (string elName in Elements)
                {
                    T Element = (T)Activator.CreateInstance(typeof(T), _PartyModeID);
                    if (Element.LoadTheme("//root/" + ThemeName, elName, xmlReader, SkinIndex))
                        MAddElement(Element, elName);
                    else
                        CBase.Log.LogError("Can't load " + typeof(T).Name.Substring(1) + " \"" + elName + "\" in screen " + ThemeName);
                }
            }
        }

        public virtual void LoadTheme(string XmlPath)
        {
            string file = Path.Combine(XmlPath, ThemeName + ".xml");

            CXMLReader xmlReader = CXMLReader.OpenFile(file);
            if (xmlReader == null)
                return;

            bool VersionCheck = false;
            VersionCheck = CheckVersion(_ScreenVersion, xmlReader);

            int SkinIndex = CBase.Theme.GetSkinIndex(_PartyModeID);

            if (VersionCheck && SkinIndex != -1)
            {
                _ThemePath = XmlPath;
                LoadThemeBasics(xmlReader, SkinIndex);

                LoadThemeElement<CBackground>(_ThemeBackgrounds, AddBackground, xmlReader, SkinIndex);
                LoadThemeElement<CStatic>(_ThemeStatics, AddStatic, xmlReader, SkinIndex);
                LoadThemeElement<CText>(_ThemeTexts, AddText, xmlReader, SkinIndex);
                LoadThemeElement<CButton>(_ThemeButtons, AddButton, xmlReader, SkinIndex);
                LoadThemeElement<CSelectSlide>(_ThemeSelectSlides, AddSelectSlide, xmlReader, SkinIndex);
                LoadThemeElement<CSongMenu>(_ThemeSongMenus, AddSongMenu, xmlReader, SkinIndex);
                LoadThemeElement<CLyric>(_ThemeLyrics, AddLyric, xmlReader, SkinIndex);
                LoadThemeElement<CSingNotesClassic>(_ThemeSingNotes, AddSingNote, xmlReader, SkinIndex);
                LoadThemeElement<CNameSelection>(_ThemeNameSelections, AddNameSelection, xmlReader, SkinIndex);
                LoadThemeElement<CEqualizer>(_ThemeEqualizers, AddEqualizer, xmlReader, SkinIndex);
                LoadThemeElement<CPlaylist>(_ThemePlaylists, AddPlaylist, xmlReader, SkinIndex);
                LoadThemeElement<CParticleEffect>(_ThemeParticleEffects, AddParticleEffect, xmlReader, SkinIndex);
                LoadThemeElement<CScreenSetting>(_ThemeScreenSettings, AddScreenSetting, xmlReader, SkinIndex);
            }
        }

        public virtual void SaveTheme()
        {
            if (_ThemePath.Length == 0)
                return;

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = Encoding.UTF8;
            settings.ConformanceLevel = ConformanceLevel.Document;

            string file = Path.Combine(_ThemePath, ThemeName + ".xml");
            using (XmlWriter writer = XmlWriter.Create(file, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("root");

                writer.WriteStartElement(ThemeName);

                // Screen Version
                writer.WriteElementString("ScreenVersion", _ScreenVersion.ToString());

                // Backgrounds
                foreach (CBackground bg in _Backgrounds)
                    bg.SaveTheme(writer);

                // Statics
                foreach (CStatic st in _Statics)
                    st.SaveTheme(writer);

                // Texts
                foreach (CText txt in _Texts)
                    txt.SaveTheme(writer);

                // Buttons
                foreach (CButton bt in _Buttons)
                    bt.SaveTheme(writer);

                // SelectSlides
                foreach (CSelectSlide ss in _SelectSlides)
                    ss.SaveTheme(writer);

                // SongMenus
                foreach (CSongMenu sm in _SongMenus)
                    sm.SaveTheme(writer);

                // Lyrics
                foreach (CLyric ly in _Lyrics)
                    ly.SaveTheme(writer);

                // SingBars
                foreach (CSingNotes sn in _SingNotes)
                    sn.SaveTheme(writer);

                // NameSelections
                foreach (CNameSelection ns in _NameSelections)
                    ns.SaveTheme(writer);

                //Equalizers
                foreach (CEqualizer eq in _Equalizers)
                    eq.SaveTheme(writer);

                //Playlists
                foreach (CPlaylist pl in _Playlists)
                    pl.SaveTheme(writer);

                //ParticleEffects
                foreach (CParticleEffect pa in _ParticleEffects)
                    pa.SaveTheme(writer);

                //ScreenSettings
                foreach (CScreenSetting cs in _ScreenSettings)
                    cs.SaveTheme(writer);

                writer.WriteEndElement();

                // End of File
                writer.WriteEndElement(); //end of root
                writer.WriteEndDocument();

                writer.Flush();
            }
        }

        public virtual void ReloadTextures()
        {
            foreach (CBackground background in _Backgrounds)
                background.ReloadTextures();

            foreach (CButton button in _Buttons)
                button.ReloadTextures();

            foreach (CText text in _Texts)
                text.ReloadTextures();

            foreach (CStatic stat in _Statics)
                stat.ReloadTextures();

            foreach (CSelectSlide slide in _SelectSlides)
                slide.ReloadTextures();

            foreach (CSongMenu sm in _SongMenus)
                sm.ReloadTextures();

            foreach (CLyric lyric in _Lyrics)
                lyric.ReloadTextures();

            foreach (CSingNotes sn in _SingNotes)
                sn.ReloadTextures();

            foreach (CNameSelection ns in _NameSelections)
                ns.ReloadTextures();

            foreach (CEqualizer eq in _Equalizers)
                eq.ReloadTextures();

            foreach (CPlaylist pls in _Playlists)
                pls.ReloadTextures();

            foreach (CParticleEffect pe in _ParticleEffects)
                pe.ReloadTextures();

            foreach (CScreenSetting se in _ScreenSettings)
                se.ReloadTextures();
        }

        public virtual void UnloadTextures()
        {
            foreach (CBackground background in _Backgrounds)
                background.UnloadTextures();

            foreach (CButton button in _Buttons)
                button.UnloadTextures();

            foreach (CText text in _Texts)
                text.UnloadTextures();

            foreach (CStatic stat in _Statics)
                stat.UnloadTextures();

            foreach (CSelectSlide slide in _SelectSlides)
                slide.UnloadTextures();

            foreach (CSongMenu sm in _SongMenus)
                sm.UnloadTextures();

            foreach (CLyric lyric in _Lyrics)
                lyric.UnloadTextures();

            foreach (CSingNotes sn in _SingNotes)
                sn.UnloadTextures();

            foreach (CNameSelection ns in _NameSelections)
                ns.UnloadTextures();
            foreach (CPlaylist pls in _Playlists)
                pls.UnloadTextures();

            foreach (CEqualizer eq in _Equalizers)
                eq.UnloadTextures();

            foreach (CParticleEffect pe in _ParticleEffects)
                pe.UnloadTextures();

            foreach (CScreenSetting se in _ScreenSettings)
                se.UnloadTextures();
        }

        public virtual void ReloadTheme(string XmlPath)
        {
            if (_ThemePath.Length == 0)
                return;

            UnloadTextures();
            Init();
            LoadTheme(XmlPath);
        }
        #endregion ThemeHandler

        #region GetLists
        /*public List<CButton> GetButtons()
        {
            return _Buttons;
        }

        public List<CText> GetTexts()
        {
            return _Texts;
        }

        public List<CBackground> GetBackgrounds()
        {
            return _Backgrounds;
        }

        public List<CStatic> GetStatics()
        {
            return _Statics;
        }

        public List<CSelectSlide> GetSelectSlides()
        {
            return _SelectSlides;
        }

        public List<CSongMenu> GetSongMenus()
        {
            return _SongMenus;
        }

        public List<CLyric> GetLyrics()
        {
            return _Lyrics;
        }

        public List<CSingNotes> GetSingNotes()
        {
            return _SingNotes;
        }

        public List<CNameSelection> GetNameSelections()
        {
            return _NameSelections;
        }

        public List<CEqualizer> GetEqualizers()
        {
            return _Equalizers;
        }

        public List<CPlaylist> GetPlaylists()
        {
            return _Playlists;
        }

        public List<CParticleEffect> GetParticleEffects()
        {
            return _ParticleEffects;
        }

        public List<CScreenSetting> GetScreenSettings()
        {
            return _ScreenSettings;
        }*/
        #endregion GetLists

        #region ElementHandler

        #region Create Elements
        public CButton GetNewButton()
        {
            return new CButton(_PartyModeID);
        }

        public CButton GetNewButton(CButton button)
        {
            return new CButton(button);
        }

        public CText GetNewText()
        {
            return new CText(_PartyModeID);
        }

        public CText GetNewText(CText text)
        {
            return new CText(text);
        }

        public CText GetNewText(float x, float y, float z, float h, float mw, EAlignment align, EStyle style, string font, SColorF col, string text)
        {
            return new CText(x, y, z, h, mw, align, style, font, col, text);
        }

        public CBackground GetNewBackground()
        {
            return new CBackground(_PartyModeID);
        }

        public CStatic GetNewStatic()
        {
            return new CStatic(_PartyModeID);
        }

        public CStatic GetNewStatic(CStatic oldStatic)
        {
            return new CStatic(oldStatic);
        }

        public CStatic GetNewStatic(STexture Texture, SColorF Color, SRectF Rect)
        {
            return new CStatic(_PartyModeID, Texture, Color, Rect);
        }

        public CSelectSlide GetNewSelectSlide()
        {
            return new CSelectSlide(_PartyModeID);
        }

        public CSelectSlide GetNewSelectSlide(CSelectSlide slide)
        {
            return new CSelectSlide(slide);
        }

        public CSongMenu GetNewSongMenu()
        {
            return new CSongMenu(_PartyModeID);
        }

        public CLyric GetNewLyric()
        {
            return new CLyric(_PartyModeID);
        }

        public CSingNotes GetNewSingNotes()
        {
            return new CSingNotesClassic(_PartyModeID);
        }

        public CNameSelection GetNewNameSelection()
        {
            return new CNameSelection(_PartyModeID);
        }

        public CEqualizer GetNewEqualizer()
        {
            return new CEqualizer(_PartyModeID);
        }

        public CPlaylist GetNewPlaylist()
        {
            return new CPlaylist(_PartyModeID);
        }

        public CParticleEffect GetNewParticleEffect(int MaxNumber, SColorF Color, SRectF Area, STexture Texture, float Size, EParticleType Type)
        {
            return new CParticleEffect(_PartyModeID, MaxNumber, Color, Area, Texture, Size, Type);
        }
        #endregion Create Elements

        #region Get Elements
        public COrderedDictionaryLite<CButton> Buttons
        {
            get { return _Buttons; }
        }

        public COrderedDictionaryLite<CText> Texts
        {
            get { return _Texts; }
        }

        public COrderedDictionaryLite<CBackground> Backgrounds
        {
            get { return _Backgrounds; }
        }

        public COrderedDictionaryLite<CStatic> Statics
        {
            get { return _Statics; }
        }

        public COrderedDictionaryLite<CSelectSlide> SelectSlides
        {
            get { return _SelectSlides; }
        }

        public COrderedDictionaryLite<CSongMenu> SongMenus
        {
            get { return _SongMenus; }
        }

        public COrderedDictionaryLite<CLyric> Lyrics
        {
            get { return _Lyrics; }
        }

        public COrderedDictionaryLite<CSingNotes> SingNotes
        {
            get { return _SingNotes; }
        }

        public COrderedDictionaryLite<CNameSelection> NameSelections
        {
            get { return _NameSelections; }
        }

        public COrderedDictionaryLite<CEqualizer> Equalizers
        {
            get { return _Equalizers; }
        }

        public COrderedDictionaryLite<CPlaylist> Playlists
        {
            get { return _Playlists; }
        }

        public COrderedDictionaryLite<CParticleEffect> ParticleEffects
        {
            get { return _ParticleEffects; }
        }

        public COrderedDictionaryLite<CScreenSetting> ScreenSettings
        {
            get { return _ScreenSettings; }
        }
        #endregion Get Arrays

        #endregion ElementHandler

        #region MenuHandler
        public virtual bool HandleInput(KeyEvent keyEvent)
        {
            if (!CBase.Settings.IsTabNavigation())
            {
                if (keyEvent.Key == Keys.Left)
                {
                    if (_Interactions.Count > 0 && _Interactions[_Selection].Type == EType.TSelectSlide && keyEvent.Mod != EModifier.Shift)
                        keyEvent.Handled = PrevElement();
                    else
                        keyEvent.Handled = _NextInteraction(keyEvent);
                }

                if (keyEvent.Key == Keys.Right)
                {
                    if (_Interactions.Count > 0 && _Interactions[_Selection].Type == EType.TSelectSlide && keyEvent.Mod != EModifier.Shift)
                        keyEvent.Handled = NextElement();
                    else
                        keyEvent.Handled = _NextInteraction(keyEvent);
                }

                if (keyEvent.Key == Keys.Up || keyEvent.Key == Keys.Down)
                    keyEvent.Handled = _NextInteraction(keyEvent);
            }
            else
            {
                if (keyEvent.Key == Keys.Tab)
                {
                    if (keyEvent.Mod == EModifier.Shift)
                        PrevInteraction();
                    else
                        NextInteraction();
                }

                if (keyEvent.Key == Keys.Left)
                    PrevElement();

                if (keyEvent.Key == Keys.Right)
                    NextElement();
            }

            return true;
        }

        public virtual bool HandleMouse(MouseEvent mouseEvent)
        {
            int selection = _Selection;
            ProcessMouseMove(mouseEvent.X, mouseEvent.Y);
            if (selection != _Selection)
            {
                _UnsetHighlighted(selection);
                _SetHighlighted(_Selection);
            }

            if (mouseEvent.LB)
                ProcessMouseClick(mouseEvent.X, mouseEvent.Y);

            _PrevMouseX = mouseEvent.X;
            _PrevMouseY = mouseEvent.Y;

            return true;
        }

        public virtual bool HandleInputThemeEditor(KeyEvent KeyEvent)
        {
            _UnsetHighlighted(_Selection);
            if (!KeyEvent.KeyPressed)
            {
                switch (KeyEvent.Key)
                {
                    case Keys.S:
                        CBase.Graphics.SaveTheme();
                        break;

                    case Keys.R:
                        ReloadThemeEditMode();
                        break;

                    case Keys.Up:
                        if (KeyEvent.Mod == EModifier.Ctrl)
                            MoveElement(0, -1);

                        if (KeyEvent.Mod == EModifier.Shift)
                            ResizeElement(0, 1);

                        break;
                    case Keys.Down:
                        if (KeyEvent.Mod == EModifier.Ctrl)
                            MoveElement(0, 1);

                        if (KeyEvent.Mod == EModifier.Shift)
                            ResizeElement(0, -1);

                        break;

                    case Keys.Right:
                        if (KeyEvent.Mod == EModifier.Ctrl)
                            MoveElement(1, 0);

                        if (KeyEvent.Mod == EModifier.Shift)
                            ResizeElement(1, 0);

                        if (KeyEvent.Mod == EModifier.None)
                            NextInteraction();
                        break;
                    case Keys.Left:
                        if (KeyEvent.Mod == EModifier.Ctrl)
                            MoveElement(-1, 0);

                        if (KeyEvent.Mod == EModifier.Shift)
                            ResizeElement(-1, 0);

                        if (KeyEvent.Mod == EModifier.None)
                            PrevInteraction();
                        break;
                }
            }
            return true;
        }

        public virtual bool HandleMouseThemeEditor(MouseEvent MouseEvent)
        {
            _UnsetHighlighted(_Selection);
            _MouseDX = MouseEvent.X - _PrevMouseX;
            _MouseDY = MouseEvent.Y - _PrevMouseY;

            int stepX = 0;
            int stepY = 0;

            if ((MouseEvent.Mod & EModifier.Ctrl) == EModifier.Ctrl)
            {
                _PrevMouseX = MouseEvent.X;
                _PrevMouseY = MouseEvent.Y;
            }
            else
            {
                while (Math.Abs(MouseEvent.X - _PrevMouseX) >= 5)
                {
                    if (MouseEvent.X - _PrevMouseX >= 5)
                        stepX += 5;

                    if (MouseEvent.X - _PrevMouseX <= -5)
                        stepX -= 5;

                    _PrevMouseX = MouseEvent.X - (_MouseDX - stepX);
                }

                while (Math.Abs(MouseEvent.Y - _PrevMouseY) >= 5)
                {
                    if (MouseEvent.Y - _PrevMouseY >= 5)
                        stepY += 5;

                    if (MouseEvent.Y - _PrevMouseY <= -5)
                        stepY -= 5;

                    _PrevMouseY = MouseEvent.Y - (_MouseDY - stepY);
                }
            }

            if (MouseEvent.LBH)
            {
                //if (IsMouseOver(MouseEvent.X, _PrevMouseY))
                //{
                if (MouseEvent.Mod == EModifier.None)
                    MoveElement(stepX, stepY);

                if (MouseEvent.Mod == EModifier.Ctrl)
                    MoveElement(_MouseDX, _MouseDY);

                if (MouseEvent.Mod == EModifier.Shift)
                    ResizeElement(stepX, stepY);

                if (MouseEvent.Mod == (EModifier.Shift | EModifier.Ctrl))
                    ResizeElement(_MouseDX, _MouseDY);
                //}
            }
            else
                ProcessMouseMove(MouseEvent.X, MouseEvent.Y);

            return true;
        }

        public abstract bool UpdateGame();

        public virtual void ApplyVolume() {}

        public virtual void OnShow()
        {
            ResumeBG();
        }

        public virtual void OnShowFinish()
        {
            ResumeBG();
            _Active = true;
        }

        public virtual void OnClose()
        {
            PauseBG();
            _Active = false;
        }
        #endregion MenuHandler

        protected void ResumeBG()
        {
            foreach (CBackground bg in _Backgrounds)
                bg.Resume();
        }

        protected void PauseBG()
        {
            foreach (CBackground bg in _Backgrounds)
                bg.Pause();
        }

        #region Drawing
        public virtual bool Draw()
        {
            DrawBG();
            DrawFG();
            return true;
        }

        public void DrawBG()
        {
            foreach (CBackground bg in _Backgrounds)
                bg.Draw();
        }

        public void DrawFG()
        {
            if (_Interactions.Count <= 0)
                return;

            List<ZSort> items = new List<ZSort>();

            for (int i = 0; i < _Interactions.Count; i++)
            {
                if (_IsVisible(i) && (
                                         _Interactions[i].Type == EType.TButton ||
                                         _Interactions[i].Type == EType.TSelectSlide ||
                                         _Interactions[i].Type == EType.TStatic ||
                                         _Interactions[i].Type == EType.TNameSelection ||
                                         _Interactions[i].Type == EType.TText ||
                                         _Interactions[i].Type == EType.TSongMenu ||
                                         _Interactions[i].Type == EType.TEqualizer ||
                                         _Interactions[i].Type == EType.TPlaylist ||
                                         _Interactions[i].Type == EType.TParticleEffect))
                {
                    ZSort zs = new ZSort();
                    zs.ID = i;
                    zs.z = _GetZValue(i);
                    items.Add(zs);
                }
            }

            if (items.Count <= 0)
                return;


            items.Sort(delegate(ZSort s1, ZSort s2) { return s2.z.CompareTo(s1.z); });

            for (int i = 0; i < items.Count; i++)
                _DrawInteraction(items[i].ID);
        }
        #endregion Drawing

        #region Elements
        public void AddBackground(CBackground bg)
        {
            AddBackground(bg, null);
        }

        public void AddButton(CButton button)
        {
            AddButton(button, null);
        }

        public void AddSelectSlide(CSelectSlide slide)
        {
            AddSelectSlide(slide, null);
        }

        public void AddStatic(CStatic stat)
        {
            AddStatic(stat, null);
        }

        public void AddText(CText text)
        {
            AddText(text, null);
        }

        public void AddSongMenu(CSongMenu songmenu)
        {
            AddSongMenu(songmenu, null);
        }

        public void AddLyric(CLyric lyric)
        {
            AddLyric(lyric, null);
        }

        public void AddSingNote(CSingNotes sn)
        {
            AddSingNote(sn, null);
        }

        public void AddNameSelection(CNameSelection ns)
        {
            AddNameSelection(ns, null);
        }

        public void AddEqualizer(CEqualizer eq)
        {
            AddEqualizer(eq, null);
        }

        public void AddPlaylist(CPlaylist pls)
        {
            AddPlaylist(pls, null);
        }

        public void AddParticleEffect(CParticleEffect pe)
        {
            AddParticleEffect(pe, null);
        }

        public void AddScreenSetting(CScreenSetting se)
        {
            AddScreenSetting(se, null);
        }

        public void AddBackground(CBackground bg, String key)
        {
            _AddInteraction(_Backgrounds.Add(bg, key), EType.TBackground);
        }

        public void AddButton(CButton button, String key)
        {
            _AddInteraction(_Buttons.Add(button, key), EType.TButton);
        }

        public void AddSelectSlide(CSelectSlide slide, String key)
        {
            _AddInteraction(_SelectSlides.Add(slide, key), EType.TSelectSlide);
        }

        public void AddStatic(CStatic stat, String key)
        {
            _AddInteraction(_Statics.Add(stat, key), EType.TStatic);
        }

        public void AddText(CText text, String key)
        {
            _AddInteraction(_Texts.Add(text, key), EType.TText);
        }

        public void AddSongMenu(CSongMenu songmenu, String key)
        {
            _AddInteraction(_SongMenus.Add(songmenu, key), EType.TSongMenu);
        }

        public void AddLyric(CLyric lyric, String key)
        {
            _AddInteraction(_Lyrics.Add(lyric, key), EType.TLyric);
        }

        public void AddSingNote(CSingNotes sn, String key)
        {
            _AddInteraction(_SingNotes.Add(sn, key), EType.TSingNote);
        }

        public void AddNameSelection(CNameSelection ns, String key)
        {
            _AddInteraction(_NameSelections.Add(ns, key), EType.TNameSelection);
        }

        public void AddEqualizer(CEqualizer eq, String key)
        {
            _AddInteraction(_Equalizers.Add(eq, key), EType.TEqualizer);
        }

        public void AddPlaylist(CPlaylist pls, String key)
        {
            _AddInteraction(_Playlists.Add(pls, key), EType.TPlaylist);
        }

        public void AddParticleEffect(CParticleEffect pe, String key)
        {
            _AddInteraction(_ParticleEffects.Add(pe, key), EType.TParticleEffect);
        }

        public void AddScreenSetting(CScreenSetting se, String key)
        {
            _ScreenSettings.Add(se, key);
        }
        #endregion Elements

        #region InteractionHandling
        public void CheckInteraction()
        {
            if (!_IsEnabled(_Selection) || !_IsVisible(_Selection))
                PrevInteraction();
        }

        public void NextInteraction()
        {
            if (_Interactions.Count > 0)
                _NextInteraction();
        }

        public void PrevInteraction()
        {
            if (_Interactions.Count > 0)
                _PrevInteraction();
        }

        /// <summary>
        ///     Selects the next element in a menu Interaction.
        /// </summary>
        /// <returns>True if the next element is selected. False if either there is no next element or the Interaction does not provide such a method.</returns>
        public bool NextElement()
        {
            if (_Interactions.Count > 0)
                return _NextElement();

            return false;
        }

        /// <summary>
        ///     Selects the previous element in a menu Interaction.
        /// </summary>
        /// <returns>True if the previous element is selected. False if either there is no next element or the Interaction does not provide such a method.</returns>
        public bool PrevElement()
        {
            if (_Interactions.Count > 0)
                return _PrevElement();

            return false;
        }

        public bool SetInteractionToButton(CButton button)
        {
            for (int i = 0; i < _Interactions.Count; i++)
            {
                if (_Interactions[i].Type == EType.TButton)
                {
                    if (_Buttons[_Interactions[i].Num] == button)
                    {
                        _UnsetSelected();
                        _UnsetHighlighted(_Selection);
                        _Selection = i;
                        _SetSelected();
                        return true;
                    }
                }
            }
            return false;
        }

        public bool SetInteractionToSelectSlide(CSelectSlide slide)
        {
            for (int i = 0; i < _Interactions.Count; i++)
            {
                if (_Interactions[i].Type == EType.TSelectSlide)
                {
                    if (_SelectSlides[_Interactions[i].Num] == slide)
                    {
                        _UnsetSelected();
                        _UnsetHighlighted(_Selection);
                        _Selection = i;
                        _SetSelected();
                        return true;
                    }
                }
            }
            return false;
        }

        public void ProcessMouseClick(int x, int y)
        {
            if (_Selection >= _Interactions.Count || _Selection < 0)
                return;

            if (_Interactions[_Selection].Type == EType.TSelectSlide)
            {
                if (_SelectSlides[_Interactions[_Selection].Num].Visible)
                    _SelectSlides[_Interactions[_Selection].Num].ProcessMouseLBClick(x, y);
            }
        }

        public void ProcessMouseMove(int x, int y)
        {
            SelectByMouse(x, y);

            if (_Selection >= _Interactions.Count || _Selection < 0)
                return;

            if (_Interactions[_Selection].Type == EType.TSelectSlide)
            {
                if (_SelectSlides[_Interactions[_Selection].Num].Visible)
                    _SelectSlides[_Interactions[_Selection].Num].ProcessMouseMove(x, y);
            }
        }

        public void SelectByMouse(int x, int y)
        {
            float z = CBase.Settings.GetZFar();
            for (int i = 0; i < _Interactions.Count; i++)
            {
                if ((CBase.Settings.GetGameState() == EGameState.EditTheme) || (!_Interactions[i].ThemeEditorOnly && _IsVisible(i) && _IsEnabled(i)))
                {
                    if (_IsMouseOver(x, y, _Interactions[i]))
                    {
                        if (_GetZValue(_Interactions[i]) <= z)
                        {
                            z = _GetZValue(_Interactions[i]);
                            _UnsetSelected();
                            _Selection = i;
                            _SetSelected();
                        }
                    }
                }
            }
        }

        public bool IsMouseOver(MouseEvent MouseEvent)
        {
            return IsMouseOver(MouseEvent.X, MouseEvent.Y);
        }

        public bool IsMouseOver(int x, int y)
        {
            if (_Selection >= _Interactions.Count || _Selection < 0)
                return false;

            if (_Interactions.Count > 0)
                return _IsMouseOver(x, y, _Interactions[_Selection]);
            else
                return false;
        }

        private bool _IsMouseOver(int x, int y, CInteraction interact)
        {
            switch (interact.Type)
            {
                case EType.TButton:
                    if (CHelper.IsInBounds(_Buttons[interact.Num].Rect, x, y))
                        return true;
                    break;
                case EType.TSelectSlide:
                    if (CHelper.IsInBounds(_SelectSlides[interact.Num].Rect, x, y) ||
                        CHelper.IsInBounds(_SelectSlides[interact.Num].RectArrowLeft, x, y) ||
                        CHelper.IsInBounds(_SelectSlides[interact.Num].RectArrowRight, x, y))
                        return true;
                    break;
                case EType.TStatic:
                    if (CHelper.IsInBounds(_Statics[interact.Num].Rect, x, y))
                        return true;
                    break;
                case EType.TText:
                    RectangleF bounds = CBase.Drawing.GetTextBounds(_Texts[interact.Num]);
                    if (CHelper.IsInBounds(new SRectF(_Texts[interact.Num].X, _Texts[interact.Num].Y, bounds.Width, bounds.Height, _Texts[interact.Num].Z), x, y))
                        return true;
                    break;
                case EType.TSongMenu:
                    if (CHelper.IsInBounds(_SongMenus[interact.Num].Rect, x, y))
                        return true;
                    break;
                case EType.TLyric:
                    if (CHelper.IsInBounds(_Lyrics[interact.Num].Rect, x, y))
                        return true;
                    break;
                case EType.TNameSelection:
                    if (CHelper.IsInBounds(_NameSelections[interact.Num].Rect, x, y))
                        return true;
                    break;
                case EType.TEqualizer:
                    if (CHelper.IsInBounds(_Equalizers[interact.Num].Rect, x, y))
                        return true;
                    break;
                case EType.TPlaylist:
                    if (CHelper.IsInBounds(_Playlists[interact.Num].Rect, x, y))
                        return true;
                    break;
                case EType.TParticleEffect:
                    if (CHelper.IsInBounds(_ParticleEffects[interact.Num].Rect, x, y))
                        return true;
                    break;
            }
            return false;
        }

        private float _GetZValue(CInteraction interact)
        {
            switch (interact.Type)
            {
                case EType.TButton:
                    return _Buttons[interact.Num].Rect.Z;
                case EType.TSelectSlide:
                    return _SelectSlides[interact.Num].Rect.Z;
                case EType.TStatic:
                    return _Statics[interact.Num].Rect.Z;
                case EType.TSongMenu:
                    return _SongMenus[interact.Num].Rect.Z;
                case EType.TText:
                    return _Texts[interact.Num].Z;
                case EType.TLyric:
                    return _Lyrics[interact.Num].Rect.Z;
                case EType.TNameSelection:
                    return _NameSelections[interact.Num].Rect.Z;
                case EType.TEqualizer:
                    return _Equalizers[interact.Num].Rect.Z;
                case EType.TPlaylist:
                    return _Playlists[interact.Num].Rect.Z;
                case EType.TParticleEffect:
                    return _ParticleEffects[interact.Num].Rect.Z;
            }
            return CBase.Settings.GetZFar();
        }

        private float _GetZValue(int interaction)
        {
            return _GetZValue(_Interactions[interaction]);
        }

        private void _NextInteraction()
        {
            _UnsetSelected();
            if (CBase.Settings.GetGameState() != EGameState.EditTheme)
            {
                bool found = false;
                int start = _Selection;
                do
                {
                    start++;
                    if (start > _Interactions.Count - 1)
                        start = 0;

                    if ((start == _Selection) || (!_Interactions[start].ThemeEditorOnly && _IsVisible(start) && _IsEnabled(start)))
                        found = true;
                } while (!found);
                _Selection = start;
            }
            else
            {
                _Selection++;
                if (_Selection > _Interactions.Count - 1)
                    _Selection = 0;
            }
            _SetSelected();
        }

        private void _PrevInteraction()
        {
            _UnsetSelected();
            if (CBase.Settings.GetGameState() != EGameState.EditTheme)
            {
                bool found = false;
                int start = _Selection;
                do
                {
                    start--;
                    if (start < 0)
                        start = _Interactions.Count - 1;

                    if ((start == _Selection) || (!_Interactions[start].ThemeEditorOnly && _IsVisible(start) && _IsEnabled(start)))
                        found = true;
                } while (!found);
                _Selection = start;
            }
            else
            {
                _Selection--;
                if (_Selection < 0)
                    _Selection = _Interactions.Count - 1;
            }
            _SetSelected();
        }

        /// <summary>
        ///     Selects the next best interaction in a menu.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        private bool _NextInteraction(KeyEvent Key)
        {
            KeyEvent[] Directions = new KeyEvent[4];
            float[] Distances = new float[4];
            int[] stages = new int[4];
            int[] elements = new int[4];

            for (int i = 0; i < 4; i++)
                Directions[i] = new KeyEvent();

            Directions[0].Key = Keys.Up;
            Directions[1].Key = Keys.Right;
            Directions[2].Key = Keys.Down;
            Directions[3].Key = Keys.Left;

            for (int i = 0; i < 4; i++)
                elements[i] = _GetNextElement(Directions[i], out Distances[i], out stages[i]);

            int element = _Selection;
            int stage = int.MaxValue;
            float distance = float.MaxValue;
            int direction = -1;

            int mute = -1;
            switch (Key.Key)
            {
                case Keys.Up:
                    mute = 2;
                    break;
                case Keys.Right:
                    mute = 3;
                    break;
                case Keys.Down:
                    mute = 0;
                    break;
                case Keys.Left:
                    mute = 1;
                    break;
            }

            for (int i = 0; i < 4; i++)
            {
                if (i != mute && elements[i] != _Selection && (stages[i] <= stage && Directions[i].Key == Key.Key))
                {
                    stage = stages[i];
                    element = elements[i];
                    distance = Distances[i];
                    direction = i;
                }
            }

            if (direction != -1)
            {
                // select the new element
                if (Directions[direction].Key == Key.Key)
                {
                    _UnsetHighlighted(_Selection);
                    _UnsetSelected();

                    _Selection = element;
                    _SetSelected();
                    _SetHighlighted(_Selection);

                    return true;
                }
            }
            return false;
        }

        private int _GetNextElement(KeyEvent Key, out float Distance, out int Stage)
        {
            Distance = float.MaxValue;
            int min = _Selection;
            SRectF actualRect = _GetRect(_Selection);
            Stage = int.MaxValue;

            for (int i = 0; i < _Interactions.Count; i++)
            {
                if (i != _Selection && !_Interactions[i].ThemeEditorOnly && _IsVisible(i) && _IsEnabled(i))
                {
                    SRectF targetRect = _GetRect(i);
                    float dist = _GetDistanceDirect(Key, actualRect, targetRect);
                    if (dist >= 0f && dist < Distance)
                    {
                        Distance = dist;
                        min = i;
                        Stage = 10;
                    }
                }
            }

            if (min == _Selection)
            {
                for (int i = 0; i < _Interactions.Count; i++)
                {
                    if (i != _Selection && !_Interactions[i].ThemeEditorOnly && _IsVisible(i) && _IsEnabled(i))
                    {
                        SRectF targetRect = _GetRect(i);
                        float dist = _GetDistance180(Key, actualRect, targetRect);
                        if (dist >= 0f && dist < Distance)
                        {
                            Distance = dist;
                            min = i;
                            Stage = 20;
                        }
                    }
                }
            }

            if (min == _Selection)
            {
                switch (Key.Key)
                {
                    case Keys.Up:
                        actualRect = new SRectF(actualRect.X, CBase.Settings.GetRenderH(), 1, 1, actualRect.Z);
                        break;
                    case Keys.Down:
                        actualRect = new SRectF(actualRect.X, 0, 1, 1, actualRect.Z);
                        break;
                    case Keys.Left:
                        actualRect = new SRectF(CBase.Settings.GetRenderW(), actualRect.Y, 1, 1, actualRect.Z);
                        break;
                    case Keys.Right:
                        actualRect = new SRectF(0, actualRect.Y, 1, 1, actualRect.Z);
                        break;
                }

                for (int i = 0; i < _Interactions.Count; i++)
                {
                    if (i != _Selection && !_Interactions[i].ThemeEditorOnly && _IsVisible(i) && _IsEnabled(i))
                    {
                        SRectF targetRect = _GetRect(i);
                        float dist = _GetDistance180(Key, actualRect, targetRect);
                        if (dist >= 0f && dist < Distance)
                        {
                            Distance = dist;
                            min = i;
                            Stage = 30;
                        }
                    }
                }
            }
            return min;
        }

        private float _GetDistanceDirect(KeyEvent Key, SRectF actualRect, SRectF targetRect)
        {
            PointF source = new PointF(actualRect.X + actualRect.W / 2f, actualRect.Y + actualRect.H / 2f);
            PointF dest = new PointF(targetRect.X + targetRect.W / 2f, targetRect.Y + targetRect.H / 2f);

            PointF vector = new PointF(dest.X - source.X, dest.Y - source.Y);
            float distance = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            bool inDirection = false;
            switch (Key.Key)
            {
                case Keys.Up:
                    if (vector.Y < 0f && (targetRect.X + targetRect.W > actualRect.X && actualRect.X + actualRect.W > targetRect.X))
                        inDirection = true;
                    break;

                case Keys.Down:
                    if (vector.Y > 0f && (targetRect.X + targetRect.W > actualRect.X && actualRect.X + actualRect.W > targetRect.X))
                        inDirection = true;
                    break;

                case Keys.Left:
                    if (vector.X < 0f && (targetRect.Y + targetRect.H > actualRect.Y && actualRect.Y + actualRect.H > targetRect.Y))
                        inDirection = true;
                    break;

                case Keys.Right:
                    if (vector.X > 0f && (targetRect.Y + targetRect.H > actualRect.Y && actualRect.Y + actualRect.H > targetRect.Y))
                        inDirection = true;
                    break;
            }
            if (!inDirection)
                return float.MaxValue;
            else
                return distance;
        }

        private float _GetDistance180(KeyEvent Key, SRectF actualRect, SRectF targetRect)
        {
            PointF source = new PointF(actualRect.X + actualRect.W / 2f, actualRect.Y + actualRect.H / 2f);
            PointF dest = new PointF(targetRect.X + targetRect.W / 2f, targetRect.Y + targetRect.H / 2f);

            PointF vector = new PointF(dest.X - source.X, dest.Y - source.Y);
            float distance = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            bool inDirection = false;
            switch (Key.Key)
            {
                case Keys.Up:
                    if (vector.Y < 0f)
                        inDirection = true;
                    break;

                case Keys.Down:
                    if (vector.Y > 0f)
                        inDirection = true;
                    break;

                case Keys.Left:
                    if (vector.X < 0f)
                        inDirection = true;
                    break;

                case Keys.Right:
                    if (vector.X > 0f)
                        inDirection = true;
                    break;
            }
            if (!inDirection)
                return float.MaxValue;
            else
                return distance;
        }

        /// <summary>
        ///     Selects the next element in a menu interaction.
        /// </summary>
        /// <returns>True if the next element is selected. False if either there is no next element or the interaction does not provide such a method.</returns>
        private bool _NextElement()
        {
            if (_Interactions[_Selection].Type == EType.TSelectSlide)
                return _SelectSlides[_Interactions[_Selection].Num].NextValue();

            return false;
        }

        /// <summary>
        ///     Selects the previous element in a menu interaction.
        /// </summary>
        /// <returns>
        ///     True if the previous element is selected. False if either there is no previous element or the interaction does not provide such a method.
        /// </returns>
        private bool _PrevElement()
        {
            if (_Interactions[_Selection].Type == EType.TSelectSlide)
                return _SelectSlides[_Interactions[_Selection].Num].PrevValue();

            return false;
        }

        private void _SetSelected()
        {
            switch (_Interactions[_Selection].Type)
            {
                case EType.TButton:
                    _Buttons[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.TSelectSlide:
                    _SelectSlides[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.TStatic:
                    _Statics[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.TText:
                    _Texts[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.TSongMenu:
                    _SongMenus[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.TLyric:
                    _Lyrics[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.TNameSelection:
                    _NameSelections[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.TEqualizer:
                    _Equalizers[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.TPlaylist:
                    _Playlists[_Interactions[_Selection].Num].Selected = true;
                    break;
                case EType.TParticleEffect:
                    _ParticleEffects[_Interactions[_Selection].Num].Selected = true;
                    break;
            }
        }

        private void _UnsetSelected()
        {
            switch (_Interactions[_Selection].Type)
            {
                case EType.TButton:
                    _Buttons[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.TSelectSlide:
                    _SelectSlides[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.TStatic:
                    _Statics[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.TText:
                    _Texts[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.TSongMenu:
                    _SongMenus[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.TLyric:
                    _Lyrics[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.TNameSelection:
                    _NameSelections[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.TEqualizer:
                    _Equalizers[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.TPlaylist:
                    _Playlists[_Interactions[_Selection].Num].Selected = false;
                    break;
                case EType.TParticleEffect:
                    _ParticleEffects[_Interactions[_Selection].Num].Selected = false;
                    break;
            }
        }

        private void _SetHighlighted(int selection)
        {
            if (selection >= _Interactions.Count || selection < 0)
                return;

            switch (_Interactions[selection].Type)
            {
                case EType.TButton:
                    //_Buttons[_Interactions[selection].Num].Selected = true;
                    break;
                case EType.TSelectSlide:
                    _SelectSlides[_Interactions[selection].Num].Highlighted = true;
                    break;
                case EType.TStatic:
                    //_Statics[_Interactions[selection].Num].Selected = true;
                    break;
                case EType.TText:
                    //_Texts[_Interactions[selection].Num].Selected = true;
                    break;
                case EType.TSongMenu:
                    //_SongMenus[_Interactions[selection].Num].Selected = true;
                    break;
                case EType.TLyric:
                    //_Lyrics[_Interactions[selection].Num].Selected = true;
                    break;
                case EType.TNameSelection:
                    //_NameSelections[_Interactions[selection].Num].Selected = true;
                    break;
                case EType.TPlaylist:
                    //_Playlists[_Interactions[_Selection].Num].Selected = true;
                    break;
            }
        }

        private void _UnsetHighlighted(int selection)
        {
            if (selection >= _Interactions.Count || selection < 0)
                return;

            switch (_Interactions[selection].Type)
            {
                case EType.TButton:
                    //_Buttons[_Interactions[selection].Num].Selected = false;
                    break;
                case EType.TSelectSlide:
                    _SelectSlides[_Interactions[selection].Num].Highlighted = false;
                    break;
                case EType.TStatic:
                    //_Statics[_Interactions[selection].Num].Selected = false;
                    break;
                case EType.TText:
                    //_Texts[_Interactions[selection].Num].Selected = false;
                    break;
                case EType.TSongMenu:
                    //_SongMenus[_Interactions[selection].Num].Selected = false;
                    break;
                case EType.TLyric:
                    //_Lyrics[_Interactions[selection].Num].Selected = false;
                    break;
                case EType.TNameSelection:
                    //_NameSelections[_Interactions[selection].Num].Selected = false;
                    break;
                case EType.TPlaylist:
                    //_Playlists[_Interactions[_Selection].Num].Selected = true;
                    break;
            }
        }

        private void _ToggleHighlighted()
        {
            if (_Selection >= _Interactions.Count || _Selection < 0)
                return;

            if (_Interactions[_Selection].Type == EType.TSelectSlide)
                _SelectSlides[_Interactions[_Selection].Num].Highlighted = !_SelectSlides[_Interactions[_Selection].Num].Highlighted;
        }

        private bool _IsHighlighted()
        {
            if (_Selection >= _Interactions.Count || _Selection < 0)
                return false;

            if (_Interactions[_Selection].Type == EType.TSelectSlide)
                return _SelectSlides[_Interactions[_Selection].Num].Highlighted;

            return false;
        }

        private bool _IsVisible(int interaction)
        {
            if (_Selection >= _Interactions.Count || _Selection < 0)
                return false;

            switch (_Interactions[interaction].Type)
            {
                case EType.TButton:
                    return _Buttons[_Interactions[interaction].Num].Visible;

                case EType.TSelectSlide:
                    return _SelectSlides[_Interactions[interaction].Num].Visible;

                case EType.TStatic:
                    return _Statics[_Interactions[interaction].Num].Visible;

                case EType.TText:
                    return _Texts[_Interactions[interaction].Num].Visible;

                case EType.TSongMenu:
                    return _SongMenus[_Interactions[interaction].Num].Visible;

                case EType.TLyric:
                    return _Lyrics[_Interactions[interaction].Num].Visible;

                case EType.TNameSelection:
                    return _NameSelections[_Interactions[interaction].Num].Visible;

                case EType.TEqualizer:
                    return _Equalizers[_Interactions[interaction].Num].Visible;

                case EType.TPlaylist:
                    return _Playlists[_Interactions[interaction].Num].Visible;

                case EType.TParticleEffect:
                    return _ParticleEffects[_Interactions[interaction].Num].Visible;
            }

            return false;
        }

        private bool _IsEnabled(int interaction)
        {
            if (_Selection >= _Interactions.Count || _Selection < 0)
                return false;

            switch (_Interactions[interaction].Type)
            {
                case EType.TButton:
                    return _Buttons[_Interactions[interaction].Num].Enabled;

                case EType.TSelectSlide:
                    return _SelectSlides[_Interactions[interaction].Num].Visible;

                case EType.TStatic:
                    return false; //_Statics[_Interactions[interaction].Num].Visible;

                case EType.TText:
                    return false; //_Texts[_Interactions[interaction].Num].Visible;

                case EType.TSongMenu:
                    return _SongMenus[_Interactions[interaction].Num].Visible;

                case EType.TLyric:
                    return false; //_Lyrics[_Interactions[interaction].Num].Visible;

                case EType.TNameSelection:
                    return _NameSelections[_Interactions[interaction].Num].Visible;

                case EType.TEqualizer:
                    return false; //_Equalizers[_Interactions[interaction].Num].Visible;

                case EType.TPlaylist:
                    return _Playlists[_Interactions[interaction].Num].Visible;

                case EType.TParticleEffect:
                    return false; //_ParticleEffects[_Interactions[interaction].Num].Visible;
            }

            return false;
        }

        private SRectF _GetRect(int interaction)
        {
            SRectF Result = new SRectF();
            switch (_Interactions[interaction].Type)
            {
                case EType.TButton:
                    return _Buttons[_Interactions[interaction].Num].Rect;

                case EType.TSelectSlide:
                    return _SelectSlides[_Interactions[interaction].Num].Rect;

                case EType.TStatic:
                    return _Statics[_Interactions[interaction].Num].Rect;

                case EType.TText:
                    return _Texts[_Interactions[interaction].Num].Bounds;

                case EType.TSongMenu:
                    return _SongMenus[_Interactions[interaction].Num].Rect;

                case EType.TLyric:
                    return _Lyrics[_Interactions[interaction].Num].Rect;

                case EType.TNameSelection:
                    return _NameSelections[_Interactions[interaction].Num].Rect;

                case EType.TEqualizer:
                    return _Equalizers[_Interactions[interaction].Num].Rect;

                case EType.TPlaylist:
                    return _Playlists[_Interactions[interaction].Num].Rect;

                case EType.TParticleEffect:
                    return _ParticleEffects[_Interactions[interaction].Num].Rect;
            }

            return Result;
        }

        private void _AddInteraction(int num, EType type)
        {
            _Interactions.Add(new CInteraction(num, type));
            if (!_Interactions[_Selection].ThemeEditorOnly)
                _SetSelected();
            else
                _NextInteraction();
        }

        private void _DrawInteraction(int interaction)
        {
            switch (_Interactions[interaction].Type)
            {
                case EType.TButton:
                    _Buttons[_Interactions[interaction].Num].Draw();
                    break;

                case EType.TSelectSlide:
                    _SelectSlides[_Interactions[interaction].Num].Draw();
                    break;

                case EType.TStatic:
                    _Statics[_Interactions[interaction].Num].Draw();
                    break;

                case EType.TText:
                    _Texts[_Interactions[interaction].Num].Draw();
                    break;

                case EType.TSongMenu:
                    _SongMenus[_Interactions[interaction].Num].Draw();
                    break;

                case EType.TNameSelection:
                    _NameSelections[_Interactions[interaction].Num].Draw();
                    break;

                case EType.TEqualizer:
                    if (!_Equalizers[_Interactions[interaction].Num].ScreenHandles)
                    {
                        //TODO:
                        //Call Update-Method of Equalizer and give infos about bg-sound.
                        //_Equalizers[_Interactions[interaction].Num].Draw();
                    }
                    break;

                case EType.TPlaylist:
                    _Playlists[_Interactions[interaction].Num].Draw();
                    break;

                case EType.TParticleEffect:
                    _ParticleEffects[_Interactions[interaction].Num].Draw();
                    break;

                    //TODO:
                    //case EType.TLyric:
                    //    _Lyrics[_Interactions[interaction].Num].Draw(0);
                    //    break;
            }
        }
        #endregion InteractionHandling

        #region Theme Handling
        private void MoveElement(int stepX, int stepY)
        {
            if (_Interactions.Count > 0)
            {
                switch (_Interactions[_Selection].Type)
                {
                    case EType.TButton:
                        _Buttons[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                        break;

                    case EType.TSelectSlide:
                        _SelectSlides[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                        break;

                    case EType.TStatic:
                        _Statics[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                        break;

                    case EType.TText:
                        _Texts[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                        break;

                    case EType.TSongMenu:
                        _SongMenus[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                        break;

                    case EType.TLyric:
                        _Lyrics[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                        break;

                    case EType.TNameSelection:
                        _NameSelections[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                        break;

                    case EType.TEqualizer:
                        _Equalizers[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                        break;

                    case EType.TPlaylist:
                        _Playlists[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                        break;

                    case EType.TParticleEffect:
                        _ParticleEffects[_Interactions[_Selection].Num].MoveElement(stepX, stepY);
                        break;
                }
            }
        }

        private void ResizeElement(int stepW, int stepH)
        {
            if (_Interactions.Count > 0)
            {
                switch (_Interactions[_Selection].Type)
                {
                    case EType.TButton:
                        _Buttons[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                        break;

                    case EType.TSelectSlide:
                        _SelectSlides[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                        break;

                    case EType.TStatic:
                        _Statics[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                        break;

                    case EType.TText:
                        _Texts[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                        break;

                    case EType.TSongMenu:
                        _SongMenus[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                        break;

                    case EType.TLyric:
                        _Lyrics[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                        break;

                    case EType.TNameSelection:
                        _NameSelections[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                        break;

                    case EType.TEqualizer:
                        _Equalizers[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                        break;

                    case EType.TPlaylist:
                        _Playlists[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                        break;

                    case EType.TParticleEffect:
                        _ParticleEffects[_Interactions[_Selection].Num].ResizeElement(stepW, stepH);
                        break;
                }
            }
        }

        private bool CheckVersion(int reqVersion, CXMLReader xmlReader)
        {
            int actualVersion = 0;
            xmlReader.TryGetIntValue("//root/" + ThemeName + "/ScreenVersion", ref actualVersion);

            if (actualVersion == reqVersion)
                return true;
            else
            {
                string msg = "Can't load screen file of screen \"" + ThemeName + "\", ";
                if (actualVersion < reqVersion)
                    msg += "the file ist outdated! ";
                else
                    msg += "the file is for newer program versions! ";

                msg += "Current screen version is " + reqVersion.ToString();
                CBase.Log.LogError(msg);
            }
            return false;
        }

        private void LoadThemeBasics(CXMLReader xmlReader, int SkinIndex)
        {
            string value = String.Empty;

            // Backgrounds
            CBackground background = new CBackground(_PartyModeID);
            int i = 1;
            while (background.LoadTheme("//root/" + ThemeName, "Background" + i.ToString(), xmlReader, SkinIndex))
            {
                AddBackground(background);
                background = new CBackground(_PartyModeID);
                i++;
            }

            // Statics
            CStatic stat = new CStatic(_PartyModeID);
            i = 1;
            while (stat.LoadTheme("//root/" + ThemeName, "Static" + i.ToString(), xmlReader, SkinIndex))
            {
                AddStatic(stat);
                stat = new CStatic(_PartyModeID);
                i++;
            }

            // Texts
            CText text = new CText(_PartyModeID);
            i = 1;
            while (text.LoadTheme("//root/" + ThemeName, "Text" + i.ToString(), xmlReader, SkinIndex))
            {
                AddText(text);
                text = new CText(_PartyModeID);
                i++;
            }

            // ParticleEffects
            CParticleEffect partef = new CParticleEffect(_PartyModeID);
            i = 1;
            while (partef.LoadTheme("//root/" + ThemeName, "ParticleEffect" + i.ToString(), xmlReader, SkinIndex))
            {
                AddParticleEffect(partef);
                partef = new CParticleEffect(_PartyModeID);
                i++;
            }
        }

        private void ReloadThemeEditMode()
        {
            CBase.Theme.UnloadSkins();
            CBase.Theme.ListSkins();
            CBase.Theme.LoadSkins();
            CBase.Theme.LoadTheme();
            CBase.Graphics.ReloadTheme();

            OnShow();
            OnShowFinish();
        }
        #endregion Theme Handling
    }
}