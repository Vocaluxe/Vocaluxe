using System;
using System.Collections.Generic;

namespace VocaluxeLib.PartyModes.Challenge
{
    public class CChallengeRounds
    {
        private readonly Random _Rand;
        private readonly int _NumPlayer;

        public List<CCombination> Rounds;

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

        private List<int> _GetPlayerDemand(int roundIndex)
        {
            List<int> numPlayed = new List<int>(_NumPlayer);
            for (int i = 0; i < _NumPlayer; i++)
                numPlayed.Add(0);

            for (int i = 0; i < roundIndex; i++)
            {
                foreach (int playerIndex in Rounds[i].Player)
                    numPlayed[playerIndex]++;
            }

            int max = 0;
            foreach (int p in numPlayed)
            {
                if (p > max)
                    max = p;
            }

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

        public CCombination GetNextCombination(List<int> playerNrDemand)
        {
            if (_Combs.Count == 0)
                _Create();

            CCombination combs = new CCombination();

            if (_NumPlayer == _NumMics)
                return _Combs[0];

            //filter against PlayerNrDemand
            List<CCombination> combsFiltered = new List<CCombination>();
            if (playerNrDemand != null)
            {
                for (int i = 0; i < _Combs.Count; i++)
                {
                    if (_Combs[i].IsAvailableAll(playerNrDemand))
                        combsFiltered.Add(_Combs[i]);
                }
            }

            //1st fallback
            if (playerNrDemand != null && combsFiltered.Count == 0)
            {
                for (int i = 0; i < _Combs.Count; i++)
                {
                    if (_Combs[i].IsAvailableSomeone(playerNrDemand))
                        combsFiltered.Add(_Combs[i]);
                }
            }

            //2nd fallback
            if (combsFiltered.Count == 0)
            {
                for (int i = 0; i < _Combs.Count; i++)
                    combsFiltered.Add(_Combs[i]);
            }

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
        public List<int> Player;

        public CCombination()
        {
            Player = new List<int>();
        }

        public bool IsAvailable(int playerIndex)
        {
            foreach (int p in Player)
            {
                if (p == playerIndex)
                    return true;
            }
            return false;
        }

        public bool IsAvailableAll(List<int> playerIndices)
        {
            bool result = true;
            foreach (int p in playerIndices)
                result &= IsAvailable(p);
            return result;
        }

        public bool IsAvailableSomeone(List<int> playerIndices)
        {
            bool result = false;
            foreach (int p in playerIndices)
                result |= IsAvailable(p);
            return result;
        }
    }
}