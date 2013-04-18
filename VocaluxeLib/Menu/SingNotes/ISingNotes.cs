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

using System.Collections.Generic;

namespace VocaluxeLib.Menu.SingNotes
{
    public interface ISingNotes
    {
        void Reset();
        int AddPlayer(SRectF rect, SColorF color, int playerNr);
        void RemovePlayer(int iD);

        void AddLine(int iD, CLine[] line, int lineNr, int player);
        void RemoveLine(int iD);

        void AddNote(int iD, CNote note);

        void SetAlpha(int iD, float alpha);
        float GetAlpha(int iD);

        void Draw(int iD, int player);
        void Draw(int iD, List<CLine> singLine, int player);
    }
}