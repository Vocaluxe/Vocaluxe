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
using System.Drawing;
using System.Xml.Serialization;
using VocaluxeLib.Draw;
using VocaluxeLib.Xml;

namespace VocaluxeLib.Menu
{
    [XmlType("SelectSlide")]
    public struct SThemeSelectSlide
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;

        [XmlElement("Skin")] public string TextureName;
        [XmlElement("SkinArrowLeft")] public string TextureArrowLeftName;
        [XmlElement("SkinArrowRight")] public string TextureArrowRightName;

        [XmlElement("SkinSelected")] public string SelTextureName;
        [XmlElement("SkinArrowLeftSelected")] public string SelTextureArrowLeftName;
        [XmlElement("SkinArrowRightSelected")] public string SelTextureArrowRightName;

        [XmlElement("SkinHighlighted")] public string HighlightTextureName;

        [XmlElement("Rect")] public SRectF Rect;
        [XmlElement("RectArrowLeft")] public SRectF RectArrowLeft;
        [XmlElement("RectArrowRight")] public SRectF RectArrowRight;

        [XmlElement("Color")] public SThemeColor Color;
        [XmlElement("SColor")] public SThemeColor SColor;
        [XmlElement("HColor")] public SThemeColor HColor;

        [XmlElement("ArrowColor")] public SThemeColor ArrowColor;
        [XmlElement("ArrowSColor")] public SThemeColor ArrowSColor;

        [XmlElement("TextColor")] public SThemeColor TextColor;
        [XmlElement("TextSColor")] public SThemeColor TextSColor;

        [XmlElement("TextH")] public float TextH;
        [XmlElement("TextRelativeX")] public float TextRelativeX;
        [XmlElement("TextRelativeY")] public float TextRelativeY;
        [XmlElement("TextMaxW")] public float TextMaxW;

        [XmlElement("TextFont")] public string TextFont;
        [XmlElement("TextStyle")] public EStyle TextStyle;

