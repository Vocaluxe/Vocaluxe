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
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;

[assembly: ComVisible(false)]

namespace VocaluxeLib.PartyModes.TicTacToe
{
    public enum ESongSource
    {
        // ReSharper disable InconsistentNaming
        TR_ALLSONGS,
        TR_CATEGORY,
        TR_PLAYLIST
        // ReSharper restore InconsistentNaming
    }

    public class CRound
    {
        public int SongID;
        public int SingerTeam1;
        public int SingerTeam2;
        public int PointsTeam1;
        public int PointsTeam2;
        public int Winner;
        public bool Finished;
    }

    public abstract class CPartyScreenTicTacToe : CMenuParty
    {
        protected new CPartyModeTicTacToe _PartyMode;

        public override void Init()
        {
            base.Init();
            _PartyMode = (CPartyModeTicTacToe)base._PartyMode;
        }
    }

    // ReSharper disable ClassNeverInstantiated.Global
    public sealed class CPartyModeTicTacToe : CPartyMode
        // ReSharper restore ClassNeverInstantiated.Global
    {
        public override int MinMics
        {
            get { return 2; }
        }
        public override int MaxMics
        {
            get { return 2; }
        }
        public override int MinPlayers
        {
            get { return 2; }
        }
        public override int MaxPlayers
        {
            get { return 20; }
        }
        public override int MinTeams
        {
            get { return 2; }
        }
        public override int MaxTeams
        {
            get { return 2; }
        }

        public override int MinPlayersPerTeam
        {
            get { return 1; }
        }
        public override int MaxPlayersPerTeam
        {
            get { return 10; }
        }

        private enum EStage
        {
            Config,
            Names,
            Main,
            Singing
        }

        public struct SData
        {
            public int NumPlayerTeam1;
            public int NumPlayerTeam2;
            public int NumFields;
            public int Team;
            public List<int> ProfileIDsTeam1;
            public List<int> ProfileIDsTeam2;
            public List<int> PlayerTeam1;
            public List<int> PlayerTeam2;

            public ESongSource SongSource;
            public int CategoryIndex;
            public int PlaylistID;

            public EGameMode GameMode;

            public List<CRound> Rounds;
            public List<int> Songs;

            public int CurrentRoundNr;
            public int FieldNr;

            public int[] NumJokerRandom;
            public int[] NumJokerRetry;
        }

        public SData GameData;
        private EStage _Stage;

        public CPartyModeTicTacToe(int id) : base(id)
        {
            _ScreenSongOptions.Selection.RandomOnly = false;
            _ScreenSongOptions.Selection.PartyMode = true;
            _ScreenSongOptions.Selection.CategoryChangeAllowed = true;
            _ScreenSongOptions.Selection.NumJokers = new int[] {5, 5};
            _ScreenSongOptions.Selection.TeamNames = new string[] {"foo", "bar"};

            _ScreenSongOptions.Sorting.SearchString = String.Empty;
            _ScreenSongOptions.Sorting.SearchActive = false;
            _ScreenSongOptions.Sorting.DuetOptions = EDuetOptions.NoDuets;

            GameData = new SData
                {
                    NumPlayerTeam1 = 2,
                    NumPlayerTeam2 = 2,
                    NumFields = 9,
                    ProfileIDsTeam1 = new List<int>(),
                    ProfileIDsTeam2 = new List<int>(),
                    PlayerTeam1 = new List<int>(),
                    PlayerTeam2 = new List<int>(),
                    CurrentRoundNr = 0,
                    FieldNr = 0,
                    SongSource = ESongSource.TR_ALLSONGS,
                    PlaylistID = 0,
                    CategoryIndex = 0,
                    GameMode = EGameMode.TR_GAMEMODE_NORMAL,
                    Rounds = new List<CRound>(),
                    Songs = new List<int>(),
                    NumJokerRandom = new int[2],
                    NumJokerRetry = new int[2]
                };
        }

        public override void SetDefaults()
        {
            _Stage = EStage.Config;

            _ScreenSongOptions.Sorting.IgnoreArticles = CBase.Config.GetIgnoreArticles();
            _ScreenSongOptions.Sorting.SongSorting = CBase.Config.GetSongSorting();
            _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_OFF;
            _ScreenSongOptions.Selection.SongIndex = -1;

            if (CBase.Config.GetTabs() == EOffOn.TR_CONFIG_ON && _ScreenSongOptions.Sorting.SongSorting != ESongSorting.TR_CONFIG_NONE)
                _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_ON;

            GameData.Songs.Clear();
            GameData.Rounds.Clear();
            GameData.PlayerTeam1.Clear();
            GameData.PlayerTeam2.Clear();
        }

        public override bool Init()
        {
            if (!base.Init())
                return false;

            SetDefaults();
            return true;
        }

        public override void UpdateGame()
        {
            /*
            if (CBase.Songs.IsInCategory() || _ScreenSongOptions.Sorting.Tabs == EOffOn.TR_CONFIG_OFF)
                _ScreenSongOptions.Selection.RandomOnly = true;
            else
                _ScreenSongOptions.Selection.RandomOnly = false;*/
        }

        private IMenu _GetNextScreen()
        {
            switch (_Stage)
            {
                case EStage.Config:
                    return _Screens["CPartyScreenTicTacToeConfig"];
                case EStage.Names:
                    return _Screens["CPartyScreenTicTacToeNames"];
                case EStage.Main:
                    return _Screens["CPartyScreenTicTacToeMain"];
                case EStage.Singing:
                    return CBase.Graphics.GetScreen(EScreen.Sing);
                default:
                    throw new ArgumentException("Invalid stage: " + _Stage);
            }
        }

        private void _FadeToScreen()
        {
            if (CBase.Graphics.GetNextScreen() != _GetNextScreen())
                CBase.Graphics.FadeTo(_GetNextScreen());
        }

        public void Next()
        {
            switch (_Stage)
            {
                case EStage.Config:
                    _Stage = EStage.Names;
                    break;
                case EStage.Names:
                    _Stage = EStage.Main;
                    GameData.Team = CBase.Game.GetRandom(100) < 50 ? 0 : 1;
                    CBase.Songs.ResetSongSung();
                    GameData.CurrentRoundNr = 1;
                    _CreateRounds();
                    _SetNumJokers();
                    _PreparePlayerList(0);
                    break;
                case EStage.Main:
                    _Stage = EStage.Singing;
                    _StartRound(GameData.FieldNr);
                    break;
                case EStage.Singing:
                    _Stage = EStage.Main;
                    GameData.Team = GameData.Team == 1 ? 0 : 1;
                    _UpdatePlayerList();
                    break;
                default:
                    throw new ArgumentException("Invalid stage: " + _Stage);
            }
            _FadeToScreen();
        }

        public void Back()
        {
            switch (_Stage)
            {
                case EStage.Config:
                    CBase.Graphics.FadeTo(EScreen.Party);
                    return;
                case EStage.Names:
                    _Stage = EStage.Config;
                    break;
                case EStage.Main:
                    _Stage = EStage.Names;
                    break;
                default: // Rest is not allowed
                    throw new ArgumentException("Invalid stage: " + _Stage);
            }
            _FadeToScreen();
        }

        public override IMenu GetStartScreen()
        {
            return _Screens["CPartyScreenTicTacToeConfig"];
        }

        public override SScreenSongOptions GetScreenSongOptions()
        {
            throw new ArgumentException("Not required!");
        }

        public override void OnSongChange(int songIndex, ref SScreenSongOptions screenSongOptions)
        {
            throw new ArgumentException("Not required!");
        }

        public override void OnCategoryChange(int categoryIndex, ref SScreenSongOptions screenSongOptions)
        {
            throw new ArgumentException("Not required!");
        }

        public override void SetSearchString(string searchString, bool visible)
        {
            throw new ArgumentException("Not required!");
        }

        public override void JokerUsed(int teamNr) {}

        public override void SongSelected(int songID)
        {
            throw new ArgumentException("Not required!");
        }

        public override void LeavingHighscore()
        {
            CBase.Songs.AddPartySongSung(CBase.Game.GetSong(0).ID);
            _UpdateScores();
            Next();
        }

        private void _CreateRounds()
        {
            GameData.Rounds = new List<CRound>();
            for (int i = 0; i < GameData.NumFields; i++)
            {
                var r = new CRound();
                GameData.Rounds.Add(r);
            }
        }

        private void _PreparePlayerList(int team)
        {
            switch (team)
            {
                case 0:
                    {
                        GameData.PlayerTeam1 = new List<int>();
                        GameData.PlayerTeam2 = new List<int>();

                        //Prepare Player-IDs
                        var ids1 = new List<int>();
                        var ids2 = new List<int>();
                        //Add IDs to team-list
                        while (GameData.PlayerTeam1.Count < GameData.NumFields + GameData.NumJokerRetry[0] &&
                               GameData.PlayerTeam2.Count < GameData.NumFields + GameData.NumJokerRetry[1])
                        {
                            if (ids1.Count == 0)
                            {
                                for (int i = 0; i < GameData.NumPlayerTeam1; i++)
                                    ids1.Add(i);
                            }
                            if (ids2.Count == 0)
                            {
                                for (int i = 0; i < GameData.NumPlayerTeam2; i++)
                                    ids2.Add(i);
                            }
                            int num;
                            if (GameData.PlayerTeam1.Count < GameData.NumFields + GameData.NumJokerRetry[0])
                            {
                                num = CBase.Game.GetRandom(ids1.Count);
                                if (num >= ids1.Count)
                                    num = ids1.Count - 1;
                                GameData.PlayerTeam1.Add(ids1[num]);
                                ids1.RemoveAt(num);
                            }
                            if (GameData.PlayerTeam2.Count < GameData.NumFields + GameData.NumJokerRetry[1])
                            {
                                num = CBase.Game.GetRandom(ids2.Count);
                                if (num >= ids2.Count)
                                    num = ids2.Count - 1;
                                GameData.PlayerTeam2.Add(ids2[num]);
                                ids2.RemoveAt(num);
                            }
                        }
                    }
                    break;
                case 1:
                    {
                        //Prepare Player-IDs
                        var ids = new List<int>();
                        //Add IDs to team-list
                        while (GameData.PlayerTeam1.Count < GameData.NumFields + GameData.NumJokerRetry[0] && ids.Count == 0)
                        {
                            if (ids.Count == 0)
                            {
                                for (int i = 0; i < GameData.NumPlayerTeam1; i++)
                                    ids.Add(i);
                            }
                            if (GameData.PlayerTeam1.Count < GameData.NumFields + GameData.NumJokerRetry[0])
                            {
                                int num = CBase.Game.GetRandom(ids.Count);
                                if (num >= ids.Count)
                                    num = ids.Count - 1;
                                GameData.PlayerTeam1.Add(ids[num]);
                                ids.RemoveAt(num);
                            }
                        }
                    }
                    break;
                case 2:
                    {
                        //Prepare Player-IDs
                        var ids = new List<int>();
                        //Add IDs to team-list
                        while (GameData.PlayerTeam2.Count < GameData.NumFields + GameData.NumJokerRetry[1] && ids.Count == 0)
                        {
                            if (ids.Count == 0)
                            {
                                for (int i = 0; i < GameData.NumPlayerTeam2; i++)
                                    ids.Add(i);
                            }
                            if (GameData.PlayerTeam2.Count < GameData.NumFields + GameData.NumJokerRetry[1])
                            {
                                int num = CBase.Game.GetRandom(ids.Count);
                                if (num >= ids.Count)
                                    num = ids.Count - 1;
                                GameData.PlayerTeam2.Add(ids[num]);
                                ids.RemoveAt(num);
                            }
                        }
                    }
                    break;
            }
        }

        public void UpdateSongList()
        {
            if (GameData.Songs.Count > 0)
                return;

            switch (GameData.SongSource)
            {
                case ESongSource.TR_PLAYLIST:
                    for (int i = 0; i < CBase.Playlist.GetSongCount(GameData.PlaylistID); i++)
                    {
                        int id = CBase.Playlist.GetSong(GameData.PlaylistID, i).SongID;
                        if (CBase.Songs.GetSongByID(id).AvailableGameModes.Contains(GameData.GameMode))
                            GameData.Songs.Add(id);
                    }
                    break;

                case ESongSource.TR_ALLSONGS:
                    ReadOnlyCollection<CSong> avSongs = CBase.Songs.GetSongs();
                    GameData.Songs.AddRange(avSongs.Where(song => song.AvailableGameModes.Contains(GameData.GameMode)).Select(song => song.ID));
                    break;

                case ESongSource.TR_CATEGORY:
                    CBase.Songs.SetCategory(GameData.CategoryIndex);
                    avSongs = CBase.Songs.GetVisibleSongs();
                    GameData.Songs.AddRange(avSongs.Where(song => song.AvailableGameModes.Contains(GameData.GameMode)).Select(song => song.ID));

                    CBase.Songs.SetCategory(-1);
                    break;
            }
            GameData.Songs.Shuffle();
        }

        private void _UpdatePlayerList()
        {
            if (GameData.PlayerTeam1.Count == 0)
                _PreparePlayerList(1);
            if (GameData.PlayerTeam2.Count == 0)
                _PreparePlayerList(2);
        }

        private void _StartRound(int roundNr)
        {
            CBase.Game.Reset();
            CBase.Game.ClearSongs();

            CBase.Game.SetNumPlayer(2);

            SPlayer[] players = CBase.Game.GetPlayers();
            if (players == null)
                return;

            if (players.Length < 2)
                return;

            CRound round = GameData.Rounds[roundNr];
            bool isDuet = CBase.Songs.GetSongByID(round.SongID).IsDuet;

            for (int i = 0; i < 2; i++)
            {
                //default values
                players[i].ProfileID = -1;
            }

            //try to fill with the right data
            players[0].ProfileID = GameData.ProfileIDsTeam1[round.SingerTeam1];
            if (isDuet)
                players[0].VoiceNr = 0;

            players[1].ProfileID = GameData.ProfileIDsTeam2[round.SingerTeam2];
            if (isDuet)
                players[1].VoiceNr = 1;

            CBase.Game.AddSong(round.SongID, GameData.GameMode);
        }

        private void _SetNumJokers()
        {
            switch (GameData.NumFields)
            {
                case 9:
                    GameData.NumJokerRandom[0] = 1;
                    GameData.NumJokerRandom[1] = 1;
                    GameData.NumJokerRetry[0] = 0;
                    GameData.NumJokerRetry[1] = 0;
                    break;

                case 16:
                    GameData.NumJokerRandom[0] = 2;
                    GameData.NumJokerRandom[1] = 2;
                    GameData.NumJokerRetry[0] = 1;
                    GameData.NumJokerRetry[1] = 1;
                    break;

                case 25:
                    GameData.NumJokerRandom[0] = 3;
                    GameData.NumJokerRandom[1] = 3;
                    GameData.NumJokerRetry[0] = 2;
                    GameData.NumJokerRetry[1] = 2;
                    break;
            }
        }

        private void _UpdateScores()
        {
            if (!GameData.Rounds[GameData.FieldNr].Finished)
                GameData.CurrentRoundNr++;

            SPlayer[] results = CBase.Game.GetPlayers();
            if (results == null)
                return;

            if (results.Length < 2)
                return;

            GameData.Rounds[GameData.FieldNr].PointsTeam1 = (int)Math.Round(results[0].Points);
            GameData.Rounds[GameData.FieldNr].PointsTeam2 = (int)Math.Round(results[1].Points);
            GameData.Rounds[GameData.FieldNr].Finished = true;
            if (GameData.Rounds[GameData.FieldNr].PointsTeam1 < GameData.Rounds[GameData.FieldNr].PointsTeam2)
                GameData.Rounds[GameData.FieldNr].Winner = 2;
            else if (GameData.Rounds[GameData.FieldNr].PointsTeam1 > GameData.Rounds[GameData.FieldNr].PointsTeam2)
                GameData.Rounds[GameData.FieldNr].Winner = 1;
            else
            {
                GameData.Rounds[GameData.FieldNr].Finished = false;
                GameData.CurrentRoundNr--;
            }
        }
    }
}