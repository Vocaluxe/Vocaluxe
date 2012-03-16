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
        private List<STextureQueque> _Queque;

        private Object MutexTexture = new Object();

        private VertexBuffer _VertexBuffer;

        private int h = 1;
        private int w = 1;
        private int y = 0;
        private int x = 0;

        private Bitmap blankMap;
        private STexture blankTexture;
        private Texture transparentTexture;

        #endregion private vars

        /// <summary>
        /// Creates a new Instance of the CDirect3D Class
        /// </summary>
        public CDirect3D()
        {
            this.Icon = new System.Drawing.Icon(Path.Combine(System.Environment.CurrentDirectory, CSettings.sIcon));
            _Textures = new List<STexture>();
            _D3DTextures = new List<Texture>();
            _Queque = new List<STextureQueque>();

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

            //Apply antialiasing and check if antialiasing mode is supported
            #region Antialiasing
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
            #endregion Antialiasing

            //Apply the VSync configuration
            if (CConfig.VSync == EOffOn.TR_CONFIG_ON)
                _PresentParameters.PresentationInterval = PresentInterval.Default;
            else
                _PresentParameters.PresentationInterval = PresentInterval.Immediate;

            //GMA 950 graphics devices can only process vertices in software mode
            Capabilities caps = _D3D.GetDeviceCaps(_D3D.Adapters.DefaultAdapter.Adapter, DeviceType.Hardware);
            CreateFlags flags = CreateFlags.None;
            if ((caps.DeviceCaps & DeviceCaps.HWTransformAndLight) != 0)
                flags = CreateFlags.HardwareVertexProcessing;
            else
                flags = CreateFlags.SoftwareVertexProcessing;
            _Device = new Device(_D3D, _D3D.Adapters.DefaultAdapter.Adapter, DeviceType.Hardware, Handle, flags, _PresentParameters);

            this.CenterToScreen();

            //This creates a new white texture and adds it to the texture pool
            //This texture is used for the DrawRect method
            blankMap = new Bitmap(1, 1);
            Graphics g = Graphics.FromImage(blankMap);
            g.Clear(Color.White);
            g.Dispose();
            blankTexture = AddTexture(blankMap);

            transparentTexture = new Texture(_Device, 1, 1, 0, Usage.None, Format.A8R8G8B8, Pool.Managed);
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
        /// <summary>
        /// Resizes the viewport
        /// </summary>
        private void RResize()
        {
            // The window was minimized, so restore it to the last known size
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
                    //The windows's width is too big
                    w = (int)Math.Round((float)h * CSettings.GetRenderAspect());
                    x = (ClientSize.Width - w) / 2;
                }
                else
                {
                    //The windows's height is too big
                    h = (int)Math.Round((float)w / CSettings.GetRenderAspect());
                    y = (ClientSize.Height - h) / 2;
                }

                //Apply the new sizes to the PresentParameters
                _PresentParameters.BackBufferWidth = ClientSize.Width;
                _PresentParameters.BackBufferHeight = ClientSize.Height;
                ClearScreen();
                //To set new PresentParameters the device has to be resetted
                Reset();
                //All configurations got flushed due to Reset(), so apply them again
                Init();

                //Set the new Viewport
                _Device.Viewport = new Viewport(x, y, w, h);
                //Store size so it can get restored after the window gets minimized
                _SizeBeforeMinimize = ClientSize;
            }
        }

        /// <summary>
        /// Triggers the Fullscreen mode
        /// </summary>
        private void EnterFullScreen()
        {
            //This currently not using real fullscreen mode but a borderless window
            //Real fullscreen could be gained setting _PresentParameters.Windowed = true
            //And calling Reset() and Init() after
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
        /// <summary>
        /// Triggers the windowed mode
        /// </summary>
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

        /// <summary>
        /// Inits the Device
        /// </summary>
        /// <returns>True if it succeeded else false</returns>
        public bool Init()
        {
            if (!_Device.Disposed)
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
                if (_PresentParameters.Multisample != MultisampleType.None)
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
            else
                return false;
        }
        /// <summary>
        /// Starts the rendering
        /// </summary>
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
                    //Clear the previous Frame
                    ClearScreen();
                    //We want to begin drawing
                    _Device.BeginScene();
                    CheckQueque();
                    _Run = _Run && CGraphics.Draw();
                    //We finished drawing the frame
                    _Device.EndScene();
                    _Run = CGraphics.UpdateGameLogic(_Keys, _Mouse);
                    try
                    {
                        //Now push the frame to the Viewport
                        _Device.Present();
                    }
                    catch (Direct3D9Exception)
                    {
                        //In Direct3D devices can get lost. 
                        //This happens for example when opening the Task Manager in Vista/Windows 7 or if a UAC message is opening
                        //We need to reset the device to get it to workable state again
                        //After a reset Init() needs to be called because all data in the Direct3D default pool are lost and need to be recreated
                        if (_Device.TestCooperativeLevel() == ResultCode.DeviceNotReset)
                        {
                            Reset();
                            Init();
                        }
                    }
                    //Apply fullscreen mode
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
                    //Calculate the FPS Rate and restart the timer after a frame
                    CTime.CalculateFPS();
                    CTime.Restart();
                }
                else
                    this.Close();
            }
        }

        /// <summary>
        /// Resets the device, all objects in the Direct3D default pool get flushed and need to be recreated
        /// </summary>
        public void Reset()
        {
            //Dispose all objects in the default pool, those need to be recreated
            TexturedColoredVertex.GetDeclaration(_Device).Dispose();
            _VertexBuffer.Dispose();
            _Device.Reset(_PresentParameters);
        }

        /// <summary>
        /// Unloads all Textures and other objects used by Direct3D for rendering
        /// </summary>
        /// <returns></returns>
        public bool Unload()
        {
            //Dispose all textures
            for (int i = 0; i < _D3DTextures.Count; i++)
            {
                if(_D3DTextures[i] != null)
                    _D3DTextures[i].Dispose();
            }
            TexturedColoredVertex.GetDeclaration(_Device).Dispose();
            _VertexBuffer.Dispose();
            _Device.Dispose();
            _D3D.Dispose();
            return true;
        }

        /// <summary>
        /// Gets the current viewport width
        /// </summary>
        /// <returns>The current viewport width</returns>
        public int GetScreenWidth()
        {
            return _Device.Viewport.Width;
        }

        /// <summary>
        /// Gets the current viewport height
        /// </summary>
        /// <returns>The current viewport height</returns>
        public int GetScreenHeight()
        {
            return _Device.Viewport.Height;
        }

        /// <summary>
        /// Calculates the bounds for a CText object
        /// </summary>
        /// <param name="text">The CText object of which the bounds should be calculated for</param>
        /// <returns>RectangleF object containing the bounds</returns>
        public RectangleF GetTextBounds(CText text)
        {
            return GetTextBounds(text, text.Height);
        }

        /// <summary>
        /// Calculates the bounds for a CText object
        /// </summary>
        /// <param name="text">The CText object of which the bounds should be calculated for</param>
        /// <param name="Height">The height of the CText object</param>
        /// <returns>RectangleF object containing the bounds</returns>
        public RectangleF GetTextBounds(CText text, float Height)
        {
            CFonts.Height = Height;
            CFonts.SetFont(text.Fon);
            CFonts.Style = text.Style;
            return new RectangleF(text.X, text.Y, CFonts.GetTextWidth(CLanguage.Translate(text.Text)), CFonts.GetTextHeight(text.Text));
        }

        #region Basic Draw Methods

        /// <summary>
        /// Removes all textures from the screen
        /// </summary>
        public void ClearScreen()
        {
            _Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
        }

        /// <summary>
        /// Not implemented yet
        /// </summary>
        /// <returns></returns>
        public STexture CopyScreen()
        {
            //FIXME
            STexture texture = new STexture(-1);
            return texture;
        }

        /// <summary>
        /// Not implemented yet
        /// </summary>
        /// <returns></returns>
        public void CopyScreen(ref STexture Texture)
        {
            //FIXME
            return;
        }

        /// <summary>
        /// Creates a Screenshot of the current frame
        /// </summary>
        public void MakeScreenShot()
        {
            string file = "Screenshot_";
            string path = Path.Combine(Environment.CurrentDirectory, CSettings.sFolderScreenshots);

            int i = 0;
            while (File.Exists(Path.Combine(path, file + i.ToString("00000") + ".bmp")))
                i++;

            //create a surface of the frame
            using (Surface surface = _Device.GetBackBuffer(0, 0))
            {
                Bitmap screen = new Bitmap(Surface.ToStream(surface, ImageFileFormat.Bmp));
                screen.Save(Path.Combine(path, file + i.ToString("00000") + ".bmp"), ImageFormat.Bmp);
                screen.Dispose();
            }
            Cursor.Hide();
            _Mouse.Visible = true;
        }

        /// <summary>
        /// Draws a line
        /// </summary>
        /// <param name="a">The alpha value from 0-255</param>
        /// <param name="r">The red value from 0-255</param>
        /// <param name="g">The red value from 0-255</param>
        /// <param name="b">The red value from 0-255</param>
        /// <param name="w">The width of the line</param>
        /// <param name="x1">The start x-value</param>
        /// <param name="y1">The start y-value</param>
        /// <param name="x2">The end x-value</param>
        /// <param name="y2">The end y-value</param>
        public void DrawLine(int a, int r, int g, int b, int w, int x1, int y1, int x2, int y2)
        {
            SColorF color = new SColorF(a / 255, g / 255, b / 255, a / 255);
            DrawTexture(blankTexture, new SRectF(x1, y1, x2, y2, 1), color);
        }

        /// <summary>
        /// Draws a text string
        /// </summary>
        /// <param name="Text">The text to be drawn</param>
        /// <param name="x">The text's x-position</param>
        /// <param name="y">The text's y-position</param>
        /// <param name="h">The text's height</param>
        public void DrawText(string Text, int x, int y, int h)
        {
            DrawText(Text, x, y, h, 0f);
        }

        /// <summary>
        /// Draws a text string
        /// </summary>
        /// <param name="Text">The text to be drawn</param>
        /// <param name="x">The text's x-position</param>
        /// <param name="y">The text's y-position</param>
        /// <param name="h">The text's height</param>
        /// <param name="z">The text's x-position</param>
        public void DrawText(string Text, int x, int y, float h, float z)
        {
            CFonts.DrawText(Text, h, x, y, z, new SColorF(1f, 1f, 1f, 1f));
        }

        /// <summary>
        /// Draws a colored rectangle 
        /// </summary>
        /// <param name="color">The color in which the rectangle will be drawn in</param>
        /// <param name="rect">The coordinates in a SRectF struct</param>
        public void DrawColor(SColorF color, SRectF rect)
        {
            //blankMap = new Bitmap(1, 1);
            //Graphics g = Graphics.FromImage(blankMap);
            //g.Clear(Color.White);
            //g.Dispose();
            //blankTexture = AddTexture(blankMap);
            //blankMap.Dispose();

            DrawTexture(blankTexture, rect, color);
            //RemoveTexture(ref blankTexture);
        }
        #endregion Basic Draw Methods
        #region Textures

        /// <summary>
        /// Adds a texture and stores it in the VRam
        /// </summary>
        /// <param name="TexturePath">The texture's filepath</param>
        /// <returns>A STexture object containing the added texture</returns>
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

        /// <summary>
        /// Calculates the next power of two
        /// </summary>
        /// <param name="n">The value of which the next power of two will be calculated</param>
        /// <returns>The next power of two</returns>
        private static float NextPowerOfTwo(float n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException("n", "Must be positive.");
            return (float)System.Math.Pow(2, System.Math.Ceiling(System.Math.Log((double)n, 2)));
        }

        /// <summary>
        /// Adds a texture and stores it in the Vram
        /// </summary>
        /// <param name="bmp">The Bitmap of which the texute will be created from</param>
        /// <param name="TexturePath">The path to the texture</param>
        /// <returns>A STexture object containing the added texture</returns>
        public STexture AddTexture(Bitmap bmp, string TexturePath)
        {
            STexture texture = new STexture(-1);
            if (bmp.Height == 0 || bmp.Width == 0)
                return texture;
            int MaxSize;
            //Apply the right max size
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

            //Older graphics card can only handle textures with sizes being powers of two
            texture.w2 = (float)NextPowerOfTwo(w);
            texture.h2 = (float)NextPowerOfTwo(h);

            //Create a new Bitmap with the new sizes
            Bitmap bmp2 = new Bitmap((int)texture.w2, (int)texture.h2);
            //Scale the texture
            Graphics g = Graphics.FromImage(bmp2);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.DrawImage(bmp, new Rectangle(0, 0, bmp2.Width, bmp2.Height));
            g.Dispose();

            texture.width_ratio = 1f;
            texture.height_ratio = 1f;

            //Fill the new Bitmap with the texture data
            BitmapData bmp_data = bmp2.LockBits(new Rectangle(0, 0, bmp2.Width, bmp2.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            byte[] Data = new byte[4 * bmp2.Width * bmp2.Height];
            Marshal.Copy(bmp_data.Scan0, Data, 0, Data.Length);
            bmp2.UnlockBits(bmp_data);

            //Create a new texture in the managed pool, which does not need to be recreated on a lost device
            //because a copy of the texture is hold in the Ram
            Texture t = new Texture(_Device, (int)texture.w2, (int)texture.h2, 0, Usage.AutoGenerateMipMap, Format.A8R8G8B8, Pool.Managed);
            //Lock the texture and fill it with the data
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
            lock (MutexTexture)
            {
                _Textures.Add(texture);
            }

            return texture;
        }

        /// <summary>
        /// Adds a texture and stores it in the Vram
        /// </summary>
        /// <param name="bmp">The Bitmap of which the texute will be created from</param>
        /// <returns>A STexture object containing the added texture</returns>
        public STexture AddTexture(Bitmap bmp)
        {
            return AddTexture(bmp, String.Empty);
        }

        public STexture AddTexture(int W, int H, IntPtr Data)
        {
            byte[] PointerData = new Byte[4 * W * H];
            Marshal.Copy(Data, PointerData, 0, PointerData.Length);
            return AddTexture(W, H, ref PointerData);
        }

        public STexture QuequeTexture(int W, int H, ref byte[] Data)
        {
            STexture texture = new STexture(-1);
            STextureQueque queque = new STextureQueque();

            queque.data = Data;
            queque.height = H;
            queque.width = W;

            lock (MutexTexture)
            {
                _D3DTextures.Add(null); //_D3DTextures.Add(transparentTexture);
                texture.index = _D3DTextures.Count - 1;
                queque.ID = texture.index;
                _Queque.Add(queque);
                _Textures.Add(texture);
            }
            return texture;
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

            lock (MutexTexture)
            {
                _Textures.Add(texture);
            }
            return texture;
        }

        /// <summary>
        /// Updates the data of a texture
        /// </summary>
        /// <param name="Texture">The texture to update</param>
        /// <param name="Data">A Pointer containing the data of which the texture should be updated</param>
        /// <returns>True if succeeded</returns>
        public bool UpdateTexture(ref STexture Texture, IntPtr Data)
        {
            byte[] PointerData = new Byte[4 * (int)Texture.width * (int)Texture.height];
            Marshal.Copy(Data, PointerData, 0, PointerData.Length);

            return UpdateTexture(ref Texture, ref PointerData);
        }

        /// <summary>
        /// Updates the data of a texture
        /// </summary>
        /// <param name="Texture">The texture to update</param>
        /// <param name="Data">A byte array containing the new texture's data</param>
        /// <returns>True if succeeded</returns>
        public bool UpdateTexture(ref STexture Texture, ref byte[] Data)
        {
            lock (MutexTexture)
            {
                if ((Texture.index >= 0) && (_Textures.Count > 0) && (_D3DTextures.Count > Texture.index) && _TextureExists(ref Texture))
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
                    return true;
                }
                else
                    return false;
            }
        }

        /// <summary>
        /// Checks if a texture exists
        /// </summary>
        /// <param name="Texture">The texture to check</param>
        /// <returns>True if the texture exists</returns>
        private bool _TextureExists(ref STexture Texture)
        {
            lock (MutexTexture)
            {
                for (int i = 0; i < _Textures.Count; i++)
                {
                    if (Texture.index == _Textures[i].index)
                    {
                        if (_Textures[i].index >= 0)
                        {
                            Texture = _Textures[i];
                            return true;
                        }
                        break;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Removes a texture from the Vram
        /// </summary>
        /// <param name="Texture">The texture to be removed</param>
        public void RemoveTexture(ref STexture Texture)
        {
            lock (MutexTexture)
            {
                if ((Texture.index >= 0) && (_Textures.Count > 0))
                {
                    for (int i = 0; i < _Textures.Count; i++)
                    {
                        if (_Textures[i].index == Texture.index)
                        {
                            _D3DTextures[Texture.index].Dispose();
                            _D3DTextures[Texture.index] = null;
                            _Textures.RemoveAt(i);
                            Texture.index = -1;
                            Texture.ID = -1;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draws a texture
        /// </summary>
        /// <param name="Texture">The texture to be drawn</param>
        public void DrawTexture(STexture Texture)
        {
            //lock (MutexTexture)
            {
                if ((Texture.index >= 0) && (_Textures.Count > 0) && (_D3DTextures.Count > Texture.index))
                    DrawTexture(Texture, Texture.rect, Texture.color);
            }
        }

        /// <summary>
        /// Draws a texture
        /// </summary>
        /// <param name="Texture">The texture to be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        public void DrawTexture(STexture Texture, SRectF rect)
        {
            //lock (MutexTexture)
            {
                if ((Texture.index >= 0) && (_Textures.Count > 0) && (_D3DTextures.Count > Texture.index))
                    DrawTexture(Texture, rect, Texture.color, false);
            }
        }

        /// <summary>
        /// Draws a texture
        /// </summary>
        /// <param name="Texture">The texture to be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        /// <param name="color">A SColorF struct containing a color which the texture will be colored in</param>
        public void DrawTexture(STexture Texture, SRectF rect, SColorF color)
        {
            //lock (MutexTexture)
            {
                if ((Texture.index >= 0) && (_Textures.Count > 0) && (_D3DTextures.Count > Texture.index))
                    DrawTexture(Texture, rect, color, false);
            }
        }

        /// <summary>
        /// Draws a texture
        /// </summary>
        /// <param name="Texture">The texture to be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        /// <param name="color">A SColorF struct containing a color which the texture will be colored in</param>
        /// <param name="bounds">A SRectF struct containing which part of the texture should be drawn</param>
        public void DrawTexture(STexture Texture, SRectF rect, SColorF color, SRectF bounds)
        {
            //lock (MutexTexture)
            {
                if ((Texture.index >= 0) && (_Textures.Count > 0) && (_D3DTextures.Count > Texture.index))
                    DrawTexture(Texture, rect, color, bounds, false);
            }
        }

        /// <summary>
        /// Draws a texture
        /// </summary>
        /// <param name="Texture">The texture to be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        /// <param name="color">A SColorF struct containing a color which the texture will be colored in</param>
        /// <param name="mirrored">True if the texture should be mirrored</param>
        public void DrawTexture(STexture Texture, SRectF rect, SColorF color, bool mirrored)
        {
            //lock (MutexTexture)
            {
                if ((Texture.index >= 0) && (_Textures.Count > 0) && (_D3DTextures.Count > Texture.index))
                    DrawTexture(Texture, rect, color, new SRectF(0, 0, CSettings.iRenderW, CSettings.iRenderH, rect.Z), mirrored);
            }
        }

        /// <summary>
        /// Draws a texture
        /// </summary>
        /// <param name="Texture">The texture to be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        /// <param name="color">A SColorF struct containing a color which the texture will be colored in</param>
        /// <param name="bounds">A SRectF struct containing which part of the texture should be drawn</param>
        /// <param name="mirrored">True if the texture should be mirrored</param>
        public void DrawTexture(STexture Texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored)
        {
            //lock (MutexTexture)
            {
                if ((Texture.index >= 0) && (_Textures.Count > 0) && (_D3DTextures.Count > Texture.index) && _TextureExists(ref Texture))
                {
                    //Calculate the position
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

                    //Calculate the size
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

                    //Align the pixels because Direct3D expects the pixels to be the left top corner
                    rx1 -= 0.5f;
                    ry1 -= 0.5f;
                    rx2 -= 0.5f;
                    ry2 -= 0.5f;

                    if (color.R >= 1f)
                        color.R = 1f;
                    if (color.B >= 1f)
                        color.B = 1f;
                    if (color.G >= 1f)
                        color.G = 1f;

                    //Apply rotation
                    //FIXME: Rotated textures get distorted when they hit the border of the window
                    if (rect.Rotation != 0)
                    {
                        rect.Rotation = rect.Rotation * (float)Math.PI / 180;
                        float centerX = (rx1 + rx2) / 2f;
                        float centerY = -(ry1 + ry2) / 2f;

                        Matrix originTranslation = _Device.GetTransform(TransformState.World);
                        Matrix translationA = Matrix.Translation(-centerX, -centerY, 0);
                        Matrix rotation = Matrix.RotationZ(-rect.Rotation);
                        Matrix translationB = Matrix.Translation(centerX, centerY, 0);

                        //Multiplicate the matrices to get the real world matrix,
                        //First shift the texture into the center
                        //Rotate it and shift it back to the origin position
                        //Apply the originTranslation after
                        Matrix result = translationA * rotation * translationB * originTranslation;

                        _Device.SetTransform(TransformState.World, result);
                    }

                    Color c = Color.FromArgb((int)(color.A * 255 * CGraphics.GlobalAlpha), (int)(color.R * 255), (int)(color.G * 255), (int)(color.B * 255));

                    //Create new vertices and write them into a vertexbuffer
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
                    //Set the texture
                    _Device.SetTexture(0, _D3DTextures[Texture.index]);
                    //Draw 2 triangles to build a quad
                    _Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);

                    //Reapply old matrices if the texture was rotated
                    if (rect.Rotation != 0)
                    {
                        Matrix originTranslation = Matrix.Translation(new Vector3(-CSettings.iRenderW / 2, CSettings.iRenderH / 2, 0));
                        _Device.SetTransform(TransformState.World, originTranslation);
                    }
                }
            }
        }

        /// <summary>
        /// Draws a texture
        /// </summary>
        /// <param name="Texture">The texture to be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        /// <param name="color">A SColorF struct containing a color which the texture will be colored in</param>
        /// <param name="begin">A Value ranging from 0 to 1 containing the beginning of the texture</param>
        /// <param name="end">A Value ranging from 0 to 1 containing the ending of the texture</param>

        public void DrawTexture(STexture Texture, SRectF rect, SColorF color, float begin, float end)
        {
            //lock (MutexTexture)
            {
                if (_TextureExists(ref Texture))
                {
                    float x1 = 0f + begin * Texture.width_ratio;
                    float x2 = Texture.width_ratio * end;
                    float y1 = 0f;
                    float y2 = Texture.height_ratio;

                    float rx1 = rect.X + begin * rect.W;
                    float rx2 = rect.X + end * rect.W;
                    float ry1 = rect.Y;
                    float ry2 = rect.Y + rect.H;

                    //Align the pixels because Direct3D expects the pixels to be the left top corner
                    rx1 -= 0.5f;
                    ry1 -= 0.5f;
                    rx2 -= 0.5f;
                    ry2 -= 0.5f;

                    Color c = Color.FromArgb((int)(color.A * 255 * CGraphics.GlobalAlpha), (int)(color.R * 255), (int)(color.G * 255), (int)(color.B * 255));

                    //Apply rotation
                    //FIXME: Rotated textures get distorted when they hit the border of the window
                    if (rect.Rotation != 0)
                    {
                        rect.Rotation = rect.Rotation * (float)Math.PI / 180;
                        float centerX = (rx1 + rx2) / 2f;
                        float centerY = -(ry1 + ry2) / 2f;

                        Matrix originTranslation = _Device.GetTransform(TransformState.World);
                        Matrix translationA = Matrix.Translation(-centerX, -centerY, 0);
                        Matrix rotation = Matrix.RotationZ(-rect.Rotation);
                        Matrix translationB = Matrix.Translation(centerX, centerY, 0);

                        //Multiplicate the matrices to get the real world matrix,
                        //First shift the texture into the center
                        //Rotate it and shift it back to the origin position
                        //Apply the originTranslation after
                        Matrix result = translationA * rotation * translationB * originTranslation;

                        _Device.SetTransform(TransformState.World, result);
                    }

                    //Create new vertices and write them into a vertexbuffer
                    DataStream stream = _VertexBuffer.Lock(0, 0, LockFlags.Discard);
                    stream.WriteRange(new[] {
	                    new TexturedColoredVertex(new Vector3(rx1, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x1, y1), c.ToArgb()),
	                    new TexturedColoredVertex(new Vector3(rx1, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x1, y2), c.ToArgb()),
	                    new TexturedColoredVertex(new Vector3(rx2, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x2, y2), c.ToArgb()),
                        new TexturedColoredVertex(new Vector3(rx2, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x2, y1), c.ToArgb()),
	                });
                    _VertexBuffer.Unlock();
                    stream.Dispose();
                    //Set the texture
                    _Device.SetTexture(0, _D3DTextures[Texture.index]);
                    //Draw 2 triangles to build a quad
                    _Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);

                    //Reapply old matrices if the texture was rotated
                    if (rect.Rotation != 0)
                    {
                        Matrix originTranslation = Matrix.Translation(new Vector3(-CSettings.iRenderW / 2, CSettings.iRenderH / 2, 0));
                        _Device.SetTransform(TransformState.World, originTranslation);
                    }
                }
            }
        }

        /// <summary>
        /// Draws a reflection of a texture
        /// </summary>
        /// <param name="Texture">The texture of which a reflection should be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        /// <param name="color">A SColorF struct containing a color which the texture will be colored in</param>
        /// <param name="bounds">A SRectF struct containing which part of the texture should be drawn</param>
        /// <param name="space">The space between the texture and the reflection</param>
        /// <param name="height">The height of the reflection</param>
        public void DrawTextureReflection(STexture Texture, SRectF rect, SColorF color, SRectF bounds, float space, float height)
        {
            //lock (MutexTexture)
            {
                if (_TextureExists(ref Texture))
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

                        //Align the pixels because Direct3D expects the pixels to be the left top corner
                        rx1 -= 0.5f;
                        ry1 -= 0.5f;
                        rx2 -= 0.5f;
                        ry2 -= 0.5f;

                        //Apply rotation
                        //FIXME: Rotated textures get distorted when they hit the border of the window
                        if (rect.Rotation != 0)
                        {
                            rect.Rotation = rect.Rotation * (float)Math.PI / 180;
                            float centerX = (rx1 + rx2) / 2f;
                            float centerY = -(ry1 + ry2) / 2f;

                            Matrix originTranslation = _Device.GetTransform(TransformState.World);
                            Matrix translationA = Matrix.Translation(-centerX, -centerY, 0);
                            Matrix rotation = Matrix.RotationZ(-rect.Rotation);
                            Matrix translationB = Matrix.Translation(centerX, centerY, 0);

                            //Multiplicate the matrices to get the real world matrix,
                            //First shift the texture into the center
                            //Rotate it and shift it back to the origin position
                            //Apply the originTranslation after
                            Matrix result = translationA * rotation * translationB * originTranslation;

                            _Device.SetTransform(TransformState.World, result);
                        }

                        Color c = Color.FromArgb((int)(color.A * 255 * CGraphics.GlobalAlpha), (int)(color.R * 255), (int)(color.G * 255), (int)(color.B * 255));
                        Color transparent = Color.FromArgb(0, (int)(color.R * 255), (int)(color.G * 255), (int)(color.B * 255));

                        //Create new vertices and write them into a vertexbuffer
                        DataStream stream = _VertexBuffer.Lock(0, 0, LockFlags.Discard);
                        stream.WriteRange(new[] {
                    new TexturedColoredVertex(new Vector3(rx2, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x2, y2), c.ToArgb()),
	                new TexturedColoredVertex(new Vector3(rx2, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x2, y1), transparent.ToArgb()),
	                new TexturedColoredVertex(new Vector3(rx1, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x1, y1), transparent.ToArgb()),
                    new TexturedColoredVertex(new Vector3(rx1, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x1, y2), c.ToArgb()),
	            });
                        _VertexBuffer.Unlock();
                        stream.Dispose();
                        //Set the texture
                        _Device.SetTexture(0, _D3DTextures[Texture.index]);
                        //Draw 2 triangles to build a quad
                        _Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);

                        //Reapply old matrices if the texture was rotated
                        if (rect.Rotation != 0)
                        {
                            Matrix originTranslation = Matrix.Translation(new Vector3(-CSettings.iRenderW / 2, CSettings.iRenderH / 2, 0));
                            _Device.SetTransform(TransformState.World, originTranslation);
                        }
                    }
                }
            }
        }

        private void CheckQueque()
        {
            lock (MutexTexture)
            {
                if (_Queque.Count == 0)
                    return;

                STextureQueque q = _Queque[0];
                STexture texture = new STexture(-1);
                int index = -1;
                for (int i = 0; i < _Textures.Count; i++)
                {
                    if (_Textures[i].index == q.ID)
                    {
                        texture = _Textures[i];
                        index = i;
                        break;
                    }
                }

                if (texture.index < 0)
                    return;

                texture.width = q.width;
                texture.height = q.height;
                texture.w2 = NextPowerOfTwo(texture.width);
                texture.h2 = NextPowerOfTwo(texture.height);

                texture.width_ratio = texture.width / texture.w2;
                texture.height_ratio = texture.height / texture.h2;

                Texture t = new Texture(_Device, (int)texture.w2, (int)texture.h2, 0, Usage.AutoGenerateMipMap, Format.A8R8G8B8, Pool.Managed);
                DataRectangle rect = t.LockRectangle(0, LockFlags.None);
                for (int i = 0; i < q.data.Length; )
                {
                    rect.Data.Write(q.data, i, 4 * q.width);
                    i += 4 * q.width;
                    rect.Data.Position = rect.Data.Position - 4 * q.width;
                    rect.Data.Position += rect.Pitch;
                }
                t.UnlockRectangle(0);
                _D3DTextures[q.ID] = t;

                // Add to Texture List
                texture.color = new SColorF(1f, 1f, 1f, 1f);
                texture.rect = new SRectF(0f, 0f, texture.width, texture.height, 0f);
                texture.TexturePath = String.Empty;

                _Textures[index] = texture;
                _Queque.RemoveAt(0);
            }
        }

        /// <summary>
        /// Gets the count of current textures
        /// </summary>
        /// <returns>The amount of textures</returns>
        public int TextureCount()
        {
            lock (MutexTexture)
            {
                return _Textures.Count;
            }
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
