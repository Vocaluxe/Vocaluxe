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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using VocaluxeLib.Songs;

namespace VocaluxeLib.PartyModes.TicTacToe
{
    // ReSharper disable UnusedMember.Global
    public class CPartyScreenTicTacToeConfig : CPartyScreenTicTacToe
        // ReSharper restore UnusedMember.Global
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _SelectSlideNumFields = "SelectSlideNumFields";
        private const string _SelectSlideSongSource = "SelectSlideSongSource";
        private const string _SelectSlidePlaylist = "SelectSlidePlaylist";
        private const string _SelectSlideCategory = "SelectSlideCategory";
        private const string _SelectSlideGameMode = "SelectSlideGameMode";
        private const string _ButtonNext = "ButtonNext";
        private const string _ButtonBack = "ButtonBack";

        private bool _ConfigOk = true;

        public override void Init()
        {
            base.Init();

            _ThemeSelectSlides = new string[]
                {
                    _SelectSlideNumFields, _SelectSlideSongSource, _SelectSlidePlaylist, _SelectSlideCategory, _SelectSlideGameMode
                };
            _ThemeButtons = new string[] {_ButtonNext, _ButtonBack};
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed) {}
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Back:
                    case Keys.Escape:
                        _PartyMode.Back();
                        break;

                    case Keys.Enter:
                        _UpdateSlides();

                        if (_Buttons[_ButtonBack].Selected)
                            _PartyMode.Back();

                        if (_Buttons[_ButtonNext].Selected)
                            _PartyMode.Next();
                        break;

                    case Keys.Left:
                        _UpdateSlides();
                        break;

                    case Keys.Right:
                        _UpdateSlides();
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                _UpdateSlides();
                if (_Buttons[_ButtonBack].Selected)
                    _PartyMode.Back();

