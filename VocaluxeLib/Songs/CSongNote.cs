namespace VocaluxeLib.Songs
{
    /// <summary>
    /// Class to represent a note in a song
    /// </summary>
    public class CSongNote : CBaseNote
    {
        #region Contructors
        public CSongNote(CSongNote note) : base(note)
        {
            NoteType = note.NoteType;
            Text = note.Text;
        }

        public CSongNote(int startBeat, int duration, int tone, string text, ENoteType noteType)
            : base(startBeat, duration, tone)
        {
            NoteType = noteType;
            Text = text;
        }
        #endregion Constructors

        #region Properties
        public ENoteType NoteType { get; set; }

        public string Text { get; set; }

        public override int PointsForBeat
        {
            get
            {
                switch (NoteType)
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
    }
}