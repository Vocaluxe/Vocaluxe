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
using VocaluxeLib.Songs;

namespace Vocaluxe.Screens
{
    public enum EStatisticsPage
    {
        Overview,
        Recent,
        Best,
        Players
    }

    public class CScreenStatisticsDetail : CMenu
    {
        protected override int _ScreenVersion
        {
            get { return 2; }
        }

        private sealed class CRow
        {
            public string[] Cells;
            public int Rank;
            public float Bar = -1f;
            public string Player;
            public bool AvatarBeforeMain;
        }

        private const int _NumRows = 12;
        private const int _NumColumns = 4;
        private const int _NumListEntries = 100;
        private const int _NumRecentEntries = 1000;

        private const string _TextTitle = "TextTitle";
        private const string _TextFilter = "TextFilter";
        private const string _TextSummary = "TextSummary";

        private static readonly string[] _OverviewFilters =
            { "TR_SCREENSTATISTICS_MOSTSUNG", "TR_SCREENSTATISTICS_GENRES", "TR_SCREENSTATISTICS_LANGUAGES", "TR_SCREENSTATISTICS_NEVERSUNG" };
        private static readonly string[] _PlayerFilters = { "TR_SCREENSTATISTICS_MOSTSONGS", "TR_SCREENSTATISTICS_BESTAVERAGE" };
        private static readonly string[] _RankColors = { "Top1Color", "Top2Color", "Top3Color" };

        public static EStatisticsPage Page { get; set; } = EStatisticsPage.Overview;

        private string[] _TextHeader;
        private string[,] _TextCell;
        private string[] _StaticRank;
        private string[] _StaticAvatar;
        private string[] _ProgressBarRow;
        private float _AvatarMainX;
        private float _AvatarPlayerX;
        private float _MainColumnX;
        private float _MainColumnW;
        private float _ListLeft;
        private float _FirstColumnX;
        private float _FirstColumnW;
        private EAlignment _FirstColumnAlign;

        private readonly List<CRow> _Rows = new List<CRow>();
        private string[] _Headers = new string[_NumColumns];
        private List<string> _PlayerNames = new List<string>();
        private List<CSong> _NeverSung = new List<CSong>();
        private int _NumFilters = 1;
        private int _Filter;
        private int _Offset;

        public override EMusicType CurrentMusicType
        {
            get { return EMusicType.Background; }
        }

        public override void Init()
        {
            base.Init();

            _TextHeader = Enumerable.Range(1, _NumColumns).Select(c => "TextHeader" + c).ToArray();
            _StaticRank = Enumerable.Range(1, _NumRows).Select(r => "StaticRank" + r).ToArray();
            _StaticAvatar = Enumerable.Range(1, _NumRows).Select(r => "StaticAvatar" + r).ToArray();
            _ProgressBarRow = Enumerable.Range(1, _NumRows).Select(r => "ProgressBarRow" + r).ToArray();
            _TextCell = new string[_NumRows, _NumColumns];
            for (var r = 0; r < _NumRows; r++)
            {
                for (var c = 0; c < _NumColumns; c++)
                {
                    _TextCell[r, c] = "TextRow" + (r + 1) + "Column" + (c + 1);
                }
            }

            _ThemeTexts = new[] { _TextTitle, _TextFilter, _TextSummary }.Concat(_TextHeader).Concat(_TextCell.Cast<string>()).ToArray();
            _ThemeStatics = _StaticRank.Concat(_StaticAvatar).ToArray();
            _ThemeProgressBars = _ProgressBarRow;
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            foreach (var avatar in _StaticAvatar)
            {
                _Statics[avatar].Aspect = EAspect.Crop;
            }

            _AvatarMainX = _Statics[_StaticAvatar[0]].X;
            _AvatarPlayerX = _Texts[_TextCell[0, 2]].X - (_Texts[_TextCell[0, 1]].X - _AvatarMainX);
            _MainColumnX = _Texts[_TextCell[0, 1]].X;
            _MainColumnW = _Texts[_TextCell[0, 1]].W;
            _ListLeft = _Statics[_StaticRank[0]].X;
            _FirstColumnX = _Texts[_TextCell[0, 0]].X;
            _FirstColumnW = _Texts[_TextCell[0, 0]].W;
            _FirstColumnAlign = _Texts[_TextCell[0, 0]].Align;
        }

