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
    struct SThemeSelectSlide
    {
        public string Name;

        public string TextureName;
        public string TextureArrowLeftName;
        public string TextureArrowRightName;

        public string STextureName;
        public string STextureArrowLeftName;
        public string STextureArrowRightName;

        public string HTextureName;

        public string ColorName;
        public string SColorName;
        public string HColorName;

        public string ArrowColorName;
        public string SArrowColorName;

        public string TextColorName;
        public string STextColorName;

        public string TextFont;
        public EStyle TextStyle;
    }

    class CSelectSlide : IMenuElement, ICloneable
    {
        private SThemeSelectSlide _Theme;
        private bool _ThemeLoaded;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public SRectF Rect;
        public SRectF RectArrowLeft;
        public SRectF RectArrowRight;

        public SColorF Color;
        public SColorF SColor;
        public SColorF HColor;

        public SColorF ColorArrow;
        public SColorF SColorArrow;

        public SColorF TextColor;
        public SColorF STextColor;

        public float TextRelativeX;
        public float TextRelativeY;
        public float TextH;
        public float MaxW;

        private bool _Selected;
        public bool Selected
        {
            get { return _Selected; }
            set
            {
                _Selected = value;

                if (!value)
                {
                    _ArrowLeftSelected = false;
                    _ArrowRightSelected = false;
                }
            }
        }
        public bool Visible = true;
        public bool Highlighted = false;
        
        private bool _ArrowLeftSelected;
        private bool _ArrowRightSelected;

        private List<string> _ValueNames;
        private List<STexture> _Textures;
        private List<int> _ValueIndexes;

        private List<SRectF> _ValueBounds = new List<SRectF>();

        public bool WithTextures = false;

        private int _Selection = -1;
        public int Selection
        {
            get { return _Selection; }
            set
            {
                if (value >= 0 && value < _ValueNames.Count)
                {
                    _Selection = value;
                }
            }
        }

        public int ValueIndex
        {
            get
            {
                if (_Selection >= 0 && _ValueIndexes.Count > _Selection)
                    return _ValueIndexes[_Selection];

                else return -1;            
            }
        }

        public int NumValues
        {
            get
            {
                return _ValueNames.Count;
            }
        }

        private int _NumVisible = -1;
        public int NumVisible
        {
            get { return _NumVisible; }
            set
            {
                if (value > 0)
                    _NumVisible = value;

                _ValueBounds.Clear();
            }
        }

        public CSelectSlide()
        {
            _Theme = new SThemeSelectSlide();
            _ThemeLoaded = false;

            Rect = new SRectF();
            RectArrowLeft = new SRectF();
            RectArrowRight = new SRectF();

            Color = new SColorF();
            SColor = new SColorF();

            ColorArrow = new SColorF();
            SColorArrow = new SColorF();

            TextColor = new SColorF();
            STextColor = new SColorF();
            TextH = 1f;
            MaxW = 0f;

            _Selected = false;
            _Textures = new List<STexture>();
            _ValueIndexes = new List<int>();
            _ValueNames = new List<string>();
        }

        public CSelectSlide(CSelectSlide slide)
        {
            _Theme = new SThemeSelectSlide();
            
            _Theme.TextureArrowLeftName = slide._Theme.TextureArrowLeftName;
            _Theme.TextureArrowRightName = slide._Theme.TextureArrowRightName;

            _Theme.STextureName = slide._Theme.STextureName;
            _Theme.STextureArrowLeftName = slide._Theme.STextureArrowLeftName;
            _Theme.STextureArrowRightName = slide._Theme.STextureArrowRightName;

            _Theme.HTextureName = slide._Theme.HTextureName;

            _Theme.ColorName = slide._Theme.ColorName;
            _Theme.SColorName = slide._Theme.SColorName;
            _Theme.HColorName = slide._Theme.HColorName;

            _Theme.ArrowColorName = slide._Theme.ArrowColorName;
            _Theme.SArrowColorName = slide._Theme.SArrowColorName;

            _Theme.TextColorName = slide._Theme.TextColorName;
            _Theme.STextColorName = slide._Theme.STextColorName;

            _Theme.TextFont = slide._Theme.TextFont;
            _Theme.TextStyle = slide._Theme.TextStyle;

            _ThemeLoaded = false;

            Rect = new SRectF(slide.Rect);
            RectArrowLeft = new SRectF(slide.RectArrowLeft);
            RectArrowRight = new SRectF(slide.RectArrowRight);

            Color = new SColorF(slide.Color);
            SColor = new SColorF(slide.SColor);

            ColorArrow = new SColorF(slide.ColorArrow);
            SColorArrow = new SColorF(slide.SColorArrow);

            TextColor = new SColorF(slide.TextColor);
            STextColor = new SColorF(slide.STextColor);
            TextH = slide.TextH;
            TextRelativeX = slide.TextRelativeX;
            TextRelativeY = slide.TextRelativeY;
            MaxW = slide.MaxW;

            _Selected = slide._Selected;
            _Textures = new List<STexture>(slide._Textures);
            _ValueIndexes = new List<int>(slide._ValueIndexes);
            _ValueNames = new List<string>(slide._ValueNames);
            _ValueBounds = new List<SRectF>(slide._ValueBounds);
            _Selection = slide._Selection;
            _NumVisible = slide._NumVisible;

            WithTextures = slide.WithTextures;
            Visible = slide.Visible;
        }

        public bool LoadTheme(string XmlPath, string ElementName, XPathNavigator navigator, int SkinIndex)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/Skin", navigator, ref _Theme.TextureName, String.Empty);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinArrowLeft", navigator, ref _Theme.TextureArrowLeftName, String.Empty);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinArrowRight", navigator, ref _Theme.TextureArrowRightName, String.Empty);

            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinSelected", navigator, ref _Theme.STextureName, String.Empty);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinArrowLeftSelected", navigator, ref _Theme.STextureArrowLeftName, String.Empty);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinArrowRightSelected", navigator, ref _Theme.STextureArrowRightName, String.Empty);

            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinHighlighted", navigator, ref _Theme.HTextureName, String.Empty);

            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/X", navigator, ref Rect.X);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Y", navigator, ref Rect.Y);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Z", navigator, ref Rect.Z);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/W", navigator, ref Rect.W);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/H", navigator, ref Rect.H);

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

            if (CHelper.GetValueFromXML(item + "/SColor", navigator, ref _Theme.SColorName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.SColorName, SkinIndex, ref SColor);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SR", navigator, ref SColor.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SG", navigator, ref SColor.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SB", navigator, ref SColor.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SA", navigator, ref SColor.A);
            }

            if (CHelper.GetValueFromXML(item + "/HColor", navigator, ref _Theme.HColorName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.HColorName, SkinIndex, ref HColor);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/HR", navigator, ref HColor.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/HG", navigator, ref HColor.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/HB", navigator, ref HColor.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/HA", navigator, ref HColor.A);
            }

            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/ArrowLeftX", navigator, ref RectArrowLeft.X);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/ArrowLeftY", navigator, ref RectArrowLeft.Y);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/ArrowLeftZ", navigator, ref RectArrowLeft.Z);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/ArrowLeftW", navigator, ref RectArrowLeft.W);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/ArrowLeftH", navigator, ref RectArrowLeft.H);

            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/ArrowRightX", navigator, ref RectArrowRight.X);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/ArrowRightY", navigator, ref RectArrowRight.Y);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/ArrowRightZ", navigator, ref RectArrowRight.Z);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/ArrowRightW", navigator, ref RectArrowRight.W);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/ArrowRightH", navigator, ref RectArrowRight.H);

            if (CHelper.GetValueFromXML(item + "/ArrowColor", navigator, ref _Theme.ArrowColorName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.ArrowColorName, SkinIndex, ref ColorArrow);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/ArrowR", navigator, ref ColorArrow.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/ArrowG", navigator, ref ColorArrow.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/ArrowB", navigator, ref ColorArrow.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/ArrowA", navigator, ref ColorArrow.A);
            }

            if (CHelper.GetValueFromXML(item + "/ArrowSColor", navigator, ref _Theme.SArrowColorName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.SArrowColorName, SkinIndex, ref SColorArrow);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/ArrowSR", navigator, ref SColorArrow.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/ArrowSG", navigator, ref SColorArrow.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/ArrowSB", navigator, ref SColorArrow.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/ArrowSA", navigator, ref SColorArrow.A);
            }

            if (CHelper.GetValueFromXML(item + "/TextColor", navigator, ref _Theme.TextColorName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.TextColorName, SkinIndex, ref TextColor);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/TextR", navigator, ref TextColor.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/TextG", navigator, ref TextColor.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/TextB", navigator, ref TextColor.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/TextA", navigator, ref TextColor.A);
            }

            if (CHelper.GetValueFromXML(item + "/TextSColor", navigator, ref _Theme.STextColorName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.STextColorName, SkinIndex, ref STextColor);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/TextSR", navigator, ref STextColor.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/TextSG", navigator, ref STextColor.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/TextSB", navigator, ref STextColor.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/TextSA", navigator, ref STextColor.A);
            }

            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/TextH", navigator, ref TextH);
            if(CHelper.TryGetFloatValueFromXML(item + "/TextRelativeX", navigator, ref TextRelativeX))
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/TextRelativeX", navigator, ref TextRelativeX);
            if(CHelper.TryGetFloatValueFromXML(item + "/TextRelativeY", navigator, ref TextRelativeY))
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/TextRelativeY", navigator, ref TextRelativeY);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/TextMaxW", navigator, ref MaxW);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/TextFont", navigator, ref _Theme.TextFont, "Normal");
            _ThemeLoaded &= CHelper.TryGetEnumValueFromXML<EStyle>(item + "/TextStyle", navigator, ref _Theme.TextStyle);

            _ThemeLoaded &= CHelper.TryGetIntValueFromXML(item + "/NumVisible", navigator, ref _NumVisible);

            if (_ThemeLoaded)
            {
                _Theme.Name = ElementName;
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

                writer.WriteComment("<SkinArrowLeft>: Texture name of left arrow");
                writer.WriteElementString("SkinArrowLeft", _Theme.TextureArrowLeftName);

                writer.WriteComment("<SkinArrowRight>: Texture name of right arrow");
                writer.WriteElementString("SkinArrowRight", _Theme.TextureArrowRightName);


                writer.WriteComment("<SkinSelected>: Texture name for selected SelectSlide");
                writer.WriteElementString("SkinSelected", _Theme.STextureName);

                writer.WriteComment("<SkinArrowLeftSelected>: Texture name of selected left arrow");
                writer.WriteElementString("SkinArrowLeftSelected", _Theme.STextureArrowLeftName);

                writer.WriteComment("<SkinArrowRightSelected>: Texture name of selected right arrow");
                writer.WriteElementString("SkinArrowRightSelected", _Theme.STextureArrowRightName);

                writer.WriteComment("<SkinHighlighted>: Texture name for highlighted SelectSlide");
                writer.WriteElementString("SkinHighlighted", _Theme.HTextureName);

                writer.WriteComment("<X>, <Y>, <Z>, <W>, <H>: SelectSlide position, width and height");
                writer.WriteElementString("X", Rect.X.ToString("#0"));
                writer.WriteElementString("Y", Rect.Y.ToString("#0"));
                writer.WriteElementString("Z", Rect.Z.ToString("#0.00"));
                writer.WriteElementString("W", Rect.W.ToString("#0"));
                writer.WriteElementString("H", Rect.H.ToString("#0"));

                writer.WriteComment("<ArrowLeftX>, <ArrowLeftY>, <ArrowLeftZ>, <ArrowLeftW>, <ArrowLeftH>: Left arrow position, width and height");
                writer.WriteElementString("ArrowLeftX", RectArrowLeft.X.ToString("#0"));
                writer.WriteElementString("ArrowLeftY", RectArrowLeft.Y.ToString("#0"));
                writer.WriteElementString("ArrowLeftZ", RectArrowLeft.Z.ToString("#0.00"));
                writer.WriteElementString("ArrowLeftW", RectArrowLeft.W.ToString("#0"));
                writer.WriteElementString("ArrowLeftH", RectArrowLeft.H.ToString("#0"));

                writer.WriteComment("<ArrowRightX>, <ArrowRightY>, <ArrowRightZ>, <ArrowRightW>, <ArrowRightH>: Right arrow position, width and height");
                writer.WriteElementString("ArrowRightX", RectArrowRight.X.ToString("#0"));
                writer.WriteElementString("ArrowRightY", RectArrowRight.Y.ToString("#0"));
                writer.WriteElementString("ArrowRightZ", RectArrowRight.Z.ToString("#0.00"));
                writer.WriteElementString("ArrowRightW", RectArrowRight.W.ToString("#0"));
                writer.WriteElementString("ArrowRightH", RectArrowRight.H.ToString("#0"));

                writer.WriteComment("<Color>: SelectSlide color from ColorScheme (high priority)");
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

                writer.WriteComment("<SColor>: Selected SelectSlide color from ColorScheme (high priority)");
                writer.WriteComment("or <SR>, <SG>, <SB>, <SA> (lower priority)");
                if (_Theme.SColorName != String.Empty)
                {
                    writer.WriteElementString("SColor", _Theme.SColorName);
                }
                else
                {
                    writer.WriteElementString("SR", SColor.R.ToString("#0.00"));
                    writer.WriteElementString("SG", SColor.G.ToString("#0.00"));
                    writer.WriteElementString("SB", SColor.B.ToString("#0.00"));
                    writer.WriteElementString("SA", SColor.A.ToString("#0.00"));
                }

                writer.WriteComment("<HColor>: Highlighted SelectSlide color from ColorScheme (high priority)");
                writer.WriteComment("or <HR>, <HG>, <HB>, <HA> (lower priority)");
                if (_Theme.HColorName != String.Empty)
                {
                    writer.WriteElementString("HColor", _Theme.HColorName);
                }
                else
                {
                    writer.WriteElementString("HR", HColor.R.ToString("#0.00"));
                    writer.WriteElementString("HG", HColor.G.ToString("#0.00"));
                    writer.WriteElementString("HB", HColor.B.ToString("#0.00"));
                    writer.WriteElementString("HA", HColor.A.ToString("#0.00"));
                }

                writer.WriteComment("<ArrowColor>: Arrow color from ColorScheme (high priority)");
                writer.WriteComment("or <ArrowR>, <ArrowG>, <ArrowB>, <ArrowA> (lower priority)");
                if (_Theme.ArrowColorName != String.Empty)
                {
                    writer.WriteElementString("ArrowColor", _Theme.ArrowColorName);
                }
                else
                {
                    writer.WriteElementString("ArrowR", ColorArrow.R.ToString("#0.00"));
                    writer.WriteElementString("ArrowG", ColorArrow.G.ToString("#0.00"));
                    writer.WriteElementString("ArrowB", ColorArrow.B.ToString("#0.00"));
                    writer.WriteElementString("ArrowA", ColorArrow.A.ToString("#0.00"));
                }

                writer.WriteComment("<ArrowSColor>: Selected arrow color from ColorScheme (high priority)");
                writer.WriteComment("or <ArrowSR>, <ArrowSG>, <ArrowSB>, <ArrowSA> (lower priority)");
                if (_Theme.SArrowColorName != String.Empty)
                {
                    writer.WriteElementString("ArrowSColor", _Theme.SArrowColorName);
                }
                else
                {
                    writer.WriteElementString("ArrowSR", SColorArrow.R.ToString("#0.00"));
                    writer.WriteElementString("ArrowSG", SColorArrow.G.ToString("#0.00"));
                    writer.WriteElementString("ArrowSB", SColorArrow.B.ToString("#0.00"));
                    writer.WriteElementString("ArrowSA", SColorArrow.A.ToString("#0.00"));
                }

                writer.WriteComment("<TextColor>: Text color from ColorScheme (high priority)");
                writer.WriteComment("or <TextR>, <TextG>, <TextB>, <TextA> (lower priority)");
                if (_Theme.TextColorName != String.Empty)
                {
                    writer.WriteElementString("TextColor", _Theme.TextColorName);
                }
                else
                {
                    writer.WriteElementString("TextR", TextColor.R.ToString("#0.00"));
                    writer.WriteElementString("TextG", TextColor.G.ToString("#0.00"));
                    writer.WriteElementString("TextB", TextColor.B.ToString("#0.00"));
                    writer.WriteElementString("TextA", TextColor.A.ToString("#0.00"));
                }

                writer.WriteComment("<TextSColor>: Selected text color from ColorScheme (high priority)");
                writer.WriteComment("or <TextSR>, <TextSG>, <TextSB>, <TextSA> (lower priority)");
                if (_Theme.STextColorName != String.Empty)
                {
                    writer.WriteElementString("TextSColor", _Theme.STextColorName);
                }
                else
                {
                    writer.WriteElementString("TextSR", STextColor.R.ToString("#0.00"));
                    writer.WriteElementString("TextSG", STextColor.G.ToString("#0.00"));
                    writer.WriteElementString("TextSB", STextColor.B.ToString("#0.00"));
                    writer.WriteElementString("TextSA", STextColor.A.ToString("#0.00"));
                }

                writer.WriteComment("<TextH>: Text height");
                writer.WriteElementString("TextH", TextH.ToString("#0.00"));

                writer.WriteComment("<TextRelativeX>: Text relative x-position");
                if (TextRelativeX != 0)
                    writer.WriteElementString("TextRelativeX", TextRelativeX.ToString("#0.00"));

                writer.WriteComment("<TextRelativeY>: Text relative y-position");
                if (TextRelativeY != 0)
                    writer.WriteElementString("TextRelativeY", TextRelativeY.ToString("#0.00"));

                writer.WriteComment("<TextMaxW>: Maximum text width (if exists)");
                writer.WriteElementString("TextMaxW", MaxW.ToString("#0.00"));

                writer.WriteComment("<TextFont>: Text font name");
                writer.WriteElementString("TextFont", _Theme.TextFont);

                writer.WriteComment("<TextStyle>: Text style: " + CConfig.ListStrings(Enum.GetNames(typeof(EStyle))));
                writer.WriteElementString("TextStyle", Enum.GetName(typeof(EStyle), _Theme.TextStyle));

                writer.WriteComment("<NumVisible>: Number of visible elements in the slide");
                writer.WriteElementString("NumVisible", _NumVisible.ToString());

                writer.WriteEndElement();

                return true;
            }
            return false;
        }

        public void AddValue(string value)
        {
            AddValue(value, new STexture(-1));
            _ValueBounds.Clear();
        }

        public void AddValue(string value, STexture texture)
        {
            AddValue(value, texture, _ValueIndexes.Count);
        }

        public void AddValue(string value, STexture texture, int valueIndex)
        {
            _ValueNames.Add(value);
            _Textures.Add(texture);
            _ValueIndexes.Add(valueIndex);

            if (Selection == -1)
                Selection = 0;

            _ValueBounds.Clear();
        }

        public void AddValues(string[] values)
        {
            foreach (string value in values)
            {
                AddValue(value, new STexture(-1));
            }

            _ValueBounds.Clear();
        }

        public void AddValues(string[] values, STexture[] textures)
        {
            if (values.Length != textures.Length)
                return;

            for (int i = 0; i < values.Length; i++)
            {
                AddValue(values[i], textures[i]);
            }
            if (Selection == -1)
                Selection = 0;

            _ValueBounds.Clear();
        }

        public void SetValues<T>(int selection)
        {
            AddValues(Enum.GetNames(typeof(T)));
            Selection = selection;
        }

        public void RenameValue(string NewName)
        {
            RenameValue(Selection, NewName);
        }

        public void RenameValue(int selection, string NewName)
        {
            RenameValue(selection, NewName, new STexture(-1));
        }

        public void RenameValue(int selection, string NewName, STexture NewTexture)
        {
            if (selection < 0 && selection >= _ValueNames.Count)
                return;

            _ValueNames[selection] = NewName;
            _Textures[selection] = NewTexture;
        }

        public bool SetSelectionByValueIndex(int ValueIndex)
        {
            for (int i = 0; i < _ValueIndexes.Count; i++)
            {
                if (_ValueIndexes[i] == ValueIndex)
                {
                    Selection = i;
                    return true;
                }
            }
            return false;
        }

        public bool NextValue()
        {
            if (Selection < _ValueNames.Count - 1)
            {
                Selection++;
                return true;
            }
            return false;
        }

        public bool PrevValue()
        {
            if (Selection > 0)
            {
                Selection--;
                return true;
            }
            return false;
        }

        public void FirstValue()
        {
            if (_ValueNames.Count > 0)
                Selection = 0;
        }

        public void LastValue()
        {
            if (_ValueNames.Count > 0)
                Selection = _ValueNames.Count - 1;
        }

        public void Clear()
        {
            _Selection = -1;
            _ValueNames.Clear();
            _Textures.Clear();
            _ValueBounds.Clear();
            _ValueIndexes.Clear();
        }

        public void ProcessMouseMove(int x, int y)
        {
            _ArrowLeftSelected = CHelper.IsInBounds(RectArrowLeft, x, y) && _Selection > 0;    
            _ArrowRightSelected = CHelper.IsInBounds(RectArrowRight, x, y) && _Selection < _ValueNames.Count - 1;
        }

        public void ProcessMouseLBClick(int x, int y)
        {
            ProcessMouseMove(x, y);

            if (_ArrowLeftSelected)
                PrevValue();

            if (_ArrowRightSelected)
                NextValue();

            for (int i = 0; i < _ValueBounds.Count; i++)
            {
                if (CHelper.IsInBounds(_ValueBounds[i], x, y))
                {
                    int offset = _Selection - (int)_NumVisible / 2;

                    if (_ValueNames.Count - _NumVisible - offset < 0)
                        offset = _ValueNames.Count - _NumVisible;

                    if (offset < 0)
                        offset = 0;

                    Selection = i + offset;
                    _ValueBounds.Clear();
                    break;
                }
            }
        }

        public void Draw()
        {
            if (!Visible && CSettings.GameState != EGameState.EditTheme)
                return;

            STexture Texture = CTheme.GetSkinTexture(_Theme.TextureName);
            STexture TextureArrowLeft = CTheme.GetSkinTexture(_Theme.TextureArrowLeftName);
            STexture TextureArrowRight = CTheme.GetSkinTexture(_Theme.TextureArrowRightName);

            STexture STexture = CTheme.GetSkinTexture(_Theme.STextureName);
            STexture STextureArrowLeft = CTheme.GetSkinTexture(_Theme.STextureArrowLeftName);
            STexture STextureArrowRight = CTheme.GetSkinTexture(_Theme.STextureArrowRightName);

            STexture HTexture = CTheme.GetSkinTexture(_Theme.HTextureName);

            if (Selected)
            {
                if (Highlighted)
                    CDraw.DrawTexture(HTexture, Rect, HColor);
                else
                    CDraw.DrawTexture(STexture, Rect, SColor);
            }
            else
                CDraw.DrawTexture(Texture, Rect, Color);

            if (_Selection > 0 || CSettings.GameState == EGameState.EditTheme)
            {
                if (_ArrowLeftSelected)
                    CDraw.DrawTexture(STextureArrowLeft, RectArrowLeft, SColorArrow);
                else
                    CDraw.DrawTexture(TextureArrowLeft, RectArrowLeft, ColorArrow);
            }

            if (_Selection < _ValueNames.Count - 1 || CSettings.GameState == EGameState.EditTheme)
            {
                if (_ArrowRightSelected)
                    CDraw.DrawTexture(STextureArrowRight, RectArrowRight, SColorArrow);
                else
                    CDraw.DrawTexture(TextureArrowRight, RectArrowRight, ColorArrow);
            }
            
			if (_NumVisible < 1 || _ValueNames.Count == 0)
				return;

            float x = Rect.X + (Rect.W - TextRelativeX) * 0.1f;
            float dx = (Rect.W - TextRelativeX) * 0.8f / _NumVisible;
            //float y = Rect.Y + (Rect.H - TextH);

            int offset = _Selection - (int)_NumVisible/2;

            if (_ValueNames.Count - _NumVisible - offset < 0)
                offset = _ValueNames.Count - _NumVisible;

            if (offset < 0)
                offset = 0;


            int numvis = _NumVisible;
            if (_ValueNames.Count < numvis)
                numvis = _ValueNames.Count;

            _ValueBounds.Clear();
            for (int i = 0; i < numvis; i++)
            {
                CText Text = new CText(0, 0, 0, TextH, MaxW, EAlignment.Center, _Theme.TextStyle, _Theme.TextFont, TextColor, _ValueNames[i + offset]);
                SColorF Alpha = new SColorF(1f, 1f, 1f, 0.35f);
                if (i + offset == _Selection)
                {
                    Text.Color = STextColor;
                    Alpha = new SColorF(1f, 1f, 1f, 1f);
                }

                RectangleF bounds = CDraw.GetTextBounds(Text);
                Text.X = (x + dx/2f + dx * i)+TextRelativeX;

                if (!WithTextures)
                    Text.Y = (int)((Rect.Y + (Rect.H - bounds.Height) / 2) + TextRelativeY);
                else
                    Text.Y = (int)((Rect.Y + (Rect.H - bounds.Height)) + TextRelativeY);

                Text.Z = Rect.Z;
                Text.Draw();

                if (WithTextures)
                {
                    float dh = Text.Y - Rect.Y - Rect.H * 0.1f;
                    SRectF rect = new SRectF(Text.X - dh / 2, Rect.Y + Rect.H * 0.05f, dh, dh, Rect.Z);
                    CDraw.DrawTexture(_Textures[i + offset], rect, Alpha);
                    _ValueBounds.Add(rect);
                }
                else
                {
                    _ValueBounds.Add(new SRectF(Text.X - bounds.Width/2f, Text.Y, bounds.Width, bounds.Height, Rect.Z));
                }
            }
        }

        public void UnloadTextures()
        {
        }

        public void LoadTextures()
        {
            if (_Theme.ColorName != String.Empty)
                Color = CTheme.GetColor(_Theme.ColorName);

            if (_Theme.SColorName != String.Empty)
                SColor = CTheme.GetColor(_Theme.SColorName);

            if (_Theme.HColorName != String.Empty)
                HColor = CTheme.GetColor(_Theme.HColorName);

            if (_Theme.ArrowColorName != String.Empty)
                ColorArrow = CTheme.GetColor(_Theme.ArrowColorName);

            if (_Theme.SArrowColorName != String.Empty)
                SColorArrow = CTheme.GetColor(_Theme.SArrowColorName);

            if (_Theme.TextColorName != String.Empty)
                TextColor = CTheme.GetColor(_Theme.TextColorName);

            if (_Theme.SColorName != String.Empty)
                STextColor = CTheme.GetColor(_Theme.STextColorName);
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        public object Clone()
        {
            return base.MemberwiseClone();
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            if (!_ArrowLeftSelected && !_ArrowRightSelected)
            {
                Rect.X += stepX;
                Rect.Y += stepY;
            }

            if (_ArrowLeftSelected)
            {
                RectArrowLeft.X += stepX;
                RectArrowLeft.Y += stepY;
            }

            if (_ArrowRightSelected)
            {
                RectArrowRight.X += stepX;
                RectArrowRight.Y += stepY;
            }
        }

        public void ResizeElement(int stepW, int stepH)
        {
            if (!_ArrowLeftSelected && !_ArrowRightSelected)
            {
                Rect.W += stepW;
                if (Rect.W <= 0)
                    Rect.W = 1;

                Rect.H += stepH;
                if (Rect.H <= 0)
                    Rect.H = 1;
            }

            if (_ArrowLeftSelected)
            {
                RectArrowLeft.W += stepW;
                if (RectArrowLeft.W <= 0)
                    RectArrowLeft.W = 1;

                RectArrowLeft.H += stepH;
                if (RectArrowLeft.H <= 0)
                    RectArrowLeft.H = 1;
            }

            if (_ArrowRightSelected)
            {
                RectArrowRight.W += stepW;
                if (RectArrowRight.W <= 0)
                    RectArrowRight.W = 1;

                RectArrowRight.H += stepH;
                if (RectArrowRight.H <= 0)
                    RectArrowRight.H = 1;
            }
        }
        #endregion ThemeEdit
    }
}
