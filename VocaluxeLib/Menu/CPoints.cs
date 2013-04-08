using System;
using System.Collections.Generic;
using System.Text;

namespace VocaluxeLib.Menu
{
    public class CPoints
    {
        private readonly SPlayer[,] _Rounds;

        public CPoints(int NumRounds, SPlayer[] Player)
        {
            _Rounds = new SPlayer[NumRounds, Player.Length];

            for (int round = 0; round < NumRounds; round++)
            {
                for (int player = 0; player < Player.Length; player++)
                {
                    _Rounds[round, player].ProfileID = Player[player].ProfileID;
                    _Rounds[round, player].Name = Player[player].Name;
                    _Rounds[round, player].Difficulty = Player[player].Difficulty;
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

        public void SetPoints(int Round, int SongID, SPlayer[] Player, bool Medley, bool Duet, bool ShortSong)
        {
            long DateTicks = DateTime.Now.Ticks;
            for (int player = 0; player < Player.Length; player++)
            {
                _Rounds[Round, player].SongID = SongID;
                _Rounds[Round, player].LineNr = Player[player].LineNr;
                _Rounds[Round, player].Points = Player[player].Points;
                _Rounds[Round, player].PointsGoldenNotes = Player[player].PointsGoldenNotes;
                _Rounds[Round, player].PointsLineBonus = Player[player].PointsLineBonus;
                _Rounds[Round, player].Medley = Medley;
                _Rounds[Round, player].Duet = Duet;
                _Rounds[Round, player].ShortSong = ShortSong;
                _Rounds[Round, player].DateTicks = DateTicks;
                _Rounds[Round, player].SongFinished = Player[player].SongFinished;
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

        public SPlayer[] GetPlayer(int Round, int numPlayer)
        {
            if (NumPlayer == 0)
                return new SPlayer[1];
            if (Round >= NumRounds)
                return new SPlayer[1];

            SPlayer[] player = new SPlayer[numPlayer];

            for (int p = 0; p < player.Length; p++)
            {
                player[p].Name = _Rounds[Round, p].Name;
                player[p].Points = _Rounds[Round, p].Points;
                player[p].PointsGoldenNotes = _Rounds[Round, p].PointsGoldenNotes;
                player[p].PointsLineBonus = _Rounds[Round, p].PointsLineBonus;
                player[p].SongID = _Rounds[Round, p].SongID;
                player[p].LineNr = _Rounds[Round, p].LineNr;
                player[p].Difficulty = _Rounds[Round, p].Difficulty;
                player[p].Medley = _Rounds[Round, p].Medley;
                player[p].Duet = _Rounds[Round, p].Duet;
                player[p].ShortSong = _Rounds[Round, p].ShortSong;
                player[p].DateTicks = _Rounds[Round, p].DateTicks;
                player[p].SongFinished = _Rounds[Round, p].SongFinished;
                player[p].ProfileID = _Rounds[Round, p].ProfileID;
            }
            return player;
        }
    }
}
