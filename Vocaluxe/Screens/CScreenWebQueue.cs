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
using System.Diagnostics;
using System.Linq;
using Vocaluxe.Base;
using Vocaluxe.Base.Web;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    /// The WebApp queue: songs added with the WebApp, shown with the next song, requester and singers.
    /// Only the next song can be started (the order is managed in the WebApp); its game mode, audio mode and singers can still be changed.
    /// Opened from the general options of the song screen.
    public class CScreenWebQueue : CMenu
    {
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const int _NumRows = 8;
        private const long _RefreshIntervalMs = 500;

        private const string _TextInfo = "TextInfo";
        private const string _TextWarning = "TextWarning";
        private const string _TextNextLabel = "TextNextLabel";
        private const string _TextNextSong = "TextNextSong";
        private const string _TextNextDetails = "TextNextDetails";
        private const string _TextNextPlayers = "TextNextPlayers";
        private const string _TextEmpty = "TextEmpty";
        private const string _TextEmpty2 = "TextEmpty2";
        private const string _TextMore = "TextMore";
        private const string _TextMode = "TextMode";
        private const string _TextAudio = "TextAudio";

        private const string _StaticWarning = "StaticWarning";
        private const string _StaticNextCover = "StaticNextCover";
        private const string _StaticNextPanel = "StaticNextPanel";

        private const string _SelectSlideMode = "SelectSlideMode";
        private const string _SelectSlideAudio = "SelectSlideAudio";

        private const string _ButtonBack = "ButtonBack";
        private const string _ButtonServer = "ButtonServer";
        private const string _ButtonPlayers = "ButtonPlayers";
        private const string _ButtonStart = "ButtonStart";
        private const string _ButtonEntryPrefix = "ButtonEntry";

        private readonly Stopwatch _RefreshTimer = new Stopwatch();
        private IList<CWebQueue.CItem> _Items = new List<CWebQueue.CItem>();
        private int _Offset;
        private string _SlidesFor = "";
        private int _PlaylistVersion = -1;
        private int _WarningStream = -1;
        private bool _HasPlayedWarningSound;

        public override void Init()
        {
            base.Init();

            _ThemeTexts = new string[]
            {
                _TextInfo, _TextWarning, _TextNextLabel, _TextNextSong, _TextNextDetails, _TextNextPlayers, _TextEmpty, _TextEmpty2, _TextMore, _TextMode, _TextAudio
            };
            _ThemeStatics = new string[] { _StaticWarning, _StaticNextCover, _StaticNextPanel };
            _ThemeSelectSlides = new string[] { _SelectSlideMode, _SelectSlideAudio };
            _ThemeButtons = new[] { _ButtonBack, _ButtonServer, _ButtonPlayers, _ButtonStart }
                .Concat(Enumerable.Range(1, _NumRows).Select(i => _ButtonEntryPrefix + i)).ToArray();
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            //The list only shows the order, the entries can't be selected
            for (var row = 1; row <= _NumRows; row++)
            {
                _Buttons[_ButtonEntryPrefix + row].Selectable = false;
            }
        }

        public override void OnShow()
        {
            base.OnShow();
            _HasPlayedWarningSound = false;
            _Offset = 0;
            _SlidesFor = "";
            _Refresh();
            _SelectElement(_Items.Count > 0 ? _Buttons[_ButtonStart] : _Buttons[_ButtonBack]);
            _RefreshTimer.Restart();
        }

        public override void OnClose()
        {
            base.OnClose();
            if (_WarningStream != -1)
            {
                CSound.Close(_WarningStream);
                _WarningStream = -1;
            }
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed)
            {
                return true;
            }

            switch (keyEvent.Key)
            {
                case Keys.Escape:
                case Keys.Back:
                    CGraphics.FadeTo(EScreen.Song);
                    break;

                case Keys.Enter:
                    _HandleEnter();
                    break;

                case Keys.PageDown:
                    _Scroll(_NumRows);
                    break;

                case Keys.PageUp:
                    _Scroll(-_NumRows);
                    break;

                case Keys.Left:
                case Keys.Right:
                    _ReadModes();
                    break;
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.Wheel != 0)
            {
                _Scroll(Math.Sign(mouseEvent.Wheel));
            }

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                if (_SelectSlides[_SelectSlideMode].Selected || _SelectSlides[_SelectSlideAudio].Selected)
                {
                    _ReadModes();
                }
                else
                {
                    _HandleEnter();
                }
            }
            else if (mouseEvent.RB)
            {
                CGraphics.FadeTo(EScreen.Song);
            }

            return true;
        }

        public override bool UpdateGame()
        {
            if (_PlaylistVersion != CWebServer.PlaylistVersion || _RefreshTimer.ElapsedMilliseconds > _RefreshIntervalMs)
            {
                _Refresh();
                _RefreshTimer.Restart();
            }

            return true;
        }

        private void _HandleEnter()
        {
            if (_Buttons[_ButtonBack].Selected)
            {
                CGraphics.FadeTo(EScreen.Song);
            }
            else if (_Buttons[_ButtonServer].Selected)
            {
                CGraphics.ShowPopup(EPopupScreens.PopupServerQR);
            }
            else if (_Buttons[_ButtonStart].Selected)
            {
                _Start(false);
            }
            else if (_Buttons[_ButtonPlayers].Selected)
            {
                _Start(true);
            }
        }

        /// Starts the next song. Directly singing removes it from the queue right away;
        /// with the player selection it stays until singing really starts (the selection can be cancelled).
        private void _Start(bool showPlayerSelection)
        {
            if (_Items.Count == 0)
            {
                return;
            }

            var item = _Items[0];
            if (CWebQueue.Start(item.SongId, item.GameMode, item.AudioMode, item.PlayerIds, showPlayerSelection, out var singsNow) && singsNow)
            {
                CWebServer.MarkQueueEntryStarted(0);
            }
        }

        private void _Scroll(int rows)
        {
            var offset = Math.Max(0, Math.Min(_Items.Count - 1 - _NumRows, _Offset + rows));
            if (offset != _Offset)
            {
                _Offset = offset;
                _Refresh();
            }
        }

        private void _ReadModes()
        {
            if (_Items.Count == 0)
            {
                return;
            }

            var item = _Items[0];
            var gameMode = _SelectSlides[_SelectSlideMode].Visible ? (EGameMode)_SelectSlides[_SelectSlideMode].SelectedTag : item.GameMode;
            var audioMode = _SelectSlides[_SelectSlideAudio].Visible ? (EAudioMode)_SelectSlides[_SelectSlideAudio].SelectedTag : item.AudioMode;
            if (gameMode != item.GameMode || audioMode != item.AudioMode)
            {
                CWebQueue.SetModes(0, gameMode, audioMode);
                _Refresh();
            }
        }

        private void _Refresh()
        {
            _PlaylistVersion = CWebServer.PlaylistVersion;
            _Items = CWebQueue.GetItems();
            _Offset = Math.Max(0, Math.Min(_Offset, _Items.Count - 1 - _NumRows));

            var serverRunning = CWebServer.IsServerRunning();
            _Statics[_StaticWarning].Visible = !serverRunning;
            _Texts[_TextWarning].Visible = !serverRunning;
            _Texts[_TextInfo].Visible = serverRunning;
            if (!serverRunning && !_HasPlayedWarningSound)
            {
                _WarningStream = CSound.PlaySound(ESounds.Warning, false);
                CSound.SetStreamVolume(_WarningStream, CConfig.SoundEffectVolume);
                _HasPlayedWarningSound = true;
            }

            _UpdateNext();
            _UpdateRows();
            _UpdateSlides();

            var hasItems = _Items.Count > 0;
            _Texts[_TextEmpty].Visible = !hasItems;
            _Texts[_TextEmpty2].Visible = !hasItems;
            _Buttons[_ButtonStart].Visible = hasItems;
            _Buttons[_ButtonPlayers].Visible = hasItems;
            if (!hasItems && (_Buttons[_ButtonStart].Selected || _Buttons[_ButtonPlayers].Selected || _SelectSlides[_SelectSlideMode].Selected ||
                              _SelectSlides[_SelectSlideAudio].Selected))
            {
                _SelectElement(_Buttons[_ButtonBack]);
            }
        }

        private void _UpdateNext()
        {
            var visible = _Items.Count > 0;
            foreach (var text in new[] { _TextNextLabel, _TextNextSong, _TextNextDetails, _TextNextPlayers })
            {
                _Texts[text].Visible = visible;
            }

            _Statics[_StaticNextCover].Visible = visible;
            _Statics[_StaticNextPanel].Visible = visible;
            if (!visible)
            {
                return;
            }

            var item = _Items[0];
            var song = CSongs.GetSong(item.SongId);
            _Statics[_StaticNextCover].Texture = song?.CoverTextureBig ?? CCover.NoCover;
            _Texts[_TextNextSong].Text = song == null ? "?" : song.Artist + " - " + song.Title;
            _Texts[_TextNextDetails].Text = _GetDetails(item);
            _Texts[_TextNextPlayers].Text = _GetPlayers(item);
        }

        /// The songs after the next one.
        private void _UpdateRows()
        {
            for (var row = 1; row <= _NumRows; row++)
            {
                var index = 1 + _Offset + row - 1;
                var button = _Buttons[_ButtonEntryPrefix + row];
                button.Visible = index < _Items.Count;
                if (!button.Visible)
                {
                    continue;
                }

                var item = _Items[index];
                var song = CSongs.GetSong(item.SongId);
                var title = song == null ? "?" : song.Artist + " - " + song.Title;
                var requester = string.IsNullOrEmpty(item.RequestedBy) ? "" : "  (" + item.RequestedBy + ")";
                button.Text.Text = (index + 1) + ". " + title + requester;
            }

            var more = _Items.Count - 1 - _Offset - _NumRows;
            _Texts[_TextMore].Visible = more > 0;
            _Texts[_TextMore].Text = CLanguage.Translate("TR_SCREENWEBQUEUE_MORE").Replace("%d", more.ToString());
        }

        /// Game mode and audio mode of the next song; only modes the song supports.
        private void _UpdateSlides()
        {
            var item = _Items.Count > 0 ? _Items[0] : null;
            var song = item == null ? null : CSongs.GetSong(item.SongId);
            var key = song == null ? null : item.SongId + ":" + item.GameMode + ":" + item.AudioMode;
            if (key == _SlidesFor)
            {
                return;
            }

            _SlidesFor = key;
            var modes = song?.AvailableGameModes ?? new List<EGameMode>();
            _SelectSlides[_SelectSlideMode].Clear();
            foreach (var mode in modes)
            {
                _SelectSlides[_SelectSlideMode].AddValue(mode.ToString(), null, (int)mode);
            }

            _SelectSlides[_SelectSlideMode].Visible = modes.Count > 1;
            _Texts[_TextMode].Visible = modes.Count > 1;
            if (item != null && modes.Contains(item.GameMode))
            {
                _SelectSlides[_SelectSlideMode].SelectedTag = (int)item.GameMode;
            }

            var audioModes = new List<EAudioMode>();
            if (song != null && song.HasInstrumental())
            {
                audioModes.Add(EAudioMode.TR_AUDIOMODE_NORMAL);
                audioModes.Add(EAudioMode.TR_AUDIOMODE_INSTRUMENTAL);
                if (song.HasVocals())
                {
                    audioModes.Add(EAudioMode.TR_AUDIOMODE_VOCALS);
                }

                audioModes.Add(EAudioMode.TR_AUDIOMODE_KARAOKE);
            }

            _SelectSlides[_SelectSlideAudio].Clear();
            foreach (var mode in audioModes)
            {
                _SelectSlides[_SelectSlideAudio].AddValue(mode.ToString(), null, (int)mode);
            }

            _SelectSlides[_SelectSlideAudio].Visible = audioModes.Count > 1;
            _Texts[_TextAudio].Visible = audioModes.Count > 1;
            if (item != null && audioModes.Contains(item.AudioMode))
            {
                _SelectSlides[_SelectSlideAudio].SelectedTag = (int)item.AudioMode;
            }
        }

        private static string _GetDetails(CWebQueue.CItem item)
        {
            var parts = new List<string> { CLanguage.Translate(item.GameMode.ToString()) };
            if (item.AudioMode != EAudioMode.TR_AUDIOMODE_NORMAL)
            {
                parts.Add(CLanguage.Translate(item.AudioMode.ToString()));
            }

            if (!string.IsNullOrEmpty(item.RequestedBy))
            {
                parts.Add(CLanguage.Translate("TR_SCREENWEBQUEUE_ADDEDBY").Replace("%s", item.RequestedBy));
            }

            return string.Join(" · ", parts);
        }

        private static string _GetPlayers(CWebQueue.CItem item)
        {
            return item.PlayerNames.Count > 0
                       ? CLanguage.Translate("TR_SCREENWEBQUEUE_SINGERS").Replace("%s", string.Join(", ", item.PlayerNames))
                       : CLanguage.Translate("TR_SCREENWEBQUEUE_NOSINGERS");
        }
    }
}
