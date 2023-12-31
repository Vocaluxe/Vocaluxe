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

using System.Linq;

namespace VocaluxeLib.Songs
{
    /// <summary>
    ///     Note that was sung by a player
    /// </summary>
    public class CSungNote : CBaseNote
    {
        /// <summary>
        ///     Constructor for a sung note that was not hit
        /// </summary>
        /// <param name="startBeat"></param>
        /// <param name="duration"></param>
        /// <param name="tone"></param>
        public CSungNote(int startBeat, int duration, int tone) : base(startBeat, duration, tone)
        {
            Perfect = false;
            HitNote = null;
        }

        /// <summary>
        ///     Constructor for a sung note that hit the given note
        /// </summary>
        /// <param name="startBeat"></param>
        /// <param name="duration"></param>
        /// <param name="tone"></param>
        /// <param name="hitNote"></param>
        /// <param name="points"></param>
        public CSungNote(int startBeat, int duration, int tone, CSongNote hitNote, double points) : this(startBeat, duration, tone)
        {
            HitNote = hitNote;
            Points = points;
        }

        #region Properties
        public new double Points;
        // for drawing player notes
        public bool Hit
        {
            get { return HitNote != null; }
        }
        public CSongNote HitNote { get; private set; }
        // for drawing perfect note effect
        public bool Perfect { get; set; }
        public bool PerfectDrawn { get; set; }
        #endregion

        #region Methods
        public void CheckPerfect()
        {
            bool result = HitNote != null;
            if (result)
            {
                result = (StartBeat == HitNote.StartBeat);
                result &= (EndBeat == HitNote.EndBeat);
                result &= (Tone == HitNote.Tone);
            }

            Perfect = result;
        }
        public override bool IsNoteType(params ENoteType[] noteTypes)
        {
            return HitNote != null && noteTypes.Contains(HitNote.Type);
        }
        #endregion Methods

        public override int PointsForBeat
        {
            get { return (HitNote == null) ? 0 : HitNote.PointsForBeat; }
        }
    }
}