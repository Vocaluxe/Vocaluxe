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
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using VocaluxeLib.Songs;

namespace VocaluxeLib.PartyModes.ChallengeMedley
{
    // ReSharper disable UnusedMember.Global
    public class CPartyScreenChallengeMedleyConfig : CPartyScreenChallengeMedley
        // ReSharper restore UnusedMember.Global
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 2; }
        }

        private const string _SelectSlideNumPlayers = "SelectSlideNumPlayers";
        private const string _SelectSlideNumMics = "SelectSlideNumMics";
        private const string _SelectSlideNumRounds = "SelectSlideNumRounds";
        private const string _SelectSlideNumSongs = "SelectSlideNumSongs";
        private const string _SelectSlideSongSource = "SelectSlideSongSource";
        private const string _SelectSlidePlaylist = "SelectSlidePlaylist";
        private const string _SelectSlideCategory = "SelectSlideCategory";
        private const string _ButtonNext = "ButtonNext";
        private const string _ButtonBack = "ButtonBack";

        private const int _MaxNumRounds = 100;
        private int _RoundSteps = 1;
        private bool _ConfigOk;

        public override void Init()
        {
            base.Init();

            _ThemeSelectSlides = new string[] {_SelectSlideNumPlayers, _SelectSlideNumMics, _SelectSlideNumRounds, _SelectSlideNumSongs, _SelectSlideSongSource, _SelectSlidePlaylist, _SelectSlideCategory};
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

            _RebuildSlides();
            _UpdateSlides();
        }

        public override bool UpdateGame()
        {
            _Buttons[_ButtonNext].Visible = _ConfigOk;
            return true;
        }

        private void _RebuildSlides()
        {
            // build num player slide (min player ... max player);
            _SelectSlides[_SelectSlideNumPlayers].Clear();
            for (int i = _PartyMode.MinPlayers; i <= _PartyMode.MaxPlayers; i++)
                _SelectSlides[_SelectSlideNumPlayers].AddValue(i.ToString());
            _SelectSlides[_SelectSlideNumPlayers].Selection = _PartyMode.GameData.NumPlayer - _PartyMode.MinPlayers;

            _SelectSlides[_SelectSlideNumSongs].Clear();
            for (int i = _PartyMode.MinSongs; i <= _PartyMode.MaxSongs; i++)
                _SelectSlides[_SelectSlideNumSongs].AddValue(i.ToString());
            _SelectSlides[_SelectSlideNumSongs].Selection = _PartyMode.GameData.NumSongs - _PartyMode.MinSongs;

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

             _UpdateMicsAtOnce();
            _SetRoundSteps();
            _UpdateSlideRounds();
        }

        private void _UpdateSlides()
        {
            int player = _PartyMode.GameData.NumPlayer;
            int mics = _PartyMode.GameData.NumPlayerAtOnce;
            _PartyMode.GameData.NumPlayer = _SelectSlides[_SelectSlideNumPlayers].Selection + _PartyMode.MinPlayers;
            _PartyMode.GameData.NumPlayerAtOnce = _SelectSlides[_SelectSlideNumMics].Selection + _PartyMode.MinPlayers;
            _PartyMode.GameData.NumRounds = (_SelectSlides[_SelectSlideNumRounds].Selection + 1) * _RoundSteps;
            _PartyMode.GameData.NumSongs = _SelectSlides[_SelectSlideNumSongs].Selection + _PartyMode.MinSongs;
            _PartyMode.GameData.SongSource = (ESongSource)_SelectSlides[_SelectSlideSongSource].Selection;
            _PartyMode.GameData.PlaylistID = _SelectSlides[_SelectSlidePlaylist].Selection;
            _PartyMode.GameData.CategoryIndex = _SelectSlides[_SelectSlideCategory].Selection;

            _UpdateMicsAtOnce();
            _SetRoundSteps();

            if (player != _PartyMode.GameData.NumPlayer || mics != _PartyMode.GameData.NumPlayerAtOnce)
            {
                int num = CHelper.CombinationCount(_PartyMode.GameData.NumPlayer, _PartyMode.GameData.NumPlayerAtOnce);
                while (num > _MaxNumRounds)
                    num -= _RoundSteps;
                _PartyMode.GameData.NumRounds = num;
            }

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
                            _ConfigOk = CBase.Songs.GetSongByID(id).AvailableGameModes.Any(mode => mode == EGameMode.TR_GAMEMODE_MEDLEY);
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
                        _ConfigOk = song.AvailableGameModes.Any(mode => mode == EGameMode.TR_GAMEMODE_MEDLEY);
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
                        _ConfigOk = CBase.Songs.GetSongByID(i).AvailableGameModes.Any(mode => mode == EGameMode.TR_GAMEMODE_MEDLEY);
                        if (_ConfigOk)
                            break;
                    }
                }
                else
                    _ConfigOk = false;
            }

            _SelectSlides[_SelectSlideCategory].Visible = _PartyMode.GameData.SongSource == ESongSource.TR_CATEGORY;
            _SelectSlides[_SelectSlidePlaylist].Visible = _PartyMode.GameData.SongSource == ESongSource.TR_PLAYLIST;

            _UpdateSlideRounds();
        }

        private void _UpdateMicsAtOnce()
        {
            int maxNum = Math.Min(_PartyMode.MaxMics, _PartyMode.GameData.NumPlayer);

            if (_PartyMode.GameData.NumPlayerAtOnce > maxNum)
                _PartyMode.GameData.NumPlayerAtOnce = maxNum;

            // build mics at once slide
            _SelectSlides[_SelectSlideNumMics].Clear();
            for (int i = 1; i <= maxNum; i++)
                _SelectSlides[_SelectSlideNumMics].AddValue(i.ToString());
            _SelectSlides[_SelectSlideNumMics].Selection = _PartyMode.GameData.NumPlayerAtOnce - _PartyMode.MinPlayers;
        }

        private void _UpdateSlideRounds()
        {
            // build num rounds slide
            _SelectSlides[_SelectSlideNumRounds].Clear();
            for (int i = _RoundSteps; i <= _MaxNumRounds; i += _RoundSteps)
                _SelectSlides[_SelectSlideNumRounds].AddValue(i.ToString());
            _SelectSlides[_SelectSlideNumRounds].Selection = _PartyMode.GameData.NumRounds / _RoundSteps - 1;
        }

        private void _SetRoundSteps()
        {
            if (_PartyMode.GameData.NumPlayerAtOnce < 1 || _PartyMode.GameData.NumPlayer < 1 || _PartyMode.GameData.NumPlayerAtOnce > _PartyMode.GameData.NumPlayer)
            {
                _RoundSteps = 1;
                return;
            }

            int res = _PartyMode.GameData.NumPlayer / _PartyMode.GameData.NumPlayerAtOnce;
            int mod = _PartyMode.GameData.NumPlayer % _PartyMode.GameData.NumPlayerAtOnce;

            _RoundSteps = mod == 0 ? res : _PartyMode.GameData.NumPlayer;
        }
    }
}