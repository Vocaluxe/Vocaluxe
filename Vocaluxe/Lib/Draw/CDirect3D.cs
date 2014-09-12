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

using SlimDX;
using SlimDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SlimDX.Windows;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Draw
{
    class CRenderFormHook : RenderForm, IFormHook
    {
        public MessageEventHandler OnMessage { private get; set; }

        protected override void WndProc(ref Message m)
        {
            if (OnMessage == null || OnMessage(ref m))
                base.WndProc(ref m);
        }
    }

    class CDirect3D : CDrawBaseWindows<Texture>, IDraw
    {
        #region private vars
        private readonly Direct3D _D3D;
        private readonly Device _Device;
        private readonly PresentParameters _PresentParameters;

        private VertexBuffer _VertexBuffer;
        private IndexBuffer _IndexBuffer;

        private int _H = 1;
        private int _W = 1;
        private int _Y;
        private int _X;

        private CTexture _BlankTexture;

        private readonly Queue<STexturedColoredVertex> _Vertices = new Queue<STexturedColoredVertex>();
        private readonly Queue<Texture> _VerticesTextures = new Queue<Texture>();
        private readonly Queue<Matrix> _VerticesRotationMatrices = new Queue<Matrix>();
        #endregion private vars

        /// <summary>
        ///     Creates a new Instance of the CDirect3D Class
        /// </summary>
        public CDirect3D()
        {
            _Form = new CRenderFormHook();
            try
            {
                _D3D = new Direct3D();
            }
            catch (Direct3DX9NotFoundException e)
            {
                CLog.LogError("No DirectX runtimes were found, please download and install them from http://www.microsoft.com/download/en/details.aspx?id=8109", true, true, e);
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

            _PresentParameters = new PresentParameters
                {
                    Windowed = true,
                    SwapEffect = SwapEffect.Discard,
                    BackBufferHeight = CConfig.ScreenH,
                    BackBufferWidth = CConfig.ScreenW,
                    BackBufferFormat = _D3D.Adapters.DefaultAdapter.CurrentDisplayMode.Format,
                    Multisample = MultisampleType.None,
                    MultisampleQuality = 0
                };

            //Apply antialiasing and check if antialiasing mode is supported

            #region Antialiasing
            int quality;
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
            _PresentParameters.PresentationInterval = CConfig.VSync == EOffOn.TR_CONFIG_ON ? PresentInterval.Default : PresentInterval.Immediate;

            //GMA 950 graphics devices can only process vertices in software mode
            Capabilities caps = _D3D.GetDeviceCaps(_D3D.Adapters.DefaultAdapter.Adapter, DeviceType.Hardware);
            CreateFlags flags = (caps.DeviceCaps & DeviceCaps.HWTransformAndLight) != 0 ? CreateFlags.HardwareVertexProcessing : CreateFlags.SoftwareVertexProcessing;

            //Check if Pow2 textures are needed
            _NonPowerOf2TextureSupported = true;
            _NonPowerOf2TextureSupported &= (caps.TextureCaps & TextureCaps.Pow2) == 0;
            _NonPowerOf2TextureSupported &= (caps.TextureCaps & TextureCaps.NonPow2Conditional) == 0;
            _NonPowerOf2TextureSupported &= (caps.TextureCaps & TextureCaps.SquareOnly) == 0;

            try
            {
                _Device = new Device(_D3D, _D3D.Adapters.DefaultAdapter.Adapter, DeviceType.Hardware, _Form.Handle, flags, _PresentParameters);
            }
            catch (Exception e)
            {
                CLog.LogError("Error during D3D device creation.", false, false, e);
            }
            finally
            {
                if (_Device == null || _Device.Disposed)
                {
                    CLog.LogError(
                        "Something went wrong during device creating, please check if your DirectX redistributables and grafic card drivers are up to date. You can download the DirectX runtimes at http://www.microsoft.com/download/en/details.aspx?id=8109",
                        true, true);
                }
            }
        }

        #region form events
        #endregion form events

        #region resize
        /// <summary>
        ///     Resizes the viewport
        /// </summary>
        protected override void _DoResize()
        {
            // The window was minimized, so restore it to the last known size
            if (_Form.ClientSize.Width == 0 || _Form.ClientSize.Height == 0)
                _Form.ClientSize = _SizeBeforeMinimize;
            if (!_Run)
                return;
            _H = _Form.ClientSize.Height;
            _W = _Form.ClientSize.Width;
            _Y = 0;
            _X = 0;

            if (_W / (float)_H > CSettings.GetRenderAspect())
            {
                //The windows width is too big
                _W = (int)Math.Round(_H * CSettings.GetRenderAspect());
                _X = (_Form.ClientSize.Width - _W) / 2;
            }
            else
            {
                //The windows height is too big
                _H = (int)Math.Round(_W / CSettings.GetRenderAspect());
                _Y = (_Form.ClientSize.Height - _H) / 2;
            }

            //Apply the new sizes to the PresentParameters
            _PresentParameters.BackBufferWidth = _Form.ClientSize.Width;
            _PresentParameters.BackBufferHeight = _Form.ClientSize.Height;
            ClearScreen();
            //To set new PresentParameters the device has to be resetted
            _Reset();
            //All configurations got flushed due to Reset(), so apply them again
            Init();

            //Set the new Viewport
            _Device.Viewport = new Viewport(_X, _Y, _W, _H);
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

        #region implementation

        #region main stuff
        /// <summary>
        ///     Inits the Device
        /// </summary>
        /// <returns>True if it succeeded else false</returns>
        public override bool Init()
        {
            if (!base.Init())
                return false;

            if (_Device.Disposed)
                return false;

            _AdjustNewBorders();

            _VertexBuffer = new VertexBuffer(_Device, CSettings.VertexBufferElements * (4 * Marshal.SizeOf(typeof(STexturedColoredVertex))), Usage.WriteOnly | Usage.Dynamic,
                                             VertexFormat.Position | VertexFormat.Texture1 | VertexFormat.Diffuse, Pool.Default);

            if (_Device.SetStreamSource(0, _VertexBuffer, 0, Marshal.SizeOf(typeof(STexturedColoredVertex))).IsFailure)
                CLog.LogError("Failed to set stream source");
            _Device.VertexDeclaration = STexturedColoredVertex.GetDeclaration(_Device);

            if (_Device.SetRenderState(RenderState.CullMode, Cull.None).IsFailure)
                CLog.LogError("Failed to set cull mode");
            if (_Device.SetRenderState(RenderState.AlphaBlendEnable, true).IsFailure)
                CLog.LogError("Failed to enable alpha blending");
            if (_Device.SetRenderState(RenderState.Lighting, false).IsFailure)
                CLog.LogError("Failed to disable lighting");
            if (_Device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha).IsFailure)
                CLog.LogError("Failed to set destination blend");
            if (_Device.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha).IsFailure)
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

            var indices = new Int16[6];
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
            using (var blankMap = new Bitmap(1, 1))
            {
                Graphics g = Graphics.FromImage(blankMap);
                g.Clear(Color.White);
                g.Dispose();
                _BlankTexture = AddTexture(blankMap);
            }
            return true;
        }

        protected override void _OnBeforeDraw()
        {
            if (_Device.BeginScene().IsFailure)
                CLog.LogError("Failed to begin scene");
        }

        protected override void _OnAfterDraw()
        {
            _RenderVertexBuffer();
            if (_Device.EndScene().IsFailure)
                CLog.LogError("Failed to end scene");
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
                    _Reset();
                    Init();
                }
            }
            Application.DoEvents();
        }

        /// <summary>
        ///     Resets the device, all objects in the Direct3D default pool get flushed and need to be recreated
        /// </summary>
        private void _Reset()
        {
            //Dispose all objects in the default pool, those need to be recreated
            STexturedColoredVertex.GetDeclaration(_Device).Dispose();
            _VertexBuffer.Dispose();
            _IndexBuffer.Dispose();
            if (_Device.Reset(_PresentParameters).IsFailure)
                CLog.LogError("Failed to reset the device");
        }

        protected override void _AdjustNewBorders()
        {
            Matrix translate = Matrix.Translation(new Vector3(-(float)CSettings.RenderW / 2, (float)CSettings.RenderH / 2, 0));
            Matrix projection = Matrix.OrthoOffCenterLH(
                -(float)CSettings.RenderW / 2 - _BorderLeft, (float)CSettings.RenderW / 2 + _BorderRight,
                -(float)CSettings.RenderH / 2 - _BorderBottom, (float)CSettings.RenderH / 2 + _BorderTop,
                CSettings.ZNear, CSettings.ZFar);

            if (_Device.SetTransform(TransformState.Projection, projection).IsFailure)
                CLog.LogError("Failed to set orthogonal matrix");
            if (_Device.SetTransform(TransformState.World, translate).IsFailure)
                CLog.LogError("Failed to set translation matrix");
        }

        /// <summary>
        ///     Unloads all Textures and other objects used by Direct3D for rendering
        /// </summary>
        /// <returns></returns>
        public override void Unload()
        {
            base.Unload();
            STexturedColoredVertex.GetDeclaration(_Device).Dispose();
            _VertexBuffer.Dispose();
            _IndexBuffer.Dispose();
            _Device.Dispose();
            _D3D.Dispose();
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
        ///     Adds a quad a list which will be added and rendered to the vertexbuffer when calling RenderToVertexBuffer to reduce vertexbuffer calls each frame to a minimum
        /// </summary>
        /// <param name="vertices">A TexturedColoredVertex array containg 4 vertices</param>
        /// <param name="tex">The texture the vertex should be textured with</param>
        /// <param name="rotation">The vertices' rotation</param>
        private void _AddToVertexBuffer(STexturedColoredVertex[] vertices, Texture tex, Matrix rotation)
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
            if (_Vertices.Count <= 0)
                return;
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
        #endregion main stuff

        #region Basic Draw Methods
        /// <summary>
        ///     Removes all textures from the screen
        /// </summary>
        public override void ClearScreen()
        {
            if (_Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0).IsFailure)
                CLog.LogError("Failed to clear the backbuffer");
        }

        /// <summary>
        ///     Copies the current frame into a texture
        ///     <returns>A texture holding the current frame</returns>
        /// </summary>
        public CTexture CopyScreen()
        {
            CTexture texture = _GetNewTextureRef(_W, _H);

            Surface backbufferSurface = _Device.GetBackBuffer(0, 0);
            var tex = new Texture(_Device, texture.W2, texture.H2, 0, Usage.AutoGenerateMipMap, Format.A8R8G8B8, Pool.Managed);
            Surface textureSurface = tex.GetSurfaceLevel(0);
            Surface.FromSurface(textureSurface, backbufferSurface, Filter.Default, 0, new Rectangle(0, 0, _W, _H), new Rectangle(0, 0, _W, _H));
            backbufferSurface.Dispose();
            lock (_Textures)
            {
                _Textures.Add(texture.ID, tex);
            }

            return texture;
        }

        /// <summary>
        ///     Copies the current frame into a texture
        /// </summary>
        /// <param name="texture">The texture in which the frame is copied to</param>
        public void CopyScreen(ref CTexture texture)
        {
            if (!_TextureExists(texture) || texture.DataSize.Width != GetScreenWidth() || texture.DataSize.Height != GetScreenHeight())
            {
                RemoveTexture(ref texture);
                texture = CopyScreen();
            }
            else
            {
                Surface backbufferSurface = _Device.GetBackBuffer(0, 0);
                Surface textureSurface = _Textures[texture.ID].GetSurfaceLevel(0);
                Surface.FromSurface(textureSurface, backbufferSurface, Filter.Default, 0);
            }
        }

        /// <summary>
        ///     Creates a Screenshot of the current frame
        /// </summary>
        public void MakeScreenShot()
        {
            string file = CHelper.GetUniqueFileName(Path.Combine(CSettings.DataFolder, CSettings.FolderNameScreenshots), "Screenshot.bmp");

            //create a surface of the frame
            using (Surface surface = _Device.GetBackBuffer(0, 0))
            {
                var screen = new Bitmap(Surface.ToStream(surface, ImageFileFormat.Bmp));
                screen.Save(file, ImageFormat.Bmp);
                screen.Dispose();
            }
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
            var lineVector = new Vector2[] {new Vector2(x1, y1), new Vector2(x2, y2)};
            using (var line = new Line(_Device))
            {
                line.Antialias = true;
                line.Begin();
                line.Draw(lineVector, new Color4((float)a / 255, (float)r / 255, (float)g / 255, (float)b / 255));
                line.End();
            }
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

        protected override Texture _CreateTexture(CTexture texture, byte[] data)
        {
            //Create a new texture in the managed pool, which does not need to be recreated on a lost device
            //because a copy of the texture is hold in the Ram
            var t = new Texture(_Device, texture.W2, texture.H2, 0, Usage.AutoGenerateMipMap, Format.A8R8G8B8, Pool.Managed);
            _WriteDataToTexture(t, texture.DataSize.Width, data);
            return t;
        }

        private static void _WriteDataToTexture(Texture t, int w, byte[] data)
        {
            //Lock the texture and fill it with the data
            DataRectangle rect = t.LockRectangle(0, LockFlags.Discard);
            int rowWidth = 4 * w;
            if (rowWidth == rect.Pitch)
                rect.Data.Write(data, 0, data.Length);
            else
            {
                for (int i = 0; i + rowWidth <= data.Length; i += rowWidth)
                {
                    rect.Data.Write(data, i, rowWidth);
                    //Go to next row
                    rect.Data.Position = rect.Data.Position - rowWidth + rect.Pitch;
                }
            }
            t.UnlockRectangle(0);
        }

        /// <summary>
        ///     Updates the data of a texture
        /// </summary>
        /// <param name="texture">The texture to update</param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="data">A byte array containing the new texture's data</param>
        /// <returns>True if succeeded</returns>
        public override bool UpdateTexture(CTexture texture, int w, int h, byte[] data)
        {
            if (!_TextureExists(texture))
                return false;
            if (texture.DataSize.Width != w || texture.DataSize.Height != h)
            {
                if (texture.W2 > w || texture.H2 > h)
                    return false; // Texture memory to small
                if (texture.W2 * 0.9 < w || texture.H2 * 0.9 < h)
                    return false; // Texture memory to big
                texture.DataSize = new Size(w, h);
            }
            lock (_Textures)
            {
                _WriteDataToTexture(_Textures[texture.ID], w, data);
            }
            return true;
        }

        #region drawing
        /// <summary>
        ///     Draws a texture
        /// </summary>
        /// <param name="texture">The texture to be drawn</param>
        public void DrawTexture(CTexture texture)
        {
            if (texture == null)
                return;
            DrawTexture(texture, texture.Rect, texture.Color);
        }

        /// <summary>
        ///     Draws a texture
        /// </summary>
        /// <param name="texture">The texture to be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        public void DrawTexture(CTexture texture, SRectF rect)
        {
            if (texture == null)
                return;
            DrawTexture(texture, rect, texture.Color);
        }

        /// <summary>
        ///     Draws a texture
        /// </summary>
        /// <param name="texture">The texture to be drawn</param>
        /// <param name="rect">A SRectF struct containing the destination coordinates</param>
        /// <param name="color">A SColorF struct containing a color which the texture will be colored in</param>
        /// <param name="mirrored">True if the texture should be mirrored</param>
        public void DrawTexture(CTexture texture, SRectF rect, SColorF color, bool mirrored = false)
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
        public void DrawTexture(CTexture texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored = false)
        {
            if (!_TextureExists(texture))
                return;
            if (_Textures[texture.ID] == null)
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

            color.A *= CGraphics.GlobalAlpha;

            if (color.A > 1)
                color.A = 1;
            if (color.R > 1)
                color.R = 1;
            if (color.G > 1)
                color.G = 1;
            if (color.B > 1)
                color.B = 1;

            Color c = color.AsColor();

            if (!mirrored)
            {
                var vert = new STexturedColoredVertex[4];
                vert[0] = new STexturedColoredVertex(new Vector3(rx1, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x1, y1), c.ToArgb());
                vert[1] = new STexturedColoredVertex(new Vector3(rx1, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x1, y2), c.ToArgb());
                vert[2] = new STexturedColoredVertex(new Vector3(rx2, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x2, y2), c.ToArgb());
                vert[3] = new STexturedColoredVertex(new Vector3(rx2, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x2, y1), c.ToArgb());
                _AddToVertexBuffer(vert, _Textures[texture.ID], _CalculateRotationMatrix(rect.Rotation, rx1, rx2, ry1, ry2));
            }
            else
            {
                var vert = new STexturedColoredVertex[4];
                vert[0] = new STexturedColoredVertex(new Vector3(rx1, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x1, -y1), c.ToArgb());
                vert[1] = new STexturedColoredVertex(new Vector3(rx1, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x1, -y2), c.ToArgb());
                vert[2] = new STexturedColoredVertex(new Vector3(rx2, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x2, -y2), c.ToArgb());
                vert[3] = new STexturedColoredVertex(new Vector3(rx2, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x2, -y1), c.ToArgb());
                _AddToVertexBuffer(vert, _Textures[texture.ID], _CalculateRotationMatrix(rect.Rotation, rx1, rx2, ry1, ry2));
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
        public void DrawTexture(CTexture texture, SRectF rect, SColorF color, float begin, float end)
        {
            if (!_TextureExists(texture))
                return;
            if (_Textures[texture.ID] == null)
                return;

            float x1 = 0f + begin * texture.WidthRatio;
            float x2 = texture.WidthRatio * end;
            const float y1 = 0f;
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

            color.A *= CGraphics.GlobalAlpha;

            Color c = color.AsColor();

            var vert = new STexturedColoredVertex[4];
            vert[0] = new STexturedColoredVertex(new Vector3(rx1, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x1, y1), c.ToArgb());
            vert[1] = new STexturedColoredVertex(new Vector3(rx1, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x1, y2), c.ToArgb());
            vert[2] = new STexturedColoredVertex(new Vector3(rx2, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x2, y2), c.ToArgb());
            vert[3] = new STexturedColoredVertex(new Vector3(rx2, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x2, y1), c.ToArgb());
            _AddToVertexBuffer(vert, _Textures[texture.ID], _CalculateRotationMatrix(rect.Rotation, rx1, rx2, ry1, ry2));
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
        public void DrawTextureReflection(CTexture texture, SRectF rect, SColorF color, SRectF bounds, float space, float height)
        {
            if (!_TextureExists(texture))
                return;
            if (_Textures[texture.ID] == null)
                return;

            if (Math.Abs(rect.W) < float.Epsilon || Math.Abs(rect.H) < float.Epsilon || Math.Abs(bounds.H) < float.Epsilon || Math.Abs(bounds.W) < float.Epsilon ||
                Math.Abs(color.A) < float.Epsilon || height <= 0f)
                return;

            if (bounds.X > rect.X + rect.W || bounds.X + bounds.W < rect.X)
                return;

            if (bounds.Y > rect.Y + rect.H || bounds.Y + bounds.H < rect.Y)
                return;

            if (height > bounds.H)
                height = bounds.H;

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

            color.A *= CGraphics.GlobalAlpha;
            Color c = color.AsColor();
            color.A = 0;
            Color transparent = color.AsColor();

            var vert = new STexturedColoredVertex[4];
            vert[0] = new STexturedColoredVertex(new Vector3(rx1, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x1, y2), c.ToArgb());
            vert[1] = new STexturedColoredVertex(new Vector3(rx1, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x1, y1), transparent.ToArgb());
            vert[2] = new STexturedColoredVertex(new Vector3(rx2, -ry2, rect.Z + CGraphics.ZOffset), new Vector2(x2, y1), transparent.ToArgb());
            vert[3] = new STexturedColoredVertex(new Vector3(rx2, -ry1, rect.Z + CGraphics.ZOffset), new Vector2(x2, y2), c.ToArgb());
            _AddToVertexBuffer(vert, _Textures[texture.ID], _CalculateRotationMatrix(rect.Rotation, rx1, rx2, ry1, ry2));
        }
        #endregion drawing

        #endregion Textures

        #endregion implementation

        #region utility
        private static Matrix _CalculateRotationMatrix(float rot, float rx1, float rx2, float ry1, float ry2)
        {
            Matrix originTranslation = Matrix.Translation(new Vector3(-(float)CSettings.RenderW / 2, (float)CSettings.RenderH / 2, 0));
            if (Math.Abs(rot) > float.Epsilon)
            {
                float rotation = rot * (float)Math.PI / 180;
                float centerX = (rx1 + rx2) / 2f;
                float centerY = -(ry1 + ry2) / 2f;

                Matrix translationA = Matrix.Translation(-centerX, -centerY, 0);
                Matrix rotationMat = Matrix.RotationZ(-rotation);
                Matrix translationB = Matrix.Translation(centerX, centerY, 0);

                //Multiplicate the matrices to get the real world matrix,
                //First shift the texture into the center
                //Rotate it and shift it back to the origin position
                //Apply the originTranslation after
                Matrix result = translationA * rotationMat * translationB * originTranslation;
                return result;
            }
            return originTranslation;
        }
        #endregion utility

        private struct STexturedColoredVertex
        {
            private static VertexDeclaration _Declaration;
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

            public static VertexDeclaration GetDeclaration(Device device)
            {
                if (_Declaration == null || _Declaration.Disposed)
                    _Declaration = new VertexDeclaration(device, _Elements);

                return _Declaration;
            }
        }
    }
}