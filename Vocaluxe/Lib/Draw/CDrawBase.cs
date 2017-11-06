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
using System.Threading;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;
using System.Diagnostics;
using Vocaluxe.Base.Server;

namespace Vocaluxe.Lib.Draw
{
    /// <summary>
    ///     Base class for graphic drivers
    ///     A few notes:
    ///     Writes to textures (TTextureType) should only be done in the main thread
    ///     Writes to the RefCount of existing textures should only be done in the main thread and guarded by _Textures lock
    /// </summary>
    /// <typeparam name="TTextureType"></typeparam>
    abstract class CDrawBase<TTextureType> : CTextureProvider<TTextureType> where TTextureType : CTextureBase, IDisposable
    {
        protected bool _Fullscreen;

        protected bool _Run;

        protected readonly CKeys _Keys = new CKeys();
        protected readonly CMouse _Mouse = new CMouse();

        protected int _H;
        protected int _W;
        protected int _Y;
        protected int _X;

        protected int _BorderLeft;
        protected int _BorderRight;
        protected int _BorderTop;
        protected int _BorderBottom;
        protected EGeneralAlignment _CurrentAlignment = EGeneralAlignment.Middle;

        protected abstract void _ClearScreen();
        protected abstract void _AdjustNewBorders();
        protected abstract void _LeaveFullScreen();

        protected abstract void _OnAfterDraw();

        protected abstract void _OnBeforeDraw();

        protected abstract void _DoResize();

        protected abstract void _EnterFullScreen();

        protected void _AdjustAspect(bool reverse)
        {
            if (_W / (float)_H > CSettings.GetRenderAspect())
            {
                _Y = 0;
                //The windows width is too big
                int old = _W;
                _W = (int)Math.Round(_H * CSettings.GetRenderAspect());
                int diff = old - _W;
                switch (_CurrentAlignment)
                {
                    case EGeneralAlignment.Start:
                        _X = 0;
                        break;
                    case EGeneralAlignment.Middle:
                        _X = diff / 2;
                        break;
                    case EGeneralAlignment.End:
                        _X = diff;
                        break;
                }
            }
            else
            {
                _X = 0;
                //The windows height is too big
                int old = _H;
                _H = (int)Math.Round(_W / CSettings.GetRenderAspect());
                int diff = old - _H;
                switch (_CurrentAlignment)
                {
                    case EGeneralAlignment.Start:
                        _Y = reverse ? diff : 0;
                        break;
                    case EGeneralAlignment.Middle:
                        _Y = diff / 2;
                        break;
                    case EGeneralAlignment.End:
                        _Y = reverse ? 0 : diff;
                        break;
                }
            }
        }

        /// <summary>
        ///     Struct that contains texture and world coordinates for drawing
        /// </summary>
        protected struct SDrawCoords
        {
            /// <summary>
            ///     Texture coordinates
            /// </summary>
            public float Tx1, Tx2, Ty1, Ty2;
            /// <summary>
            ///     World coordinates
            /// </summary>
            public float Wx1, Wx2, Wy1, Wy2, Wz;
            public float Rotation;
        }

        /// <summary>
        ///     Adds a texture to the vertext buffer
        /// </summary>
        /// <param name="texture">Texture to draw</param>
        /// <param name="dc">Coordinates to draw on</param>
        /// <param name="color">Color to use</param>
        /// <param name="isReflection">If true, then color is faded out in y direction</param>
        protected abstract void _DrawTexture(TTextureType texture, SDrawCoords dc, SColorF color, bool isReflection = false);