        public override void OnShow()
        {
            base.OnShow();

            _Filter = 0;
            _Offset = 0;
            _PlayerNames = CDataBase.LoadPlayerStatistics().Select(player => player.PlayerName).ToList();
            _NeverSung = CStatistics.GetNeverSung(_NumListEntries);
            _Update();
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
                case Keys.Left:
                    _ChangeFilter(-1);
                    break;

                case Keys.Right:
                    _ChangeFilter(1);
                    break;

                case Keys.Up:
                    _Scroll(-1);
                    break;

                case Keys.Down:
                    _Scroll(1);
                    break;

                case Keys.PageUp:
                    _Scroll(-_NumRows);
                    break;

                case Keys.PageDown:
                    _Scroll(_NumRows);
                    break;

                case Keys.Escape:
                case Keys.Back:
                case Keys.Enter:
                    CGraphics.FadeTo(EScreen.Statistics);
                    break;
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.Wheel != 0)
            {
                _Scroll(mouseEvent.Wheel);
            }

            if (mouseEvent.LB && CHelper.IsInBounds(_Texts[_TextFilter].Rect, mouseEvent))
            {
                _ChangeFilter(1);
            }

            if (mouseEvent.RB)
            {
                CGraphics.FadeTo(EScreen.Statistics);
            }

            return true;
        }

        public override bool UpdateGame()
        {
            return true;
        }

        private void _ChangeFilter(int step)
        {
            if (_NumFilters <= 1)
            {
                return;
            }

            _Filter = (_Filter + step + _NumFilters) % _NumFilters;
            _Offset = 0;
            _Update();
        }

        private void _Scroll(int step)
        {
            _Offset = Math.Max(0, Math.Min(Math.Max(0, _Rows.Count - _NumRows), _Offset + step));
            _UpdateRows();
        }

        private void _Update()
        {
            _Rows.Clear();
            _Texts[_TextSummary].Text = string.Empty;
            string filterText;

            switch (Page)
            {
                case EStatisticsPage.Overview:
                    _Texts[_TextTitle].Text = "TR_SCREENSTATISTICS_OVERVIEW";
                    _NumFilters = _OverviewFilters.Length;
                    filterText = _OverviewFilters[_Filter];
                    _Texts[_TextSummary].Text = string.Format(CLanguage.Translate("TR_SCREENSTATISTICS_SUMMARY"), CStatistics.NumSongs.ToString("N0"),
                                                              CStatistics.NumSungSongs.ToString("N0"), CStatistics.Coverage, CStatistics.NumPlayed.ToString("N0"));
                    _FillOverview();
                    break;

                case EStatisticsPage.Recent:
                    _Texts[_TextTitle].Text = "TR_SCREENSTATISTICS_RECENT";
                    _NumFilters = _PlayerNames.Count + 1;
                    filterText = _Filter == 0 ? "TR_SCREENSTATISTICS_ALLPLAYERS" : _PlayerNames[_Filter - 1];
                    _FillRecent();
                    break;

                case EStatisticsPage.Best:
                    _Texts[_TextTitle].Text = "TR_SCREENSTATISTICS_BEST";
                    _NumFilters = 3;
                    filterText = Enum.GetName(typeof(EGameDifficulty), (EGameDifficulty)_Filter);
                    _FillBest();
                    break;

                default:
                    _Texts[_TextTitle].Text = "TR_SCREENSTATISTICS_PLAYERS";
                    _NumFilters = _PlayerFilters.Length;
                    filterText = _PlayerFilters[_Filter];
                    _FillPlayers();
                    break;
            }

            _Texts[_TextFilter].Text = _NumFilters > 1 ? "<  " + CLanguage.Translate(filterText) + "  >" : string.Empty;
            _UpdateRows();
        }

