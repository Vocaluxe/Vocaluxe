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
using System.Windows.Forms;
using VocaluxeLib.Draw;
using VocaluxeLib.Songs;

namespace VocaluxeLib.Menu
{
    public abstract class CMenuPartySongSelection : CMenuParty
    {
        private const string _ButtonNext = "ButtonNext";
        private const string _ButtonBack = "ButtonBack";

        private const string _SelectSlideSongMode = "SelectSlideSongMode";
        private const string _SelectSlideSource = "SelectSlideSource";
        private const string _SelectSlidePlaylist = "SelectSlidePlaylist";
        private const string _SelectSlideSorting = "SelectSlideSorting";
        private const string _SelectSlideCategory = "SelectSlideCategory";
        private const string _SelectSlideNumMedleySongs = "SelectSlideNumMedleySongs";

        protected int Playlist;
        protected EGameMode SongMode
        {
            get { return AllowedSongModes[_SongMode]; }
            set
            {
                _SongMode = 0;
                for (int i = 0; i < AllowedSongModes.Length; i++)
                {
                    if (AllowedSongModes[i] == value)
                    {
                        _SongMode = i;
                        break;
                    }
                }
            }
        }
        private int _SongMode;

        protected ESongSource Source
        {
            get { return AllowedSongSources[_Source]; }
            set
            {
                _Source = 0;
                for (int i = 0; i < AllowedSongSources.Length; i++)
                {
                    if (AllowedSongSources[i] == value)
                    {
                        _Source = i;
                        break;
                    }
                }
            }
        }
        private int _Source;

        protected ESongSorting Sorting
        {
            get { return AllowedSongSorting[_Sorting]; }
            set
            {
                _Sorting = 0;
                for (int i = 0; i < AllowedSongSorting.Length; i++)
                {
                    if (AllowedSongSorting[i] == value)
                    {
                        _Sorting = i;
                        break;
                    }
                }
            }
        }
        private int _Sorting;

        protected int Category;

        protected EGameMode[] AllowedSongModes;
        protected ESongSource[] AllowedSongSources;
        protected ESongSorting[] AllowedSongSorting;

        protected int NumMedleySongs = 5;
        protected int NumMinMedleySongs = 3;
        protected int NumMaxMedleySongs = 10;

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[]{ _ButtonNext, _ButtonBack};
            _ThemeSelectSlides = new string[] { _SelectSlideSongMode, _SelectSlideSource, _SelectSlidePlaylist, _SelectSlideSorting, _SelectSlideCategory, _SelectSlideNumMedleySongs };

            _SetAllowedOptions();
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            switch (keyEvent.Key)
            {
                case Keys.Back:
                case Keys.Escape:
                    Back();
                    break;
                case Keys.Enter:
                    if (_Buttons[_ButtonNext].Selected)
                        Next();
                    else if (_Buttons[_ButtonBack].Selected)
                        Back();
                    break;

                case Keys.Left:
                case Keys.Right:
                    _GetSelectedOptions();
                    _UpdateSelectSlideVisibility();
                    break;
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                if (_Buttons[_ButtonBack].Selected)
                    Back();
                else if (_Buttons[_ButtonNext].Selected)
                    Next();
                else
                {
                    _GetSelectedOptions();
                    _UpdateSelectSlideVisibility();
                }
            }
            else if (mouseEvent.RB)
                Back();

            return true;
        }

        public abstract void Back();
        public abstract void Next();

        public override void OnShow()
        {
            base.OnShow();

            CBase.Songs.SortSongs(Sorting, EOffOn.TR_CONFIG_ON, CBase.Config.GetIgnoreArticles(), "", EDuetOptions.All, -1);

            _FillSlides();
            _SetSelectedOptions();
            _UpdateSelectSlideVisibility();
        }

