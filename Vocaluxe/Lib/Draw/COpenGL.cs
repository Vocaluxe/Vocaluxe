﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using Vocaluxe.Base;
using Vocaluxe.Menu;

namespace Vocaluxe.Lib.Draw
{
    public struct SClientRect
    {
        public Point location;
        public int width;
        public int height;
    };
       

    class COpenGL : Form, IDraw
    {
        #region private vars

        private CKeys _Keys;
        private CMouse _Mouse;
        private bool _Run;
                        
        GLControl control = null;

        private SClientRect _restore;
        private bool _fullscreen = false;

        private List<STexture> _Textures;

        private int h = 1;
        private int w = 1;
        private int y = 0;
        private int x = 0;

        private bool _UsePBO = false;
        
        #endregion private vars

        public COpenGL()
        {
            this.Icon = new System.Drawing.Icon(Path.Combine(System.Environment.CurrentDirectory, CSettings.sIcon));

            _Textures = new List<STexture>();

            //Check AA Mode
            CConfig.AAMode = (EAntiAliasingModes)CheckAntiAliasingMode((int)CConfig.AAMode);

            OpenTK.Graphics.ColorFormat cf = new OpenTK.Graphics.ColorFormat(32);
            OpenTK.Graphics.GraphicsMode gm;

            bool ok = false;
            try
            {
                gm = new OpenTK.Graphics.GraphicsMode(cf, 24, 0, (int)CConfig.AAMode);
                control = new GLControl(gm, 2, 1, OpenTK.Graphics.GraphicsContextFlags.Default);
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

            this.Controls.Add(control);
                       
            
            _Keys = new CKeys();
            this.Paint += new PaintEventHandler(this.OnPaintEvent);
            this.Closing += new CancelEventHandler(this.OnClosingEvent);
            this.Resize += new EventHandler(this.OnResizeEvent);
            
            control.KeyDown += new KeyEventHandler(this.OnKeyDownEvent);
            control.PreviewKeyDown += new PreviewKeyDownEventHandler(this.OnPreviewKeyDownEvent);
            control.KeyPress += new KeyPressEventHandler(this.OnKeyPressEvent);
            control.KeyUp += new KeyEventHandler(this.OnKeyUpEvent);
            
            _Mouse = new CMouse();
            control.MouseMove += new MouseEventHandler(this.OnMouseMove);
            control.MouseWheel += new MouseEventHandler(this.OnMouseWheel);
            control.MouseDown += new MouseEventHandler(this.OnMouseDown);
            control.MouseUp += new MouseEventHandler(this.OnMouseUp);
            control.MouseLeave += new EventHandler(this.OnMouseLeave);
            control.MouseEnter += new EventHandler(this.OnMouseEnter);            

            this.ClientSize = new Size(CConfig.ScreenW, CConfig.ScreenH);
            //this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);
            this.CenterToScreen();
        }

        #region Helpers
        private int CheckAntiAliasingMode(int SetValue)
        {
            int _Result = 0;
            OpenTK.Graphics.GraphicsMode _mode = null;
            bool _done = false;

            while (!_done && (_Result <= 32))
            {
                try
                {
                    _mode = new OpenTK.Graphics.GraphicsMode(16, 0, 0, _Result);
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
            OpenTK.Graphics.GraphicsMode _mode = null;
            bool _done = false;

            while (!_done && (_Result <= 32))
            {
                try
                {
                    _mode = new OpenTK.Graphics.GraphicsMode(_Result, 0, 0, 0);
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
                        {
                            _Result += 8;
                        }
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

            _restore.location = this.Location;
            _restore.width = this.Width;
            _restore.height = this.Height;

            this.FormBorderStyle = FormBorderStyle.None;

            int ScreenNr = 0;
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                Screen scr = Screen.AllScreens[i];
                if (scr.Bounds.Top <= this.Top && scr.Bounds.Left <= this.Left)
                    ScreenNr = i;
            }

            this.DesktopBounds = new Rectangle(Screen.AllScreens[ScreenNr].Bounds.Location,
                new Size(Screen.AllScreens[ScreenNr].Bounds.Width, Screen.AllScreens[ScreenNr].Bounds.Height));
            this.TopMost = true;
            this.Show();

            CConfig.SaveConfig();
        }

        private void LeaveFullScreen()
        {
            _fullscreen = false;
            CConfig.FullScreen = EOffOn.TR_CONFIG_OFF;

            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.DesktopBounds = new Rectangle(_restore.location, new Size(_restore.width, _restore.height));

            this.TopMost = false;

            CConfig.SaveConfig();
        }
        #endregion Helpers

        #region form events
        private void OnPaintEvent(object sender, PaintEventArgs e)
        {

        }

        private void OnResizeEvent(object sender, EventArgs e)
        {
            
        }

        private void OnClosingEvent(object sender, CancelEventArgs e)
        {
            _Run = false;
        }

        protected override void OnLoad(EventArgs e)
        {
            GL.ClearColor(Color.Black);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            control.ClientSize = this.ClientSize;
            RResize();
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
        private void OnPreviewKeyDownEvent(object sender, System.Windows.Forms.PreviewKeyDownEventArgs e)
		{
			OnKeyDownEvent(sender, new KeyEventArgs(e.KeyData));
		}
		
        private void OnKeyDownEvent(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            _Keys.KeyDown(e);
        }

        		
        private void OnKeyPressEvent(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            _Keys.KeyPress(e);
        }

        private void OnKeyUpEvent(object sender, System.Windows.Forms.KeyEventArgs e)
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


            if ((float)w / (float)h > CSettings.GetRenderAspect())
            {
                w = (int)Math.Round((float)h * CSettings.GetRenderAspect());
                x = (control.Width - w) / 2;
            }
            else
            {
                h = (int)Math.Round((float)w / CSettings.GetRenderAspect());
                y = (control.Height - h) / 2;
            }
            
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, CSettings.iRenderW, CSettings.iRenderH, 0, (double)CSettings.zNear, (double)CSettings.zFar);
            GL.Viewport(x, y, w, h);
        }


        #region implementation

        public bool Init()
        {
            this.Text = CSettings.GetFullVersionText();
            
            // Init Texturing
            GL.Enable(EnableCap.Texture2D);
            
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            GL.DepthRange(CSettings.zFar, CSettings.zNear);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Enable(EnableCap.DepthTest);

            return true;
        }

        public void MainLoop()
        {
            _Run = true;
            int delay = 0;
            this.Show();

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

                    if (CTime.IsRunning())
                        delay = (int)Math.Floor(CConfig.CalcCycleTime() - CTime.GetMilliseconds());
                                                           
                    if (delay >= 1)
                        System.Threading.Thread.Sleep(delay);

                    CTime.CalculateFPS();
                    CTime.Restart();
                }
                else
                    this.Close();
            }
        }

        public bool Unload()
        {
            while (_Textures.Count > 0)
            {
                STexture tex = _Textures[0];
                RemoveTexture(ref tex);
            }
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
            CFonts.SetFont(text.Fon);
            CFonts.Style = text.Style;
            return new RectangleF(text.X, text.Y, CFonts.GetTextWidth(CLanguage.Translate(text.Text)), CFonts.GetTextHeight(text.Text));
        }
        
        #region Basic Draw Methods
        public void ClearScreen()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        public STexture CopyScreen()
        {
            STexture texture = new STexture(-1);

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);
            texture.index = id;

            texture.width = w;
            texture.height = h;
            texture.w2 = (float)MathHelper.NextPowerOfTwo(texture.width);
            texture.h2 = (float)MathHelper.NextPowerOfTwo(texture.height);

            texture.width_ratio = texture.width / texture.w2;
            texture.height_ratio = texture.height / texture.h2;

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, (int)texture.w2, (int)texture.h2, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

            GL.CopyTexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, x, y,
                (int)texture.width, (int)texture.height);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

