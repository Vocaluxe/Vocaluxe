namespace VocaluxeLib.Songs
{
    /// <summary>
    /// Base class for all note types
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