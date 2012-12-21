using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Menu;

namespace Vocaluxe.Menu.SingNotes
{
    public class CNotes 
    {
        private List<CLines> _Lines = new List<CLines>();
        
        public CLines[] Lines
        {
            get { return _Lines.ToArray(); }
        }

        public CNotes()
        {
        }

        public CNotes(CNotes notes)
        {
            foreach (CLines lines in notes._Lines)
            {
                _Lines.Add(new CLines(lines));
            }
        }

        public CLines GetLines(int Index)
        {
            while (Index >= _Lines.Count)
            {
                _Lines.Add(new CLines());
            }

            return _Lines[Index];
        }

        public int GetPoints(int Index)
        {
            if (Index >= _Lines.Count)
                return 0;

            return _Lines[Index].Points;
        }

        public int GetNumLinesWithPoints(int Index)
        {
            if (Index >= _Lines.Count)
                return 0;

            return _Lines[Index].NumLinesWithPoints;
        }

        public void AddLines(CLines Lines)
        {
            _Lines.Add(Lines);
        }

        public bool ReplaceLinesAt(int Index, CLines Lines)
        {
            if (Index >= _Lines.Count)
                return false;

            _Lines[Index] = Lines;
            return true;
        }

        public void Reset()
        {
            _Lines.Clear();
        }

        public void SetMedley(int StartBeat, int EndBeat)
        {
            foreach (CLines lines in _Lines)
            {
                lines.SetMedley(StartBeat, EndBeat);
            }
        }
    }

    public class CNote
    {
        private Basic _Base;
        private int _StartBeat;
        private int _Duration;
        private int _Tone;
        private ENoteType _NoteType;
        private string _Text;
        private bool _Hit;              // for drawing player notes
        private bool _Perfect;          // for drawing perfect note effect

        #region Contructors
        public CNote(Basic Base)
        {
            _Base = Base;
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
            _Base = note._Base;
            _StartBeat = note._StartBeat;
            _Duration = note._Duration;
            _Tone = note._Tone;
            _NoteType = note._NoteType;
            _Text = note._Text;
            _Hit = note._Hit;
            _Perfect = note._Perfect;
        }

        public CNote(Basic Base, int StartBeat, int Duration, int Tone, string Text)
            : this(Base)
        {
            this.StartBeat = StartBeat;
            this.Duration = Duration;
            this.Tone = Tone;
            this.NoteType = ENoteType.Normal;
            this.Text = Text;
        }

        public CNote(Basic Base, int StartBeat, int Duration, int Tone, string Text, bool Hit)
            : this(Base)
        {
            this.StartBeat = StartBeat;
            this.Duration = Duration;
            this.Tone = Tone;
            this.NoteType = ENoteType.Normal;
            this.Text = Text;
            this.Hit = Hit;
        }

        public CNote(Basic Base, int StartBeat, int Duration, int Tone, string Text, ENoteType NoteType)
            : this(Base, StartBeat, Duration, Tone, Text)
        {
            this.NoteType = NoteType;
        }

        public CNote(Basic Base, int StartBeat, int Duration, int Tone, string Text, bool Hit, ENoteType NoteType)
            : this(Base, StartBeat, Duration, Tone, Text)
        {
            this.NoteType = NoteType;
            this.Hit = Hit;
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
            get
            {
                return _StartBeat + _Duration - 1;
            }
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
                if ((value >= _Base.Settings.GetToneMin()) && (value <= _Base.Settings.GetToneMax()))
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
        public bool IsPerfect(CNote CompareNote)
        {
            bool Result = true;

            if (!Hit)
                Result = false;

            if (StartBeat != CompareNote.StartBeat)
                Result = false;

            if (EndBeat != CompareNote.EndBeat)
                Result = false;

            if (Tone != CompareNote.Tone)
                Result = false;

            Perfect = Result;
            return Result;
        }
        #endregion Methods
    }

    public class CLine
    {
        private int _StartBeat;
        private int _EndBeat;
        private bool _PerfectLine;      // for drawing perfect line effect
        private bool _VisibleInTimeLine;
        
        private int _MinBeat = int.MaxValue;
        private int _MaxBeat = int.MinValue;
        private List<CNote> _Notes = new List<CNote>();

        #region Constructors
        public CLine()
        {
            StartBeat = int.MinValue;
            EndBeat = int.MaxValue;
            _PerfectLine = false;
            _VisibleInTimeLine = true;
        }

        public CLine(CLine line)
        {
            foreach (CNote note in line._Notes)
            {
                _Notes.Add(new CNote(note));
            }
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
            get
            {
                return _MinBeat;
            }
        }

        public int LastNoteBeat
        {
            get
            {
                return _MaxBeat;
            }
        }

        public string Lyrics
        {
            get
            {
                string lyrics = String.Empty;
                foreach (CNote note in _Notes)
                {
                    lyrics += note.Text;
                }
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
                {
                    points += note.Points;
                }
                return points;
            }
        }
        
        public CNote[] Notes
        {
            get
            {
                return _Notes.ToArray();
            }
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
                int Min = int.MaxValue;
                foreach (CNote note in _Notes)
                {
                    if (note.Tone < Min)
                        Min = note.Tone;
                }

                int Max = int.MinValue;
                foreach (CNote note in _Notes)
                {
                    if (note.Tone > Max)
                        Max = note.Tone;
                }

                return Min - (Max-Min)/4;
            }
        }
        #endregion Properties

