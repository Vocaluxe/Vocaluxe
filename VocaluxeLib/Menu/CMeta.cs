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

using System.Xml.Serialization;

namespace VocaluxeLib.Menu
{
    [XmlType("Meta")]
    public struct SThemeMeta
    {
        [XmlAttribute(AttributeName = "Name")]
        public string Name;
        public SRectF Rect;
    }

    public sealed class CMeta : CMenuElementBase, IMenuElement, IThemeable
    {
        private readonly int _PartyModeID;

        private SThemeMeta _Theme;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded { get; private set; }

        public bool Selectable
        {
            get { return false; }
        }

        public void UnloadSkin() { }

        public void LoadSkin()
        {
            if (!ThemeLoaded)
                return;            

            MaxRect = _Theme.Rect;
        }

        public void ReloadSkin()
        {
            UnloadSkin();
            LoadSkin();
        }

        public object GetTheme()
        {
            return _Theme;
        }

        public CMeta(int partyModeID)
        {
            _PartyModeID = partyModeID;
        }

        public CMeta(SRectF rect)
        {
            MaxRect = rect;
        }

        public CMeta(CMeta m)
        {
            MaxRect = m.MaxRect;
            Visible = m.Visible;
        }

        public CMeta(SThemeMeta theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;
            ThemeLoaded = true;
        }

        public void Draw()
        {
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            X += stepX;
            Y += stepY;

            _Theme.Rect.X += stepX;
            _Theme.Rect.Y += stepY;
        }

        public void ResizeElement(int stepW, int stepH)
        {
            W += stepW;
            if (W <= 0)
                W = 1;

            H += stepH;
            if (H <= 0)
                H = 1;

            _Theme.Rect.W = Rect.W;
            _Theme.Rect.H = Rect.H;
        }
        #endregion ThemeEdit
    }
}