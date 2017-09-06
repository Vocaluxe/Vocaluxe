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
using System.Diagnostics;
using System.Xml.Serialization;
using VocaluxeLib.Draw;
using VocaluxeLib.Xml;

namespace VocaluxeLib.Menu
{
    [XmlType("ProgressBar")]
    public struct SThemeProgressBar
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;
        public string SkinBackground;
        public string SkinForeground;
        public string SkinProgressLeft;
        public string SkinProgressMid;
        public string SkinProgressRight;
        public SThemeColor ColorBackground;
        public SThemeColor ColorForeground;
        public EDirection Direction;
        public EOffOn AnimateMovement;
        public EOffOn AnimateColoring;
        public SRectF Rect;
        public SReflection? Reflection;
    }

    public sealed class CProgressBar : CMenuElementBase, IMenuElement, IThemeable
    {
        private readonly int _PartyModeID;

        private SThemeProgressBar _Theme;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded { get; private set; }

        public bool Selectable
        {
            get { return false; }
        }

        private CTextureRef _TextureBackground;
        public CTextureRef TextureBackground
        {
            get { return _TextureBackground ?? CBase.Themes.GetSkinTexture(_Theme.SkinBackground, _PartyModeID); }

            set { _TextureBackground = value; }
        }

        private CTextureRef _TextureForeground;
        public CTextureRef TextureForeground
        {
            get { return _TextureForeground ?? CBase.Themes.GetSkinTexture(_Theme.SkinForeground, _PartyModeID); }

            set { _TextureForeground = value; }
        }

        private CTextureRef _TextureProgressLeft;
        public CTextureRef TextureProgressLeft
        {
            get { return _TextureProgressLeft ?? CBase.Themes.GetSkinTexture(_Theme.SkinProgressLeft, _PartyModeID); }

            set { _TextureProgressLeft = value; }
        }

        private CTextureRef _TextureProgressMid;
        public CTextureRef TextureProgressMid
        {
            get { return _TextureProgressMid ?? CBase.Themes.GetSkinTexture(_Theme.SkinProgressMid, _PartyModeID); }

            set { _TextureProgressMid = value; }
        }

        private CTextureRef _TextureProgressRight;
        public CTextureRef TextureProgressRight
        {
            get { return _TextureProgressRight ?? CBase.Themes.GetSkinTexture(_Theme.SkinProgressRight, _PartyModeID); }

            set { _TextureProgressRight = value; }
        }

        public SColorF ColorBackground;
        public SColorF ColorForeground;

        public bool Reflection;
        public float ReflectionSpace;
        public float ReflectionHeight;

        public float Alpha = 1;

        private float _ProgressTarget;
        public float Progress
        {
            get { return _ProgressTarget; }
            set
            {
                if (value != _ProgressTarget)
                    _UpdateProgress();

                _ProgressTarget = value;
            }
        }
        private float _ProgressCurrent;

        private SColorF _ColorProgressCurrent;
        private SColorF _ColorProgressTarget;

        private SRectF _RectProgressBegin;
        private SRectF _RectProgressMid;
        private SRectF _RectProgressEnd;

        private CTextureRef _TextureProgressBegin;
        private CTextureRef _TextureProgressEnd;

        public CProgressBar(int partyModeID)
        {
            _PartyModeID = partyModeID;
        }

        public CProgressBar(CProgressBar pb)
        {
            _PartyModeID = pb._PartyModeID;

            _TextureBackground = pb.TextureBackground;
            _TextureForeground = pb.TextureForeground;
            _TextureProgressLeft = pb.TextureProgressLeft;
            _TextureProgressMid = pb._TextureProgressMid;
            _TextureProgressRight = pb._TextureProgressRight;

            ColorBackground = pb.ColorBackground;
            ColorForeground = pb.ColorForeground;

            MaxRect = pb.MaxRect;
            Reflection = pb.Reflection;
            ReflectionSpace = pb.ReflectionHeight;
            ReflectionHeight = pb.ReflectionSpace;

            Alpha = pb.Alpha;
            Visible = pb.Visible;
        }

        public CProgressBar(SThemeProgressBar theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;
            _UpdateProgress();
            ThemeLoaded = true;
        }

        public bool LoadTheme(string xmlPath, string elementName, CXmlReader xmlReader)
        {
            //Will be removed
            return false;
        }

        public void Draw()
        {
            Draw(1f);
        }

        public void Draw(float scale = 1f, float zModify = 0f, bool forceDraw = false)
        {
            if (scale <= 0)
                return;

            if (Visible || forceDraw || (CBase.Settings.GetProgramState() == EProgramState.EditTheme))
            {
                /**
                //Draw background
                SColorF color = new SColorF(ColorBackground.R, ColorBackground.G, ColorBackground.B, ColorBackground.A * Alpha);
                if (_TextureBackground != null)
                {
                    CBase.Drawing.DrawTexture(_TextureBackground, Rect, color);
                    if (Reflection)
                        CBase.Drawing.DrawTextureReflection(_TextureBackground, Rect, color, Rect, ReflectionSpace, ReflectionHeight);
                }
                else
                    CBase.Drawing.DrawRect(ColorBackground, Rect);
                **/
                //Draw progress
                if (_TextureProgressBegin != null)
                {
                    CBase.Drawing.DrawTexture(_TextureProgressBegin, _RectProgressBegin, _ColorProgressCurrent);
                    if (Reflection)
                        CBase.Drawing.DrawTextureReflection(_TextureProgressBegin, _RectProgressBegin, _ColorProgressCurrent, _RectProgressBegin, ReflectionSpace, ReflectionHeight);
                }
                else
                    CBase.Drawing.DrawRect(_ColorProgressCurrent, _RectProgressBegin);

                if (_TextureProgressMid != null)
                {
                    CBase.Drawing.DrawTexture(_TextureProgressMid, _RectProgressMid, _ColorProgressCurrent);
                    if (Reflection)
                        CBase.Drawing.DrawTextureReflection(_TextureProgressMid, _RectProgressMid, _ColorProgressCurrent, _RectProgressMid, ReflectionSpace, ReflectionHeight);
                }
                else
                    CBase.Drawing.DrawRect(_ColorProgressCurrent, _RectProgressMid);

                if (_TextureProgressEnd != null)
                {
                    CBase.Drawing.DrawTexture(_TextureProgressEnd, _RectProgressEnd, _ColorProgressCurrent);
                    if (Reflection)
                        CBase.Drawing.DrawTextureReflection(_TextureProgressEnd, _RectProgressEnd, _ColorProgressCurrent, _RectProgressEnd, ReflectionSpace, ReflectionHeight);
                }
                else
                    CBase.Drawing.DrawRect(_ColorProgressCurrent, _RectProgressEnd);
                /*
                                //Width-related variables rounded and then floored to prevent 1px gaps in progress bar
                                SRectF rect = new SRectF(Rect.X, Rect.Y, (float)Math.Round(Rect.W), (float)Math.Round(Rect.H), Rect.Z);

                                int dh = (int)((1f - scale) * rect.H / 2);
                                int dw = (int)Math.Min(dh, rect.W / 2);

                                var progressRect = new SRectF(rect.X + dw, rect.Y + dh, rect.W - 2 * dw, rect.H - 2 * dh, rect.Z);

                                //Width of each of the ends (round parts)
                                //Need 2 of them so use minimum
                                int endsW = (int)Math.Min(progressRect.H * _TextureProgressLeft.OrigAspect, progressRect.W / 2);

                                CBase.Drawing.DrawTexture(_TextureProgressLeft, new SRectF(progressRect.X, progressRect.Y, endsW, progressRect.H, progressRect.Z), color);

                                SRectF middleRect = new SRectF(progressRect.X + endsW, progressRect.Y, progressRect.W - 2 * endsW, progressRect.H, progressRect.Z);

                                int midW = (int)Math.Round(progressRect.H * _TextureProgressMid.OrigAspect);

                                int midCount = (int)middleRect.W / midW;

                                for (int i = 0; i < midCount; ++i)
                                {
                                    CBase.Drawing.DrawTexture(_TextureProgressMid, new SRectF(middleRect.X + (i * midW), progressRect.Y, midW, progressRect.H, progressRect.Z), color);
                                }

                                SRectF lastMidRect = new SRectF(middleRect.X + midCount * midW, progressRect.Y, middleRect.W - (midCount * midW), progressRect.H, progressRect.Z);

                                CBase.Drawing.DrawTexture(_TextureProgressMid, new SRectF(middleRect.X + (midCount * midW), middleRect.Y, midW, middleRect.H, middleRect.Z), color, lastMidRect);

                                CBase.Drawing.DrawTexture(_TextureProgressRight, new SRectF(progressRect.X + progressRect.W - endsW, progressRect.Y, endsW, progressRect.H, progressRect.Z), color);
                                */
                //Draw foreground
                /**
                color = new SColorF(ColorForeground.R, ColorForeground.G, ColorForeground.B, ColorForeground.A * Alpha);
                if (_TextureForeground != null)
                {
                    CBase.Drawing.DrawTexture(_TextureForeground, Rect, color);
                    if (Reflection)
                        CBase.Drawing.DrawTextureReflection(_TextureForeground, Rect, color, Rect, ReflectionSpace, ReflectionHeight);
                }
                else
                    CBase.Drawing.DrawRect(ColorForeground, Rect);
                */
            }

            /*
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
                        }*/

            if (Selected && (CBase.Settings.GetProgramState() == EProgramState.EditTheme))
                CBase.Drawing.DrawRect(new SColorF(1f, 1f, 1f, 0.5f), Rect);
        }

        public void UnloadSkin() {}

        public void LoadSkin()
        {
            if (!ThemeLoaded)
                return;
            _Theme.ColorBackground.Get(_PartyModeID, out ColorBackground);
            _Theme.ColorForeground.Get(_PartyModeID, out ColorForeground);

            TextureBackground = CBase.Themes.GetSkinTexture(_Theme.SkinBackground, _PartyModeID);
            TextureForeground = CBase.Themes.GetSkinTexture(_Theme.SkinForeground, _PartyModeID);
            TextureProgressLeft = CBase.Themes.GetSkinTexture(_Theme.SkinProgressLeft, _PartyModeID);
            TextureProgressMid = CBase.Themes.GetSkinTexture(_Theme.SkinProgressMid, _PartyModeID);
            TextureProgressRight = CBase.Themes.GetSkinTexture(_Theme.SkinProgressRight, _PartyModeID);

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

        private void _UpdateProgress()
        {
            _ColorProgressCurrent = new SColorF(1, 1, 1, 1);
            _UpdateProgressLeft();
            _UpdateProgressMid();
            _UpdateProgressRight();
        }

        private void _UpdateProgressLeft()
        {
            switch (_Theme.Direction)
            {
                case EDirection.Right:
                    _RectProgressBegin = new SRectF(Rect.X, Rect.Y, Rect.H, Rect.H, Rect.Z);
                    _TextureProgressBegin = _TextureProgressLeft;
                    break;

                case EDirection.Up:
                    _RectProgressBegin = new SRectF(Rect.X, Rect.Y + Rect.H - Rect.W, Rect.W, Rect.W, Rect.Z);
                    _TextureProgressBegin = _TextureProgressRight;
                    break;

                case EDirection.Left:
                    _RectProgressBegin = new SRectF(Rect.X + Rect.W - Rect.H, Rect.Y, Rect.H, Rect.H, Rect.Z);
                    _TextureProgressBegin = _TextureProgressRight;
                    break;

                case EDirection.Down:
                    _RectProgressBegin = new SRectF(Rect.X, Rect.Y, Rect.W, Rect.W, Rect.Z);
                    _TextureProgressBegin = _TextureProgressLeft;
                    break;
            }
        }

        private void _UpdateProgressMid()
        {
            _ProgressCurrent = _ProgressTarget;
            switch (_Theme.Direction)
            {
                case EDirection.Right:
                    _RectProgressMid = new SRectF(Rect.X + Rect.H, Rect.Y, (Rect.W - 2 * Rect.H) * _ProgressCurrent, Rect.H, Rect.Z);
                    break;

                case EDirection.Up:
                    float newHeight = (Rect.H - 2 * Rect.W) * _ProgressCurrent;
                    _RectProgressMid = new SRectF(Rect.X, Rect.Y + (Rect.H - Rect.W) - newHeight, Rect.W, newHeight, Rect.Z);
                    break;

                case EDirection.Left:
                    float newWidth = (Rect.W - 2 * Rect.H) * _ProgressCurrent;
                    _RectProgressMid = new SRectF(Rect.X + (Rect.W - Rect.H) - newWidth, Rect.Y, newWidth, Rect.H, Rect.Z);
                    break;

                case EDirection.Down:
                    _RectProgressMid = new SRectF(Rect.X, Rect.Y, (Rect.H - 2 * Rect.W) * _ProgressCurrent, Rect.W, Rect.Z);
                    break;
            }
        }

        private void _UpdateProgressRight()
        {
            switch (_Theme.Direction)
            {
                case EDirection.Right:
                    _RectProgressEnd = new SRectF(_RectProgressMid.X + _RectProgressMid.W, Rect.Y, Rect.H, Rect.H, Rect.Z);
                    _TextureProgressEnd = _TextureProgressRight;
                    break;

                case EDirection.Up:
                    _RectProgressEnd = new SRectF(Rect.X, _RectProgressMid.Y + Rect.W, Rect.W, Rect.W, Rect.Z);
                    _TextureProgressEnd = _TextureProgressLeft;
                    break;

                case EDirection.Left:
                    _RectProgressEnd = new SRectF(_RectProgressMid.X - Rect.H, Rect.Y, Rect.H, Rect.H, Rect.Z);
                    _TextureProgressEnd = _TextureProgressLeft;
                    break;

                case EDirection.Down:
                    _RectProgressEnd = new SRectF(Rect.X, _RectProgressMid.Y + _RectProgressMid.H, Rect.W, Rect.W, Rect.Z);
                    _TextureProgressEnd = _TextureProgressRight;
                    break;
            }
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