        /// <summary>
        ///     Calculates the texture and world coordinates for drawing the texture in the rect cropping at the the bounds
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="rect">Rect to draw the texture (might stretch the texture)</param>
        /// <param name="bounds">Rect to stay in (might crop the texture)</param>
        /// <param name="drawCoords">Struct for the coordinates</param>
        /// <param name="mirrored">Mirror on y-axis</param>
        /// <returns>True if anything will be dawn</returns>
        protected bool _CalcDrawCoords(TTextureType texture, SRectF rect, SRectF bounds, out SDrawCoords drawCoords, bool mirrored = false)
        {
            drawCoords = new SDrawCoords();
            if (Math.Abs(rect.W) < 1 || Math.Abs(rect.H) < 1 || Math.Abs(bounds.H) < 1 || Math.Abs(bounds.W) < 1)
                return false;

            if (bounds.X >= rect.Right || bounds.Right <= rect.X)
                return false;

            if (bounds.Y >= rect.Bottom || bounds.Bottom <= rect.Y)
                return false;

            drawCoords.Tx1 = Math.Max(0, (bounds.X - rect.X) / rect.W * texture.WidthRatio);
            drawCoords.Wx1 = Math.Max(rect.X, bounds.X);
            drawCoords.Tx2 = Math.Min(1, (bounds.Right - rect.X) / rect.W) * texture.WidthRatio;
            drawCoords.Wx2 = Math.Min(rect.Right, bounds.Right);

            drawCoords.Ty1 = Math.Max(0, (bounds.Y - rect.Y) / rect.H * texture.HeightRatio);
            drawCoords.Wy1 = Math.Max(rect.Y, bounds.Y);
            drawCoords.Ty2 = Math.Min(1, (bounds.Bottom - rect.Y) / rect.H) * texture.HeightRatio;
            drawCoords.Wy2 = Math.Min(rect.Bottom, bounds.Bottom);

            if (mirrored)
            {
                float tmp = drawCoords.Ty1;
                drawCoords.Ty1 = drawCoords.Ty2;
                drawCoords.Ty2 = tmp;
            }

            drawCoords.Wz = rect.Z + CGraphics.ZOffset;
            drawCoords.Rotation = rect.Rotation;

            return true;
        }

        /// <summary>
        ///     Calculates the texture and world coordinates for drawing (a part of) the texture in the rect
        /// </summary>
        /// <param name="texture">Texture to draw</param>
        /// <param name="rect">Rect to draw in (might stretch the texture)</param>
        /// <param name="drawCoords">Struct for the coordinates</param>
        /// <param name="mirrored">Mirror on y axis</param>
        /// <param name="begin">Position of the texture to begin drawing (0 &lt;=x&lt;=1)</param>
        /// <param name="end">Position of the texture to end drawing (0 &lt;=x&lt;=1)</param>
        /// <returns>True if anything will be dawn</returns>
        protected bool _CalcDrawCoords(TTextureType texture, SRectF rect, out SDrawCoords drawCoords, bool mirrored = false, float begin = 0, float end = 1)
        {
            drawCoords = new SDrawCoords();
            if (Math.Abs(rect.W) < 1 || Math.Abs(rect.H) < 1)
                return false;
            if (begin >= 1 || begin >= end)
                return false;

            Debug.Assert(begin.IsInRange(0, 1) && end.IsInRange(0, 1));

            drawCoords.Tx1 = begin * texture.WidthRatio;
            drawCoords.Wx1 = rect.X + begin * rect.W;
            drawCoords.Tx2 = end * texture.WidthRatio;
            drawCoords.Wx2 = rect.X + end * rect.W;

            drawCoords.Ty1 = 0;
            drawCoords.Wy1 = rect.Y;
            drawCoords.Ty2 = texture.HeightRatio;
            drawCoords.Wy2 = rect.Bottom;

            if (mirrored)
            {
                float tmp = drawCoords.Ty1;
                drawCoords.Ty1 = drawCoords.Ty2;
                drawCoords.Ty2 = tmp;
            }

            drawCoords.Wz = rect.Z + CGraphics.ZOffset;
            drawCoords.Rotation = rect.Rotation;

            return true;
        }

