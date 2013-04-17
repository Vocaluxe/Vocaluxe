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
using SlimDX;
using SlimDX.Direct3D9;
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
    class CDirect3D : Form, IDraw
    {
        #region private vars
        private readonly CKeys _Keys;
        private readonly CMouse _Mouse;
        private bool _Run;

        private readonly Direct3D _D3D;
        private readonly Device _Device;
        private readonly PresentParameters _PresentParameters;

        private bool _Fullscreen;
        private Size _OldSize;
        private Size _SizeBeforeMinimize;

        private readonly Dictionary<int, STexture> _Textures;
        private readonly Dictionary<int, Texture> _D3DTextures;
        private readonly List<STextureQueque> _Queque;
        private readonly Queue<int> _IDs;

        private readonly Object _MutexTexture = new Object();

        private VertexBuffer _VertexBuffer;
        private IndexBuffer _IndexBuffer;

        private int _H = 1;
        private int _W = 1;
        private int _Y;
        private int _X;

        private STexture _BlankTexture;

        private readonly Queue<STexturedColoredVertex> _Vertices;
        private readonly Queue<Texture> _VerticesTextures;
        private readonly Queue<SlimDX.Matrix> _VerticesRotationMatrices;

        private readonly bool _NonPowerOf2TextureSupported;
        #endregion private vars

        /// <summary>
        ///     Creates a new Instance of the CDirect3D Class
        /// </summary>
        public CDirect3D()
        {
            Icon = new Icon(Path.Combine(Environment.CurrentDirectory, CSettings.Icon));
            _Textures = new Dictionary<int, STexture>();
            _D3DTextures = new Dictionary<int, Texture>();
            _Queque = new List<STextureQueque>();
            _IDs = new Queue<int>();

            //Fill Queue with 100000 IDs
            for (int i = 0; i < 100000; i++)
                _IDs.Enqueue(i);

            _Vertices = new Queue<STexturedColoredVertex>();
            _VerticesTextures = new Queue<Texture>();
            _VerticesRotationMatrices = new Queue<SlimDX.Matrix>();

            _Keys = new CKeys();
            try
            {
                _D3D = new Direct3D();
            }
            catch (Direct3D9NotFoundException e)
            {
                MessageBox.Show("No DirectX runtimes were found, please download and install them " +
                                "from http://www.microsoft.com/download/en/details.aspx?id=8109",
                                CSettings.ProgramName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                CLog.LogError(e.Message + " - No DirectX runtimes were found, please download and install them from http://www.microsoft.com/download/en/details.aspx?id=8109");
                Environment.Exit(Environment.ExitCode);
            }

            Paint += _OnPaintEvent;
            Closing += _OnClosingEvent;
            Resize += _OnResizeEvent;

            KeyDown += _OnKeyDown;
            PreviewKeyDown += _OnPreviewKeyDown;
            KeyPress += _OnKeyPress;
            KeyUp += _OnKeyUp;

            _Mouse = new CMouse();
            MouseMove += _OnMouseMove;
            MouseWheel += _OnMouseWheel;
            MouseDown += _OnMouseDown;
            MouseUp += _OnMouseUp;
            MouseLeave += _OnMouseLeave;
            MouseEnter += _OnMouseEnter;

            ClientSize = new Size(CConfig.ScreenW, CConfig.ScreenH);
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
            int quality = 1;
            MultisampleType msType;
            switch (CConfig.AAMode)
            {
                case EAntiAliasingModes.X2:
                    msType = MultisampleType.TwoSamples;
                    break;
                case EAntiAliasingModes.X4:
                    msType = MultisampleType.FourSamples;
                    break;
                case EAntiAliasingModes.X8:
                    msType = MultisampleType.EightSamples;
                    break;
                case EAntiAliasingModes.X16:
                case EAntiAliasingModes.X32: //x32 is not supported, fallback to x16
                    msType = MultisampleType.SixteenSamples;
                    break;
                default:
                    msType = MultisampleType.None;
                    break;
            }

            if (
                !_D3D.CheckDeviceMultisampleType(_D3D.Adapters.DefaultAdapter.Adapter, DeviceType.Hardware, _D3D.Adapters.DefaultAdapter.CurrentDisplayMode.Format, false, msType,
                                                 out quality))
            {
                CLog.LogError("[Direct3D] This AAMode is not supported by this device or driver, fallback to no AA");
                msType = MultisampleType.None;
                quality = 1;
            }

            _PresentParameters.Multisample = msType;
            _PresentParameters.MultisampleQuality = quality - 1;
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

            //Check if Pow2 textures are needed
            _NonPowerOf2TextureSupported = true;
            _NonPowerOf2TextureSupported &= (caps.TextureCaps & TextureCaps.Pow2) == 0;
            _NonPowerOf2TextureSupported &= (caps.TextureCaps & TextureCaps.NonPow2Conditional) == 0;
            _NonPowerOf2TextureSupported &= (caps.TextureCaps & TextureCaps.SquareOnly) == 0;

            try
            {
                _Device = new Device(_D3D, _D3D.Adapters.DefaultAdapter.Adapter, DeviceType.Hardware, Handle, flags, _PresentParameters);
            }
            finally
            {
                if (_Device == null || _Device.Disposed)
                {
                    MessageBox.Show("Something went wrong during device creating, please check if your DirectX redistributables " +
                                    "and graphic card drivers are up to date. You can download the DirectX runtimes at http://www.microsoft.com/download/en/details.aspx?id=8109",
                                    CSettings.ProgramName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    CLog.LogError(
                        "Something went wrong during device creating, please check if your DirectX redistributables and grafic card drivers are up to date. You can download the DirectX runtimes at http://www.microsoft.com/download/en/details.aspx?id=8109");
                    Environment.Exit(Environment.ExitCode);
                }
            }

            CenterToScreen();
        }

        #region form events
        private void _OnPaintEvent(object sender, PaintEventArgs e) {}

        private void _OnResizeEvent(object sender, EventArgs e) {}

        private void _OnClosingEvent(object sender, CancelEventArgs e)
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

        #region resize
        /// <summary>
        ///     Resizes the viewport
        /// </summary>
        private void _DoResize()
        {
            // The window was minimized, so restore it to the last known size
            if (ClientSize.Width == 0 || ClientSize.Height == 0)
                ClientSize = _SizeBeforeMinimize;
            if (_Run)
            {
                _H = ClientSize.Height;
                _W = ClientSize.Width;
                _Y = 0;
                _X = 0;

                if (_W / (float)_H > CSettings.GetRenderAspect())
                {
                    //The windows's width is too big
                    _W = (int)Math.Round(_H * CSettings.GetRenderAspect());
                    _X = (ClientSize.Width - _W) / 2;
                }
                else
                {
                    //The windows's height is too big
                    _H = (int)Math.Round(_W / CSettings.GetRenderAspect());
                    _Y = (ClientSize.Height - _H) / 2;
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
                _Device.Viewport = new Viewport(_X, _Y, _W, _H);
                //Store size so it can get restored after the window gets minimized
                _SizeBeforeMinimize = ClientSize;
            }
        }

        /// <summary>
        ///     Triggers the Fullscreen mode
        /// </summary>
        private void _EnterFullScreen()
        {
            //This currently not using real fullscreen mode but a borderless window
            //Real fullscreen could be gained setting _PresentParameters.Windowed = true
            //And calling Reset() and Init() after
            _OldSize = ClientSize;

            int screenNr = 0;
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                Screen scr = Screen.AllScreens[i];
                if (scr.Bounds.Top <= Top && scr.Bounds.Left <= Left)
                    screenNr = i;
            }

            ClientSize = new Size(Screen.AllScreens[screenNr].Bounds.Width, Screen.AllScreens[screenNr].Bounds.Height);
            FormBorderStyle = FormBorderStyle.None;
            CenterToScreen();
            _Fullscreen = true;
            CConfig.FullScreen = EOffOn.TR_CONFIG_ON;

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

        /// <summary>
        ///     Triggers the windowed mode
        /// </summary>
        private void _LeaveFullScreen()
        {
            ClientSize = _OldSize;
            FormBorderStyle = FormBorderStyle.Sizable;
            CenterToScreen();
            _Fullscreen = false;
            CConfig.FullScreen = EOffOn.TR_CONFIG_OFF;

            CConfig.SaveConfig();
        }
        #endregion resize

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
        private void _OnPreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            _OnKeyDown(sender, new KeyEventArgs(e.KeyData));
        }

        private void _OnKeyDown(object sender, KeyEventArgs e)
        {
            _Keys.KeyDown(e);
        }

        private void _OnKeyPress(object sender, KeyPressEventArgs e)
        {
            _Keys.KeyPress(e);
        }

        private void _OnKeyUp(object sender, KeyEventArgs e)
        {
            _Keys.KeyUp(e);
        }
        #endregion keyboard event handlers

        #region implementation

        #region main stuff
        /// <summary>
        ///     Inits the Device
        /// </summary>
        /// <returns>True if it succeeded else false</returns>
        public bool Init()
        {
            if (!_Device.Disposed)
            {
                Text = CSettings.GetFullVersionText();
                SlimDX.Matrix translate = SlimDX.Matrix.Translation(new Vector3(-CSettings.RenderW / 2, CSettings.RenderH / 2, 0));
                SlimDX.Matrix projection = SlimDX.Matrix.OrthoOffCenterLH(-CSettings.RenderW / 2, CSettings.RenderW / 2, -CSettings.RenderH / 2, CSettings.RenderH / 2,
                                                                          CSettings.ZNear, CSettings.ZFar);
                _VertexBuffer = new VertexBuffer(_Device, CSettings.VertexBufferElements * (4 * Marshal.SizeOf(typeof(STexturedColoredVertex))), Usage.WriteOnly | Usage.Dynamic,
                                                 VertexFormat.Position | VertexFormat.Texture1 | VertexFormat.Diffuse, Pool.Default);

                if (_Device.SetStreamSource(0, _VertexBuffer, 0, Marshal.SizeOf(typeof(STexturedColoredVertex))).IsFailure)
                    CLog.LogError("Failed to set stream source");
                _Device.VertexDeclaration = STexturedColoredVertex.GetDeclaration(_Device);
                if (_Device.SetTransform(TransformState.Projection, projection).IsFailure)
                    CLog.LogError("Failed to set orthogonal matrix");
                if (_Device.SetTransform(TransformState.World, translate).IsFailure)
                    CLog.LogError("Failed to set translation matrix");
                if (_Device.SetRenderState(RenderState.CullMode, Cull.None).IsFailure)
                    CLog.LogError("Failed to set cull mode");
                if (_Device.SetRenderState(RenderState.AlphaBlendEnable, true).IsFailure)
                    CLog.LogError("Failed to enable alpha blending");
                if (_Device.SetRenderState(RenderState.Lighting, false).IsFailure)
                    CLog.LogError("Failed to disable lighting");
                if (_Device.SetRenderState(RenderState.DestinationBlend, SlimDX.Direct3D9.Blend.InverseSourceAlpha).IsFailure)
                    CLog.LogError("Failed to set destination blend");
                if (_Device.SetRenderState(RenderState.SourceBlend, SlimDX.Direct3D9.Blend.SourceAlpha).IsFailure)
                    CLog.LogError("Failed to set source blend");
                if (_PresentParameters.Multisample != MultisampleType.None)
                {
                    if (_Device.SetRenderState(RenderState.MultisampleAntialias, true).IsFailure)
                        CLog.LogError("Failed to set antialiasing");
                }
                if (_Device.SetSamplerState(0, SamplerState.MinFilter, TextureFilter.Linear).IsFailure)
                    CLog.LogError("Failed to set min filter");
                if (_Device.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.Linear).IsFailure)
                    CLog.LogError("Failed to set mag filter");
                if (_Device.SetSamplerState(0, SamplerState.MipFilter, TextureFilter.Linear).IsFailure)
                    CLog.LogError("Failed to set mip filter");
                if (_Device.SetSamplerState(0, SamplerState.AddressU, TextureAddress.Clamp).IsFailure)
                    CLog.LogError("Failed to set clamping on u");
                if (_Device.SetSamplerState(0, SamplerState.AddressV, TextureAddress.Clamp).IsFailure)
                    CLog.LogError("Failed to set claming on v");
                if (_Device.SetTextureStageState(0, TextureStage.AlphaArg1, TextureArgument.Texture).IsFailure)
                    CLog.LogError("Failed to set alpha argument 1");
                if (_Device.SetTextureStageState(0, TextureStage.AlphaArg2, TextureArgument.Diffuse).IsFailure)
                    CLog.LogError("Failed to set alpha argument 2");
                if (_Device.SetTextureStageState(0, TextureStage.AlphaOperation, TextureOperation.Modulate).IsFailure)
                    CLog.LogError("Failed to set alpha operation");

                Int16[] indices = new Int16[6];
                indices[0] = 0;
                indices[1] = 1;
                indices[2] = 2;
                indices[3] = 0;
                indices[4] = 2;
                indices[5] = 3;

                _IndexBuffer = new IndexBuffer(_Device, 6 * sizeof(Int16), Usage.WriteOnly, Pool.Managed, true);

                DataStream stream = _IndexBuffer.Lock(0, 0, LockFlags.Discard);
                stream.WriteRange(indices);
                _IndexBuffer.Unlock();
                _Device.Indices = _IndexBuffer;

                //This creates a new white texture and adds it to the texture pool
                //This texture is used for the DrawRect method
                using (Bitmap blankMap = new Bitmap(1, 1))
                {
                    Graphics g = Graphics.FromImage(blankMap);
                    g.Clear(Color.White);
                    g.Dispose();
                    _BlankTexture = AddTexture(blankMap);
                }
                return true;
            }
            else
                return false;
        }

        /// <summary>
        ///     Starts the rendering
        /// </summary>
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

            //Resize window if aspect ratio is incorrect
            if (_W / (float)_H != CSettings.GetRenderAspect())
                _DoResize();

            while (_Run)
            {
                Application.DoEvents();

                if (_Run)
                {
                    //Clear the previous Frame
                    ClearScreen();
                    //We want to begin drawing
                    if (_Device.BeginScene().IsFailure)
                        CLog.LogError("Failed to begin scene");
                    _CheckQueque();
                    _Run = _Run && CGraphics.Draw();
                    _RenderVertexBuffer();
                    //We finished drawing the frame
                    if (_Device.EndScene().IsFailure)
                        CLog.LogError("Failed to end scene");
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
                    if (CSettings.IsFullScreen != _Fullscreen)
                    {
                        if (!_Fullscreen)
                            _EnterFullScreen();
                        else
                            _LeaveFullScreen();
                    }
                    if (CTime.IsRunning())
                        delay = (int)Math.Floor(CConfig.CalcCycleTime() - CTime.GetMilliseconds());

                    if (delay >= 1 && CConfig.VSync == EOffOn.TR_CONFIG_OFF)
                        Thread.Sleep(delay);
                    //Calculate the FPS Rate and restart the timer after a frame
                    CTime.CalculateFPS();
                    CTime.Restart();
                }
            }
            Close();
        }

        /// <summary>
        ///     Resets the device, all objects in the Direct3D default pool get flushed and need to be recreated
        /// </summary>
        public void Reset()
        {
            //Dispose all objects in the default pool, those need to be recreated
            STexturedColoredVertex.GetDeclaration(_Device).Dispose();
            _VertexBuffer.Dispose();
            _IndexBuffer.Dispose();
            if (_Device.Reset(_PresentParameters).IsFailure)
                CLog.LogError("Failed to reset the device");
        }

        /// <summary>
        ///     Unloads all Textures and other objects used by Direct3D for rendering
        /// </summary>
        /// <returns></returns>
        public bool Unload()
        {
            //Dispose all textures
            foreach (KeyValuePair<int, Texture> p in _D3DTextures)
            {
                if (p.Value != null)
                    p.Value.Dispose();
            }

            STexturedColoredVertex.GetDeclaration(_Device).Dispose();
            _VertexBuffer.Dispose();
            _IndexBuffer.Dispose();
            _Device.Dispose();
            _D3D.Dispose();
            return true;
        }

        /// <summary>
        ///     Gets the current viewport width
        /// </summary>
        /// <returns>The current viewport width</returns>
        public int GetScreenWidth()
        {
            return _Device.Viewport.Width;
        }

        /// <summary>
        ///     Gets the current viewport height
        /// </summary>
        /// <returns>The current viewport height</returns>
        public int GetScreenHeight()
        {
            return _Device.Viewport.Height;
        }

        /// <summary>
        ///     Calculates the bounds for a CText object
        /// </summary>
        /// <param name="text">The CText object of which the bounds should be calculated for</param>
        /// <returns>RectangleF object containing the bounds</returns>
        public RectangleF GetTextBounds(CText text)
        {
            return GetTextBounds(text, text.Height);
        }

        /// <summary>
        ///     Calculates the bounds for a CText object
        /// </summary>
        /// <param name="text">The CText object of which the bounds should be calculated for</param>
        /// <param name="height">The height of the CText object</param>
        /// <returns>RectangleF object containing the bounds</returns>
        public RectangleF GetTextBounds(CText text, float height)
        {
            CFonts.Height = height;
            CFonts.SetFont(text.Font);
            CFonts.Style = text.Style;
            return new RectangleF(text.X, text.Y, CFonts.GetTextWidth(CLanguage.Translate(text.Text)), CFonts.GetTextHeight(CLanguage.Translate(text.Text)));
        }

        /// <summary>
        ///     Adds a quad a list which will be added and rendered to the vertexbuffer when calling RenderToVertexBuffer to reduce vertexbuffer calls each frame to a minimum
        /// </summary>
        /// <param name="vertices">A TexturedColoredVertex array containg 4 vertices</param>
        /// <param name="tex">The texture the vertex should be textured with</param>
        /// <param name="rotation">The vertices' rotation</param>
        private void _AddToVertexBuffer(STexturedColoredVertex[] vertices, Texture tex, SlimDX.Matrix rotation)
        {
            //The vertexbuffer is full, so we need to flush it before we can continue
            if (_Vertices.Count >= CSettings.VertexBufferElements)
                _RenderVertexBuffer();
            _Vertices.Enqueue(vertices[0]);
            _Vertices.Enqueue(vertices[1]);
            _Vertices.Enqueue(vertices[2]);
            _Vertices.Enqueue(vertices[3]);
            _VerticesTextures.Enqueue(tex);
            _VerticesRotationMatrices.Enqueue(rotation);
        }

        /// <summary>
        ///     Renders the vertex buffer
        /// </summary>
        private void _RenderVertexBuffer()
        {
            if (_Vertices.Count > 0)
            {
                //The vertex buffer locks are slow actions, its better to lock once per frame and write all vertices to the buffer at once
                DataStream stream = _VertexBuffer.Lock(0, _Vertices.Count * Marshal.SizeOf(typeof(STexturedColoredVertex)), LockFlags.Discard);
                stream.WriteRange(_Vertices.ToArray());
                _VertexBuffer.Unlock();
                stream.Dispose();

                for (int i = 0; i < _Vertices.Count; i += 4)
                {
                    //Apply rotation
                    if (_Device.SetTransform(TransformState.World, _VerticesRotationMatrices.Dequeue()).IsFailure)
                        CLog.LogError("Failed to set world transformation");
                    //Apply texture
                    if (_Device.SetTexture(0, _VerticesTextures.Dequeue()).IsFailure)
                        CLog.LogError("Failed to set texture");
                    //Draw 2 triangles from vertexbuffer
                    if (_Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, i, 0, 4, 0, 2).IsFailure)
                        CLog.LogError("Failed to draw quad");
                }
                //Clear the queues for the next frame
                _Vertices.Clear();
                _VerticesTextures.Clear();
                _VerticesRotationMatrices.Clear();
            }
        }
        #endregion main stuff

        #region Basic Draw Methods
        /// <summary>
        ///     Removes all textures from the screen
        /// </summary>
        public void ClearScreen()
        {
            if (_Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0).IsFailure)
                CLog.LogError("Failed to clear the backbuffer");
        }

        /// <summary>
        ///     Copies the current frame into a texture
        ///     <returns>A texture holding the current frame</returns>
        /// </summary>
        public STexture CopyScreen()
        {
            STexture texture = new STexture(-1);

            Surface backbufferSurface = _Device.GetBackBuffer(0, 0);
            Texture tex = new Texture(_Device, _PresentParameters.BackBufferWidth, _PresentParameters.BackBufferHeight, 0, Usage.AutoGenerateMipMap, Format.A8R8G8B8, Pool.Managed);
            Surface textureSurface = tex.GetSurfaceLevel(0);
            Surface.FromSurface(textureSurface, backbufferSurface, Filter.Default, 0);
            backbufferSurface.Dispose();
            lock (_MutexTexture)
            {
                _D3DTextures.Add(_IDs.Peek(), tex);

                texture.Color = new SColorF(1f, 1f, 1f, 1f);
                texture.Rect = new SRectF(0f, 0f, texture.Width, texture.Height, 0f);
                texture.Width = _W;
                texture.Height = _H;
                texture.W2 = _CheckForNextPowerOf2(texture.Width);
                texture.H2 = _CheckForNextPowerOf2(texture.Height);
                texture.Index = _IDs.Dequeue();

                _Textures[texture.Index] = texture;
            }
            return texture;
        }

        /// <summary>
        ///     Copies the current frame into a texture
        /// </summary>
        /// <param name="texture">The texture in which the frame is copied to</param>
        public void CopyScreen(ref STexture texture)
        {
            if (!_TextureExists(ref texture) || (texture.Width != GetScreenWidth()) || (texture.Height != GetScreenHeight()))
            {
                RemoveTexture(ref texture);
                texture = CopyScreen();
            }
            else
            {
                Surface backbufferSurface = _Device.GetBackBuffer(0, 0);
                Surface textureSurface = _D3DTextures[texture.Index].GetSurfaceLevel(0);
                Surface.FromSurface(textureSurface, backbufferSurface, Filter.Default, 0);
            }
        }

        /// <summary>
        ///     Creates a Screenshot of the current frame
        /// </summary>
        public void MakeScreenShot()
        {
            string file = "Screenshot_";
            string path = Path.Combine(Environment.CurrentDirectory, CSettings.FolderScreenshots);

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
        ///     Draws a line
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
            DrawTexture(_BlankTexture, new SRectF(x1, y1, x2, y2, 1), color);
        }

        /// <summary>
        ///     Draws a text string
        /// </summary>
        /// <param name="text">The text to be drawn</param>
        /// <param name="x">The text's x-position</param>
        /// <param name="y">The text's y-position</param>
        /// <param name="h">The text's height</param>
        public void DrawText(string text, int x, int y, int h)
        {
            DrawText(text, x, y, h, 0f);
        }

        /// <summary>
        ///     Draws a text string
        /// </summary>
        /// <param name="text">The text to be drawn</param>
        /// <param name="x">The text's x-position</param>
        /// <param name="y">The text's y-position</param>
        /// <param name="h">The text's height</param>
        /// <param name="z">The text's x-position</param>
        public void DrawText(string text, int x, int y, float h, float z)
        {
            CFonts.DrawText(text, h, x, y, z, new SColorF(1f, 1f, 1f, 1f));
        }

        /// <summary>
        ///     Draws a colored rectangle
        /// </summary>
        /// <param name="color">The color in which the rectangle will be drawn in</param>
        /// <param name="rect">The coordinates in a SRectF struct</param>
        public void DrawColor(SColorF color, SRectF rect)
        {
            DrawTexture(_BlankTexture, rect, color);
        }

        /// <summary>
        ///     Draws reflection of a colored rectangle
        /// </summary>
        /// <param name="color">The color in which the rectangle will be drawn in</param>
        /// <param name="rect">The coordinates in a SRectF struct</param>
        /// <param name="space">The space between the texture and the reflection</param>
        /// <param name="height">The height of the reflection</param>
        public void DrawColorReflection(SColorF color, SRectF rect, float space, float height)
        {
            DrawTextureReflection(_BlankTexture, rect, color, rect, space, height);
        }
        #endregion Basic Draw Methods

        #region Textures

        #region adding
        /// <summary>
        ///     Adds a texture and stores it in the VRam
        /// </summary>
        /// <param name="texturePath">The texture's filepath</param>
        /// <returns>A STexture object containing the added texture</returns>
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
                STexture s;
                try
                {
                    s = AddTexture(bmp, texturePath);
                }
                finally
                {
                    bmp.Dispose();
                }
                return s;
            }
            CLog.LogError("Can't find File: " + texturePath);
            return new STexture(-1);
        }

        /// <summary>
        ///     Adds a texture and stores it in the Vram
        /// </summary>
        /// <param name="bmp">The Bitmap of which the texure will be created from</param>
        /// <param name="texturePath">The path to the texture</param>
        /// <returns>A STexture object containing the added texture</returns>
        public STexture AddTexture(Bitmap bmp, string texturePath)
        {
            if (bmp.Height == 0 || bmp.Width == 0)
                return new STexture(-1);
            int maxSize;
            //Apply the right max size
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

            if (w > maxSize && w > h)
            {
                h = (int)Math.Round((float)maxSize / bmp.Width * bmp.Height);
                w = maxSize;
            }

            if (h > maxSize)
            {
                w = (int)Math.Round((float)maxSize / bmp.Height * bmp.Width);
                h = maxSize;
            }

            //Fill the new Bitmap with the texture data
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] data = new byte[4 * w * h];
            Marshal.Copy(bmpData.Scan0, data, 0, data.Length);
            bmp.UnlockBits(bmpData);

            return AddTexture(w, h, ref data);
        }

        /// <summary>
        ///     Adds a texture and stores it in the Vram
        /// </summary>
        /// <param name="bmp">The Bitmap of which the texure will be created from</param>
        /// <returns>A STexture object containing the added texture</returns>
        public STexture AddTexture(Bitmap bmp)
        {
            return AddTexture(bmp, String.Empty);
        }

        public STexture AddTexture(int w, int h, IntPtr data)
        {
            byte[] pointerData = new Byte[4 * w * h];
            Marshal.Copy(data, pointerData, 0, pointerData.Length);
            return AddTexture(w, h, ref pointerData);
        }

        public STexture AddTexture(int w, int h, ref byte[] data)
        {
            STexture texture = new STexture(-1);

            Texture t = _CreateTexture(w, h, ref data, ref texture);

            lock (_MutexTexture)
            {
                _D3DTextures.Add(_IDs.Peek(), t);
                texture.Index = _IDs.Dequeue();
                _Textures[texture.Index] = texture;
            }
            return texture;
        }

        private Texture _CreateTexture(int w, int h, ref byte[] data, ref STexture texture)
        {
            texture.Width = w;
            texture.Height = h;
            texture.W2 = _CheckForNextPowerOf2(w);
            texture.H2 = _CheckForNextPowerOf2(h);
            texture.WidthRatio = w / texture.W2;
            texture.HeightRatio = h / texture.H2;

            //Create a new texture in the managed pool, which does not need to be recreated on a lost device
            //because a copy of the texture is hold in the Ram
            Texture t = null;
            try
            {
                t = new Texture(_Device, (int)texture.W2, (int)texture.H2, 0, Usage.AutoGenerateMipMap, Format.A8R8G8B8, Pool.Managed);
                //Lock the texture and fill it with the data
                DataRectangle rect = t.LockRectangle(0, LockFlags.Discard);
                int rowWidth = 4 * (int)texture.W2;
                for (int i = 0; i + rowWidth <= data.Length; i += 4 * w)
                {
                    rect.Data.Write(data, i, rowWidth);
                    rect.Data.Position = rect.Data.Position - rowWidth;
                    rect.Data.Position += rect.Pitch;
                }
                t.UnlockRectangle(0);

                texture.Color = new SColorF(1f, 1f, 1f, 1f);
                texture.Rect = new SRectF(0f, 0f, texture.Width, texture.Height, 0f);
                texture.TexturePath = String.Empty;
            }
            catch (Exception)
            {
                if (t != null)
                    t.Dispose();
                throw;
            }
            return t;
        }

        public STexture QuequeTexture(int w, int h, ref byte[] data)
        {
            STexture texture = new STexture(-1);
            STextureQueque queque = new STextureQueque();

            queque.Data = data;
            queque.Height = h;
            queque.Width = w;
            texture.Height = h;
            texture.Width = w;

            lock (_MutexTexture)
            {
                _D3DTextures.Add(_IDs.Peek(), null);
                texture.Index = _IDs.Dequeue();
                queque.ID = texture.Index;
                _Queque.Add(queque);
                _Textures[texture.Index] = texture;
            }
            return texture;
        }
        #endregion adding

        #region updating
        /// <summary>
        ///     Updates the data of a texture
        /// </summary>
        /// <param name="texture">The texture to update</param>
        /// <param name="data">A Pointer containing the data of which the texture should be updated</param>
        /// <returns>True if succeeded</returns>
        public bool UpdateTexture(ref STexture texture, IntPtr data)
        {
            byte[] pointerData = new Byte[4 * (int)texture.Width * (int)texture.Height];
            Marshal.Copy(data, pointerData, 0, pointerData.Length);

            return UpdateTexture(ref texture, ref pointerData);
        }

        /// <summary>
        ///     Updates the data of a texture
        /// </summary>
        /// <param name="texture">The texture to update</param>
        /// <param name="data">A byte array containing the new texture's data</param>
        /// <returns>True if succeeded</returns>
        public bool UpdateTexture(ref STexture texture, ref byte[] data)
        {
            if ((texture.Index >= 0) && (_Textures.Count > 0) && _TextureExists(ref texture))
            {
                lock (_MutexTexture)
                {
                    DataRectangle rect = _D3DTextures[texture.Index].LockRectangle(0, LockFlags.Discard);
                    int w = (int)texture.Width;
                    for (int i = 0; i < data.Length; i += 4 * w)
                    {
                        rect.Data.Write(data, i, 4 * w);
                        rect.Data.Position -= 4 * w;
                        rect.Data.Position += rect.Pitch;
                    }
                    _D3DTextures[texture.Index].UnlockRectangle(0);
                }

                //Flamefire: Unneccesary as height can not be changed by this! (Texture gets copied)
                //Texture.height_ratio = Texture.height / CheckForNextPowerOf2(Texture.height);
                //Texture.width_ratio = Texture.width / CheckForNextPowerOf2(Texture.width);
                return true;

                /*Surface s = _D3DTextures[Texture.index].GetSurfaceLevel(0);
                DataRectangle d = s.LockRectangle(LockFlags.Discard);

                for (int i = 0; i < Data.Length; )
                {
                    d.Data.Write(Data, i, 4 * (int)Texture.width);
                    i += 4 * (int)Texture.width;
                    d.Data.Position = d.Data.Position - 4 * (int)Texture.width;
                    d.Data.Position += d.Pitch;
                }

                Texture.height_ratio = Texture.height / NextPowerOfTwo(Texture.height);
                Texture.width_ratio = Texture.width / NextPowerOfTwo(Texture.width);

                s.UnlockRectangle();
                return true; */
            }
            else
                return false;
        }
        #endregion updating

        /// <summary>
        ///     Checks if a texture exists
        /// </summary>
        /// <param name="texture">The texture to check</param>
        /// <returns>True if the texture exists</returns>
        private bool _TextureExists(ref STexture texture)
        {
            lock (_MutexTexture)
            {
                if (_Textures.ContainsKey(texture.Index))
                {
                    if (_Textures[texture.Index].Index >= 0)
                    {
                        texture = _Textures[texture.Index];
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        ///     Removes a texture from the Vram
        /// </summary>
        /// <param name="texture">The texture to be removed</param>
        public void RemoveTexture(ref STexture texture)
        {
            lock (_MutexTexture)
            {
                if ((texture.Index >= 0) && (_Textures.Count > 0))
                {
                    _D3DTextures[texture.Index].Dispose();
                    _D3DTextures.Remove(texture.Index);
                    _Textures.Remove(texture.Index);
                    _IDs.Enqueue(texture.Index);
                    texture.Index = -1;
                    texture.ID = -1;
                }
            }
        }

        #region drawing
        /// <summary>
        ///     Draws a texture
        /// </summary>
        /// <param name="texture">The texture to be drawn</param>
        public void DrawTexture(STexture texture)
        {
            DrawTexture(texture, texture.Rect, texture.Color);
        }

        /// <summary>
        ///     Draws a texture
        /// </summary>
        /// <param name="texture">The texture to be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        public void DrawTexture(STexture texture, SRectF rect)
        {
            DrawTexture(texture, rect, texture.Color, false);
        }

        /// <summary>
        ///     Draws a texture
        /// </summary>
        /// <param name="texture">The texture to be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        /// <param name="color">A SColorF struct containing a color which the texture will be colored in</param>
        public void DrawTexture(STexture texture, SRectF rect, SColorF color)
        {
            DrawTexture(texture, rect, color, false);
        }

        /// <summary>
        ///     Draws a texture
        /// </summary>
        /// <param name="texture">The texture to be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        /// <param name="color">A SColorF struct containing a color which the texture will be colored in</param>
        /// <param name="bounds">A SRectF struct containing which part of the texture should be drawn</param>
        public void DrawTexture(STexture texture, SRectF rect, SColorF color, SRectF bounds)
        {
            DrawTexture(texture, rect, color, bounds, false);
        }

        /// <summary>
        ///     Draws a texture
        /// </summary>
        /// <param name="texture">The texture to be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        /// <param name="color">A SColorF struct containing a color which the texture will be colored in</param>
        /// <param name="mirrored">True if the texture should be mirrored</param>
        public void DrawTexture(STexture texture, SRectF rect, SColorF color, bool mirrored)
        {
            DrawTexture(texture, rect, color, new SRectF(0, 0, CSettings.RenderW, CSettings.RenderH, rect.Z), mirrored);
        }

        /// <summary>
        ///     Draws a texture
        /// </summary>
        /// <param name="texture">The texture to be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        /// <param name="color">A SColorF struct containing a color which the texture will be colored in</param>
        /// <param name="bounds">A SRectF struct containing which part of the texture should be drawn</param>
        /// <param name="mirrored">True if the texture should be mirrored</param>
        public void DrawTexture(STexture texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored)
        {
            if (_TextureExists(ref texture))
            {
                if (_D3DTextures[texture.Index] == null)
                    return;

                //Calculate the position
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

                if (color.A > 1)
                    color.A = 1;
                if (color.R > 1)
                    color.R = 1;
                if (color.G > 1)
                    color.G = 1;
                if (color.B > 1)
                    color.B = 1;

                Color c = Color.FromArgb((int)(color.A * 255 * CGraphics.GlobalAlpha), (int)(color.R * 255), (int)(color.G * 255), (int)(color.B * 255));

                if (!mirrored)
                {
                    STexturedColoredVertex[] vert = new STexturedColoredVertex[4];
                    vert[0] = new STexturedColoredVertex(new Vector3(rx1, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x1, y1), c.ToArgb());
                    vert[1] = new STexturedColoredVertex(new Vector3(rx1, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x1, y2), c.ToArgb());
                    vert[2] = new STexturedColoredVertex(new Vector3(rx2, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x2, y2), c.ToArgb());
                    vert[3] = new STexturedColoredVertex(new Vector3(rx2, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x2, y1), c.ToArgb());
                    _AddToVertexBuffer(vert, _D3DTextures[texture.Index], _CalculateRotationMatrix(rect.Rotation, rx1, rx2, ry1, ry2));
                }
                else
                {
                    STexturedColoredVertex[] vert = new STexturedColoredVertex[4];
                    vert[0] = new STexturedColoredVertex(new Vector3(rx1, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x1, -y1), c.ToArgb());
                    vert[1] = new STexturedColoredVertex(new Vector3(rx1, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x1, -y2), c.ToArgb());
                    vert[2] = new STexturedColoredVertex(new Vector3(rx2, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x2, -y2), c.ToArgb());
                    vert[3] = new STexturedColoredVertex(new Vector3(rx2, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x2, -y1), c.ToArgb());
                    _AddToVertexBuffer(vert, _D3DTextures[texture.Index], _CalculateRotationMatrix(rect.Rotation, rx1, rx2, ry1, ry2));
                }
            }
        }

        /// <summary>
        ///     Draws a texture
        /// </summary>
        /// <param name="texture">The texture to be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        /// <param name="color">A SColorF struct containing a color which the texture will be colored in</param>
        /// <param name="begin">A Value ranging from 0 to 1 containing the beginning of the texture</param>
        /// <param name="end">A Value ranging from 0 to 1 containing the ending of the texture</param>
        public void DrawTexture(STexture texture, SRectF rect, SColorF color, float begin, float end)
        {
            if (_TextureExists(ref texture))
            {
                if (_D3DTextures[texture.Index] == null)
                    return;

                float x1 = 0f + begin * texture.WidthRatio;
                float x2 = texture.WidthRatio * end;
                float y1 = 0f;
                float y2 = texture.HeightRatio;

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

                STexturedColoredVertex[] vert = new STexturedColoredVertex[4];
                vert[0] = new STexturedColoredVertex(new Vector3(rx1, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x1, y1), c.ToArgb());
                vert[1] = new STexturedColoredVertex(new Vector3(rx1, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x1, y2), c.ToArgb());
                vert[2] = new STexturedColoredVertex(new Vector3(rx2, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x2, y2), c.ToArgb());
                vert[3] = new STexturedColoredVertex(new Vector3(rx2, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x2, y1), c.ToArgb());
                _AddToVertexBuffer(vert, _D3DTextures[texture.Index], _CalculateRotationMatrix(rect.Rotation, rx1, rx2, ry1, ry2));
            }
        }

        /// <summary>
        ///     Draws a reflection of a texture
        /// </summary>
        /// <param name="texture">The texture of which a reflection should be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        /// <param name="color">A SColorF struct containing a color which the texture will be colored in</param>
        /// <param name="bounds">A SRectF struct containing which part of the texture should be drawn</param>
        /// <param name="space">The space between the texture and the reflection</param>
        /// <param name="height">The height of the reflection</param>
        public void DrawTextureReflection(STexture texture, SRectF rect, SColorF color, SRectF bounds, float space, float height)
        {
            if (_TextureExists(ref texture))
            {
                if (_D3DTextures[texture.Index] == null)
                    return;

                if (rect.W == 0f || rect.H == 0f || bounds.H == 0f || bounds.W == 0f || color.A == 0f || height <= 0f)
                    return;

                if (bounds.X > rect.X + rect.W || bounds.X + bounds.W < rect.X)
                    return;

                if (bounds.Y > rect.Y + rect.H || bounds.Y + bounds.H < rect.Y)
                    return;

                if (height > bounds.H)
                    height = bounds.H;

                if (_TextureExists(ref texture))
                {
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

                    //Align the pixels because Direct3D expects the pixels to be the left top corner
                    rx1 -= 0.5f;
                    ry1 -= 0.5f;
                    rx2 -= 0.5f;
                    ry2 -= 0.5f;

                    Color c = Color.FromArgb((int)(color.A * 255 * CGraphics.GlobalAlpha), (int)(color.R * 255), (int)(color.G * 255), (int)(color.B * 255));
                    Color transparent = Color.FromArgb(0, (int)(color.R * 255), (int)(color.G * 255), (int)(color.B * 255));

                    STexturedColoredVertex[] vert = new STexturedColoredVertex[4];
                    vert[0] = new STexturedColoredVertex(new Vector3(rx1, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x1, y2), c.ToArgb());
                    vert[1] = new STexturedColoredVertex(new Vector3(rx1, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x1, y1), transparent.ToArgb());
                    vert[2] = new STexturedColoredVertex(new Vector3(rx2, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x2, y1), transparent.ToArgb());
                    vert[3] = new STexturedColoredVertex(new Vector3(rx2, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x2, y2), c.ToArgb());
                    _AddToVertexBuffer(vert, _D3DTextures[texture.Index], _CalculateRotationMatrix(rect.Rotation, rx1, rx2, ry1, ry2));
                }
            }
        }
        #endregion drawing

        private void _CheckQueque()
        {
            lock (_MutexTexture)
            {
                if (_Queque.Count == 0)
                    return;

                STextureQueque q = _Queque[0];
                STexture texture;
                if (_Textures.ContainsKey(q.ID))
                    texture = _Textures[q.ID];
                else
                    return;

                Texture t = _CreateTexture(q.Width, q.Height, ref q.Data, ref texture);
                _D3DTextures[q.ID] = t;

                _Textures[texture.Index] = texture;
                _Queque.RemoveAt(0);
            }
        }

        /// <summary>
        ///     Gets the count of current textures
        /// </summary>
        /// <returns>The amount of textures</returns>
        public int TextureCount()
        {
            return _Textures.Count;
        }
        #endregion Textures

        #endregion implementation

        #region utility
        /// <summary>
        ///     Calculates the next power of two if the device has the POW2 flag set
        /// </summary>
        /// <param name="n">The value of which the next power of two will be calculated</param>
        /// <returns>The next power of two</returns>
        private float _CheckForNextPowerOf2(float n)
        {
            if (!_NonPowerOf2TextureSupported)
            {
                if (n < 0)
                    throw new ArgumentOutOfRangeException("n", "Must be positive.");
                return (float)Math.Pow(2, Math.Ceiling(Math.Log(n, 2)));
            }
            else
                return n;
        }

        private SlimDX.Matrix _CalculateRotationMatrix(float rot, float rx1, float rx2, float ry1, float ry2)
        {
            SlimDX.Matrix originTranslation = SlimDX.Matrix.Translation(new Vector3(-CSettings.RenderW / 2, CSettings.RenderH / 2, 0));
            if (rot != 0)
            {
                float rotation = rot * (float)Math.PI / 180;
                float centerX = (rx1 + rx2) / 2f;
                float centerY = -(ry1 + ry2) / 2f;

                SlimDX.Matrix translationA = SlimDX.Matrix.Translation(-centerX, -centerY, 0);
                SlimDX.Matrix rotationMat = SlimDX.Matrix.RotationZ(-rotation);
                SlimDX.Matrix translationB = SlimDX.Matrix.Translation(centerX, centerY, 0);

                //Multiplicate the matrices to get the real world matrix,
                //First shift the texture into the center
                //Rotate it and shift it back to the origin position
                //Apply the originTranslation after
                SlimDX.Matrix result = translationA * rotationMat * translationB * originTranslation;
                return result;
            }
            return originTranslation;
        }
        #endregion utility

        #region TexturedColoredVertex
        public struct STexturedColoredVertex
        {
            private static VertexDeclaration _Declaration;
            public static VertexElement[] Elements =
                {
                    new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
                    new VertexElement(0, sizeof(float) * 3, DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
                    new VertexElement(0, sizeof(float) * 3 + sizeof(float) * 2, DeclarationType.Color, DeclarationMethod.Default, DeclarationUsage.Color, 0),
                    VertexElement.VertexDeclarationEnd
                };

            public Vector3 Position;
            public Vector2 Texture;
            public int Color;

            public STexturedColoredVertex(Vector3 position, Vector2 texture, int color)
            {
                Position = position;
                Texture = texture;
                Color = color;
            }

            public static VertexDeclaration GetDeclaration(Device device)
            {
                if (_Declaration == null || _Declaration.Disposed)
                    _Declaration = new VertexDeclaration(device, Elements);

                return _Declaration;
            }
        }
        #endregion TexturedColoredVertex
    }
}