            GL.BindTexture(TextureTarget.Texture2D, 0);

            // Add to Texture List
            texture.color = new SColorF(1f, 1f, 1f, 1f);
            texture.rect = new SRectF(0f, 0f, texture.width, texture.height, 0f);
            _Textures.Add(texture);

            return texture;
        }

        public void CopyScreen(ref STexture Texture)
        {
            if (!_TextureExists(Texture) || (Texture.width != GetScreenWidth()) || (Texture.height != GetScreenHeight()))
            {
                RemoveTexture(ref Texture);
                Texture = CopyScreen();
            }
            else
            {
                GL.BindTexture(TextureTarget.Texture2D, Texture.index);

                GL.CopyTexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 0, 0,
                    (int)Texture.width, (int)Texture.height);

                GL.BindTexture(TextureTarget.Texture2D, 0);
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

            Bitmap screen = new Bitmap(width, height);


            BitmapData bmp_data = screen.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.ReadPixels(0, 0, width, height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmp_data.Scan0);
            screen.UnlockBits(bmp_data);

            screen.RotateFlip(RotateFlipType.RotateNoneFlipY);
            screen.Save(Path.Combine(path, file + i.ToString("00000") + ".bmp"), ImageFormat.Bmp);
            screen.Dispose();
        }

        public void DrawLine(int a, int r, int g, int b, int w, int x1, int y1, int x2, int y2)
        {

        }

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
            GL.Enable(EnableCap.Blend);
            GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);

            GL.Begin(BeginMode.Quads);
            GL.Vertex3(rect.X, rect.Y, rect.Z + CGraphics.ZOffset);
            GL.Vertex3(rect.X, rect.Y + rect.H, rect.Z + CGraphics.ZOffset);
            GL.Vertex3(rect.X + rect.W, rect.Y + rect.H, rect.Z + CGraphics.ZOffset);
            GL.Vertex3(rect.X + rect.W, rect.Y, rect.Z + CGraphics.ZOffset);
            GL.End();

            GL.Disable(EnableCap.Blend);
        }

        #endregion Basic Draw Methods

        #region Textures
        public STexture AddTexture(string TexturePath)
        {
            if (System.IO.File.Exists(TexturePath))
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
                return AddTexture(bmp, TexturePath);
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

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);
            texture.index = id;

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

            if (h >= w && w > MaxSize)
            {
                h = (int)Math.Round((float)MaxSize / bmp.Width * bmp.Height);
                w = MaxSize;
            }

            if (w >= h && h > MaxSize)
            {
                w = (int)Math.Round((float)MaxSize / bmp.Height * bmp.Width);
                h = MaxSize;
            }

            texture.width = w;
            texture.height = h;

            texture.w2 = (float)MathHelper.NextPowerOfTwo(w);
            texture.h2 = (float)MathHelper.NextPowerOfTwo(h);

            Bitmap bmp2 = new Bitmap((int)texture.w2, (int)texture.h2);
            Graphics g = Graphics.FromImage(bmp2);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.DrawImage(bmp, new Rectangle(0, 0, bmp2.Width, bmp2.Height));
            
            texture.width_ratio = 1f;
            texture.height_ratio = 1f;

            BitmapData bmp_data = bmp2.LockBits(new Rectangle(0, 0, bmp2.Width, bmp2.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);


            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, (int)texture.w2, (int)texture.h2, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, bmp_data.Width, bmp_data.Height,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmp_data.Scan0);

            bmp2.UnlockBits(bmp_data);
            bmp2.Dispose();

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureParameterName.ClampToEdge);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                    //GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.Ext.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                

            // Add to Texture List
            texture.color = new SColorF(1f, 1f, 1f, 1f);
            texture.rect = new SRectF(0f, 0f, texture.width, texture.height, 0f);
            texture.TexturePath = String.Empty;
            _Textures.Add(texture);

            return texture;
        }

        public STexture AddTexture(int W, int H, IntPtr Data)
        {
            STexture texture = new STexture(-1);

            if (_UsePBO)
            {
                try
                {
                    GL.GenBuffers(1, out texture.PBO);
                    GL.BindBuffer(BufferTarget.PixelUnpackBuffer, texture.PBO);
                    GL.BufferData(BufferTarget.PixelUnpackBuffer, (IntPtr)(W * H * 4), IntPtr.Zero, BufferUsageHint.StreamDraw);
                    GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
                }
                catch (Exception)
                {
                    _UsePBO = false;
                }
            }

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);
            texture.index = id;

            texture.width = W;
            texture.height = H;
            texture.w2 = (float)MathHelper.NextPowerOfTwo(texture.width);
            texture.h2 = (float)MathHelper.NextPowerOfTwo(texture.height);

            texture.width_ratio = texture.width / texture.w2;
            texture.height_ratio = texture.height / texture.h2;

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, (int)texture.w2, (int)texture.h2, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, W, H,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, Data);


            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureParameterName.ClampToEdge);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureParameterName.ClampToEdge);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            //GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            //GL.Ext.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.BindTexture(TextureTarget.Texture2D, 0);

            // Add to Texture List
            texture.color = new SColorF(1f, 1f, 1f, 1f);
            texture.rect = new SRectF(0f, 0f, texture.width, texture.height, 0f);
            texture.TexturePath = String.Empty;
            _Textures.Add(texture);

            return texture;
        }

        public STexture AddTexture(int W, int H, ref byte[] Data)
        {
            STexture texture = new STexture(-1);

            if (_UsePBO)
            {
                try
                {
                    GL.GenBuffers(1, out texture.PBO);
                    GL.BindBuffer(BufferTarget.PixelUnpackBuffer, texture.PBO);
                    GL.BufferData(BufferTarget.PixelUnpackBuffer, (IntPtr)Data.Length, IntPtr.Zero, BufferUsageHint.StreamDraw);
                    GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
                }
                catch (Exception)
                {
                    //throw;
                    _UsePBO = false;
                }
            }

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);
            texture.index = id;

            texture.width = W;
            texture.height = H;
            texture.w2 = (float)MathHelper.NextPowerOfTwo(texture.width);
            texture.h2 = (float)MathHelper.NextPowerOfTwo(texture.height);

            texture.width_ratio = texture.width / texture.w2;
            texture.height_ratio = texture.height / texture.h2;

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, (int)texture.w2, (int)texture.h2, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, W, H,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, Data);


            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureParameterName.ClampToEdge);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureParameterName.ClampToEdge);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            //GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            //GL.Ext.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.BindTexture(TextureTarget.Texture2D, 0);

            // Add to Texture List
            texture.color = new SColorF(1f, 1f, 1f, 1f);
            texture.rect = new SRectF(0f, 0f, texture.width, texture.height, 0f);
            texture.TexturePath = String.Empty;
            _Textures.Add(texture);

            return texture;
        }

        public bool UpdateTexture(ref STexture Texture, IntPtr Data)
        {
            if (_TextureExists(Texture))
            {
                if (_UsePBO)
                {
                    try
                    {
                        GL.BindBuffer(BufferTarget.PixelUnpackBuffer, Texture.PBO);
                                
                        IntPtr Buffer = GL.MapBuffer(BufferTarget.PixelUnpackBuffer, BufferAccess.WriteOnly);
                        byte[] d = new byte[(int)Texture.height * (int)Texture.width * 4];

                        Marshal.Copy(Data, d, 0, (int)Texture.height * (int)Texture.width * 4);
                        Marshal.Copy(d, 0, Buffer, (int)Texture.height * (int)Texture.width * 4);
                                
                        GL.UnmapBuffer(BufferTarget.PixelUnpackBuffer);

                        GL.BindTexture(TextureTarget.Texture2D, Texture.index);
                        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (int)Texture.width, (int)Texture.height,
                            OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

                        GL.BindTexture(TextureTarget.Texture2D, 0);
                        GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

                        return true;
                    }
                    catch (Exception)
                    {
                        _UsePBO = false;
                    }
                }

                GL.BindTexture(TextureTarget.Texture2D, Texture.index);

                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (int)Texture.width, (int)Texture.height,
                    OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, Data);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureParameterName.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureParameterName.ClampToEdge);

                //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                //GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                GL.Ext.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                GL.BindTexture(TextureTarget.Texture2D, 0);

                return true;
            }         
            return false;
        }

        public bool UpdateTexture(ref STexture Texture, ref byte[] Data)
        {
            if (_TextureExists(Texture))
            {
                if (_UsePBO)
                {
                    try
                    {
                        GL.BindBuffer(BufferTarget.PixelUnpackBuffer, Texture.PBO);
                                
                        IntPtr Buffer = GL.MapBuffer(BufferTarget.PixelUnpackBuffer, BufferAccess.WriteOnly);
                        Marshal.Copy(Data, 0, Buffer, Data.Length);

                        GL.UnmapBuffer(BufferTarget.PixelUnpackBuffer);

                        GL.BindTexture(TextureTarget.Texture2D, Texture.index);
                        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (int)Texture.width, (int)Texture.height,
                            OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

                        GL.BindTexture(TextureTarget.Texture2D, 0);
                        GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

                        return true;
                    }
                    catch (Exception)
                    {
                        throw;
                        //_UsePBO = false;
                    }
                }

                GL.BindTexture(TextureTarget.Texture2D, Texture.index);

                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (int)Texture.width, (int)Texture.height,
                    OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, Data);

                //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureParameterName.ClampToEdge);
                //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureParameterName.ClampToEdge);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

                //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                //GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                //GL.Ext.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                GL.BindTexture(TextureTarget.Texture2D, 0);

                return true;
            }
            return false;
        }

        public void RemoveTexture(ref STexture Texture)
        {
            if ((Texture.index > 0) && (_Textures.Count > 0))
            {
                for (int i = 0; i < _Textures.Count; i++)
			    { 
                    if (_Textures[i].index == Texture.index)
                    {
                        GL.DeleteTexture(Texture.index);
                        if (Texture.PBO > 0)
                            GL.DeleteBuffers(1, ref Texture.PBO);
                        _Textures.RemoveAt(i);
                        Texture.index = -1;
                        break;
                    }
                }
            }
        }

        private bool _TextureExists(STexture Texture)
        {
            if ((Texture.index > 0) && (_Textures.Count > 0))
            {
                for (int i = 0; i < _Textures.Count; i++)
                {
                    if (_Textures[i].index == Texture.index)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

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

            if (_TextureExists(Texture))
            {
                GL.BindTexture(TextureTarget.Texture2D, Texture.index);
                
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

                GL.Enable(EnableCap.Blend);
                GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);

                GL.MatrixMode(MatrixMode.Texture);
                GL.PushMatrix();
                
                if (rect.Rotation != 0f)
                {
                    GL.Translate(0.5f, 0.5f, 0);
                    GL.Rotate(-rect.Rotation, 0f, 0f, 1f);
                    GL.Translate(-0.5f, -0.5f, 0);  
                }
                                
                if (!mirrored)
                {
                    GL.Begin(BeginMode.Quads);

                    GL.TexCoord2(x1, y1);
                    GL.Vertex3(rx1, ry1, rect.Z + CGraphics.ZOffset);
                    
                    GL.TexCoord2(x1, y2);
                    GL.Vertex3(rx1, ry2, rect.Z + CGraphics.ZOffset);

                    GL.TexCoord2(x2, y2);
                    GL.Vertex3(rx2, ry2, rect.Z + CGraphics.ZOffset);

                    GL.TexCoord2(x2, y1);
                    GL.Vertex3(rx2, ry1, rect.Z + CGraphics.ZOffset);

                    GL.End();
                }
                else
                {
                    GL.Begin(BeginMode.Quads);

                    GL.TexCoord2(x2, y2);
                    GL.Vertex3(rx2, ry1, rect.Z + CGraphics.ZOffset);

                    GL.TexCoord2(x2, y1);
                    GL.Vertex3(rx2, ry2, rect.Z + CGraphics.ZOffset);

                    GL.TexCoord2(x1, y1);
                    GL.Vertex3(rx1, ry2, rect.Z + CGraphics.ZOffset);

                    GL.TexCoord2(x1, y2);
                    GL.Vertex3(rx1, ry1, rect.Z + CGraphics.ZOffset);

                    GL.End();
                }

                GL.PopMatrix();
                                
                GL.Disable(EnableCap.Blend);
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
        }

        public void DrawTexture(STexture Texture, SRectF rect, SColorF color, float begin, float end)
        {
            if (_TextureExists(Texture))
            {
                GL.BindTexture(TextureTarget.Texture2D, Texture.index);
                
                GL.Enable(EnableCap.Blend);
                GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);

                
                GL.Begin(BeginMode.Quads);

                GL.TexCoord2(0f + begin * Texture.width_ratio, 0f);
                GL.Vertex3(rect.X + begin * rect.W, rect.Y, rect.Z + CGraphics.ZOffset);

                GL.TexCoord2(0f + begin * Texture.width_ratio, Texture.height_ratio);
                GL.Vertex3(rect.X + begin * rect.W, rect.Y + rect.H, rect.Z + CGraphics.ZOffset);

                GL.TexCoord2(Texture.width_ratio * end, Texture.height_ratio);
                GL.Vertex3(rect.X + end * rect.W, rect.Y + rect.H, rect.Z + CGraphics.ZOffset);

                GL.TexCoord2(Texture.width_ratio * end, 0f);
                GL.Vertex3(rect.X + end * rect.W, rect.Y, rect.Z + CGraphics.ZOffset);

                GL.End();
                

                GL.Disable(EnableCap.Blend);
                GL.BindTexture(TextureTarget.Texture2D, 0);
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

            if (_TextureExists(Texture))
            {
                GL.BindTexture(TextureTarget.Texture2D, Texture.index);

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

                GL.Enable(EnableCap.Blend);
                
                GL.MatrixMode(MatrixMode.Texture);
                GL.PushMatrix();

                if (rect.Rotation != 0f)
                {
                    GL.Translate(0.5f, 0.5f, 0);
                    GL.Rotate(-rect.Rotation, 0f, 0f, 1f);
                    GL.Translate(-0.5f, -0.5f, 0);
                }

                
                GL.Begin(BeginMode.Quads);

                GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);
                GL.TexCoord2(x2, y2);
                GL.Vertex3(rx2, ry1, rect.Z + CGraphics.ZOffset);

                GL.Color4(color.R, color.G, color.B, 0f);
                GL.TexCoord2(x2, y1);
                GL.Vertex3(rx2, ry2, rect.Z + CGraphics.ZOffset);

                GL.Color4(color.R, color.G, color.B, 0f);
                GL.TexCoord2(x1, y1);
                GL.Vertex3(rx1, ry2, rect.Z + CGraphics.ZOffset);

                GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);
                GL.TexCoord2(x1, y2);
                GL.Vertex3(rx1, ry1, rect.Z + CGraphics.ZOffset);

                GL.End();
                

                GL.PopMatrix();

                GL.Disable(EnableCap.Blend);
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
        }

        public int TextureCount()
        {
            return _Textures.Count;
        }
        #endregion Textures

        #endregion implementation
    }
}
