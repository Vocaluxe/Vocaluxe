using System;
using System.Collections.Generic;
using System.Text;

namespace VocaluxeLib.Menu
{
    public class CPoints
    {
        private readonly SPlayer[,] _Rounds;

        public CPoints(int numRounds, SPlayer[] players)
        {
            _Rounds = new SPlayer[numRounds, players.Length];

            for (int round = 0; round < numRounds; round++)
            {
                for (int player = 0; player < players.Length; player++)
                {
                    _Rounds[round, player].ProfileID = players[player].ProfileID;
                    _Rounds[round, player].Name = players[player].Name;
                    _Rounds[round, player].Difficulty = players[player].Difficulty;
                    _Rounds[round, player].Points = 0f;
                    _Rounds[round, player].PointsGoldenNotes = 0f;
                    _Rounds[round, player].PointsLineBonus = 0f;
                    _Rounds[round, player].Medley = false;
                    _Rounds[round, player].Duet = false;
                    _Rounds[round, player].ShortSong = false;
                    _Rounds[round, player].SongFinished = false;
                }
            }
        }

        public void SetPoints(int round, int songID, SPlayer[] players, bool medley, bool duet, bool shortSong)
        {
            long dateTicks = DateTime.Now.Ticks;
            for (int player = 0; player < players.Length; player++)
            {
                _Rounds[round, player].SongID = songID;
                _Rounds[round, player].LineNr = players[player].LineNr;
                _Rounds[round, player].Points = players[player].Points;
                _Rounds[round, player].PointsGoldenNotes = players[player].PointsGoldenNotes;
                _Rounds[round, player].PointsLineBonus = players[player].PointsLineBonus;
                _Rounds[round, player].Medley = medley;
                _Rounds[round, player].Duet = duet;
                _Rounds[round, player].ShortSong = shortSong;
                _Rounds[round, player].DateTicks = dateTicks;
                _Rounds[round, player].SongFinished = players[player].SongFinished;
            }
        }

        public int NumRounds
        {
            get { return _Rounds.GetLength(0); }
        }

        public int NumPlayer
        {
            get { return _Rounds.GetLength(1); }
        }

        public SPlayer[] GetPlayer(int round, int numPlayer)
        {
            if (NumPlayer == 0)
                return new SPlayer[1];
            if (round >= NumRounds)
                return new SPlayer[1];

            SPlayer[] player = new SPlayer[numPlayer];

            for (int p = 0; p < player.Length; p++)
            {
                player[p].Name = _Rounds[round, p].Name;
                player[p].Points = _Rounds[round, p].Points;
                player[p].PointsGoldenNotes = _Rounds[round, p].PointsGoldenNotes;
                player[p].PointsLineBonus = _Rounds[round, p].PointsLineBonus;
                player[p].SongID = _Rounds[round, p].SongID;
                player[p].LineNr = _Rounds[round, p].LineNr;
                player[p].Difficulty = _Rounds[round, p].Difficulty;
                player[p].Medley = _Rounds[round, p].Medley;
                player[p].Duet = _Rounds[round, p].Duet;
                player[p].ShortSong = _Rounds[round, p].ShortSong;
                player[p].DateTicks = _Rounds[round, p].DateTicks;
                player[p].SongFinished = _Rounds[round, p].SongFinished;
                player[p].ProfileID = _Rounds[round, p].ProfileID;
            }
            return player;
        }
    }
}
