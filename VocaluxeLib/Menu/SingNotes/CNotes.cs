using System;
using System.Collections.Generic;

namespace VocaluxeLib.Menu.SingNotes
{
    public class CNotes
    {
        private readonly List<CLines> _Lines = new List<CLines>();

        public CLines[] Lines
        {
            get { return _Lines.ToArray(); }
        }

        public CNotes() {}

        public int LinesCount
        {
            get { return _Lines.Count; }
        }

        public CNotes(CNotes notes)
        {
            foreach (CLines lines in notes._Lines)
                _Lines.Add(new CLines(lines));
        }

        public CLines GetLines(int index)
        {
            while (index >= _Lines.Count)
                _Lines.Add(new CLines());

            return _Lines[index];
        }

        public int GetPoints(int index)
        {
            if (index >= _Lines.Count)
                return 0;

            return _Lines[index].Points;
        }

        public int GetNumLinesWithPoints(int index)
        {
            if (index >= _Lines.Count)
                return 0;

            return _Lines[index].NumLinesWithPoints;
        }

        public void AddLines(CLines lines)
        {
            _Lines.Add(lines);
        }

        public bool ReplaceLinesAt(int index, CLines lines)
        {
            if (index >= _Lines.Count)
                return false;

            _Lines[index] = lines;
            return true;
        }

        public void Reset()
        {
            _Lines.Clear();
        }

        public void SetMedley(int startBeat, int endBeat)
        {
            foreach (CLines lines in _Lines)
                lines.SetMedley(startBeat, endBeat);
        }
    }

    public class CNote
    {
        private int _StartBeat;
        private int _Duration;
        private int _Tone;
        private ENoteType _NoteType;
        private string _Text;
        private bool _Hit; // for drawing player notes
        private bool _Perfect; // for drawing perfect note effect

        #region Contructors
        public CNote()
        {
            StartBeat = 0;
            Duration = 1;
            Tone = 0;
            NoteType = ENoteType.Normal;
            Text = String.Empty;
            _Hit = false;
            _Perfect = false;
        }

        public CNote(CNote note)
        {
            _StartBeat = note._StartBeat;
            _Duration = note._Duration;
            _Tone = note._Tone;
            _NoteType = note._NoteType;
            _Text = note._Text;
            _Hit = note._Hit;
            _Perfect = note._Perfect;
        }

        public CNote(int startBeat, int duration, int tone, string text)
            : this()
        {
            this.StartBeat = startBeat;
            this.Duration = duration;
            this.Tone = tone;
            NoteType = ENoteType.Normal;
            this.Text = text;
        }

        public CNote(int startBeat, int duration, int tone, string text, bool hit)
            : this()
        {
            this.StartBeat = startBeat;
            this.Duration = duration;
            this.Tone = tone;
            NoteType = ENoteType.Normal;
            this.Text = text;
            this.Hit = hit;
        }

        public CNote(int startBeat, int duration, int tone, string text, ENoteType noteType)
            : this(startBeat, duration, tone, text)
        {
            this.NoteType = noteType;
        }

        public CNote(int startBeat, int duration, int tone, string text, bool hit, ENoteType noteType)
            : this(startBeat, duration, tone, text)
        {
            this.NoteType = noteType;
            this.Hit = hit;
        }
        #endregion Constructors

        #region Properties
        public bool Hit
        {
            get { return _Hit; }
            set { _Hit = value; }
        }

        public bool Perfect
        {
            get { return _Perfect; }
            set { _Perfect = value; }
        }

        public int StartBeat
        {
            get { return _StartBeat; }
            set { _StartBeat = value; }
        }

        public int EndBeat
        {
            get { return _StartBeat + _Duration - 1; }
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

        public ENoteType NoteType
        {
            get { return _NoteType; }
            set { _NoteType = value; }
        }

        public string Text
        {
            get { return _Text; }
            set { _Text = value; }
        }

        public int Points
        {
            get
            {
                switch (_NoteType)
                {
                    case ENoteType.Normal:
                        return _Duration;
                    case ENoteType.Golden:
                        return _Duration * 2;
                    case ENoteType.Freestyle:
                        return 0;
                    default:
                        return 0;
                }
            }
        }

        public int PointsForBeat
        {
            get
            {
                switch (_NoteType)
                {
                    case ENoteType.Normal:
                        return 1;
                    case ENoteType.Golden:
                        return 2;
                    case ENoteType.Freestyle:
                        return 0;
                    default:
                        return 0;
                }
            }
        }
        #endregion Properties

        #region Methods
        public bool IsPerfect(CNote compareNote)
        {
            bool result = true;

            if (!Hit)
                result = false;

            if (StartBeat != compareNote.StartBeat)
                result = false;

            if (EndBeat != compareNote.EndBeat)
                result = false;

            if (Tone != compareNote.Tone)
                result = false;

            Perfect = result;
            return result;
        }
        #endregion Methods
    }

