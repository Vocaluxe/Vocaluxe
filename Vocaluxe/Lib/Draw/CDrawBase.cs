using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Draw
{
    struct SClientRect
    {
        public Point Location;
        public int Width;
        public int Height;
    };

    struct STextureQueue
    {
        public readonly CTexture Texture;
        public readonly byte[] Data;

        public STextureQueue(CTexture texture, byte[] data)
        {
            Texture = texture;
            Data = data;
        }
    }

    abstract class CDrawBaseWindows : CDrawBase
    {
        public delegate bool MessageEventHandler(ref Message m);

        public interface IFormHook
        {
            MessageEventHandler OnMessage { set; }
        }

        protected Form _Form;
        private SClientRect _Restore;

        protected void _CenterToScreen()
        {
            Screen screen = Screen.FromControl(_Form);
            _Form.Location = new Point((screen.WorkingArea.Width - _Form.Width) / 2,
                                       (screen.WorkingArea.Height - _Form.Height) / 2);
        }

        private bool _OnMessageAvoidScreenOff(ref Message m)
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

        protected void _ToggleFullScreen()
        {
            if (!_Fullscreen)
                _EnterFullScreen();
            else
                _LeaveFullScreen();
        }

        protected virtual void _EnterFullScreen()
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

        protected abstract void _DoResize();

        protected void _LeaveFullScreen()
        {
            Debug.Assert(_Fullscreen);
            _Fullscreen = false;

            _Form.FormBorderStyle = FormBorderStyle.Sizable;
            _Form.DesktopBounds = new Rectangle(_Restore.Location, new Size(_Restore.Width, _Restore.Height));
        }

        public virtual bool Init()
        {
            _Form.Icon = new Icon(Path.Combine(CSettings.ProgramFolder, CSettings.FileNameIcon));
            _Form.Text = CSettings.GetFullVersionText();
            ((IFormHook)_Form).OnMessage = _OnMessageAvoidScreenOff;
            return true;
        }
    }

    abstract class CDrawBase
    {
        protected bool _NonPowerOf2TextureSupported;
        protected bool _Fullscreen;

        private readonly Object _MutexID = new object();
        private int _NextID;

        /// <summary>
        ///     Calculates the next power of two if the device has the POW2 flag set
        /// </summary>
        /// <param name="n">The value of which the next power of two will be calculated</param>
        /// <returns>The next power of two</returns>
        private int _CheckForNextPowerOf2(int n)
        {
            if (_NonPowerOf2TextureSupported)
                return n;
            if (n < 0)
                throw new ArgumentOutOfRangeException("n", "Must be positive.");
            return (int)Math.Pow(2, Math.Ceiling(Math.Log(n, 2)));
        }

        public CTexture GetNewTexture(int origWidth, int origHeight, int dataWidth = 0, int dataHeight = 0)
        {
            Debug.Assert(origWidth > 0 && origHeight > 0);
            Debug.Assert(dataWidth > 0 || dataHeight <= 0);
            int id;
            lock (_MutexID)
            {
                id = _NextID++;
            }
            Size origSize = new Size(origWidth, origHeight);
            Size dataSize = (dataHeight > 0) ? new Size(dataWidth, dataHeight) : origSize;
            return new CTexture(id, origSize, dataSize, _CheckForNextPowerOf2(dataSize.Width), _CheckForNextPowerOf2(dataSize.Height));
        }
    }
}