        public void DrawTexture(CTextureRef textureRef, SRectF rect, SColorF color, bool mirrored = false, bool allMonitors = true)
        {
            if (Math.Abs(color.A) < 0.01)
                return;
            SDrawCoords dc;
            TTextureType texture;
            if (!_GetTexture(textureRef, out texture))
                return;
            if (allMonitors)
            {
                for (int i = 0; i < CConfig.Config.Graphics.NumScreens; i++)
                {
                    SRectF newrect = rect;
                    newrect.X += CSettings.RenderW * i;
                    if (!_CalcDrawCoords(texture, newrect, out dc, mirrored))
                        return;
                    _DrawTexture(texture, dc, color);
                }
            }
            else
            {
                if (!_CalcDrawCoords(texture, rect, out dc, mirrored))
                    return;
                _DrawTexture(texture, dc, color);
            }
        }

        /// <summary>
        ///     Draws a texture
        /// </summary>
        /// <param name="textureRef">The texture to be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        /// <param name="color">A SColorF struct containing a color which the texture will be colored in</param>
        /// <param name="bounds">A SRectF struct containing which part of the texture should be drawn</param>
        /// <param name="mirrored">True if the texture should be mirrored</param>
        public void DrawTexture(CTextureRef textureRef, SRectF rect, SColorF color, SRectF bounds, bool mirrored = false, bool allMonitors = true)
        {
            if (Math.Abs(color.A) < 0.01)
                return;
            SDrawCoords dc;
            TTextureType texture;
            if (!_GetTexture(textureRef, out texture))
                return;

            if (allMonitors)
            {
                for (int i = 0; i < CConfig.Config.Graphics.NumScreens; i++)
                {
                    SRectF newrect = rect;
                    SRectF newbounds = bounds;
                    newrect.X += CSettings.RenderW * i;
                    newbounds.X += CSettings.RenderW * i;
                    if (!_CalcDrawCoords(texture, newrect, newbounds, out dc, mirrored))
                        return;
                    _DrawTexture(texture, dc, color);
                }
            }
            else
            {
                if (!_CalcDrawCoords(texture, rect, bounds, out dc, mirrored))
                    return;
                _DrawTexture(texture, dc, color);
            }
        }

        /// <summary>
        ///     Draws a texture
        /// </summary>
        /// <param name="textureRef">The texture to be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        /// <param name="color">A SColorF struct containing a color which the texture will be colored in</param>
        /// <param name="begin">A Value ranging from 0 to 1 containing the beginning of the texture</param>
        /// <param name="end">A Value ranging from 0 to 1 containing the ending of the texture</param>
        public void DrawTexture(CTextureRef textureRef, SRectF rect, SColorF color, float begin, float end, bool allMonitors = true)
        {
            if (Math.Abs(color.A) < 0.01)
                return;
            SDrawCoords dc;
            TTextureType texture;
            if (!_GetTexture(textureRef, out texture))
                return;

            if (allMonitors)
            {
                for (int i = 0; i < CConfig.Config.Graphics.NumScreens; i++)
                {
                    SRectF newrect = rect;
                    newrect.X += CSettings.RenderW * i;
                    if (!_CalcDrawCoords(texture, newrect, out dc, false, begin, end))
                        return;
                    _DrawTexture(texture, dc, color);
                }
            }
            else
            {
                if (!_CalcDrawCoords(texture, rect, out dc, false, begin, end))
                    return;
                _DrawTexture(texture, dc, color);
            }
        }

