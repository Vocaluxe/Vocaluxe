using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Menu;

namespace Vocaluxe.Lib.Draw
{
    class CDrawWinForm : Form, IDraw
    {
        private bool _Run;
        private Bitmap _backbuffer;
        private Graphics _g;
        private bool _fullscreen = false;
        
        private CKeys _Keys;
        private CMouse _Mouse;

        private FormBorderStyle _brdStyle;
        private bool _topMost;
        private Rectangle _bounds;
        
        private List<STexture> _Textures;
        private List<Bitmap> _Bitmaps;

        private Color ClearColor = Color.DarkBlue;

        public CDrawWinForm()
        {
            this.Icon = new System.Drawing.Icon(Path.Combine(System.Environment.CurrentDirectory, CSettings.sIcon));

            _Textures = new List<STexture>();
            _Bitmaps = new List<Bitmap>();

            _Keys = new CKeys();
            _Mouse = new CMouse();
            this.ClientSize = new Size(1280, 720);

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);

            // Create the backbuffer
            _backbuffer = new Bitmap(CSettings.iRenderW, CSettings.iRenderH);
            _g = Graphics.FromImage(_backbuffer);
            _g.Clear(Color.DarkBlue);

            this.Paint += new PaintEventHandler(this.OnPaintEvent);
            this.Closing += new CancelEventHandler(this.OnClosingEvent);
            this.KeyDown += new KeyEventHandler(this.OnKeyDownEvent);
            this.KeyPress += new KeyPressEventHandler(this.OnKeyPressEvent);
            this.KeyUp += new KeyEventHandler(this.OnKeyUpEvent);
            this.Resize += new EventHandler(this.OnResizeEvent);

            this.MouseMove += new MouseEventHandler(this.OnMouseMove);
            this.MouseDown += new MouseEventHandler(this.OnMouseDown);
            this.MouseUp += new MouseEventHandler(this.OnMouseUp);

            FlipBuffer();
            Cursor.Show();
        }

        private void FlipBuffer()
        {
            DrawBuffer();
            _g.Clear(ClearColor);
        }

        private void DrawBuffer()
        {
            Graphics gFrontBuffer = Graphics.FromHwnd(this.Handle);
            int h = this.ClientSize.Height;
            int w = this.ClientSize.Width;
            int y = 0;
            int x = 0;

            if ((float)this.ClientSize.Width / (float)this.ClientSize.Height > CSettings.GetRenderAspect())
            {
                w = (int)Math.Round((float)this.ClientSize.Height * CSettings.GetRenderAspect());
                x = (this.ClientSize.Width - w) / 2;
            }
            else
            {
                h = (int)Math.Round((float)this.ClientSize.Width / CSettings.GetRenderAspect());
                y = (this.ClientSize.Height - h) / 2;
            }

            gFrontBuffer.DrawImage(_backbuffer, new Rectangle(x, y, w, h), new Rectangle(0, 0, _backbuffer.Width, _backbuffer.Height), GraphicsUnit.Pixel);
        }

        private void OnPaintEvent(object sender, PaintEventArgs e)
        {
            
        }

        #region FullScreenStuff

        private void ToggleFullScreen()
        {
            if (!_fullscreen)
                Maximize(this);
            else
                Restore(this);
        }

        private void Maximize(Form targetForm)
        {
            if (!_fullscreen)
            {
                Save(targetForm);
                targetForm.FormBorderStyle = FormBorderStyle.None;
                targetForm.Bounds = Screen.PrimaryScreen.Bounds;
                targetForm.TopMost = true;
                _fullscreen = true;

                CConfig.FullScreen = EOffOn.TR_CONFIG_ON;
                CConfig.SaveConfig();
            }
        }

        private void Save(Form targetForm)
        {
            _brdStyle = targetForm.FormBorderStyle;
            _topMost = targetForm.TopMost;
            _bounds = targetForm.Bounds;
        }

        private void Restore(Form targetForm)
        {
            targetForm.FormBorderStyle = _brdStyle;
            targetForm.TopMost = _topMost;
            targetForm.Bounds = _bounds;
            _fullscreen = false;

            CConfig.FullScreen = EOffOn.TR_CONFIG_OFF;
            CConfig.SaveConfig();
        }
        #endregion FullScreenStuff