    public class CLine
    {
        private int _StartBeat = int.MinValue;
        private int _EndBeat = int.MinValue;
        private bool _PerfectLine; // for drawing perfect line effect
        private bool _VisibleInTimeLine = true;

        private int _MinBeat = int.MaxValue;
        private int _MaxBeat = int.MinValue;
        private readonly List<CNote> _Notes = new List<CNote>();

        #region Constructors
        public CLine() {}

        public CLine(CLine line)
        {
            foreach (CNote note in line._Notes)
                _Notes.Add(new CNote(note));
            _StartBeat = line._StartBeat;
            _EndBeat = line._EndBeat;
            _PerfectLine = line._PerfectLine;
            _MinBeat = line._MinBeat;
            _MaxBeat = line._MaxBeat;
            _VisibleInTimeLine = line._VisibleInTimeLine;
        }
        #endregion Constructors

        #region Properties
        public bool VisibleInTimeLine
        {
            get { return _VisibleInTimeLine; }
            set { _VisibleInTimeLine = value; }
        }

        public int StartBeat
        {
            get { return _StartBeat; }
            set
            {
                if (value <= _MinBeat)
                    _StartBeat = value;
            }
        }

        public int EndBeat
        {
            get { return _EndBeat; }
            set
            {
                if (value >= _MaxBeat)
                    _EndBeat = value;
            }
        }

        public bool PerfectLine
        {
            get { return _PerfectLine; }
            set { _PerfectLine = value; }
        }

        public int FirstNoteBeat
        {
            get { return _MinBeat; }
        }

        public int LastNoteBeat
        {
            get { return _MaxBeat; }
        }

        public string Lyrics
        {
            get
            {
                string lyrics = String.Empty;
                foreach (CNote note in _Notes)
                    lyrics += note.Text;
                return lyrics;
            }
        }

        public int NoteCount
        {
            get { return _Notes.Count; }
        }

        public int Points
        {
            get
            {
                int points = 0;
                foreach (CNote note in _Notes)
                    points += note.Points;
                return points;
            }
        }

        public CNote[] Notes
        {
            get { return _Notes.ToArray(); }
        }

        public CNote FirstNote
        {
            get
            {
                if (_Notes.Count > 0)
                    return _Notes[0];
                else
                    return null;
            }
        }

        public CNote LastNote
        {
            get
            {
                if (_Notes.Count > 0)
                    return _Notes[_Notes.Count - 1];
                else
                    return null;
            }
        }

        public int BaseLine
        {
            get
            {
                int min = int.MaxValue;
                int max = int.MinValue;
                foreach (CNote note in _Notes)
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
        public bool IsPerfect(CLine compareLine)
        {
            if (_Notes.Count == 0)
                return false;

            if (_Notes.Count != compareLine.NoteCount)
                return false;

            if (compareLine.Points == 0)
                return false;

            _PerfectLine = Points == compareLine.Points;
            return _PerfectLine;
        }

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

        public bool AddNote(CNote note)
        {
            if (_Notes.Count == 0)
                _Notes.Add(note);
            else
            {
                int insPos = FindPreviousNote(note.StartBeat);
                //Flamefire: May be a performance hit as notes are also used for sung notes
                //But for song loading this might be very helpful!
                //Check for overlapping notes
                /*if (insPos >= 0 && _Notes[insPos].EndBeat > Note.StartBeat)
                    return false;
                if (insPos < _Notes.Count - 1 && _Notes[insPos+1].StartBeat > Note.EndBeat)
                    return false;
                 * */
                _Notes.Insert(insPos + 1, note);
            }
            _UpdateMinMaxBeat(note);
            return true;
        }

        public bool DeleteNote(int index)
        {
            if (_Notes.Count > index)
            {
                _Notes.RemoveAt(index);
                if (index == 0 || index == _Notes.Count - 1)
                    _UpdateMinMaxBeat();
                return true;
            }
            return false;
        }

        public bool ReplaceNote(int index, CNote note)
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
            {
                _Notes[_Notes.Count - 1].Duration++;
                _MaxBeat++;
            }
            return false;
        }

        public void DeleteAllNotes()
        {
            _Notes.Clear();
            _UpdateMinMaxBeat();
        }

        public void SetMedley(int startBeat, int endBeat)
        {
            foreach (CNote note in _Notes)
            {
                if (note.StartBeat < startBeat || note.EndBeat > endBeat)
                    note.NoteType = ENoteType.Freestyle;
            }

            _VisibleInTimeLine = _MinBeat >= startBeat && _MaxBeat <= endBeat;
        }

        private void _UpdateMinMaxBeat(CNote note)
        {
            if (note.StartBeat < _MinBeat)
                _MinBeat = note.StartBeat;

            if (note.EndBeat > _MaxBeat)
                _MaxBeat = note.EndBeat;
        }

        private void _UpdateMinMaxBeat()
        {
            if (_Notes.Count > 0)
            {
                _MinBeat = _Notes[0].StartBeat;
                _MaxBeat = _Notes[_Notes.Count - 1].EndBeat;
            }
            else
            {
                _MinBeat = int.MaxValue;
                _MaxBeat = int.MinValue;
            }
        }
        #endregion Methods
    }

