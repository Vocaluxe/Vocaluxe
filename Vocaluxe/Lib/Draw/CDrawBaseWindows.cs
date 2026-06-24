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
using System.ComponentModel;
using System.Diagnostics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using GlfwKeys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vocaluxe.Base;
using VKeys = VocaluxeLib.Keys;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Draw
{
    /// <summary>
    ///     Cross-platform window host built on an OpenTK 4 <see cref="NativeWindow" /> (GLFW backend).
    ///     Replaces the former WinForms host so the OpenGL renderer runs natively on Windows, Linux and macOS.
    ///     Concrete drivers (e.g. COpenGL) create <see cref="_Window" /> with the desired GL context settings;
    ///     this base wires window/input events into the game's <c>_Keys</c>/<c>_Mouse</c> queues and owns fullscreen.
    /// </summary>
    abstract class CDrawBaseGlfw<TTextureType> : CDrawBase<TTextureType> where TTextureType : CTextureBase, IDisposable
    {
        protected NativeWindow _Window;
        private Vector2i _RestoreLocation;
        private Vector2i _RestoreSize;
        private WindowBorder _RestoreBorder;

        public override void Close()
        {
            base.Close();
            try
            {
                _Window?.Close();
            }
            catch {}
        }

        protected override void _EnterFullScreen()
        {
            Debug.Assert(!_Fullscreen);
            _Fullscreen = true;

            _RestoreLocation = _Window.Location;
            _RestoreSize = _Window.ClientSize;
            _RestoreBorder = _Window.WindowBorder;

#if WIN
            // Windows: any window that EXACTLY covers a monitor (exclusive GLFW WindowState.Fullscreen
            // OR a borderless window sized to the monitor) gets promoted by Windows to fullscreen-
            // exclusive present, whose path freezes our manually-driven render loop — SwapBuffers keeps
            // succeeding but the visible front buffer never updates (app looks stuck on the first
            // frame). Work around it with a borderless window sized 1px taller than the monitor: it
            // still looks fullscreen (the extra row is off-screen) but stays on the composited (DWM)
            // present path, which works. Linux/macOS keep native fullscreen.
            try
            {
                MonitorInfo monitor = Monitors.GetMonitorFromWindow(_Window);
                Vector2i size = monitor.ClientArea.Size;
                _Window.WindowBorder = WindowBorder.Hidden;
                _Window.WindowState = WindowState.Normal;
                _Window.Location = monitor.ClientArea.Min;
                _Window.ClientSize = new Vector2i(size.X, size.Y + 1);
                CLog.Information("Windows borderless fullscreen: " + size.X + "x" + size.Y + " (+1px to stay composited)");
            }
            catch (Exception e)
            {
                // Never let fullscreen setup kill startup — fall back to a normal maximized window.
                CLog.Error("Borderless fullscreen setup failed, falling back to windowed: " + e);
                _Window.WindowBorder = _RestoreBorder;
                _Window.WindowState = WindowState.Maximized;
                _Fullscreen = false;
            }
#else
            _Window.WindowState = WindowState.Fullscreen;
#endif
            _DoResize();
        }

        protected override void _LeaveFullScreen()
        {
            Debug.Assert(_Fullscreen);
            _Fullscreen = false;

            _Window.WindowState = WindowState.Normal;
            _Window.WindowBorder = _RestoreBorder;
            _Window.ClientSize = _RestoreSize;
            _Window.Location = _RestoreLocation;
        }

        #region window/input event handlers
        private void _OnClosing(CancelEventArgs e)
        {
            _Run = false;
        }

        protected virtual void _OnResize(ResizeEventArgs e)
        {
            _DoResize();
        }

        private void _OnMouseMove(MouseMoveEventArgs e)
        {
            _Mouse.MouseMove((int)e.X, (int)e.Y,
                             _Window.IsMouseButtonDown(MouseButton.Left),
                             _Window.IsMouseButtonDown(MouseButton.Right),
                             _Window.IsMouseButtonDown(MouseButton.Middle),
                             _Shift, _Alt, _Ctrl);
        }

        private void _OnMouseWheel(MouseWheelEventArgs e)
        {
            Vector2 p = _Window.MousePosition;
            // OpenTK reports the wheel in notches; CMouse expects WinForms-style 120-per-notch deltas.
            _Mouse.MouseWheel((int)p.X, (int)p.Y, (int)(e.OffsetY * 120), _Shift, _Alt, _Ctrl);
        }

        private void _OnMouseDown(MouseButtonEventArgs e)
        {
            Vector2 p = _Window.MousePosition;
            _Mouse.MouseDown((int)p.X, (int)p.Y,
                             e.Button == MouseButton.Left,
                             e.Button == MouseButton.Right,
                             e.Button == MouseButton.Middle,
                             _Shift, _Alt, _Ctrl);
        }

        private void _OnMouseUp(MouseButtonEventArgs e)
        {
            Vector2 p = _Window.MousePosition;
            _Mouse.MouseUp((int)p.X, (int)p.Y,
                           e.Button == MouseButton.Left,
                           e.Button == MouseButton.Right,
                           e.Button == MouseButton.Middle,
                           _Shift, _Alt, _Ctrl);
        }

        private void _OnKeyDown(KeyboardKeyEventArgs e)
        {
            _Keys.KeyDown(_MapKey(e.Key), e.Shift, e.Alt, e.Control);
        }

        private void _OnKeyUp(KeyboardKeyEventArgs e)
        {
            _Keys.KeyUp(_MapKey(e.Key), e.Shift, e.Alt, e.Control);
        }

        private void _OnTextInput(TextInputEventArgs e)
        {
            _Keys.KeyPress((char)e.Unicode, _Shift, _Alt, _Ctrl);
        }

        private bool _Shift
        {
            get { return _Window.IsKeyDown(GlfwKeys.LeftShift) || _Window.IsKeyDown(GlfwKeys.RightShift); }
        }

        private bool _Alt
        {
            get { return _Window.IsKeyDown(GlfwKeys.LeftAlt) || _Window.IsKeyDown(GlfwKeys.RightAlt); }
        }

        private bool _Ctrl
        {
            get { return _Window.IsKeyDown(GlfwKeys.LeftControl) || _Window.IsKeyDown(GlfwKeys.RightControl); }
        }
        #endregion

        /// <summary>
        ///     Maps a GLFW key code to the platform-independent VocaluxeLib.Keys enum.
        ///     Letters and top-row digits share ASCII values with our enum, so they are cast directly.
        /// </summary>
        private static VKeys _MapKey(GlfwKeys key)
        {
            if (key >= GlfwKeys.A && key <= GlfwKeys.Z)
                return (VKeys)(int)key; // A..Z == 65..90 in both enums
            if (key >= GlfwKeys.D0 && key <= GlfwKeys.D9)
                return (VKeys)(int)key; // D0..D9 == 48..57 in both enums

            switch (key)
            {
                case GlfwKeys.Left: return VKeys.Left;
                case GlfwKeys.Right: return VKeys.Right;
                case GlfwKeys.Up: return VKeys.Up;
                case GlfwKeys.Down: return VKeys.Down;
                case GlfwKeys.Enter:
                case GlfwKeys.KeyPadEnter: return VKeys.Enter;
                case GlfwKeys.Escape: return VKeys.Escape;
                case GlfwKeys.Backspace: return VKeys.Back;
                case GlfwKeys.Tab: return VKeys.Tab;
                case GlfwKeys.Space: return VKeys.Space;
                case GlfwKeys.Delete: return VKeys.Delete;
                case GlfwKeys.Home: return VKeys.Home;
                case GlfwKeys.End: return VKeys.End;
                case GlfwKeys.PageUp: return VKeys.PageUp;
                case GlfwKeys.PageDown: return VKeys.PageDown;
                case GlfwKeys.KeyPad0: return VKeys.NumPad0;
                case GlfwKeys.KeyPad1: return VKeys.NumPad1;
                case GlfwKeys.KeyPad2: return VKeys.NumPad2;
                case GlfwKeys.KeyPad3: return VKeys.NumPad3;
                case GlfwKeys.KeyPad4: return VKeys.NumPad4;
                case GlfwKeys.KeyPad5: return VKeys.NumPad5;
                case GlfwKeys.KeyPad6: return VKeys.NumPad6;
                case GlfwKeys.KeyPad7: return VKeys.NumPad7;
                case GlfwKeys.KeyPad8: return VKeys.NumPad8;
                case GlfwKeys.KeyPad9: return VKeys.NumPad9;
                case GlfwKeys.KeyPadAdd: return VKeys.Add;
                case GlfwKeys.KeyPadSubtract: return VKeys.Subtract;
                case GlfwKeys.F1: return VKeys.F1;
                case GlfwKeys.F2: return VKeys.F2;
                case GlfwKeys.F3: return VKeys.F3;
                case GlfwKeys.F4: return VKeys.F4;
                case GlfwKeys.F5: return VKeys.F5;
                case GlfwKeys.F6: return VKeys.F6;
                case GlfwKeys.F7: return VKeys.F7;
                case GlfwKeys.F8: return VKeys.F8;
                case GlfwKeys.F9: return VKeys.F9;
                case GlfwKeys.F10: return VKeys.F10;
                case GlfwKeys.F11: return VKeys.F11;
                case GlfwKeys.F12: return VKeys.F12;
                default: return VKeys.None;
            }
        }

        public override bool Init()
        {
            if (!base.Init())
                return false;

            _Window.Title = CSettings.GetFullVersionText();

            _Window.Closing += _OnClosing;
            _Window.Resize += _OnResize;
            _Window.MouseMove += _OnMouseMove;
            _Window.MouseWheel += _OnMouseWheel;
            _Window.MouseDown += _OnMouseDown;
            _Window.MouseUp += _OnMouseUp;
            _Window.KeyDown += _OnKeyDown;
            _Window.KeyUp += _OnKeyUp;
            _Window.TextInput += _OnTextInput;

            _Window.CenterWindow();

            return true;
        }

        protected override void _ShowWindow()
        {
            _Window.IsVisible = true;
        }

        public override void MainLoop()
        {
            _Window.IsVisible = true;
            base.MainLoop();
            _Window.IsVisible = false;
        }
    }
}
