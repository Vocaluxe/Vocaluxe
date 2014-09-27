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
using VocaluxeLib.Draw;

namespace VocaluxeLib.Menu
{
    [XmlType("Button")]
    public struct SThemeButton
    {
        [XmlAttributeAttribute(AttributeName = "Name")]
        public string Name;

        [XmlElement("Skin")]
        public string TextureName;
        [XmlElement("SkinSelected")]
        public string SelTextureName;
        public SRectF Rect;
        [XmlElement("Color")]
        public SThemeColor Color;
        [XmlElement("SelColor")]
        public SThemeColor SelColor;
        [XmlElement("Text")]
        public SThemeText Text;
        [XmlElement("SText")]
        public SThemeText SText;
        public bool STextSpecified;
        [XmlElement("Reflection")]
        public SReflection Reflection;
        [XmlElement("SelReflection")]
        public SReflection SelReflection;
    }

    public class CButton : IMenuElement
    {
        private SThemeButton _Theme;
        private bool _ThemeLoaded;
        private readonly int _PartyModeID;

        public CTexture Texture;
        public CTexture SelTexture;
        public SRectF Rect;
        public SColorF Color;
        public SColorF SelColor;

        public CText Text;
        private CText _SelText;

        private bool _Reflection;
        private float _ReflectionSpace;
        private float _ReflectionHeight;

        private bool _SelReflection;
        private float _SelReflectionSpace;
        private float _SelReflectionHeight;

        public bool Pressed;

        public bool EditMode
        {
            get { return Text.EditMode; }
            set
            {
                Text.EditMode = value;
                _SelText.EditMode = value;
            }
        }

        private bool _Selected;
        public bool Selected
        {
            get { return _Selected; }
            set
            {
                _Selected = value;
                Text.Selected = value;
            }
        }
        public bool Visible = true;
        private bool _Enabled = true;

        public bool Enabled
        {
            get { return _Enabled; }
            set
            {
                _Enabled = value;
                if (!_Enabled)
                    _Selected = false;
            }
        }

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded
        {
            get { return _ThemeLoaded; }
        }

        public CButton(int partyModeID)
        {
            _PartyModeID = partyModeID;
            Text = new CText(_PartyModeID);
            _SelText = new CText(_PartyModeID);
            Selected = false;
            EditMode = false;
        }

        public CButton(SThemeButton theme, int partyModeID, bool buttonText = false)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;

            Text = new CText(_Theme.Text, _PartyModeID, buttonText);
            _SelText = new CText(_Theme.SText, _PartyModeID, buttonText);

            Selected = false;
            EditMode = false;

            LoadTextures();
        }

