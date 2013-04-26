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

            _ThemeButtons = new string[] {_ButtonPlay, _ButtonPause, _ButtonPrevious, _ButtonNext, _ButtonRepeat, _ButtonShowVideo, _ButtonSing, _ButtonToBackgroundVideo};
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _ScreenArea = _Statics[_StaticBG].Rect;
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
                        if (_Buttons[_ButtonNext].Selected)
                            CBackgroundMusic.Next();
                        if (_Buttons[_ButtonPrevious].Selected)
                            CBackgroundMusic.Previous();
                        if (_Buttons[_ButtonPlay].Selected)
                            CBackgroundMusic.Play();
                        if (_Buttons[_ButtonPause].Selected)
                            CBackgroundMusic.Pause();
                        if (_Buttons[_ButtonRepeat].Selected)
                            CBackgroundMusic.RepeatSong = !CBackgroundMusic.RepeatSong;
                        if (_Buttons[_ButtonShowVideo].Selected)
                            _VideoPreview = !_VideoPreview;
                        if (_Buttons[_ButtonSing].Selected)
                            _StartSong(CBackgroundMusic.SongID);
                        if (_Buttons[_ButtonToBackgroundVideo].Selected)
                            _VideoBackground = !_VideoBackground;
                        break;
                }
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);
            if (mouseEvent.LB && _IsMouseOver(mouseEvent))
            {
                if (_Buttons[_ButtonNext].Selected)
                    CBackgroundMusic.Next();
                if (_Buttons[_ButtonPrevious].Selected)
                    CBackgroundMusic.Previous();
                if (_Buttons[_ButtonPlay].Selected)
                    CBackgroundMusic.Play();
                if (_Buttons[_ButtonPause].Selected)
                    CBackgroundMusic.Pause();
                if (_Buttons[_ButtonRepeat].Selected)
                    CBackgroundMusic.RepeatSong = !CBackgroundMusic.RepeatSong;
                if (_Buttons[_ButtonShowVideo].Selected)
                    _VideoPreview = !_VideoPreview;
                if (_Buttons[_ButtonSing].Selected)
                    _StartSong(CBackgroundMusic.SongID);
                if (_Buttons[_ButtonToBackgroundVideo].Selected)
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
            _Statics[_StaticCover].Visible = !_VideoPreviewInt || !CBackgroundMusic.SongHasVideo;
            _Buttons[_ButtonToBackgroundVideo].Pressed = _VideoBackground;
            _Buttons[_ButtonShowVideo].Pressed = _VideoPreviewInt;
            _Buttons[_ButtonRepeat].Pressed = CBackgroundMusic.RepeatSong;
            _Buttons[_ButtonSing].Visible = CBackgroundMusic.CanSing && CParty.CurrentPartyModeID == -1;
            return true;
        }

        public override bool Draw()
        {
            if (!_Active)
                return false;
            _Statics[_StaticCover].Texture = CBackgroundMusic.Cover;
            if (CBackgroundMusic.VideoEnabled && _VideoPreview && CBackgroundMusic.SongHasVideo)
                CDraw.DrawTexture(_Statics[_StaticCover], CBackgroundMusic.GetVideoTexture(), EAspect.Crop);
            _Buttons[_ButtonPause].Visible = CBackgroundMusic.IsPlaying;
            _Buttons[_ButtonPlay].Visible = !CBackgroundMusic.IsPlaying;
            _Texts[_TextCurrentSong].Text = CBackgroundMusic.ArtistAndTitle;

            return base.Draw();
        }

        private void _StartSong(int songNr)
        {
            if (songNr < 0 || !CSongs.SongsLoaded)
                return;
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