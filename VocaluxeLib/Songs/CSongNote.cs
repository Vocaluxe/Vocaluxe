#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

namespace VocaluxeLib.Songs
{
    /// <summary>
    ///     Class to represent a note in a song
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