        private void OnResizeEvent(object sender, EventArgs e)
        {
            Graphics gFrontBuffer = Graphics.FromHwnd(this.Handle);
            gFrontBuffer.Clear(Color.Black);
        }

        private void OnClosingEvent(object sender, CancelEventArgs e)
        {
            _Run = false;
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

        #region mouse event handlers
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            _Mouse.MouseMove(e);
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            _Mouse.MouseDown(e);
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            _Mouse.MouseUp(e);
        }
        #endregion

        public bool Init()
        {
            this.Text = CSettings.GetFullVersionText();
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
                Maximize(this);
            }

            while (_Run)
            {
                Application.DoEvents();

                if (_Run)
                {
                    _Run = _Run && CGraphics.Draw();
                    _Run = CGraphics.UpdateGameLogic(_Keys, _Mouse);
                    FlipBuffer();          

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
            this.Dispose();
            return true;
        }

        public int GetScreenWidth()
        {
            return this.ClientSize.Width;
        }

        public int GetScreenHeight()
        {
            return this.ClientSize.Height;
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

        public void ClearScreen()
        {
            _g.Clear(ClearColor);
        }

        public STexture CopyScreen()
        {
            STexture texture = new STexture(0);
            Bitmap bmp = new Bitmap(_backbuffer);
            _Bitmaps.Add(bmp);

            texture.index = _Bitmaps.Count - 1;

            texture.width = bmp.Width;
            texture.height = bmp.Height;

            // Add to Texture List
            texture.color = new SColorF(1f, 1f, 1f, 1f);
            texture.rect = new SRectF(0f, 0f, texture.width, texture.height, 0f);
            _Textures.Add(texture);

            return texture;
        }

        public void CopyScreen(ref STexture Texture)
        {
            if ((Texture.index == 0) || (Texture.width != GetScreenWidth()) || (Texture.height != GetScreenHeight()))
            {
                RemoveTexture(ref Texture);
                Texture = CopyScreen();
            }
            else
            {
                _Bitmaps[Texture.index] = new Bitmap(_backbuffer);
            }
        }

        public void MakeScreenShot()
        {
            string file = "Screenshot_";
            string path = Path.Combine(Environment.CurrentDirectory, CSettings.sFolderScreenshots);

            int i = 0;
            while (File.Exists(Path.Combine(path, file + i.ToString("00000") + ".png")))
                i++;

            _backbuffer.Save(Path.Combine(path, file + i.ToString("00000") + ".png"), ImageFormat.Png);
        }

        public void DrawLine(int a, int r, int g, int b, int w, int x1, int y1, int x2, int y2)
        {
            _g.DrawLine(new Pen(Color.FromArgb(a, r, g, b), w), new Point(x1, y1), new Point(x2, y2));
        }

        // Draw Basic Text
        public void DrawText(string Text, int x, int y, int h)
        {
            DrawText(Text, x, y, h, 0f);
        }

        // Draw Basic Text
        public void DrawText(string Text, int x, int y, float h, float z)
        {
            CFonts.DrawText(Text, h, x, y, z, new SColorF(1, 1, 1, 1));
        }


        public void DrawColor(SColorF color, SRectF rect)
        {

        }

        public STexture AddTexture(Bitmap bmp)
        {
            Bitmap bmp2 = new Bitmap(bmp);
            _Bitmaps.Add(bmp2);
            STexture texture = new STexture();

            texture.index = _Bitmaps.Count - 1;

            texture.width = bmp.Width;
            texture.height = bmp.Height;

            // Add to Texture List
            texture.color = new SColorF(1f, 1f, 1f, 1f);
            texture.rect = new SRectF(0f, 0f, texture.width, texture.height, 0f);
            texture.TexturePath = String.Empty;

            _Textures.Add(texture);

            return texture;
        }

        public STexture AddTexture(string TexturePath)
        {
            STexture texture = new STexture();
            if (System.IO.File.Exists(TexturePath))
            {
                bool found = false;
                foreach(STexture tex in _Textures)
                {
                    if (tex.TexturePath == TexturePath)
                    {
                        texture = tex;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Bitmap bmp = new Bitmap(TexturePath);
                    return AddTexture(bmp);
                }
            }
            
            return texture;
        }

        public void RemoveTexture(ref STexture Texture)
        {
            if ((Texture.index >= 0) && (_Textures.Count > 0))
            {
                for (int i = 0; i < _Textures.Count; i++)
                {
                    if (_Textures[i].index == Texture.index)
                    {
                        _Bitmaps[Texture.index].Dispose();
                        _Textures.RemoveAt(i);
                        Texture.index = -1;
                        break;
                    }
                }
            }
        }

        public STexture AddTexture(int W, int H, IntPtr Data)
        {
            return new STexture(-1);
        }

        public STexture AddTexture(int W, int H, ref byte[] Data)
        {
            Bitmap bmp = new Bitmap(W, H);
            BitmapData bmp_data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Marshal.Copy(Data, 0, bmp_data.Scan0, Data.Length);
            bmp.UnlockBits(bmp_data);
            return AddTexture(bmp);
        }

        public bool UpdateTexture(ref STexture Texture, IntPtr Data)
        {
            return true;
        }

        public bool UpdateTexture(ref STexture Texture, ref byte[] Data)
        {
            if ((Texture.index >= 0) && (_Textures.Count > 0) && (_Bitmaps.Count > Texture.index))
            {
                BitmapData bmp_data = _Bitmaps[Texture.index].LockBits(new Rectangle(0, 0, _Bitmaps[Texture.index].Width, _Bitmaps[Texture.index].Height),
                    ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Marshal.Copy(Data, 0, bmp_data.Scan0, Data.Length);
                _Bitmaps[Texture.index].UnlockBits(bmp_data);
            }
            return true;
        }

        public void DrawTexture(STexture Texture)
        {
            if ((Texture.index >= 0) && (_Textures.Count > 0) && (_Bitmaps.Count > Texture.index))
            {
                DrawTexture(Texture, Texture.rect, Texture.color);   
            }
        }

        public void DrawTexture(STexture Texture, SRectF rect)
        {
            if ((Texture.index >= 0) && (_Textures.Count > 0) && (_Bitmaps.Count > Texture.index))
            {
                DrawTexture(Texture, rect, Texture.color);
            }
        }

        public void DrawTexture(STexture Texture, SRectF rect, SColorF color)
        {
            if ((Texture.index >= 0) && (_Textures.Count > 0) && (_Bitmaps.Count > Texture.index))
            {
                DrawTexture(Texture, rect, color, rect, false);   
            }
        }

        public void DrawTexture(STexture Texture, SRectF rect, SColorF color, SRectF bounds)
        {
            if ((Texture.index >= 0) && (_Textures.Count > 0) && (_Bitmaps.Count > Texture.index))
            {
                DrawTexture(Texture, rect, color, bounds, false);
            }
        }

        public void DrawTexture(STexture Texture, SRectF rect, SColorF color, bool mirrored)
        {
            if ((Texture.index >= 0) && (_Textures.Count > 0) && (_Bitmaps.Count > Texture.index))
            {
                DrawTexture(Texture, rect, color, rect, mirrored);
            }
        }

        public void DrawTexture(STexture Texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored)
        {
            if ((Texture.index >= 0) && (_Textures.Count > 0) && (_Bitmaps.Count > Texture.index))
            {
                Bitmap ColoredBitmap = ColorizeBitmap(_Bitmaps[Texture.index], color);
                _g.DrawImage(ColoredBitmap, new RectangleF(rect.X, rect.Y, rect.W, rect.H));
                ColoredBitmap.Dispose();
            }
        }

        public void DrawTexture(STexture Texture, SRectF rect, SColorF color, float begin, float end)
        {

        }

        public void DrawTextureReflection(STexture Texture, SRectF rect, SColorF color, SRectF bounds, float space, float height)
        {
        }

        public int TextureCount()
        {
            return _Textures.Count;
        }

        public static Bitmap ColorizeBitmap(Bitmap original, SColorF color)
        {
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            Graphics g = Graphics.FromImage(newBitmap);

            ColorMatrix cm = new ColorMatrix();
            cm.Matrix33 = color.A;
            cm.Matrix00 = color.R;
            cm.Matrix11 = color.G;
            cm.Matrix22 = color.B;
            cm.Matrix44 = 1;

            ImageAttributes ia = new ImageAttributes();
            ia.SetColorMatrix(cm);

            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                0, 0, original.Width, original.Height, GraphicsUnit.Pixel, ia);

            ia.Dispose();
            g.Dispose();

            return newBitmap;
        }
    }
}