                if (_Buttons[_ButtonNext].Selected)
                    _PartyMode.Next();
            }

            if (mouseEvent.RB)
                _PartyMode.Back();

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            Debug.Assert(CBase.Config.GetMaxNumMics() >= 2);

            _FillSlides();
            _UpdateSlides();
        }

        public override bool UpdateGame()
        {
            _Buttons[_ButtonNext].Visible = _ConfigOk;
            return true;
        }

        private void _FillSlides()
        {
            _SelectSlides[_SelectSlideNumFields].Clear();
            _SelectSlides[_SelectSlideNumFields].AddValue(9);
            _SelectSlides[_SelectSlideNumFields].AddValue(16);
            _SelectSlides[_SelectSlideNumFields].AddValue(25);

            _SelectSlides[_SelectSlideNumFields].SelectedTag = _PartyMode.GameData.NumFields;

            _SelectSlides[_SelectSlideSongSource].Clear();
            _SelectSlides[_SelectSlideSongSource].SetValues<ESongSource>((int)_PartyMode.GameData.SongSource);

            List<string> playlists = CBase.Playlist.GetNames();
            _SelectSlides[_SelectSlidePlaylist].Clear();
            for (int i = 0; i < playlists.Count; i++)
            {
                string value = playlists[i] + " (" + CBase.Playlist.GetSongCount(i) + " " + CBase.Language.Translate("TR_SONGS", PartyModeID) + ")";
                _SelectSlides[_SelectSlidePlaylist].AddValue(value);
            }
            _SelectSlides[_SelectSlidePlaylist].Selection = _PartyMode.GameData.PlaylistID;
            _SelectSlides[_SelectSlidePlaylist].Visible = _PartyMode.GameData.SongSource == ESongSource.TR_PLAYLIST;

            _SelectSlides[_SelectSlideCategory].Clear();
            for (int i = 0; i < CBase.Songs.GetNumCategories(); i++)
            {
                CCategory cat = CBase.Songs.GetCategory(i);
                string value = cat.Name + " (" + cat.GetNumSongsNotSung() + " " + CBase.Language.Translate("TR_SONGS", PartyModeID) + ")";
                _SelectSlides[_SelectSlideCategory].AddValue(value);
            }
            _SelectSlides[_SelectSlideCategory].Selection = _PartyMode.GameData.CategoryIndex;
            _SelectSlides[_SelectSlideCategory].Visible = _PartyMode.GameData.SongSource == ESongSource.TR_CATEGORY;

            _SelectSlides[_SelectSlideGameMode].Visible = true;
            _SelectSlides[_SelectSlideGameMode].Clear();
            _SelectSlides[_SelectSlideGameMode].AddValues(_PartyMode.AvailableGameModes.Select(gm => gm.ToString()));
            _SelectSlides[_SelectSlideGameMode].Selection = _PartyMode.GameData.GameMode;
        }

        private void _UpdateSlides()
        {
            _PartyMode.GameData.NumFields = _SelectSlides[_SelectSlideNumFields].SelectedTag;

            _PartyMode.GameData.SongSource = (ESongSource)_SelectSlides[_SelectSlideSongSource].Selection;
            _PartyMode.GameData.PlaylistID = _SelectSlides[_SelectSlidePlaylist].Selection;
            _PartyMode.GameData.CategoryIndex = _SelectSlides[_SelectSlideCategory].Selection;
            _PartyMode.GameData.GameMode = _SelectSlides[_SelectSlideGameMode].Selection;

            EGameMode gm = _PartyMode.AvailableGameModes[_PartyMode.GameData.GameMode];

            if (_PartyMode.GameData.SongSource == ESongSource.TR_PLAYLIST)
            {
                if (CBase.Playlist.GetNumPlaylists() > 0)
                {
                    if (CBase.Playlist.GetSongCount(_PartyMode.GameData.PlaylistID) > 0)
                    {
                        _ConfigOk = false;
                        for (int i = 0; i < CBase.Playlist.GetSongCount(_PartyMode.GameData.PlaylistID); i++)
                        {
                            int id = CBase.Playlist.GetSong(_PartyMode.GameData.PlaylistID, i).SongID;
                            _ConfigOk = CBase.Songs.GetSongByID(id).AvailableGameModes.Any(mode => mode == gm);
                            if (_ConfigOk)
                                break;
                        }
                    }
                    else
                        _ConfigOk = false;
                }
                else
                    _ConfigOk = false;
            }
            if (_PartyMode.GameData.SongSource == ESongSource.TR_CATEGORY)
            {
                if (CBase.Songs.GetNumCategories() == 0)
                    _ConfigOk = false;
                else if (CBase.Songs.GetNumSongsNotSungInCategory(_PartyMode.GameData.CategoryIndex) <= 0)
                    _ConfigOk = false;
                else
                {
                    CBase.Songs.SetCategory(_PartyMode.GameData.CategoryIndex);
                    _ConfigOk = false;
                    foreach (CSong song in CBase.Songs.GetVisibleSongs())
                    {
                        _ConfigOk = song.AvailableGameModes.Any(mode => mode == gm);
                        if (_ConfigOk)
                            break;
                    }
                    CBase.Songs.SetCategory(-1);
                }
            }
            if (_PartyMode.GameData.SongSource == ESongSource.TR_ALLSONGS)
            {
                if (CBase.Songs.GetNumSongs() > 0)
                {
                    for (int i = 0; i < CBase.Songs.GetNumSongs(); i++)
                    {
                        _ConfigOk = CBase.Songs.GetSongByID(i).AvailableGameModes.Any(mode => mode == gm);
                        if (_ConfigOk)
                            break;
                    }
                }
                else
                    _ConfigOk = false;
            }
            _SelectSlides[_SelectSlideCategory].Visible = _PartyMode.GameData.SongSource == ESongSource.TR_CATEGORY;
            _SelectSlides[_SelectSlidePlaylist].Visible = _PartyMode.GameData.SongSource == ESongSource.TR_PLAYLIST;
        }
    }
}