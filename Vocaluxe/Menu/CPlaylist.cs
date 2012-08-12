using System;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.GameModes;

namespace Vocaluxe.Menu
{

    struct SThemePlaylist
    {
        public string Name;

        public string ColorBackgroundName;
        public string SColorBackgroundName;

        public string TextureBackgroundName;
        public string STextureBackgroundName;

        public float EntryHeight;

        public string StringText1;
        public string StringText2;
        public string StringText3;

        public CText Text1;
        public CText Text2;
        public CText Text3;
        public CText TextPlaylistHeader;

        public CStatic StaticCover;
        public CStatic StaticPlaylistHeader;
        public CStatic StaticPlaylistFooter;

        public CButton ButtonPlaylistClose;
        public CButton ButtonPlaylistSave;
        public CButton ButtonPlaylistDelete;
        public CButton ButtonPlaylistSing;

        public CSelectSlide SelectSlideGameMode;
    }

    class CPlaylist : IMenuElement
    {
        class PlaylistElementContent
        {
            public EGameMode[] Modes;
            public int SongID;
            public EGameMode Mode;
        }

        class PlaylistElement
        {
            public CStatic Cover;
            public CStatic Background;
            public CText Text1;
            public CText Text2;
            public CText Text3;
            public CSelectSlide SelectSlide;
            public int Content;
        }

        private SThemePlaylist _Theme;
        private bool _ThemeLoaded;

        private List<PlaylistElement> PlaylistElements;
        private List<PlaylistElementContent> PlaylistElementContents;

        public SRectF Rect;
        public SColorF BackgroundColor;
        public SColorF BackgroundSColor;

        public bool Visible;

        private bool _Selected;
        public bool Selected
        {
            get { return _Selected; }
            set
            {
                _Selected = value;
                if (CurrentPlaylistElement == -1 && _Selected && PlaylistElementContents.Count > 0)
                {
                    CurrentPlaylistElement = 0;
                    ActiveHover = CurrentPlaylistElement;
                }
            }
        }

        public bool changeOrder = false;
        public int ActivePlaylistID = -1;
        public int Offset = 0;
        public int ActiveHover = -1;
        public int CurrentPlaylistElement = -1;

        public CPlaylist()
        {
            _Theme = new SThemePlaylist();
            _Theme.Text1 = new CText();
            _Theme.Text2 = new CText();
            _Theme.Text3 = new CText();
            _Theme.TextPlaylistHeader = new CText();
            _Theme.StaticCover = new CStatic();
            _Theme.StaticPlaylistFooter = new CStatic();
            _Theme.StaticPlaylistHeader = new CStatic();
            _Theme.ButtonPlaylistClose = new CButton();
            _Theme.ButtonPlaylistDelete = new CButton();
            _Theme.ButtonPlaylistSave = new CButton();
            _Theme.ButtonPlaylistSing = new CButton();
            _Theme.SelectSlideGameMode = new CSelectSlide();

            Rect = new SRectF();
            BackgroundColor = new SColorF();
            BackgroundSColor = new SColorF();

            PlaylistElements = new List<PlaylistElement>();
            PlaylistElementContents = new List<PlaylistElementContent>();

            Visible = false;
            Selected = false;
        }

        public void Init()
        {
            PrepareList();
        }

