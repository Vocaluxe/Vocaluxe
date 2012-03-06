using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;
using Vocaluxe.Lib.Song;

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

        public CPopupScreenPlayerControl()
        {
            Init();
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

        public override void LoadTheme()
        {
            base.LoadTheme();

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
                            CBackgroundMusic.Repeat();
                        if (Buttons[htButtons(ButtonShowVideo)].Selected)
                            CBackgroundMusic.ToggleVideo();
                        if (Buttons[htButtons(ButtonSing)].Selected)
                            StartSong(CBackgroundMusic.GetSongNr(), CBackgroundMusic.IsDuet());
                        if (Buttons[htButtons(ButtonToBackgroundVideo)].Selected)
                            CBackgroundMusic.ToggleVideoToBackground();
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
                    CBackgroundMusic.Repeat();
                if (Buttons[htButtons(ButtonShowVideo)].Selected)
                    CBackgroundMusic.ToggleVideo();
                if (Buttons[htButtons(ButtonSing)].Selected)
                    StartSong(CBackgroundMusic.GetSongNr(), CBackgroundMusic.IsDuet());
                if (Buttons[htButtons(ButtonToBackgroundVideo)].Selected)
                    CBackgroundMusic.ToggleVideoToBackground();
            } else if (MouseEvent.LB)
            {
                CGraphics.HidePopup(EPopupScreens.PopupPlayerControl);
                return false;
            } else if (MouseEvent.RB)
            {
                CGraphics.HidePopup(EPopupScreens.PopupPlayerControl);
                return false;
            }
            return true;
        }

        public override bool UpdateGame()
        {
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
            if (CBackgroundMusic.IsVideoEnabled() && CBackgroundMusic.HasVideo())
            {
                Statics[htStatics(StaticCover)].Texture = CBackgroundMusic.GetCover();
                Statics[htStatics(StaticCover)].Visible = false;
                CDraw.DrawTexture(Statics[htStatics(StaticCover)], CBackgroundMusic.GetVideoTexture(), EAspect.Crop);
            }
            else
            {
                Statics[htStatics(StaticCover)].Visible = true;
                Statics[htStatics(StaticCover)].Texture = CBackgroundMusic.GetCover();
            }
            Buttons[htButtons(ButtonPause)].Visible = CBackgroundMusic.IsPlaying();
            Buttons[htButtons(ButtonPlay)].Visible = !CBackgroundMusic.IsPlaying();
            Texts[htTexts(TextCurrentSong)].Text = CBackgroundMusic.GetSongArtistAndTitle();
            if (CBackgroundMusic.IsVideoEnabled())
                Buttons[htButtons(ButtonShowVideo)].SColor = CTheme.GetColor("ButtonSColor");

            return base.Draw();
        }

        private void StartSong(int SongNr, bool Duet)
        {
            if (SongNr >= 0 && CSongs.SongsLoaded)
            {
                if (Duet)
                    CGame.SetGameMode(GameModes.EGameMode.Duet);
                else
                    CGame.SetGameMode(GameModes.EGameMode.Normal);

                CGame.Reset();
                CGame.ClearSongs();
                CGame.AddSong(SongNr);

                CGraphics.FadeTo(EScreens.ScreenNames);
            }
        }
    }
}
