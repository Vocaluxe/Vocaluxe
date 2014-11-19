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
    public abstract class CMenuElementBase
    {
        public bool Visible { get; set; }
        public bool Highlighted { get; set; }
        public virtual bool Selected { get; set; }

        public virtual SRectF Rect
        {
            get { return MaxRect; }
        }

        // Those values are the ones from MaxRect for easier accessibility
        public virtual float X { get; set; }
        public virtual float Y { get; set; }
        public virtual float Z { get; set; }
        public virtual float W { get; set; }
        public virtual float H { get; set; }

        public virtual SRectF MaxRect
        {
            get { return new SRectF(X, Y, W, H, Z); }
            set
            {
                X = value.X;
                Y = value.Y;
                W = value.W;
                H = value.H;
                Z = value.Z;
            }
        }

        protected CMenuElementBase()
        {
            Visible = true;
        }
    }
}