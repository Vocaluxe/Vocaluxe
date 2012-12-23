using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.GameModes;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;
using Vocaluxe.Menu.SongMenu;

namespace Vocaluxe.Screens
{
    class CPopupScreenPlayerControl : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        private const string StaticBG = "StaticBG";
        private const string StaticCover = "StaticCover";

        private const string ButtonPrevious = "ButtonPrevious";
        private const string ButtonPlay = "ButtonPlay";
        private const string ButtonPause = "ButtonPause";
        private const string ButtonNext = "ButtonNext";
        private const string ButtonRepeat = "ButtonRepeat";
        private const string ButtonShowVideo = "ButtonShowVideo";
        private const string ButtonSing = "ButtonSing";
        private const string ButtonToBackgroundVideo = "ButtonToBackgroundVideo";

        private const string TextCurrentSong = "TextCurrentSong";
        private bool _VideoPreview = false;

        private bool VideoPreview
        {
            get
            {
                return _VideoPreview;
            }
            set
            {
                _VideoPreview = value;
                if (!_VideoPreview && !VideoBackground)
                    CBackgroundMusic.VideoEnabled = false;
                else
                    CBackgroundMusic.VideoEnabled = true;
            }
        }

        private bool VideoBackground
        {
            get
            {
                return CConfig.VideosToBackground == EOffOn.TR_CONFIG_ON;
            }
            set
            {
                if (!value)
                {
                    if (CConfig.VideosToBackground == EOffOn.TR_CONFIG_ON)
                    {
                        CConfig.VideosToBackground = EOffOn.TR_CONFIG_OFF;
                        if (!_VideoPreview)
                            CBackgroundMusic.VideoEnabled = false;
                    }
                }
                else
                {
                    CConfig.VideosToBackground = EOffOn.TR_CONFIG_ON;
                    CBackgroundMusic.VideoEnabled = true;
                }
                CConfig.SaveConfig();
            }
        }

        public CPopupScreenPlayerControl()
        {
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "PopupScreenPlayerControl";
            _ScreenVersion = ScreenVersion;

            _ThemeStatics = new string[] { StaticBG, StaticCover };
            _ThemeTexts = new string[] { TextCurrentSong };

            List<string> buttons = new List<string>();
            buttons.Add(ButtonPlay);
            buttons.Add(ButtonPause);
            buttons.Add(ButtonPrevious);
            buttons.Add(ButtonNext);
            buttons.Add(ButtonRepeat);
            buttons.Add(ButtonShowVideo);
            buttons.Add(ButtonSing);
            buttons.Add(ButtonToBackgroundVideo);
            _ThemeButtons = buttons.ToArray();
        }

        public override void LoadTheme(string XmlPath)
        {
            base.LoadTheme(XmlPath);

            _ScreenArea = Statics[htStatics(StaticBG)].Rect;
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            base.HandleInput(KeyEvent);
            if (KeyEvent.KeyPressed && !Char.IsControl(KeyEvent.Unicode))
            {

            }
            else
            {
                switch (KeyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        CGraphics.HidePopup(EPopupScreens.PopupPlayerControl);
                        return false;

                    case Keys.Enter:
                        if (Buttons[htButtons(ButtonNext)].Selected)
                            CBackgroundMusic.Next();
                        if (Buttons[htButtons(ButtonPrevious)].Selected)
                            CBackgroundMusic.Previous();
                        if (Buttons[htButtons(ButtonPlay)].Selected)
                            CBackgroundMusic.Play();
                        if (Buttons[htButtons(ButtonPause)].Selected)
                            CBackgroundMusic.Pause();
                        if (Buttons[htButtons(ButtonRepeat)].Selected)
                            CBackgroundMusic.RepeatSong = !CBackgroundMusic.RepeatSong;
                        if (Buttons[htButtons(ButtonShowVideo)].Selected)
                            VideoPreview = !VideoPreview;
                        if (Buttons[htButtons(ButtonSing)].Selected)
                            StartSong(CBackgroundMusic.SongID, CBackgroundMusic.Duet);
                        if (Buttons[htButtons(ButtonToBackgroundVideo)].Selected)
                            VideoBackground = !VideoBackground;
                        break;
                }
            }

            return true;
        }

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            base.HandleMouse(MouseEvent);
            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {
                if (Buttons[htButtons(ButtonNext)].Selected)
                    CBackgroundMusic.Next();
                if (Buttons[htButtons(ButtonPrevious)].Selected)
                    CBackgroundMusic.Previous();
                if (Buttons[htButtons(ButtonPlay)].Selected)
                    CBackgroundMusic.Play();
                if (Buttons[htButtons(ButtonPause)].Selected)
                    CBackgroundMusic.Pause();
                if (Buttons[htButtons(ButtonRepeat)].Selected)
                    CBackgroundMusic.RepeatSong = !CBackgroundMusic.RepeatSong;
                if (Buttons[htButtons(ButtonShowVideo)].Selected)
                    VideoPreview = !VideoPreview;
                if (Buttons[htButtons(ButtonSing)].Selected)
                    StartSong(CBackgroundMusic.SongID, CBackgroundMusic.Duet);
                if (Buttons[htButtons(ButtonToBackgroundVideo)].Selected)
                    VideoBackground = !VideoBackground;
            } else if (MouseEvent.LB)
            {
                //CGraphics.HidePopup(EPopupScreens.PopupPlayerControl);
                return false;
            } else if (MouseEvent.RB)
            {
                //CGraphics.HidePopup(EPopupScreens.PopupPlayerControl);
                return false;
            }
            return true;
        }

        public override bool UpdateGame()
        {

            Statics[htStatics(StaticCover)].Visible = !_VideoPreview || !CBackgroundMusic.SongHasVideo;
            Buttons[htButtons(ButtonToBackgroundVideo)].Pressed = VideoBackground;
            Buttons[htButtons(ButtonShowVideo)].Pressed = _VideoPreview;
            Buttons[htButtons(ButtonRepeat)].Pressed = CBackgroundMusic.RepeatSong;
            Buttons[htButtons(ButtonSing)].Visible = CBackgroundMusic.CanSing && CParty.CurrentPartyModeID == -1;
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
        }

        public override bool Draw()
        {
            if (!_Active)
                return false;
            Statics[htStatics(StaticCover)].Texture = CBackgroundMusic.Cover;
            if (CBackgroundMusic.VideoEnabled && VideoPreview && CBackgroundMusic.SongHasVideo)
                CDraw.DrawTexture(Statics[htStatics(StaticCover)], CBackgroundMusic.GetVideoTexture(), EAspect.Crop);
            Buttons[htButtons(ButtonPause)].Visible = CBackgroundMusic.Playing;
            Buttons[htButtons(ButtonPlay)].Visible = !CBackgroundMusic.Playing;
            Texts[htTexts(TextCurrentSong)].Text = CBackgroundMusic.ArtistAndTitle;
            
            return base.Draw();
        }

        private void StartSong(int SongNr, bool Duet)
        {
            if (SongNr >= 0 && CSongs.SongsLoaded)
            {
                CGame.Reset();
                CGame.ClearSongs();

                EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;
                if (CSongs.AllSongs[SongNr].IsDuet)
                    gm = EGameMode.TR_GAMEMODE_DUET;

                CGame.AddSong(SongNr, gm);

                CGraphics.FadeTo(EScreens.ScreenNames);
            }
        }
    }
}
