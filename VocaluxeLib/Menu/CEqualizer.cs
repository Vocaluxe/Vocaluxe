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

using System;
using System.Xml;

namespace VocaluxeLib.Menu
{
    enum EEqualizerStyle
    {
        Collums
    }

    struct SThemeEqualizer
    {
        public string Name;

        public string ColorName;
        public string MaxColorName;
        public string TextureName;

        public int NumBars;

        public EEqualizerStyle Style;
        public EOffOn DrawNegative;
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

        private float[] _Bars;

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

            if (xmlReader.GetValue(item + "/Color", out _Theme.ColorName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorName, skinIndex, out Color);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref Color.A);
            }

            if (xmlReader.GetValue(item + "/MaxColor", out _Theme.MaxColorName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorName, skinIndex, out Color);
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
            }
            else
                Reflection = false;

            if (_ThemeLoaded)
            {
                _Theme.Name = elementName;
                _Bars = new float[_Theme.NumBars];
                for (int i = 0; i < _Bars.Length; i++)
                    _Bars[i] = 0f;
                LoadTextures();
            }
            return _ThemeLoaded;
        }

        public bool SaveTheme(XmlWriter writer)
        {
            if (_ThemeLoaded)
            {
                writer.WriteStartElement(_Theme.Name);

                writer.WriteComment("<Skin>: Texture name");
                writer.WriteElementString("Skin", _Theme.TextureName);

                writer.WriteComment("<X>, <Y>, <Z>, <W>, <H>: Equalizer position, width and height");
                writer.WriteElementString("X", Rect.X.ToString("#0"));
                writer.WriteElementString("Y", Rect.Y.ToString("#0"));
                writer.WriteElementString("Z", Rect.Z.ToString("#0.00"));
                writer.WriteElementString("W", Rect.W.ToString("#0"));
                writer.WriteElementString("H", Rect.H.ToString("#0"));

                writer.WriteComment("<NumBars>: Number of equalizer-elements.");
                writer.WriteElementString("NumBars", _Theme.NumBars.ToString("#0"));
                writer.WriteComment("<Space>: Space between equalizer-elements.");
                writer.WriteElementString("Space", Space.ToString("#0.00"));
                writer.WriteComment("<Style>: Style of equalizer-elements: " + CHelper.ListStrings(Enum.GetNames(typeof(EEqualizerStyle))));
                writer.WriteElementString("Style", Enum.GetName(typeof(EEqualizerStyle), _Theme.Style));
                writer.WriteComment("<DrawNegative>: Draw negative values: " + CHelper.ListStrings(Enum.GetNames(typeof(EOffOn))));
                writer.WriteElementString("DrawNegative", Enum.GetName(typeof(EOffOn), _Theme.DrawNegative));

                writer.WriteComment("<Color>: Equalizer color from ColorScheme (high priority)");
                writer.WriteComment("or <R>, <G>, <B>, <A> (lower priority)");
                if (!String.IsNullOrEmpty(_Theme.ColorName))
                    writer.WriteElementString("Color", _Theme.ColorName);
                else
                {
                    writer.WriteElementString("R", Color.R.ToString("#0.00"));
                    writer.WriteElementString("G", Color.G.ToString("#0.00"));
                    writer.WriteElementString("B", Color.B.ToString("#0.00"));
                    writer.WriteElementString("A", Color.A.ToString("#0.00"));
                }
                writer.WriteComment("<MaxColor>: Equalizer color for maximal volume from ColorScheme (high priority)");
                writer.WriteComment("or <MaxR>, <MaxG>, <MaxB>, <MaxA> (lower priority)");
                if (!String.IsNullOrEmpty(_Theme.ColorName))
                    writer.WriteElementString("MaxColor", _Theme.ColorName);
                else
                {
                    writer.WriteElementString("MaxR", Color.R.ToString("#0.00"));
                    writer.WriteElementString("MaxG", Color.G.ToString("#0.00"));
                    writer.WriteElementString("MaxB", Color.B.ToString("#0.00"));
                    writer.WriteElementString("MaxA", Color.A.ToString("#0.00"));
                }

                writer.WriteComment("<Reflection> If exists:");
                writer.WriteComment("   <Space>: Reflection Space");
                writer.WriteComment("   <Height>: Reflection Height");
                if (Reflection)
                {
                    writer.WriteStartElement("Reflection");
                    writer.WriteElementString("Space", ReflectionSpace.ToString("#0"));
                    writer.WriteElementString("Height", ReflectionHeight.ToString("#0"));
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                return true;
            }
            return false;
        }

        public void Update(float[] weights)
        {
            if (weights == null || weights.Length == 0 || _Bars == null)
                return;
            for (int i = 0; i < _Bars.Length; i++)
            {
                if (weights.Length > i)
                {
                    _Bars[i] = 1f - weights[i];
                    if (_Theme.DrawNegative == EOffOn.TR_CONFIG_OFF && weights[i] < 0)
                        _Bars[i] = 1f + weights[i];
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
        }

        public void Draw()
        {
            if (_Bars == null || _Theme.Style != EEqualizerStyle.Collums)
                return;

            float dx = Rect.W / _Bars.Length;
            int max = _Bars.Length - 1;
            float maxB = _Bars[max];
            for (int i = 0; i < _Bars.Length - 1; i++)
            {
                if (_Bars[i] > maxB)
                {
                    maxB = _Bars[i];
                    max = i;
                }
            }

            for (int i = 0; i < _Bars.Length; i++)
            {
                SRectF bar = new SRectF(Rect.X + dx * i, Rect.Y + Rect.H - _Bars[i] * Rect.H, dx - Space, _Bars[i] * Rect.H, Rect.Z);
                SColorF color = Color;
                if (i == max)
                    color = MaxColor;

                CBase.Drawing.DrawColor(color, bar);

                if (Reflection)
                    CBase.Drawing.DrawColorReflection(color, bar, ReflectionSpace, ReflectionHeight);
            }
        }

        public void UnloadTextures() {}

        public void LoadTextures()
        {
            if (!String.IsNullOrEmpty(_Theme.ColorName))
                Color = CBase.Theme.GetColor(_Theme.ColorName, _PartyModeID);

            if (!String.IsNullOrEmpty(_Theme.MaxColorName))
                MaxColor = CBase.Theme.GetColor(_Theme.MaxColorName, _PartyModeID);
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            Rect.X += stepX;
            Rect.Y += stepY;
        }

        public void ResizeElement(int stepW, int stepH)
        {
            Rect.W += stepW;
            if (Rect.W <= 0)
                Rect.W = 1;

            Rect.H += stepH;
            if (Rect.H <= 0)
                Rect.H = 1;
        }
        #endregion ThemeEdit
    }
}