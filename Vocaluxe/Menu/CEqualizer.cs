using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;

namespace Vocaluxe.Menu
{
    enum EEqualizerStyle 
    {
        Bars,
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

    class CEqualizer : IMenuElement
    {
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

        private float[] _Bars;

        public CEqualizer()
        {
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

        public bool LoadTheme(string XmlPath, string ElementName, XPathNavigator navigator, int SkinIndex)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/Skin", navigator, ref _Theme.TextureName, String.Empty);

            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/X", navigator, ref Rect.X);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Y", navigator, ref Rect.Y);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Z", navigator, ref Rect.Z);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/W", navigator, ref Rect.W);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/H", navigator, ref Rect.H);

            _ThemeLoaded &= CHelper.TryGetIntValueFromXML(item + "/NumBars", navigator, ref _Theme.NumBars);

            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Space", navigator, ref Space);

            _ThemeLoaded &= CHelper.TryGetEnumValueFromXML<EEqualizerStyle>(item + "/Style", navigator, ref _Theme.Style);

            _ThemeLoaded &= CHelper.TryGetEnumValueFromXML<EOffOn>(item + "/DrawNegative", navigator, ref _Theme.DrawNegative);

            if (CHelper.GetValueFromXML(item + "/Color", navigator, ref _Theme.ColorName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.ColorName, SkinIndex, ref Color);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/R", navigator, ref Color.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/G", navigator, ref Color.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/B", navigator, ref Color.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/A", navigator, ref Color.A);
            }

            if (CHelper.GetValueFromXML(item + "/MaxColor", navigator, ref _Theme.MaxColorName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.ColorName, SkinIndex, ref Color);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/MaxR", navigator, ref MaxColor.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/MaxG", navigator, ref MaxColor.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/MaxB", navigator, ref MaxColor.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/MaxA", navigator, ref MaxColor.A);
            }

            //Reflection
            if (CHelper.ItemExistsInXML(item + "/Reflection", navigator))
            {
                Reflection = true;
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Reflection/Space", navigator, ref ReflectionSpace);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Reflection/Height", navigator, ref ReflectionHeight);
            }
            else
                Reflection = false;

            if (_ThemeLoaded)
            {
                _Theme.Name = ElementName;
                _Bars = new float[_Theme.NumBars];
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
                writer.WriteComment("<Style>: Style of equalizer-elements: " + CConfig.ListStrings(Enum.GetNames(typeof(EEqualizerStyle))));
                writer.WriteElementString("Style", Enum.GetName(typeof(EEqualizerStyle), _Theme.Style));
                writer.WriteComment("<DrawNegative>: Draw negative values: " + CConfig.ListStrings(Enum.GetNames(typeof(EOffOn))));
                writer.WriteElementString("Style", Enum.GetName(typeof(EOffOn), _Theme.DrawNegative));

                writer.WriteComment("<Color>: Equalizer color from ColorScheme (high priority)");
                writer.WriteComment("or <R>, <G>, <B>, <A> (lower priority)");
                if (_Theme.ColorName != String.Empty)
                {
                    writer.WriteElementString("Color", _Theme.ColorName);
                }
                else
                {
                    writer.WriteElementString("R", Color.R.ToString("#0.00"));
                    writer.WriteElementString("G", Color.G.ToString("#0.00"));
                    writer.WriteElementString("B", Color.B.ToString("#0.00"));
                    writer.WriteElementString("A", Color.A.ToString("#0.00"));
                }
                writer.WriteComment("<Color>: Equalizer color from ColorScheme (high priority)");
                writer.WriteComment("or <R>, <G>, <B>, <A> (lower priority)");
                if (_Theme.ColorName != String.Empty)
                {
                    writer.WriteElementString("Color", _Theme.ColorName);
                }
                else
                {
                    writer.WriteElementString("R", Color.R.ToString("#0.00"));
                    writer.WriteElementString("G", Color.G.ToString("#0.00"));
                    writer.WriteElementString("B", Color.B.ToString("#0.00"));
                    writer.WriteElementString("A", Color.A.ToString("#0.00"));
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
                
                if (weights.Length > i){
                    _Bars[i] = weights[i];
                    if (_Theme.DrawNegative == EOffOn.TR_CONFIG_OFF && weights[i] < 0)
                        _Bars[i] = 0f;
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
            {
                _Bars[i] = 0f;
            }
        }

        public void Draw()
        {
            if (_Bars == null)
                return;

            float dx = Rect.W / _Bars.Length + Space;
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

            switch (_Theme.Style){
                case EEqualizerStyle.Collums:
                    for (int i = 0; i < _Bars.Length; i++)
                    {
                        SRectF bar = new SRectF(Rect.X + dx * i, Rect.Y + Rect.H - _Bars[i] * Rect.H, dx - Space, _Bars[i] * Rect.H, Rect.Z);
                        SColorF color = Color;
                        if (i == max)
                        {
                            color = MaxColor;
                        }

                        CDraw.DrawColor(color, bar);

                        if (Reflection)
                            CDraw.DrawColorReflection(color, bar, ReflectionSpace, ReflectionHeight);
                    }
                    break;

                case EEqualizerStyle.Bars:
                    for (int i = 0; i < _Bars.Length; i++)
                    {
                        SRectF bar = new SRectF(Rect.X + dx * i, Rect.Y + Rect.H - _Bars[i] * Rect.H, dx - Space, _Bars[i] * Rect.H, Rect.Z);
                        SColorF color = Color;
                        if (i == max)
                        {
                            color = MaxColor;
                        }

                        CDraw.DrawColor(color, bar);

                        if (Reflection)
                            CDraw.DrawColorReflection(color, bar, ReflectionSpace, ReflectionHeight);
                    }
                    break;
            }
        }

        public void UnloadTextures()
        {
        }

        public void LoadTextures()
        {
            if (_Theme.ColorName != String.Empty)
                Color = CTheme.GetColor(_Theme.ColorName);

            if (_Theme.MaxColorName != String.Empty)
                MaxColor = CTheme.GetColor(_Theme.MaxColorName);
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
