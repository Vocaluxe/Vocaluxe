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

using System.Drawing.Drawing2D;
using System.Threading;
using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Menu;

namespace Vocaluxe.Lib.Draw
{
    struct SClientRect
    {
        public Point Location;
        public int Width;
        public int Height;
    };

    struct STextureQueque
    {
        public int ID;
        public int Width;
        public int Height;
        public byte[] Data;
    }

    class COpenGL : Form, IDraw
    {
        #region private vars
        private readonly CKeys _Keys;
        private readonly CMouse _Mouse;
        private bool _Run;

        private readonly GLControl _Control;

        private SClientRect _Restore;
        private bool _Fullscreen;

        private readonly Dictionary<int, STexture> _Textures;
        private readonly Queue<int> _IDs;
        private readonly List<STextureQueque> _Queque;

        private readonly Object _MutexTexture = new Object();

        private int _H = 1;
        private int _W = 1;
        private int _Y;
        private int _X;

        private bool _UsePBO;
        #endregion private vars

        public COpenGL()
        {
            Icon = new Icon(Path.Combine(Environment.CurrentDirectory, CSettings.Icon));

            _Textures = new Dictionary<int, STexture>();
            _Queque = new List<STextureQueque>();
            _IDs = new Queue<int>(1000000);

            for (int i = 1; i < 1000000; i++)
                _IDs.Enqueue(i);

            //Check AA Mode
            CConfig.AAMode = (EAntiAliasingModes)_CheckAntiAliasingMode((int)CConfig.AAMode);
            CConfig.ColorDepth = (EColorDepth)_CheckColorDepth((int)CConfig.ColorDepth);

            bool ok = false;
            try
            {
                GraphicsMode gm = new GraphicsMode((int)CConfig.ColorDepth, 24, 0, (int)CConfig.AAMode);
                _Control = new GLControl(gm, 2, 1, GraphicsContextFlags.Default);
                if (_Control.GraphicsMode != null)
                    ok = true;
            }
            catch (Exception)
            {
                ok = false;
            }

            if (!ok)
                _Control = new GLControl();

            _Control.MakeCurrent();
            _Control.VSync = CConfig.VSync == EOffOn.TR_CONFIG_ON;

            Controls.Add(_Control);


            _Keys = new CKeys();
            Paint += _OnPaintEvent;
            Closing += _OnClosingEvent;
            Resize += _OnResizeEvent;

            _Control.KeyDown += _OnKeyDownEvent;
            _Control.PreviewKeyDown += _OnPreviewKeyDownEvent;
            _Control.KeyPress += _OnKeyPressEvent;
            _Control.KeyUp += _OnKeyUpEvent;

            _Mouse = new CMouse();
            _Control.MouseMove += _OnMouseMove;
            _Control.MouseWheel += _OnMouseWheel;
            _Control.MouseDown += _OnMouseDown;
            _Control.MouseUp += _OnMouseUp;
            _Control.MouseLeave += _OnMouseLeave;
            _Control.MouseEnter += _OnMouseEnter;

            ClientSize = new Size(CConfig.ScreenW, CConfig.ScreenH);
            CenterToScreen();
        }

        #region Helpers
        private int _CheckAntiAliasingMode(int setValue)
        {
            int samples = 0;

            if (setValue > 32)
                setValue = 32;

            while (samples <= setValue)
            {
                GraphicsMode mode;
                try
                {
                    mode = new GraphicsMode(16, 0, 0, samples);
                }
                catch (Exception)
                {
                    break;
                }

                if (mode.Samples != samples)
                    break;
                if (samples == 0)
                    samples = 2;
                else
                    samples *= 2;
            }

            if (samples == 2)
                return 0;
            return samples / 2;
        }

        private int _CheckColorDepth(int setValue)
        {
            int result = 16;

            if (setValue > 32)
                setValue = 32;

            while (result <= setValue)
            {
                GraphicsMode mode;
                try
                {
                    mode = new GraphicsMode(result, 0, 0, 0);
                }
                catch (Exception)
                {
                    break;
                }
                if (mode.ColorFormat != result)
                    break;
                result += 8;
            }

            return result - 8;
        }

        private void _ToggleFullScreen()
        {
            if (!_Fullscreen)
                _EnterFullScreen();
            else
                _LeaveFullScreen();
        }

        private void _EnterFullScreen()
        {
            _Fullscreen = true;
            CConfig.FullScreen = EOffOn.TR_CONFIG_ON;

            _Restore.Location = Location;
            _Restore.Width = Width;
            _Restore.Height = Height;

            FormBorderStyle = FormBorderStyle.None;

            int screenNr = 0;
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                Screen scr = Screen.AllScreens[i];
                if (scr.Bounds.Top <= Top && scr.Bounds.Left <= Left)
                    screenNr = i;
            }

            DesktopBounds = new Rectangle(Screen.AllScreens[screenNr].Bounds.Location,
                                          new Size(Screen.AllScreens[screenNr].Bounds.Width, Screen.AllScreens[screenNr].Bounds.Height));

            if (WindowState == FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Normal;
                _DoResize();
                WindowState = FormWindowState.Maximized;
            }
            else
                _DoResize();

            CConfig.SaveConfig();
        }

        private void _LeaveFullScreen()
        {
            _Fullscreen = false;
            CConfig.FullScreen = EOffOn.TR_CONFIG_OFF;

            FormBorderStyle = FormBorderStyle.Sizable;
            DesktopBounds = new Rectangle(_Restore.Location, new Size(_Restore.Width, _Restore.Height));

            CConfig.SaveConfig();
        }
        #endregion Helpers

        #region form events
        private void _OnPaintEvent(object sender, PaintEventArgs e) {}

        private void _OnResizeEvent(object sender, EventArgs e) {}

        private void _OnClosingEvent(object sender, CancelEventArgs e)
        {
            _Run = false;
        }