        public CButton(CButton button)
        {
            _PartyModeID = button._PartyModeID;
            _Theme = new SThemeButton
                {
                    TextureName = button._Theme.TextureName,
                    SelTextureName = button._Theme.SelTextureName
                };

            Rect = new SRectF(button.Rect);
            Color = new SColorF(button.Color);
            SelColor = new SColorF(button.Color);
            Texture = button.Texture;
            SelTexture = button.SelTexture;

            Text = new CText(button.Text);
            _SelText = new CText(button._SelText);
            Selected = false;
            EditMode = false;
            _Enabled = button._Enabled;

            _Reflection = button._Reflection;
            _ReflectionHeight = button._ReflectionHeight;
            _ReflectionSpace = button._ReflectionSpace;

            _SelReflection = button._SelReflection;
            _SelReflectionHeight = button._SelReflectionHeight;
            _SelReflectionSpace = button._SelReflectionSpace;
        }

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader, int skinIndex)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/Skin", out _Theme.TextureName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinSelected", out _Theme.SelTextureName, String.Empty);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref Rect.X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref Rect.Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref Rect.Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/W", ref Rect.W);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref Rect.H);

            if (xmlReader.GetValue(item + "/Color", out _Theme.Color.Name, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.Color.Name, skinIndex, out Color);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref Color.A);
            }

            if (xmlReader.GetValue(item + "/SColor", out _Theme.SelColor.Name, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.SelColor.Name, skinIndex, out SelColor);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SR", ref SelColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SG", ref SelColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SB", ref SelColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SA", ref SelColor.A);
            }

            _ThemeLoaded &= Text.LoadTheme(item, "Text", xmlReader, skinIndex, true);
            Text.Z = Rect.Z;
            if (xmlReader.ItemExists(item + "/SText"))
            {
                _Theme.STextSpecified = true;
                _ThemeLoaded &= _SelText.LoadTheme(item, "SText", xmlReader, skinIndex, true);
                _SelText.Z = Rect.Z;
            }


            //Reflections
            if (xmlReader.ItemExists(item + "/Reflection"))
            {
                _Reflection = true;
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Reflection/Space", ref _ReflectionSpace);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Reflection/Height", ref _ReflectionHeight);

                _Theme.Reflection = new SReflection(true, _ReflectionHeight, _ReflectionSpace);
            }
            else
            {
                _Reflection = false;
                _Theme.Reflection = new SReflection(false, 0f, 0f);
            }

            if (xmlReader.ItemExists(item + "/SReflection"))
            {
                _SelReflection = true;
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SReflection/Space", ref _SelReflectionSpace);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SReflection/Height", ref _SelReflectionHeight);

                _Theme.SelReflection = new SReflection(true, _SelReflectionHeight, _SelReflectionSpace);
            }
            else
            {
                _SelReflection = false;
                _Theme.SelReflection = new SReflection(false, 0f, 0f);
            }

            if (_ThemeLoaded)
            {
                _Theme.Name = elementName;
                _Theme.Rect = new SRectF(Rect);
                _Theme.Color.Color = new SColorF(Color);
                _Theme.SelColor.Color = new SColorF(SelColor);
                _Theme.Text = Text.GetTheme();
                _Theme.SText = _SelText.GetTheme();

                LoadTextures();
            }
            return _ThemeLoaded;
        }

       
        public void Draw(bool forceDraw = false)
        {
            if (!Visible && CBase.Settings.GetProgramState() != EProgramState.EditTheme && !forceDraw)
                return;

            CTexture texture;

            if (!Selected && !Pressed || !_Enabled)
            {
                texture = Texture ?? CBase.Theme.GetSkinTexture(_Theme.TextureName, _PartyModeID);

                CBase.Drawing.DrawTexture(texture, Rect, Color);

                if (_Reflection)
                {
                    CBase.Drawing.DrawTextureReflection(texture, Rect, Color, Rect, _ReflectionSpace, _ReflectionHeight);
                    Text.DrawRelative(Rect.X, Rect.Y, _ReflectionHeight, _ReflectionSpace, Rect.H);
                }
                else
                    Text.DrawRelative(Rect.X, Rect.Y);
            }
            else if (!_Theme.STextSpecified)
            {
                texture = Texture ?? CBase.Theme.GetSkinTexture(_Theme.SelTextureName, _PartyModeID);

                CBase.Drawing.DrawTexture(texture, Rect, SelColor);

                if (_Reflection)
                {
                    CBase.Drawing.DrawTextureReflection(texture, Rect, SelColor, Rect, _ReflectionSpace, _ReflectionHeight);
                    Text.DrawRelative(Rect.X, Rect.Y, _ReflectionHeight, _ReflectionSpace, Rect.H);
                }
                else
                    Text.DrawRelative(Rect.X, Rect.Y);
            }
            else if (_Theme.STextSpecified)
            {
                texture = SelTexture ?? CBase.Theme.GetSkinTexture(_Theme.SelTextureName, _PartyModeID);

                CBase.Drawing.DrawTexture(texture, Rect, SelColor);

                if (_Reflection)
                {
                    CBase.Drawing.DrawTextureReflection(texture, Rect, SelColor, Rect, _ReflectionSpace, _ReflectionHeight);
                    _SelText.DrawRelative(Rect.X, Rect.Y, _ReflectionHeight, _ReflectionSpace, Rect.H);
                }
                else
                    _SelText.DrawRelative(Rect.X, Rect.Y);
            }
        }

        public void ProcessMouseMove(int x, int y)
        {
            Selected = CHelper.IsInBounds(Rect, x, y);
        }

        public void UnloadTextures()
        {
            Text.UnloadTextures();
        }

        public void LoadTextures()
        {
            Text = new CText(_Theme.Text, _PartyModeID);
            Text.LoadTextures();

            if (_Theme.STextSpecified)
            {
                _SelText = new CText(_Theme.SText, _PartyModeID);
                _SelText.LoadTextures();
            }

            if (!String.IsNullOrEmpty(_Theme.Color.Name))
                Color = CBase.Theme.GetColor(_Theme.Color.Name, _PartyModeID);
            else
                Color = _Theme.Color.Color;

            if (!String.IsNullOrEmpty(_Theme.SelColor.Name))
                SelColor = CBase.Theme.GetColor(_Theme.SelColor.Name, _PartyModeID);
            else
                SelColor = _Theme.SelColor.Color;

            Rect = _Theme.Rect;

            _Reflection = _Theme.Reflection.Enabled;
            if (_Reflection)
            {
                _ReflectionHeight = _Theme.Reflection.Height;
                _ReflectionSpace = _Theme.Reflection.Space;
            }

            _SelReflection = _Theme.Reflection.Enabled;
            if (_SelReflection)
            {
                _SelReflectionHeight = _Theme.SelReflection.Height;
                _SelReflectionSpace = _Theme.SelReflection.Space;
            }

            if (_Theme.Rect.Z < Text.Z)
                Text.Z = _Theme.Rect.Z;
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        public SThemeButton GetTheme()
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

            Rect.H += stepH;
            if (Rect.H <= 0)
                Rect.H = 1;

            _Theme.Rect.W = Rect.W;
            _Theme.Rect.H = Rect.H;
        }
        #endregion ThemeEdit
    }
}