#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Draw
{
    class CDrawWinForm : Form, IDraw
    {
        private bool _Run;
        private readonly Bitmap _Backbuffer;
        private readonly Graphics _G;
        private bool _Fullscreen;

        private readonly CKeys _Keys;
        private readonly CMouse _Mouse;

        private FormBorderStyle _BrdStyle;
        private bool _TopMost;
        private Rectangle _Bounds;

        private readonly Dictionary<int, CTexture> _Textures = new Dictionary<int, CTexture>();
        private readonly List<Bitmap> _Bitmaps = new List<Bitmap>();

        private readonly Color _ClearColor = Color.DarkBlue;

        public CDrawWinForm()
        {
            Icon = new Icon(Path.Combine(Environment.CurrentDirectory, CSettings.Icon));

            _Keys = new CKeys();
            _Mouse = new CMouse();
            ClientSize = new Size(1280, 720);

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);

            // Create the backbuffer
            _Backbuffer = new Bitmap(CSettings.RenderW, CSettings.RenderH);
            _G = Graphics.FromImage(_Backbuffer);
            _G.Clear(Color.DarkBlue);

            Paint += _OnPaintEvent;
            Closing += _OnClosingEvent;
            KeyDown += _OnKeyDownEvent;
            KeyPress += _OnKeyPressEvent;
            KeyUp += _OnKeyUpEvent;
            Resize += _OnResizeEvent;

            MouseMove += _OnMouseMove;
            MouseDown += _OnMouseDown;
            MouseUp += _OnMouseUp;

            _FlipBuffer();
            Cursor.Show();
        }

        private void _FlipBuffer()
        {
            _DrawBuffer();
            _G.Clear(_ClearColor);
        }

        private void _DrawBuffer()
        {
            Graphics frontBuffer = Graphics.FromHwnd(Handle);
            int h = ClientSize.Height;
            int w = ClientSize.Width;
            int y = 0;
            int x = 0;

            if (ClientSize.Width / (float)ClientSize.Height > CSettings.GetRenderAspect())
            {
                w = (int)Math.Round(ClientSize.Height * CSettings.GetRenderAspect());
                x = (ClientSize.Width - w) / 2;
            }
            else
            {
                h = (int)Math.Round(ClientSize.Width / CSettings.GetRenderAspect());
                y = (ClientSize.Height - h) / 2;
            }

            frontBuffer.DrawImage(_Backbuffer, new Rectangle(x, y, w, h), new Rectangle(0, 0, _Backbuffer.Width, _Backbuffer.Height), GraphicsUnit.Pixel);
        }

        private void _OnPaintEvent(object sender, PaintEventArgs e) {}

        #region FullScreenStuff
        private void _ToggleFullScreen()
        {
            if (!_Fullscreen)
                _Maximize(this);
            else
                _Restore(this);
        }

        private void _Maximize(Form targetForm)
        {
            if (!_Fullscreen)
            {
                _Save(targetForm);
                targetForm.FormBorderStyle = FormBorderStyle.None;
                targetForm.Bounds = Screen.PrimaryScreen.Bounds;
                targetForm.TopMost = true;
                _Fullscreen = true;

                CConfig.FullScreen = EOffOn.TR_CONFIG_ON;
                CConfig.SaveConfig();
            }
        }

        private void _Save(Form targetForm)
        {
            _BrdStyle = targetForm.FormBorderStyle;
            _TopMost = targetForm.TopMost;
            _Bounds = targetForm.Bounds;
        }

        private void _Restore(Form targetForm)
        {
            targetForm.FormBorderStyle = _BrdStyle;
            targetForm.TopMost = _TopMost;
            targetForm.Bounds = _Bounds;
            _Fullscreen = false;

            CConfig.FullScreen = EOffOn.TR_CONFIG_OFF;
            CConfig.SaveConfig();
        }
        #endregion FullScreenStuff

        private void _OnResizeEvent(object sender, EventArgs e)
        {
            Graphics frontBuffer = Graphics.FromHwnd(Handle);
            frontBuffer.Clear(Color.Black);
        }

        private void _OnClosingEvent(object sender, CancelEventArgs e)
        {
            _Run = false;
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x112: // WM_SYSCOMMAND
                    switch ((int)m.WParam & 0xFFF0)
                    {
                        case 0xF100: // SC_KEYMENU
                            m.Result = IntPtr.Zero;
                            break;
                        default:
                            base.WndProc(ref m);
                            break;
                    }
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        private void _OnKeyDownEvent(object sender, KeyEventArgs e)
        {
            _Keys.KeyDown(e);
        }

        private void _OnKeyPressEvent(object sender, KeyPressEventArgs e)
        {
            _Keys.KeyPress(e);
        }

        private void _OnKeyUpEvent(object sender, KeyEventArgs e)
        {
            _Keys.KeyUp(e);
        }

        #region mouse event handlers
        private void _OnMouseMove(object sender, MouseEventArgs e)
        {
            _Mouse.MouseMove(e);
        }

        private void _OnMouseDown(object sender, MouseEventArgs e)
        {
            _Mouse.MouseDown(e);
        }

        private void _OnMouseUp(object sender, MouseEventArgs e)
        {
            _Mouse.MouseUp(e);
        }
        #endregion

        public bool Init()
        {
            Text = CSettings.GetFullVersionText();
            return true;
        }

        public void MainLoop()
        {
            _Run = true;
            int delay = 0;
            Show();

            if (CConfig.FullScreen == EOffOn.TR_CONFIG_ON)
            {
                CSettings.IsFullScreen = true;
                _Maximize(this);
            }

            while (_Run)
            {
                Application.DoEvents();

                if (_Run)
                {
                    _Run = _Run && CGraphics.Draw();
                    _Run = CGraphics.UpdateGameLogic(_Keys, _Mouse);
                    _FlipBuffer();

                    if ((CSettings.IsFullScreen && !_Fullscreen) || (!CSettings.IsFullScreen && _Fullscreen))
                        _ToggleFullScreen();

                    if (CTime.IsRunning())
                        delay = (int)Math.Floor(CConfig.CalcCycleTime() - CTime.GetMilliseconds());

                    if (delay >= 1)
                        Thread.Sleep(delay);

                    CTime.CalculateFPS();
                    CTime.Restart();
                }
            }
            Close();
        }

        public void Unload()
        {
            try
            {
                Close();
            }
            catch {}
            Dispose();
        }

        public int GetScreenWidth()
        {
            return ClientSize.Width;
        }

        public int GetScreenHeight()
        {
            return ClientSize.Height;
        }

        public void ClearScreen()
        {
            _G.Clear(_ClearColor);
        }

        public CTexture CopyScreen()
        {
            Bitmap bmp = new Bitmap(_Backbuffer);
            _Bitmaps.Add(bmp);
            CTexture texture = _GetNewTexture(bmp.Width, bmp.Height);

            // Add to Texture List
            _Textures[texture.ID] = texture;

            return texture;
        }

        public void CopyScreen(ref CTexture texture)
        {
            if (!_TextureExists(texture) || texture.W2 != GetScreenWidth() || texture.H2 != GetScreenHeight())
            {
                RemoveTexture(ref texture);
                texture = CopyScreen();
            }
            else
                _Bitmaps[texture.ID] = new Bitmap(_Backbuffer);
        }

        public void MakeScreenShot()
        {
            const string file = "Screenshot_";
            string path = Path.Combine(CSettings.DataPath, CSettings.FolderScreenshots);

            int i = 0;
            while (File.Exists(Path.Combine(path, file + i.ToString("00000") + ".png")))
                i++;

            _Backbuffer.Save(Path.Combine(path, file + i.ToString("00000") + ".png"), ImageFormat.Png);
        }

        public void DrawLine(int a, int r, int g, int b, int w, int x1, int y1, int x2, int y2)
        {
            _G.DrawLine(new Pen(Color.FromArgb(a, r, g, b), w), new Point(x1, y1), new Point(x2, y2));
        }

        public void DrawColor(SColorF color, SRectF rect) {}

        public void DrawColorReflection(SColorF color, SRectF rect, float space, float height) {}

        public CTexture AddTexture(Bitmap bmp)
        {
            Bitmap bmp2 = new Bitmap(bmp);
            _Bitmaps.Add(bmp2);
            CTexture texture = _GetNewTexture(bmp.Width, bmp.Height);

            // Add to Texture List
            _Textures[texture.ID] = texture;

            return texture;
        }

        private CTexture _GetNewTexture(int w, int h)
        {
            return new CTexture(w, h) {ID = _Bitmaps.Count - 1};
        }

        public CTexture AddTexture(string texturePath)
        {
            if (File.Exists(texturePath))
            {
                foreach (CTexture tex in _Textures.Values)
                {
                    if (tex.TexturePath == texturePath)
                        return tex;
                }
                using (Bitmap bmp = new Bitmap(texturePath))
                {
                    CTexture texture = AddTexture(bmp);
                    texture.TexturePath = texturePath;
                }
            }

            return null;
        }

        private bool _TextureExists(CTexture texture)
        {
            return texture != null && _Textures.ContainsKey(texture.ID);
        }

        public void RemoveTexture(ref CTexture texture)
        {
            if (_TextureExists(texture))
            {
                _Bitmaps[texture.ID].Dispose();
                _Textures.Remove(texture.ID);
            }
            texture = null;
        }

        public CTexture EnqueueTexture(int w, int h, byte[] data)
        {
            return AddTexture(w, h, data);
        }

        public CTexture AddTexture(int w, int h, byte[] data)
        {
            using (Bitmap bmp = new Bitmap(w, h))
            {
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                Marshal.Copy(data, 0, bmpData.Scan0, data.Length);
                bmp.UnlockBits(bmpData);
                return AddTexture(bmp);
            }
        }

        public bool UpdateTexture(CTexture texture, int w, int h, byte[] data)
        {
            if (_TextureExists(texture))
            {
                BitmapData bmpData = _Bitmaps[texture.ID].LockBits(new Rectangle(0, 0, _Bitmaps[texture.ID].Width, _Bitmaps[texture.ID].Height),
                                                                   ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                Marshal.Copy(data, 0, bmpData.Scan0, data.Length);
                _Bitmaps[texture.ID].UnlockBits(bmpData);
                return true;
            }
            return false;
        }

        public bool UpdateOrAddTexture(ref CTexture texture, int w, int h, byte[] data)
        {
            if (!UpdateTexture(texture, w, h, data))
            {
                RemoveTexture(ref texture);
                texture = AddTexture(w, h, data);
            }
            return true;
        }

        public void DrawTexture(CTexture texture)
        {
            if (texture != null)
                DrawTexture(texture, texture.Rect, texture.Color);
        }

        public void DrawTexture(CTexture texture, SRectF rect)
        {
            if (texture != null)
                DrawTexture(texture, rect, texture.Color);
        }

        public void DrawTexture(CTexture texture, SRectF rect, SColorF color, bool mirrored = false)
        {
            if (texture != null)
                DrawTexture(texture, rect, color, rect, mirrored);
        }

        public void DrawTexture(CTexture texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored = false)
        {
            if (!_TextureExists(texture))
                return;
            Bitmap coloredBitmap = _ColorizeBitmap(_Bitmaps[texture.ID], color);
            _G.DrawImage(coloredBitmap, new RectangleF(rect.X, rect.Y, rect.W, rect.H));
            coloredBitmap.Dispose();
        }

        public void DrawTexture(CTexture texture, SRectF rect, SColorF color, float begin, float end) {}

        public void DrawTextureReflection(CTexture texture, SRectF rect, SColorF color, SRectF bounds, float space, float height) {}

        public int GetTextureCount()
        {
            return _Textures.Count;
        }

        private static Bitmap _ColorizeBitmap(Bitmap original, SColorF color)
        {
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                ColorMatrix cm = new ColorMatrix {Matrix33 = color.A, Matrix00 = color.R, Matrix11 = color.G, Matrix22 = color.B, Matrix44 = 1};

                using (ImageAttributes ia = new ImageAttributes())
                {
                    ia.SetColorMatrix(cm);

                    g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                                0, 0, original.Width, original.Height, GraphicsUnit.Pixel, ia);
                }
            }

            return newBitmap;
        }
    }
}