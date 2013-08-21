namespace VocaluxeLib.Songs
{
    /// <summary>
    /// Note that was sung by a player
    /// </summary>
    public class CSungNote : CBaseNote
    {
        /// <summary>
        /// Constructor for a sung note that was not hit
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
        /// Constructor for a sung note that hit the given note
        /// </summary>
        /// <param name="startBeat"></param>
        /// <param name="duration"></param>
        /// <param name="tone"></param>
        /// <param name="hitNote"></param>
        public CSungNote(int startBeat, int duration, int tone, CSongNote hitNote) : this(startBeat, duration, tone)
        {
            HitNote = hitNote;
        }

        #region Properties
        // for drawing player notes
        public bool Hit
        {
            get { return HitNote != null; }
        }
        public CSongNote HitNote { get; set; }
        // for drawing perfect note effect
        public bool Perfect { get; set; }
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
        #endregion Methods

        public override int PointsForBeat
        {
            get { return (HitNote == null) ? 0 : HitNote.PointsForBeat; }
        }
    }
}