    public class CLines
    {
        private readonly List<CLine> _Lines = new List<CLine>();

        public CLines() {}

        public CLines(CLines lines)
        {
            foreach (CLine line in lines._Lines)
                _Lines.Add(new CLine(line));
        }

        public CLine[] Line
        {
            get { return _Lines.ToArray(); }
        }
        public int LineCount
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

                int startbeat = int.MaxValue;
                for (int i = 0; i < _Lines.Count; i++)
                {
                    if (_Lines[i].FirstNoteBeat < startbeat)
                        startbeat = _Lines[i].FirstNoteBeat;
                }

                int endbeat = int.MinValue;
                for (int i = 0; i < _Lines.Count; i++)
                {
                    if (_Lines[i].LastNoteBeat < endbeat)
                        endbeat = _Lines[i].LastNoteBeat;
                }

                int result = endbeat - startbeat;
                if (result > 0)
                    return result;

                return 0;
            }
        }

        public int Points
        {
            get
            {
                int points = 0;
                foreach (CLine line in _Lines)
                    points += line.Points;
                return points;
            }
        }

        public int NumLinesWithPoints
        {
            get
            {
                int num = 0;
                foreach (CLine line in _Lines)
                {
                    if (line.Points > 0f)
                        num++;
                }
                return num;
            }
        }

        #region Methods
        //Find last line with StartBeat<=Beat
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

        public void AddLine(CLine line)
        {
            if (_Lines.Count == 0)
                _Lines.Add(line);
            else
            {
                int insPos = FindPreviousLine(line.StartBeat);
                _Lines.Insert(insPos + 1, line);
            }
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

        public bool AddNote(CNote note, bool updateTimings = false)
        {
            int lineIndex = FindPreviousLine(note.StartBeat);
            if (lineIndex < 0)
            {
                //Note is before ALL lines
                CLine line = new CLine();
                line.StartBeat = note.StartBeat;
                line.AddNote(note);
                _Lines.Insert(0, line);
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

        public bool InsertNote(CNote note, int lineIndex, bool updateTimings = false)
        {
            if (_Lines.Count > lineIndex && _Lines[lineIndex].StartBeat <= note.StartBeat)
            {
                _Lines[lineIndex].AddNote(note);
                if (updateTimings)
                    UpdateTimings();
                return true;
            }
            return false;
        }

        public void UpdateTimings()
        {
            CNote lastNote;
            int min, max, s;

            if (_Lines.Count > 0)
                _Lines[0].StartBeat = -10000;

            for (int i = 1; i < _Lines.Count; i++)
            {
                lastNote = _Lines[i - 1].LastNote;
                CNote firstNote = _Lines[i].FirstNote;

                if ((lastNote != null) && (firstNote != null))
                {
                    min = lastNote.EndBeat;
                    max = firstNote.StartBeat;

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
            }

            lastNote = _Lines[_Lines.Count - 1].LastNote;
            if (lastNote != null)
                _Lines[_Lines.Count - 1].EndBeat = lastNote.EndBeat;
        }

        public void SetMedley(int startBeat, int endBeat)
        {
            foreach (CLine line in _Lines)
                line.SetMedley(startBeat, endBeat);
        }
        #endregion Methods
    }
}