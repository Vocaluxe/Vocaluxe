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
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Vocaluxe.Base;

namespace Vocaluxe.Lib.Draw
{
    public delegate bool MessageEventHandler(ref Message m);

    public interface IFormHook
    {
        MessageEventHandler OnMessage { set; }
    }

    abstract class CDrawBaseWindows<TTextureType> : CDrawBase<TTextureType> where TTextureType : CTextureBase, IDisposable
    {
        private struct SClientRect
        {
            public Point Location;
            public int Width;
            public int Height;
        }

        protected Form _Form;
        private SClientRect _Restore;
        protected Size _SizeBeforeMinimize;

        public override void Close()
        {
            base.Close();
            try
            {
                _Form.Close();
            }
            catch {}
        }

        protected void _CenterToScreen()
        {
            Screen screen = Screen.FromControl(_Form);
            _Form.Location = new Point((screen.WorkingArea.Width - _Form.Width) / 2,
                                       (screen.WorkingArea.Height - _Form.Height) / 2);
        }

        private static bool _OnMessageAvoidScreenOff(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x112: // WM_SYSCOMMAND
                    switch ((int)m.WParam & 0xFFF0)
                    {
                        case 0xF100: // SC_KEYMENU
                            m.Result = IntPtr.Zero;
                            return false;
                        case 0xF140: // SC_SCREENSAVER
                        case 0xF170: // SC_MONITORPOWER
                            return false;
                    }
                    break;
            }
            return true;
        }

        protected override void _EnterFullScreen()
        {
            Debug.Assert(!_Fullscreen);
            _Fullscreen = true;

            _Restore.Location = _Form.Location;
            _Restore.Width = _Form.Width;
            _Restore.Height = _Form.Height;

            _Form.FormBorderStyle = FormBorderStyle.None;

            Screen screen = Screen.FromControl(_Form);
            _Form.DesktopBounds = new Rectangle(screen.Bounds.Location, new Size(screen.Bounds.Width, screen.Bounds.Height));

            if (_Form.WindowState == FormWindowState.Maximized)
            {
                _Form.WindowState = FormWindowState.Normal;
                _DoResize();
                _Form.WindowState = FormWindowState.Maximized;
            }
            else
                _DoResize();
        }

        protected override void _LeaveFullScreen()
        {
            Debug.Assert(_Fullscreen);
            _Fullscreen = false;

            _Form.FormBorderStyle = FormBorderStyle.Sizable;
            _Form.DesktopBounds = new Rectangle(_Restore.Location, new Size(_Restore.Width, _Restore.Height));
        }

        #region form event handlers
        private void _OnClose(object sender, CancelEventArgs e)
        {
            _Run = false;
        }

        private void _OnLoad(object sender, EventArgs e)
        {
            _ClearScreen();
        }

        protected virtual void _OnResize(object sender, EventArgs e)
        {
            _DoResize();
        }

        #region mouse event handlers
        protected void _OnMouseMove(object sender, MouseEventArgs e)
        {
            _Mouse.MouseMove(e);
        }

        protected void _OnMouseWheel(object sender, MouseEventArgs e)
        {
            _Mouse.MouseWheel(e);
        }

        protected void _OnMouseDown(object sender, MouseEventArgs e)
        {
            _Mouse.MouseDown(e);
        }

        protected void _OnMouseUp(object sender, MouseEventArgs e)
        {
            _Mouse.MouseUp(e);
        }

        protected void _OnMouseLeave(object sender, EventArgs e)
        {
            _Mouse.Visible = false;
            #if !WIN && !DEBUG
            _Form.Cursor = Cursors.Default;
            #endif
            Cursor.Show();
        }

        protected void _OnMouseEnter(object sender, EventArgs e)
        {
            Cursor.Hide();
            _Mouse.Visible = true;
            #if !WIN && !DEBUG //don't want to be stuck without a cursor when debugging
            _Form.Cursor = new Cursor("Linux/blank.cur"); //Cursor.Hide() doesn't work in Mono
            #endif
        }
        #endregion

        #region keyboard event handlers
        protected void _OnPreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            _OnKeyDown(sender, new KeyEventArgs(e.KeyData));
        }

        protected void _OnKeyDown(object sender, KeyEventArgs e)
        {
            _Keys.KeyDown(e);
        }

        protected void _OnKeyPress(object sender, KeyPressEventArgs e)
        {
            _Keys.KeyPress(e);
        }

        protected void _OnKeyUp(object sender, KeyEventArgs e)
        {
            _Keys.KeyUp(e);
        }
        #endregion keyboard event handlers

        #endregion

        public override bool Init()
        {
            if (!base.Init())
                return false;
            _Form.Icon = new Icon(Path.Combine(CSettings.ProgramFolder, CSettings.FileNameIcon));
            _Form.Text = CSettings.GetFullVersionText();
            ((IFormHook)_Form).OnMessage = _OnMessageAvoidScreenOff;
            _Form.Closing += _OnClose;
            _Form.Resize += _OnResize;
            _Form.Load += _OnLoad;

            _SizeBeforeMinimize = _Form.ClientSize;
            _CenterToScreen();

            return true;
        }

        public override void MainLoop()
        {
            _Form.Show();
            base.MainLoop();
            _Form.Hide();
        }
    }
}