        private void _FillOverview()
        {
            switch (_Filter)
            {
                case 0:
                    _Headers = new[] { "#", "TR_SCREENSTATISTICS_COL_SONG", "TR_SCREENSTATISTICS_COL_GENRE", "TR_SCREENSTATISTICS_COL_TIMES" };
                    var mostSung = CStatistics.GetMostSung(_NumListEntries);
                    var maxPlayed = mostSung.Count > 0 ? mostSung[0].NumPlayed : 1;
                    for (var i = 0; i < mostSung.Count; i++)
                    {
                        var song = mostSung[i];
                        _Rows.Add(new CRow
                        {
                            Cells = new[] { (i + 1).ToString(), song.Artist + " - " + song.Title, song.Genres.FirstOrDefault() ?? string.Empty, song.NumPlayed + "×" },
                            Rank = i + 1,
                            Bar = song.NumPlayed / (float)maxPlayed
                        });
                    }

                    break;

                case 1:
                case 2:
                    _Headers = new[] { string.Empty, _Filter == 1 ? "TR_SCREENSTATISTICS_COL_GENRE" : "TR_SCREENSTATISTICS_COL_LANGUAGE", "TR_SCREENSTATISTICS_COL_SUNG", "%" };
                    foreach (var category in _Filter == 1 ? CStatistics.GetGenres() : CStatistics.GetLanguages())
                    {
                        _Rows.Add(new CRow
                        {
                            Cells = new[] { string.Empty, category.Name, category.NumSung.ToString("N0") + " / " + category.NumSongs.ToString("N0"), category.Coverage + " %" },
                            Bar = category.Coverage / 100f
                        });
                    }

                    break;

                default:
                    _Headers = new[] { string.Empty, "TR_SCREENSTATISTICS_COL_SONG", "TR_SCREENSTATISTICS_COL_GENRE", "TR_SCREENSTATISTICS_COL_YEAR" };
                    foreach (var song in _NeverSung)
                    {
                        _Rows.Add(new CRow { Cells = new[] { string.Empty, song.Artist + " - " + song.Title, string.Join(", ", song.Genres), song.Year } });
                    }

                    break;
            }
        }

        private void _FillRecent()
        {
            _Headers = new[] { "TR_SCREENSTATISTICS_COL_DATE", "TR_SCREENSTATISTICS_COL_SONG", "TR_SCREENSTATISTICS_COL_PLAYER", "TR_SCREENSTATISTICS_COL_SCORE" };
            var scores = CDataBase.LoadRecentScores(_NumRecentEntries).Where(score => _Filter == 0 || score.PlayerName == _PlayerNames[_Filter - 1]).Take(_NumListEntries);
            foreach (var score in scores)
            {
                _Rows.Add(new CRow
                {
                    Cells = new[] { CStatistics.FormatDate(score.Date, true), score.Artist + " - " + score.Title, score.PlayerName, score.Score.ToString() },
                    Bar = score.Score / (float)CSettings.MaxScore,
                    Player = score.PlayerName
                });
            }
        }

        private void _FillBest()
        {
            _Headers = new[] { "#", "TR_SCREENSTATISTICS_COL_SONG", "TR_SCREENSTATISTICS_COL_PLAYER", "TR_SCREENSTATISTICS_COL_SCORE" };
            var rank = 1;
            foreach (var score in CDataBase.LoadTopScores((EGameDifficulty)_Filter, _NumListEntries))
            {
                _Rows.Add(new CRow
                {
                    Cells = new[] { rank.ToString(), score.Artist + " - " + score.Title, score.PlayerName, score.Score.ToString() },
                    Rank = rank++,
                    Bar = score.Score / (float)CSettings.MaxScore,
                    Player = score.PlayerName
                });
            }
        }