        /// <summary>
        ///     Draws a reflection of a texture
        /// </summary>
        /// <param name="textureRef">The texture of which a reflection should be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        /// <param name="color">A SColorF struct containing a color which the texture will be colored in</param>
        /// <param name="bounds">A SRectF struct containing which part of the texture should be drawn</param>
        /// <param name="space">The space between the texture and the reflection</param>
        /// <param name="height">The height of the reflection</param>
        public void DrawTextureReflection(CTextureRef textureRef, SRectF rect, SColorF color, SRectF bounds, float space, float height, bool allMonitors = true)
        {
            Debug.Assert(height >= 0);

            if (Math.Abs(color.A) < 0.01 || height < 1)
                return;
            SDrawCoords dc;
            TTextureType texture;
            if (!_GetTexture(textureRef, out texture))
                return;

            int loops;
            if (allMonitors)
            {
                loops = CConfig.Config.Graphics.NumScreens;
            }
            else
            {
                loops = 1;
            }

            for (int i = 0; i < loops; i++)
            {
                SRectF newrect = rect;
                SRectF newbounds = bounds;
                newrect.X += CSettings.RenderW * i;
                newbounds.X += CSettings.RenderW * i;
                if (!_CalcDrawCoords(texture, newrect, newbounds, out dc, true))
                    return;

                if (height > newrect.H)
                    height = newrect.H;

                dc.Wy1 += newrect.H + space; // Move from start of rect to end of rect with spacing
                dc.Wy2 += space + height; // Move from end of rect
                dc.Ty2 += (newrect.H - height) / newrect.H; // Adjust so not all of the start of the texture is drawn (mirrored--> Ty1>Ty2)
                if (dc.Ty2 < dc.Ty1) // Make sure we actually draw something
                    _DrawTexture(texture, dc, color, true);
            }
        }

        // Resharper doesn't get that this is used -.-
        // ReSharper disable UnusedMemberHiearchy.Global
        /// <summary>
        ///     Starts the rendering
        /// </summary>
        public virtual void MainLoop()
        {
            // ReSharper restore UnusedMemberHiearchy.Global
            _EnsureMainThread();
            _Run = true;

            _Fullscreen = false;
            if (CConfig.Config.Graphics.FullScreen == EOffOn.TR_CONFIG_ON)
                _EnterFullScreen();
            else
                _DoResize(); //Resize window if aspect ratio is incorrect

            while (_Run)
            {
                _CheckQueue();
                CVocaluxeServer.ProcessServerTasks();

                //We want to begin drawing
                _OnBeforeDraw();

                //Clear the previous Frame
                _ClearScreen();
                if (!CGraphics.Draw())
                    _Run = false;
                _OnAfterDraw();

                if (!CGraphics.UpdateGameLogic(_Keys, _Mouse))
                    _Run = false;

                //Apply fullscreen mode
                if ((CConfig.Config.Graphics.FullScreen == EOffOn.TR_CONFIG_ON) != _Fullscreen)
                    _ToggleFullScreen();

                //Apply border changes
                if (_BorderLeft != CConfig.Config.Graphics.BorderLeft || _BorderRight != CConfig.Config.Graphics.BorderRight || _BorderTop != CConfig.Config.Graphics.BorderTop ||
                    _BorderBottom != CConfig.Config.Graphics.BorderBottom)
                {
                    _BorderLeft = CConfig.Config.Graphics.BorderLeft;
                    _BorderRight = CConfig.Config.Graphics.BorderRight;
                    _BorderTop = CConfig.Config.Graphics.BorderTop;
                    _BorderBottom = CConfig.Config.Graphics.BorderBottom;

                    _AdjustNewBorders();
                }

                if (_CurrentAlignment != CConfig.Config.Graphics.ScreenAlignment)
                    _DoResize();

                if (CConfig.Config.Graphics.VSync == EOffOn.TR_CONFIG_OFF)
                {
                    if (CTime.IsRunning())
                    {
                        int delay = (int)Math.Floor(CConfig.CalcCycleTime() - CTime.GetMilliseconds());

                        if (delay >= 1 && delay < 500)
                            Thread.Sleep(delay);
                    }
                }
                //Calculate the FPS Rate and restart the timer after a frame
                CTime.CalculateFPS();
                CTime.Restart();
            }
        }

        private void _ToggleFullScreen()
        {
            if (!_Fullscreen)
                _EnterFullScreen();
            else
                _LeaveFullScreen();
        }
    }
}