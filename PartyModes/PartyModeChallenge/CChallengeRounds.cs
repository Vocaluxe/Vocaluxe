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
using System.Collections.Generic;
using System.Linq;

namespace VocaluxeLib.PartyModes.Challenge
{
    public class CChallengeRounds
    {
        private readonly Random _Rand;
        private readonly int _NumPlayer;

        public readonly List<CCombination> Rounds;

        public CChallengeRounds(int numRounds, int numPlayer, int numPlayerAtOnce)
        {
            Rounds = new List<CCombination>();
            _Rand = new Random(DateTime.Now.Millisecond);

            if (numPlayerAtOnce < 1 || numRounds < 1 || numPlayer == 0)
                _NumPlayer = 0;
            else
            {
                _NumPlayer = numPlayer;
                _BuildRounds(numRounds, numPlayerAtOnce, _Rand);
            }
        }

        public CCombination GetRound(int roundNr)
        {
            if (roundNr >= Rounds.Count || _NumPlayer == 0 || Rounds.Count == 0)
                return null;

            return Rounds[roundNr];
        }

        private void _BuildRounds(int numRounds, int numPlayerAtOnce, Random rand)
        {
            CCombinations combinations = new CCombinations(_NumPlayer, numPlayerAtOnce, rand);
            Rounds.Add(combinations.GetNextCombination(null));

            for (int i = 1; i < numRounds; i++)
                Rounds.Add(combinations.GetNextCombination(_GetPlayerDemand(i)));
        }

        private IEnumerable<int> _GetPlayerDemand(int roundIndex)
        {
            List<int> numPlayed = new List<int>(_NumPlayer);
            for (int i = 0; i < _NumPlayer; i++)
                numPlayed.Add(0);

            for (int i = 0; i < roundIndex; i++)
            {
                foreach (int playerIndex in Rounds[i].Player)
                    numPlayed[playerIndex]++;
            }

            int max = numPlayed.Max();

            int num = Rounds[0].Player.Count;
            List<int> result = new List<int>();
            List<int> other = new List<int>();
            List<int> last = new List<int>();

            for (int i = 0; i < numPlayed.Count; i++)
            {
                if (roundIndex == 0 || numPlayed[i] < max)
                {
                    if (roundIndex == 0 || roundIndex > 0 && numPlayed[i] < max && !Rounds[roundIndex - 1].IsAvailable(i))
                        result.Add(i);
                    else if (numPlayed[i] < max)
                        other.Add(i);
                }
                else
                {
                    if (roundIndex > 0)
                    {
                        if (!Rounds[roundIndex - 1].IsAvailable(i))
                            other.Add(i);
                        else
                            last.Add(i);
                    }
                    else
                        last.Add(i);
                }
            }

            while (result.Count < num && other.Count > 0)
            {
                int n = other.Count;
                int r = _Rand.Next(n);
                result.Add(other[r]);
                other.RemoveAt(r);
            }

            while (result.Count < num)
            {
                int n = last.Count;
                int r = _Rand.Next(n);
                result.Add(last[r]);
                last.RemoveAt(r);
            }

            while (result.Count > num)
            {
                int n = result.Count;
                int r = _Rand.Next(n);
                result.RemoveAt(r);
            }

            result.Sort();
            return result;
        }
    }

    class CCombinations
    {
        private readonly List<CCombination> _Combs;
        private readonly Random _Rand;
        private readonly int _NumPlayer;
        private readonly int _NumMics;

        public CCombinations(int numPlayer, int numMics, Random rand)
        {
            _Combs = new List<CCombination>();
            _NumMics = numMics;
            _NumPlayer = numPlayer;
            _Rand = rand;
        }

        public CCombination GetNextCombination(IEnumerable<int> playerNrDemand)
        {
            if (_Combs.Count == 0)
                _Create();

            if (_NumPlayer == _NumMics)
                return _Combs[0];

            //filter against PlayerNrDemand
            List<CCombination> combsFiltered = new List<CCombination>();
            if (playerNrDemand != null)
                combsFiltered.AddRange(_Combs.Where(t => t.IsAvailableAll(playerNrDemand)));

            //1st fallback
            if (playerNrDemand != null && combsFiltered.Count == 0)
                combsFiltered.AddRange(_Combs.Where(t => t.IsAvailableSomeone(playerNrDemand)));

            //2nd fallback
            if (combsFiltered.Count == 0)
                combsFiltered.AddRange(_Combs);

            int num = combsFiltered.Count;
            int rand = _Rand.Next(num);
            CCombination c = new CCombination();
            for (int i = 0; i < combsFiltered[rand].Player.Count; i++)
                c.Player.Add(combsFiltered[rand].Player[i]);
            _Combs.Remove(combsFiltered[rand]);

            return combsFiltered[rand];
        }

        private void _Create()
        {
            _Combs.Clear();

            int num = (int)Math.Pow(2, _NumPlayer);

            for (int i = 1; i <= num; i++)
            {
                CCombination c = new CCombination();

                for (int j = 0; j < _NumPlayer; j++)
                {
                    if ((i & (1 << j)) > 0)
                        c.Player.Add(j);
                }

                if (c.Player.Count == _NumMics)
                    _Combs.Add(c);
            }
        }
    }

    public class CCombination
    {
        public readonly List<int> Player;

        public CCombination()
        {
            Player = new List<int>();
        }

        public bool IsAvailable(int playerIndex)
        {
            return Player.Any(p => p == playerIndex);
        }

        public bool IsAvailableAll(IEnumerable<int> playerIndices)
        {
            return playerIndices.All(IsAvailable);
        }

        public bool IsAvailableSomeone(IEnumerable<int> playerIndices)
        {
            return playerIndices.Any(IsAvailable);
        }
    }
}