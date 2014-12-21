using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.PartyModes.Challenge;

namespace VocaluxeTests
{
    [TestClass]
    public class CPartyModeTests
    {
        [TestMethod]
        public void TestRoundBoundaries()
        {
            // ReSharper disable ObjectCreationAsStatement
            new CChallengeRounds(1, 1, 1); // Should succeed
            // ReSharper restore ObjectCreationAsStatement
            for (int inv = -1; inv <= 0; inv++)
            {
                int invL = inv;
                CTestHelpers.AssertFail<ArgumentException>(() => new CChallengeRounds(invL, 1, 1));
                CTestHelpers.AssertFail<ArgumentException>(() => new CChallengeRounds(1, invL, 1));
                CTestHelpers.AssertFail<ArgumentException>(() => new CChallengeRounds(1, 1, invL));
            }
        }

        private static void _CheckRounds(CChallengeRounds rounds, int numPlayer)
        {
            List<int> numSongs = new List<int>(numPlayer);
            for (int i = 0; i < numPlayer; i++)
                numSongs.Add(0);
            for (int i = 0; i < rounds.Count; i++)
            {
                foreach (int player in rounds[i].Players)
                {
                    Assert.IsTrue(player >= 0 && player < numPlayer);
                    numSongs[player]++;
                }
            }
            Assert.IsTrue(numSongs.Min() == numSongs.Max(), "Some players have more songs than others");
        }

        [TestMethod]
        public void TestRoundGeneration()
        {
            CBase.Game = new CBGame();
            CLog.Init();
            CBase.Log = new CBlog();
            for (int i = 0; i < 10; i++)
            {
                for (int numPlayer = 1; numPlayer <= 20; numPlayer++)
                {
                    for (int numMic = 1; numMic <= 6; numMic++)
                    {
                        int roundFactor = (numPlayer % numMic == 0) ? numPlayer / numMic : numPlayer;
                        for (int numRounds = roundFactor; numRounds <= 100 && numRounds <= roundFactor * 5; numRounds += roundFactor)
                        {
                            CChallengeRounds rounds = new CChallengeRounds(numRounds, numPlayer, numMic);
                            Assert.IsTrue(rounds.Count >= numRounds);
                            _CheckRounds(rounds, numPlayer);
                            if (rounds.Count != numRounds)
                                CBase.Log.LogDebug("Number of rounds does not match. Expected: " + numRounds + ", Is: " + rounds.Count + " for " + numPlayer + "/" +
                                                   ((numPlayer < numMic) ? numPlayer : numMic));
                        }
                    }
                }
            }
        }
    }
}