        private void _FillPlayers()
        {
            var players = CDataBase.LoadPlayerStatistics();
            var rank = 1;
            if (_Filter == 0)
            {
                _Headers = new[] { "#", "TR_SCREENSTATISTICS_COL_PLAYER", "TR_SCREENSTATISTICS_COL_AVERAGE", "TR_SCREENSTATISTICS_COL_SONGS" };
                var maxSongs = players.Count > 0 ? players.Max(player => player.NumSongs) : 1;
                foreach (var player in players.OrderByDescending(player => player.NumSongs).ThenByDescending(player => player.AverageScore))
                {
                    _Rows.Add(new CRow
                    {
                        Cells = new[]
                        {
                            rank.ToString(), player.PlayerName,
                            string.Format(CLanguage.Translate("TR_SCREENSTATISTICS_PLAYERAVERAGEDETAIL"), Math.Round(player.AverageScore).ToString("N0"), player.BestScore.ToString()),
                            string.Format(CLanguage.Translate("TR_SCREENSTATISTICS_SONGCOUNT"), player.NumSongs.ToString("N0"))
                        },
                        Rank = rank++,
                        Bar = player.NumSongs / (float)maxSongs,
                        Player = player.PlayerName,
                        AvatarBeforeMain = true
                    });
                }

                return;
            }

            _Headers = new[] { "#", "TR_SCREENSTATISTICS_COL_PLAYER", "TR_SCREENSTATISTICS_COL_SONGS", "TR_SCREENSTATISTICS_COL_AVERAGE" };
            foreach (var player in players.OrderByDescending(player => player.AverageScore).ThenByDescending(player => player.NumSongs))
            {
                _Rows.Add(new CRow
                {
                    Cells = new[]
                    {
                        rank.ToString(), player.PlayerName,
                        string.Format(CLanguage.Translate("TR_SCREENSTATISTICS_PLAYERDETAIL"), player.NumSongs.ToString("N0"), player.BestScore.ToString()),
                        string.Format(CLanguage.Translate("TR_SCREENSTATISTICS_AVERAGE"), Math.Round(player.AverageScore).ToString("N0"))
                    },
                    Rank = rank++,
                    Bar = (float)player.AverageScore / CSettings.MaxScore,
                    Player = player.PlayerName,
                    AvatarBeforeMain = true
                });
            }
        }

        private void _UpdateRows()
        {
            for (var c = 0; c < _NumColumns; c++)
            {
                _Texts[_TextHeader[c]].Text = _Headers[c];
            }

            var mainX = string.IsNullOrEmpty(_Headers[0]) ? _ListLeft : _MainColumnX;
            _Texts[_TextHeader[1]].X = mainX;

            var dateColumn = Page == EStatisticsPage.Recent;
            foreach (var text in Enumerable.Range(0, _NumRows).Select(r => _Texts[_TextCell[r, 0]]).Concat(new[] { _Texts[_TextHeader[0]] }))
            {
                text.Align = dateColumn ? EAlignment.Left : _FirstColumnAlign;
                text.X = dateColumn ? _ListLeft : _FirstColumnX;
                text.W = dateColumn ? _AvatarMainX - _ListLeft - 10 : _FirstColumnW;
            }

            for (var r = 0; r < _NumRows; r++)
            {
                _Texts[_TextCell[r, 1]].X = mainX;
                _Texts[_TextCell[r, 1]].W = _MainColumnW + _MainColumnX - mainX;
            }

            for (var r = 0; r < _NumRows; r++)
            {
                var index = _Offset + r;
                var row = index < _Rows.Count ? _Rows[index] : null;

                for (var c = 0; c < _NumColumns; c++)
                {
                    var text = _Texts[_TextCell[r, c]];
                    var isNoData = r == 0 && c == 1 && _Rows.Count == 0;
                    if (row != null)
                    {
                        text.Text = row.Cells[c];
                    }
                    else
                    {
                        text.Text = isNoData ? "TR_SCREENSTATISTICS_NODATA" : string.Empty;
                    }
                    text.Visible = row != null || isNoData;
                }

                var rank = _Statics[_StaticRank[r]];
                rank.Visible = row != null && row.Rank >= 1 && row.Rank <= _RankColors.Length;
                if (rank.Visible && row != null)
                {
                    SColorF color;
                    if (CBase.Themes.GetColor(_RankColors[row.Rank - 1], PartyModeId, out color))
                    {
                        rank.Color = color;
                    }
                }

                var bar = _ProgressBars[_ProgressBarRow[r]];
                bar.Visible = row != null && row.Bar >= 0f;
                if (bar.Visible && row != null && Math.Abs(bar.Progress - row.Bar) > 0.0001f)
                {
                    bar.Progress = row.Bar;
                }

                var avatar = _Statics[_StaticAvatar[r]];
                var texture = row != null && row.Player != null ? CStatistics.GetAvatar(row.Player) : null;
                avatar.Texture = texture;
                avatar.Visible = texture != null;
                avatar.X = row != null && row.AvatarBeforeMain ? _AvatarMainX : _AvatarPlayerX;
            }
        }
    }
}
