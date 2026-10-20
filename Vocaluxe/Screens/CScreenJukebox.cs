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
using System.Linq;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    public class CScreenJukebox : CMenu
    {
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _SelectSlideSource = "SelectSlideSource";
        private const string _SelectSlidePlaylist = "SelectSlidePlaylist";
        private const string _SelectSlideSorting = "SelectSlideSorting";
        private const string _SelectSlideCategory = "SelectSlideCategory";
        private const string _SelectSlideOrder = "SelectSlideOrder";
        private const string _SelectSlideSection = "SelectSlideSection";
        private const string _SelectSlideLyrics = "SelectSlideLyrics";
        private const string _SelectSlideAudioMode = "SelectSlideAudioMode";
        private const string _SelectSlideVideo = "SelectSlideVideo";

        private const string _TextSource = "TextSource";
        private const string _TextPlaylist = "TextPlaylist";
        private const string _TextSorting = "TextSorting";
        private const string _TextCategory = "TextCategory";
        private const string _TextOrder = "TextOrder";
        private const string _TextSection = "TextSection";
        private const string _TextLyrics = "TextLyrics";
        private const string _TextAudioMode = "TextAudioMode";
        private const string _TextVideo = "TextVideo";
        private const string _TextInfo = "TextInfo";

        private const string _ButtonBack = "ButtonBack";
        private const string _ButtonStart = "ButtonStart";

        private static readonly ESongSource[] _Sources = { ESongSource.TR_SONGSOURCE_ALLSONGS, ESongSource.TR_SONGSOURCE_CATEGORY, ESongSource.TR_SONGSOURCE_PLAYLIST };
        private static readonly EAudioMode[] _AudioModes = { EAudioMode.TR_AUDIOMODE_NORMAL, EAudioMode.TR_AUDIOMODE_INSTRUMENTAL };

        private readonly string[,] _Rows =
        {
            { _TextSource, _SelectSlideSource }, { _TextPlaylist, _SelectSlidePlaylist }, { _TextSorting, _SelectSlideSorting },
            { _TextCategory, _SelectSlideCategory }, { _TextOrder, _SelectSlideOrder }, { _TextSection, _SelectSlideSection },
            { _TextLyrics, _SelectSlideLyrics }, { _TextAudioMode, _SelectSlideAudioMode }, { _TextVideo, _SelectSlideVideo }
        };

        private readonly List<float> _RowSlots = new List<float>();
        private float _TextOffsetY;
        private List<int> _PlaylistIds = new List<int>();
        private ESongSorting[] _Sortings;

        public override void Init()
        {
            base.Init();

            _ThemeSelectSlides = new string[]
            {
                _SelectSlideSource, _SelectSlidePlaylist, _SelectSlideSorting, _SelectSlideCategory, _SelectSlideOrder, _SelectSlideSection, _SelectSlideLyrics,
                _SelectSlideAudioMode, _SelectSlideVideo
            };
            _ThemeTexts = new string[]
                { _TextSource, _TextPlaylist, _TextSorting, _TextCategory, _TextOrder, _TextSection, _TextLyrics, _TextAudioMode, _TextVideo, _TextInfo };
            _ThemeButtons = new string[] { _ButtonBack, _ButtonStart };

            _Sortings = ((ESongSorting[])Enum.GetValues(typeof(ESongSorting))).Where(sorting => sorting != ESongSorting.TR_CONFIG_NONE).ToArray();
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _RowSlots.Clear();
            for (var i = 0; i < _Rows.GetLength(0); i++)
            {
                var y = _SelectSlides[_Rows[i, 1]].Y;
                if (!_RowSlots.Contains(y))
                {
                    _RowSlots.Add(y);
                }
            }

            _RowSlots.Sort();
            _TextOffsetY = _Texts[_TextSource].Y - _SelectSlides[_SelectSlideSource].Y;

            _SelectSlides[_SelectSlideSource].Clear();
            foreach (var source in _Sources)
            {
                _SelectSlides[_SelectSlideSource].AddValue(source.ToString());
            }

            _SelectSlides[_SelectSlideSorting].Clear();
            foreach (var sorting in _Sortings)
            {
                _SelectSlides[_SelectSlideSorting].AddValue(sorting.ToString());
            }

            _SelectSlides[_SelectSlideOrder].SetValues<EJukeboxOrder>((int)CJukebox.Order);
            _SelectSlides[_SelectSlideSection].SetValues<EJukeboxSection>((int)CJukebox.Section);
            _SelectSlides[_SelectSlideLyrics].SetValues<EOffOn>(CJukebox.ShowLyrics ? (int)EOffOn.TR_CONFIG_ON : (int)EOffOn.TR_CONFIG_OFF);
            _SelectSlides[_SelectSlideVideo].SetValues<EOffOn>(CJukebox.ShowVideo ? (int)EOffOn.TR_CONFIG_ON : (int)EOffOn.TR_CONFIG_OFF);

            _SelectSlides[_SelectSlideAudioMode].Clear();
            foreach (var audioMode in _AudioModes)
            {
                _SelectSlides[_SelectSlideAudioMode].AddValue(audioMode.ToString());
            }
        }

        public override void OnShow()
        {
            base.OnShow();

            CSongs.Sort(CJukebox.Sorting, EOffOn.TR_CONFIG_ON, CConfig.Config.Game.IgnoreArticles, string.Empty, EDuetOptions.All, -1);

            _FillPlaylists();
            _FillCategories();

            _SelectSlides[_SelectSlideSource].Selection = Math.Max(0, Array.IndexOf(_Sources, CJukebox.Source));
            _SelectSlides[_SelectSlideSorting].Selection = Math.Max(0, Array.IndexOf(_Sortings, CJukebox.Sorting));
            _SelectSlides[_SelectSlideAudioMode].Selection = Math.Max(0, Array.IndexOf(_AudioModes, CJukebox.AudioMode));
            _SelectSlides[_SelectSlideOrder].Selection = (int)CJukebox.Order;
            _SelectSlides[_SelectSlideSection].Selection = (int)CJukebox.Section;
            _SelectSlides[_SelectSlideLyrics].Selection = CJukebox.ShowLyrics ? (int)EOffOn.TR_CONFIG_ON : (int)EOffOn.TR_CONFIG_OFF;
            _SelectSlides[_SelectSlideVideo].Selection = CJukebox.ShowVideo ? (int)EOffOn.TR_CONFIG_ON : (int)EOffOn.TR_CONFIG_OFF;

            _UpdateLayout();
            _SelectElement(_Buttons[_ButtonStart].Visible ? _Buttons[_ButtonStart] : _Buttons[_ButtonBack]);
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
                    CGraphics.FadeTo(EScreen.Main);
                    break;

                case Keys.Enter:
                    if (_Buttons[_ButtonBack].Selected)
                    {
                        CGraphics.FadeTo(EScreen.Main);
                    }
                    else if (_Buttons[_ButtonStart].Selected)
                    {
                        _Start();
                    }

                    break;

                case Keys.Left:
                case Keys.Right:
                    _ReadSelection();
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
                {
                    CGraphics.FadeTo(EScreen.Main);
                }
                else if (_Buttons[_ButtonStart].Selected)
                {
                    _Start();
                }
                else
                {
                    _ReadSelection();
                }
            }
            else if (mouseEvent.RB)
            {
                CGraphics.FadeTo(EScreen.Main);
            }

            return true;
        }

        public override bool UpdateGame()
        {
            return true;
        }

        private void _Start()
        {
            _ReadSelection();
            if (CJukebox.Start())
            {
                CGraphics.FadeTo(EScreen.JukeboxPlayer);
            }
        }

        private void _ReadSelection()
        {
            CJukebox.Source = _Sources[Math.Max(0, _SelectSlides[_SelectSlideSource].Selection)];

            var playlist = _SelectSlides[_SelectSlidePlaylist].Selection;
            CJukebox.PlaylistId = playlist >= 0 && playlist < _PlaylistIds.Count ? _PlaylistIds[playlist] : -1;

            var sorting = _Sortings[Math.Max(0, _SelectSlides[_SelectSlideSorting].Selection)];
            if (sorting != CJukebox.Sorting)
            {
                CJukebox.Sorting = sorting;
                CJukebox.Category = 0;
                CSongs.Sort(CJukebox.Sorting, EOffOn.TR_CONFIG_ON, CConfig.Config.Game.IgnoreArticles, string.Empty, EDuetOptions.All, -1);
                _FillCategories();
            }
            else
            {
                CJukebox.Category = Math.Max(0, _SelectSlides[_SelectSlideCategory].Selection);
            }

            CJukebox.Order = (EJukeboxOrder)_SelectSlides[_SelectSlideOrder].Selection;
            CJukebox.Section = (EJukeboxSection)_SelectSlides[_SelectSlideSection].Selection;
            CJukebox.ShowLyrics = _SelectSlides[_SelectSlideLyrics].Selection == (int)EOffOn.TR_CONFIG_ON;
            CJukebox.ShowVideo = _SelectSlides[_SelectSlideVideo].Selection == (int)EOffOn.TR_CONFIG_ON;
            CJukebox.AudioMode = _AudioModes[Math.Max(0, _SelectSlides[_SelectSlideAudioMode].Selection)];

            _UpdateLayout();
        }

        private void _FillPlaylists()
        {
            _SelectSlides[_SelectSlidePlaylist].Clear();
            _PlaylistIds = CPlaylists.Ids;
            var names = CPlaylists.Names;
            for (var i = 0; i < _PlaylistIds.Count; i++)
            {
                _SelectSlides[_SelectSlidePlaylist].AddValue(names[i] + " (" + CPlaylists.GetSongCount(_PlaylistIds[i]) + ")", null, i);
            }

            var selection = _PlaylistIds.IndexOf(CJukebox.PlaylistId);
            _SelectSlides[_SelectSlidePlaylist].Selection = Math.Max(0, selection);
            CJukebox.PlaylistId = _PlaylistIds.Count > 0 ? _PlaylistIds[Math.Max(0, selection)] : -1;
        }

        private void _FillCategories()
        {
            _SelectSlides[_SelectSlideCategory].Clear();
            for (var i = 0; i < CSongs.NumCategories; i++)
            {
                var category = CSongs.GetCategoryByIndex(i);
                _SelectSlides[_SelectSlideCategory].AddValue(category.Name + " (" + category.Songs.Count + ")", null, i);
            }

            if (CJukebox.Category >= CSongs.NumCategories)
            {
                CJukebox.Category = 0;
            }

            _SelectSlides[_SelectSlideCategory].Selection = CJukebox.Category;
        }

        private void _UpdateLayout()
        {
            _SelectSlides[_SelectSlidePlaylist].Visible = CJukebox.Source == ESongSource.TR_SONGSOURCE_PLAYLIST;
            _SelectSlides[_SelectSlideSorting].Visible = CJukebox.Source == ESongSource.TR_SONGSOURCE_CATEGORY;
            _SelectSlides[_SelectSlideCategory].Visible = CJukebox.Source == ESongSource.TR_SONGSOURCE_CATEGORY;

            var slot = 0;
            for (var i = 0; i < _Rows.GetLength(0); i++)
            {
                var slide = _SelectSlides[_Rows[i, 1]];
                var text = _Texts[_Rows[i, 0]];
                text.Visible = slide.Visible;
                if (!slide.Visible || slot >= _RowSlots.Count)
                {
                    continue;
                }

                slide.Y = _RowSlots[slot];
                text.Y = _RowSlots[slot] + _TextOffsetY;
                slot++;
            }

            var numSongs = CJukebox.GetSongIds().Count;
            _Texts[_TextInfo].Text = numSongs > 0
                                         ? CLanguage.Translate("TR_SCREENJUKEBOX_NUMSONGS").Replace("%d", numSongs.ToString())
                                         : CLanguage.Translate("TR_SCREENJUKEBOX_NOSONGS");
            _Buttons[_ButtonStart].Visible = numSongs > 0;
        }
    }
}