        private void _FillSlides()
        {
            _SelectSlides[_SelectSlideSongMode].Clear();
            foreach (EGameMode gm in AllowedSongModes)
                _SelectSlides[_SelectSlideSongMode].AddValue(gm.ToString());

            _SelectSlides[_SelectSlideSource].Clear();
            foreach (ESongSource ss in AllowedSongSources)
                _SelectSlides[_SelectSlideSource].AddValue(ss.ToString());

            _SelectSlides[_SelectSlideSorting].Clear();
            foreach (ESongSorting ss in AllowedSongSorting)
                _SelectSlides[_SelectSlideSorting].AddValue(ss.ToString());

            _SelectSlides[_SelectSlidePlaylist].Clear();
            List<string> playlists = CBase.Playlist.GetNames();
            for (int i = 0; i < playlists.Count; i++)
            {
                string value = playlists[i] + " (" + CBase.Playlist.GetSongCount(i) + " " + CBase.Language.Translate("TR_SONGS", PartyModeID) + ")";
                _SelectSlides[_SelectSlidePlaylist].AddValue(value);
            }

            _SelectSlides[_SelectSlideNumMedleySongs].Clear();
            for(int num = NumMinMedleySongs; num <= NumMaxMedleySongs; num++)
            {
                _SelectSlides[_SelectSlideNumMedleySongs].AddValue(num + " " + CBase.Language.Translate("TR_SONGS", PartyModeID));
            }

            _FillCategorySlide();
        }

        private void _FillCategorySlide()
        {
            _SelectSlides[_SelectSlideCategory].Clear();
            for (int i = 0; i < CBase.Songs.GetNumCategories(); i++)
            {
                CCategory cat = CBase.Songs.GetCategory(i);
                string value = cat.Name + " (" + cat.GetNumSongsNotSung() + " " + CBase.Language.Translate("TR_SONGS", PartyModeID) + ")";
                _SelectSlides[_SelectSlideCategory].AddValue(value);
            }
        }

        private void _GetSelectedOptions()
        {
            _SongMode = _SelectSlides[_SelectSlideSongMode].Selection;
            _Source = _SelectSlides[_SelectSlideSource].Selection;
            NumMedleySongs = NumMinMedleySongs + _SelectSlides[_SelectSlideNumMedleySongs].Selection;
            Playlist = _SelectSlides[_SelectSlidePlaylist].Selection;

            if (_SelectSlides[_SelectSlideSorting].Selection != _Sorting)
            {
                _Sorting = _SelectSlides[_SelectSlideSorting].Selection;
                CBase.Songs.SortSongs(Sorting, EOffOn.TR_CONFIG_ON, CBase.Config.GetIgnoreArticles(), "", EDuetOptions.All, -1);
                _FillCategorySlide();

                _SelectSlides[_SelectSlideCategory].Selection = 0;
            }    
            
            Category = _SelectSlides[_SelectSlideCategory].Selection;
        }

        private void _SetSelectedOptions()
        {
            _SelectSlides[_SelectSlideSongMode].Selection = _SongMode;
            _SelectSlides[_SelectSlideSource].Selection = _Source;
            _SelectSlides[_SelectSlideNumMedleySongs].Selection = NumMedleySongs - NumMinMedleySongs;
            _SelectSlides[_SelectSlidePlaylist].Selection = Playlist;
            _SelectSlides[_SelectSlideSorting].Selection = _Sorting;
            _SelectSlides[_SelectSlideCategory].Selection = Category;
        }

        private void _UpdateSelectSlideVisibility()
        {
            _SelectSlides[_SelectSlideNumMedleySongs].Visible = SongMode == EGameMode.TR_GAMEMODE_MEDLEY;
            _SelectSlides[_SelectSlidePlaylist].Visible = Source == ESongSource.TR_SONGSOURCE_PLAYLIST;
            _SelectSlides[_SelectSlideSorting].Visible = Source == ESongSource.TR_SONGSOURCE_CATEGORY;
            _SelectSlides[_SelectSlideCategory].Visible = Source == ESongSource.TR_SONGSOURCE_CATEGORY;
        }

        protected virtual void _SetAllowedOptions()
        {
            AllowedSongModes = (EGameMode[])Enum.GetValues(typeof(EGameMode));
            AllowedSongSources = (ESongSource[])Enum.GetValues(typeof(ESongSource));
            AllowedSongSorting = (ESongSorting[])Enum.GetValues(typeof(ESongSorting));
        }
    }
}
