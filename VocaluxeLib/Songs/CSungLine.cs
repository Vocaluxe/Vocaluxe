using System.Linq;

namespace VocaluxeLib.Songs
{
    public class CSungLine : CLineBase<CSungNote>
    {
        // for drawing perfect line effect
        public bool PerfectLine { get; set; }

        public bool IsPerfect(CSongLine compareLine)
        {
            PerfectLine = _Notes.Count > 0;
            PerfectLine &= _Notes.Count == compareLine.NoteCount;
            PerfectLine &= _Notes.All(note => note.Perfect);
            return PerfectLine;
        }
    }
}