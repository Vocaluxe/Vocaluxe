using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    public class ChallengeRounds
    {
        private Random _Rand;
        private int _NumPlayer;

        public List<Combination> Rounds;

        public ChallengeRounds(int NumRounds, int NumPlayer, int NumPlayerAtOnce)
        {
            Rounds = new List<Combination>();
            _Rand = new Random(System.DateTime.Now.Millisecond);

            if (NumPlayerAtOnce < 1 || NumRounds < 1 || NumPlayer == 0)
                _NumPlayer = 0;
            else
            {
                _NumPlayer = NumPlayer;
                BuildRounds(NumRounds, NumPlayerAtOnce, _Rand);
            }
        }

        public Combination GetRound(int RoundNr)
        {
            if (RoundNr >= Rounds.Count || _NumPlayer == 0 || Rounds.Count == 0)
                return null;

            return Rounds[RoundNr];
        }

        private void BuildRounds(int NumRounds, int NumPlayerAtOnce, Random Rand)
        {
            Combinations Combinations = new Combinations(_NumPlayer, NumPlayerAtOnce, Rand);
            Rounds.Add(Combinations.GetNextCombination(null));

            for (int i = 1; i < NumRounds; i++)
            {

                Rounds.Add(Combinations.GetNextCombination(GetPlayerDemand(i)));
            }
        }

        private List<int> GetPlayerDemand(int RoundIndex)
        {
            List<int> NumPlayed = new List<int>(_NumPlayer);
            for (int i = 0; i < _NumPlayer; i++)
            {
                NumPlayed.Add(0);
            }

            for (int i = 0; i < RoundIndex; i++)
            {
                foreach (int PlayerIndex in Rounds[i].Player)
                {
                    NumPlayed[PlayerIndex]++;
                }
            }

            int max = 0;
            foreach (int p in NumPlayed)
            {
                if (p > max)
                    max = p;
            }

            int num = Rounds[0].Player.Count;
            List<int> result = new List<int>();
            List<int> other = new List<int>();
            List<int> last = new List<int>();

            for (int i = 0; i < NumPlayed.Count; i++)
            {
                if (RoundIndex == 0 || NumPlayed[i] < max)
                {
                    if (RoundIndex == 0 || RoundIndex > 0 && NumPlayed[i] < max && !Rounds[RoundIndex - 1].IsAvailable(i))
                        result.Add(i);
                    else if (NumPlayed[i] < max)
                        other.Add(i);
                }
                else
                {
                    if (RoundIndex > 0)
                    {
                        if (!Rounds[RoundIndex - 1].IsAvailable(i))
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

    class Combinations
    {
        private List<Combination> _Combs;
        private Random _Rand;
        private int _NumPlayer;
        private int _NumMics;

        public Combinations(int NumPlayer, int NumMics, Random Rand)
        {
            _Combs = new List<Combination>();
            _NumMics = NumMics;
            _NumPlayer = NumPlayer;
            _Rand = Rand;            
        }

        public Combination GetNextCombination(List<int> PlayerNrDemand)
        {
            if (_Combs.Count == 0)
                Create();

            Combination combs = new Combination();

            if (_NumPlayer == _NumMics)
            {
                return _Combs[0];
            }

            //filter against PlayerNrDemand
            List<Combination> combsFiltered = new List<Combination>();
            if (PlayerNrDemand != null)
            {
                for (int i = 0; i < _Combs.Count; i++)
                {
                    if (_Combs[i].IsAvailableAll(PlayerNrDemand))
                        combsFiltered.Add(_Combs[i]);
                }
            }

            //1st fallback
            if (PlayerNrDemand != null && combsFiltered.Count == 0)
            {
                for (int i = 0; i < _Combs.Count; i++)
                {
                    if (_Combs[i].IsAvailableSomeone(PlayerNrDemand))
                        combsFiltered.Add(_Combs[i]);
                }
            }

            //2nd fallback
            if (combsFiltered.Count == 0)
            {
                for (int i = 0; i < _Combs.Count; i++)
                {
                    combsFiltered.Add(_Combs[i]);
                }
            }

            int num = combsFiltered.Count;
            int rand = _Rand.Next(num);
            Combination c = new Combination();
            for (int i = 0; i < combsFiltered[rand].Player.Count; i++)
            {
                c.Player.Add(combsFiltered[rand].Player[i]);
            }
            _Combs.Remove(combsFiltered[rand]);

            return combsFiltered[rand];
        }

        private void Create()
        {
            _Combs.Clear();

            int num = (int)Math.Pow(2, _NumPlayer);

            for (int i = 1; i <= num; i++)
            {
                Combination c = new Combination();

                for (int j = 0; j < _NumPlayer; j++)
                {
                    if ((i & (1 << j)) > 0)
                    {
                        c.Player.Add(j);
                    }
                }

                if (c.Player.Count == _NumMics)
                    _Combs.Add(c);
            }
        }
    }

    public class Combination
    {
        public List<int> Player;

        public Combination()
        {
            Player = new List<int>();
        }

        public bool IsAvailable(int PlayerIndex)
        {
            foreach (int p in Player)
            {
                if (p == PlayerIndex)
                    return true;
            }
            return false;
        }

        public bool IsAvailableAll(List<int> PlayerIndices)
        {
            bool result = true;
            foreach (int p in PlayerIndices)
            {
                result &= IsAvailable(p);
            }
            return result;
        }

        public bool IsAvailableSomeone(List<int> PlayerIndices)
        {
            bool result = false;
            foreach (int p in PlayerIndices)
            {
                result |= IsAvailable(p);
            }
            return result;
        }
    }
}
