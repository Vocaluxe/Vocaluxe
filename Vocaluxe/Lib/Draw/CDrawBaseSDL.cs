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
using System.IO;
using SDL;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Draw
{
    abstract unsafe class CDrawBaseSDL<TTextureType> : CDrawBase<TTextureType> where TTextureType : CTextureBase, IDisposable
    {
        protected SDL_Window* _Window;

        protected bool _CreateWindow(SDL_WindowFlags flags)
        {
            _Window = SDL3.SDL_CreateWindow(CSettings.GetFullVersionText(), CConfig.Config.Graphics.ScreenW * CConfig.Config.Graphics.NumScreens, CConfig.Config.Graphics.ScreenH,
                flags | SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL_WindowFlags.SDL_WINDOW_HIDDEN | SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY);
            if (_Window == null)
            {
                CLog.Error("Failed to create the window: " + SDL3.SDL_GetError());
                return false;
            }

            CSdl.MainWindow = _Window;
            return true;
        }

        protected Size _GetClientSize()
        {
            int w, h;
            if (_Window == null || !SDL3.SDL_GetWindowSizeInPixels(_Window, &w, &h))
            {
                return Size.Empty;
            }

            return new Size(w, h);
        }

        public override void Close()
        {
            base.Close();
            if (_Window != null)
            {
                if (CSdl.MainWindow == _Window)
                {
                    CSdl.MainWindow = null;
                }

                SDL3.SDL_DestroyWindow(_Window);
                _Window = null;
            }
        }

        protected override void _EnterFullScreen()
        {
            Debug.Assert(!_Fullscreen);
            _Fullscreen = true;

            SDL3.SDL_SetWindowFullscreen(_Window, true);
            SDL3.SDL_SyncWindow(_Window);
            _DoResize();
        }

        protected override void _LeaveFullScreen()
        {
            Debug.Assert(_Fullscreen);
            _Fullscreen = false;

            SDL3.SDL_SetWindowFullscreen(_Window, false);
            SDL3.SDL_SyncWindow(_Window);
            _DoResize();
        }

        #region event handlers
        protected void _HandleEvents()
        {
            SDL_Event e;
            while (SDL3.SDL_PollEvent(&e))
            {
                switch (e.Type)
                {
                    case SDL_EventType.SDL_EVENT_QUIT:
                    case SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED:
                        _Run = false;
                        break;
                    case SDL_EventType.SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED:
                        _DoResize();
                        break;
                    case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER:
                        SDL3.SDL_HideCursor();
                        _Mouse.Visible = true;
                        break;
                    case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
                        _Mouse.Visible = false;
                        SDL3.SDL_ShowCursor();
                        break;
                    case SDL_EventType.SDL_EVENT_KEY_DOWN:
                        _OnKeyDown(e.key);
                        break;
                    case SDL_EventType.SDL_EVENT_KEY_UP:
                        _Keys.KeyUp(CSdl.GetModifiers(e.key.mod));
                        break;
                    case SDL_EventType.SDL_EVENT_TEXT_INPUT:
                        var text = e.text.GetText();
                        if (!string.IsNullOrEmpty(text))
                        {
                            var modifiers = CSdl.GetModifiers();
                            foreach (var chr in text)
                            {
                                _Keys.KeyPress(chr, modifiers);
                            }
                        }

                        break;
                    case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                        _Mouse.MouseMove(_ToPixelX(e.motion.x), _ToPixelY(e.motion.y), _GetHeldButton(e.motion.state), CSdl.GetModifiers());
                        break;
                    case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                        _Mouse.MouseDown(_ToPixelX(e.button.x), _ToPixelY(e.button.y), _GetButton(e.button.Button), CSdl.GetModifiers());
                        break;
                    case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                        var delta = e.wheel.direction == SDL_MouseWheelDirection.SDL_MOUSEWHEEL_FLIPPED ? -e.wheel.y : e.wheel.y;
                        _Mouse.MouseWheel(_ToPixelX(e.wheel.mouse_x), _ToPixelY(e.wheel.mouse_y), (int)Math.Round(delta * 120), CSdl.GetModifiers());
                        break;
                }
            }
        }

        private void _OnKeyDown(SDL_KeyboardEvent key)
        {
            var keyCode = CSdl.GetKey(key.key);
            var modifiers = CSdl.GetModifiers(key.mod);
            if (keyCode == Keys.None)
            {
                return;
            }

            _Keys.KeyDown(keyCode, modifiers);

            switch (keyCode)
            {
                case Keys.Enter:
                    _Keys.KeyPress('\r', modifiers);
                    break;
                case Keys.Back:
                    _Keys.KeyPress('\b', modifiers);
                    break;
                case Keys.Escape:
                    _Keys.KeyPress((char)27, modifiers);
                    break;
                case Keys.Tab:
                    _Keys.KeyPress('\t', modifiers);
                    break;
            }
        }

        private int _ToPixelX(float x)
        {
            int w, h;
            SDL3.SDL_GetWindowSize(_Window, &w, &h);
            return w > 0 ? (int)(x * _GetClientSize().Width / w) : (int)x;
        }

        private int _ToPixelY(float y)
        {
            int w, h;
            SDL3.SDL_GetWindowSize(_Window, &w, &h);
            return h > 0 ? (int)(y * _GetClientSize().Height / h) : (int)y;
        }

        private static EMouseButton _GetButton(SDLButton button)
        {
            switch (button)
            {
                case SDLButton.SDL_BUTTON_LEFT:
                    return EMouseButton.Left;
                case SDLButton.SDL_BUTTON_RIGHT:
                    return EMouseButton.Right;
                case SDLButton.SDL_BUTTON_MIDDLE:
                    return EMouseButton.Middle;
            }

            return EMouseButton.None;
        }

        private static EMouseButton _GetHeldButton(SDL_MouseButtonFlags state)
        {
            if ((state & SDL_MouseButtonFlags.SDL_BUTTON_LMASK) != 0)
            {
                return EMouseButton.Left;
            }

            if ((state & SDL_MouseButtonFlags.SDL_BUTTON_RMASK) != 0)
            {
                return EMouseButton.Right;
            }

            if ((state & SDL_MouseButtonFlags.SDL_BUTTON_MMASK) != 0)
            {
                return EMouseButton.Middle;
            }

            return EMouseButton.None;
        }
        #endregion

        public override bool Init()
        {
            if (!base.Init())
            {
                return false;
            }

            if (_Window == null)
            {
                return false;
            }

            CSdl.SetWindowIcon(_Window, Path.Combine(CSettings.ProgramFolder, CSettings.FileNameIcon));
            SDL3.SDL_SetWindowPosition(_Window, (int)SDL3.SDL_WINDOWPOS_CENTERED, (int)SDL3.SDL_WINDOWPOS_CENTERED);
            SDL3.SDL_StartTextInput(_Window);

            return true;
        }

        public override void MainLoop()
        {
            SDL3.SDL_ShowWindow(_Window);
            SDL3.SDL_RaiseWindow(_Window);
            _ClearScreen();
            base.MainLoop();
            SDL3.SDL_HideWindow(_Window);
        }
    }
}
