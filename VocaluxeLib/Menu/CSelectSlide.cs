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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Xml.Serialization;
using VocaluxeLib.Draw;

namespace VocaluxeLib.Menu
{
    [XmlType("SelectSlide")]
    public struct SThemeSelectSlide
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;

        public string Skin;
        public string SkinArrowLeft;
        public string SkinArrowRight;

        public string SkinSelected;
        public string SkinArrowLeftSelected;
        public string SkinArrowRightSelected;

        public SRectF Rect;
        public SRectF RectArrowLeft;
        public SRectF RectArrowRight;

        public SThemeColor Color;
        public SThemeColor SelColor;

        public SThemeColor ArrowColor;
        public SThemeColor ArrowSelColor;

        public SThemeColor TextColor;
        public SThemeColor TextSelColor;

        public float TextH;
        [DefaultValue(0.0f)] public float TextRelativeX;
        [DefaultValue(0.0f)] public float TextRelativeY;
        public float TextMaxW;

        public string TextFont;
        public EStyle TextStyle;

        public int NumVisible;
    }

    public sealed class CSelectSlide : CMenuElementBase, IMenuElement, IThemeable
    {
        private struct SValue
        {
            public string Text;
            public int TranslationId;
            public int Tag;
            public CTextureRef Texture;
        }

        private class CElement
        {
            public CText Text;
            public CStatic Img;
            public SRectF Bounds;
        }

        private readonly int _PartyModeID;
        private SThemeSelectSlide _Theme;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded { get; private set; }

        public SRectF RectArrowLeft;
        public SRectF RectArrowRight;

        private SColorF _Color;
        private SColorF _SelColor;

        private SColorF _ColorArrow;
        private SColorF _SelColorArrow;

        private SColorF _TextColor;
        private SColorF _SelTextColor;

        private float _TextRelativeX;
        private float _TextRelativeY;
        private float _TextH;
        private float _MaxW;

        public bool Selectable
        {
            get { return Visible; }
        }
        public override float X
        {
            set
            {
                float delta = value - X;
                RectArrowLeft.X += delta;
                RectArrowRight.X += delta;
                base.X = value;
            }
        }
        public override float Y
        {
            set
            {
                float delta = value - Y;
                RectArrowLeft.Y += delta;
                RectArrowRight.Y += delta;
                base.Y = value;
            }
        }
        public override float Z
        {
            set
            {
                float delta = value - Z;
                RectArrowLeft.Z += delta;
                RectArrowRight.Z += delta;
                base.Z = value;
            }
        }

        public override bool Selected
        {
            set
            {
                base.Selected = value;

                if (!value)
                {
                    _ArrowLeftSelected = false;
                    _ArrowRightSelected = false;
                }
            }
        }
        public bool IsValueSelected
        {
            get { return Selected && !_ArrowLeftSelected && !_ArrowRightSelected; }
        }

        private bool _ArrowLeftSelected;
        private bool _ArrowRightSelected;

        public bool SelectByHovering;
        public bool DrawTextures;

        private int _Selection = -1;
        public int Selection
        {
            get { return _Selection; }
            set
            {
                value = value.Clamp(0, _Values.Count - 1, false);
                if (value == _Selection)
                    return;
                _Selection = value;
                _Invalidate();
            }
        }

        public string SelectedValue
        {
            get { return _Selection >= 0 ? _Values[_Selection].Text : null; }
            set { Selection = _Values.FindIndex(val => val.Text == value); }
        }

        public int SelectedTag
        {
            get { return _Selection >= 0 ? _Values[_Selection].Tag : -1; }
            set { Selection = _Values.FindIndex(val => val.Tag == value); }
        }

        public int NumValues
        {
            get { return _Values.Count; }
        }

        private readonly List<SValue> _Values = new List<SValue>();
        private readonly List<CElement> _VisibleElements = new List<CElement>();
        private bool _NeedsRevalidate = true;

        private int _NumVisible = -1;
        private CTextureRef _Texture;
        private CTextureRef _TextureArrowLeft;
        private CTextureRef _TextureArrowRight;
        private CTextureRef _SelTexture;
        private CTextureRef _SelTextureArrowLeft;
        private CTextureRef _SelTextureArrowRight;

        public int NumVisible
        {
            get { return _NumVisible; }
            set
            {
                if (value <= 0 || value == _NumVisible)
                    return;
                _NumVisible = value;

                _Invalidate();
            }
        }

        public CSelectSlide(int partyModeID)
        {
            _PartyModeID = partyModeID;
            ThemeLoaded = false;
            _TextH = 1f;
            _MaxW = 0f;
        }

        public CSelectSlide(CSelectSlide slide)
        {
            _PartyModeID = slide._PartyModeID;
            _Theme = slide._Theme;

            ThemeLoaded = false;

            MaxRect = slide.MaxRect;
            RectArrowLeft = slide.RectArrowLeft;
            RectArrowRight = slide.RectArrowRight;

            _Color = slide._Color;
            _SelColor = slide._SelColor;

            _ColorArrow = slide._ColorArrow;
            _SelColorArrow = slide._SelColorArrow;

            _TextColor = slide._TextColor;
            _SelTextColor = slide._SelTextColor;
            _TextH = slide._TextH;
            _TextRelativeX = slide._TextRelativeX;
            _TextRelativeY = slide._TextRelativeY;
            _MaxW = slide._MaxW;

            _Values.AddRange(slide._Values);
            _Selection = slide._Selection;
            _NumVisible = slide._NumVisible;

            DrawTextures = slide.DrawTextures;
            Visible = slide.Visible;
        }

        public CSelectSlide(SThemeSelectSlide theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;

            ThemeLoaded = true;
        }

        private void _Invalidate()
        {
            _NeedsRevalidate = true;
        }

        /// <summary>
        ///     Adds an integer to the slide setting its tag to the value and its text to the text representation of the tag
        /// </summary>
        /// <param name="tag">Value to add</param>
        public void AddValue(int tag)
        {
            AddValue(tag.ToString(), null, tag);
        }

        /// <summary>
        ///     Adds an entry to the slide.
        /// </summary>
        /// <param name="text">Label to show</param>
        /// <param name="texture">Texture to show</param>
        /// <param name="tag">User value (e.g. id of entry)</param>
        public void AddValue(string text, CTextureRef texture = null, int tag = 0)
        {
            AddValue(text, _PartyModeID, texture, tag);
        }

        /// <summary>
        ///     Adds an entry to the slide.
        /// </summary>
        /// <param name="text">Label to show</param>
        /// <param name="translationId">Translation id to use for the text</param>
        /// <param name="texture">Texture to show</param>
        /// <param name="tag">User value (e.g. id of entry)</param>
        public void AddValue(string text, int translationId, CTextureRef texture = null, int tag = 0)
        {
            SValue value = new SValue {Text = text, TranslationId = translationId, Tag = tag, Texture = texture};

            _Values.Add(value);

            if (Selection < 0)
                Selection = 0;

            _Invalidate();
        }

        public void AddValues(IEnumerable<string> values)
        {
            foreach (string value in values)
                AddValue(value);
        }

        public void AddValues(IEnumerable<string> values, IEnumerable<int> tags)
        {
            using (var e1 = values.GetEnumerator())
            using (var e2 = tags.GetEnumerator())
            {
                while (e1.MoveNext() && e2.MoveNext())
                {
                    AddValue(e1.Current, tag: e2.Current);
                }
                if (e1.MoveNext() || e2.MoveNext())
                {
                    throw new ArgumentException("the lists must have the same length");
                }
            }
        }

        public void AddValues(string[] values, CTextureRef[] textures)
        {
            Debug.Assert(values.Length == textures.Length);

            for (int i = 0; i < values.Length; i++)
                AddValue(values[i], textures[i]);
        }

        public void RemoveValue(string text)
        {
            int idx = _Values.FindIndex(val => val.Text == text);
            if (idx < 0)
                return;
            _Values.RemoveAt(idx);
            if (Selection >= idx)
                Selection--;
            _Invalidate();
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

        public void RenameValue(int index, string newName, CTextureRef newTexture = null)
        {
            if (index < 0 && index >= _Values.Count)
                return;

            SValue value = _Values[index];
            value.Text = newName;
            value.Texture = newTexture;
            _Values[index] = value;
        }

        public bool SelectNextValue()
        {
            if (Selection < _Values.Count - 1)
            {
                Selection++;
                return true;
            }
            return false;
        }

        public bool SelectPrevValue()
        {
            if (Selection > 0)
            {
                Selection--;
                return true;
            }
            return false;
        }

        public void SelectFirstValue()
        {
            if (_Values.Count > 0)
                Selection = 0;
        }

        public void SelectLastValue()
        {
            if (_Values.Count > 0)
                Selection = _Values.Count - 1;
        }

        public void Clear()
        {
            _Values.Clear();
            _Selection = -1;
            _Invalidate();
        }

        private int _GetCurOffset()
        {
            int offset = _Selection - _NumVisible / 2;
            return offset.Clamp(0, _Values.Count - _NumVisible, true);
        }

        private void _SelectAtPos(int x, int y)
        {
            if (_NeedsRevalidate)
                _Revalidate();
            int index = _VisibleElements.FindIndex(el => CHelper.IsInBounds(el.Bounds, x, y));
            if (index < 0)
                return;
            Selection = index + _GetCurOffset();
        }

        public void ProcessMouseMove(int x, int y)
        {
            _ArrowLeftSelected = CHelper.IsInBounds(RectArrowLeft, x, y) && _Selection > 0;
            _ArrowRightSelected = CHelper.IsInBounds(RectArrowRight, x, y) && _Selection < _Values.Count - 1;

            if (SelectByHovering)
                _SelectAtPos(x, y);
        }

        public void ProcessMouseLBClick(int x, int y)
        {
            ProcessMouseMove(x, y);

            if (_ArrowLeftSelected)
                SelectPrevValue();

            if (_ArrowRightSelected)
                SelectNextValue();

            _SelectAtPos(x, y);
        }

        private void _Revalidate()
        {
            if (!_NeedsRevalidate)
                return;
            _NeedsRevalidate = false;
            int numvis = Math.Min(_NumVisible, _Values.Count);
            if (numvis != _VisibleElements.Count)
            {
                _VisibleElements.Clear();
                for (int i = 0; i < numvis; i++)
                {
                    var el = new CElement
                    {
                        Text = new CText(0, 0, 0, _TextH, _MaxW, EAlignment.Center, _Theme.TextStyle, _Theme.TextFont, _TextColor, "T", _PartyModeID),
                        Img = new CStatic(_PartyModeID)
                    };
                    el.Img.Aspect = EAspect.Crop;
                    _VisibleElements.Add(el);
                }
            }
            if (numvis == 0)
                return;

            float elWidth = (Rect.W - _TextRelativeX * 2) / numvis;
            //Center point of the first entry
            float xStart = Rect.X + _TextRelativeX + elWidth / 2f;

            int offset = _GetCurOffset();

            for (int i = 0; i < numvis; i++)
            {
                CText text = _VisibleElements[i].Text;
                RectangleF textBounds;
                float curX = xStart + elWidth * i;
                if (String.IsNullOrEmpty(_Values[i + offset].Text))
                {
                    text.Visible = false;
                    textBounds = new RectangleF();
                }
                else
                {
                    text.Visible = true;
                    text.Text = _Values[i + offset].Text;
                    text.TranslationID = _Values[i + offset].TranslationId;
                    text.Color = (i + offset == Selection) ? _SelTextColor : _TextColor;
                    textBounds = CBase.Fonts.GetTextBounds(text);
                    text.X = curX;
                    text.Z = Rect.Z;
                }

                CStatic img = _VisibleElements[i].Img;
                if (!DrawTextures || _Values[i + offset].Texture == null)
                {
                    if (text.Visible)
                        text.Y = Rect.Y + (Rect.H - textBounds.Height) / 2 - _TextRelativeY;
                    img.Visible = false;
                    _VisibleElements[i].Bounds = new SRectF(text.X - textBounds.Width / 2f, text.Y, textBounds.Width, textBounds.Height, Rect.Z);
                }
                else
                {
                    text.Y = (int)(Rect.Y + Rect.H - textBounds.Height - _TextRelativeY);
                    img.Texture = _Values[i + offset].Texture;
                    float alpha = (i + offset == _Selection) ? 1f : 0.35f;
                    img.Color = new SColorF(1f, 1f, 1f, alpha);
                    float size = Rect.H - textBounds.Height - 2 * _TextRelativeY;
                    if (size > elWidth)
                        size = elWidth;
                    var imgRect = new SRectF(curX - size / 2, Rect.Y + _TextRelativeY, size, size, Rect.Z);
                    img.MaxRect = imgRect;
                    _VisibleElements[i].Bounds = imgRect;
                }
            }
        }

        public void Draw()
        {
            if (!Visible && CBase.Settings.GetProgramState() != EProgramState.EditTheme)
                return;

            if (Selected)
                CBase.Drawing.DrawTexture(_SelTexture, Rect, _SelColor);
            else
                CBase.Drawing.DrawTexture(_Texture, Rect, _Color);

            if (_Selection > 0 || CBase.Settings.GetProgramState() == EProgramState.EditTheme)
            {
                if (_ArrowLeftSelected)
                    CBase.Drawing.DrawTexture(_SelTextureArrowLeft, RectArrowLeft, _SelColorArrow);
                else
                    CBase.Drawing.DrawTexture(_TextureArrowLeft, RectArrowLeft, _ColorArrow);
            }

            if (_Selection < _Values.Count - 1 || CBase.Settings.GetProgramState() == EProgramState.EditTheme)
            {
                if (_ArrowRightSelected)
                    CBase.Drawing.DrawTexture(_SelTextureArrowRight, RectArrowRight, _SelColorArrow);
                else
                    CBase.Drawing.DrawTexture(_TextureArrowRight, RectArrowRight, _ColorArrow);
            }

            if (_NeedsRevalidate)
                _Revalidate();
            foreach (CElement element in _VisibleElements)
            {
                element.Text.Draw();
                element.Img.Draw();
            }
        }

        public object GetTheme()
        {
            return _Theme;
        }

        public void UnloadSkin() {}

        public void LoadSkin()
        {
            _Theme.Color.Get(_PartyModeID, out _Color);
            _Theme.SelColor.Get(_PartyModeID, out _SelColor);
            _Theme.ArrowColor.Get(_PartyModeID, out _ColorArrow);
            _Theme.ArrowSelColor.Get(_PartyModeID, out _SelColorArrow);
            _Theme.TextColor.Get(_PartyModeID, out _TextColor);
            _Theme.TextSelColor.Get(_PartyModeID, out _SelTextColor);

            MaxRect = _Theme.Rect;
            RectArrowLeft = _Theme.RectArrowLeft;
            RectArrowRight = _Theme.RectArrowRight;

            NumVisible = _Theme.NumVisible;

            _TextH = _Theme.TextH;
            _TextRelativeX = _Theme.TextRelativeX;
            _TextRelativeY = _Theme.TextRelativeY;
            _MaxW = _Theme.TextMaxW;

            _Texture = CBase.Themes.GetSkinTexture(_Theme.Skin, _PartyModeID);
            _TextureArrowLeft = CBase.Themes.GetSkinTexture(_Theme.SkinArrowLeft, _PartyModeID);
            _TextureArrowRight = CBase.Themes.GetSkinTexture(_Theme.SkinArrowRight, _PartyModeID);

            _SelTexture = CBase.Themes.GetSkinTexture(_Theme.SkinSelected, _PartyModeID);
            _SelTextureArrowLeft = CBase.Themes.GetSkinTexture(_Theme.SkinArrowLeftSelected, _PartyModeID);
            _SelTextureArrowRight = CBase.Themes.GetSkinTexture(_Theme.SkinArrowRightSelected, _PartyModeID);
            _Invalidate();
        }

        public void ReloadSkin()
        {
            UnloadSkin();
            LoadSkin();
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            if (_ArrowLeftSelected)
            {
                RectArrowLeft.X += stepX;
                RectArrowLeft.Y += stepY;

                _Theme.RectArrowLeft.X += stepX;
                _Theme.RectArrowLeft.Y += stepY;
            }
            else if (_ArrowRightSelected)
            {
                RectArrowRight.X += stepX;
                RectArrowRight.Y += stepY;

                _Theme.RectArrowRight.X += stepX;
                _Theme.RectArrowRight.Y += stepY;
            }
            else
            {
                X += stepX;
                Y += stepY;

                _Theme.Rect.X += stepX;
                _Theme.Rect.Y += stepY;
                _Theme.RectArrowLeft.X += stepX;
                _Theme.RectArrowLeft.Y += stepY;
                _Theme.RectArrowRight.X += stepX;
                _Theme.RectArrowRight.Y += stepY;
            }
        }

        public void ResizeElement(int stepW, int stepH)
        {
            if (!_ArrowLeftSelected && !_ArrowRightSelected)
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