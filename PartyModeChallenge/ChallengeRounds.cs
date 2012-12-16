using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.PartyModes
{
    public class ChallengeRounds
    {
        private List<ChallengeRound> _Rounds;

        public ChallengeRounds(int NumRounds, int NumPlayer, int NumPlayerAtOnce)
        {
            BuildRounds(NumRounds, NumPlayer, NumPlayerAtOnce);
        }

        public ChallengeRound GetRound(int RoundNr)
        {
            return null;
        }

        private void BuildRounds(int NumRounds, int NumPlayer, int NumPlayerAtOnce)
        {
            _Rounds = new List<ChallengeRound>();

            if (NumPlayerAtOnce < 1 || NumPlayerAtOnce > 6 || NumPlayer < 1 || NumRounds < 1)
                return;

            switch (NumPlayerAtOnce)
            {
                case 1:
                    for (int i = 0; i < NumRounds; i++)
                    {
                        
                    }
                    break;

                case 2:
                    break;

                case 3:
                    break;

                case 4:
                    break;

                case 5:
                    break;

                case 6:
                    break;

                default:
                    break;
            }
        }

        
    }

    public class ChallengeRound
    {
        private List<int> _ProfileIDs;

        public int NumPlayer { get{ return _ProfileIDs.Count; } }


        public ChallengeRound()
        {
            _ProfileIDs = new List<int>();
        }

        public void AddPlayer(int ProfileID)
        {
            _ProfileIDs.Add(ProfileID);
        }

        public int GetPlayer(int Nr)
        {
            if (Nr >= _ProfileIDs.Count)
                return -1;

            return _ProfileIDs[Nr];
        }
    }
}
