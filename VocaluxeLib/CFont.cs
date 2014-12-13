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

using System;
using System.Collections.Generic;

namespace VocaluxeLib
{
    public interface IFontObserver
    {
        void FontChanged();
    }

    public struct SThemeFont
    {
        // ReSharper disable UnassignedField.Global
        public string Name;
        public EStyle Style;
        public float Size;
        // ReSharper restore UnassignedField.Global
    }

    public class CFont
    {
        private string _Name;
        private EStyle _Style;
        private float _Height;
        private readonly List<IFontObserver> _Observers = new List<IFontObserver>();

        public string Name
        {
            get { return _Name; }
            set
            {
                if (_Name == value)
                    return;
                _Name = value;
                foreach (IFontObserver observer in _Observers)
                    observer.FontChanged();
            }
        }
        public EStyle Style
        {
            get { return _Style; }
            set
            {
                if (_Style == value)
                    return;
                _Style = value;
                foreach (IFontObserver observer in _Observers)
                    observer.FontChanged();
            }
        }
        public float Height
        {
            get { return _Height; }
            set
            {
                if (Math.Abs(_Height - value) < 0.001)
                    return;
                _Height = value;
                foreach (IFontObserver observer in _Observers)
                    observer.FontChanged();
            }
        }

        public CFont()
        {
            _Name = "Normal";
            _Style = EStyle.Normal;
            _Height = 25f;
        }

        public CFont(SThemeFont font)
        {
            _Name = font.Name;
            _Style = font.Style;
            _Height = font.Size;
        }

        public CFont(CFont font)
        {
            _Name = font._Name;
            _Style = font._Style;
            _Height = font._Height;
        }

        public CFont(string fontFamily, EStyle style, float h)
        {
            _Name = fontFamily;
            _Style = style;
            _Height = h;
        }

        public void AddObserver(IFontObserver observer)
        {
            if (!_Observers.Contains(observer))
                _Observers.Add(observer);
        }

        public void RemoveObserver(IFontObserver observer)
        {
            _Observers.Remove(observer);
        }
    }
}