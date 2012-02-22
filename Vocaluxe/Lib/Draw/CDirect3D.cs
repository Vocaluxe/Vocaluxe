using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Base;
using Vocaluxe.Menu;
using System.Drawing;

using SlimDX;
using SlimDX.Direct3D9;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using SlimDX.Windows;


namespace Vocaluxe.Lib.Draw
{
    class CDirect3D : Form, IDraw
    {
        #region private vars

        private CKeys _Keys;
        private CMouse _Mouse;
        private bool _Run;

        private Direct3D _D3D;
        private Device _Device;
        private PresentParameters _PresentParameters;

        private bool _fullscreen = false;
        private Size _OldSize;
        private Size _SizeBeforeMinimize;

        private List<STexture> _Textures;
        private List<Texture> _D3DTextures;

        private VertexBuffer _VertexBuffer;

        private int h = 1;
        private int w = 1;
        private int y = 0;
        private int x = 0;

        private Bitmap blankMap;
        private STexture blankTexture;

        #endregion private vars

        public CDirect3D()
        {
            this.Icon = new System.Drawing.Icon(Path.Combine(System.Environment.CurrentDirectory, CSettings.sIcon));
            _Textures = new List<STexture>();
            _D3DTextures = new List<Texture>();
            _Keys = new CKeys();
            _D3D = new Direct3D();

            this.Paint += new PaintEventHandler(this.OnPaintEvent);
            this.Closing += new CancelEventHandler(this.OnClosingEvent);
            this.Resize += new EventHandler(this.OnResizeEvent);

            this.KeyDown += new KeyEventHandler(this.OnKeyDown);
            this.PreviewKeyDown += new PreviewKeyDownEventHandler(this.OnPreviewKeyDown);
            this.KeyPress += new KeyPressEventHandler(this.OnKeyPress);
            this.KeyUp += new KeyEventHandler(this.OnKeyUp);

            _Mouse = new CMouse();
            this.MouseMove += new MouseEventHandler(this.OnMouseMove);
            this.MouseWheel += new MouseEventHandler(this.OnMouseWheel);
            this.MouseDown += new MouseEventHandler(this.OnMouseDown);
            this.MouseUp += new MouseEventHandler(this.OnMouseUp);
            this.MouseLeave += new EventHandler(this.OnMouseLeave);
            this.MouseEnter += new EventHandler(this.OnMouseEnter);

            this.ClientSize = new Size(CConfig.ScreenW, CConfig.ScreenH);
            _SizeBeforeMinimize = ClientSize;

            _PresentParameters = new PresentParameters();
            _PresentParameters.Windowed = true;
            _PresentParameters.SwapEffect = SwapEffect.Discard;
            _PresentParameters.BackBufferHeight = CConfig.ScreenH;
            _PresentParameters.BackBufferWidth = CConfig.ScreenW;
            _PresentParameters.BackBufferFormat = _D3D.Adapters.DefaultAdapter.CurrentDisplayMode.Format;
            _PresentParameters.Multisample = MultisampleType.None;
            _PresentParameters.MultisampleQuality = 0;

            int quality = 0;
            if (CConfig.AAMode == EAntiAliasingModes.x0)
            {
                _PresentParameters.Multisample = MultisampleType.None;
                _PresentParameters.MultisampleQuality = quality;
            }
            else if (CConfig.AAMode == EAntiAliasingModes.x2)
            {
                if (_D3D.CheckDeviceMultisampleType(_D3D.Adapters.DefaultAdapter.Adapter, DeviceType.Hardware, _D3D.Adapters.DefaultAdapter.CurrentDisplayMode.Format, false, MultisampleType.TwoSamples, out quality))
                {
                    _PresentParameters.Multisample = MultisampleType.TwoSamples;
                    _PresentParameters.MultisampleQuality = quality - 1;
                }
                else
                    CLog.LogError("[Direct3D] This AAMode is not supported by this device or driver, fallback to no AA");
            }
            else if (CConfig.AAMode == EAntiAliasingModes.x4)
            {
                if (_D3D.CheckDeviceMultisampleType(_D3D.Adapters.DefaultAdapter.Adapter, DeviceType.Hardware, _D3D.Adapters.DefaultAdapter.CurrentDisplayMode.Format, false, MultisampleType.FourSamples, out quality))
                {
                    _PresentParameters.Multisample = MultisampleType.FourSamples;
                    _PresentParameters.MultisampleQuality = quality - 1;
                }
                else
                    CLog.LogError("[Direct3D] This AAMode is not supported by this device or driver, fallback to no AA");
            }
            else if (CConfig.AAMode == EAntiAliasingModes.x8)
            {
                if (_D3D.CheckDeviceMultisampleType(_D3D.Adapters.DefaultAdapter.Adapter, DeviceType.Hardware, _D3D.Adapters.DefaultAdapter.CurrentDisplayMode.Format, false, MultisampleType.EightSamples, out quality))
                {
                    _PresentParameters.Multisample = MultisampleType.EightSamples;
                    _PresentParameters.MultisampleQuality = quality - 1;
                }
                else
                    CLog.LogError("[Direct3D] This AAMode is not supported by this device or driver, fallback to no AA");
            }
            else if (CConfig.AAMode == EAntiAliasingModes.x16 || CConfig.AAMode == EAntiAliasingModes.x32) //x32 is not supported, fallback to x16
            {
                if (_D3D.CheckDeviceMultisampleType(_D3D.Adapters.DefaultAdapter.Adapter, DeviceType.Hardware, _D3D.Adapters.DefaultAdapter.CurrentDisplayMode.Format, false, MultisampleType.SixteenSamples, out quality))
                {
                    _PresentParameters.Multisample = MultisampleType.SixteenSamples;
                    _PresentParameters.MultisampleQuality = quality - 1;
                }
                else
                    CLog.LogError("[Direct3D] This AAMode is not supported by this device or driver, fallback to no AA");
            }

            if(CConfig.VSync == EOffOn.TR_CONFIG_ON)
                _PresentParameters.PresentationInterval = PresentInterval.Default;
            else
                _PresentParameters.PresentationInterval = PresentInterval.Immediate;
            
            Capabilities caps = _D3D.GetDeviceCaps(_D3D.Adapters.DefaultAdapter.Adapter, DeviceType.Hardware);
            CreateFlags flags = CreateFlags.None;
            if ((caps.DeviceCaps & DeviceCaps.HWTransformAndLight) != 0)
                flags = CreateFlags.HardwareVertexProcessing;
            else
                flags = CreateFlags.SoftwareVertexProcessing;
            _Device = new Device(_D3D, _D3D.Adapters.DefaultAdapter.Adapter, DeviceType.Hardware, Handle, flags, _PresentParameters);

            this.CenterToScreen();
            
            blankMap = new Bitmap(1, 1); //Quick and Dirty, used for DrawColor
            Graphics g = Graphics.FromImage(blankMap);
            g.Clear(Color.White);
            g.Dispose();
            blankTexture = AddTexture(blankMap);
            h = ClientSize.Height;
            w = ClientSize.Width;
            blankMap.Dispose();
        }

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
            _Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.ClientSize = this.ClientSize;
            RResize();
        }
        #endregion form events

