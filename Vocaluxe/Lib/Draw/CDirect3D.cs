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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SharpGen.Runtime;
using Vortice.Direct3D9;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Draw
{
    class CRenderFormHook : Form, IFormHook
    {
        private const int _WmMenuChar = 0x0120;
        private const int _MncClose = 1;

        public MessageEventHandler OnMessage { private get; set; }

        public CRenderFormHook()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Opaque, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == _WmMenuChar)
            {
                m.Result = new IntPtr(_MncClose << 16);
                return;
            }

            if (OnMessage == null || OnMessage(ref m))
            {
                base.WndProc(ref m);
            }
        }
    }

    class CD3DTexture : CTextureBase
    {
        public readonly IDirect3DTexture9 D3DTexture;

        public CD3DTexture(IDirect3DDevice9 device, Size dataSize, int texWidth = 0, int texHeight = 0) : base(dataSize, new Size(texWidth, texHeight))
        {
            var noMips = texWidth >= 1280 || texHeight >= 720;
            var usage = noMips ? Usage.None : Usage.AutoGenerateMipMap;
            var levels = noMips ? 1 : 0;

            D3DTexture = device?.CreateTexture(texWidth, texHeight, levels, usage, Format.A8R8G8B8, Pool.Managed);
        }

        public override bool IsLoaded
        {
            get { return D3DTexture != null; }
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            base.Dispose();
            if (D3DTexture != null)
            {
                D3DTexture.Dispose();
            }
        }
    }

    class CDirect3D : CDrawBaseWindows<CD3DTexture>, IDraw
    {
        private const int _D3DErrDeviceLost = unchecked((int)0x88760868);
        private const int _D3DErrDeviceNotReset = unchecked((int)0x88760869);

        private readonly IDirect3D9 _D3D;
        private readonly IDirect3DDevice9 _Device;
        private PresentParameters _PresentParameters;

        private IDirect3DVertexBuffer9 _VertexBuffer;
        private IDirect3DIndexBuffer9 _IndexBuffer;

        private CTextureRef _BlankTexture;

        private readonly Queue<STexturedColoredVertex> _Vertices = new Queue<STexturedColoredVertex>();
        private readonly Queue<IDirect3DTexture9> _VerticesTextures = new Queue<IDirect3DTexture9>();
        private readonly Queue<Matrix4x4> _VerticesRotationMatrices = new Queue<Matrix4x4>();

        /// <summary>
        ///     Creates a new Instance of the CDirect3D Class
        /// </summary>
        public CDirect3D()
        {
            _Form = new CRenderFormHook { ClientSize = new Size(CConfig.Config.Graphics.ScreenW * CConfig.Config.Graphics.NumScreens, CConfig.Config.Graphics.ScreenH) };

            try
            {
                _D3D = D3D9.Direct3DCreate9();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Direct3D 9 could not be initialized, please check if your graphic card drivers are up to date.");
            }

            if (_D3D == null)
            {
                CLog.Fatal("Direct3D 9 is not available on this system.");
                return;
            }

            _Form.KeyDown += _OnKeyDown;
            _Form.PreviewKeyDown += _OnPreviewKeyDown;
            _Form.KeyPress += _OnKeyPress;
            _Form.KeyUp += _OnKeyUp;

            _Form.MouseMove += _OnMouseMove;
            _Form.MouseWheel += _OnMouseWheel;
            _Form.MouseDown += _OnMouseDown;
            _Form.MouseUp += _OnMouseUp;
            _Form.MouseLeave += _OnMouseLeave;
            _Form.MouseEnter += _OnMouseEnter;

            const int adapter = 0;
            var displayFormat = _D3D.GetAdapterDisplayMode(adapter).Format;

            var depthFormat = _D3D.CheckDepthStencilMatch(adapter, DeviceType.Hardware, displayFormat, displayFormat, Format.D24X8).Success ? Format.D24X8 : Format.D16;

            _PresentParameters = new PresentParameters
            {
                Windowed = true,
                SwapEffect = SwapEffect.Discard,
                BackBufferHeight = CConfig.Config.Graphics.ScreenH,
                BackBufferWidth = CConfig.Config.Graphics.ScreenW * CConfig.Config.Graphics.NumScreens,
                BackBufferFormat = displayFormat,
                EnableAutoDepthStencil = true,
                AutoDepthStencilFormat = depthFormat,
                MultiSampleType = MultisampleType.None,
                MultiSampleQuality = 0
            };

            //Apply antialiasing and check if antialiasing mode is supported

            #region Antialiasing
            int quality;
            MultisampleType msType;
            switch (CConfig.Config.Graphics.AAMode)
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

            if (_D3D.CheckDeviceMultiSampleType(adapter, DeviceType.Hardware, displayFormat, false, msType, out quality).Failure)
            {
                CLog.Error("[Direct3D] This AAMode is not supported by this device or driver, fallback to no AA");
                msType = MultisampleType.None;
                quality = 1;
            }

            _PresentParameters.MultiSampleType = msType;
            _PresentParameters.MultiSampleQuality = quality - 1;
            #endregion Antialiasing

            //Apply the VSync configuration
            _PresentParameters.PresentationInterval = CConfig.Config.Graphics.VSync == EOffOn.TR_CONFIG_ON ? PresentInterval.Default : PresentInterval.Immediate;

            //GMA 950 graphics devices can only process vertices in software mode
            var caps = _D3D.GetDeviceCaps(adapter, DeviceType.Hardware);
            var flags = (caps.DeviceCaps & DeviceCaps.HWTransformAndLight) != 0 ? CreateFlags.HardwareVertexProcessing : CreateFlags.SoftwareVertexProcessing;

            //Check if Pow2 textures are needed
            _NonPowerOf2TextureSupported = true;
            _NonPowerOf2TextureSupported &= (caps.TextureCaps & TextureCaps.Pow2) == 0;
            _NonPowerOf2TextureSupported &= (caps.TextureCaps & TextureCaps.NonPow2Conditional) == 0;
            _NonPowerOf2TextureSupported &= (caps.TextureCaps & TextureCaps.SquareOnly) == 0;

            try
            {
                _Device = _D3D.CreateDevice(adapter, DeviceType.Hardware, _Form.Handle, flags, _PresentParameters);
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error during D3D device creation.");
            }
            finally
            {
                if (_Device == null)
                {
                    CLog.Fatal("Something went wrong during device creating, please check if your grafic card drivers are up to date.");
                }
            }
        }

        #region resize
        /// <summary>
        ///     Resizes the viewport
        /// </summary>
        protected override void _DoResize()
        {
            // The window was minimized, so restore it to the last known size
            if (_Form.ClientSize.Width == 0 || _Form.ClientSize.Height == 0)
            {
                _Form.ClientSize = _SizeBeforeMinimize;
            }

            if (_H == _Form.ClientSize.Height && _W == _Form.ClientSize.Width && CConfig.Config.Graphics.ScreenAlignment == _CurrentAlignment)
            {
                return;
            }

            _CurrentAlignment = CConfig.Config.Graphics.ScreenAlignment;
            _H = _Form.ClientSize.Height;
            _W = _Form.ClientSize.Width;

            if (CConfig.Config.Graphics.Stretch != EOffOn.TR_CONFIG_ON)
            {
                _AdjustAspect(false);
            }

            //Apply the new sizes to the PresentParameters
            _PresentParameters.BackBufferWidth = _Form.ClientSize.Width;
            _PresentParameters.BackBufferHeight = _Form.ClientSize.Height;
            if (_Run)
            {
                _ClearScreen();
                //To set new PresentParameters the device has to be resetted
                _Reset();
            }

            //Store size so it can get restored after the window gets minimized
            _SizeBeforeMinimize = _Form.ClientSize;
        }

        // ReSharper disable RedundantOverridenMember
        /// <summary>
        ///     Triggers the Fullscreen mode
        /// </summary>
        protected override void _EnterFullScreen()
        {
            //This currently not using real fullscreen mode but a borderless window
            //Real fullscreen could be gained setting _PresentParameters.Windowed = true
            //And calling Reset() and Init() after
            base._EnterFullScreen();
        }

        // ReSharper restore RedundantOverridenMember
        #endregion resize

        #region main stuff
        /// <summary>
        ///     Inits the Device
        /// </summary>
        /// <returns>True if it succeeded else false</returns>
        public override bool Init()
        {
            if (!base.Init())
            {
                return false;
            }

            if (_Device == null)
            {
                return false;
            }

            _InitDevice();

            //This creates a new white texture and adds it to the texture pool
            //This texture is used for the DrawRect method
            using (var blankMap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(blankMap))
            {
                g.Clear(Color.White);
                _BlankTexture = AddTexture(blankMap);
            }

            return true;
        }

        private void _InitDevice()
        {
            _AdjustNewBorders();

            var vertexSize = Marshal.SizeOf(typeof(STexturedColoredVertex));
            _VertexBuffer = _Device.CreateVertexBuffer(CSettings.VertexBufferElements * 4 * vertexSize, Usage.WriteOnly | Usage.Dynamic,
                VertexFormat.Position | VertexFormat.Texture1 | VertexFormat.Diffuse, Pool.Default);

            _Device.SetStreamSource(0, _VertexBuffer, 0, vertexSize);
            _Device.VertexDeclaration = STexturedColoredVertex.GetDeclaration(_Device);

            _Device.SetRenderState(RenderState.CullMode, (int)Cull.None);
            _Device.SetRenderState(RenderState.ZEnable, (int)ZBufferType.UseZBuffer);
            _Device.SetRenderState(RenderState.ZFunc, (int)Compare.LessEqual);
            _Device.SetRenderState(RenderState.ZWriteEnable, true);
            _Device.SetRenderState(RenderState.AlphaBlendEnable, true);
            _Device.SetRenderState(RenderState.Lighting, false);
            _Device.SetRenderState(RenderState.DestinationBlend, (int)Blend.InverseSourceAlpha);
            _Device.SetRenderState(RenderState.SourceBlend, (int)Blend.SourceAlpha);

            if (_PresentParameters.MultiSampleType != MultisampleType.None)
            {
                _Device.SetRenderState(RenderState.MultisampleAntialias, true);
            }

            _Device.SetSamplerState(0, SamplerState.MinFilter, (int)TextureFilter.Linear);
            _Device.SetSamplerState(0, SamplerState.MagFilter, (int)TextureFilter.Linear);
            _Device.SetSamplerState(0, SamplerState.MipFilter, (int)TextureFilter.Linear);
            _Device.SetSamplerState(0, SamplerState.AddressU, (int)TextureAddress.Clamp);
            _Device.SetSamplerState(0, SamplerState.AddressV, (int)TextureAddress.Clamp);

            _Device.SetTextureStageState(0, TextureStage.AlphaArg1, (int)TextureArgument.Texture);
            _Device.SetTextureStageState(0, TextureStage.AlphaArg2, (int)TextureArgument.Diffuse);
            _Device.SetTextureStageState(0, TextureStage.AlphaOperation, (int)TextureOperation.Modulate);

            var indices = new Int16[6];
            indices[0] = 0;
            indices[1] = 1;
            indices[2] = 2;
            indices[3] = 0;
            indices[4] = 2;
            indices[5] = 3;

            _IndexBuffer = _Device.CreateIndexBuffer(6 * sizeof(Int16), Usage.WriteOnly, true, Pool.Managed);

            var indexData = _IndexBuffer.Lock<byte>(0, 6 * sizeof(Int16), LockFlags.Discard);
            MemoryMarshal.AsBytes(indices.AsSpan()).CopyTo(indexData);
            _IndexBuffer.Unlock();
            _Device.Indices = _IndexBuffer;
        }

        protected override void _OnBeforeDraw()
        {
            try
            {
                _Device.BeginScene();
            }
            catch (SharpGenException e)
            {
                CLog.Error(e, "Failed to begin scene");
            }
        }

        protected override void _OnAfterDraw()
        {
            _RenderVertexBuffer();
            try
            {
                _Device.EndScene();
            }
            catch (SharpGenException e)
            {
                CLog.Error(e, "Failed to end scene");
            }

            //Now push the frame to the Viewport
            var result = _Device.Present();
            if (result.Failure)
            {
                //In Direct3D devices can get lost. 
                //This happens for example when opening the Task Manager in Vista/Windows 7 or if a UAC message is opening
                //We need to reset the device to get it to workable state again
                //After a reset Init() needs to be called because all data in the Direct3D default pool are lost and need to be recreated
                if (_GetCooperativeLevel() == _D3DErrDeviceNotReset)
                {
                    _Reset();
                }
                else if (result.Code != _D3DErrDeviceLost)
                {
                    CLog.Error("Failed to present the frame: " + result);
                }
            }

            Application.DoEvents();
        }

        private int _GetCooperativeLevel()
        {
            try
            {
                _Device.TestCooperativeLevel();
                return 0;
            }
            catch (SharpGenException e)
            {
                return e.ResultCode.Code;
            }
        }

        /// <summary>
        ///     Resets the device, all objects in the Direct3D default pool get flushed and need to be recreated
        /// </summary>
        private void _Reset()
        {
            //Dispose all objects in the default pool, those need to be recreated
            _DisposeDeviceResources();
            try
            {
                _Device.Reset(ref _PresentParameters);
                //All configurations got flushed due to Reset(), so apply them again
                _InitDevice();
                _Device.Viewport = new Viewport { X = _X, Y = _Y, Width = _W, Height = _H, MinZ = 0f, MaxZ = 1f };
            }
            catch (SharpGenException e)
            {
                CLog.Error(e, "Failed to reset the device");
                _DisposeDeviceResources();
            }
        }

        private void _DisposeDeviceResources()
        {
            STexturedColoredVertex.DisposeDeclaration();
            _VertexBuffer?.Dispose();
            _VertexBuffer = null;
            _IndexBuffer?.Dispose();
            _IndexBuffer = null;
        }

        protected override void _AdjustNewBorders()
        {
            const float dx = (float)CSettings.RenderW / 2;
            const float dy = (float)CSettings.RenderH / 2;
            var translate = Matrix4x4.CreateTranslation(-dx, dy, 0);
            var projection = _CreateOrthoOffCenterLH(
                -dx - _BorderLeft, CSettings.RenderW * CConfig.Config.Graphics.NumScreens - dx + _BorderRight,
                -dy - _BorderBottom, dy + _BorderTop,
                CSettings.ZNear, CSettings.ZFar);

            _Device.SetTransform(TransformState.Projection, projection);
            _Device.SetTransform(0, translate);
        }

        private static Matrix4x4 _CreateOrthoOffCenterLH(float left, float right, float bottom, float top, float zNear, float zFar)
        {
            return new Matrix4x4(
                2f / (right - left), 0f, 0f, 0f,
                0f, 2f / (top - bottom), 0f, 0f,
                0f, 0f, 1f / (zFar - zNear), 0f,
                (left + right) / (left - right), (top + bottom) / (bottom - top), zNear / (zNear - zFar), 1f);
        }

        /// <summary>
        ///     Unloads all Textures and other objects used by Direct3D for rendering
        /// </summary>
        /// <returns></returns>
        public override void Close()
        {
            base.Close();
            _DisposeDeviceResources();
            _Device?.Dispose();
            _D3D?.Dispose();
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
        ///     Adds a texture to the vertext buffer
        /// </summary>
        /// <param name="texture">Texture to draw</param>
        /// <param name="dc">Coordinates to draw on</param>
        /// <param name="color">Color to use</param>
        /// <param name="isReflection">If true, then color is faded out in y direction</param>
        protected override void _DrawTexture(CD3DTexture texture, SDrawCoords dc, SColorF color, bool isReflection = false)
        {
            //Align the pixels because Direct3D expects the pixels to be the left top corner
            dc.Wx1 -= 0.5f;
            dc.Wy1 -= 0.5f;
            dc.Wx2 -= 0.5f;
            dc.Wy2 -= 0.5f;

            color.A *= CGraphics.GlobalAlpha;
            var c = color.AsColor().ToArgb();
            int c2;
            if (isReflection)
            {
                color.A = 0;
                c2 = color.AsColor().ToArgb();
            }
            else
            {
                c2 = c;
            }

            var vert = new STexturedColoredVertex[4];
            vert[0] = new STexturedColoredVertex(new Vector3(dc.Wx1, -dc.Wy1, dc.Wz), new Vector2(dc.Tx1, dc.Ty1), c);
            vert[1] = new STexturedColoredVertex(new Vector3(dc.Wx1, -dc.Wy2, dc.Wz), new Vector2(dc.Tx1, dc.Ty2), c2);
            vert[2] = new STexturedColoredVertex(new Vector3(dc.Wx2, -dc.Wy2, dc.Wz), new Vector2(dc.Tx2, dc.Ty2), c2);
            vert[3] = new STexturedColoredVertex(new Vector3(dc.Wx2, -dc.Wy1, dc.Wz), new Vector2(dc.Tx2, dc.Ty1), c);
            _AddToVertexBuffer(vert, texture.D3DTexture, _CalculateRotationMatrix(dc.Rotation, dc.Wx1, dc.Wx2, dc.Wy1, dc.Wy2));
        }

        /// <summary>
        ///     Adds a quad a list which will be added and rendered to the vertexbuffer when calling RenderToVertexBuffer to reduce vertexbuffer calls each frame to a minimum
        /// </summary>
        /// <param name="vertices">A TexturedColoredVertex array containg 4 vertices</param>
        /// <param name="tex">The texture the vertex should be textured with</param>
        /// <param name="rotation">The vertices' rotation</param>
        private void _AddToVertexBuffer(STexturedColoredVertex[] vertices, IDirect3DTexture9 tex, Matrix4x4 rotation)
        {
            //The vertexbuffer is full, so we need to flush it before we can continue
            if (_Vertices.Count >= CSettings.VertexBufferElements)
            {
                _RenderVertexBuffer();
            }

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
            if (_Vertices.Count <= 0)
            {
                return;
            }

            if (_VertexBuffer != null)
            {
                try
                {
                    //The vertex buffer locks are slow actions, its better to lock once per frame and write all vertices to the buffer at once
                    var vertexData = MemoryMarshal.AsBytes(_Vertices.ToArray().AsSpan());
                    var bufferData = _VertexBuffer.Lock<byte>(0, vertexData.Length, LockFlags.Discard);
                    vertexData.CopyTo(bufferData);
                    _VertexBuffer.Unlock();

                    for (var i = 0; i < _Vertices.Count; i += 4)
                    {
                        //Apply rotation
                        _Device.SetTransform(0, _VerticesRotationMatrices.Dequeue());

                        //Apply texture
                        _Device.SetTexture(0, _VerticesTextures.Dequeue());

                        //Draw 2 triangles from vertexbuffer
                        _Device.DrawIndexedPrimitive(PrimitiveType.TriangleList, i, 0, 4, 0, 2);
                    }
                }
                catch (SharpGenException e)
                {
                    CLog.Error(e, "Failed to render the vertex buffer");
                }
            }

            //Clear the queues for the next frame
            _Vertices.Clear();
            _VerticesTextures.Clear();
            _VerticesRotationMatrices.Clear();
        }
        #endregion main stuff

        /// <summary>
        ///     Removes all textures from the screen
        /// </summary>
        protected override void _ClearScreen()
        {
            try
            {
                _Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, new Vortice.Mathematics.Color(0, 0, 0, 255), 1.0f, 0);
            }
            catch (SharpGenException e)
            {
                CLog.Error(e, "Failed to clear the backbuffer");
            }
        }

        private int[] _ReadBackBuffer(out int width, out int height)
        {
            width = 0;
            height = 0;
            try
            {
                using (var backBuffer = _Device.GetBackBuffer(0, 0))
                {
                    var desc = backBuffer.Description;
                    if (desc.Format != Format.X8R8G8B8 && desc.Format != Format.A8R8G8B8)
                    {
                        CLog.Error("Reading the backbuffer is not supported for format " + desc.Format);
                        return null;
                    }

                    width = desc.Width;
                    height = desc.Height;
                    var rect = new Rect(0, 0, width, height);

                    using (var renderTarget = _Device.CreateRenderTarget(width, height, desc.Format, MultisampleType.None, 0, false))
                    using (var sysMemSurface = _Device.CreateOffscreenPlainSurface(width, height, desc.Format, Pool.SystemMemory))
                    {
                        _Device.StretchRect(backBuffer, rect, renderTarget, rect, TextureFilter.None);
                        _Device.GetRenderTargetData(renderTarget, sysMemSurface);

                        var pixels = new int[width * height];
                        var locked = sysMemSurface.LockRect(LockFlags.ReadOnly);
                        try
                        {
                            for (var y = 0; y < height; y++)
                            {
                                Marshal.Copy(IntPtr.Add(locked.DataPointer, y * locked.Pitch), pixels, y * width, width);
                            }
                        }
                        finally
                        {
                            sysMemSurface.UnlockRect();
                        }

                        for (var i = 0; i < pixels.Length; i++)
                        {
                            pixels[i] |= unchecked((int)0xFF000000);
                        }

                        return pixels;
                    }
                }
            }
            catch (SharpGenException e)
            {
                CLog.Error(e, "Failed to read the backbuffer");
                return null;
            }
        }

        private void _CopyBackBufferToTexture(CD3DTexture texture)
        {
            int bufferWidth, bufferHeight;
            var pixels = _ReadBackBuffer(out bufferWidth, out bufferHeight);
            if (pixels == null)
            {
                return;
            }

            var width = Math.Min(Math.Min(_W, bufferWidth), texture.Size.Width);
            var height = Math.Min(Math.Min(_H, bufferHeight), texture.Size.Height);

            var locked = texture.D3DTexture.LockRect(0, LockFlags.Discard);
            try
            {
                for (var y = 0; y < height; y++)
                {
                    Marshal.Copy(pixels, y * bufferWidth, IntPtr.Add(locked.DataPointer, y * locked.Pitch), width);
                }
            }
            finally
            {
                texture.D3DTexture.UnlockRect(0);
            }
        }

        /// <summary>
        ///     Copies the current frame into a texture
        ///     <returns>A texture holding the current frame</returns>
        /// </summary>
        public CTextureRef CopyScreen()
        {
            var tex = _CreateTexture(new Size(_W, _H));
            _CopyBackBufferToTexture(tex);

            return _GetTextureReference(_W, _H, tex);
        }

        /// <summary>
        ///     Copies the current frame into a texture
        /// </summary>
        /// <param name="textureRef">The texture in which the frame is copied to</param>
        public void CopyScreen(ref CTextureRef textureRef)
        {
            CD3DTexture texture;
            if (!_GetTexture(textureRef, out texture) || texture.DataSize.Width != GetScreenWidth() || texture.DataSize.Height != GetScreenHeight())
            {
                RemoveTexture(ref textureRef);
                textureRef = CopyScreen();
            }
            else
            {
                _CopyBackBufferToTexture(texture);
            }
        }

        /// <summary>
        ///     Creates a Screenshot of the current frame
        /// </summary>
        public void MakeScreenShot()
        {
            var file = CHelper.GetUniqueFileName(Path.Combine(CSettings.DataFolder, CSettings.FolderNameScreenshots), "Screenshot.png");

            int width, height;
            var pixels = _ReadBackBuffer(out width, out height);
            if (pixels == null)
            {
                return;
            }

            using (var screen = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                var bmpData = screen.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                try
                {
                    for (var y = 0; y < height; y++)
                    {
                        Marshal.Copy(pixels, y * width, IntPtr.Add(bmpData.Scan0, y * bmpData.Stride), width);
                    }
                }
                finally
                {
                    screen.UnlockBits(bmpData);
                }

                screen.Save(file, ImageFormat.Png);
            }
        }

        /// <summary>
        ///     Draws a colored rectangle
        /// </summary>
        /// <param name="color">The color in which the rectangle will be drawn in</param>
        /// <param name="rect">The coordinates in a SRectF struct</param>
        public void DrawRect(SColorF color, SRectF rect, bool allMonitors = true)
        {
            DrawTexture(_BlankTexture, rect, color, false, allMonitors);
        }

        /// <summary>
        ///     Draws reflection of a colored rectangle
        /// </summary>
        /// <param name="color">The color in which the rectangle will be drawn in</param>
        /// <param name="rect">The coordinates in a SRectF struct</param>
        /// <param name="space">The space between the texture and the reflection</param>
        /// <param name="height">The height of the reflection</param>
        public void DrawRectReflection(SColorF color, SRectF rect, float space, float height)
        {
            DrawTextureReflection(_BlankTexture, rect, color, rect, space, height);
        }

        protected override CD3DTexture _CreateTexture(Size dataSize)
        {
            if (dataSize.Width < 0)
            {
                return new CD3DTexture(null, dataSize);
            }

            return new CD3DTexture(_Device, dataSize, _CheckForNextPowerOf2(dataSize.Width), _CheckForNextPowerOf2(dataSize.Height));
        }

        protected override void _WriteDataToTexture(CD3DTexture texture, byte[] data)
        {
            var rect = texture.D3DTexture.LockRect(0, LockFlags.Discard);
            try
            {
                var rowWidth = 4 * texture.DataSize.Width;
                var dst = rect.DataPointer;
                if (rowWidth == rect.Pitch)
                    Marshal.Copy(data, 0, dst, data.Length);
                else
                {
                    var rows = data.Length / rowWidth;
                    for (var y = 0; y < rows; y++)
                        Marshal.Copy(data, y * rowWidth, IntPtr.Add(dst, y * rect.Pitch), rowWidth);
                }
            }
            finally { texture.D3DTexture.UnlockRect(0); }
        }

        private static Matrix4x4 _CalculateRotationMatrix(float rot, float rx1, float rx2, float ry1, float ry2)
        {
            var originTranslation = Matrix4x4.CreateTranslation(-(float)CSettings.RenderW / 2, (float)CSettings.RenderH / 2, 0);
            if (Math.Abs(rot) > float.Epsilon)
            {
                var rotation = rot * (float)Math.PI / 180;
                var centerX = (rx1 + rx2) / 2f;
                var centerY = -(ry1 + ry2) / 2f;

                var translationA = Matrix4x4.CreateTranslation(-centerX, -centerY, 0);
                var rotationMat = Matrix4x4.CreateRotationZ(-rotation);
                var translationB = Matrix4x4.CreateTranslation(centerX, centerY, 0);

                //Multiplicate the matrices to get the real world matrix,
                //First shift the texture into the center
                //Rotate it and shift it back to the origin position
                //Apply the originTranslation after
                var result = translationA * rotationMat * translationB * originTranslation;
                return result;
            }

            return originTranslation;
        }

        private struct STexturedColoredVertex
        {
            private static IDirect3DVertexDeclaration9 _Declaration;
            private static readonly VertexElement[] _Elements =
            {
                new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
                new VertexElement(0, sizeof(float) * 3, DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
                new VertexElement(0, sizeof(float) * 3 + sizeof(float) * 2, DeclarationType.Color, DeclarationMethod.Default, DeclarationUsage.Color, 0),
                VertexElement.VertexDeclarationEnd
            };

            // ReSharper disable NotAccessedField.Local
            private Vector3 _Position;
            private Vector2 _Texture;
            private int _Color;
            // ReSharper restore NotAccessedField.Local

            public STexturedColoredVertex(Vector3 position, Vector2 texture, int color)
            {
                _Position = position;
                _Texture = texture;
                _Color = color;
            }

            public static IDirect3DVertexDeclaration9 GetDeclaration(IDirect3DDevice9 device)
            {
                if (_Declaration == null)
                {
                    _Declaration = device.CreateVertexDeclaration(_Elements);
                }

                return _Declaration;
            }

            public static void DisposeDeclaration()
            {
                if (_Declaration != null)
                {
                    _Declaration.Dispose();
                    _Declaration = null;
                }
            }
        }
    }
}
