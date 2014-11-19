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

namespace VocaluxeLib.Menu
{
    public interface IMenuElement
    {
        /// <summary>
        ///     Current extend of the element. Smaller or equal then MaxRect
        /// </summary>
        SRectF Rect { get; }
        /// <summary>
        ///     Maximum extend of the element. Equal to Rect in most cases
        /// </summary>
        SRectF MaxRect { get; set; }
        bool Selectable { get; }
        bool Visible { get; set; }
        bool Highlighted { get; set; }
        bool Selected { get; set; }
        void MoveElement(int stepX, int stepY);
        void ResizeElement(int stepW, int stepH);

        void Draw();
    }
}