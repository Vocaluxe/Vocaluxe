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
        private bool _VideoPreview;

        private bool VideoPreview
        {
            get { return _VideoPreview; }
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
            get { return CConfig.VideosToBackground == EOffOn.TR_CONFIG_ON; }
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

        public override void Init()
        {
            base.Init();

            _ThemeStatics = new[] {StaticBG, StaticCover};
            _ThemeTexts = new[] {TextCurrentSong};

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

            _ScreenArea = Statics[StaticBG].Rect;
        }

        public override bool HandleInput(KeyEvent keyEvent)
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
                        if (Buttons[ButtonNext].Selected)
                            CBackgroundMusic.Next();
                        if (Buttons[ButtonPrevious].Selected)
                            CBackgroundMusic.Previous();
                        if (Buttons[ButtonPlay].Selected)
                            CBackgroundMusic.Play();
                        if (Buttons[ButtonPause].Selected)
                            CBackgroundMusic.Pause();
                        if (Buttons[ButtonRepeat].Selected)
                            CBackgroundMusic.RepeatSong = !CBackgroundMusic.RepeatSong;
                        if (Buttons[ButtonShowVideo].Selected)
                            VideoPreview = !VideoPreview;
                        if (Buttons[ButtonSing].Selected)
                            StartSong(CBackgroundMusic.SongID, CBackgroundMusic.Duet);
                        if (Buttons[ButtonToBackgroundVideo].Selected)
                            VideoBackground = !VideoBackground;
                        break;
                }
            }

            return true;
        }

        public override bool HandleMouse(MouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);
            if (mouseEvent.LB && IsMouseOver(mouseEvent))
            {
                if (Buttons[ButtonNext].Selected)
                    CBackgroundMusic.Next();
                if (Buttons[ButtonPrevious].Selected)
                    CBackgroundMusic.Previous();
                if (Buttons[ButtonPlay].Selected)
                    CBackgroundMusic.Play();
                if (Buttons[ButtonPause].Selected)
                    CBackgroundMusic.Pause();
                if (Buttons[ButtonRepeat].Selected)
                    CBackgroundMusic.RepeatSong = !CBackgroundMusic.RepeatSong;
                if (Buttons[ButtonShowVideo].Selected)
                    VideoPreview = !VideoPreview;
                if (Buttons[ButtonSing].Selected)
                    StartSong(CBackgroundMusic.SongID, CBackgroundMusic.Duet);
                if (Buttons[ButtonToBackgroundVideo].Selected)
                    VideoBackground = !VideoBackground;
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
            Statics[StaticCover].Visible = !_VideoPreview || !CBackgroundMusic.SongHasVideo;
            Buttons[ButtonToBackgroundVideo].Pressed = VideoBackground;
            Buttons[ButtonShowVideo].Pressed = _VideoPreview;
            Buttons[ButtonRepeat].Pressed = CBackgroundMusic.RepeatSong;
            Buttons[ButtonSing].Visible = CBackgroundMusic.CanSing && CParty.CurrentPartyModeID == -1;
            return true;
        }

        public override bool Draw()
        {
            if (!_Active)
                return false;
            Statics[StaticCover].Texture = CBackgroundMusic.Cover;
            if (CBackgroundMusic.VideoEnabled && VideoPreview && CBackgroundMusic.SongHasVideo)
                CDraw.DrawTexture(Statics[StaticCover], CBackgroundMusic.GetVideoTexture(), EAspect.Crop);
            Buttons[ButtonPause].Visible = CBackgroundMusic.Playing;
            Buttons[ButtonPlay].Visible = !CBackgroundMusic.Playing;
            Texts[TextCurrentSong].Text = CBackgroundMusic.ArtistAndTitle;

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