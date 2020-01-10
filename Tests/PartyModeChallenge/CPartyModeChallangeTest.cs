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
using System.Linq;
using NUnit.Framework;
using Vocaluxe.Base;
using Vocaluxe.Reporting;
using VocaluxeLib;
using VocaluxeLib.Log;
using VocaluxeLib.PartyModes.Challenge;

namespace Tests.PartyModeChallenge
{
    [TestFixture]
    public class CPartyModeChallangeTest
    {
        #region Tests

        [Test]
        public void TestRoundBoundaries()
        {
            // ReSharper disable ObjectCreationAsStatement
            new CChallengeRounds(1, 1, 1); // Should succeed
            // ReSharper restore ObjectCreationAsStatement
            for (int inv = -1; inv <= 0; inv++)
            {
                int invL = inv;
                // ReSharper disable ObjectCreationAsStatement
                Assert.Throws<ArgumentException>(() => new CChallengeRounds(invL, 1, 1));
                Assert.Throws<ArgumentException>(() => new CChallengeRounds(1, invL, 1));
                Assert.Throws<ArgumentException>(() => new CChallengeRounds(1, 1, invL));
                // ReSharper restore ObjectCreationAsStatement
            }
        }

        [Test]
        public void TestRoundGeneration([Range(1, 20)] int numPlayer, [Range(1, 6)] int numMic)
        {
            CBase.Game = new CBGame();

            int roundFactor = (numPlayer % numMic == 0) ? numPlayer / numMic : numPlayer;
            for (int numRounds = roundFactor; numRounds <= 100 && numRounds <= roundFactor * 5; numRounds += roundFactor)
            {
                CChallengeRounds rounds = new CChallengeRounds(numRounds, numPlayer, numMic);
                Assert.IsTrue(rounds.Count >= numRounds);
                _CheckRounds(rounds, numPlayer);
                Warn.If(rounds.Count != numRounds,
                    $"Number of rounds does not match. Expected: {numRounds}, Is: {rounds.Count} for {numPlayer}/{((numPlayer < numMic) ? numPlayer : numMic)}");
            }
        }

        #endregion

        #region Helper methods
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

        #endregion
    }
}
