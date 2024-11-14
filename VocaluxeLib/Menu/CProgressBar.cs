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
using System.Diagnostics;
using System.Xml.Serialization;
using VocaluxeLib.Draw;
using VocaluxeLib.Xml;

namespace VocaluxeLib.Menu
{
    [XmlType("ProgressColor")]
    public struct SThemeProgressBarColor
    {
        public float From;
        public SThemeColor Color;
    }

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
        [XmlArrayItem("ProgressColor"), XmlArray] public List<SThemeProgressBarColor> ProgressColors;
        public SReflection? Reflection;
        public bool? AllMonitors;
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

        private EDirection _Direction;
        private EOffOn _AnimateMovement;
        private EOffOn _AnimateColoring;
        private List<SThemeProgressBarColor> _ProgressColors;

        public SColorF ColorBackground;
        public SColorF ColorForeground;

        public bool Reflection;
        public float ReflectionSpace;
        public float ReflectionHeight;

        public bool AllMonitors = true;

        public float Alpha = 1;

        private float _ProgressTarget;
        public float Progress
        {
            get { return _ProgressTarget; }
            set
            {
                _ProgressLast = 0;
                _AnimTimer.Reset();
                //Animation is still running, so use current state for calculations
                if(_ProgressCurrent != _ProgressTarget)
                {
                    _ProgressLast = _ProgressCurrent;
                    _ColorProgressLast = _ColorProgressCurrent;
                }
                _ProgressTarget = value;
            }
        }
        private float _ProgressCurrent;
        private float _ProgressLast;

        private SColorF _ColorProgressCurrent;
        private SColorF _ColorProgressTarget;
        private SColorF _ColorProgressLast;

        private SRectF _RectProgressBegin;
        private SRectF _RectProgressMid;
        private SRectF _RectProgressEnd;

        private CTextureRef _TextureProgressBegin;
        private CTextureRef _TextureProgressEnd;

        private Stopwatch _AnimTimer;
        private float _AnimDuration;
        private bool _Animate;

        public CProgressBar(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _AnimTimer = new Stopwatch();
        }

        public CProgressBar(CProgressBar pb)
        {
            _PartyModeID = pb._PartyModeID;

            _TextureBackground = pb.TextureBackground;
            _TextureForeground = pb.TextureForeground;
            _TextureProgressLeft = pb.TextureProgressLeft;
            _TextureProgressMid = pb._TextureProgressMid;
            _TextureProgressRight = pb._TextureProgressRight;
            _AnimateMovement = pb._AnimateMovement;
            _AnimateColoring = pb._AnimateColoring;
            _Direction = pb._Direction;
            _ProgressColors = pb._ProgressColors;

            ColorBackground = pb.ColorBackground;
            ColorForeground = pb.ColorForeground;

            MaxRect = pb.MaxRect;
            Reflection = pb.Reflection;
            ReflectionSpace = pb.ReflectionHeight;
            ReflectionHeight = pb.ReflectionSpace;

            AllMonitors = pb.AllMonitors;

            Alpha = pb.Alpha;
            Visible = pb.Visible;

            _AnimTimer = new Stopwatch();
        }

        public CProgressBar(SThemeProgressBar theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;
            _Theme.ProgressColors.Sort((x, y) => x.From.CompareTo(y.From));
            _ProgressColors = _Theme.ProgressColors;
            _Direction = _Theme.Direction;
            _AnimateMovement = _Theme.AnimateMovement;
            _AnimateColoring = _Theme.AnimateColoring;
            _UpdateProgress();
            ThemeLoaded = true;

            _AnimTimer = new Stopwatch();
        }

