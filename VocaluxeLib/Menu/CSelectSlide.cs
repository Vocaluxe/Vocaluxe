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
using System.Collections.Generic;
using System.Drawing;
using System.Xml;

namespace VocaluxeLib.Menu
{
    struct SThemeSelectSlide
    {
        public string Name;

        public string TextureName;
        public string TextureArrowLeftName;
        public string TextureArrowRightName;

        public string SelTextureName;
        public string SelTextureArrowLeftName;
        public string SelTextureArrowRightName;

        public string HighlightTextureName;

        public string ColorName;
        public string SelColorName;
        public string HighlightColorName;

        public string ArrowColorName;
        public string SelArrowColorName;

        public string TextColorName;
        public string SelTextColorName;

        public string TextFont;
        public EStyle TextStyle;
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

        private bool _ArrowLeftSelected;
        private bool _ArrowRightSelected;

        private readonly List<string> _ValueNames;
        private readonly List<int> _ValuePartyModeIDs;
        private readonly List<STexture> _Textures;
        private readonly List<int> _ValueIndexes;

        private readonly List<SRectF> _ValueBounds = new List<SRectF>();

        public readonly bool WithTextures;

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
            _Textures = new List<STexture>();
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
                    SelTextureName = slide._Theme.SelTextureName,
                    SelTextureArrowLeftName = slide._Theme.SelTextureArrowLeftName,
                    SelTextureArrowRightName = slide._Theme.SelTextureArrowRightName,
                    HighlightTextureName = slide._Theme.HighlightTextureName,
                    ColorName = slide._Theme.ColorName,
                    SelColorName = slide._Theme.SelColorName,
                    HighlightColorName = slide._Theme.HighlightColorName,
                    ArrowColorName = slide._Theme.ArrowColorName,
                    SelArrowColorName = slide._Theme.SelArrowColorName,
                    TextColorName = slide._Theme.TextColorName,
                    SelTextColorName = slide._Theme.SelTextColorName,
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

            if (xmlReader.GetValue(item + "/Color", out _Theme.ColorName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorName, skinIndex, out Color);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref Color.A);
            }

