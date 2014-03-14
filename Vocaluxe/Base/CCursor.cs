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
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Base
{
    class CCursor
    {
        private CFading _Fading;
        private CTexture _Cursor;

        private readonly Stopwatch _Movetimer = new Stopwatch();

        public bool ShowCursor = true;
        public bool Visible = true;

        public int X
        {
            get { return (int)_Cursor.Rect.X; }
        }

        public int Y
        {
            get { return (int)_Cursor.Rect.Y; }
        }

        public bool IsActive
        {
            get { return _Movetimer.IsRunning; }
        }

        public void Draw()
        {
            if (_Movetimer.IsRunning && _Movetimer.ElapsedMilliseconds > CSettings.MouseMoveOffTime)
            {
                _Movetimer.Stop();
                _Fade(0f, 0.5f);
            }


            if (_Fading != null)
            {
                bool finished;
                _Cursor.Color.A = _Fading.GetValue(out finished);
                if (finished)
                    _Fading = null;
            }

            if (Visible && (CSettings.GameState == EGameState.EditTheme || ShowCursor))
                CDraw.DrawTexture(_Cursor);
        }

        public void UpdatePosition(int x, int y)
        {
            if (Math.Abs(_Cursor.Rect.X - x) > CSettings.MouseMoveDiffMin ||
                Math.Abs(_Cursor.Rect.Y - y) > CSettings.MouseMoveDiffMin)
            {
                if (!IsActive)
                    _Fade(1f, 0.2f);

                _Movetimer.Restart();
                CSettings.MouseActive();
            }

            _Cursor.Rect.X = x;
            _Cursor.Rect.Y = y;
        }

        public void LoadTextures()
        {
            _Cursor = CDraw.AddTexture(CTheme.GetSkinFilePath(CTheme.Cursor.SkinName, -1));

            _Cursor.Color = !String.IsNullOrEmpty(CTheme.Cursor.Color)
                                ? CTheme.GetColor(CTheme.Cursor.Color, -1) : new SColorF(CTheme.Cursor.R, CTheme.Cursor.G, CTheme.Cursor.B, CTheme.Cursor.A);
            _Cursor.Rect.W = CTheme.Cursor.W;
            _Cursor.Rect.H = CTheme.Cursor.H;
            _Cursor.Rect.Z = CSettings.ZNear;
        }

        public void UnloadTextures()
        {
            CDraw.RemoveTexture(ref _Cursor);
        }

        public void ReloadTextures()
        {
            int x = X;
            int y = Y;
            UnloadTextures();
            LoadTextures();
            UpdatePosition(x, y);
        }

        public void FadeOut()
        {
            _Movetimer.Stop();
            _Fade(0f, 0.5f);
        }

        public void FadeIn()
        {
            _Movetimer.Restart();
            _Fade(1f, 0.2f);
        }

        private void _Fade(float targetAlpha, float time)
        {
            _Fading = new CFading(_Cursor.Color.A, targetAlpha, time);
        }
    }
}