        public void Reset(bool animateInit = false)
        {
            _Animate = animateInit;

            _ProgressColors[0].Color.Get(_PartyModeID, out _ColorProgressLast);
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
                _UpdateProgress();

                //Draw background
                SColorF color = new SColorF(ColorBackground.R, ColorBackground.G, ColorBackground.B, ColorBackground.A * Alpha);
                if (_TextureBackground != null)
                {
                    CBase.Drawing.DrawTexture(_TextureBackground, Rect, color, AllMonitors);
                    if (Reflection)
                        CBase.Drawing.DrawTextureReflection(_TextureBackground, Rect, color, Rect, ReflectionSpace, ReflectionHeight, AllMonitors);
                }

                //Draw progress
                color = new SColorF(_ColorProgressCurrent.R, _ColorProgressCurrent.G, _ColorProgressCurrent.B, _ColorProgressCurrent.A * Alpha);
                if (_TextureProgressBegin != null)
                {
                    CBase.Drawing.DrawTexture(_TextureProgressBegin, _RectProgressBegin, color, AllMonitors);
                    if (Reflection)
                        CBase.Drawing.DrawTextureReflection(_TextureProgressBegin, _RectProgressBegin, color, _RectProgressBegin, ReflectionSpace, ReflectionHeight, AllMonitors);
                }

                if (_TextureProgressMid != null)
                {
                    CBase.Drawing.DrawTexture(_TextureProgressMid, _RectProgressMid, color, AllMonitors);
                    if (Reflection)
                        CBase.Drawing.DrawTextureReflection(_TextureProgressMid, _RectProgressMid, color, _RectProgressMid, ReflectionSpace, ReflectionHeight, AllMonitors);
                }
                else
                    CBase.Drawing.DrawRect(_ColorProgressCurrent, _RectProgressMid);

                if (_TextureProgressEnd != null)
                {
                    CBase.Drawing.DrawTexture(_TextureProgressEnd, _RectProgressEnd, color, AllMonitors);
                    if (Reflection)
                        CBase.Drawing.DrawTextureReflection(_TextureProgressEnd, _RectProgressEnd, color, _RectProgressEnd, ReflectionSpace, ReflectionHeight, AllMonitors);
                }

                //Draw foreground
                color = new SColorF(ColorForeground.R, ColorForeground.G, ColorForeground.B, ColorForeground.A * Alpha);
                if (_TextureForeground != null)
                {
                    CBase.Drawing.DrawTexture(_TextureForeground, Rect, color, AllMonitors);
                    if (Reflection)
                        CBase.Drawing.DrawTextureReflection(_TextureForeground, Rect, color, Rect, ReflectionSpace, ReflectionHeight, AllMonitors);
                }
            }

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
            if (_ProgressCurrent == _ProgressTarget)
                return;

            if (_Animate)
                _CheckAnimation();
            else
                _ProgressCurrent = _ProgressLast = _ProgressTarget;

            _UpdateProgressColor();

