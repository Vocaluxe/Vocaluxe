using System.Drawing.Drawing2D;
using System.Threading;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
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
        public Point location;
        public int width;
        public int height;
    };

    struct STextureQueque
    {
        public int ID;
        public int width;
        public int height;
        public byte[] data;
    }

    class COpenGL : Form, IDraw
    {
        #region private vars
        private readonly CKeys _Keys;
        private readonly CMouse _Mouse;
        private bool _Run;

        private readonly GLControl control;

        private SClientRect _restore;
        private bool _fullscreen;

        private readonly Dictionary<int, STexture> _Textures;
        private readonly Queue<int> _IDs;
        private readonly List<STextureQueque> _Queque;

        private readonly Object MutexTexture = new Object();

        private int h = 1;
        private int w = 1;
        private int y;
        private int x;

        private bool _UsePBO;
        #endregion private vars

        public COpenGL()
        {
            Icon = new Icon(Path.Combine(Environment.CurrentDirectory, CSettings.sIcon));

            _Textures = new Dictionary<int, STexture>();
            _Queque = new List<STextureQueque>();
            _IDs = new Queue<int>(1000000);

            for (int i = 1; i < 1000000; i++)
                _IDs.Enqueue(i);

            //Check AA Mode
            CConfig.AAMode = (EAntiAliasingModes)CheckAntiAliasingMode((int)CConfig.AAMode);

            ColorFormat cf = new ColorFormat(32);
            GraphicsMode gm;

            bool ok = false;
            try
            {
                gm = new GraphicsMode(cf, 24, 0, (int)CConfig.AAMode);
                control = new GLControl(gm, 2, 1, GraphicsContextFlags.Default);
                if (control.GraphicsMode != null)
                    ok = true;
            }
            catch (Exception)
            {
                ok = false;
            }

            if (!ok)
                control = new GLControl();

            control.MakeCurrent();
            control.VSync = (CConfig.VSync == EOffOn.TR_CONFIG_ON);

            Controls.Add(control);


            _Keys = new CKeys();
            Paint += OnPaintEvent;
            Closing += OnClosingEvent;
            Resize += OnResizeEvent;

            control.KeyDown += OnKeyDownEvent;
            control.PreviewKeyDown += OnPreviewKeyDownEvent;
            control.KeyPress += OnKeyPressEvent;
            control.KeyUp += OnKeyUpEvent;

            _Mouse = new CMouse();
            control.MouseMove += OnMouseMove;
            control.MouseWheel += OnMouseWheel;
            control.MouseDown += OnMouseDown;
            control.MouseUp += OnMouseUp;
            control.MouseLeave += OnMouseLeave;
            control.MouseEnter += OnMouseEnter;

            ClientSize = new Size(CConfig.ScreenW, CConfig.ScreenH);
            CenterToScreen();
        }

        #region Helpers
        private int CheckAntiAliasingMode(int SetValue)
        {
            int _Result = 0;
            GraphicsMode _mode = null;
            bool _done = false;

            while (!_done && (_Result <= 32))
            {
                try
                {
                    _mode = new GraphicsMode(16, 0, 0, _Result);
                }
                catch (Exception)
                {
                    _done = true;
                    _Result /= 2;
                    if (_Result == 1)
                        _Result = 0;
                }

                if (_mode != null)
                {
                    try
                    {
                        if (_mode.Samples == _Result)
                        {
                            if (_Result == 0)
                                _Result = 2;
                            else
                                _Result *= 2;
                        }
                        else
                        {
                            _done = true;
                            _Result /= 2;
                            if (_Result == 1)
                                _Result = 0;
                        }
                    }
                    catch (Exception)
                    {
                        _done = true;
                        _Result /= 2;
                        if (_Result == 1)
                            _Result = 0;
                    }
                }
                else
                {
                    _done = true;
                    _Result /= 2;
                    if (_Result == 1)
                        _Result = 0;
                }
            }

            if (_Result > 64)
                _Result = 32;

            if (SetValue < _Result)
                return SetValue;
            else
                return _Result;
        }

        private int CheckColorDeep(int SetValue)
        {
            int _Result = 8;
            GraphicsMode _mode = null;
            bool _done = false;

            while (!_done && (_Result <= 32))
            {
                try
                {
                    _mode = new GraphicsMode(_Result, 0, 0, 0);
                }
                catch (Exception)
                {
                    _done = true;
                    _Result -= 8;
                    if (_Result == 0)
                        _Result = 8;
                }

                if (_mode != null)
                {
                    try
                    {
                        if (_mode.ColorFormat == _Result)
                            _Result += 8;
                        else
                        {
                            _done = true;
                            _Result -= 8;
                            if (_Result == 0)
                                _Result = 8;
                        }
                    }
                    catch (Exception)
                    {
                        _done = true;
                        _Result -= 8;
                        if (_Result == 0)
                            _Result = 8;
                    }
                }
                else
                {
                    _done = true;
                    _Result -= 8;
                    if (_Result == 0)
                        _Result = 8;
                }
            }

            if (_Result > 32)
                _Result = 32;

            if (SetValue < _Result)
                return SetValue;
            else
                return _Result;
        }

        private void ToggleFullScreen()
        {
            if (!_fullscreen)
                EnterFullScreen();
            else
                LeaveFullScreen();
        }

        private void EnterFullScreen()
        {
            _fullscreen = true;
            CConfig.FullScreen = EOffOn.TR_CONFIG_ON;

            _restore.location = Location;
            _restore.width = Width;
            _restore.height = Height;

            FormBorderStyle = FormBorderStyle.None;

            int ScreenNr = 0;
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                Screen scr = Screen.AllScreens[i];
                if (scr.Bounds.Top <= Top && scr.Bounds.Left <= Left)
                    ScreenNr = i;
            }

            DesktopBounds = new Rectangle(Screen.AllScreens[ScreenNr].Bounds.Location,
                                          new Size(Screen.AllScreens[ScreenNr].Bounds.Width, Screen.AllScreens[ScreenNr].Bounds.Height));

            if (WindowState == FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Normal;
                RResize();
                WindowState = FormWindowState.Maximized;
            }
            else
                RResize();

            CConfig.SaveConfig();
        }

        private void LeaveFullScreen()
        {
            _fullscreen = false;
            CConfig.FullScreen = EOffOn.TR_CONFIG_OFF;

            FormBorderStyle = FormBorderStyle.Sizable;
            DesktopBounds = new Rectangle(_restore.location, new Size(_restore.width, _restore.height));

            CConfig.SaveConfig();
        }
        #endregion Helpers

        #region form events
        private void OnPaintEvent(object sender, PaintEventArgs e) {}

        private void OnResizeEvent(object sender, EventArgs e) {}

        private void OnClosingEvent(object sender, CancelEventArgs e)
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

            control.ClientSize = ClientSize;
            RResize();
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
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            _Mouse.MouseMove(e);
        }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            _Mouse.MouseWheel(e);
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            _Mouse.MouseDown(e);
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            _Mouse.MouseUp(e);
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            _Mouse.Visible = false;
            Cursor.Show();
        }

        private void OnMouseEnter(object sender, EventArgs e)
        {
            Cursor.Hide();
            _Mouse.Visible = true;
        }
        #endregion

        #region keyboard event handlers
        private void OnPreviewKeyDownEvent(object sender, PreviewKeyDownEventArgs e)
        {
            OnKeyDownEvent(sender, new KeyEventArgs(e.KeyData));
        }

        private void OnKeyDownEvent(object sender, KeyEventArgs e)
        {
            _Keys.KeyDown(e);
        }

        private void OnKeyPressEvent(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            _Keys.KeyPress(e);
        }

        private void OnKeyUpEvent(object sender, KeyEventArgs e)
        {
            _Keys.KeyUp(e);
        }
        #endregion keyboard event handlers

        private void RResize()
        {
            h = control.Height;
            w = control.Width;
            y = 0;
            x = 0;


            if (w / (float)h > CSettings.GetRenderAspect())
            {
                w = (int)Math.Round(h * CSettings.GetRenderAspect());
                x = (control.Width - w) / 2;
            }
            else
            {
                h = (int)Math.Round(w / CSettings.GetRenderAspect());
                y = (control.Height - h) / 2;
            }

            OpenTK.Graphics.OpenGL.GL.MatrixMode(OpenTK.Graphics.OpenGL.MatrixMode.Projection);
            OpenTK.Graphics.OpenGL.GL.LoadIdentity();
            OpenTK.Graphics.OpenGL.GL.Ortho(0, CSettings.iRenderW, CSettings.iRenderH, 0, CSettings.zNear, CSettings.zFar);
            OpenTK.Graphics.OpenGL.GL.Viewport(x, y, w, h);
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

            OpenTK.Graphics.OpenGL.GL.DepthRange(CSettings.zFar, CSettings.zNear);
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
                CSettings.bFullScreen = true;
                EnterFullScreen();
            }

            while (_Run)
            {
                Application.DoEvents();

                if (_Run)
                {
                    ClearScreen();
                    _Run = _Run && CGraphics.Draw();

                    _Run = CGraphics.UpdateGameLogic(_Keys, _Mouse);
                    control.SwapBuffers();

                    if ((CSettings.bFullScreen && !_fullscreen) || (!CSettings.bFullScreen && _fullscreen))
                        ToggleFullScreen();

                    CheckQueque();

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
            return control.Width;
        }

        public int GetScreenHeight()
        {
            return control.Height;
        }

        public RectangleF GetTextBounds(CText text)
        {
            return GetTextBounds(text, text.Height);
        }

        public RectangleF GetTextBounds(CText text, float Height)
        {
            CFonts.Height = Height;
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

            texture.width = w;
            texture.height = h;
            texture.w2 = MathHelper.NextPowerOfTwo(texture.width);
            texture.h2 = MathHelper.NextPowerOfTwo(texture.height);

            texture.width_ratio = texture.width / texture.w2;
            texture.height_ratio = texture.height / texture.h2;

            OpenTK.Graphics.OpenGL.GL.TexImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelInternalFormat.Rgba, (int)texture.w2,
                                                 (int)texture.h2, 0,
                                                 OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, IntPtr.Zero);

            OpenTK.Graphics.OpenGL.GL.CopyTexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, x, y,
                                                        (int)texture.width, (int)texture.height);

            OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureMagFilter,
                                                   (int)OpenTK.Graphics.OpenGL.TextureMagFilter.Linear);
            OpenTK.Graphics.OpenGL.GL.TexParameter(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL.TextureParameterName.TextureMinFilter,
                                                   (int)OpenTK.Graphics.OpenGL.TextureMinFilter.Linear);

            OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0);

            // Add to Texture List
            texture.color = new SColorF(1f, 1f, 1f, 1f);
            texture.rect = new SRectF(0f, 0f, texture.width, texture.height, 0f);

            lock (MutexTexture)
            {
                texture.index = _IDs.Dequeue();
                _Textures[texture.index] = texture;
            }

            return texture;
        }

        public void CopyScreen(ref STexture Texture)
        {
            if (!_TextureExists(ref Texture) || (Texture.width != GetScreenWidth()) || (Texture.height != GetScreenHeight()))
            {
                RemoveTexture(ref Texture);
                Texture = CopyScreen();
            }
            else
            {
                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, Texture.ID);

                OpenTK.Graphics.OpenGL.GL.CopyTexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, 0, 0,
                                                            (int)Texture.width, (int)Texture.height);

                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0);
            }
        }

        public void MakeScreenShot()
        {
            string file = "Screenshot_";
            string path = Path.Combine(Environment.CurrentDirectory, CSettings.sFolderScreenshots);

            int i = 0;
            while (File.Exists(Path.Combine(path, file + i.ToString("00000") + ".bmp")))
                i++;

            int width = GetScreenWidth();
            int height = GetScreenHeight();

            using (Bitmap screen = new Bitmap(width, height))
            {
                BitmapData bmp_data = screen.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                OpenTK.Graphics.OpenGL.GL.ReadPixels(0, 0, width, height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, bmp_data.Scan0);
                screen.UnlockBits(bmp_data);

                screen.RotateFlip(RotateFlipType.RotateNoneFlipY);
                screen.Save(Path.Combine(path, file + i.ToString("00000") + ".bmp"), ImageFormat.Bmp);
            }
        }

        public void DrawLine(int a, int r, int g, int b, int w, int x1, int y1, int x2, int y2) {}

        // Draw Basic Text (must be deleted later)
        public void DrawText(string Text, int x, int y, int h)
        {
            DrawText(Text, x, y, h, 0f);
        }

        public void DrawText(string Text, int x, int y, float h, float z)
        {
            CFonts.DrawText(Text, h, x, y, z, new SColorF(1, 1, 1, 1));
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

            if (rect.Rotation != 0f)
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
        public STexture AddTexture(string TexturePath)
        {
            if (File.Exists(TexturePath))
            {
                Bitmap bmp;
                try
                {
                    bmp = new Bitmap(TexturePath);
                }
                catch (Exception)
                {
                    CLog.LogError("Error loading Texture: " + TexturePath);
                    return new STexture(-1);
                }
                try
                {
                    return AddTexture(bmp, TexturePath);
                }
                finally
                {
                    bmp.Dispose();
                }
            }
            CLog.LogError("Can't find File: " + TexturePath);
            return new STexture(-1);
        }

        public STexture AddTexture(Bitmap bmp)
        {
            return AddTexture(bmp, String.Empty);
        }

        public STexture AddTexture(Bitmap bmp, string TexturePath)
        {
            STexture texture = new STexture(-1);

            if (bmp.Height == 0 || bmp.Width == 0)
                return texture;

            int MaxSize;
            switch (CConfig.TextureQuality)
            {
                case ETextureQuality.TR_CONFIG_TEXTURE_LOWEST:
                    MaxSize = 128;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_LOW:
                    MaxSize = 256;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_MEDIUM:
                    MaxSize = 512;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_HIGH:
                    MaxSize = 1024;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_HIGHEST:
                    MaxSize = 2048;
                    break;
                default:
                    MaxSize = 512;
                    break;
            }

            int w = bmp.Width;
            int h = bmp.Height;

            if (w > MaxSize)
            {
                h = (int)Math.Round((float)MaxSize / bmp.Width * bmp.Height);
                w = MaxSize;
            }

            if (h > MaxSize)
            {
                w = (int)Math.Round((float)MaxSize / bmp.Height * bmp.Width);
                h = MaxSize;
            }

            int id = OpenTK.Graphics.OpenGL.GL.GenTexture();
            OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, id);
            texture.ID = id;

            texture.width = w;
            texture.height = h;

            texture.w2 = MathHelper.NextPowerOfTwo(w);
            texture.h2 = MathHelper.NextPowerOfTwo(h);

            using (Bitmap bmp2 = new Bitmap((int)texture.w2, (int)texture.h2))
            {
                Graphics g = Graphics.FromImage(bmp2);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(bmp, new Rectangle(0, 0, bmp2.Width, bmp2.Height));
                g.Dispose();

                texture.width_ratio = 1f;
                texture.height_ratio = 1f;

                BitmapData bmp_data = bmp2.LockBits(new Rectangle(0, 0, bmp2.Width, bmp2.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);


                OpenTK.Graphics.OpenGL.GL.TexImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelInternalFormat.Rgba, (int)texture.w2,
                                                     (int)texture.h2, 0,
                                                     OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, IntPtr.Zero);

                OpenTK.Graphics.OpenGL.GL.TexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, bmp_data.Width, bmp_data.Height,
                                                        OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, bmp_data.Scan0);

                bmp2.UnlockBits(bmp_data);
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
            texture.color = new SColorF(1f, 1f, 1f, 1f);
            texture.rect = new SRectF(0f, 0f, texture.width, texture.height, 0f);
            texture.TexturePath = String.Empty;

            lock (MutexTexture)
            {
                texture.index = _IDs.Dequeue();
                _Textures[texture.index] = texture;
            }

            return texture;
        }

        public STexture AddTexture(int W, int H, IntPtr Data)
        {
            STexture texture = new STexture(-1);

            if (_UsePBO)
            {
                try
                {
                    OpenTK.Graphics.OpenGL.GL.GenBuffers(1, out texture.PBO);
                    OpenTK.Graphics.OpenGL.GL.BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, texture.PBO);
                    OpenTK.Graphics.OpenGL.GL.BufferData(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, (IntPtr)(W * H * 4), IntPtr.Zero,
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

            texture.width = W;
            texture.height = H;
            texture.w2 = MathHelper.NextPowerOfTwo(texture.width);
            texture.h2 = MathHelper.NextPowerOfTwo(texture.height);

            texture.width_ratio = texture.width / texture.w2;
            texture.height_ratio = texture.height / texture.h2;

            OpenTK.Graphics.OpenGL.GL.TexImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelInternalFormat.Rgba, (int)texture.w2,
                                                 (int)texture.h2, 0,
                                                 OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, IntPtr.Zero);

            OpenTK.Graphics.OpenGL.GL.TexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, W, H,
                                                    OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, Data);


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
            texture.color = new SColorF(1f, 1f, 1f, 1f);
            texture.rect = new SRectF(0f, 0f, texture.width, texture.height, 0f);
            texture.TexturePath = String.Empty;

            lock (MutexTexture)
            {
                texture.index = _IDs.Dequeue();
                _Textures[texture.index] = texture;
            }

            return texture;
        }

        public STexture AddTexture(int W, int H, ref byte[] Data)
        {
            STexture texture = new STexture(-1);

            if (_UsePBO)
            {
                try
                {
                    OpenTK.Graphics.OpenGL.GL.GenBuffers(1, out texture.PBO);
                    OpenTK.Graphics.OpenGL.GL.BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, texture.PBO);
                    OpenTK.Graphics.OpenGL.GL.BufferData(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, (IntPtr)Data.Length, IntPtr.Zero,
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

            texture.width = W;
            texture.height = H;
            texture.w2 = MathHelper.NextPowerOfTwo(texture.width);
            texture.h2 = MathHelper.NextPowerOfTwo(texture.height);

            texture.width_ratio = texture.width / texture.w2;
            texture.height_ratio = texture.height / texture.h2;

            OpenTK.Graphics.OpenGL.GL.TexImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelInternalFormat.Rgba, (int)texture.w2,
                                                 (int)texture.h2, 0,
                                                 OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, IntPtr.Zero);

            OpenTK.Graphics.OpenGL.GL.TexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, W, H,
                                                    OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, Data);


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
            texture.color = new SColorF(1f, 1f, 1f, 1f);
            texture.rect = new SRectF(0f, 0f, texture.width, texture.height, 0f);
            texture.TexturePath = String.Empty;

            lock (MutexTexture)
            {
                texture.index = _IDs.Dequeue();
                _Textures[texture.index] = texture;
            }

            return texture;
        }

        public STexture QuequeTexture(int W, int H, ref byte[] Data)
        {
            STexture texture = new STexture(-1);
            STextureQueque queque = new STextureQueque();

            queque.data = Data;
            queque.height = H;
            queque.width = W;
            texture.height = H;
            texture.width = W;

            lock (MutexTexture)
            {
                texture.index = _IDs.Dequeue();
                queque.ID = texture.index;
                _Queque.Add(queque);
                _Textures[texture.index] = texture;
            }

            return texture;
        }
        #endregion adding

        #region updating
        public bool UpdateTexture(ref STexture Texture, IntPtr Data)
        {
            if (_TextureExists(ref Texture))
            {
                if (_UsePBO)
                {
                    try
                    {
                        OpenTK.Graphics.OpenGL.GL.BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, Texture.PBO);

                        IntPtr Buffer = OpenTK.Graphics.OpenGL.GL.MapBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, OpenTK.Graphics.OpenGL.BufferAccess.WriteOnly);
                        byte[] d = new byte[(int)Texture.height * (int)Texture.width * 4];

                        Marshal.Copy(Data, d, 0, (int)Texture.height * (int)Texture.width * 4);
                        Marshal.Copy(d, 0, Buffer, (int)Texture.height * (int)Texture.width * 4);

                        OpenTK.Graphics.OpenGL.GL.UnmapBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer);

                        OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, Texture.ID);
                        OpenTK.Graphics.OpenGL.GL.TexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, (int)Texture.width, (int)Texture.height,
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

                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, Texture.ID);

                OpenTK.Graphics.OpenGL.GL.TexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, (int)Texture.width, (int)Texture.height,
                                                        OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, Data);

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

        public bool UpdateTexture(ref STexture Texture, ref byte[] Data)
        {
            if (_TextureExists(ref Texture))
            {
                if (_UsePBO)
                {
                    try
                    {
                        OpenTK.Graphics.OpenGL.GL.BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, Texture.PBO);

                        IntPtr Buffer = OpenTK.Graphics.OpenGL.GL.MapBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, OpenTK.Graphics.OpenGL.BufferAccess.WriteOnly);
                        Marshal.Copy(Data, 0, Buffer, Data.Length);

                        OpenTK.Graphics.OpenGL.GL.UnmapBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer);

                        OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, Texture.ID);
                        OpenTK.Graphics.OpenGL.GL.TexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, (int)Texture.width, (int)Texture.height,
                                                                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, IntPtr.Zero);

                        OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0);
                        OpenTK.Graphics.OpenGL.GL.BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, 0);

                        return true;
                    }
                    catch (Exception)
                    {
                        throw;
                        //_UsePBO = false;
                    }
                }

                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, Texture.ID);

                OpenTK.Graphics.OpenGL.GL.TexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, (int)Texture.width, (int)Texture.height,
                                                        OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, Data);

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

        public void RemoveTexture(ref STexture Texture)
        {
            if ((Texture.index > 0) && (_Textures.Count > 0))
            {
                lock (MutexTexture)
                {
                    _IDs.Enqueue(Texture.index);
                    OpenTK.Graphics.OpenGL.GL.DeleteTexture(Texture.ID);
                    if (Texture.PBO > 0)
                        OpenTK.Graphics.OpenGL.GL.DeleteBuffers(1, ref Texture.PBO);
                    _Textures.Remove(Texture.index);
                    Texture.index = -1;
                    Texture.ID = -1;
                }
            }
        }

        private bool _TextureExists(ref STexture Texture)
        {
            lock (MutexTexture)
            {
                if (_Textures.ContainsKey((Texture.index)))
                {
                    if (_Textures[Texture.index].ID > 0)
                    {
                        Texture = _Textures[Texture.index];
                        return true;
                    }
                }
            }
            return false;
        }

        #region drawing
        public void DrawTexture(STexture Texture)
        {
            DrawTexture(Texture, Texture.rect, Texture.color);
        }

        public void DrawTexture(STexture Texture, SRectF rect)
        {
            DrawTexture(Texture, rect, Texture.color, false);
        }

        public void DrawTexture(STexture Texture, SRectF rect, SColorF color)
        {
            DrawTexture(Texture, rect, color, false);
        }

        public void DrawTexture(STexture Texture, SRectF rect, SColorF color, SRectF bounds)
        {
            DrawTexture(Texture, rect, color, bounds, false);
        }

        public void DrawTexture(STexture Texture, SRectF rect, SColorF color, bool mirrored)
        {
            DrawTexture(Texture, rect, color, new SRectF(0, 0, CSettings.iRenderW, CSettings.iRenderH, rect.Z), mirrored);
        }

        public void DrawTexture(STexture Texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored)
        {
            if (rect.W == 0f || rect.H == 0f || bounds.H == 0f || bounds.W == 0f || color.A == 0f)
                return;

            if (bounds.X > rect.X + rect.W || bounds.X + bounds.W < rect.X)
                return;

            if (bounds.Y > rect.Y + rect.H || bounds.Y + bounds.H < rect.Y)
                return;

            if (_TextureExists(ref Texture))
            {
                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, Texture.ID);

                float x1 = (bounds.X - rect.X) / rect.W * Texture.width_ratio;
                float x2 = (bounds.X + bounds.W - rect.X) / rect.W * Texture.width_ratio;
                float y1 = (bounds.Y - rect.Y) / rect.H * Texture.height_ratio;
                float y2 = (bounds.Y + bounds.H - rect.Y) / rect.H * Texture.height_ratio;

                if (x1 < 0)
                    x1 = 0f;

                if (x2 > Texture.width_ratio)
                    x2 = Texture.width_ratio;

                if (y1 < 0)
                    y1 = 0f;

                if (y2 > Texture.height_ratio)
                    y2 = Texture.height_ratio;


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

                if (rect.Rotation != 0f)
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

        public void DrawTexture(STexture Texture, SRectF rect, SColorF color, float begin, float end)
        {
            if (_TextureExists(ref Texture))
            {
                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, Texture.ID);

                OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Blend);
                OpenTK.Graphics.OpenGL.GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);


                OpenTK.Graphics.OpenGL.GL.Begin(OpenTK.Graphics.OpenGL.BeginMode.Quads);

                OpenTK.Graphics.OpenGL.GL.TexCoord2(0f + begin * Texture.width_ratio, 0f);
                OpenTK.Graphics.OpenGL.GL.Vertex3(rect.X + begin * rect.W, rect.Y, rect.Z + CGraphics.ZOffset);

                OpenTK.Graphics.OpenGL.GL.TexCoord2(0f + begin * Texture.width_ratio, Texture.height_ratio);
                OpenTK.Graphics.OpenGL.GL.Vertex3(rect.X + begin * rect.W, rect.Y + rect.H, rect.Z + CGraphics.ZOffset);

                OpenTK.Graphics.OpenGL.GL.TexCoord2(Texture.width_ratio * end, Texture.height_ratio);
                OpenTK.Graphics.OpenGL.GL.Vertex3(rect.X + end * rect.W, rect.Y + rect.H, rect.Z + CGraphics.ZOffset);

                OpenTK.Graphics.OpenGL.GL.TexCoord2(Texture.width_ratio * end, 0f);
                OpenTK.Graphics.OpenGL.GL.Vertex3(rect.X + end * rect.W, rect.Y, rect.Z + CGraphics.ZOffset);

                OpenTK.Graphics.OpenGL.GL.End();


                OpenTK.Graphics.OpenGL.GL.Disable(OpenTK.Graphics.OpenGL.EnableCap.Blend);
                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0);
            }
        }

        public void DrawTextureReflection(STexture Texture, SRectF rect, SColorF color, SRectF bounds, float space, float height)
        {
            if (rect.W == 0f || rect.H == 0f || bounds.H == 0f || bounds.W == 0f || color.A == 0f || height <= 0f)
                return;

            if (bounds.X > rect.X + rect.W || bounds.X + bounds.W < rect.X)
                return;

            if (bounds.Y > rect.Y + rect.H || bounds.Y + bounds.H < rect.Y)
                return;

            if (height > bounds.H)
                height = bounds.H;

            if (_TextureExists(ref Texture))
            {
                OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, Texture.ID);

                float x1 = (bounds.X - rect.X) / rect.W * Texture.width_ratio;
                float x2 = (bounds.X + bounds.W - rect.X) / rect.W * Texture.width_ratio;
                float y1 = (bounds.Y - rect.Y + rect.H - height) / rect.H * Texture.height_ratio;
                float y2 = (bounds.Y + bounds.H - rect.Y) / rect.H * Texture.height_ratio;

                if (x1 < 0)
                    x1 = 0f;

                if (x2 > Texture.width_ratio)
                    x2 = Texture.width_ratio;

                if (y1 < 0)
                    y1 = 0f;

                if (y2 > Texture.height_ratio)
                    y2 = Texture.height_ratio;


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

                if (rect.Rotation != 0f)
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

        private void CheckQueque()
        {
            lock (MutexTexture)
            {
                if (_Queque.Count == 0)
                    return;

                STextureQueque q = _Queque[0];
                STexture texture = new STexture(-1);
                if (_Textures.ContainsKey(q.ID))
                    texture = _Textures[q.ID];

                if (texture.index < 1)
                    return;

                if (_UsePBO)
                {
                    try
                    {
                        OpenTK.Graphics.OpenGL.GL.GenBuffers(1, out texture.PBO);
                        OpenTK.Graphics.OpenGL.GL.BindBuffer(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, texture.PBO);
                        OpenTK.Graphics.OpenGL.GL.BufferData(OpenTK.Graphics.OpenGL.BufferTarget.PixelUnpackBuffer, (IntPtr)q.data.Length, IntPtr.Zero,
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

                texture.width = q.width;
                texture.height = q.height;
                texture.w2 = MathHelper.NextPowerOfTwo(texture.width);
                texture.h2 = MathHelper.NextPowerOfTwo(texture.height);

                texture.width_ratio = texture.width / texture.w2;
                texture.height_ratio = texture.height / texture.h2;

                OpenTK.Graphics.OpenGL.GL.TexImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelInternalFormat.Rgba, (int)texture.w2,
                                                     (int)texture.h2, 0,
                                                     OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, IntPtr.Zero);

                OpenTK.Graphics.OpenGL.GL.TexSubImage2D(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0, 0, 0, q.width, q.height,
                                                        OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, q.data);

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
                texture.color = new SColorF(1f, 1f, 1f, 1f);
                texture.rect = new SRectF(0f, 0f, texture.width, texture.height, 0f);
                texture.TexturePath = String.Empty;

                _Textures[texture.index] = texture;
                q.data = null;
                _Queque.RemoveAt(0);
            }
        }
        #endregion Textures

        #endregion implementation
    }
}