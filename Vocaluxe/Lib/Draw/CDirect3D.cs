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

    class CD3DTexture : CTextureBase
    {
        public readonly Texture D3DTexture;

        public CD3DTexture(Device device, Size dataSize, int texWidth = 0, int texHeight = 0) : base(dataSize, new Size(texWidth, texHeight))
        {
            //Create a new texture in the managed pool, which does not need to be recreated on a lost device
            //because a copy of the texture is hold in the Ram
            D3DTexture = device == null ? null : new Texture(device, texWidth, texHeight, 0, Usage.AutoGenerateMipMap, Format.A8R8G8B8, Pool.Managed);
        }

        public override bool IsLoaded
        {
            get { return D3DTexture != null; }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (D3DTexture != null)
                D3DTexture.Dispose();
        }
    }

    class CDirect3D : CDrawBaseWindows<CD3DTexture>, IDraw
    {
        private readonly Direct3D _D3D;
        private readonly Device _Device;
        private readonly PresentParameters _PresentParameters;

        private VertexBuffer _VertexBuffer;
        private IndexBuffer _IndexBuffer;

        private CTextureRef _BlankTexture;

        private readonly Queue<STexturedColoredVertex> _Vertices = new Queue<STexturedColoredVertex>();
        private readonly Queue<Texture> _VerticesTextures = new Queue<Texture>();
        private readonly Queue<Matrix> _VerticesRotationMatrices = new Queue<Matrix>();

        /// <summary>
        ///     Creates a new Instance of the CDirect3D Class
        /// </summary>
        public CDirect3D()
        {
            _Form = new CRenderFormHook {ClientSize = new Size(CConfig.Config.Graphics.ScreenW, CConfig.Config.Graphics.ScreenH)};

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
                    BackBufferHeight = CConfig.Config.Graphics.ScreenH,
                    BackBufferWidth = CConfig.Config.Graphics.ScreenW,
                    BackBufferFormat = _D3D.Adapters.DefaultAdapter.CurrentDisplayMode.Format,
                    Multisample = MultisampleType.None,
                    MultisampleQuality = 0
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
            _PresentParameters.PresentationInterval = CConfig.Config.Graphics.VSync == EOffOn.TR_CONFIG_ON ? PresentInterval.Default : PresentInterval.Immediate;

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

        #region resize
        /// <summary>
        ///     Resizes the viewport
        /// </summary>
        protected override void _DoResize()
        {
            // The window was minimized, so restore it to the last known size
            if (_Form.ClientSize.Width == 0 || _Form.ClientSize.Height == 0)
                _Form.ClientSize = _SizeBeforeMinimize;

            if (_H == _Form.ClientSize.Height && _W == _Form.ClientSize.Width && CConfig.Config.Graphics.ScreenAlignment == _CurrentAlignment)
                return;

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
                //All configurations got flushed due to Reset(), so apply them again
                _InitDevice();
                _Device.Viewport = new Viewport(_X, _Y, _W, _H);
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
                return false;

            if (_Device.Disposed)
                return false;

            _InitDevice();

            //This creates a new white texture and adds it to the texture pool
            //This texture is used for the DrawRect method
            using (var blankMap = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(blankMap))
            {
                g.Clear(Color.White);
                _BlankTexture = AddTexture(blankMap);
            }
            return true;
        }

        private void _InitDevice()
        {
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
                    _InitDevice();
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
            const float dx = (float)CSettings.RenderW / 2;
            const float dy = (float)CSettings.RenderH / 2;
            Matrix translate = Matrix.Translation(new Vector3(-dx, dy, 0));
            Matrix projection = Matrix.OrthoOffCenterLH(
                -dx - _BorderLeft, dx + _BorderRight,
                -dy - _BorderBottom, dy + _BorderTop,
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
        public override void Close()
        {
            base.Close();
            STexturedColoredVertex.DisposeDeclaration();
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
            int c = color.AsColor().ToArgb();
            int c2;
            if (isReflection)
            {
                color.A = 0;
                c2 = color.AsColor().ToArgb();
            }
            else
                c2 = c;

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

        /// <summary>
        ///     Removes all textures from the screen
        /// </summary>
        protected override void _ClearScreen()
        {
            if (_Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0).IsFailure)
                CLog.LogError("Failed to clear the backbuffer");
        }

        /// <summary>
        ///     Copies the current frame into a texture
        ///     <returns>A texture holding the current frame</returns>
        /// </summary>
        public CTextureRef CopyScreen()
        {
            CD3DTexture tex = _CreateTexture(new Size(_W, _H));
            Surface backbufferSurface = _Device.GetBackBuffer(0, 0);
            Surface textureSurface = tex.D3DTexture.GetSurfaceLevel(0);
            Surface.FromSurface(textureSurface, backbufferSurface, Filter.Default, 0, new Rectangle(0, 0, _W, _H), new Rectangle(0, 0, _W, _H));
            backbufferSurface.Dispose();

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
                Surface backbufferSurface = _Device.GetBackBuffer(0, 0);
                Surface textureSurface = texture.D3DTexture.GetSurfaceLevel(0);
                Surface.FromSurface(textureSurface, backbufferSurface, Filter.Default, 0);
                backbufferSurface.Dispose();
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
        ///     Draws a colored rectangle
        /// </summary>
        /// <param name="color">The color in which the rectangle will be drawn in</param>
        /// <param name="rect">The coordinates in a SRectF struct</param>
        public void DrawRect(SColorF color, SRectF rect)
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
        public void DrawRectReflection(SColorF color, SRectF rect, float space, float height)
        {
            DrawTextureReflection(_BlankTexture, rect, color, rect, space, height);
        }

        protected override CD3DTexture _CreateTexture(Size dataSize)
        {
            if (dataSize.Width < 0)
                return new CD3DTexture(null, dataSize);
            return new CD3DTexture(_Device, dataSize, _CheckForNextPowerOf2(dataSize.Width), _CheckForNextPowerOf2(dataSize.Height));
        }

        protected override void _WriteDataToTexture(CD3DTexture texture, byte[] data)
        {
            //Lock the texture and fill it with the data
            DataRectangle rect = texture.D3DTexture.LockRectangle(0, LockFlags.Discard);
            int rowWidth = 4 * texture.DataSize.Width;
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
            texture.D3DTexture.UnlockRectangle(0);
        }

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

            public static void DisposeDeclaration()
            {
                if (_Declaration != null && !_Declaration.Disposed)
                {
                    _Declaration.Dispose();
                    _Declaration = null;
                }
            }
        }
    }
}