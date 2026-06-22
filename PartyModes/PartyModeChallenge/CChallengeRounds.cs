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
using System.Diagnostics;
using System.Linq;
using VocaluxeLib.Utils.Combinatorics;

namespace VocaluxeLib.PartyModes.Challenge
{
    public class CChallengeRounds
    {
        private readonly List<CRound> _Rounds = new List<CRound>();

        public int Count
        {
            get { return _Rounds.Count; }
        }

        public CRound this[int index]
        {
            get { return _Rounds[index]; }
        }

        public CChallengeRounds(int numRounds, int numPlayer, int playersPerRound)
        {
            if (numRounds < 1 || numPlayer < 1 || playersPerRound < 1)
            {
                throw new ArgumentException("Invalid paramters for the rounds");
            }

            if (playersPerRound > numPlayer)
            {
                playersPerRound = numPlayer;
            }

            _BuildRounds(numRounds, numPlayer, playersPerRound);
            if (Count > numRounds)
            {
                //Try again and take the better one
                var old = new List<CRound>(_Rounds);
                _Rounds.Clear();
                _BuildRounds(numRounds, numPlayer, playersPerRound);
                if (Count > old.Count)
                {
                    _Rounds.Clear();
                    _Rounds.AddRange(old);
                }
            }
        }

        /// <summary>
        ///     Divides players in 2 categories: Those how sung the most and the others
        /// </summary>
        /// <param name="numSung">List matching the playerIndex to the number of songs sung</param>
        private static List<int> _GetPlayersForRound(IList<int> numSung)
        {
            var mustSing = new List<int>();
            var max = numSung.Max();
            for (var i = 0; i < numSung.Count; i++)
            {
                if (numSung[i] < max)
                {
                    mustSing.Add(i);
                }
            }

            return mustSing;
        }

        private static void _AddCombinations(int numPlayer, int playersPerRound, List<CRound> combinations)
        {
            var input = new List<int>(numPlayer);
            for (var i = 0; i < numPlayer; i++)
            {
                input.Add(i);
            }

            var combs = new CCombinations<int>(input, playersPerRound);
            combinations.AddRange(combs.Select(combination => new CRound(combination)));
            combinations.Shuffle();
        }

        private static CRound _GetMatchingCombination(List<int> playersInRound, List<int> numSung, List<CRound> combinations)
        {
            foreach (var round in combinations)
            {
                var matching = round.Players.Count(playersInRound.Contains);
                // A valid round contains only players from playersInRound or all players from playersInRound (and some others)
                if (matching == round.Players.Count || matching == playersInRound.Count)
                {
                    return round;
                }
            }

            // It may happen, that there are only players that already sung against each other. So create a 2nd best option with the most number of players that should sing in it
            var maxMatching = combinations.Max(c => c.Players.Count(playersInRound.Contains));
            var nextOptions = combinations.Where(c => c.Players.Count(playersInRound.Contains) == maxMatching).ToList();
            // And select the combination that minimizes the sum of the number of songs sung (--> favor rounds with players that sung less than others)
            CRound result = null;
            var minSung = int.MaxValue;
            foreach (var round in nextOptions)
            {
                var curNumSung = round.Players.Sum(pl => numSung[pl]);
                if (curNumSung < minSung)
                {
                    minSung = curNumSung;
                    result = round;
                }
            }

            Debug.Assert(result != null);
            return result;
        }

        private void _BuildRounds(int numRounds, int numPlayer, int playersPerRound)
        {
            // What we want is that every player sung the same amount of songs
            // So we keep track how many songs each palyer sung
            var numSung = new List<int>(numPlayer);
            for (var i = 0; i < numPlayer; i++)
            {
                numSung.Add(0);
            }

            var combinations = new List<CRound>();
            // Posibly add more rounds than requested if the song count per player is unequal
            for (var i = 0; i < numRounds * 10; i++)
            {
                if (combinations.Count == 0)
                {
                    _AddCombinations(numPlayer, playersPerRound, combinations);
                }

                var playersInRound = _GetPlayersForRound(numSung);
                var curRound = _GetMatchingCombination(playersInRound, numSung, combinations);
                foreach (var player in curRound.Players)
                {
                    numSung[player]++;
                }

                _Rounds.Add(curRound);
                combinations.Remove(curRound);
                //Stop if we have enough rounds and all players sung the same amount of songs
                if (_Rounds.Count >= numRounds && numSung.All(ct => ct == numSung[0]))
                {
                    break;
                }
            }
        }
    }

    public class CRound
    {
        public readonly List<int> Players;

        public CRound(List<int> players)
        {
            Players = players.Shuffle();
        }
    }
}