        public bool LoadTheme(string XmlPath, string ElementName, XPathNavigator navigator, int SkinIndex)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinBackground", navigator, ref _Theme.TextureBackgroundName, String.Empty);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinBackgroundSelected", navigator, ref _Theme.STextureBackgroundName, String.Empty);

            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/X", navigator, ref Rect.X);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Y", navigator, ref Rect.Y);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Z", navigator, ref Rect.Z);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/W", navigator, ref Rect.W);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/H", navigator, ref Rect.H);
            if (CHelper.GetValueFromXML(item + "/ColorBackground", navigator, ref _Theme.ColorBackgroundName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.ColorBackgroundName, SkinIndex, ref BackgroundColor);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/BackgroundR", navigator, ref BackgroundColor.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/BackgroundG", navigator, ref BackgroundColor.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/BackgroundB", navigator, ref BackgroundColor.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/BackgroundA", navigator, ref BackgroundColor.A);
            }
            if (CHelper.GetValueFromXML(item + "/SColorBackground", navigator, ref _Theme.SColorBackgroundName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.SColorBackgroundName, SkinIndex, ref BackgroundSColor);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SBackgroundR", navigator, ref BackgroundSColor.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SBackgroundG", navigator, ref BackgroundSColor.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SBackgroundB", navigator, ref BackgroundSColor.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SBackgroundA", navigator, ref BackgroundSColor.A);
            }

            _ThemeLoaded &= _Theme.Text1.LoadTheme(item, "TextPart1", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.Text2.LoadTheme(item, "TextPart2", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.Text2.LoadTheme(item, "TextPart3", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.TextPlaylistHeader.LoadTheme(item, "TextPlaylistHeader", navigator, SkinIndex);
            _Theme.StringText1 = _Theme.Text1.Text;
            _Theme.StringText2 = _Theme.Text2.Text;
            _Theme.StringText3 = _Theme.Text3.Text;

            _ThemeLoaded &= _Theme.StaticCover.LoadTheme(item, "StaticCover", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.StaticPlaylistHeader.LoadTheme(item, "StaticPlaylistHeader", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.StaticPlaylistFooter.LoadTheme(item, "StaticPlaylistFooter", navigator, SkinIndex);

            _ThemeLoaded &= _Theme.ButtonPlaylistSing.LoadTheme(item, "ButtonPlaylistSing", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.ButtonPlaylistClose.LoadTheme(item, "ButtonPlaylistClose", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.ButtonPlaylistSave.LoadTheme(item, "ButtonPlaylistSave", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.ButtonPlaylistDelete.LoadTheme(item, "ButtonPlaylistDelete", navigator, SkinIndex);

            _ThemeLoaded &= _Theme.SelectSlideGameMode.LoadTheme(item, "SelectSlideGameMode", navigator, SkinIndex);

            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/EntryHeight", navigator, ref _Theme.EntryHeight);
            if (_ThemeLoaded)
            {
                _Theme.Name = ElementName;
                LoadTextures();
            }
            return _ThemeLoaded;
        }

        public bool SaveTheme(XmlWriter writer)
        {
            if (_ThemeLoaded)
            {
                writer.WriteStartElement(_Theme.Name);

                writer.WriteComment("<X>, <Y>, <Z>, <W>, <H>: Playlist position, width and height");
                writer.WriteElementString("X", Rect.X.ToString("#0"));
                writer.WriteElementString("Y", Rect.Y.ToString("#0"));
                writer.WriteElementString("Z", Rect.Z.ToString("#0.00"));
                writer.WriteElementString("W", Rect.W.ToString("#0"));
                writer.WriteElementString("H", Rect.H.ToString("#0"));

                writer.WriteElementString("EntryHeight", _Theme.EntryHeight.ToString("#0.00"));

                writer.WriteComment("<SkinBackground>: Texture name");
                writer.WriteElementString("SkinBackground", _Theme.TextureBackgroundName);

                writer.WriteComment("<SkinBackgroundSelected>: Texture name for selected playlist-entry");
                writer.WriteElementString("SkinBackgroundSelected", _Theme.STextureBackgroundName);

                writer.WriteComment("<ColorBackground>: Button color from ColorScheme (high priority)");
                writer.WriteComment("or <BackgroundR>, <BackgroundG>, <BackgroundB>, <BackgroundA> (lower priority)");
                if (_Theme.ColorBackgroundName != String.Empty)
                {
                    writer.WriteElementString("BackgroundColor", _Theme.ColorBackgroundName);
                }
                else
                {
                    writer.WriteElementString("BackgroundR", BackgroundColor.R.ToString("#0.00"));
                    writer.WriteElementString("BackgroundG", BackgroundColor.G.ToString("#0.00"));
                    writer.WriteElementString("BackgroundB", BackgroundColor.B.ToString("#0.00"));
                    writer.WriteElementString("BackgroundA", BackgroundColor.A.ToString("#0.00"));
                }

                writer.WriteComment("<SColorBackground>: Selected paylist-entry color from ColorScheme (high priority)");
                writer.WriteComment("or <SBackgroundR>, <SBackgroundG>, <SBackgroundB>, <SBackgroundA> (lower priority)");
                if (_Theme.SColorBackgroundName != String.Empty)
                {
                    writer.WriteElementString("SColor", _Theme.SColorBackgroundName);
                }
                else
                {
                    writer.WriteElementString("SR", BackgroundSColor.R.ToString("#0.00"));
                    writer.WriteElementString("SG", BackgroundSColor.G.ToString("#0.00"));
                    writer.WriteElementString("SB", BackgroundSColor.B.ToString("#0.00"));
                    writer.WriteElementString("SA", BackgroundSColor.A.ToString("#0.00"));
                }

                writer.WriteComment("Positions of <TextPart1>, <TextPart2>, <TextPart3>, <StaticCover> and <SelectSlideGameMode> are relative to playlist-entry!");
                writer.WriteComment("Use placeholders for Text of <TextPart1>, <TextPart2> and <TextPart3>: %t, %a, %l");
                _Theme.Text1.SaveTheme(writer);
                _Theme.Text2.SaveTheme(writer);
                _Theme.Text3.SaveTheme(writer);
                _Theme.StaticCover.SaveTheme(writer);
                _Theme.SelectSlideGameMode.SaveTheme(writer);

                writer.WriteEndElement();

                return true;
            }
            return false;
        }

        public void Draw()
        {
            Draw(false);
        }

        public void ForceDraw()
        {
            Draw(true);
        }

        public void Draw(bool ForceDraw)
        {
            if (PlaylistElements.Count <= 0)
            {
                LoadPlaylist(0);
            }
            if (!Visible && CSettings.GameState != EGameState.EditTheme && !ForceDraw)
                return;

            for (int i = 0; i < PlaylistElements.Count; i++ )
            {
                if (i == ActiveHover && _Selected)
                {
                    PlaylistElements[i].Background.Texture = CTheme.GetSkinTexture(_Theme.STextureBackgroundName);
                    PlaylistElements[i].Background.Color = BackgroundSColor;
                }
                else
                {
                    PlaylistElements[i].Background.Texture = CTheme.GetSkinTexture(_Theme.TextureBackgroundName);
                    PlaylistElements[i].Background.Color = BackgroundColor;
                }
                PlaylistElements[i].Background.Draw();
                PlaylistElements[i].Cover.Draw();
                PlaylistElements[i].Text1.Draw();
                PlaylistElements[i].Text2.Draw();
                PlaylistElements[i].Text3.Draw();
                PlaylistElements[i].SelectSlide.Draw();
            }
            _Theme.ButtonPlaylistSing.Draw();
            _Theme.ButtonPlaylistSave.Draw();
            _Theme.ButtonPlaylistDelete.Draw();
            _Theme.ButtonPlaylistClose.Draw();
            _Theme.StaticPlaylistHeader.Draw();
            _Theme.StaticPlaylistFooter.Draw();
            _Theme.TextPlaylistHeader.Draw();
        }

        public void UnloadTextures()
        {
        }

        public void LoadTextures()
        {
            _Theme.Text1.LoadTextures();
            _Theme.Text2.LoadTextures();
            _Theme.Text3.LoadTextures();

            if (_Theme.ColorBackgroundName != String.Empty)
                BackgroundColor = CTheme.GetColor(_Theme.ColorBackgroundName);

            if (_Theme.SColorBackgroundName != String.Empty)
                BackgroundSColor = CTheme.GetColor(_Theme.SColorBackgroundName);
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        public bool HandleInput(KeyEvent kevent)
        {
            if (Selected)
            {
                switch (kevent.Key)
                {
                    case Keys.Up:
                        if (CurrentPlaylistElement == -1 && PlaylistElementContents.Count > 0)
                        {
                            CurrentPlaylistElement = 0;
                            ActiveHover = CurrentPlaylistElement;
                            return true;
                        }
                        else if (CurrentPlaylistElement - 1 > 0)
                        {
                            CurrentPlaylistElement--;
                            ActiveHover = CurrentPlaylistElement;
                            return true;
                        }
                        else if (CurrentPlaylistElement - 1 == 0)
                        {
                            CurrentPlaylistElement--;
                            if (Offset > 0)
                            {
                                Offset--;
                                Update();
                            }
                            ActiveHover = CurrentPlaylistElement;
                            return true;
                        }
                        return false;

                    case Keys.Down:
                        if (CurrentPlaylistElement == -1 && PlaylistElementContents.Count > 0)
                        {
                            CurrentPlaylistElement = 0;
                            ActiveHover = CurrentPlaylistElement;
                            return true;
                        }
                        else if (CurrentPlaylistElement + 1 < PlaylistElements.Count)
                        {
                            if (PlaylistElements[CurrentPlaylistElement + 1].Content != -1)
                            {
                                CurrentPlaylistElement++;
                                ActiveHover = CurrentPlaylistElement;
                                return true;
                            }
                        }
                        else if (CurrentPlaylistElement + 1 >= PlaylistElements.Count && CurrentPlaylistElement + Offset + 1 < PlaylistElementContents.Count)
                        {
                            if (PlaylistElementContents[CurrentPlaylistElement + Offset + 1].SongID != -1)
                            {
                                Offset++;
                                Update();
                                ActiveHover = CurrentPlaylistElement;
                                return true;
                            }
                        }
                        return false;

                    case Keys.Delete:
                        if (CurrentPlaylistElement != -1)
                        {
                            CPlaylists.Playlists[ActivePlaylistID].DeleteSong(PlaylistElements[CurrentPlaylistElement].Content);
                            UpdatePlaylist();
                            return true;
                        }
                        return false;

                    case Keys.Back:
                        ClosePlaylist();
                        break;

                    case Keys.Enter:
                        StartPlaylistSongs();
                        break;

                    case Keys.PageUp:
                        if (CurrentPlaylistElement != -1)
                        {
                            CPlaylists.Playlists[ActivePlaylistID].SongDown(CurrentPlaylistElement+Offset);
                            if (CurrentPlaylistElement - 1 > -1)
                            {
                                CurrentPlaylistElement--;
                                ActiveHover = CurrentPlaylistElement;
                            }
                            else if(Offset > 0)
                            {
                                Offset--;
                            }
                            UpdatePlaylist();
                            return true;
                        }
                        break;

                    case Keys.PageDown:
                        if (CurrentPlaylistElement != -1)
                        {
                            CPlaylists.Playlists[ActivePlaylistID].SongUp(CurrentPlaylistElement+Offset);
                            if (CurrentPlaylistElement + 1 < PlaylistElements.Count)
                            {
                                CurrentPlaylistElement++;
                                ActiveHover = CurrentPlaylistElement;
                            }
                            else if (Offset + CurrentPlaylistElement < PlaylistElementContents.Count) 
                            {
                                Offset++;
                            }
                            UpdatePlaylist();
                            return true;
                        }
                        break;

                    case Keys.Left:
                        if (CurrentPlaylistElement != -1)
                        {
                            int oldValue = PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection;
                            PlaylistElements[CurrentPlaylistElement].SelectSlide.PrevValue();
                            if (oldValue != PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection)
                                CPlaylists.Playlists[ActivePlaylistID].Songs[CurrentPlaylistElement + Offset].GameMode = PlaylistElementContents[CurrentPlaylistElement + Offset].Modes[PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection];
                            return true;
                        }
                        return false;

                    case Keys.Right:
                        if (CurrentPlaylistElement != -1)
                        {
                            int oldValue = PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection;
                            PlaylistElements[CurrentPlaylistElement].SelectSlide.NextValue();
                            if (oldValue != PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection)
                                CPlaylists.Playlists[ActivePlaylistID].Songs[CurrentPlaylistElement + Offset].GameMode = PlaylistElementContents[CurrentPlaylistElement + Offset].Modes[PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection];
                            return true;
                        }
                        return false;
                }
            }
            return false;
        }

        public bool HandleMouse(MouseEvent mevent)
        {
            if (CHelper.IsInBounds(Rect, mevent) && Visible)
            {
                if (mevent.Wheel > 0)
                {
                    if (PlaylistElements.Count + Offset + mevent.Wheel <= PlaylistElementContents.Count)
                    {
                        Offset = Offset + mevent.Wheel;
                        Update();
                        return true;
                    }
                    return true;
                }
                else if (mevent.Wheel < 0)
                {
                    if (Offset + mevent.Wheel >= 0)
                    {
                        Offset = Offset + mevent.Wheel;
                        Update();
                        return true;
                    }
                }
                if (mevent.LB)
                {
                    if (CurrentPlaylistElement != -1)
                    {
                        int oldValue = PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection;
                        PlaylistElements[CurrentPlaylistElement].SelectSlide.ProcessMouseLBClick(mevent.X, mevent.Y);
                        if (oldValue != PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection)
                            CPlaylists.Playlists[ActivePlaylistID].Songs[CurrentPlaylistElement + Offset].GameMode = PlaylistElementContents[CurrentPlaylistElement + Offset].Modes[PlaylistElements[CurrentPlaylistElement].SelectSlide.Selection];
                        return true;
                    }
                }

                for (int i = 0; i < PlaylistElements.Count; i++)
                {
                    //Hover for playlist-element
                    if (PlaylistElementContents.Count - 1 >= i && CHelper.IsInBounds(PlaylistElements[i].Background.Rect, mevent))
                    {
                        PlaylistElements[i].Background.Texture = CTheme.GetSkinTexture(_Theme.STextureBackgroundName);
                        PlaylistElements[i].Background.Color = BackgroundSColor;
                        ActiveHover = i;
                        CurrentPlaylistElement = i;
                    }
                    else
                    {
                        PlaylistElements[i].Background.Texture = CTheme.GetSkinTexture(_Theme.TextureBackgroundName);
                        PlaylistElements[i].Background.Color = BackgroundColor;
                    }
                    //Hover for SelectSlide-Arrow
                    PlaylistElements[i].SelectSlide.ProcessMouseMove(mevent.X, mevent.Y);
                    //Delete Entry with RB
                    if (CHelper.IsInBounds(PlaylistElements[i].Background.Rect, mevent) && mevent.RB && PlaylistElements[i].Content != -1)
                    {
                        CPlaylists.Playlists[ActivePlaylistID].DeleteSong(PlaylistElements[i].Content);
                        UpdatePlaylist();
                        return true;
                    }
                    //Change order with holding LB
                    else if (CHelper.IsInBounds(PlaylistElements[i].Background.Rect, mevent) && mevent.LBH && PlaylistElements[i].Content != -1 && !changeOrder)
                    {
                        changeOrder = true;
                        CurrentPlaylistElement = i;
                    }
                    else if (CHelper.IsInBounds(PlaylistElements[i].Background.Rect, mevent) && mevent.LBH && PlaylistElements[i].Content != -1 && changeOrder)
                    {
                        /*
                        if (PlaylistElements.Count < CurrentPlaylistElement + 1)
                        {
                        }
                        else if (PlaylistElementContents.Count < CurrentPlaylistElement + 1 + Offset)
                        {
                        }
                        if (mevent.Y < PlaylistElements[CurrentPlaylistElement + 1].Background.Rect.Y)
                        {
                        }
                         */
                    }
                }
            }
            else 
            {
                if (ActiveHover != -1)
                {
                    ActiveHover = -1;
                    CurrentPlaylistElement = -1;
                }
            }

            if (ActivePlaylistID > -1)
            {
                _Theme.ButtonPlaylistClose.ProcessMouseMove(mevent.X, mevent.Y);
                _Theme.ButtonPlaylistSing.ProcessMouseMove(mevent.X, mevent.Y);
                _Theme.ButtonPlaylistSave.ProcessMouseMove(mevent.X, mevent.Y);
                _Theme.ButtonPlaylistDelete.ProcessMouseMove(mevent.X, mevent.Y);
                if (mevent.LB)
                {
                    if (CHelper.IsInBounds(_Theme.ButtonPlaylistClose.Rect, mevent))
                    {
                        ClosePlaylist();
                    }
                    else if (CHelper.IsInBounds(_Theme.ButtonPlaylistSing.Rect, mevent))
                    {
                        StartPlaylistSongs();
                    }
                    else if (CHelper.IsInBounds(_Theme.ButtonPlaylistSave.Rect, mevent))
                    {
                        CPlaylists.SavePlaylist(ActivePlaylistID);
                    }
                    else if (CHelper.IsInBounds(_Theme.ButtonPlaylistDelete.Rect, mevent))
                    {
                        CPlaylists.DeletePlaylist(ActivePlaylistID);
                        ClosePlaylist();
                    }
                }
            }
            return false;
        }

        private void PrepareList()
        {
            PlaylistElements.Clear();
            for (int i = 0; i < Math.Floor(Rect.H / _Theme.EntryHeight); i++)
            {
                PlaylistElement en = new PlaylistElement();
                en.Background = new CStatic(_Theme.TextureBackgroundName, BackgroundColor, new SRectF(Rect.X, Rect.Y + (i * _Theme.EntryHeight), Rect.W, _Theme.EntryHeight, Rect.Z));
                en.Cover = new CStatic(new STexture(), _Theme.StaticCover.Color, new SRectF(Rect.X + _Theme.StaticCover.Rect.X, Rect.Y + _Theme.StaticCover.Rect.Y + (i * _Theme.EntryHeight), _Theme.StaticCover.Rect.W, _Theme.StaticCover.Rect.H, _Theme.StaticCover.Rect.Z));
                en.Text1 = new CText(Rect.X + _Theme.Text1.X, Rect.Y + _Theme.Text1.Y + (i * _Theme.EntryHeight), _Theme.Text1.Z, _Theme.Text1.Height, _Theme.Text1.MaxWidth, _Theme.Text1.Align, _Theme.Text1.Style, _Theme.Text1.Fon, _Theme.Text1.Color, "", _Theme.Text1.ReflectionSpace, _Theme.Text1.ReflectionHeight);
                en.Text2 = new CText(Rect.X + _Theme.Text2.X, Rect.Y + _Theme.Text2.Y + (i * _Theme.EntryHeight), _Theme.Text2.Z, _Theme.Text2.Height, _Theme.Text2.MaxWidth, _Theme.Text2.Align, _Theme.Text2.Style, _Theme.Text2.Fon, _Theme.Text2.Color, "", _Theme.Text2.ReflectionSpace, _Theme.Text2.ReflectionHeight);
                en.Text3 = new CText(Rect.X + _Theme.Text3.X, Rect.Y + _Theme.Text3.Y + (i * _Theme.EntryHeight), _Theme.Text3.Z, _Theme.Text3.Height, _Theme.Text3.MaxWidth, _Theme.Text3.Align, _Theme.Text3.Style, _Theme.Text3.Fon, _Theme.Text3.Color, "", _Theme.Text3.ReflectionSpace, _Theme.Text3.ReflectionHeight);
                en.SelectSlide = (CSelectSlide)_Theme.SelectSlideGameMode.Clone();
                en.SelectSlide.Rect = new SRectF(Rect.X + _Theme.SelectSlideGameMode.Rect.X, Rect.Y + _Theme.SelectSlideGameMode.Rect.Y + (i * _Theme.EntryHeight), _Theme.SelectSlideGameMode.Rect.W, _Theme.SelectSlideGameMode.Rect.H, _Theme.SelectSlideGameMode.Rect.Z);
                en.SelectSlide.RectArrowLeft = new SRectF(Rect.X + _Theme.SelectSlideGameMode.RectArrowLeft.X, Rect.Y + _Theme.SelectSlideGameMode.RectArrowLeft.Y + (i * _Theme.EntryHeight), _Theme.SelectSlideGameMode.RectArrowLeft.W, _Theme.SelectSlideGameMode.RectArrowLeft.H, _Theme.SelectSlideGameMode.RectArrowLeft.Z);
                en.SelectSlide.RectArrowRight = new SRectF(Rect.X + _Theme.SelectSlideGameMode.RectArrowRight.X, Rect.Y + _Theme.SelectSlideGameMode.RectArrowRight.Y + (i * _Theme.EntryHeight), _Theme.SelectSlideGameMode.RectArrowRight.W, _Theme.SelectSlideGameMode.RectArrowRight.H, _Theme.SelectSlideGameMode.RectArrowRight.Z);
                en.Content = -1;
                PlaylistElements.Add(en);
            }
        }

        public bool LoadPlaylist(int PlaylistID) 
        {
            if (PlaylistID > -1 && PlaylistID < CPlaylists.NumPlaylists)
            {
                _Theme.TextPlaylistHeader.Text = CPlaylists.Playlists[ActivePlaylistID].PlaylistName;
                ActivePlaylistID = PlaylistID;
                PlaylistElementContents.Clear();
                for (int i = 0; i < CPlaylists.Playlists[ActivePlaylistID].Songs.Count; i++)
                {
                    PlaylistElementContent pec = new PlaylistElementContent();
                    pec.SongID = CPlaylists.Playlists[ActivePlaylistID].Songs[i].SongID;
                    pec.Modes = CSongs.AllSongs[CPlaylists.Playlists[ActivePlaylistID].Songs[i].SongID].AvailableGameModes;
                    pec.Mode = CPlaylists.Playlists[ActivePlaylistID].Songs[i].GameMode;
                    PlaylistElementContents.Add(pec);
                }
                Update();
                return true;
            }
            else
                return false;
        }

        public void UpdatePlaylist()
        {
            PlaylistElementContents.Clear();
            for (int i = 0; i < CPlaylists.Playlists[ActivePlaylistID].Songs.Count; i++)
            {
                PlaylistElementContent pec = new PlaylistElementContent();
                pec.SongID = CPlaylists.Playlists[ActivePlaylistID].Songs[i].SongID;
                pec.Modes = CSongs.AllSongs[CPlaylists.Playlists[ActivePlaylistID].Songs[i].SongID].AvailableGameModes;
                pec.Mode = CPlaylists.Playlists[ActivePlaylistID].Songs[i].GameMode;
                PlaylistElementContents.Add(pec);
            }
            Update();
            if (PlaylistElementContents.Count - 1 < ActiveHover || PlaylistElementContents.Count - 1 < CurrentPlaylistElement)
            {
                ActiveHover = PlaylistElementContents.Count - 1;
                CurrentPlaylistElement = PlaylistElementContents.Count - 1;
            }
        }

        private void StartPlaylistSongs()
        {
            CGame.Reset();
            CGame.ClearSongs();

            if (ActivePlaylistID > -1 && ActivePlaylistID < CPlaylists.NumPlaylists)
            {
                for (int i = 0; i < CPlaylists.Playlists[ActivePlaylistID].Songs.Count; i++)
                {
                    CGame.AddSong(CPlaylists.Playlists[ActivePlaylistID].Songs[i].SongID, CPlaylists.Playlists[ActivePlaylistID].Songs[i].GameMode);
                }
                if (CGame.GetNumSongs() > 0)
                    CGraphics.FadeTo(EScreens.ScreenNames);
            }
        }

        private void ClosePlaylist()
        {
            Visible = false;
            Selected = false;
            ActivePlaylistID = -1;
        }

        public void Update()
        {
            if (ActivePlaylistID > -1 && ActivePlaylistID < CPlaylists.NumPlaylists)
            {
                int e = 0 + Offset;
                for (int i = 0; i < PlaylistElements.Count; i++ )
                {
                    if (e < PlaylistElementContents.Count)
                    {
                        PlaylistElements[i].Content = e;
                        PlaylistElements[i].Background.Visible = true;
                        PlaylistElements[i].Cover.Visible = true;
                        PlaylistElements[i].SelectSlide.Visible = true;
                        PlaylistElements[i].Text1.Visible = true;
                        PlaylistElements[i].Text2.Visible = true;
                        PlaylistElements[i].Text3.Visible = true;
                        PlaylistElements[i].Cover.Texture = CSongs.GetSong(PlaylistElementContents[e].SongID).CoverTextureSmall;
                        string t1 = _Theme.Text1.Text.Replace("%a", CSongs.GetSong(PlaylistElementContents[e].SongID).Artist).Replace("%t", CSongs.GetSong(PlaylistElementContents[e].SongID).Title);
                        string t2 = _Theme.Text2.Text.Replace("%a", CSongs.GetSong(PlaylistElementContents[e].SongID).Artist).Replace("%t", CSongs.GetSong(PlaylistElementContents[e].SongID).Title);
                        string t3 = _Theme.Text3.Text.Replace("%a", CSongs.GetSong(PlaylistElementContents[e].SongID).Artist).Replace("%t", CSongs.GetSong(PlaylistElementContents[e].SongID).Title);
                        PlaylistElements[i].Text1.Text = t1;
                        PlaylistElements[i].Text2.Text = t2;
                        PlaylistElements[i].Text3.Text = t3;
                        PlaylistElements[i].SelectSlide.Clear();
                        for (int g = 0; g < PlaylistElementContents[e].Modes.Length; g++)
                        {
                            PlaylistElements[i].SelectSlide.AddValue(Enum.GetName(typeof(GameModes.EGameMode), PlaylistElementContents[e].Modes[g]));
                            if (PlaylistElementContents[e].Modes[g] == PlaylistElementContents[e].Mode)
                                PlaylistElements[i].SelectSlide.SetSelectionByValueIndex(g);
                        }
                        e++;                      
                    }
                    else
                    {
                        PlaylistElements[i].Background.Visible = false;
                        PlaylistElements[i].Cover.Visible = false;
                        PlaylistElements[i].SelectSlide.Visible = false;
                        PlaylistElements[i].Text1.Visible = false;
                        PlaylistElements[i].Text2.Visible = false;
                        PlaylistElements[i].Text3.Visible = false;
                        PlaylistElements[i].Content = -1;
                    }
                }
            }
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            Rect.X += stepX;
            Rect.Y += stepY;
        }

        public void ResizeElement(int stepW, int stepH)
        {
            Rect.W += stepW;
            if (Rect.W <= 0)
                Rect.W = 1;

            Rect.H += stepH;
            if (Rect.H <= 0)
                Rect.H = 1;
        }
        #endregion ThemeEdit
    }
}