            _UpdateProgressLeft();
            _UpdateProgressMid();
            _UpdateProgressRight();
        }

        private void _CheckAnimation()
        {
            //Check if animation needs to be started
            if (_AnimateColoring == EOffOn.TR_CONFIG_ON || _AnimateMovement == EOffOn.TR_CONFIG_ON)
            {
                if (!_AnimTimer.IsRunning)
                {
                    _AnimTimer.Restart();
                    //Calc animation duration in ms based on rect size and progress change
                    if (_Direction == EDirection.Left || _Direction == EDirection.Right)
                        _AnimDuration = Math.Max(100f, (Rect.W * 0.015f * 1000) * Math.Abs(_ProgressTarget - _ProgressCurrent));
                    else
                        _AnimDuration = Math.Max(100f, (Rect.H * 0.015f * 1000) * Math.Abs(_ProgressTarget - _ProgressCurrent));
                } 
            }
            //Movement animation
            if (_AnimateMovement == EOffOn.TR_CONFIG_ON)
                _ProgressCurrent = _ProgressLast + (_AnimTimer.ElapsedMilliseconds / _AnimDuration).Clamp(0, 1) * (_ProgressTarget - _ProgressLast);

            //Check if animation needs to be stopped
            if (_ProgressCurrent == _ProgressTarget)
            {
                _AnimTimer.Stop();

                _ProgressLast = _ProgressCurrent = _ProgressTarget;
                _ColorProgressLast = _ColorProgressCurrent = _ColorProgressTarget;
            }

            _Animate = true;
        }

        private void _UpdateProgressColor()
        {
            foreach (SThemeProgressBarColor col in _ProgressColors)
                if (col.From < _ProgressTarget)
                    col.Color.Get(_PartyModeID, out _ColorProgressTarget);

            if(_Animate && _AnimateColoring == EOffOn.TR_CONFIG_ON)
            {
                float animFactor = (_AnimTimer.ElapsedMilliseconds / _AnimDuration).Clamp(0, 1);

                _ColorProgressCurrent.R = _ColorProgressLast.R + animFactor * (_ColorProgressTarget.R - _ColorProgressLast.R);
                _ColorProgressCurrent.G = _ColorProgressLast.G + animFactor * (_ColorProgressTarget.G - _ColorProgressLast.G);
                _ColorProgressCurrent.B = _ColorProgressLast.B + animFactor * (_ColorProgressTarget.B - _ColorProgressLast.B);
                _ColorProgressCurrent.A = _ColorProgressLast.A + animFactor * (_ColorProgressTarget.A - _ColorProgressLast.A);
            }
            else
                _ColorProgressCurrent = _ColorProgressLast = _ColorProgressTarget;
        }

        private void _UpdateProgressLeft()
        {
            switch (_Direction)
            {
                case EDirection.Right:
                    _TextureProgressBegin = TextureProgressLeft;
                    if (_TextureProgressBegin == null)
                        _RectProgressBegin = new SRectF(Rect.X, Rect.Y, 0, 0, Rect.Z);
                    else
                        _RectProgressBegin = new SRectF(Rect.X, Rect.Y, Rect.H * _TextureProgressBegin.OrigAspect, Rect.H, Rect.Z);
                    break;

                case EDirection.Up:
                    _TextureProgressBegin = TextureProgressRight;
                    if (_TextureProgressBegin == null)
                        _RectProgressBegin = new SRectF(Rect.X, Rect.Y + Rect.H, 0, 0, Rect.Z);
                    else
                        _RectProgressBegin = new SRectF(Rect.X, Rect.Y + Rect.H - Rect.W * _TextureProgressBegin.OrigAspect, Rect.W, Rect.W * _TextureProgressBegin.OrigAspect, Rect.Z);
                    break;

                case EDirection.Left:
                    _TextureProgressBegin = TextureProgressRight;
                    if (_TextureProgressBegin == null)
                        _RectProgressBegin = new SRectF(Rect.X + Rect.W, Rect.Y, 0, 0, Rect.Z);
                    else
                        _RectProgressBegin = new SRectF(Rect.X + Rect.W - Rect.H * _TextureProgressBegin.OrigAspect, Rect.Y, Rect.H *_TextureProgressBegin.OrigAspect, Rect.H, Rect.Z);
                    break;

                case EDirection.Down:
                    _TextureProgressBegin = TextureProgressLeft;
                    if (_TextureProgressBegin == null)
                        _RectProgressBegin = new SRectF(Rect.X, Rect.Y, 0, 0, Rect.Z);
                    else
                        _RectProgressBegin = new SRectF(Rect.X, Rect.Y, Rect.W, Rect.W * _TextureProgressBegin.OrigAspect, Rect.Z);
                    break;
            }
        }

        private void _UpdateProgressMid()
        {
            switch (_Direction)
            {
                case EDirection.Right:
                    _RectProgressMid = new SRectF(_RectProgressBegin.X + _RectProgressBegin.W, Rect.Y, (Rect.W - 2 * _RectProgressBegin.W) * _ProgressCurrent, Rect.H, Rect.Z);
                    break;

                case EDirection.Up:
                    float newHeight = (Rect.H - 2 * _RectProgressBegin.H) * _ProgressCurrent;
                    _RectProgressMid = new SRectF(Rect.X, _RectProgressBegin.Y - newHeight, Rect.W, newHeight, Rect.Z);
                    break;

                case EDirection.Left:
                    float newWidth = (Rect.W - 2 * _RectProgressBegin.H) * _ProgressCurrent;
                    _RectProgressMid = new SRectF(_RectProgressBegin.X - newWidth, Rect.Y, newWidth, Rect.H, Rect.Z);
                    break;

                case EDirection.Down:
                    _RectProgressMid = new SRectF(Rect.X, _RectProgressBegin.Y + _RectProgressBegin.H, (Rect.H - 2 * _RectProgressBegin.W) * _ProgressCurrent, Rect.W, Rect.Z);
                    break;
            }
        }

        private void _UpdateProgressRight()
        {
            switch (_Direction)
            {
                case EDirection.Right:
                    _TextureProgressEnd = _TextureProgressRight;
                    if (_TextureProgressEnd == null)
                        _RectProgressEnd = new SRectF(_RectProgressMid.X + _RectProgressMid.W, Rect.Y, 0, 0, Rect.Z);
                    else
                        _RectProgressEnd = new SRectF(_RectProgressMid.X + _RectProgressMid.W, Rect.Y, Rect.H * _TextureProgressBegin.OrigAspect, Rect.H, Rect.Z);
                    break;

                case EDirection.Up:
                    _TextureProgressEnd = _TextureProgressLeft;
                    if (_TextureProgressEnd == null)
                        _RectProgressEnd = new SRectF(Rect.X, _RectProgressMid.Y + Rect.W, 0, 0, Rect.Z);
                    else
                        _RectProgressEnd = new SRectF(Rect.X, _RectProgressMid.Y + Rect.W, Rect.W, Rect.W * _TextureProgressBegin.OrigAspect, Rect.Z);
                    break;

                case EDirection.Left:
                    _TextureProgressEnd = _TextureProgressLeft;
                    if (_TextureProgressEnd == null)
                        _RectProgressEnd = new SRectF(_RectProgressMid.X - Rect.H, Rect.Y, 0, 0, Rect.Z);
                    else
                        _RectProgressEnd = new SRectF(_RectProgressMid.X - Rect.H, Rect.Y, Rect.H * _TextureProgressBegin.OrigAspect, Rect.H, Rect.Z);
                    break;

                case EDirection.Down:
                    _TextureProgressEnd = _TextureProgressRight;
                    if (_TextureProgressEnd == null)
                        _RectProgressEnd = new SRectF(Rect.X, _RectProgressMid.Y + _RectProgressMid.H, 0, 0, Rect.Z);
                    else
                        _RectProgressEnd = new SRectF(Rect.X, _RectProgressMid.Y + _RectProgressMid.H, Rect.W, Rect.W * _TextureProgressBegin.OrigAspect, Rect.Z);
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
