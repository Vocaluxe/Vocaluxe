﻿#region license
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
using System.Xml;
using System.Xml.Serialization;

namespace VocaluxeLib.Menu
{
    public enum EEqualizerStyle
    {
        Columns
    }

    [XmlType("Equalizer")]
    public struct SThemeEqualizer
    {
        [XmlAttributeAttribute(AttributeName = "Name")]
        public string Name;

        [XmlElement("Skin")]
        public string TextureName;

        [XmlElement("Rect")]
        public SRectF Rect;

        [XmlElement("NumBars")]
        public int NumBars;
        [XmlElement("Space")]
        public float Space;
        [XmlElement("Style")]
        public EEqualizerStyle Style;
        [XmlElement("DrawNegative")]
        public EOffOn DrawNegative;

        [XmlElement("Color")]
        public SThemeColor Color;
        [XmlElement("MaxColor")]
        public SThemeColor MaxColor;
        [XmlElement("Reflection")]
        public SReflection Reflection;
    }

    public class CEqualizer : IMenuElement
    {
        private readonly int _PartyModeID;
        private SThemeEqualizer _Theme;
        private bool _ThemeLoaded;

        public SRectF Rect;
        public SColorF Color;
        public SColorF MaxColor;
        public float Space;

        public bool Reflection;
        public float ReflectionSpace;
        public float ReflectionHeight;

        public bool Selected;
        public bool Visible;
        public bool ScreenHandles;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded
        {
            get { return _ThemeLoaded; }
        }

        private float[] _Bars;
        private int _MaxBar;
        private float _MaxVolume;

        public CEqualizer(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = new SThemeEqualizer();
            _ThemeLoaded = false;

            Rect = new SRectF();
            Color = new SColorF();
            MaxColor = new SColorF();

            Selected = false;
            Visible = true;
            ScreenHandles = false;

            Reflection = false;
            ReflectionSpace = 0f;
            ReflectionHeight = 0f;
        }

        public CEqualizer(SThemeEqualizer theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;

            Visible = true;
            ScreenHandles = false;

            LoadTextures();
        }

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader, int skinIndex)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/Skin", out _Theme.TextureName, String.Empty);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref Rect.X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref Rect.Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref Rect.Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/W", ref Rect.W);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref Rect.H);

            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/NumBars", ref _Theme.NumBars);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Space", ref Space);

            _ThemeLoaded &= xmlReader.TryGetEnumValue(item + "/Style", ref _Theme.Style);

            _ThemeLoaded &= xmlReader.TryGetEnumValue(item + "/DrawNegative", ref _Theme.DrawNegative);

            if (xmlReader.GetValue(item + "/Color", out _Theme.Color.Name, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.Color.Name, skinIndex, out Color);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref Color.A);
            }

            if (xmlReader.GetValue(item + "/MaxColor", out _Theme.MaxColor.Name, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.MaxColor.Name, skinIndex, out Color);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/MaxR", ref MaxColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/MaxG", ref MaxColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/MaxB", ref MaxColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/MaxA", ref MaxColor.A);
            }

            //Reflection
            if (xmlReader.ItemExists(item + "/Reflection"))
            {
                Reflection = true;
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Reflection/Space", ref ReflectionSpace);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Reflection/Height", ref ReflectionHeight);

                _Theme.Reflection = new SReflection(true, ReflectionHeight, ReflectionSpace);
            }
            else
            {
                Reflection = false;
                _Theme.Reflection = new SReflection(false, 0f, 0f);
            }

            if (_ThemeLoaded)
            {
                _Theme.Name = elementName;
                _Theme.Space = Space;
                _Theme.Color.Color = new SColorF(Color);
                _Theme.MaxColor.Color = new SColorF(MaxColor);
                _Theme.Rect = Rect;

                LoadTextures();
            }
            return _ThemeLoaded;
        }

        public void Update(float[] weights, float volume)
        {
            if (weights == null || weights.Length == 0 || _Bars == null)
                return;
            if (volume < 0.001)
            {
                for (int i = 0; i < _Bars.Length; i++)
                    _Bars[i] = 0f;
                return;
            }
            if (volume > _MaxVolume)
                _MaxVolume = volume;
            _MaxBar = 0;
            float maxVal = -99f;
            for (int i = 0; i < _Bars.Length; i++)
            {
                if (i < weights.Length)
                {
                    if (_Theme.DrawNegative == EOffOn.TR_CONFIG_OFF && weights[i] < 0)
                        _Bars[i] = 0f;
                    else
                    {
                        _Bars[i] = weights[i] * volume / _MaxVolume;
                        if (_Bars[i] > maxVal)
                        {
                            maxVal = _Bars[i];
                            _MaxBar = i;
                        }
                    }
                }
                else
                    _Bars[i] = 0f;
            }
        }

        public void Reset()
        {
            if (_Bars == null || _Bars.Length == 0)
                return;

            for (int i = 0; i < _Bars.Length; i++)
                _Bars[i] = 0f;
            _MaxBar = 0;
            _MaxVolume = 0f;
        }

        public void Draw()
        {
            if (_Bars == null || _Theme.Style != EEqualizerStyle.Columns)
                return;

            float dx = Rect.W / _Bars.Length;
            float scaleVal = (_Bars[_MaxBar] < 0.00001f) ? 0f : 1 / _Bars[_MaxBar];

            for (int i = 0; i < _Bars.Length; i++)
            {
                float value = _Bars[i] * scaleVal;
                var bar = new SRectF(Rect.X + dx * i, Rect.Y + Rect.H - value * Rect.H, dx - Space, value * Rect.H, Rect.Z);
                SColorF color = Color;
                if (i == _MaxBar)
                    color = MaxColor;

                CBase.Drawing.DrawColor(color, bar);

                if (Reflection)
                    CBase.Drawing.DrawColorReflection(color, bar, ReflectionSpace, ReflectionHeight);
            }
        }

        public void UnloadTextures() {}

        public void LoadTextures()
        {
            if (!String.IsNullOrEmpty(_Theme.Color.Name))
                Color = CBase.Theme.GetColor(_Theme.Color.Name, _PartyModeID);
            else
                Color = _Theme.Color.Color;

            if (!String.IsNullOrEmpty(_Theme.MaxColor.Name))
                MaxColor = CBase.Theme.GetColor(_Theme.MaxColor.Name, _PartyModeID);
            else
                MaxColor = _Theme.MaxColor.Color;

            Rect = _Theme.Rect;
            Space = _Theme.Space;
            Reflection = _Theme.Reflection.Enabled;
            if(Reflection)
            {
                ReflectionHeight = _Theme.Reflection.Height;
                ReflectionSpace = _Theme.Reflection.Space;
            }

            _Bars = new float[_Theme.NumBars];
            for (int i = 0; i < _Bars.Length; i++)
                _Bars[i] = 0f;
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        public SThemeEqualizer GetTheme()
        {
            return _Theme;
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            Rect.X += stepX;
            Rect.Y += stepY;

            _Theme.Rect.X += stepX;
            _Theme.Rect.Y += stepY;
        }

        public void ResizeElement(int stepW, int stepH)
        {
            Rect.W += stepW;
            if (Rect.W <= 0)
                Rect.W = 1;

            _Theme.Rect.W = Rect.W;

            Rect.H += stepH;
            if (Rect.H <= 0)
                Rect.H = 1;

            _Theme.Rect.H = Rect.H;
        }
        #endregion ThemeEdit
    }
}