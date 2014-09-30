using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace VocaluxeLib
{
    public interface IFontObserver
    {
        void FontChanged();
    }

    public struct SThemeFont
    {
        public string Name;
        public EStyle Style;
        public float Size;
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