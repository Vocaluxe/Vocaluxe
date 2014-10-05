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
using System.Xml;
using System.Xml.Serialization;
using VocaluxeLib.Draw;
using VocaluxeLib.Xml;

namespace VocaluxeLib.Menu
{
    [XmlType("Static")]
    public struct SThemeStatic
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;
        [XmlElement("Skin")] public string TextureName;
        public SThemeColor Color;
        public SRectF Rect;
        public SReflection Reflection;
    }

    public class CStatic : IMenuElement
    {
        private readonly int _PartyModeID;

        private SThemeStatic _Theme;
        private bool _ThemeLoaded;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded
        {
            get { return _ThemeLoaded; }
        }

        private CTextureRef _Texture;
        public CTextureRef Texture
        {
            get { return _Texture ?? CBase.Themes.GetSkinTexture(_Theme.TextureName, _PartyModeID); }

            set { _Texture = value; }
        }

        public SColorF Color;
        public SRectF Rect;

        public bool Reflection;
        public float ReflectionSpace;
        public float ReflectionHeight;

        public bool Selected;
        public bool Visible = true;

        public float Alpha = 1;

        public EAspect Aspect = EAspect.Stretch;

        public CStatic() {}

        public CStatic(int partyModeID)
        {
            _PartyModeID = partyModeID;
        }

        public CStatic(CStatic s)
        {
            _PartyModeID = s._PartyModeID;

            _Texture = s.Texture;
            Color = new SColorF(s.Color);
            Rect = new SRectF(s.Rect);
            Reflection = s.Reflection;
            ReflectionSpace = s.ReflectionHeight;
            ReflectionHeight = s.ReflectionSpace;

            Selected = s.Selected;
            Alpha = s.Alpha;
            Visible = s.Visible;
        }

        public CStatic(int partyModeID, CTextureRef texture, SColorF color, SRectF rect)
        {
            _PartyModeID = partyModeID;

            _Texture = texture;
            Color = color;
            Rect = rect;
        }

        public CStatic(int partyModeID, string textureSkinName, SColorF color, SRectF rect)
        {
            _PartyModeID = partyModeID;
            _Theme.TextureName = textureSkinName;
            Color = color;
            Rect = rect;
        }

        public CStatic(SThemeStatic theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;

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

            _Theme.Rect = new SRectF(Rect);

            if (xmlReader.GetValue(item + "/Color", out _Theme.Color.Name, String.Empty))
                _ThemeLoaded &= _Theme.Color.Get(_PartyModeID, out Color);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref Color.A);
            }

            _Theme.Color.Color = new SColorF(Color);

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
                LoadTextures();
            }
            return _ThemeLoaded;
        }

        public void Draw(bool forceDraw = false)
        {
            Draw(Aspect, 1f, 0f, forceDraw);
        }

        public void Draw(EAspect aspect, float scale = 1f, float zModify = 0f, bool forceDraw = false)
        {
            CTextureRef texture = Texture;
            SRectF bounds = Rect.Scale(scale);
            bounds.Z += zModify;
            SRectF rect = texture == null ? bounds : CHelper.FitInBounds(bounds, texture.OrigAspect, aspect);
            var color = new SColorF(Color.R, Color.G, Color.B, Color.A * Alpha);
            if (Visible || forceDraw || (CBase.Settings.GetProgramState() == EProgramState.EditTheme))
            {
                if (texture != null)
                {
                    CBase.Drawing.DrawTexture(texture, rect, color, bounds);
                    if (Reflection)
                        CBase.Drawing.DrawTextureReflection(texture, rect, color, bounds, ReflectionSpace, ReflectionHeight);
                }
                else
                    CBase.Drawing.DrawRect(color, rect);
            }

            if (Selected && (CBase.Settings.GetProgramState() == EProgramState.EditTheme))
                CBase.Drawing.DrawRect(new SColorF(1f, 1f, 1f, 0.5f), rect);
        }

        public void UnloadTextures() {}

        public void LoadTextures()
        {
            _Theme.Color.Get(_PartyModeID, out Color);

            Rect = new SRectF(_Theme.Rect);
            Reflection = _Theme.Reflection.Enabled;
            if (Reflection)
            {
                ReflectionSpace = _Theme.Reflection.Space;
                ReflectionHeight = _Theme.Reflection.Height;
            }
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        public SThemeStatic GetTheme()
        {
            return _Theme;
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