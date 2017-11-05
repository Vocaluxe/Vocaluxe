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

using System.Diagnostics;
using System.Xml.Serialization;
using VocaluxeLib.Draw;

namespace VocaluxeLib.Menu
{
    [XmlType("Static")]
    public struct SThemeStatic
    {
        [XmlAttribute(AttributeName = "Name")]
        public string Name;
        public string Skin;
        public SThemeColor Color;
        public SRectF Rect;
        public SReflection? Reflection;
        public bool? AllMonitors;
    }

    public sealed class CStatic : CMenuElementBase, IMenuElement, IThemeable
    {
        private readonly int _PartyModeID;

        private SThemeStatic _Theme;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded { get; private set; }

        public bool Selectable
        {
            get { return false; }
        }

        private CTextureRef _Texture;
        public CTextureRef Texture
        {
            get { return _Texture ?? CBase.Themes.GetSkinTexture(_Theme.Skin, _PartyModeID); }

            set { _Texture = value; }
        }

        public SColorF Color;

        public bool Reflection;
        public float ReflectionSpace;
        public float ReflectionHeight;

        public bool AllMonitors = true;

        public float Alpha = 1;

        public EAspect Aspect = EAspect.Stretch;

        /// <summary>
        /// Adjust this value if you want to draw only a part of your texture, width
        /// </summary>
        public float ModW = -1f;
        /// <summary>
        /// Adjust this value if you want to draw only a part of your texture, height
        /// </summary>
        public float ModH = -1f;

        public CStatic(int partyModeID)
        {
            _PartyModeID = partyModeID;
        }

        public CStatic(CStatic s)
        {
            _PartyModeID = s._PartyModeID;

            _Texture = s.Texture;
            Color = s.Color;
            MaxRect = s.MaxRect;
            Reflection = s.Reflection;
            ReflectionSpace = s.ReflectionHeight;
            ReflectionHeight = s.ReflectionSpace;
            AllMonitors = s.AllMonitors;

            Alpha = s.Alpha;
            Visible = s.Visible;
        }

        public CStatic(int partyModeID, CTextureRef texture, SColorF color, SRectF rect)
        {
            _PartyModeID = partyModeID;

            _Texture = texture;
            Color = color;
            MaxRect = rect;
        }

        public CStatic(int partyModeID, string textureSkinName, SColorF color, SRectF rect)
        {
            _PartyModeID = partyModeID;
            _Theme.Skin = textureSkinName;
            Color = color;
            MaxRect = rect;
        }

        public CStatic(SThemeStatic theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;
            ThemeLoaded = true;
        }

        public void Draw()
        {
            Draw(Aspect);
        }

        public void Draw(EAspect aspect, float scale = 1f, float zModify = 0f, bool forceDraw = false)
        {
            CTextureRef texture = Texture;
            SRectF bounds = Rect.Scale(scale);

            //Change bounds if rect size should be modified without adjusting texture
            if (ModH != -1)
                bounds.H = ModH * scale;
            if (ModW != -1)
                bounds.W = ModW * scale;

            bounds.Z += zModify;

            SRectF rect = texture == null ? bounds : CHelper.FitInBounds(bounds, texture.OrigAspect, aspect);

            //Use original rect
            if (ModH != -1 || ModW != -1)
                rect = Rect.Scale(scale);

            var color = new SColorF(Color.R, Color.G, Color.B, Color.A * Alpha);
            if (Visible || forceDraw || (CBase.Settings.GetProgramState() == EProgramState.EditTheme))
            {
                if (texture != null)
                {
                    CBase.Drawing.DrawTexture(texture, rect, color, bounds, false, AllMonitors);
                    if (Reflection)
                        CBase.Drawing.DrawTextureReflection(texture, rect, color, bounds, ReflectionSpace, ReflectionHeight, AllMonitors);
                }
                else
                    CBase.Drawing.DrawRect(color, rect);
            }

            if (Selected && (CBase.Settings.GetProgramState() == EProgramState.EditTheme))
                CBase.Drawing.DrawRect(new SColorF(1f, 1f, 1f, 0.5f), rect);
        }

        public void UnloadSkin() { }

        public void LoadSkin()
        {
            if (!ThemeLoaded)
                return;
            _Theme.Color.Get(_PartyModeID, out Color);

            MaxRect = _Theme.Rect;
            Reflection = _Theme.Reflection.HasValue;
            if (Reflection)
            {
                Debug.Assert(_Theme.Reflection != null);
                ReflectionSpace = _Theme.Reflection.Value.Space;
                ReflectionHeight = _Theme.Reflection.Value.Height;
            }
        }

        public void ReloadSkin()
        {
            UnloadSkin();
            LoadSkin();
        }

        public object GetTheme()
        {
            return _Theme;
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            X += stepX;
            Y += stepY;

            _Theme.Rect.X += stepX;
            _Theme.Rect.Y += stepY;
        }

        public void ResizeElement(int stepW, int stepH)
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
        #endregion ThemeEdit
    }
}