        [XmlElement("NumVisible")] public int NumVisible;
    }

    public class CSelectSlide : IMenuElement, ICloneable
    {
        private readonly int _PartyModeID;
        private SThemeSelectSlide _Theme;
        private bool _ThemeLoaded;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded
        {
            get { return _ThemeLoaded; }
        }

        public SRectF Rect;
        public SRectF RectArrowLeft;
        public SRectF RectArrowRight;

        public SColorF Color;
        public SColorF SelColor;
        public SColorF HighlightColor;

        public SColorF ColorArrow;
        public SColorF SelColorArrow;

        public SColorF TextColor;
        public SColorF SelTextColor;

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
        public bool ValueSelected
        {
            get { return _Selected && !_ArrowLeftSelected && !_ArrowRightSelected; }
        }

        public bool Visible = true;
        public bool Highlighted;

        public bool SelectionByHover;

        private bool _ArrowLeftSelected;
        private bool _ArrowRightSelected;

        private readonly List<string> _ValueNames;
        private readonly List<int> _ValuePartyModeIDs;
        private readonly List<CTextureRef> _Textures;
        private readonly List<int> _ValueIndexes;

        private readonly List<SRectF> _ValueBounds = new List<SRectF>();

        public bool WithTextures;

        private int _Selection = -1;
        public int Selection
        {
            get { return _Selection; }
            set
            {
                if (value >= 0 && value < _ValueNames.Count)
                    _Selection = value;
            }
        }

        public int ValueIndex
        {
            get
            {
                if (_Selection >= 0 && _ValueIndexes.Count > _Selection)
                    return _ValueIndexes[_Selection];
                return -1;
            }
        }

        public int NumValues
        {
            get { return _ValueNames.Count; }
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

        public CSelectSlide(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = new SThemeSelectSlide();
            _ThemeLoaded = false;

            Rect = new SRectF();
            RectArrowLeft = new SRectF();
            RectArrowRight = new SRectF();

            Color = new SColorF();
            SelColor = new SColorF();

            ColorArrow = new SColorF();
            SelColorArrow = new SColorF();

            TextColor = new SColorF();
            SelTextColor = new SColorF();
            TextH = 1f;
            MaxW = 0f;

            _Selected = false;
            _Textures = new List<CTextureRef>();
            _ValueIndexes = new List<int>();
            _ValueNames = new List<string>();
            _ValuePartyModeIDs = new List<int>();
        }

        public CSelectSlide(CSelectSlide slide)
        {
            _PartyModeID = slide._PartyModeID;
            _Theme = new SThemeSelectSlide
                {
                    TextureArrowLeftName = slide._Theme.TextureArrowLeftName,
                    TextureArrowRightName = slide._Theme.TextureArrowRightName,
                    Color = slide._Theme.Color,
                    SColor = slide._Theme.SColor,
                    HColor = slide._Theme.HColor,
                    ArrowColor = slide._Theme.ArrowColor,
                    ArrowSColor = slide._Theme.ArrowSColor,
                    TextColor = slide._Theme.TextColor,
                    TextSColor = slide._Theme.TextSColor,
                    SelTextureName = slide._Theme.SelTextureName,
                    SelTextureArrowLeftName = slide._Theme.SelTextureArrowLeftName,
                    SelTextureArrowRightName = slide._Theme.SelTextureArrowRightName,
                    HighlightTextureName = slide._Theme.HighlightTextureName,
                    TextFont = slide._Theme.TextFont,
                    TextStyle = slide._Theme.TextStyle
                };

            _ThemeLoaded = false;

            Rect = new SRectF(slide.Rect);
            RectArrowLeft = new SRectF(slide.RectArrowLeft);
            RectArrowRight = new SRectF(slide.RectArrowRight);

            Color = new SColorF(slide.Color);
            SelColor = new SColorF(slide.SelColor);

            ColorArrow = new SColorF(slide.ColorArrow);
            SelColorArrow = new SColorF(slide.SelColorArrow);

            TextColor = new SColorF(slide.TextColor);
            SelTextColor = new SColorF(slide.SelTextColor);
            TextH = slide.TextH;
            TextRelativeX = slide.TextRelativeX;
            TextRelativeY = slide.TextRelativeY;
            MaxW = slide.MaxW;

            _Selected = slide._Selected;
            _Textures = new List<CTextureRef>(slide._Textures);
            _ValueIndexes = new List<int>(slide._ValueIndexes);
            _ValueNames = new List<string>(slide._ValueNames);
            _ValueBounds = new List<SRectF>(slide._ValueBounds);
            _ValuePartyModeIDs = new List<int>(slide._ValuePartyModeIDs);
            _Selection = slide._Selection;
            _NumVisible = slide._NumVisible;

            WithTextures = slide.WithTextures;
            Visible = slide.Visible;
        }

        public CSelectSlide(SThemeSelectSlide theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;

            _Selected = false;
            _Textures = new List<CTextureRef>();
            _ValueIndexes = new List<int>();
            _ValueNames = new List<string>();
            _ValuePartyModeIDs = new List<int>();

            LoadTextures();
        }

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader, int skinIndex)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/Skin", out _Theme.TextureName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinArrowLeft", out _Theme.TextureArrowLeftName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinArrowRight", out _Theme.TextureArrowRightName, String.Empty);

            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinSelected", out _Theme.SelTextureName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinArrowLeftSelected", out _Theme.SelTextureArrowLeftName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinArrowRightSelected", out _Theme.SelTextureArrowRightName, String.Empty);

            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinHighlighted", out _Theme.HighlightTextureName, String.Empty);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref Rect.X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref Rect.Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref Rect.Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/W", ref Rect.W);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref Rect.H);

            if (xmlReader.GetValue(item + "/Color", out _Theme.Color.Name, String.Empty))
                _ThemeLoaded &= _Theme.Color.Get(_PartyModeID, out Color);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref Color.A);
            }

            if (xmlReader.GetValue(item + "/SColor", out _Theme.SColor.Name, String.Empty))
                _ThemeLoaded &= _Theme.SColor.Get(_PartyModeID, out SelColor);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SR", ref SelColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SG", ref SelColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SB", ref SelColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SA", ref SelColor.A);
            }

            if (xmlReader.GetValue(item + "/HColor", out _Theme.HColor.Name, String.Empty))
                _ThemeLoaded &= _Theme.HColor.Get(_PartyModeID, out HighlightColor);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/HR", ref HighlightColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/HG", ref HighlightColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/HB", ref HighlightColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/HA", ref HighlightColor.A);
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

            if (xmlReader.GetValue(item + "/ArrowColor", out _Theme.ArrowColor.Name, String.Empty))
                _ThemeLoaded &= _Theme.ArrowColor.Get(_PartyModeID, out ColorArrow);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowR", ref ColorArrow.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowG", ref ColorArrow.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowB", ref ColorArrow.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowA", ref ColorArrow.A);
            }

            if (xmlReader.GetValue(item + "/ArrowSColor", out _Theme.ArrowSColor.Name, String.Empty))
                _ThemeLoaded &= _Theme.ArrowSColor.Get(_PartyModeID, out SelColorArrow);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowSR", ref SelColorArrow.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowSG", ref SelColorArrow.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowSB", ref SelColorArrow.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowSA", ref SelColorArrow.A);
            }

            if (xmlReader.GetValue(item + "/TextColor", out _Theme.TextColor.Name, String.Empty))
                _ThemeLoaded &= _Theme.TextColor.Get(_PartyModeID, out TextColor);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextR", ref TextColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextG", ref TextColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextB", ref TextColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextA", ref TextColor.A);
            }

            if (xmlReader.GetValue(item + "/TextSColor", out _Theme.TextSColor.Name, String.Empty))
                _ThemeLoaded &= _Theme.TextSColor.Get(_PartyModeID, out SelTextColor);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextSR", ref SelTextColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextSG", ref SelTextColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextSB", ref SelTextColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextSA", ref SelTextColor.A);
            }

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextH", ref TextH);
            if (xmlReader.TryGetFloatValue(item + "/TextRelativeX", ref TextRelativeX))
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextRelativeX", ref TextRelativeX);
            if (xmlReader.TryGetFloatValue(item + "/TextRelativeY", ref TextRelativeY))
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextRelativeY", ref TextRelativeY);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextMaxW", ref MaxW);
            _ThemeLoaded &= xmlReader.GetValue(item + "/TextFont", out _Theme.TextFont, "Normal");
            _ThemeLoaded &= xmlReader.TryGetEnumValue(item + "/TextStyle", ref _Theme.TextStyle);

            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/NumVisible", ref _NumVisible);

            if (_ThemeLoaded)
            {
                _Theme.Name = elementName;
                _Theme.ArrowColor.Color = new SColorF(ColorArrow);
                _Theme.ArrowSColor.Color = new SColorF(SelColorArrow);
                _Theme.Color.Color = new SColorF(Color);
                _Theme.HColor.Color = new SColorF(HighlightColor);
                _Theme.NumVisible = _NumVisible;
                _Theme.Rect = new SRectF(Rect);
                _Theme.RectArrowLeft = new SRectF(RectArrowLeft);
                _Theme.RectArrowRight = new SRectF(RectArrowRight);
                _Theme.SColor.Color = new SColorF(SelColor);
                _Theme.TextColor.Color = new SColorF(TextColor);
                _Theme.TextH = TextH;
                _Theme.TextMaxW = MaxW;
                _Theme.TextRelativeX = TextRelativeX;
                _Theme.TextRelativeY = TextRelativeY;
                _Theme.TextSColor.Color = new SColorF(SelTextColor);

                LoadTextures();
            }
            return _ThemeLoaded;
        }

        public void AddValue(string value)
        {
            AddValue(value, _PartyModeID);
        }

        public void AddValue(string value, int partyModeID)
        {
            AddValue(value, null, _ValueIndexes.Count, partyModeID);
        }

        public void AddValue(string value, CTextureRef texture)
        {
            AddValue(value, texture, _ValueIndexes.Count, _PartyModeID);
        }

        private void _AddValue(string value, CTextureRef texture, int valueIndex)
        {
            AddValue(value, texture, valueIndex, _PartyModeID);
        }

        public void AddValue(string value, CTextureRef texture, int valueIndex, int partyModeID)
        {
            _ValueNames.Add(value);
            _Textures.Add(texture);
            _ValueIndexes.Add(valueIndex);
            _ValuePartyModeIDs.Add(partyModeID);

            if (Selection == -1)
                Selection = 0;

            _ValueBounds.Clear();
        }

        public void AddValues(IEnumerable<string> values)
        {
            foreach (string value in values)
                _AddValue(value, null, _PartyModeID);

            _ValueBounds.Clear();
        }

        public void AddValues(string[] values, CTextureRef[] textures)
        {
            if (values.Length != textures.Length)
                return;

            for (int i = 0; i < values.Length; i++)
                AddValue(values[i], textures[i]);
            if (Selection == -1)
                Selection = 0;

            _ValueBounds.Clear();
        }

        public void SetValues<T>(int selection)
        {
            AddValues(Enum.GetNames(typeof(T)));
            Selection = selection;
        }

        public void RenameValue(string newName)
        {
            RenameValue(Selection, newName);
        }

        public void RenameValue(int selection, string newName, CTextureRef newTexture = null)
        {
            if (selection < 0 && selection >= _ValueNames.Count)
                return;

            _ValueNames[selection] = newName;
            _Textures[selection] = newTexture;
        }

        public bool SetSelectionByValueIndex(int valueIndex)
        {
            for (int i = 0; i < _ValueIndexes.Count; i++)
            {
                if (_ValueIndexes[i] == valueIndex)
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

            if (SelectionByHover)
            {
                for (int i = 0; i < _ValueBounds.Count; i++)
                {
                    if (CHelper.IsInBounds(_ValueBounds[i], x, y))
                    {
                        int offset = _Selection - _NumVisible / 2;

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
                    int offset = _Selection - _NumVisible / 2;

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
            if (!Visible && CBase.Settings.GetProgramState() != EProgramState.EditTheme)
                return;

            CTextureRef texture = CBase.Themes.GetSkinTexture(_Theme.TextureName, _PartyModeID);
            CTextureRef textureArrowLeft = CBase.Themes.GetSkinTexture(_Theme.TextureArrowLeftName, _PartyModeID);
            CTextureRef textureArrowRight = CBase.Themes.GetSkinTexture(_Theme.TextureArrowRightName, _PartyModeID);

            CTextureRef selTexture = CBase.Themes.GetSkinTexture(_Theme.SelTextureName, _PartyModeID);
            CTextureRef selTextureArrowLeft = CBase.Themes.GetSkinTexture(_Theme.SelTextureArrowLeftName, _PartyModeID);
            CTextureRef selTextureArrowRight = CBase.Themes.GetSkinTexture(_Theme.SelTextureArrowRightName, _PartyModeID);

            CTextureRef highlightTexture = CBase.Themes.GetSkinTexture(_Theme.HighlightTextureName, _PartyModeID);

            if (Selected)
            {
                if (Highlighted)
                    CBase.Drawing.DrawTexture(highlightTexture, Rect, HighlightColor);
                else
                    CBase.Drawing.DrawTexture(selTexture, Rect, SelColor);
            }
            else
                CBase.Drawing.DrawTexture(texture, Rect, Color);

            if (_Selection > 0 || CBase.Settings.GetProgramState() == EProgramState.EditTheme)
            {
                if (_ArrowLeftSelected)
                    CBase.Drawing.DrawTexture(selTextureArrowLeft, RectArrowLeft, SelColorArrow);
                else
                    CBase.Drawing.DrawTexture(textureArrowLeft, RectArrowLeft, ColorArrow);
            }

            if (_Selection < _ValueNames.Count - 1 || CBase.Settings.GetProgramState() == EProgramState.EditTheme)
            {
                if (_ArrowRightSelected)
                    CBase.Drawing.DrawTexture(selTextureArrowRight, RectArrowRight, SelColorArrow);
                else
                    CBase.Drawing.DrawTexture(textureArrowRight, RectArrowRight, ColorArrow);
            }

            if (_NumVisible < 1 || _ValueNames.Count == 0)
                return;

            float x = Rect.X + (Rect.W - TextRelativeX) * 0.1f;
            float dx = (Rect.W - TextRelativeX) * 0.8f / _NumVisible;
            //float y = Rect.Y + (Rect.H - TextH);
            int offset = _Selection - _NumVisible / 2;

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
                var text = new CText(0, 0, 0, TextH, MaxW, EAlignment.Center, _Theme.TextStyle, _Theme.TextFont, TextColor, "T",
                                     _ValuePartyModeIDs[i + offset]);

                if (_ValueNames[i + offset] != "")
                    text.Text = _ValueNames[i + offset];
                else
                    text.Visible = false;

                var alpha = new SColorF(1f, 1f, 1f, 0.35f);
                if (i + offset == _Selection)
                {
                    text.Color = SelTextColor;
                    alpha = new SColorF(1f, 1f, 1f, 1f);
                }

                RectangleF bounds = CBase.Fonts.GetTextBounds(text);
                text.X = (x + dx / 2f + dx * i) + TextRelativeX;

                if (!WithTextures)
                    text.Y = (int)((Rect.Y + (Rect.H - bounds.Height) / 2) + TextRelativeY);
                else
                    text.Y = (int)((Rect.Y + (Rect.H - bounds.Height)) + TextRelativeY);

                text.Z = Rect.Z;
                text.Draw();

                if (WithTextures)
                {
                    float dh = text.Y - Rect.Y - Rect.H * 0.1f;
                    var rect = new SRectF(text.X - dh / 2, Rect.Y + Rect.H * 0.05f, dh, dh, Rect.Z);
                    CBase.Drawing.DrawTexture(_Textures[i + offset], rect, alpha, rect);
                    _ValueBounds.Add(rect);
                }
                else
                    _ValueBounds.Add(new SRectF(text.X - bounds.Width / 2f, text.Y, bounds.Width, bounds.Height, Rect.Z));
            }
        }

        public SThemeSelectSlide GetTheme()
        {
            return _Theme;
        }

        public void UnloadTextures() {}

        public void LoadTextures()
        {
            _Theme.Color.Get(_PartyModeID, out Color);
            _Theme.SColor.Get(_PartyModeID, out SelColor);
            _Theme.HColor.Get(_PartyModeID, out HighlightColor);
            _Theme.ArrowColor.Get(_PartyModeID, out ColorArrow);
            _Theme.ArrowSColor.Get(_PartyModeID, out SelColorArrow);
            _Theme.TextColor.Get(_PartyModeID, out TextColor);
            _Theme.TextSColor.Get(_PartyModeID, out SelTextColor);

            Rect = _Theme.Rect;
            RectArrowLeft = _Theme.RectArrowLeft;
            RectArrowRight = _Theme.RectArrowRight;

            NumVisible = _Theme.NumVisible;

            TextH = _Theme.TextH;
            TextRelativeX = _Theme.TextRelativeX;
            TextRelativeY = _Theme.TextRelativeY;
            MaxW = _Theme.TextMaxW;
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            if (!_ArrowLeftSelected && !_ArrowRightSelected)
            {
                Rect.X += stepX;
                Rect.Y += stepY;

                _Theme.Rect.X += stepX;
                _Theme.Rect.Y += stepY;
            }

            if (_ArrowLeftSelected)
            {
                RectArrowLeft.X += stepX;
                RectArrowLeft.Y += stepY;

                _Theme.RectArrowLeft.X += stepX;
                _Theme.RectArrowLeft.Y += stepY;
            }

            if (_ArrowRightSelected)
            {
                RectArrowRight.X += stepX;
                RectArrowRight.Y += stepY;

                _Theme.RectArrowRight.X += stepX;
                _Theme.RectArrowRight.Y += stepY;
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

                _Theme.Rect.W = Rect.W;
                _Theme.Rect.H = Rect.H;
            }

            if (_ArrowLeftSelected)
            {
                RectArrowLeft.W += stepW;
                if (RectArrowLeft.W <= 0)
                    RectArrowLeft.W = 1;

                RectArrowLeft.H += stepH;
                if (RectArrowLeft.H <= 0)
                    RectArrowLeft.H = 1;

                _Theme.RectArrowLeft.W = RectArrowLeft.W;
                _Theme.RectArrowLeft.H = RectArrowLeft.H;
            }

            if (_ArrowRightSelected)
            {
                RectArrowRight.W += stepW;
                if (RectArrowRight.W <= 0)
                    RectArrowRight.W = 1;

                RectArrowRight.H += stepH;
                if (RectArrowRight.H <= 0)
                    RectArrowRight.H = 1;

                _Theme.RectArrowRight.W = RectArrowRight.W;
                _Theme.RectArrowRight.H = RectArrowRight.H;
            }
        }
        #endregion ThemeEdit
    }
}