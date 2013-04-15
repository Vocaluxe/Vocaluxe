using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    class CPopupScreenPlayerControl : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _StaticBG = "StaticBG";
        private const string _StaticCover = "StaticCover";

        private const string _ButtonPrevious = "ButtonPrevious";
        private const string _ButtonPlay = "ButtonPlay";
        private const string _ButtonPause = "ButtonPause";
        private const string _ButtonNext = "ButtonNext";
        private const string _ButtonRepeat = "ButtonRepeat";
        private const string _ButtonShowVideo = "ButtonShowVideo";
        private const string _ButtonSing = "ButtonSing";
        private const string _ButtonToBackgroundVideo = "ButtonToBackgroundVideo";

        private const string _TextCurrentSong = "TextCurrentSong";
        private bool _VideoPreviewInt;

        private bool _VideoPreview
        {
            get { return _VideoPreviewInt; }
            set
            {
                _VideoPreviewInt = value;
                if (!_VideoPreviewInt && !_VideoBackground)
                    CBackgroundMusic.VideoEnabled = false;
                else
                    CBackgroundMusic.VideoEnabled = true;
            }
        }

        private bool _VideoBackground
        {
            get { return CConfig.VideosToBackground == EOffOn.TR_CONFIG_ON; }
            set
            {
                if (!value)
                {
                    if (CConfig.VideosToBackground == EOffOn.TR_CONFIG_ON)
                    {
                        CConfig.VideosToBackground = EOffOn.TR_CONFIG_OFF;
                        if (!_VideoPreviewInt)
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

        public override void Init()
        {
            base.Init();

            _ThemeStatics = new string[] {_StaticBG, _StaticCover};
            _ThemeTexts = new string[] {_TextCurrentSong};

            List<string> buttons = new List<string>();
            buttons.Add(_ButtonPlay);
            buttons.Add(_ButtonPause);
            buttons.Add(_ButtonPrevious);
            buttons.Add(_ButtonNext);
            buttons.Add(_ButtonRepeat);
            buttons.Add(_ButtonShowVideo);
            buttons.Add(_ButtonSing);
            buttons.Add(_ButtonToBackgroundVideo);
            _ThemeButtons = buttons.ToArray();
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _ScreenArea = Statics[_StaticBG].Rect;
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);
            if (keyEvent.KeyPressed && !Char.IsControl(keyEvent.Unicode)) {}
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        CGraphics.HidePopup(EPopupScreens.PopupPlayerControl);
                        return false;

                    case Keys.Enter:
                        if (Buttons[_ButtonNext].Selected)
                            CBackgroundMusic.Next();
                        if (Buttons[_ButtonPrevious].Selected)
                            CBackgroundMusic.Previous();
                        if (Buttons[_ButtonPlay].Selected)
                            CBackgroundMusic.Play();
                        if (Buttons[_ButtonPause].Selected)
                            CBackgroundMusic.Pause();
                        if (Buttons[_ButtonRepeat].Selected)
                            CBackgroundMusic.RepeatSong = !CBackgroundMusic.RepeatSong;
                        if (Buttons[_ButtonShowVideo].Selected)
                            _VideoPreview = !_VideoPreview;
                        if (Buttons[_ButtonSing].Selected)
                            _StartSong(CBackgroundMusic.SongID, CBackgroundMusic.Duet);
                        if (Buttons[_ButtonToBackgroundVideo].Selected)
                            _VideoBackground = !_VideoBackground;
                        break;
                }
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);
            if (mouseEvent.LB && IsMouseOver(mouseEvent))
            {
                if (Buttons[_ButtonNext].Selected)
                    CBackgroundMusic.Next();
                if (Buttons[_ButtonPrevious].Selected)
                    CBackgroundMusic.Previous();
                if (Buttons[_ButtonPlay].Selected)
                    CBackgroundMusic.Play();
                if (Buttons[_ButtonPause].Selected)
                    CBackgroundMusic.Pause();
                if (Buttons[_ButtonRepeat].Selected)
                    CBackgroundMusic.RepeatSong = !CBackgroundMusic.RepeatSong;
                if (Buttons[_ButtonShowVideo].Selected)
                    _VideoPreview = !_VideoPreview;
                if (Buttons[_ButtonSing].Selected)
                    _StartSong(CBackgroundMusic.SongID, CBackgroundMusic.Duet);
                if (Buttons[_ButtonToBackgroundVideo].Selected)
                    _VideoBackground = !_VideoBackground;
            }
            else if (mouseEvent.LB)
            {
                //CGraphics.HidePopup(EPopupScreens.PopupPlayerControl);
                return false;
            }
            else if (mouseEvent.RB)
            {
                //CGraphics.HidePopup(EPopupScreens.PopupPlayerControl);
                return false;
            }
            return true;
        }

        public override bool UpdateGame()
        {
            Statics[_StaticCover].Visible = !_VideoPreviewInt || !CBackgroundMusic.SongHasVideo;
            Buttons[_ButtonToBackgroundVideo].Pressed = _VideoBackground;
            Buttons[_ButtonShowVideo].Pressed = _VideoPreviewInt;
            Buttons[_ButtonRepeat].Pressed = CBackgroundMusic.RepeatSong;
            Buttons[_ButtonSing].Visible = CBackgroundMusic.CanSing && CParty.CurrentPartyModeID == -1;
            return true;
        }

        public override bool Draw()
        {
            if (!_Active)
                return false;
            Statics[_StaticCover].Texture = CBackgroundMusic.Cover;
            if (CBackgroundMusic.VideoEnabled && _VideoPreview && CBackgroundMusic.SongHasVideo)
                CDraw.DrawTexture(Statics[_StaticCover], CBackgroundMusic.GetVideoTexture(), EAspect.Crop);
            Buttons[_ButtonPause].Visible = CBackgroundMusic.Playing;
            Buttons[_ButtonPlay].Visible = !CBackgroundMusic.Playing;
            Texts[_TextCurrentSong].Text = CBackgroundMusic.ArtistAndTitle;

            return base.Draw();
        }

        private void _StartSong(int songNr, bool duet)
        {
            if (songNr >= 0 && CSongs.SongsLoaded)
            {
                CGame.Reset();
                CGame.ClearSongs();

                EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;
                if (CSongs.AllSongs[songNr].IsDuet)
                    gm = EGameMode.TR_GAMEMODE_DUET;

                CGame.AddSong(songNr, gm);

                CGraphics.FadeTo(EScreens.ScreenNames);
            }
        }
    }
}