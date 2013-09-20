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
using System.Linq;

namespace VocaluxeLib.Songs
{
    public class CSongLine : CLineBase<CSongNote>
    {
        private int _StartBeat = int.MinValue;
        private int _EndBeat = int.MinValue;

        #region Constructors
        public CSongLine()
        {
            VisibleInTimeLine = true;
        }

        public CSongLine(CSongLine line)
        {
            foreach (CSongNote note in line._Notes)
                _Notes.Add(new CSongNote(note));
            _StartBeat = line._StartBeat;
            _EndBeat = line._EndBeat;
            VisibleInTimeLine = line.VisibleInTimeLine;
        }
        #endregion Constructors

        #region Properties
        public bool VisibleInTimeLine { get; set; }

        public int StartBeat
        {
            get { return _StartBeat; }
            set
            {
                if (value <= FirstNoteBeat)
                    _StartBeat = value;
            }
        }

        public int EndBeat
        {
            get { return _EndBeat; }
            set
            {
                if (value >= LastNoteBeat)
                    _EndBeat = value;
            }
        }

        public string Lyrics
        {
            get { return _Notes.Aggregate(String.Empty, (current, note) => current + note.Text); }
        }
        #endregion Properties

        #region Methods
        public override bool AddNote(CSongNote note)
        {
            if (_Notes.Count == 0)
                _Notes.Add(note);
            else
            {
                int insPos = FindPreviousNote(note.StartBeat);
                //Check for overlapping notes
                if (insPos >= 0 && _Notes[insPos].EndBeat > note.StartBeat)
                    return false;
                if (insPos < _Notes.Count - 1 && _Notes[insPos + 1].StartBeat > note.EndBeat)
                    return false;
                _Notes.Insert(insPos + 1, note);
            }
            return true;
        }

        public void SetMedley(int startBeat, int endBeat)
        {
            foreach (CSongNote note in _Notes)
            {
                if (note.StartBeat < startBeat || note.EndBeat > endBeat)
                    note.NoteType = ENoteType.Freestyle;
            }

            VisibleInTimeLine = FirstNoteBeat >= startBeat && LastNoteBeat <= endBeat;
        }
        #endregion Methods
    }
}