            if (xmlReader.GetValue(item + "/SColor", out _Theme.SelColorName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.SelColorName, skinIndex, out SelColor);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SR", ref SelColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SG", ref SelColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SB", ref SelColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SA", ref SelColor.A);
            }

            if (xmlReader.GetValue(item + "/HColor", out _Theme.HighlightColorName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.HighlightColorName, skinIndex, out HighlightColor);
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

            if (xmlReader.GetValue(item + "/ArrowColor", out _Theme.ArrowColorName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ArrowColorName, skinIndex, out ColorArrow);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowR", ref ColorArrow.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowG", ref ColorArrow.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowB", ref ColorArrow.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowA", ref ColorArrow.A);
            }

            if (xmlReader.GetValue(item + "/ArrowSColor", out _Theme.SelArrowColorName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.SelArrowColorName, skinIndex, out SelColorArrow);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowSR", ref SelColorArrow.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowSG", ref SelColorArrow.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowSB", ref SelColorArrow.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/ArrowSA", ref SelColorArrow.A);
            }

            if (xmlReader.GetValue(item + "/TextColor", out _Theme.TextColorName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.TextColorName, skinIndex, out TextColor);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextR", ref TextColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextG", ref TextColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextB", ref TextColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/TextA", ref TextColor.A);
            }

            if (xmlReader.GetValue(item + "/TextSColor", out _Theme.SelTextColorName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.SelTextColorName, skinIndex, out SelTextColor);
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
                writer.WriteElementString("SkinSelected", _Theme.SelTextureName);

                writer.WriteComment("<SkinArrowLeftSelected>: Texture name of selected left arrow");
                writer.WriteElementString("SkinArrowLeftSelected", _Theme.SelTextureArrowLeftName);

                writer.WriteComment("<SkinArrowRightSelected>: Texture name of selected right arrow");
                writer.WriteElementString("SkinArrowRightSelected", _Theme.SelTextureArrowRightName);

                writer.WriteComment("<SkinHighlighted>: Texture name for highlighted SelectSlide");
                writer.WriteElementString("SkinHighlighted", _Theme.HighlightTextureName);

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
                if (_Theme.ColorName != "")
                    writer.WriteElementString("Color", _Theme.ColorName);
                else
                {
                    writer.WriteElementString("R", Color.R.ToString("#0.00"));
                    writer.WriteElementString("G", Color.G.ToString("#0.00"));
                    writer.WriteElementString("B", Color.B.ToString("#0.00"));
                    writer.WriteElementString("A", Color.A.ToString("#0.00"));
                }

                writer.WriteComment("<SColor>: Selected SelectSlide color from ColorScheme (high priority)");
                writer.WriteComment("or <SR>, <SG>, <SB>, <SA> (lower priority)");
                if (_Theme.SelColorName != "")
                    writer.WriteElementString("SColor", _Theme.SelColorName);
                else
                {
                    writer.WriteElementString("SR", SelColor.R.ToString("#0.00"));
                    writer.WriteElementString("SG", SelColor.G.ToString("#0.00"));
                    writer.WriteElementString("SB", SelColor.B.ToString("#0.00"));
                    writer.WriteElementString("SA", SelColor.A.ToString("#0.00"));
                }

                writer.WriteComment("<HColor>: Highlighted SelectSlide color from ColorScheme (high priority)");
                writer.WriteComment("or <HR>, <HG>, <HB>, <HA> (lower priority)");
                if (_Theme.HighlightColorName != "")
                    writer.WriteElementString("HColor", _Theme.HighlightColorName);
                else
                {
                    writer.WriteElementString("HR", HighlightColor.R.ToString("#0.00"));
                    writer.WriteElementString("HG", HighlightColor.G.ToString("#0.00"));
                    writer.WriteElementString("HB", HighlightColor.B.ToString("#0.00"));
                    writer.WriteElementString("HA", HighlightColor.A.ToString("#0.00"));
                }

                writer.WriteComment("<ArrowColor>: Arrow color from ColorScheme (high priority)");
                writer.WriteComment("or <ArrowR>, <ArrowG>, <ArrowB>, <ArrowA> (lower priority)");
                if (_Theme.ArrowColorName != "")
                    writer.WriteElementString("ArrowColor", _Theme.ArrowColorName);
                else
                {
                    writer.WriteElementString("ArrowR", ColorArrow.R.ToString("#0.00"));
                    writer.WriteElementString("ArrowG", ColorArrow.G.ToString("#0.00"));
                    writer.WriteElementString("ArrowB", ColorArrow.B.ToString("#0.00"));
                    writer.WriteElementString("ArrowA", ColorArrow.A.ToString("#0.00"));
                }

                writer.WriteComment("<ArrowSColor>: Selected arrow color from ColorScheme (high priority)");
                writer.WriteComment("or <ArrowSR>, <ArrowSG>, <ArrowSB>, <ArrowSA> (lower priority)");
                if (_Theme.SelArrowColorName != "")
                    writer.WriteElementString("ArrowSColor", _Theme.SelArrowColorName);
                else
                {
                    writer.WriteElementString("ArrowSR", SelColorArrow.R.ToString("#0.00"));
                    writer.WriteElementString("ArrowSG", SelColorArrow.G.ToString("#0.00"));
                    writer.WriteElementString("ArrowSB", SelColorArrow.B.ToString("#0.00"));
                    writer.WriteElementString("ArrowSA", SelColorArrow.A.ToString("#0.00"));
                }

                writer.WriteComment("<TextColor>: Text color from ColorScheme (high priority)");
                writer.WriteComment("or <TextR>, <TextG>, <TextB>, <TextA> (lower priority)");
                if (_Theme.TextColorName != "")
                    writer.WriteElementString("TextColor", _Theme.TextColorName);
                else
                {
                    writer.WriteElementString("TextR", TextColor.R.ToString("#0.00"));
                    writer.WriteElementString("TextG", TextColor.G.ToString("#0.00"));
                    writer.WriteElementString("TextB", TextColor.B.ToString("#0.00"));
                    writer.WriteElementString("TextA", TextColor.A.ToString("#0.00"));
                }

                writer.WriteComment("<TextSColor>: Selected text color from ColorScheme (high priority)");
                writer.WriteComment("or <TextSR>, <TextSG>, <TextSB>, <TextSA> (lower priority)");
                if (_Theme.SelTextColorName != "")
                    writer.WriteElementString("TextSColor", _Theme.SelTextColorName);
                else
                {
                    writer.WriteElementString("TextSR", SelTextColor.R.ToString("#0.00"));
                    writer.WriteElementString("TextSG", SelTextColor.G.ToString("#0.00"));
                    writer.WriteElementString("TextSB", SelTextColor.B.ToString("#0.00"));
                    writer.WriteElementString("TextSA", SelTextColor.A.ToString("#0.00"));
                }

                writer.WriteComment("<TextH>: Text height");
                writer.WriteElementString("TextH", TextH.ToString("#0.00"));

                writer.WriteComment("<TextRelativeX>: Text relative x-position");
                if (Math.Abs(TextRelativeX) > 0.01)
                    writer.WriteElementString("TextRelativeX", TextRelativeX.ToString("#0.00"));

                writer.WriteComment("<TextRelativeY>: Text relative y-position");
                if (Math.Abs(TextRelativeY) > 0.01)
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

        public void AddValue(string value, int partyModeID)
        {
            AddValue(value, new STexture(-1), _ValueIndexes.Count, partyModeID);
        }

        public void AddValue(string value, STexture texture)
        {
            AddValue(value, texture, _ValueIndexes.Count, _PartyModeID);
        }

        private void _AddValue(string value, STexture texture, int valueIndex)
        {
            AddValue(value, texture, valueIndex, _PartyModeID);
        }

        public void AddValue(string value, STexture texture, int valueIndex, int partyModeID)
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
                _AddValue(value, new STexture(-1), _PartyModeID);

            _ValueBounds.Clear();
        }

        public void AddValues(string[] values, STexture[] textures)
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

        public void RenameValue(int selection, string newName)
        {
            RenameValue(selection, newName, new STexture(-1));
        }

        public void RenameValue(int selection, string newName, STexture newTexture)
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
            if (!Visible && CBase.Settings.GetGameState() != EGameState.EditTheme)
                return;

            STexture texture = CBase.Theme.GetSkinTexture(_Theme.TextureName, _PartyModeID);
            STexture textureArrowLeft = CBase.Theme.GetSkinTexture(_Theme.TextureArrowLeftName, _PartyModeID);
            STexture textureArrowRight = CBase.Theme.GetSkinTexture(_Theme.TextureArrowRightName, _PartyModeID);

            STexture selTexture = CBase.Theme.GetSkinTexture(_Theme.SelTextureName, _PartyModeID);
            STexture selTextureArrowLeft = CBase.Theme.GetSkinTexture(_Theme.SelTextureArrowLeftName, _PartyModeID);
            STexture selTextureArrowRight = CBase.Theme.GetSkinTexture(_Theme.SelTextureArrowRightName, _PartyModeID);

            STexture highlightTexture = CBase.Theme.GetSkinTexture(_Theme.HighlightTextureName, _PartyModeID);

            if (Selected)
            {
                if (Highlighted)
                    CBase.Drawing.DrawTexture(highlightTexture, Rect, HighlightColor);
                else
                    CBase.Drawing.DrawTexture(selTexture, Rect, SelColor);
            }
            else
                CBase.Drawing.DrawTexture(texture, Rect, Color);

            if (_Selection > 0 || CBase.Settings.GetGameState() == EGameState.EditTheme)
            {
                if (_ArrowLeftSelected)
                    CBase.Drawing.DrawTexture(selTextureArrowLeft, RectArrowLeft, SelColorArrow);
                else
                    CBase.Drawing.DrawTexture(textureArrowLeft, RectArrowLeft, ColorArrow);
            }

            if (_Selection < _ValueNames.Count - 1 || CBase.Settings.GetGameState() == EGameState.EditTheme)
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
                CText text = new CText(0, 0, 0, TextH, MaxW, EAlignment.Center, _Theme.TextStyle, _Theme.TextFont, TextColor, _ValueNames[i + offset],
                                       _ValuePartyModeIDs[i + offset]);

                SColorF alpha = new SColorF(1f, 1f, 1f, 0.35f);
                if (i + offset == _Selection)
                {
                    text.Color = SelTextColor;
                    alpha = new SColorF(1f, 1f, 1f, 1f);
                }

                RectangleF bounds = CBase.Drawing.GetTextBounds(text);
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
                    SRectF rect = new SRectF(text.X - dh / 2, Rect.Y + Rect.H * 0.05f, dh, dh, Rect.Z);
                    CBase.Drawing.DrawTexture(_Textures[i + offset], rect, alpha);
                    _ValueBounds.Add(rect);
                }
                else
                    _ValueBounds.Add(new SRectF(text.X - bounds.Width / 2f, text.Y, bounds.Width, bounds.Height, Rect.Z));
            }
        }

        public void UnloadTextures() {}

        public void LoadTextures()
        {
            if (_Theme.ColorName != "")
                Color = CBase.Theme.GetColor(_Theme.ColorName, _PartyModeID);

            if (_Theme.SelColorName != "")
                SelColor = CBase.Theme.GetColor(_Theme.SelColorName, _PartyModeID);

            if (_Theme.HighlightColorName != "")
                HighlightColor = CBase.Theme.GetColor(_Theme.HighlightColorName, _PartyModeID);

            if (_Theme.ArrowColorName != "")
                ColorArrow = CBase.Theme.GetColor(_Theme.ArrowColorName, _PartyModeID);

            if (_Theme.SelArrowColorName != "")
                SelColorArrow = CBase.Theme.GetColor(_Theme.SelArrowColorName, _PartyModeID);

            if (_Theme.TextColorName != "")
                TextColor = CBase.Theme.GetColor(_Theme.TextColorName, _PartyModeID);

            if (_Theme.SelColorName != "")
                SelTextColor = CBase.Theme.GetColor(_Theme.SelTextColorName, _PartyModeID);
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