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
using VocaluxeLib;
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
            for (var inv = -1; inv <= 0; inv++)
            {
                var invL = inv;
                // ReSharper disable ObjectCreationAsStatement
                Assert.Throws<ArgumentException>(new Action(() => new CChallengeRounds(invL, 1, 1)));
                Assert.Throws<ArgumentException>(new Action(() => new CChallengeRounds(1, invL, 1)));
                Assert.Throws<ArgumentException>(new Action(() => new CChallengeRounds(1, 1, invL)));
                // ReSharper restore ObjectCreationAsStatement
            }
        }

        [Test]
        public void TestRoundGeneration([Range(1, 20)] int numPlayer, [Range(1, 6)] int numMic)
        {
            CBase.Game = new CBGame();

            var roundFactor = numPlayer % numMic == 0 ? numPlayer / numMic : numPlayer;
            for (var numRounds = roundFactor; numRounds <= 100 && numRounds <= roundFactor * 5; numRounds += roundFactor)
            {
                var rounds = new CChallengeRounds(numRounds, numPlayer, numMic);
                Assert.That(rounds.Count >= numRounds, Is.True);
                _CheckRounds(rounds, numPlayer);
                Assert.That(rounds.Count, Is.GreaterThanOrEqualTo(numRounds),
                    $"Number of rounds should be >= {numRounds}, is {rounds.Count} for {numPlayer}/{numMic}");
            }
        }
        #endregion

        #region Helper methods
        private static void _CheckRounds(CChallengeRounds rounds, int numPlayer)
        {
            var numSongs = new List<int>(numPlayer);
            for (var i = 0; i < numPlayer; i++)
            {
                numSongs.Add(0);
            }

            for (var i = 0; i < rounds.Count; i++)
            {
                foreach (var player in rounds[i].Players)
                {
                    Assert.That(player >= 0 && player < numPlayer, Is.True);
                    numSongs[player]++;
                }
            }

            Assert.That(numSongs.Min() == numSongs.Max(), Is.True, "Some players have more songs than others");
        }
        #endregion
    }
}