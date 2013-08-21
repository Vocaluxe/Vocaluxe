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
            get { return (_Notes.Count == 0) ? int.MaxValue : _Notes[0].StartBeat; }
        }

        public int LastNoteBeat
        {
            get { return (_Notes.Count == 0) ? int.MinValue : _Notes[_Notes.Count - 1].EndBeat; }
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
            get { return _Notes.Count > 0 ? _Notes[0] : null; }
        }

        public T LastNote
        {
            get { return _Notes.Count > 0 ? _Notes[_Notes.Count - 1] : null; }
        }

        public int Points
        {
            get { return _Notes.Sum(note => note.Points); }
        }
        public int BaseLine
        {
            get
            {
                int min = int.MaxValue;
                int max = int.MinValue;
                foreach (T note in _Notes)
                {
                    if (note.Tone < min)
                        min = note.Tone;
                    if (note.Tone > max)
                        max = note.Tone;
                }

                return min - (max - min) / 4;
            }
        }
        #endregion Properties

        #region Methods
        //Find last note with StartBeat<=Beat
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

        public bool IncLastNoteLength()
        {
            if (_Notes.Count > 0)
                _Notes[_Notes.Count - 1].Duration++;
            return false;
        }

        public void DeleteAllNotes()
        {
            _Notes.Clear();
        }
        #endregion Methods
    }
}