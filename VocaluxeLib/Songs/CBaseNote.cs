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

namespace VocaluxeLib.Songs
{
    /// <summary>
    ///     Base class for all note types
    /// </summary>
    public abstract class CBaseNote
    {
        private int _Duration = 1;
        private int _Tone;

        #region Contructors
        protected CBaseNote(CBaseNote note)
        {
            StartBeat = note.StartBeat;
            _Duration = note._Duration;
            _Tone = note._Tone;
        }

        protected CBaseNote(int startBeat, int duration, int tone)
        {
            StartBeat = startBeat;
            Duration = duration;
            Tone = tone;
            if (Duration < 1)
                throw new Exception("Note to short. All notes should be at least 1 beat long!");
        }
        #endregion Constructors

        #region Properties
        public int StartBeat { get; set; }

        public int EndBeat
        {
            get { return StartBeat + _Duration - 1; }
        }

        public int Duration
        {
            get { return _Duration; }
            set
            {
                if (value > 0)
                    _Duration = value;
            }
        }

        public int Tone
        {
            get { return _Tone; }
            set
            {
                if ((value >= CBase.Settings.GetToneMin()) && (value <= CBase.Settings.GetToneMax()))
                    _Tone = value;
            }
        }

        public int Points
        {
            get { return PointsForBeat * Duration; }
        }

        public abstract int PointsForBeat { get; }
        #endregion Properties
    }
}