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

namespace VocaluxeLib.Songs
{
    public class CVoice
    {
        private readonly List<CSongLine> _Lines = new List<CSongLine>();

        public CVoice() {}

        public CVoice(CVoice voice)
        {
            foreach (CSongLine line in voice._Lines)
                _Lines.Add(new CSongLine(line));
        }

        public CSongLine[] Lines
        {
            get { return _Lines.ToArray(); }
        }
        public int NumLines
        {
            get { return _Lines.Count; }
        }

        /// <summary>
        ///     Total song length in beats
        /// </summary>
        public int Length
        {
            get
            {
                if (_Lines.Count == 0)
                    return 0;

                int result = _Lines[_Lines.Count - 1].EndBeat - _Lines[0].StartBeat;
                return result > 0 ? result : 0;
            }
        }

        public int Points
        {
            get { return _Lines.Sum(line => line.Points); }
        }

        public int NumLinesWithPoints
        {
            get { return _Lines.Count(line => line.Points > 0f); }
        }

        #region Methods
        /// <summary>
        /// Find last line with StartBeat<=Beat
        /// </summary>
        /// <param name="beat"></param>
        /// <returns></returns>
        public int FindPreviousLine(int beat)
        {
            //If no line -> No previous line
            if (_Lines.Count == 0)
                return -1;
            int start = 0;
            int end = _Lines.Count - 1;
            //Ensure that start.StartBeat<=Beat && end.StartBeat>Beat
            if (_Lines[0].StartBeat > beat)
                return -1;
            if (_Lines[end].StartBeat <= beat)
                return end;
            //Binary search
            while (end - start > 1)
            {
                int mid = (start + end) / 2;
                if (_Lines[mid].StartBeat <= beat)
                    start = mid;
                else
                    end = mid;
            }
            return start;
        }

        public bool AddLine(CSongLine line, bool updateTimings = true)
        {
            if (_Lines.Count == 0)
                _Lines.Add(line);
            else
            {
                int insPos = FindPreviousLine(line.StartBeat);
                //Check if previous line ends before this one
                if (insPos >= 0 && _Lines[insPos].LastNoteBeat > line.StartBeat)
                    return false;
                //Check if next line starts before this one ends
                if (insPos + 1 < _Lines.Count && _Lines[insPos + 1].FirstNoteBeat < line.LastNoteBeat)
                    return false;
                _Lines.Insert(insPos + 1, line);
            }
            if (updateTimings)
                UpdateTimings();
            return true;
        }

        public bool AddLine(int startBeat)
        {
            int insPos = FindPreviousLine(startBeat);
            //Check for actual notes (startbeat may be lower than first note)
            while (insPos >= 0 && _Lines[insPos].NoteCount > 0 && _Lines[insPos].FirstNoteBeat > startBeat)
                insPos--;
            CSongLine line = new CSongLine {StartBeat = startBeat};
            if (insPos >= 0)
            {
                CSongLine prevLine = _Lines[insPos];
                //We already have a line break here
                if (prevLine.StartBeat == startBeat && prevLine.FirstNoteBeat == startBeat)
                    return false;
                //Maybe we have to split the previous line
                while (prevLine.NoteCount > 0 && prevLine.LastNote.StartBeat >= startBeat)
                {
                    //throw new Exception("This should not happen on song loading!");
                    line.AddNote(prevLine.LastNote);
                    prevLine.DeleteNote(prevLine.NoteCount - 1);
                }
            }
            _Lines.Insert(insPos + 1, line);
            return true;
        }

        public bool DeleteLine(int index)
        {
            if (_Lines.Count > index)
            {
                _Lines.RemoveAt(index);
                UpdateTimings();
                return true;
            }
            return false;
        }

        public void DeleteAllLines()
        {
            _Lines.Clear();
        }

        public bool AddNote(CSongNote note, bool updateTimings = true)
        {
            int lineIndex = FindPreviousLine(note.StartBeat);
            if (lineIndex + 1 < _Lines.Count && _Lines[lineIndex + 1].FirstNoteBeat < note.EndBeat) //First note in next line starts before this one ends
                return false;
            if (lineIndex < 0)
            {
                //Note is before ALL lines
                if (_Lines.Count > 0)
                {
                    //Add to first line
                    if (!_Lines[0].AddNote(note))
                        return false;
                }
                else
                {
                    var line = new CSongLine();
                    line.AddNote(note);
                    _Lines.Add(line);
                }
            }
            else
            {
                if (!_Lines[lineIndex].AddNote(note))
                    return false;
            }
            if (updateTimings)
                UpdateTimings();
            return true;
        }

        /*Check this first for consistency (next line might be affected)
         * public bool InsertNote(CSongNote note, int lineIndex, bool updateTimings = false)
        {
            if (_Lines.Count > lineIndex && _Lines[lineIndex].StartBeat <= note.StartBeat)
            {
                if (!_Lines[lineIndex].AddNote(note))
                    return false;
                if (updateTimings)
                    UpdateTimings();
                return true;
            }
            return false;
        }*/

        public void UpdateTimings()
        {
            CSongNote lastNote;

            if (_Lines.Count == 0)
                return;
            _Lines[0].StartBeat = -10000;

            for (int i = 1; i < _Lines.Count; i++)
            {
                lastNote = _Lines[i - 1].LastNote;
                CSongNote firstNote = _Lines[i].FirstNote;

                if ((lastNote != null) && (firstNote != null))
                {
                    int min = lastNote.EndBeat;
                    int max = firstNote.StartBeat;

                    int s;
                    switch (max - min)
                    {
                        case 0:
                            s = max;
                            break;
                        case 1:
                            s = max;
                            break;
                        case 2:
                            s = max - 1;
                            break;
                        case 3:
                            s = max - 2;
                            break;
                        default:
                            s = min + 2;
                            break;
                    }

                    _Lines[i].StartBeat = s;
                    _Lines[i - 1].EndBeat = min;
                }
                else if (firstNote != null)
                {
                    _Lines[i - 1].EndBeat = Math.Min(_Lines[i - 1].StartBeat, firstNote.StartBeat - 2);
                    _Lines[i].StartBeat = Math.Max(_Lines[i - 1].EndBeat, firstNote.StartBeat - 2);
                }
                else if (lastNote != null)
                {
                    _Lines[i - 1].EndBeat = lastNote.EndBeat;
                    _Lines[i].StartBeat = Math.Max(lastNote.EndBeat, _Lines[i].StartBeat); //Prefer current setting
                }
                else
                {
                    //No note
                    _Lines[i - 1].EndBeat = _Lines[i - 1].StartBeat; //Assume at least startbeats were set right
                }
            }

            _Lines[_Lines.Count - 1].EndBeat = Math.Max(_Lines[_Lines.Count - 1].LastNoteBeat, _Lines[_Lines.Count - 1].StartBeat); //Use note or (when not set) start beat
        }

        public void SetMedley(int startBeat, int endBeat)
        {
            foreach (CSongLine line in _Lines)
                line.SetMedley(startBeat, endBeat);
        }
        #endregion Methods
    }
}