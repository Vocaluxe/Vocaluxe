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
using System.Xml.Serialization;
using VocaluxeLib.Draw;
using VocaluxeLib.Xml;

namespace VocaluxeLib.Menu
{
    [XmlType("Button")]
    public struct SThemeButton
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;

        public string Skin;
        public string SkinSelected;
        public SRectF Rect;
        public SThemeColor Color;
        public SThemeColor SelColor;
        public SThemeText Text;
        public SThemeText SText;
        public bool STextSpecified;
        public SReflection Reflection;
        public SReflection SelReflection;
    }

    public class CButton : IMenuElement
    {
        private SThemeButton _Theme;
        private bool _ThemeLoaded;
        private readonly int _PartyModeID;

        public CTextureRef Texture;
        public CTextureRef SelTexture;
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
                if (_SelText != null)
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
            _SelText = _Theme.STextSpecified ? new CText(_Theme.SText, _PartyModeID, buttonText) : null;

            Selected = false;
            EditMode = false;

            LoadSkin();
        }

        public CButton(CButton button)
        {
            _PartyModeID = button._PartyModeID;
            _Theme = new SThemeButton
                {
                    Skin = button._Theme.Skin,
                    SkinSelected = button._Theme.SkinSelected
                };

            Rect = button.Rect;
            Color = button.Color;
            SelColor = button.Color;
            Texture = button.Texture;
            SelTexture = button.SelTexture;

            Text = new CText(button.Text);
            _SelText = _SelText == null ? null : new CText(button._SelText);
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

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/Skin", out _Theme.Skin, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinSelected", out _Theme.SkinSelected, String.Empty);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref _Theme.Rect.X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref _Theme.Rect.Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref _Theme.Rect.Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/W", ref _Theme.Rect.W);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref _Theme.Rect.H);

            if (xmlReader.GetValue(item + "/Color", out _Theme.Color.Name, String.Empty))
                _ThemeLoaded &= _Theme.Color.Get(_PartyModeID, out Color);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref Color.A);
            }

            if (xmlReader.GetValue(item + "/SColor", out _Theme.SelColor.Name, String.Empty))
                _ThemeLoaded &= _Theme.SelColor.Get(_PartyModeID, out SelColor);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SR", ref SelColor.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SG", ref SelColor.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SB", ref SelColor.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SA", ref SelColor.A);
            }

            _ThemeLoaded &= Text.LoadTheme(item, "Text", xmlReader, true);
            Text.Z = Rect.Z;
            if (xmlReader.ItemExists(item + "/SText"))
            {
                _Theme.STextSpecified = true;
                _ThemeLoaded &= _SelText.LoadTheme(item, "SText", xmlReader, true);
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
                _Theme.Color.Color = Color;
                _Theme.SelColor.Color = SelColor;
                _Theme.Text = Text.GetTheme();
                _Theme.SText = _SelText.GetTheme();

                LoadSkin();
            }
            return _ThemeLoaded;
        }

        public void Draw(bool forceDraw = false)
        {
            if (!Visible && CBase.Settings.GetProgramState() != EProgramState.EditTheme && !forceDraw)
                return;

            CTextureRef texture;

            if (!Selected && !Pressed || !_Enabled)
            {
                texture = Texture ?? CBase.Themes.GetSkinTexture(_Theme.Skin, _PartyModeID);

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
                texture = Texture ?? CBase.Themes.GetSkinTexture(_Theme.SkinSelected, _PartyModeID);

                CBase.Drawing.DrawTexture(texture, Rect, SelColor);

                if (_Reflection)
                {
                    CBase.Drawing.DrawTextureReflection(texture, Rect, SelColor, Rect, _ReflectionSpace, _ReflectionHeight);
                    Text.DrawRelative(Rect.X, Rect.Y, _ReflectionHeight, _ReflectionSpace, Rect.H);
                }
                else
                    Text.DrawRelative(Rect.X, Rect.Y);
            }
            else
            {
                texture = SelTexture ?? CBase.Themes.GetSkinTexture(_Theme.SkinSelected, _PartyModeID);

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

        public void UnloadSkin()
        {
            Text.UnloadSkin();
        }

        public void LoadSkin()
        {
            Text = new CText(_Theme.Text, _PartyModeID);
            Text.LoadSkin();

            if (_Theme.STextSpecified)
            {
                _SelText = new CText(_Theme.SText, _PartyModeID);
                _SelText.LoadSkin();
            }

            _Theme.Color.Get(_PartyModeID, out Color);
            _Theme.SelColor.Get(_PartyModeID, out SelColor);

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

        public void ReloadSkin()
        {
            UnloadSkin();
            LoadSkin();
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