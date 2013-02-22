using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Xml;

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

    public class CSelectSlide : IMenuElement, ICloneable
    {
        private int _PartyModeID;
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
        private List<int> _ValuePartyModeIDs;
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

        public CSelectSlide(int PartyModeID)
        {
            _PartyModeID = PartyModeID;
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
            _ValuePartyModeIDs = new List<int>();
        }

        public CSelectSlide(CSelectSlide slide)
        {
            _PartyModeID = slide._PartyModeID;
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
            _ValuePartyModeIDs = new List<int>(slide._ValuePartyModeIDs);
            _Selection = slide._Selection;
            _NumVisible = slide._NumVisible;

            WithTextures = slide.WithTextures;
            Visible = slide.Visible;
        }

        public bool LoadTheme(string XmlPath, string ElementName, CXMLReader xmlReader, int SkinIndex)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/Skin", ref _Theme.TextureName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinArrowLeft", ref _Theme.TextureArrowLeftName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinArrowRight", ref _Theme.TextureArrowRightName, String.Empty);

            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinSelected", ref _Theme.STextureName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinArrowLeftSelected", ref _Theme.STextureArrowLeftName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinArrowRightSelected", ref _Theme.STextureArrowRightName, String.Empty);

            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinHighlighted", ref _Theme.HTextureName, String.Empty);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref Rect.X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref Rect.Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref Rect.Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/W", ref Rect.W);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref Rect.H);

            if (xmlReader.GetValue(item + "/Color", ref _Theme.ColorName, String.Empty))
            {
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorName, SkinIndex, ref Color);
            }
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref Color.A);
            }

            if (xmlReader.GetValue(item + "/SColor", ref _Theme.SColorName, String.Empty))
            {
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.SColorName, SkinIndex, ref SColor);
            }
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SR", ref SColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SG", ref SColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SB", ref SColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SA", ref SColor.A);
            }

            if (xmlReader.GetValue(item + "/HColor", ref _Theme.HColorName, String.Empty))
            {
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.HColorName, SkinIndex, ref HColor);
            }
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/HR", ref HColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/HG", ref HColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/HB", ref HColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/HA", ref HColor.A);
            }

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowLeftX", ref RectArrowLeft.X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowLeftY", ref RectArrowLeft.Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowLeftZ", ref RectArrowLeft.Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowLeftW", ref RectArrowLeft.W);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowLeftH", ref RectArrowLeft.H);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowRightX", ref RectArrowRight.X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowRightY", ref RectArrowRight.Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowRightZ", ref RectArrowRight.Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowRightW", ref RectArrowRight.W);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowRightH", ref RectArrowRight.H);

            if (xmlReader.GetValue(item + "/ArrowColor", ref _Theme.ArrowColorName, String.Empty))
            {
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ArrowColorName, SkinIndex, ref ColorArrow);
            }
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowR", ref ColorArrow.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowG", ref ColorArrow.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowB", ref ColorArrow.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowA", ref ColorArrow.A);
            }

            if (xmlReader.GetValue(item + "/ArrowSColor", ref _Theme.SArrowColorName, String.Empty))
            {
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.SArrowColorName, SkinIndex, ref SColorArrow);
            }
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowSR", ref SColorArrow.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowSG", ref SColorArrow.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowSB", ref SColorArrow.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowSA", ref SColorArrow.A);
            }

            if (xmlReader.GetValue(item + "/TextColor", ref _Theme.TextColorName, String.Empty))
            {
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.TextColorName, SkinIndex, ref TextColor);
            }
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextR", ref TextColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextG", ref TextColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextB", ref TextColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextA", ref TextColor.A);
            }

            if (xmlReader.GetValue(item + "/TextSColor", ref _Theme.STextColorName, String.Empty))
            {
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.STextColorName, SkinIndex, ref STextColor);
            }
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextSR", ref STextColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextSG", ref STextColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextSB", ref STextColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextSA", ref STextColor.A);
            }

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextH", ref TextH);
            if(xmlReader.TryGetFloatValue(item + "/TextRelativeX", ref TextRelativeX))
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextRelativeX", ref TextRelativeX);
            if(xmlReader.TryGetFloatValue(item + "/TextRelativeY", ref TextRelativeY))
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextRelativeY", ref TextRelativeY);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextMaxW", ref MaxW);
            _ThemeLoaded &= xmlReader.GetValue(item + "/TextFont", ref _Theme.TextFont, "Normal");
            _ThemeLoaded &= xmlReader.TryGetEnumValue<EStyle>(item + "/TextStyle", ref _Theme.TextStyle);

            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/NumVisible", ref _NumVisible);

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

                writer.WriteComment("<TextStyle>: Text style: " + CHelper.ListStrings(Enum.GetNames(typeof(EStyle))));
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
            AddValue(value, _PartyModeID);
        }

        public void AddValue(string value, int PartyModeID)
        {
            AddValue(value, new STexture(-1), _ValueIndexes.Count, PartyModeID);
        }

        public void AddValue(string value, STexture texture)
        {
            AddValue(value, texture, _ValueIndexes.Count, _PartyModeID);
        }

        private void AddValue(string value, STexture texture, int valueIndex)
        {
            AddValue(value, texture, valueIndex, _PartyModeID);
        }

        public void AddValue(string value, STexture texture, int valueIndex, int PartyModeID)
        {
            _ValueNames.Add(value);
            _Textures.Add(texture);
            _ValueIndexes.Add(valueIndex);
            _ValuePartyModeIDs.Add(PartyModeID);

            if (Selection == -1)
                Selection = 0;

            _ValueBounds.Clear();
        }

        public void AddValues(string[] values)
        {
            foreach (string value in values)
            {
                AddValue(value, new STexture(-1), _PartyModeID);
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
            if (!Visible && CBase.Settings.GetGameState() != EGameState.EditTheme)
                return;

            STexture Texture = CBase.Theme.GetSkinTexture(_Theme.TextureName, _PartyModeID);
            STexture TextureArrowLeft = CBase.Theme.GetSkinTexture(_Theme.TextureArrowLeftName, _PartyModeID);
            STexture TextureArrowRight = CBase.Theme.GetSkinTexture(_Theme.TextureArrowRightName, _PartyModeID);

            STexture STexture = CBase.Theme.GetSkinTexture(_Theme.STextureName, _PartyModeID);
            STexture STextureArrowLeft = CBase.Theme.GetSkinTexture(_Theme.STextureArrowLeftName, _PartyModeID);
            STexture STextureArrowRight = CBase.Theme.GetSkinTexture(_Theme.STextureArrowRightName, _PartyModeID);

            STexture HTexture = CBase.Theme.GetSkinTexture(_Theme.HTextureName, _PartyModeID);

            if (Selected)
            {
                if (Highlighted)
                    CBase.Drawing.DrawTexture(HTexture, Rect, HColor);
                else
                    CBase.Drawing.DrawTexture(STexture, Rect, SColor);
            }
            else
                CBase.Drawing.DrawTexture(Texture, Rect, Color);

            if (_Selection > 0 || CBase.Settings.GetGameState() == EGameState.EditTheme)
            {
                if (_ArrowLeftSelected)
                    CBase.Drawing.DrawTexture(STextureArrowLeft, RectArrowLeft, SColorArrow);
                else
                    CBase.Drawing.DrawTexture(TextureArrowLeft, RectArrowLeft, ColorArrow);
            }

            if (_Selection < _ValueNames.Count - 1 || CBase.Settings.GetGameState() == EGameState.EditTheme)
            {
                if (_ArrowRightSelected)
                    CBase.Drawing.DrawTexture(STextureArrowRight, RectArrowRight, SColorArrow);
                else
                    CBase.Drawing.DrawTexture(TextureArrowRight, RectArrowRight, ColorArrow);
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
                CText Text = new CText(0, 0, 0, TextH, MaxW, EAlignment.Center, _Theme.TextStyle, _Theme.TextFont, TextColor, String.Empty);
                Text.PartyModeID = _ValuePartyModeIDs[i + offset];
                Text.Text = _ValueNames[i + offset];

                SColorF Alpha = new SColorF(1f, 1f, 1f, 0.35f);
                if (i + offset == _Selection)
                {
                    Text.Color = STextColor;
                    Alpha = new SColorF(1f, 1f, 1f, 1f);
                }

                RectangleF bounds = CBase.Drawing.GetTextBounds(Text);
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
                    CBase.Drawing.DrawTexture(_Textures[i + offset], rect, Alpha);
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
                Color = CBase.Theme.GetColor(_Theme.ColorName, _PartyModeID);

            if (_Theme.SColorName != String.Empty)
                SColor = CBase.Theme.GetColor(_Theme.SColorName, _PartyModeID);

            if (_Theme.HColorName != String.Empty)
                HColor = CBase.Theme.GetColor(_Theme.HColorName, _PartyModeID);

            if (_Theme.ArrowColorName != String.Empty)
                ColorArrow = CBase.Theme.GetColor(_Theme.ArrowColorName, _PartyModeID);

            if (_Theme.SArrowColorName != String.Empty)
                SColorArrow = CBase.Theme.GetColor(_Theme.SArrowColorName, _PartyModeID);

            if (_Theme.TextColorName != String.Empty)
                TextColor = CBase.Theme.GetColor(_Theme.TextColorName, _PartyModeID);

            if (_Theme.SColorName != String.Empty)
                STextColor = CBase.Theme.GetColor(_Theme.STextColorName, _PartyModeID);
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