        private void RResize()
        {
            if (ClientSize.Width == 0 || ClientSize.Height == 0)
            {
                ClientSize = _SizeBeforeMinimize;
            }
            if (_Run)
            {
                h = ClientSize.Height;
                w = ClientSize.Width;
                y = 0;
                x = 0;

                if ((float)w / (float)h > CSettings.GetRenderAspect())
                {
                    w = (int)Math.Round((float)h * CSettings.GetRenderAspect());
                    x = (ClientSize.Width - w) / 2;
                }
                else
                {
                    h = (int)Math.Round((float)w / CSettings.GetRenderAspect());
                    y = (ClientSize.Height - h) / 2;
                }

                _PresentParameters.BackBufferWidth = ClientSize.Width;
                _PresentParameters.BackBufferHeight = ClientSize.Height;
                ClearScreen();
                Reset();
                Init();

                _Device.Viewport = new Viewport(x, y, w, h);
                _SizeBeforeMinimize = ClientSize;
            }
        }

        private void EnterFullScreen()
        {
            _OldSize = this.ClientSize;
            this.ClientSize = new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            this.FormBorderStyle = FormBorderStyle.None;
            CenterToScreen();
            _fullscreen = true;
            CConfig.FullScreen = EOffOn.TR_CONFIG_ON;
            RResize();

            Cursor.Hide();
            _Mouse.Visible = true;

            CConfig.SaveConfig();
        }