        #region Methods
        public bool IsPerfect(CLine CompareLine)
        {
            if (_Notes.Count == 0)
                return false;

            if (_Notes.Count != CompareLine.NoteCount)
                return false;

            if (CompareLine.Points == 0)
                return false;

            _PerfectLine = (this.Points == CompareLine.Points);
            return _PerfectLine;
        }

        public void AddNote(CNote Note)
        {
            _Notes.Add(Note);
            updateMinMaxBeat(Note);
        }

        public bool InsertNote(CNote Note, int Index)
        {
            if (_Notes.Count >= Index)
            {
                _Notes.Insert(Index, Note);
                updateMinMaxBeat(Note);
                return true;
            }
            return false;
        }

        public bool DeleteNote(int Index)
        {
            if (_Notes.Count > Index)
            {
                _Notes.RemoveAt(Index);
                updateMinMaxBeat();
                return true;
            }
            return false;
        }

        public bool ReplaceNote(int Index, CNote Note)
        {
            if (_Notes.Count > Index)
            {
                _Notes.RemoveAt(Index);
                _Notes.Insert(Index, Note);
                updateMinMaxBeat();
                return true;
            }
            return false;
        }

        public bool IncLastNoteLength()
        {
            if (_Notes.Count > 0)
            {
                _Notes[_Notes.Count - 1].Duration++;
                updateMinMaxBeat();
            }
            return false;
        }

        public void DeleteAllNotes()
        {
            _Notes.Clear();
            _MinBeat = int.MaxValue;
            _MaxBeat = int.MinValue;
        }

        public void SetMedley(int StartBeat, int EndBeat)
        {
            foreach (CNote note in _Notes)
            {
                if (note.StartBeat < StartBeat || note.EndBeat > EndBeat)
                    note.NoteType = ENoteType.Freestyle;
            }

            _VisibleInTimeLine = _MinBeat >= StartBeat && _MaxBeat <= EndBeat;
        }

        private void updateMinMaxBeat(CNote Note)
        {
            if (Note.StartBeat < _MinBeat)
            {
                _MinBeat = Note.StartBeat;
            }

            if (Note.EndBeat > _MaxBeat)
            {
                _MaxBeat = Note.EndBeat;
            }
        }

        private void updateMinMaxBeat()
        {
            _MinBeat = int.MaxValue;
            _MaxBeat = int.MinValue;
            foreach (CNote note in _Notes)
            {
                updateMinMaxBeat(note);
            }
        }

        #endregion Methods
    }

    public class CLines
    {
        private List<CLine> _Lines = new List<CLine>();

        public CLines()
        {
        }

        public CLines(CLines lines)
        {
            foreach (CLine line in lines._Lines)
            {
                _Lines.Add(new CLine(line));
            }
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
        /// Total song length in beats
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
                {
                    points += line.Points;
                }
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
        public void AddLine(CLine Line)
        {
            AddLine(Line, true);
        }
        public void AddLine(CLine Line, bool updateTimings)
        {
            _Lines.Add(Line);
            if (updateTimings)
            {
                UpdateTimings();
            }
        }

        public bool InsertLine(CLine Line, int Index)
        {
            if (_Lines.Count >= Index)
            {
                _Lines.Insert(Index, Line);
                UpdateTimings();
                return true;
            }
            return false;
        }

        public bool DeleteLine(int Index)
        {
            if (_Lines.Count > Index)
            {
                _Lines.RemoveAt(Index);
                UpdateTimings();
                return true;
            }
            return false;
        }

        public void DeleteAllLines()
        {
            _Lines.Clear();
        }

        public bool AddNote(CNote Note, int LineIndex, bool updateTimings)
        {
            if (_Lines.Count > LineIndex)
            {
                _Lines[LineIndex].AddNote(Note);
                if (updateTimings)
                {
                    UpdateTimings();
                }
                return true;
            }
            return false;
        }
        public bool AddNote(CNote Note, int LineIndex)
        {
            return AddNote(Note, LineIndex, true);
        }

        public bool InsertNote(CNote Note, int LineIndex, int NoteIndex)
        {
            if (_Lines.Count > LineIndex)
            {
                bool res = _Lines[LineIndex].InsertNote(Note, NoteIndex);
                UpdateTimings();
                return res;
            }
            return false;
        }
        public void UpdateTimings()
        {
            CNote LastNote, FirstNote;
            int min, max, s;

            if (_Lines.Count > 0)
            {
               _Lines[0].StartBeat = -10000;
            }

            for (int i = 1; i < _Lines.Count; i++)
            {
                LastNote = _Lines[i - 1].LastNote;
                FirstNote = _Lines[i].FirstNote;

                if ((LastNote != null) && (FirstNote != null))
                {
                    min = LastNote.EndBeat;
                    max = FirstNote.StartBeat;

                    s = 0;
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

            LastNote = _Lines[_Lines.Count - 1].LastNote;
            if (LastNote != null)
            {
                _Lines[_Lines.Count - 1].EndBeat = LastNote.EndBeat;
            }
        }

        public void SetMedley(int StartBeat, int EndBeat)
        {
            foreach (CLine line in _Lines)
            {
                line.SetMedley(StartBeat, EndBeat);
            }
        }
        #endregion Methods
    }
}
