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

namespace VocaluxeLib.Menu
{
    public class CPoints
    {
        private readonly SPlayer[,] _Rounds;

        public CPoints(int numRounds, SPlayer[] players)
        {
            _Rounds = new SPlayer[numRounds,players.Length];

            for (int round = 0; round < numRounds; round++)
            {
                for (int player = 0; player < players.Length; player++)
                {
                    _Rounds[round, player].ProfileID = players[player].ProfileID;
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

            SPlayer[] players = new SPlayer[numPlayer];

            for (int p = 0; p < players.Length; p++)
            {
                players[p].Points = _Rounds[round, p].Points;
                players[p].PointsGoldenNotes = _Rounds[round, p].PointsGoldenNotes;
                players[p].PointsLineBonus = _Rounds[round, p].PointsLineBonus;
                players[p].SongID = _Rounds[round, p].SongID;
                players[p].LineNr = _Rounds[round, p].LineNr;
                players[p].Medley = _Rounds[round, p].Medley;
                players[p].Duet = _Rounds[round, p].Duet;
                players[p].ShortSong = _Rounds[round, p].ShortSong;
                players[p].DateTicks = _Rounds[round, p].DateTicks;
                players[p].SongFinished = _Rounds[round, p].SongFinished;
                players[p].ProfileID = _Rounds[round, p].ProfileID;
            }
            return players;
        }
    }
}