        private void LeaveFullScreen()
        {
            this.ClientSize = _OldSize;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            CenterToScreen();
            _fullscreen = false;
            CConfig.FullScreen = EOffOn.TR_CONFIG_OFF;

            Cursor.Hide();
            _Mouse.Visible = true;

            CConfig.SaveConfig();
        }

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

        private void OnPreviewKeyDown(object sender, System.Windows.Forms.PreviewKeyDownEventArgs e)
        {
            OnKeyDown(sender, new KeyEventArgs(e.KeyData));
        }

        private void OnKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            _Keys.KeyDown(e);
        }


        private void OnKeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            _Keys.KeyPress(e);
        }

        private void OnKeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            _Keys.KeyUp(e);
        }
        #endregion keyboard event handlers

        #region implementation

        public bool Init()
        {
            this.Text = CSettings.GetFullVersionText();
            Matrix translate = Matrix.Translation(new Vector3(-CSettings.iRenderW / 2, CSettings.iRenderH / 2, 0));
            Matrix projection = Matrix.OrthoOffCenterLH(-CSettings.iRenderW / 2, CSettings.iRenderW / 2, -CSettings.iRenderH / 2, CSettings.iRenderH / 2, CSettings.zNear, CSettings.zFar);
            _VertexBuffer = new VertexBuffer(_Device, 4 * Marshal.SizeOf(typeof(TexturedColoredVertex)), Usage.WriteOnly | Usage.Dynamic, VertexFormat.Position | VertexFormat.Texture1 | VertexFormat.Diffuse, Pool.Default);
            _Device.SetStreamSource(0, _VertexBuffer, 0, Marshal.SizeOf(typeof(TexturedColoredVertex)));
            _Device.VertexDeclaration = TexturedColoredVertex.GetDeclaration(_Device);
            _Device.SetTransform(TransformState.Projection, projection);
            _Device.SetTransform(TransformState.World, translate);
            _Device.SetRenderState(RenderState.CullMode, Cull.None);
            _Device.SetRenderState(RenderState.AlphaBlendEnable, true);
            _Device.SetRenderState(RenderState.Lighting, false);
            _Device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
            _Device.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
            if(_PresentParameters.Multisample != MultisampleType.None)
                _Device.SetRenderState(RenderState.MultisampleAntialias, true);
            _Device.SetSamplerState(0, SamplerState.MinFilter, TextureFilter.Linear);
            _Device.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.Linear);
            _Device.SetSamplerState(0, SamplerState.MipFilter, TextureFilter.Linear);
            _Device.SetSamplerState(0, SamplerState.AddressU, TextureAddress.Clamp);
            _Device.SetSamplerState(0, SamplerState.AddressV, TextureAddress.Clamp);
            _Device.SetTextureStageState(0, TextureStage.AlphaArg1, TextureArgument.Texture);
            _Device.SetTextureStageState(0, TextureStage.AlphaArg2, TextureArgument.Diffuse);
            _Device.SetTextureStageState(0, TextureStage.AlphaOperation, TextureOperation.Modulate);
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
                    _Device.BeginScene();
                    _Run = _Run && CGraphics.Draw();
                    _Run = CGraphics.UpdateGameLogic(_Keys, _Mouse);
                    _Device.EndScene();
                    try
                    {
                        _Device.Present();
                    }
                    catch (Direct3D9Exception)
                    {
                        if (_Device.TestCooperativeLevel() == ResultCode.DeviceNotReset) //Do only reset when the device in a resetable state!
                        {
                            Reset();
                            Init();
                        }
                    }
                    if ((CSettings.bFullScreen && !_fullscreen) || (!CSettings.bFullScreen && _fullscreen))
                    {
                        if (!_fullscreen)
                            EnterFullScreen();
                        else
                            LeaveFullScreen();                        
                    }
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

        public void Reset()
        {
            TexturedColoredVertex.GetDeclaration(_Device).Dispose();
            _VertexBuffer.Dispose();
            _Device.Reset(_PresentParameters);
        }

        public bool Unload()
        {
            for (int i = 0; i < _D3DTextures.Count; i++)
            {
                _D3DTextures[i].Dispose();

            }
            TexturedColoredVertex.GetDeclaration(_Device).Dispose();
            _VertexBuffer.Dispose();
            _Device.Dispose();
            _D3D.Dispose();
            return true;
        }

        public int GetScreenWidth()
        {
            return _Device.Viewport.Width;
        }

        public int GetScreenHeight()
        {
            return _Device.Viewport.Height;
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
            _Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
        }
        public STexture CopyScreen()
        {
            STexture texture = new STexture(-1);
            return texture;
        }

        public void CopyScreen(ref STexture Texture)
        {
            return;
        }

        public void MakeScreenShot()
        {
            string file = "Screenshot_";
            string path = Path.Combine(Environment.CurrentDirectory, CSettings.sFolderScreenshots);

            int i = 0;
            while (File.Exists(Path.Combine(path, file + i.ToString("00000") + ".bmp")))
                i++;

            using (Surface surface = _Device.GetBackBuffer(0, 0))
            {
                Bitmap screen = new Bitmap(Surface.ToStream(surface, ImageFileFormat.Bmp));
                screen.Save(Path.Combine(path, file + i.ToString("00000") + ".bmp"), ImageFormat.Bmp);
                screen.Dispose();
            }
            Cursor.Hide();
            _Mouse.Visible = true;
        }

        public void DrawLine(int a, int r, int g, int b, int w, int x1, int y1, int x2, int y2)
        {
            SColorF color = new SColorF(a / 255, g / 255, b / 255, a / 255);
            DrawTexture(blankTexture, new SRectF(x1, y1, x2, y2, 1), color);
        }

        public void DrawText(string Text, int x, int y, int h)
        {
            DrawText(Text, x, y, h, 0f);
        }

        public void DrawText(string Text, int x, int y, float h, float z)
        {
            CFonts.DrawText(Text, h, x, y, z, new SColorF(1f, 1f, 1f, 1f));
        }

        public void DrawColor(SColorF color, SRectF rect)
        {
            DrawTexture(blankTexture, rect, color);
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

        private static float NextPowerOfTwo(float n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException("n", "Must be positive.");
            return (float)System.Math.Pow(2, System.Math.Ceiling(System.Math.Log((double)n, 2)));
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

            texture.w2 = (float)NextPowerOfTwo(w);
            texture.h2 = (float)NextPowerOfTwo(h);

            Bitmap bmp2 = new Bitmap((int)texture.w2, (int)texture.h2);
            Graphics g = Graphics.FromImage(bmp2);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.DrawImage(bmp, new Rectangle(0, 0, bmp2.Width, bmp2.Height));
            g.Dispose();

            texture.width_ratio = 1f;
            texture.height_ratio = 1f;
            
            BitmapData bmp_data = bmp2.LockBits(new Rectangle(0, 0, bmp2.Width, bmp2.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            byte[] Data = new byte[4 * bmp2.Width * bmp2.Height];
            Marshal.Copy(bmp_data.Scan0, Data, 0, Data.Length);
            bmp2.UnlockBits(bmp_data);
            
            Texture t = new Texture(_Device, (int)texture.w2, (int)texture.h2, 0, Usage.AutoGenerateMipMap, Format.A8R8G8B8, Pool.Managed);
            DataRectangle rect = t.LockRectangle(0, LockFlags.None);
            for (int i = 0; i < Data.Length; )
            {
                rect.Data.Write(Data, i, 4 * bmp2.Width);
                i += 4 * bmp2.Width;
                rect.Data.Position = rect.Data.Position - 4 * bmp2.Width;
                rect.Data.Position += rect.Pitch;
            }
            t.UnlockRectangle(0);
            _D3DTextures.Add(t);
            bmp2.Dispose();
            bmp_data = null;

            // Add to Texture List
            texture.index = _D3DTextures.Count - 1;
            texture.color = new SColorF(1f, 1f, 1f, 1f);
            texture.rect = new SRectF(0f, 0f, texture.width, texture.height, 0f);
            texture.TexturePath = String.Empty;
            _Textures.Add(texture);
            
            return texture;
        }

        public STexture AddTexture(Bitmap bmp)
        {
            return AddTexture(bmp, String.Empty);
        }

        public STexture AddTexture(int W, int H, IntPtr Data)
        {
            byte[] PointerData = new Byte[4 * W * H];
            Marshal.Copy(Data, PointerData, 0, PointerData.Length);
            return AddTexture(W, H , ref PointerData);
        }

        public STexture AddTexture(int W, int H, ref byte[] Data)
        {
            STexture texture = new STexture(-1);
            texture.width = W;
            texture.height = H;
            texture.w2 = NextPowerOfTwo(texture.width);
            texture.h2 = NextPowerOfTwo(texture.height);
            texture.width_ratio = texture.width / texture.w2;
            texture.height_ratio = texture.height / texture.h2;

            Texture t = new Texture(_Device, (int)texture.w2, (int)texture.h2, 0, Usage.AutoGenerateMipMap, Format.A8R8G8B8, Pool.Managed);
            DataRectangle rect = t.LockRectangle(0, LockFlags.None);
            for (int i = 0; i < Data.Length; )
            {
                rect.Data.Write(Data, i, 4 * W);
                i += 4 * W;
                rect.Data.Position = rect.Data.Position - 4 * W;
                rect.Data.Position += rect.Pitch;
            }
            t.UnlockRectangle(0);
            _D3DTextures.Add(t);

            texture.index = _D3DTextures.Count - 1;
            texture.color = new SColorF(1f, 1f, 1f, 1f);
            texture.rect = new SRectF(0f, 0f, texture.width, texture.height, 0f);
            texture.TexturePath = String.Empty;
            _Textures.Add(texture);
            return texture;
        }

        public bool UpdateTexture(ref STexture Texture, IntPtr Data)
        {
            byte[] PointerData = new Byte[4 * (int)Texture.width * (int)Texture.height];
            Marshal.Copy(Data, PointerData, 0, PointerData.Length);

            return UpdateTexture(ref Texture, ref PointerData);
        }

        public bool UpdateTexture(ref STexture Texture, ref byte[] Data)
        {
            if ((Texture.index >= 0) && (_Textures.Count > 0) && (_D3DTextures.Count > Texture.index))
            {
                DataRectangle rect = _D3DTextures[Texture.index].LockRectangle(0, LockFlags.None);
                for (int i = 0; i < Data.Length; )
                {
                    rect.Data.Write(Data, i, 4 * (int)Texture.width);
                    i += 4 * (int)Texture.width;
                    rect.Data.Position = rect.Data.Position - 4 * (int)Texture.width;
                    rect.Data.Position += rect.Pitch;
                }
                _D3DTextures[Texture.index].UnlockRectangle(0);

                Texture.height_ratio = Texture.height / NextPowerOfTwo(Texture.height);
                Texture.width_ratio = Texture.width / NextPowerOfTwo(Texture.width);
            }
            return true;
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

        public void RemoveTexture(ref STexture Texture)
        {
            if ((Texture.index >= 0) && (_Textures.Count > 0))
            {
                for (int i = 0; i < _Textures.Count; i++)
                {
                    if (_Textures[i].index == Texture.index)  
                    {
                        _D3DTextures[Texture.index].Dispose();
                        _Textures.RemoveAt(i);
                        Texture.index = -1;
                        GC.Collect();
                        break;
                    }
                }
            }
        }

        public void DrawTexture(STexture Texture)
        {
            if ((Texture.index >= 0) && (_Textures.Count > 0) && (_D3DTextures.Count > Texture.index))
                DrawTexture(Texture, Texture.rect, Texture.color);
        }

        public void DrawTexture(STexture Texture, SRectF rect)
        {
            if ((Texture.index >= 0) && (_Textures.Count > 0) && (_D3DTextures.Count > Texture.index))
                DrawTexture(Texture, rect, Texture.color, false);
        }

        public void DrawTexture(STexture Texture, SRectF rect, SColorF color)
        {
            if ((Texture.index >= 0) && (_Textures.Count > 0) && (_D3DTextures.Count > Texture.index))
                DrawTexture(Texture, rect, color, false);
        }

        public void DrawTexture(STexture Texture, SRectF rect, SColorF color, SRectF bounds)
        {
            if ((Texture.index >= 0) && (_Textures.Count > 0) && (_D3DTextures.Count > Texture.index))
                DrawTexture(Texture, rect, color, bounds, false);
        }

        public void DrawTexture(STexture Texture, SRectF rect, SColorF color, bool mirrored)
        {
            if ((Texture.index >= 0) && (_Textures.Count > 0) && (_D3DTextures.Count > Texture.index))
                DrawTexture(Texture, rect, color, new SRectF(0, 0, CSettings.iRenderW, CSettings.iRenderH, rect.Z), mirrored);
        }

        public void DrawTexture(STexture Texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored)
        {
            if ((Texture.index >= 0) && (_Textures.Count > 0) && (_D3DTextures.Count > Texture.index))
            {
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

                rx1 -= 0.5f; // Align pixels
                ry1 -= 0.5f;
                rx2 -= 0.5f;
                ry2 -= 0.5f;

                if (color.R >= 1f)
                    color.R = 1f;
                if (color.B >= 1f)
                    color.B = 1f;
                if (color.G >= 1f)
                    color.G = 1f;

                if (rect.Rotation != 0)
                {
                    rect.Rotation = rect.Rotation * (float)Math.PI / 180;
                    float centerX = (rx1 + rx2) / 2f;
                    float centerY = -(ry1 + ry2) / 2f;

                    Matrix originTranslation = _Device.GetTransform(TransformState.World);
                    Matrix translationA = Matrix.Translation(-centerX, -centerY, 0);
                    Matrix rotation = Matrix.RotationZ(-rect.Rotation);
                    Matrix translationB = Matrix.Translation(centerX, centerY, 0);

                    Matrix result = translationA * rotation * translationB * originTranslation;

                    _Device.SetTransform(TransformState.World, result);
                }

                Color c = Color.FromArgb((int)(color.A * 255 * CGraphics.GlobalAlpha), (int)(color.R * 255), (int)(color.G * 255), (int)(color.B * 255));
                DataStream stream = _VertexBuffer.Lock(0, 0, LockFlags.Discard);
                if (!mirrored)
                {
                    stream.WriteRange(new[] {
	                    new TexturedColoredVertex(new Vector3(rx1, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x1, y1), c.ToArgb()),
	                    new TexturedColoredVertex(new Vector3(rx1, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x1, y2), c.ToArgb()),
	                    new TexturedColoredVertex(new Vector3(rx2, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x2, y2), c.ToArgb()),
                        new TexturedColoredVertex(new Vector3(rx2, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x2, y1), c.ToArgb()),
	                });
                }
                else
                {
                    stream.WriteRange(new[] {
                    new TexturedColoredVertex(new Vector3(rx1, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x1, -y1), c.ToArgb()),
	                new TexturedColoredVertex(new Vector3(rx1, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x1, -y2), c.ToArgb()),
	                new TexturedColoredVertex(new Vector3(rx2, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x2, -y2), c.ToArgb()),
                    new TexturedColoredVertex(new Vector3(rx2, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x2, -y1), c.ToArgb()),
                    });
                }
                _VertexBuffer.Unlock();
                stream.Dispose();
                _Device.SetTexture(0, _D3DTextures[Texture.index]);
                _Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);

                if (rect.Rotation != 0)
                {
                    Matrix originTranslation = Matrix.Translation(new Vector3(-CSettings.iRenderW / 2, CSettings.iRenderH / 2, 0));
                    _Device.SetTransform(TransformState.World, originTranslation);
                }
            }
        }

        public void DrawTexture(STexture Texture, SRectF rect, SColorF color, float begin, float end)
        {

            float x1 = 0f + begin * Texture.width_ratio;
            float x2 = Texture.width_ratio * end;
            float y1 = 0f;
            float y2 = Texture.height_ratio;

            float rx1 = rect.X + begin* rect.W;
            float rx2 = rect.X + end * rect.W;
            float ry1 = rect.Y;
            float ry2 = rect.Y + rect.H;

            rx1 -= 0.5f; // Align pixels
            ry1 -= 0.5f;
            rx2 -= 0.5f;
            ry2 -= 0.5f;

            Color c = Color.FromArgb((int)(color.A * 255 * CGraphics.GlobalAlpha), (int)(color.R * 255), (int)(color.G * 255), (int)(color.B * 255));
            DataStream stream = _VertexBuffer.Lock(0, 0, LockFlags.Discard);
            stream.WriteRange(new[] {
	                    new TexturedColoredVertex(new Vector3(rx1, ry1*-1f, rect.Z + CGraphics.ZOffset), new Vector2(x1, y1), c.ToArgb()),
	                    new TexturedColoredVertex(new Vector3(rx1, ry2*-1f, rect.Z + CGraphics.ZOffset), new Vector2(x1, y2), c.ToArgb()),
	                    new TexturedColoredVertex(new Vector3(rx2, ry2*-1f, rect.Z + CGraphics.ZOffset), new Vector2(x2, y2), c.ToArgb()),
                        new TexturedColoredVertex(new Vector3(rx2, ry1*-1f, rect.Z + CGraphics.ZOffset), new Vector2(x2, y1), c.ToArgb()),
	                });
            _VertexBuffer.Unlock();
            stream.Dispose();
            _Device.SetTexture(0, _D3DTextures[Texture.index]);
            _Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);
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

                rx1 -= 0.5f; // Align pixels
                ry1 -= 0.5f;
                rx2 -= 0.5f;
                ry2 -= 0.5f;

                DataStream stream = _VertexBuffer.Lock(0, 0, LockFlags.Discard);

                Color c = Color.FromArgb((int)(color.A * 255 * CGraphics.GlobalAlpha), (int)(color.R * 255), (int)(color.G * 255), (int)(color.B * 255));
                Color t = Color.FromArgb(0, (int)(color.R * 255), (int)(color.G * 255), (int)(color.B * 255));

                stream.WriteRange(new[] {
                    new TexturedColoredVertex(new Vector3(rx2, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x2, y2), c.ToArgb()),
	                new TexturedColoredVertex(new Vector3(rx2, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x2, y1), t.ToArgb()),
	                new TexturedColoredVertex(new Vector3(rx1, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x1, y1), t.ToArgb()),
                    new TexturedColoredVertex(new Vector3(rx1, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x1, y2), c.ToArgb()),
	            });
                _VertexBuffer.Unlock();
                stream.Dispose();
                _Device.SetTexture(0, _D3DTextures[Texture.index]);
                _Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);
            }
        }

        public int TextureCount()
        {
            return _Textures.Count;
        }
        #endregion Textures
        #endregion implementation

        #region TexturedColoredVertex
        public struct TexturedColoredVertex
        {
            private static VertexDeclaration sDeclaration;
            public static VertexElement[] Elements =
        {
            new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
            new VertexElement(0, sizeof(float) *3, DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
            new VertexElement(0, sizeof(float) * 3 + sizeof(float)*2, DeclarationType.Color, DeclarationMethod.Default, DeclarationUsage.Color, 0),
            VertexElement.VertexDeclarationEnd
        };

            public Vector3 Position;
            public Vector2 Texture;
            public int Color;

            public TexturedColoredVertex(Vector3 position, Vector2 texture, int color)
            {
                this.Position = position;
                this.Texture = texture;
                this.Color = color;
            }

            public static VertexDeclaration GetDeclaration(Device device)
            {
                if (sDeclaration == null || sDeclaration.Disposed)
                {
                    sDeclaration = new VertexDeclaration(device, Elements);
                }

                return sDeclaration;
            }
        }
        #endregion TexturedColoredVertex
    }
}
