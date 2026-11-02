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
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    public class CScreenStatistics : CMenu
    {
        protected override int _ScreenVersion
        {
            get { return 2; }
        }

        private const int _NumTiles = 4;
        private const int _NumMostSung = 2;
        private const int _NumRecent = 5;
        private const int _NumDifficulties = 3;
        private const int _NumBest = 3;
        private const int _NumPlayers = 4;

        private const string _StaticTileSelection = "StaticTileSelection";
        private const string _TextSongsValue = "TextSongsValue";
        private const string _TextSungValue = "TextSungValue";
        private const string _TextPlayersValue = "TextPlayersValue";
        private const string _TextCoverage = "TextCoverage";
        private const string _ProgressBarCoverage = "ProgressBarCoverage";

        private string[] _StaticTile;
        private string[] _TextMostSung;
        private string[] _TextRecentDate;
        private string[] _TextRecentSong;
        private string[] _TextRecentScore;
        private string[,] _TextBestName;
        private string[,] _TextBestScore;
        private string[,] _StaticBestMedal;
        private string[] _StaticAvatar;
        private string[] _TextPlayerName;
        private string[] _TextPlayerAverage;

        private int _SelectedTile;

        public override EMusicType CurrentMusicType
        {
            get { return EMusicType.Background; }
        }

        public override void Init()
        {
            base.Init();

            _StaticTile = Enumerable.Range(1, _NumTiles).Select(i => "StaticTile" + i).ToArray();
            _TextMostSung = Enumerable.Range(1, _NumMostSung).Select(i => "TextMostSung" + i).ToArray();
            _TextRecentDate = Enumerable.Range(1, _NumRecent).Select(i => "TextRecentDate" + i).ToArray();
            _TextRecentSong = Enumerable.Range(1, _NumRecent).Select(i => "TextRecentSong" + i).ToArray();
            _TextRecentScore = Enumerable.Range(1, _NumRecent).Select(i => "TextRecentScore" + i).ToArray();
            _StaticAvatar = Enumerable.Range(1, _NumPlayers).Select(i => "StaticAvatar" + i).ToArray();
            _TextPlayerName = Enumerable.Range(1, _NumPlayers).Select(i => "TextPlayerName" + i).ToArray();
            _TextPlayerAverage = Enumerable.Range(1, _NumPlayers).Select(i => "TextPlayerAverage" + i).ToArray();

            _TextBestName = new string[_NumDifficulties, _NumBest];
            _TextBestScore = new string[_NumDifficulties, _NumBest];
            _StaticBestMedal = new string[_NumDifficulties, _NumBest];
            for (var d = 0; d < _NumDifficulties; d++)
            {
                for (var i = 0; i < _NumBest; i++)
                {
                    _TextBestName[d, i] = "TextBestName" + (d + 1) + (i + 1);
                    _TextBestScore[d, i] = "TextBestScore" + (d + 1) + (i + 1);
                    _StaticBestMedal[d, i] = "StaticBestMedal" + (d + 1) + (i + 1);
                }
            }

            _ThemeStatics = _StaticTile.Concat(_StaticAvatar).Concat(_StaticBestMedal.Cast<string>()).Concat(new[] { _StaticTileSelection }).ToArray();
            _ThemeTexts = new[] { _TextSongsValue, _TextSungValue, _TextPlayersValue, _TextCoverage }
                          .Concat(_TextMostSung).Concat(_TextRecentDate).Concat(_TextRecentSong).Concat(_TextRecentScore)
                          .Concat(_TextBestName.Cast<string>()).Concat(_TextBestScore.Cast<string>())
                          .Concat(_TextPlayerName).Concat(_TextPlayerAverage).ToArray();
            _ThemeProgressBars = new[] { _ProgressBarCoverage };
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            foreach (var avatar in _StaticAvatar)
            {
                _Statics[avatar].Aspect = EAspect.Crop;
            }
        }

        public override void OnShow()
        {
            base.OnShow();

            _UpdateOverview();
            _UpdateRecent();
            _UpdateBest();
            _UpdatePlayers();
            _SelectTile(_SelectedTile);
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
                case Keys.Right:
                    _SelectTile(_SelectedTile ^ 1);
                    break;

                case Keys.Up:
                case Keys.Down:
                    _SelectTile(_SelectedTile ^ 2);
                    break;

                case Keys.Enter:
                    _OpenDetail();
                    break;

                case Keys.Escape:
                case Keys.Back:
                    CGraphics.FadeTo(EScreen.Main);
                    break;
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            for (var i = 0; i < _NumTiles; i++)
            {
                if (CHelper.IsInBounds(_Statics[_StaticTile[i]].Rect, mouseEvent))
                {
                    _SelectTile(i);
                    if (mouseEvent.LB)
                    {
                        _OpenDetail();
                    }
                }
            }

            if (mouseEvent.RB)
            {
                CGraphics.FadeTo(EScreen.Main);
            }

            return true;
        }

        public override bool UpdateGame()
        {
            return true;
        }

        private void _SelectTile(int tile)
        {
            _SelectedTile = tile;
            var selection = _Statics[_StaticTileSelection];
            var rect = _Statics[_StaticTile[tile]].Rect;
            selection.X = rect.X;
            selection.Y = rect.Y;
            selection.W = rect.W;
            selection.H = rect.H;

            for (var i = 0; i < _StaticTile.Length; i++)
            {
                _Statics[_StaticTile[i]].Visible = i != tile;
            }
        }

        private void _OpenDetail()
        {
            CScreenStatisticsDetail.Page = (EStatisticsPage)_SelectedTile;
            CGraphics.FadeTo(EScreen.StatisticsDetail);
        }

        private void _UpdateOverview()
        {
            _Texts[_TextSongsValue].Text = CStatistics.NumSongs.ToString("N0");
            _Texts[_TextSungValue].Text = CStatistics.NumSungSongs.ToString("N0");
            _Texts[_TextPlayersValue].Text = CStatistics.NumPlayers.ToString("N0");
            _Texts[_TextCoverage].Text = string.Format(CLanguage.Translate("TR_SCREENSTATISTICS_COVERAGE"), CStatistics.Coverage);
            _ProgressBars[_ProgressBarCoverage].Progress = CStatistics.Coverage / 100f;

            var mostSung = CStatistics.GetMostSung(_NumMostSung);
            for (var i = 0; i < _NumMostSung; i++)
            {
                _Texts[_TextMostSung[i]].Visible = i < mostSung.Count;
                if (i < mostSung.Count)
                {
                    _Texts[_TextMostSung[i]].Text = mostSung[i].NumPlayed + "×  " + mostSung[i].Artist + " - " + mostSung[i].Title;
                }
            }
        }

        private void _UpdateRecent()
        {
            var scores = CDataBase.LoadRecentScores(_NumRecent);
            for (var i = 0; i < _NumRecent; i++)
            {
                var isEntry = i < scores.Count;
                _Texts[_TextRecentDate[i]].Visible = isEntry;
                _Texts[_TextRecentScore[i]].Visible = isEntry;
                _Texts[_TextRecentSong[i]].Visible = isEntry || i == 0;

                if (!isEntry)
                {
                    _Texts[_TextRecentSong[i]].Text = "TR_SCREENSTATISTICS_NODATA";
                    continue;
                }

                _Texts[_TextRecentDate[i]].Text = CStatistics.FormatDate(scores[i].Date, false);
                _Texts[_TextRecentSong[i]].Text = scores[i].Artist + " - " + scores[i].Title + " · " + scores[i].PlayerName;
                _Texts[_TextRecentScore[i]].Text = scores[i].Score.ToString();
            }
        }

        private void _UpdateBest()
        {
            for (var d = 0; d < _NumDifficulties; d++)
            {
                var scores = CDataBase.LoadTopScores((EGameDifficulty)d, _NumBest);
                for (var i = 0; i < _NumBest; i++)
                {
                    var isEntry = i < scores.Count;
                    _Texts[_TextBestName[d, i]].Visible = isEntry;
                    _Texts[_TextBestScore[d, i]].Visible = isEntry;
                    _Statics[_StaticBestMedal[d, i]].Visible = isEntry;
                    if (isEntry)
                    {
                        _Texts[_TextBestName[d, i]].Text = scores[i].PlayerName;
                        _Texts[_TextBestScore[d, i]].Text = scores[i].Score.ToString();
                    }
                }
            }
        }

        private void _UpdatePlayers()
        {
            var players = CDataBase.LoadPlayerStatistics();
            for (var i = 0; i < _NumPlayers; i++)
            {
                var isEntry = i < players.Count;
                _Texts[_TextPlayerName[i]].Visible = isEntry;
                _Texts[_TextPlayerAverage[i]].Visible = isEntry;
                _Statics[_StaticAvatar[i]].Visible = false;
                if (!isEntry)
                {
                    continue;
                }

                _Texts[_TextPlayerName[i]].Text = string.Format(CLanguage.Translate("TR_SCREENSTATISTICS_PLAYERSONGS"), players[i].PlayerName, players[i].NumSongs);
                _Texts[_TextPlayerAverage[i]].Text = string.Format(CLanguage.Translate("TR_SCREENSTATISTICS_AVERAGE"), Math.Round(players[i].AverageScore).ToString("N0"));

                var avatar = CStatistics.GetAvatar(players[i].PlayerName);
                _Statics[_StaticAvatar[i]].Texture = avatar;
                _Statics[_StaticAvatar[i]].Visible = avatar != null;
            }
        }
    }
}