        protected override void OnLoad(EventArgs e)
        {
            OpenTK.Graphics.OpenGL.GL.ClearColor(Color.Black);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            _Control.ClientSize = ClientSize;
            _DoResize();
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
        #endregion form events

        #region mouse event handlers
        private void _OnMouseMove(object sender, MouseEventArgs e)
        {
            _Mouse.MouseMove(e);
        }

        private void _OnMouseWheel(object sender, MouseEventArgs e)
        {
            _Mouse.MouseWheel(e);
        }

        private void _OnMouseDown(object sender, MouseEventArgs e)
        {
            _Mouse.MouseDown(e);
        }

        private void _OnMouseUp(object sender, MouseEventArgs e)
        {
            _Mouse.MouseUp(e);
        }

        private void _OnMouseLeave(object sender, EventArgs e)
        {
            _Mouse.Visible = false;
            Cursor.Show();
        }

        private void _OnMouseEnter(object sender, EventArgs e)
        {
            Cursor.Hide();
            _Mouse.Visible = true;
        }
        #endregion

        #region keyboard event handlers
        private void _OnPreviewKeyDownEvent(object sender, PreviewKeyDownEventArgs e)
        {
            _OnKeyDownEvent(sender, new KeyEventArgs(e.KeyData));
        }

        private void _OnKeyDownEvent(object sender, KeyEventArgs e)
        {
            _Keys.KeyDown(e);
        }

        private void _OnKeyPressEvent(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            _Keys.KeyPress(e);
        }

        private void _OnKeyUpEvent(object sender, KeyEventArgs e)
        {
            _Keys.KeyUp(e);
        }
        #endregion keyboard event handlers

        private void _DoResize()
        {
            _H = _Control.Height;
            _W = _Control.Width;
            _Y = 0;
            _X = 0;


            if (_W / (float)_H > CSettings.GetRenderAspect())
            {
                _W = (int)Math.Round(_H * CSettings.GetRenderAspect());
                _X = (_Control.Width - _W) / 2;
            }
            else
            {
                _H = (int)Math.Round(_W / CSettings.GetRenderAspect());
                _Y = (_Control.Height - _H) / 2;
            }

            OpenTK.Graphics.OpenGL.GL.MatrixMode(OpenTK.Graphics.OpenGL.MatrixMode.Projection);
            OpenTK.Graphics.OpenGL.GL.LoadIdentity();
            OpenTK.Graphics.OpenGL.GL.Ortho(0, CSettings.RenderW, CSettings.RenderH, 0, CSettings.ZNear, CSettings.ZFar);
            OpenTK.Graphics.OpenGL.GL.Viewport(_X, _Y, _W, _H);
        }

        #region implementation

        #region main stuff
        public bool Init()
        {
            Text = CSettings.GetFullVersionText();

            // Init Texturing
            OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Texture2D);

            OpenTK.Graphics.OpenGL.GL.BlendFunc(OpenTK.Graphics.OpenGL.BlendingFactorSrc.SrcAlpha, OpenTK.Graphics.OpenGL.BlendingFactorDest.OneMinusSrcAlpha);
            OpenTK.Graphics.OpenGL.GL.PixelStore(OpenTK.Graphics.OpenGL.PixelStoreParameter.UnpackAlignment, 1);

            OpenTK.Graphics.OpenGL.GL.DepthRange(CSettings.ZFar, CSettings.ZNear);
            OpenTK.Graphics.OpenGL.GL.DepthFunc(OpenTK.Graphics.OpenGL.DepthFunction.Lequal);
            OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.DepthTest);

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
                _EnterFullScreen();
            }

            while (_Run)
            {
                Application.DoEvents();

                if (_Run)
                {
                    ClearScreen();
                    _Run = _Run && CGraphics.Draw();

                    _Run = CGraphics.UpdateGameLogic(_Keys, _Mouse);
                    _Control.SwapBuffers();

                    if ((CSettings.IsFullScreen && !_Fullscreen) || (!CSettings.IsFullScreen && _Fullscreen))
                        _ToggleFullScreen();

                    _CheckQueque();

                    if (CTime.IsRunning())
                        delay = (int)Math.Floor(CConfig.CalcCycleTime() - CTime.GetMilliseconds());

                    if (delay >= 1 && CConfig.VSync == EOffOn.TR_CONFIG_OFF)
                        Thread.Sleep(delay);

                    CTime.CalculateFPS();
                    CTime.Restart();
                }
            }
            Close();
        }

        public bool Unload()
        {
            STexture[] textures = new STexture[_Textures.Count];
            _Textures.Values.CopyTo(textures, 0);
            for (int i = 0; i < _Textures.Count; i++)
                RemoveTexture(ref textures[i]);

            return true;
        }

        public int GetScreenWidth()
        {
            return _Control.Width;
        }

        public int GetScreenHeight()
        {
            return _Control.Height;
        }

        public RectangleF GetTextBounds(CText text)
        {
            return GetTextBounds(text, text.Height);
        }

        public RectangleF GetTextBounds(CText text, float height)
        {
            CFonts.Height = height;
            CFonts.SetFont(text.Font);
            CFonts.Style = text.Style;
            return new RectangleF(text.X, text.Y, CFonts.GetTextWidth(CLanguage.Translate(text.Text)), CFonts.GetTextHeight(CLanguage.Translate(text.Text)));
        }
        #endregion main stuff

        #region Basic Draw Methods
        public void ClearScreen()
        {
            OpenTK.Graphics.OpenGL.GL.Clear(OpenTK.Graphics.OpenGL.ClearBufferMask.ColorBufferBit | OpenTK.Graphics.OpenGL.ClearBufferMask.DepthBufferBit);
        }

        public STexture CopyScreen()
        {
            STexture texture = new STexture(-1);

            int id = OpenTK.Graphics.OpenGL.GL.GenTexture();
            OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, id);
            texture.ID = id;

            texture.Width = _W;
            texture.Height = _H;
            texture.W2 = MathHelper.NextPowerOfTwo(texture.Width);
            texture.H2 = MathHelper.NextPowerOfTwo(texture.Height);

            texture.WidthRatio = texture.Width / texture.W2;
            texture.HeightRatio = texture.Height / texture.H2;

            OpenTK.Graphics.OpenGL.GL.TexImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelInternalFormat.Rgba, (int)texture.W2,
                                                 (int)texture.H2, 0,
                                                 OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, IntPtr.Zero);

            OpenTK.Graphics.OpenGL.GL.CopyTexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, _X, _Y,
                                                        (int)texture.Width, (int)texture.Height);

            OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureMagFilter,
                                                   (int)OpenTK.Graphics.OpenGL.TextureMagFilter.Linear);
            OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureMinFilter,
                                                   (int)OpenTK.Graphics.OpenGL.TextureMinFilter.Linear);

            OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0);

            // Add to Texture List
            texture.Color = new SColorF(1f, 1f, 1f, 1f);
            texture.Rect = new SRectF(0f, 0f, texture.Width, texture.Height, 0f);

            lock (_MutexTexture)
            {
                texture.Index = _IDs.Dequeue();
                _Textures[texture.Index] = texture;
            }

            return texture;
        }

        public void CopyScreen(ref STexture texture)
        {
            if (!_TextureExists(ref texture) || Math.Abs(texture.Width - GetScreenWidth()) > 1 || Math.Abs(texture.Height - GetScreenHeight()) > 1)
            {
                RemoveTexture(ref texture);
                texture = CopyScreen();
            }
            else
            {
                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, texture.ID);

                OpenTK.Graphics.OpenGL.GL.CopyTexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, 0, 0,
                                                            (int)texture.Width, (int)texture.Height);

                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0);
            }
        }

        public void MakeScreenShot()
        {
            const string file = "Screenshot_";
            string path = Path.Combine(Environment.CurrentDirectory, CSettings.FolderScreenshots);

            int i = 0;
            while (File.Exists(Path.Combine(path, file + i.ToString("00000") + ".bmp")))
                i++;

            int width = GetScreenWidth();
            int height = GetScreenHeight();

            using (Bitmap screen = new Bitmap(width, height))
            {
                BitmapData bmpData = screen.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                OpenTK.Graphics.OpenGL.GL.ReadPixels(0, 0, width, height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, bmpData.Scan0);
                screen.UnlockBits(bmpData);

                screen.RotateFlip(RotateFlipType.RotateNoneFlipY);
                screen.Save(Path.Combine(path, file + i.ToString("00000") + ".bmp"), ImageFormat.Bmp);
            }
        }

        public void DrawLine(int a, int r, int g, int b, int w, int x1, int y1, int x2, int y2) {}

        // Draw Basic Text (must be deleted later)
        public void DrawText(string text, int x, int y, int h, int z = 0)
        {
            CFonts.DrawText(text, h, x, y, z, new SColorF(1, 1, 1, 1));
        }

        public void DrawColor(SColorF color, SRectF rect)
        {
            OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Blend);
            OpenTK.Graphics.OpenGL.GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);

            OpenTK.Graphics.OpenGL.GL.Begin(OpenTK.Graphics.OpenGL.BeginMode.Quads);
            OpenTK.Graphics.OpenGL.GL.Vertex3(rect.X, rect.Y, rect.Z + CGraphics.ZOffset);
            OpenTK.Graphics.OpenGL.GL.Vertex3(rect.X, rect.Y + rect.H, rect.Z + CGraphics.ZOffset);
            OpenTK.Graphics.OpenGL.GL.Vertex3(rect.X + rect.W, rect.Y + rect.H, rect.Z + CGraphics.ZOffset);
            OpenTK.Graphics.OpenGL.GL.Vertex3(rect.X + rect.W, rect.Y, rect.Z + CGraphics.ZOffset);
            OpenTK.Graphics.OpenGL.GL.End();

            OpenTK.Graphics.OpenGL.GL.Disable(OpenTK.Graphics.OpenGL.EnableCap.Blend);
        }

        public void DrawColorReflection(SColorF color, SRectF rect, float space, float height)
        {
            if (rect.H < height)
                height = rect.H;

            float rx1 = rect.X;
            float rx2 = rect.X + rect.W;
            float ry1 = rect.Y + rect.H + space;
            float ry2 = rect.Y + rect.H + space + height;

            if (rx1 < rect.X)
                rx1 = rect.X;

            if (rx2 > rect.X + rect.W)
                rx2 = rect.X + rect.W;

            if (ry1 < rect.Y + space)
                ry1 = rect.Y + space;

            if (ry2 > rect.Y + rect.H + space + height)
                ry2 = rect.Y + rect.H + space + height;


            OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Blend);

            if (Math.Abs(rect.Rotation) > 0.001)
            {
                OpenTK.Graphics.OpenGL.GL.Translate(0.5f, 0.5f, 0);
                OpenTK.Graphics.OpenGL.GL.Rotate(-rect.Rotation, 0f, 0f, 1f);
                OpenTK.Graphics.OpenGL.GL.Translate(-0.5f, -0.5f, 0);
            }

            OpenTK.Graphics.OpenGL.GL.Begin(OpenTK.Graphics.OpenGL.BeginMode.Quads);

            OpenTK.Graphics.OpenGL.GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);
            OpenTK.Graphics.OpenGL.GL.Vertex3(rx2, ry1, rect.Z + CGraphics.ZOffset);

            OpenTK.Graphics.OpenGL.GL.Color4(color.R, color.G, color.B, 0f);
            OpenTK.Graphics.OpenGL.GL.Vertex3(rx2, ry2, rect.Z + CGraphics.ZOffset);

            OpenTK.Graphics.OpenGL.GL.Color4(color.R, color.G, color.B, 0f);
            OpenTK.Graphics.OpenGL.GL.Vertex3(rx1, ry2, rect.Z + CGraphics.ZOffset);

            OpenTK.Graphics.OpenGL.GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);
            OpenTK.Graphics.OpenGL.GL.Vertex3(rx1, ry1, rect.Z + CGraphics.ZOffset);

            OpenTK.Graphics.OpenGL.GL.End();

            OpenTK.Graphics.OpenGL.GL.Disable(OpenTK.Graphics.OpenGL.EnableCap.Blend);
        }
        #endregion Basic Draw Methods

        #region Textures

        #region adding
        public STexture AddTexture(string texturePath)
        {
            if (File.Exists(texturePath))
            {
                Bitmap bmp;
                try
                {
                    bmp = new Bitmap(texturePath);
                }
                catch (Exception)
                {
                    CLog.LogError("Error loading Texture: " + texturePath);
                    return new STexture(-1);
                }
                try
                {
                    return AddTexture(bmp);
                }
                finally
                {
                    bmp.Dispose();
                }
            }
            CLog.LogError("Can't find File: " + texturePath);
            return new STexture(-1);
        }

        public STexture AddTexture(Bitmap bmp)
        {
            STexture texture = new STexture(-1);

            if (bmp.Height == 0 || bmp.Width == 0)
                return texture;

            int maxSize;
            switch (CConfig.TextureQuality)
            {
                case ETextureQuality.TR_CONFIG_TEXTURE_LOWEST:
                    maxSize = 128;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_LOW:
                    maxSize = 256;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_MEDIUM:
                    maxSize = 512;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_HIGH:
                    maxSize = 1024;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_HIGHEST:
                    maxSize = 2048;
                    break;
                default:
                    maxSize = 512;
                    break;
            }

            int w = bmp.Width;
            int h = bmp.Height;

            if (w > maxSize)
            {
                h = (int)Math.Round((float)maxSize / bmp.Width * bmp.Height);
                w = maxSize;
            }

            if (h > maxSize)
            {
                w = (int)Math.Round((float)maxSize / bmp.Height * bmp.Width);
                h = maxSize;
            }

            int id = OpenTK.Graphics.OpenGL.GL.GenTexture();
            OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, id);
            texture.ID = id;

            texture.Width = w;
            texture.Height = h;

            texture.W2 = MathHelper.NextPowerOfTwo(w);
            texture.H2 = MathHelper.NextPowerOfTwo(h);

            using (Bitmap bmp2 = new Bitmap((int)texture.W2, (int)texture.H2))
            {
                Graphics g = Graphics.FromImage(bmp2);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(bmp, new Rectangle(0, 0, bmp2.Width, bmp2.Height));
                g.Dispose();

                texture.WidthRatio = 1f;
                texture.HeightRatio = 1f;

                BitmapData bmpData = bmp2.LockBits(new Rectangle(0, 0, bmp2.Width, bmp2.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);


                OpenTK.Graphics.OpenGL.GL.TexImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelInternalFormat.Rgba, (int)texture.W2,
                                                     (int)texture.H2, 0,
                                                     OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, IntPtr.Zero);

                OpenTK.Graphics.OpenGL.GL.TexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, bmpData.Width, bmpData.Height,
                                                        OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, bmpData.Scan0);

                bmp2.UnlockBits(bmpData);
            }

            OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureWrapS,
                                                   (int)OpenTK.Graphics.OpenGL.TextureParameterName.ClampToEdge);
            OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureWrapT,
                                                   (int)OpenTK.Graphics.OpenGL.TextureParameterName.ClampToEdge);

            OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureMagFilter,
                                                   (int)OpenTK.Graphics.OpenGL.TextureMagFilter.Linear);
            OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureMinFilter,
                                                   (int)OpenTK.Graphics.OpenGL.TextureMinFilter.Linear);

            OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureMinFilter,
                                                   (int)OpenTK.Graphics.OpenGL.TextureMinFilter.LinearMipmapLinear);
            //GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            OpenTK.Graphics.OpenGL.GL.Ext.GenerateMipmap(OpenTK.Graphics.OpenGL.GenerateMipmapTarget.Texture2D);


            // Add to Texture List
            texture.Color = new SColorF(1f, 1f, 1f, 1f);
            texture.Rect = new SRectF(0f, 0f, texture.Width, texture.Height, 0f);
            texture.TexturePath = String.Empty;

            lock (_MutexTexture)
            {
                texture.Index = _IDs.Dequeue();
                _Textures[texture.Index] = texture;
            }

            return texture;
        }

        public STexture AddTexture(int w, int h, IntPtr data)
        {
            STexture texture = new STexture(-1);

            if (_UsePBO)
            {
                try
                {
                    OpenTK.Graphics.OpenGL.GL.GenBuffers(1, out texture.PBO);
                    OpenTK.Graphics.OpenGL.GL.BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, texture.PBO);
                    OpenTK.Graphics.OpenGL.GL.BufferData(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, (IntPtr)(w * h * 4), IntPtr.Zero,
                                                         OpenTK.Graphics.OpenGL.BufferUsageHint.StreamDraw);
                    OpenTK.Graphics.OpenGL.GL.BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, 0);
                }
                catch (Exception)
                {
                    _UsePBO = false;
                }
            }

            int id = OpenTK.Graphics.OpenGL.GL.GenTexture();
            OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, id);
            texture.ID = id;

            texture.Width = w;
            texture.Height = h;
            texture.W2 = MathHelper.NextPowerOfTwo(texture.Width);
            texture.H2 = MathHelper.NextPowerOfTwo(texture.Height);

            texture.WidthRatio = texture.Width / texture.W2;
            texture.HeightRatio = texture.Height / texture.H2;

            OpenTK.Graphics.OpenGL.GL.TexImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelInternalFormat.Rgba, (int)texture.W2,
                                                 (int)texture.H2, 0,
                                                 OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, IntPtr.Zero);

            OpenTK.Graphics.OpenGL.GL.TexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, w, h,
                                                    OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, data);


            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureParameterName.ClampToEdge);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureParameterName.ClampToEdge);
            OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureMagFilter,
                                                   (int)OpenTK.Graphics.OpenGL.TextureMagFilter.Linear);
            OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureMinFilter,
                                                   (int)OpenTK.Graphics.OpenGL.TextureMinFilter.Linear);

            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            //GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            //GL.Ext.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0);

            // Add to Texture List
            texture.Color = new SColorF(1f, 1f, 1f, 1f);
            texture.Rect = new SRectF(0f, 0f, texture.Width, texture.Height, 0f);
            texture.TexturePath = String.Empty;

            lock (_MutexTexture)
            {
                texture.Index = _IDs.Dequeue();
                _Textures[texture.Index] = texture;
            }

            return texture;
        }

        public STexture AddTexture(int w, int h, ref byte[] data)
        {
            STexture texture = new STexture(-1);

            if (_UsePBO)
            {
                try
                {
                    OpenTK.Graphics.OpenGL.GL.GenBuffers(1, out texture.PBO);
                    OpenTK.Graphics.OpenGL.GL.BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, texture.PBO);
                    OpenTK.Graphics.OpenGL.GL.BufferData(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, (IntPtr)data.Length, IntPtr.Zero,
                                                         OpenTK.Graphics.OpenGL.BufferUsageHint.StreamDraw);
                    OpenTK.Graphics.OpenGL.GL.BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, 0);
                }
                catch (Exception)
                {
                    //throw;
                    _UsePBO = false;
                }
            }

            int id = OpenTK.Graphics.OpenGL.GL.GenTexture();
            OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, id);
            texture.ID = id;

            texture.Width = w;
            texture.Height = h;
            texture.W2 = MathHelper.NextPowerOfTwo(texture.Width);
            texture.H2 = MathHelper.NextPowerOfTwo(texture.Height);

            texture.WidthRatio = texture.Width / texture.W2;
            texture.HeightRatio = texture.Height / texture.H2;

            OpenTK.Graphics.OpenGL.GL.TexImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelInternalFormat.Rgba, (int)texture.W2,
                                                 (int)texture.H2, 0,
                                                 OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, IntPtr.Zero);

            OpenTK.Graphics.OpenGL.GL.TexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, w, h,
                                                    OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, data);


            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureParameterName.ClampToEdge);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureParameterName.ClampToEdge);
            OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureMagFilter,
                                                   (int)OpenTK.Graphics.OpenGL.TextureMagFilter.Linear);
            OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureMinFilter,
                                                   (int)OpenTK.Graphics.OpenGL.TextureMinFilter.Linear);

            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            //GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            //GL.Ext.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0);

            // Add to Texture List
            texture.Color = new SColorF(1f, 1f, 1f, 1f);
            texture.Rect = new SRectF(0f, 0f, texture.Width, texture.Height, 0f);
            texture.TexturePath = String.Empty;

            lock (_MutexTexture)
            {
                texture.Index = _IDs.Dequeue();
                _Textures[texture.Index] = texture;
            }

            return texture;
        }

        public STexture QuequeTexture(int w, int h, ref byte[] data)
        {
            STexture texture = new STexture(-1);
            STextureQueque queque = new STextureQueque {Data = data, Height = h, Width = w};

            texture.Height = h;
            texture.Width = w;

            lock (_MutexTexture)
            {
                texture.Index = _IDs.Dequeue();
                queque.ID = texture.Index;
                _Queque.Add(queque);
                _Textures[texture.Index] = texture;
            }

            return texture;
        }
        #endregion adding

        #region updating
        public bool UpdateTexture(ref STexture texture, IntPtr data)
        {
            if (_TextureExists(ref texture))
            {
                if (_UsePBO)
                {
                    try
                    {
                        OpenTK.Graphics.OpenGL.GL.BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, texture.PBO);

                        IntPtr buffer = OpenTK.Graphics.OpenGL.GL.MapBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, OpenTK.Graphics.OpenGL.BufferAccess.WriteOnly);
                        byte[] d = new byte[(int)texture.Height * (int)texture.Width * 4];

                        Marshal.Copy(data, d, 0, (int)texture.Height * (int)texture.Width * 4);
                        Marshal.Copy(d, 0, buffer, (int)texture.Height * (int)texture.Width * 4);

                        OpenTK.Graphics.OpenGL.GL.UnmapBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer);

                        OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, texture.ID);
                        OpenTK.Graphics.OpenGL.GL.TexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, (int)texture.Width, (int)texture.Height,
                                                                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, IntPtr.Zero);

                        OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0);
                        OpenTK.Graphics.OpenGL.GL.BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, 0);

                        return true;
                    }
                    catch (Exception)
                    {
                        _UsePBO = false;
                    }
                }

                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, texture.ID);

                OpenTK.Graphics.OpenGL.GL.TexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, (int)texture.Width, (int)texture.Height,
                                                        OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, data);

                OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureWrapS,
                                                       (int)OpenTK.Graphics.OpenGL.TextureParameterName.ClampToEdge);
                OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureWrapT,
                                                       (int)OpenTK.Graphics.OpenGL.TextureParameterName.ClampToEdge);

                //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureMinFilter,
                                                       (int)OpenTK.Graphics.OpenGL.TextureMinFilter.Linear);

                OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureMinFilter,
                                                       (int)OpenTK.Graphics.OpenGL.TextureMinFilter.LinearMipmapLinear);
                //GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                OpenTK.Graphics.OpenGL.GL.Ext.GenerateMipmap(OpenTK.Graphics.OpenGL.GenerateMipmapTarget.Texture2D);

                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0);

                return true;
            }
            return false;
        }

        public bool UpdateTexture(ref STexture texture, ref byte[] data)
        {
            if (_TextureExists(ref texture))
            {
                if (_UsePBO)
                {
                    OpenTK.Graphics.OpenGL.GL.BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, texture.PBO);

                    IntPtr buffer = OpenTK.Graphics.OpenGL.GL.MapBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, OpenTK.Graphics.OpenGL.BufferAccess.WriteOnly);
                    Marshal.Copy(data, 0, buffer, data.Length);

                    OpenTK.Graphics.OpenGL.GL.UnmapBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer);

                    OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, texture.ID);
                    OpenTK.Graphics.OpenGL.GL.TexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, (int)texture.Width, (int)texture.Height,
                                                            OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, IntPtr.Zero);

                    OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0);
                    OpenTK.Graphics.OpenGL.GL.BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, 0);

                    return true;
                }

                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, texture.ID);

                OpenTK.Graphics.OpenGL.GL.TexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, (int)texture.Width, (int)texture.Height,
                                                        OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, data);

                //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureParameterName.ClampToEdge);
                //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureParameterName.ClampToEdge);
                OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureMagFilter,
                                                       (int)OpenTK.Graphics.OpenGL.TextureMagFilter.Linear);
                OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureMinFilter,
                                                       (int)OpenTK.Graphics.OpenGL.TextureMinFilter.Linear);

                //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                //GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                //GL.Ext.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0);

                return true;
            }
            return false;
        }
        #endregion updating

        public void RemoveTexture(ref STexture texture)
        {
            if ((texture.Index > 0) && (_Textures.Count > 0))
            {
                lock (_MutexTexture)
                {
                    _IDs.Enqueue(texture.Index);
                    OpenTK.Graphics.OpenGL.GL.DeleteTexture(texture.ID);
                    if (texture.PBO > 0)
                        OpenTK.Graphics.OpenGL.GL.DeleteBuffers(1, ref texture.PBO);
                    _Textures.Remove(texture.Index);
                    texture.Index = -1;
                    texture.ID = -1;
                }
            }
        }

        private bool _TextureExists(ref STexture texture)
        {
            lock (_MutexTexture)
            {
                if (_Textures.ContainsKey(texture.Index))
                {
                    if (_Textures[texture.Index].ID > 0)
                    {
                        texture = _Textures[texture.Index];
                        return true;
                    }
                }
            }
            return false;
        }

        #region drawing
        public void DrawTexture(STexture texture)
        {
            DrawTexture(texture, texture.Rect, texture.Color);
        }

        public void DrawTexture(STexture texture, SRectF rect)
        {
            DrawTexture(texture, rect, texture.Color, false);
        }

        public void DrawTexture(STexture texture, SRectF rect, SColorF color)
        {
            DrawTexture(texture, rect, color, false);
        }

        public void DrawTexture(STexture texture, SRectF rect, SColorF color, SRectF bounds)
        {
            DrawTexture(texture, rect, color, bounds, false);
        }

        public void DrawTexture(STexture texture, SRectF rect, SColorF color, bool mirrored)
        {
            DrawTexture(texture, rect, color, new SRectF(0, 0, CSettings.RenderW, CSettings.RenderH, rect.Z), mirrored);
        }

        public void DrawTexture(STexture texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored)
        {
            if (Math.Abs(rect.W) < float.Epsilon || Math.Abs(rect.H) < float.Epsilon || Math.Abs(bounds.H) < float.Epsilon || Math.Abs(bounds.W) < float.Epsilon ||
                Math.Abs(color.A) < float.Epsilon)
                return;

            if (bounds.X > rect.X + rect.W || bounds.X + bounds.W < rect.X)
                return;

            if (bounds.Y > rect.Y + rect.H || bounds.Y + bounds.H < rect.Y)
                return;

            if (_TextureExists(ref texture))
            {
                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, texture.ID);

                float x1 = (bounds.X - rect.X) / rect.W * texture.WidthRatio;
                float x2 = (bounds.X + bounds.W - rect.X) / rect.W * texture.WidthRatio;
                float y1 = (bounds.Y - rect.Y) / rect.H * texture.HeightRatio;
                float y2 = (bounds.Y + bounds.H - rect.Y) / rect.H * texture.HeightRatio;

                if (x1 < 0)
                    x1 = 0f;

                if (x2 > texture.WidthRatio)
                    x2 = texture.WidthRatio;

                if (y1 < 0)
                    y1 = 0f;

                if (y2 > texture.HeightRatio)
                    y2 = texture.HeightRatio;


                float rx1 = rect.X;
                float rx2 = rect.X + rect.W;
                float ry1 = rect.Y;
                float ry2 = rect.Y + rect.H;

                if (rx1 < bounds.X)
                    rx1 = bounds.X;

                if (rx2 > bounds.X + bounds.W)
                    rx2 = bounds.X + bounds.W;

                if (ry1 < bounds.Y)
                    ry1 = bounds.Y;

                if (ry2 > bounds.Y + bounds.H)
                    ry2 = bounds.Y + bounds.H;

                OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Blend);
                OpenTK.Graphics.OpenGL.GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);

                OpenTK.Graphics.OpenGL.GL.MatrixMode(OpenTK.Graphics.OpenGL.MatrixMode.Texture);
                OpenTK.Graphics.OpenGL.GL.PushMatrix();

                if (Math.Abs(rect.Rotation) > float.Epsilon)
                {
                    OpenTK.Graphics.OpenGL.GL.Translate(0.5f, 0.5f, 0);
                    OpenTK.Graphics.OpenGL.GL.Rotate(-rect.Rotation, 0f, 0f, 1f);
                    OpenTK.Graphics.OpenGL.GL.Translate(-0.5f, -0.5f, 0);
                }

                if (!mirrored)
                {
                    OpenTK.Graphics.OpenGL.GL.Begin(OpenTK.Graphics.OpenGL.BeginMode.Quads);

                    OpenTK.Graphics.OpenGL.GL.TexCoord2(x1, y1);
                    OpenTK.Graphics.OpenGL.GL.Vertex3(rx1, ry1, rect.Z + CGraphics.ZOffset);

                    OpenTK.Graphics.OpenGL.GL.TexCoord2(x1, y2);
                    OpenTK.Graphics.OpenGL.GL.Vertex3(rx1, ry2, rect.Z + CGraphics.ZOffset);

                    OpenTK.Graphics.OpenGL.GL.TexCoord2(x2, y2);
                    OpenTK.Graphics.OpenGL.GL.Vertex3(rx2, ry2, rect.Z + CGraphics.ZOffset);

                    OpenTK.Graphics.OpenGL.GL.TexCoord2(x2, y1);
                    OpenTK.Graphics.OpenGL.GL.Vertex3(rx2, ry1, rect.Z + CGraphics.ZOffset);

                    OpenTK.Graphics.OpenGL.GL.End();
                }
                else
                {
                    OpenTK.Graphics.OpenGL.GL.Begin(OpenTK.Graphics.OpenGL.BeginMode.Quads);

                    OpenTK.Graphics.OpenGL.GL.TexCoord2(x2, y2);
                    OpenTK.Graphics.OpenGL.GL.Vertex3(rx2, ry1, rect.Z + CGraphics.ZOffset);

                    OpenTK.Graphics.OpenGL.GL.TexCoord2(x2, y1);
                    OpenTK.Graphics.OpenGL.GL.Vertex3(rx2, ry2, rect.Z + CGraphics.ZOffset);

                    OpenTK.Graphics.OpenGL.GL.TexCoord2(x1, y1);
                    OpenTK.Graphics.OpenGL.GL.Vertex3(rx1, ry2, rect.Z + CGraphics.ZOffset);

                    OpenTK.Graphics.OpenGL.GL.TexCoord2(x1, y2);
                    OpenTK.Graphics.OpenGL.GL.Vertex3(rx1, ry1, rect.Z + CGraphics.ZOffset);

                    OpenTK.Graphics.OpenGL.GL.End();
                }

                OpenTK.Graphics.OpenGL.GL.PopMatrix();

                OpenTK.Graphics.OpenGL.GL.Disable(OpenTK.Graphics.OpenGL.EnableCap.Blend);
                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0);
            }
        }

        public void DrawTexture(STexture texture, SRectF rect, SColorF color, float begin, float end)
        {
            if (_TextureExists(ref texture))
            {
                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, texture.ID);

                OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Blend);
                OpenTK.Graphics.OpenGL.GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);


                OpenTK.Graphics.OpenGL.GL.Begin(OpenTK.Graphics.OpenGL.BeginMode.Quads);

                OpenTK.Graphics.OpenGL.GL.TexCoord2(0f + begin * texture.WidthRatio, 0f);
                OpenTK.Graphics.OpenGL.GL.Vertex3(rect.X + begin * rect.W, rect.Y, rect.Z + CGraphics.ZOffset);

                OpenTK.Graphics.OpenGL.GL.TexCoord2(0f + begin * texture.WidthRatio, texture.HeightRatio);
                OpenTK.Graphics.OpenGL.GL.Vertex3(rect.X + begin * rect.W, rect.Y + rect.H, rect.Z + CGraphics.ZOffset);

                OpenTK.Graphics.OpenGL.GL.TexCoord2(texture.WidthRatio * end, texture.HeightRatio);
                OpenTK.Graphics.OpenGL.GL.Vertex3(rect.X + end * rect.W, rect.Y + rect.H, rect.Z + CGraphics.ZOffset);

                OpenTK.Graphics.OpenGL.GL.TexCoord2(texture.WidthRatio * end, 0f);
                OpenTK.Graphics.OpenGL.GL.Vertex3(rect.X + end * rect.W, rect.Y, rect.Z + CGraphics.ZOffset);

                OpenTK.Graphics.OpenGL.GL.End();


                OpenTK.Graphics.OpenGL.GL.Disable(OpenTK.Graphics.OpenGL.EnableCap.Blend);
                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0);
            }
        }

        public void DrawTextureReflection(STexture texture, SRectF rect, SColorF color, SRectF bounds, float space, float height)
        {
            if (Math.Abs(rect.W) < float.Epsilon || Math.Abs(rect.H) < float.Epsilon || Math.Abs(bounds.H) < float.Epsilon || Math.Abs(bounds.W) < float.Epsilon ||
                Math.Abs(color.A) < float.Epsilon || height <= float.Epsilon)
                return;

            if (bounds.X > rect.X + rect.W || bounds.X + bounds.W < rect.X)
                return;

            if (bounds.Y > rect.Y + rect.H || bounds.Y + bounds.H < rect.Y)
                return;

            if (height > bounds.H)
                height = bounds.H;

            if (_TextureExists(ref texture))
            {
                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, texture.ID);

                float x1 = (bounds.X - rect.X) / rect.W * texture.WidthRatio;
                float x2 = (bounds.X + bounds.W - rect.X) / rect.W * texture.WidthRatio;
                float y1 = (bounds.Y - rect.Y + rect.H - height) / rect.H * texture.HeightRatio;
                float y2 = (bounds.Y + bounds.H - rect.Y) / rect.H * texture.HeightRatio;

                if (x1 < 0)
                    x1 = 0f;

                if (x2 > texture.WidthRatio)
                    x2 = texture.WidthRatio;

                if (y1 < 0)
                    y1 = 0f;

                if (y2 > texture.HeightRatio)
                    y2 = texture.HeightRatio;


                float rx1 = rect.X;
                float rx2 = rect.X + rect.W;
                float ry1 = rect.Y + rect.H + space;
                float ry2 = rect.Y + rect.H + space + height;

                if (rx1 < bounds.X)
                    rx1 = bounds.X;

                if (rx2 > bounds.X + bounds.W)
                    rx2 = bounds.X + bounds.W;

                if (ry1 < bounds.Y + space)
                    ry1 = bounds.Y + space;

                if (ry2 > bounds.Y + bounds.H + space + height)
                    ry2 = bounds.Y + bounds.H + space + height;

                OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Blend);

                OpenTK.Graphics.OpenGL.GL.MatrixMode(OpenTK.Graphics.OpenGL.MatrixMode.Texture);
                OpenTK.Graphics.OpenGL.GL.PushMatrix();

                if (Math.Abs(rect.Rotation) > float.Epsilon)
                {
                    OpenTK.Graphics.OpenGL.GL.Translate(0.5f, 0.5f, 0);
                    OpenTK.Graphics.OpenGL.GL.Rotate(-rect.Rotation, 0f, 0f, 1f);
                    OpenTK.Graphics.OpenGL.GL.Translate(-0.5f, -0.5f, 0);
                }


                OpenTK.Graphics.OpenGL.GL.Begin(OpenTK.Graphics.OpenGL.BeginMode.Quads);

                OpenTK.Graphics.OpenGL.GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);
                OpenTK.Graphics.OpenGL.GL.TexCoord2(x2, y2);
                OpenTK.Graphics.OpenGL.GL.Vertex3(rx2, ry1, rect.Z + CGraphics.ZOffset);

                OpenTK.Graphics.OpenGL.GL.Color4(color.R, color.G, color.B, 0f);
                OpenTK.Graphics.OpenGL.GL.TexCoord2(x2, y1);
                OpenTK.Graphics.OpenGL.GL.Vertex3(rx2, ry2, rect.Z + CGraphics.ZOffset);

                OpenTK.Graphics.OpenGL.GL.Color4(color.R, color.G, color.B, 0f);
                OpenTK.Graphics.OpenGL.GL.TexCoord2(x1, y1);
                OpenTK.Graphics.OpenGL.GL.Vertex3(rx1, ry2, rect.Z + CGraphics.ZOffset);

                OpenTK.Graphics.OpenGL.GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);
                OpenTK.Graphics.OpenGL.GL.TexCoord2(x1, y2);
                OpenTK.Graphics.OpenGL.GL.Vertex3(rx1, ry1, rect.Z + CGraphics.ZOffset);

                OpenTK.Graphics.OpenGL.GL.End();


                OpenTK.Graphics.OpenGL.GL.PopMatrix();

                OpenTK.Graphics.OpenGL.GL.Disable(OpenTK.Graphics.OpenGL.EnableCap.Blend);
                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0);
            }
        }
        #endregion drawing

        public int TextureCount()
        {
            return _Textures.Count;
        }

        private void _CheckQueque()
        {
            lock (_MutexTexture)
            {
                if (_Queque.Count == 0)
                    return;

                STextureQueque q = _Queque[0];
                STexture texture = new STexture(-1);
                if (_Textures.ContainsKey(q.ID))
                    texture = _Textures[q.ID];

                if (texture.Index < 1)
                    return;

                if (_UsePBO)
                {
                    try
                    {
                        OpenTK.Graphics.OpenGL.GL.GenBuffers(1, out texture.PBO);
                        OpenTK.Graphics.OpenGL.GL.BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, texture.PBO);
                        OpenTK.Graphics.OpenGL.GL.BufferData(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, (IntPtr)q.Data.Length, IntPtr.Zero,
                                                             OpenTK.Graphics.OpenGL.BufferUsageHint.StreamDraw);
                        OpenTK.Graphics.OpenGL.GL.BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, 0);
                    }
                    catch (Exception)
                    {
                        //throw;
                        _UsePBO = false;
                    }
                }

                int id = OpenTK.Graphics.OpenGL.GL.GenTexture();
                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, id);
                texture.ID = id;

                texture.Width = q.Width;
                texture.Height = q.Height;
                texture.W2 = MathHelper.NextPowerOfTwo(texture.Width);
                texture.H2 = MathHelper.NextPowerOfTwo(texture.Height);

                texture.WidthRatio = texture.Width / texture.W2;
                texture.HeightRatio = texture.Height / texture.H2;

                OpenTK.Graphics.OpenGL.GL.TexImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelInternalFormat.Rgba, (int)texture.W2,
                                                     (int)texture.H2, 0,
                                                     OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, IntPtr.Zero);

                OpenTK.Graphics.OpenGL.GL.TexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, q.Width, q.Height,
                                                        OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, q.Data);

                OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureWrapS,
                                                       (int)OpenTK.Graphics.OpenGL.TextureParameterName.ClampToEdge);
                OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureWrapT,
                                                       (int)OpenTK.Graphics.OpenGL.TextureParameterName.ClampToEdge);

                OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureMagFilter,
                                                       (int)OpenTK.Graphics.OpenGL.TextureMagFilter.Linear);
                OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureMinFilter,
                                                       (int)OpenTK.Graphics.OpenGL.TextureMinFilter.Linear);

                OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureMinFilter,
                                                       (int)OpenTK.Graphics.OpenGL.TextureMinFilter.LinearMipmapLinear);
                OpenTK.Graphics.OpenGL.GL.Ext.GenerateMipmap(OpenTK.Graphics.OpenGL.GenerateMipmapTarget.Texture2D);

                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0);

                // Add to Texture List
                texture.Color = new SColorF(1f, 1f, 1f, 1f);
                texture.Rect = new SRectF(0f, 0f, texture.Width, texture.Height, 0f);
                texture.TexturePath = String.Empty;

                _Textures[texture.Index] = texture;
                q.Data = null;
                _Queque.RemoveAt(0);
            }
        }
        #endregion Textures

        #endregion implementation
    }
}