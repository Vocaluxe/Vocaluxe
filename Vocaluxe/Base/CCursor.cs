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
using System.Drawing;
using Vocaluxe.Base.ThemeSystem;
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Base
{
    class CCursor
    {
        private int _MouseMoveDiffMin = CSettings.MouseMoveDiffMinActive;
        private CFading _Fading;
        private CTextureRef _Cursor;
        private Point _LastDiffPos;

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
            if (_Movetimer.ElapsedMilliseconds > CSettings.MouseMoveOffTime)
                Deactivate();

            if (_Fading != null)
            {
                bool finished;
                _Cursor.Color.A = _Fading.GetValue(out finished);
                if (finished)
                    _Fading = null;
            }

            if (Visible && (CSettings.ProgramState == EProgramState.EditTheme || ShowCursor))
                CDraw.DrawTexture(_Cursor);
        }

        public void UpdatePosition(int x, int y)
        {
            if (Math.Abs(_LastDiffPos.X - x) > _MouseMoveDiffMin ||
                Math.Abs(_LastDiffPos.Y - y) > _MouseMoveDiffMin)
            {
                //This will restart the timer and show the cursor if required
                Activate();
                //Use that point so cursor is deactivated only when there is no move by the diff value from this position the the given offTime
                //Using the curent cursor position won't work because there will only be moves by 1 in most cases
                _LastDiffPos.X = x;
                _LastDiffPos.Y = y;
            }

            _Cursor.Rect.X = x;
            _Cursor.Rect.Y = y;
        }

        public void LoadSkin()
        {
            SThemeCursor theme = CThemes.GetCursorTheme();
            _Cursor = CDraw.CopyTexture(CThemes.GetSkinTexture(theme.SkinName, -1));

            theme.Color.Get(-1, out _Cursor.Color);
            _Cursor.Rect.W = theme.W;
            _Cursor.Rect.H = theme.H;
            _Cursor.Rect.Z = CSettings.ZNear;
        }

        public void UnloadSkin()
        {
            CDraw.RemoveTexture(ref _Cursor);
        }

        public void ReloadSkin()
        {
            int x = X;
            int y = Y;
            UnloadSkin();
            LoadSkin();
            UpdatePosition(x, y);
        }

        public void Deactivate()
        {
            if (!IsActive)
                return;
            _MouseMoveDiffMin = CSettings.MouseMoveDiffMinInactive;
            _Movetimer.Reset();
            _Fade(0f, 0.5f);
        }

        public void Activate()
        {
            // Here we need to restart the timer in all cases but only set fading and diff if we are not yet active
            bool active = IsActive;
            _Movetimer.Restart();
            if (active)
                return;
            _MouseMoveDiffMin = CSettings.MouseMoveDiffMinActive;
            _Fade(1f, 0.2f);
        }

        private void _Fade(float targetAlpha, float time)
        {
            _Fading = new CFading(_Cursor.Color.A, targetAlpha, time);
        }
    }
}