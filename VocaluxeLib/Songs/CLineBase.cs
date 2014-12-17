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

using System.Collections.Generic;
using System.Linq;

namespace VocaluxeLib.Songs
{
    public class CLineBase<T> where T : CBaseNote
    {
        protected readonly List<T> _Notes = new List<T>();

        #region Properties
        public int FirstNoteBeat
        {
            get { return (NoteCount == 0) ? int.MaxValue : _Notes[0].StartBeat; }
        }

        public int LastNoteBeat
        {
            get { return (NoteCount == 0) ? int.MinValue : _Notes[NoteCount - 1].EndBeat; }
        }

        public int NoteCount
        {
            get { return _Notes.Count; }
        }

        public T[] Notes
        {
            get { return _Notes.ToArray(); }
        }

        public T FirstNote
        {
            get { return NoteCount > 0 ? _Notes[0] : null; }
        }

        public T LastNote
        {
            get { return NoteCount > 0 ? _Notes[NoteCount - 1] : null; }
        }

        public int Points
        {
            get { return _Notes.Sum(note => note.Points); }
        }
        public int BaseLine
        {
            get
            {
                if (_Notes.Count == 0)
                    return 0;
                int min = _Notes.Min(note => note.Tone);
                int max = _Notes.Max(note => note.Tone);

                return min - (max - min) / 4;
            }
        }
        #endregion Properties

        #region Methods
        /// <summary>
        ///     Find last note with StartBeat &lt;= beat
        /// </summary>
        /// <param name="beat"></param>
        /// <returns>Note index of last note with StartBeat &lt;= beat or -1 if all notes start after beat</returns>
        public int FindPreviousNote(int beat)
        {
            //If no notes -> No previous note
            if (_Notes.Count == 0)
                return -1;
            int start = 0;
            int end = _Notes.Count - 1;
            //Ensure that start.StartBeat<=Beat && end.StartBeat>Beat
            if (_Notes[0].StartBeat > beat)
                return -1;
            if (_Notes[end].StartBeat <= beat)
                return end;
            //Binary search
            while (end - start > 1)
            {
                int mid = (start + end) / 2;
                if (_Notes[mid].StartBeat <= beat)
                    start = mid;
                else
                    end = mid;
            }
            return start;
        }

        public virtual bool AddNote(T note)
        {
            if (_Notes.Count == 0)
                _Notes.Add(note);
            else
            {
                int insPos = FindPreviousNote(note.StartBeat);
                _Notes.Insert(insPos + 1, note);
            }
            return true;
        }

        public bool DeleteNote(int index)
        {
            if (_Notes.Count > index)
            {
                _Notes.RemoveAt(index);
                return true;
            }
            return false;
        }

        public bool ReplaceNote(int index, T note)
        {
            if (_Notes.Count > index)
            {
                _Notes.RemoveAt(index);
                return AddNote(note);
            }
            return false;
        }

        public void IncLastNoteLength()
        {
            if (_Notes.Count > 0)
                _Notes[NoteCount - 1].Duration++;
        }

        public void DeleteAllNotes()
        {
            _Notes.Clear();
        }
        #endregion Methods
    }
}