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
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using SDL;
using SharpGen.Runtime;
using SkiaSharp;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Log;
using Color = System.Drawing.Color;
using Format = Vortice.DXGI.Format;
using MapFlags = Vortice.Direct3D11.MapFlags;
using Size = System.Drawing.Size;

namespace Vocaluxe.Lib.Draw
{
    class CD3DTexture : CTextureBase
    {
        public readonly ID3D11Texture2D D3DTexture;
        public readonly ID3D11ShaderResourceView View;
        public readonly bool HasMips;
        public ID3D11Texture2D[] Planes;
        public ID3D11ShaderResourceView[] PlaneViews;
        public ID3D11RenderTargetView TargetView;

        public CD3DTexture(ID3D11Device device, Size dataSize, int texWidth = 0, int texHeight = 0) : base(dataSize, new Size(texWidth, texHeight))
        {
            if (device == null)
            {
                return;
            }

            HasMips = texWidth < 1280 && texHeight < 720;
            var description = new Texture2DDescription(Format.B8G8R8A8_UNorm, (uint)texWidth, (uint)texHeight, 1, HasMips ? 0u : 1u,
                BindFlags.ShaderResource | BindFlags.RenderTarget, ResourceUsage.Default, CpuAccessFlags.None, 1, 0,
                HasMips ? ResourceOptionFlags.GenerateMips : ResourceOptionFlags.None);
            D3DTexture = device.CreateTexture2D(description);
            View = device.CreateShaderResourceView(D3DTexture);
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
            TargetView?.Dispose();
            if (Planes != null)
            {
                for (var i = 0; i < Planes.Length; i++)
                {
                    PlaneViews[i]?.Dispose();
                    Planes[i]?.Dispose();
                }
            }

            View?.Dispose();
            D3DTexture?.Dispose();
        }
    }

    unsafe class CDirect3D : CDrawBaseSDL<CD3DTexture>, IDraw
    {
        private const Format _BackBufferFormat = Format.B8G8R8A8_UNorm;
        private const string _YuvShader = @"
cbuffer CYuv : register(b0)
{
    float4 RowR;
    float4 RowG;
    float4 RowB;
    float4 Size;
};

Texture2D TexY : register(t0);
Texture2D TexU : register(t1);
Texture2D TexV : register(t2);
SamplerState Sampler : register(s0);

float4 VS(uint id : SV_VertexID) : SV_POSITION
{
    float2 position = float2((id << 1) & 2, id & 2);
    return float4(position * float2(2, -2) + float2(-1, 1), 0, 1);
}

float4 PS(float4 position : SV_POSITION) : SV_TARGET
{
    float2 uv = position.xy / Size.xy;
    float4 yuv = float4(TexY.Sample(Sampler, uv).r, TexU.Sample(Sampler, uv).r, TexV.Sample(Sampler, uv).r, 1);
    return float4(saturate(dot(RowR, yuv)), saturate(dot(RowG, yuv)), saturate(dot(RowB, yuv)), 1);
}";
        private const string _Shader = @"
cbuffer CTransform : register(b0)
{
    row_major float4x4 Projection;
};

Texture2D Tex : register(t0);
SamplerState Sampler : register(s0);

struct SVertex
{
    float3 Position : POSITION;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
};

struct SPixel
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
};

SPixel VS(SVertex input)
{
    SPixel output;
    output.Position = mul(float4(input.Position, 1), Projection);
    output.TexCoord = input.TexCoord;
    output.Color = input.Color;
    return output;
}

float4 PS(SPixel input) : SV_TARGET
{
    return Tex.Sample(Sampler, input.TexCoord) * input.Color;
}";

        private readonly IDXGIFactory2 _Factory;
        private readonly ID3D11Device _Device;
        private readonly ID3D11DeviceContext _Context;
        private IDXGISwapChain1 _SwapChain;
        private WaitHandle _FrameLatencyWaitHandle;
        private readonly bool _TearingSupported;
        private readonly uint _SampleCount = 1;
        private readonly uint _SampleQuality;

        private ID3D11Texture2D _BackBuffer;
        private ID3D11Texture2D _RenderTarget;
        private ID3D11RenderTargetView _RenderTargetView;
        private ID3D11Texture2D _DepthBuffer;
        private ID3D11DepthStencilView _DepthStencilView;
        private int _BufferWidth;
        private int _BufferHeight;

        private ID3D11VertexShader _VertexShader;
        private ID3D11PixelShader _PixelShader;
        private ID3D11InputLayout _InputLayout;
        private ID3D11Buffer _VertexBuffer;
        private ID3D11Buffer _IndexBuffer;
        private ID3D11Buffer _ConstantBuffer;
        private ID3D11BlendState _BlendState;
        private ID3D11DepthStencilState _DepthStencilState;
        private ID3D11RasterizerState _RasterizerState;
        private ID3D11SamplerState _SamplerState;
        private ID3D11VertexShader _YuvVertexShader;
        private ID3D11PixelShader _YuvPixelShader;
        private ID3D11Buffer _YuvConstantBuffer;

        private CTextureRef _BlankTexture;

        private readonly List<STexturedColoredVertex> _Vertices = new List<STexturedColoredVertex>();
        private readonly List<ID3D11ShaderResourceView> _VerticesTextures = new List<ID3D11ShaderResourceView>();

        /// <summary>
        ///     Creates a new Instance of the CDirect3D Class
        /// </summary>
        public CDirect3D()
        {
            if (!_CreateWindow(0))
            {
                return;
            }

            try
            {
                _Factory = DXGI.CreateDXGIFactory1<IDXGIFactory2>();
                var featureLevels = new[] { FeatureLevel.Level_11_0, FeatureLevel.Level_10_1, FeatureLevel.Level_10_0 };
                D3D11.D3D11CreateDevice(IntPtr.Zero, DriverType.Hardware, DeviceCreationFlags.BgraSupport, featureLevels, out _Device, out _Context).CheckError();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Direct3D 11 could not be initialized, please check if your graphic card drivers are up to date.");
            }

            if (_Device == null)
            {
                CLog.Fatal("Direct3D 11 is not available on this system.");
                return;
            }

            using (var factory5 = _Factory.QueryInterfaceOrNull<IDXGIFactory5>())
            {
                _TearingSupported = factory5 != null && factory5.PresentAllowTearing;
            }

            //Apply antialiasing and check if antialiasing mode is supported
            #region Antialiasing
            uint sampleCount;
            switch (CConfig.Config.Graphics.AAMode)
            {
                case EAntiAliasingModes.X2:
                    sampleCount = 2;
                    break;
                case EAntiAliasingModes.X4:
                    sampleCount = 4;
                    break;
                case EAntiAliasingModes.X8:
                    sampleCount = 8;
                    break;
                case EAntiAliasingModes.X16:
                case EAntiAliasingModes.X32: //x32 is not supported, fallback to x16
                    sampleCount = 16;
                    break;
                default:
                    sampleCount = 1;
                    break;
            }

            if (sampleCount > 1)
            {
                var quality = _Device.CheckMultisampleQualityLevels(_BackBufferFormat, sampleCount);
                if (quality == 0)
                {
                    CLog.Error("[Direct3D] This AAMode is not supported by this device or driver, fallback to no AA");
                }
                else
                {
                    _SampleCount = sampleCount;
                    _SampleQuality = quality - 1;
                }
            }
            #endregion Antialiasing

            _NonPowerOf2TextureSupported = true;
        }

        #region resize
        /// <summary>
        ///     Resizes the viewport
        /// </summary>
        protected override void _DoResize()
        {
            var clientSize = _GetClientSize();

            // The window was minimized, so keep the last known size
            if (clientSize.Width == 0 || clientSize.Height == 0)
            {
                return;
            }

            if (_H == clientSize.Height && _W == clientSize.Width && CConfig.Config.Graphics.ScreenAlignment == _CurrentAlignment)
            {
                return;
            }

            _CurrentAlignment = CConfig.Config.Graphics.ScreenAlignment;
            _H = clientSize.Height;
            _W = clientSize.Width;
            _X = 0;
            _Y = 0;

            if (CConfig.Config.Graphics.Stretch != EOffOn.TR_CONFIG_ON)
            {
                _AdjustAspect(false);
            }

            if (_SwapChain != null)
            {
                _CreateSizeDependentResources(clientSize.Width, clientSize.Height);
            }
        }

        // ReSharper disable RedundantOverridenMember
        /// <summary>
        ///     Triggers the Fullscreen mode
        /// </summary>
        protected override void _EnterFullScreen()
        {
            //This currently not using real fullscreen mode but a borderless window
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

            try
            {
                _InitDevice();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error during D3D device initialization.");
                return false;
            }

            //This creates a new white texture and adds it to the texture pool
            //This texture is used for the DrawRect method
            _BlankTexture = AddTexture(1, 1, new byte[] { 255, 255, 255, 255 });

            return true;
        }

        private void _InitDevice()
        {
            var clientSize = _GetClientSize();
            var hwnd = SDL3.SDL_GetPointerProperty(SDL3.SDL_GetWindowProperties(_Window), SDL3.SDL_PROP_WINDOW_WIN32_HWND_POINTER, IntPtr.Zero);
            var description = new SwapChainDescription1
            {
                Width = (uint)clientSize.Width,
                Height = (uint)clientSize.Height,
                Format = _BackBufferFormat,
                SampleDescription = new SampleDescription(1, 0),
                BufferUsage = Usage.RenderTargetOutput,
                BufferCount = 2,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipDiscard,
                AlphaMode = AlphaMode.Ignore,
                Flags = _SwapChainFlags
            };
            _SwapChain = _Factory.CreateSwapChainForHwnd(_Device, hwnd, description);
            using (var swapChain2 = _SwapChain.QueryInterface<IDXGISwapChain2>())
            {
                swapChain2.MaximumFrameLatency = 1;
                _FrameLatencyWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset) { SafeWaitHandle = new SafeWaitHandle(swapChain2.FrameLatencyWaitableObject, true) };
            }

            _Factory.MakeWindowAssociation(hwnd, WindowAssociationFlags.IgnoreAltEnter);
            _CreateSizeDependentResources(clientSize.Width, clientSize.Height);

            var vertexShader = _CompileShader(_Shader, "VS", "vs_4_0");
            var pixelShader = _CompileShader(_Shader, "PS", "ps_4_0");
            _VertexShader = _Device.CreateVertexShader(vertexShader);
            _PixelShader = _Device.CreatePixelShader(pixelShader);
            _InputLayout = _Device.CreateInputLayout(new[]
            {
                new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, sizeof(float) * 3, 0),
                new InputElementDescription("COLOR", 0, Format.B8G8R8A8_UNorm, sizeof(float) * 5, 0)
            }, vertexShader);

            var vertexSize = (uint)Marshal.SizeOf<STexturedColoredVertex>();
            _VertexBuffer = _Device.CreateBuffer(CSettings.VertexBufferElements * 4 * vertexSize, BindFlags.VertexBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write,
                ResourceOptionFlags.None, 0);

            var indices = new ushort[CSettings.VertexBufferElements * 6];
            for (var i = 0; i < CSettings.VertexBufferElements; i++)
            {
                var vertex = (ushort)(i * 4);
                indices[i * 6] = vertex;
                indices[i * 6 + 1] = (ushort)(vertex + 1);
                indices[i * 6 + 2] = (ushort)(vertex + 2);
                indices[i * 6 + 3] = vertex;
                indices[i * 6 + 4] = (ushort)(vertex + 2);
                indices[i * 6 + 5] = (ushort)(vertex + 3);
            }

            _IndexBuffer = _Device.CreateBuffer(indices, BindFlags.IndexBuffer, ResourceUsage.Immutable, CpuAccessFlags.None, ResourceOptionFlags.None, 0, 0);
            _ConstantBuffer = _Device.CreateBuffer((uint)Marshal.SizeOf<Matrix4x4>(), BindFlags.ConstantBuffer, ResourceUsage.Default, CpuAccessFlags.None,
                ResourceOptionFlags.None, 0);

            _BlendState = _Device.CreateBlendState(new BlendDescription(Blend.SourceAlpha, Blend.InverseSourceAlpha, Blend.Zero, Blend.One));
            _DepthStencilState = _Device.CreateDepthStencilState(new DepthStencilDescription(true, DepthWriteMask.All, ComparisonFunction.LessEqual));
            _RasterizerState = _Device.CreateRasterizerState(new RasterizerDescription(CullMode.None, FillMode.Solid) { DepthClipEnable = true, MultisampleEnable = _SampleCount > 1 });
            _SamplerState = _Device.CreateSamplerState(new SamplerDescription(Filter.MinMagMipLinear, TextureAddressMode.Clamp, 0, 1, ComparisonFunction.Never, 0, float.MaxValue));

            try
            {
                _YuvVertexShader = _Device.CreateVertexShader(_CompileShader(_YuvShader, "VS", "vs_4_0"));
                _YuvPixelShader = _Device.CreatePixelShader(_CompileShader(_YuvShader, "PS", "ps_4_0"));
                _YuvConstantBuffer = _Device.CreateBuffer(16 * sizeof(float), BindFlags.ConstantBuffer, ResourceUsage.Default, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            }
            catch (Exception e)
            {
                CLog.Error(e, "[Direct3D] Unable to compile the YUV shader, using CPU conversion for videos");
                _YuvPixelShader?.Dispose();
                _YuvPixelShader = null;
            }

            _AdjustNewBorders();
        }

        private SwapChainFlags _SwapChainFlags
        {
            get { return SwapChainFlags.FrameLatencyWaitableObject | (_TearingSupported ? SwapChainFlags.AllowTearing : SwapChainFlags.None); }
        }

        private static byte[] _CompileShader(string source, string entryPoint, string profile)
        {
            Blob code = null, errors = null;
            try
            {
                var result = Compiler.Compile(source, entryPoint, "Vocaluxe", profile, out code, out errors);
                if (result.Failure)
                {
                    throw new Exception("Shader compilation failed: " + (errors != null ? errors.AsString() : result.ToString()));
                }

                return code.AsBytes();
            }
            finally
            {
                code?.Dispose();
                errors?.Dispose();
            }
        }

        private void _CreateSizeDependentResources(int width, int height)
        {
            width = Math.Max(1, width);
            height = Math.Max(1, height);
            if (_BackBuffer != null && width == _BufferWidth && height == _BufferHeight)
            {
                return;
            }

            _Context.OMSetRenderTargets((ID3D11RenderTargetView)null, null);
            _DisposeSizeDependentResources();

            _SwapChain.ResizeBuffers(2, (uint)width, (uint)height, _BackBufferFormat, _SwapChainFlags).CheckError();
            _BufferWidth = width;
            _BufferHeight = height;

            _BackBuffer = _SwapChain.GetBuffer<ID3D11Texture2D>(0);
            if (_SampleCount > 1)
            {
                _RenderTarget = _Device.CreateTexture2D(new Texture2DDescription(_BackBufferFormat, (uint)width, (uint)height, 1, 1, BindFlags.RenderTarget,
                    ResourceUsage.Default, CpuAccessFlags.None, _SampleCount, _SampleQuality, ResourceOptionFlags.None));
            }

            _RenderTargetView = _Device.CreateRenderTargetView(_RenderTarget ?? _BackBuffer);
            _DepthBuffer = _Device.CreateTexture2D(new Texture2DDescription(Format.D24_UNorm_S8_UInt, (uint)width, (uint)height, 1, 1, BindFlags.DepthStencil,
                ResourceUsage.Default, CpuAccessFlags.None, _SampleCount, _SampleQuality, ResourceOptionFlags.None));
            _DepthStencilView = _Device.CreateDepthStencilView(_DepthBuffer);
            _ClearScreen();
        }

        private void _DisposeSizeDependentResources()
        {
            _DepthStencilView?.Dispose();
            _DepthStencilView = null;
            _DepthBuffer?.Dispose();
            _DepthBuffer = null;
            _RenderTargetView?.Dispose();
            _RenderTargetView = null;
            _RenderTarget?.Dispose();
            _RenderTarget = null;
            _BackBuffer?.Dispose();
            _BackBuffer = null;
        }

        protected override void _OnBeforeDraw()
        {
            _FrameLatencyWaitHandle?.WaitOne(1000);
            _ApplyPipeline();
        }

        private void _ApplyPipeline()
        {
            _Context.OMSetRenderTargets(_RenderTargetView, _DepthStencilView);
            _Context.RSSetViewport(new Viewport(_X, _Y, _W, _H, 0f, 1f));
            _Context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            _Context.IASetInputLayout(_InputLayout);
            _Context.IASetVertexBuffer(0, _VertexBuffer, (uint)Marshal.SizeOf<STexturedColoredVertex>(), 0);
            _Context.IASetIndexBuffer(_IndexBuffer, Format.R16_UInt, 0);
            _Context.VSSetShader(_VertexShader);
            _Context.VSSetConstantBuffer(0, _ConstantBuffer);
            _Context.PSSetShader(_PixelShader);
            _Context.PSSetSampler(0, _SamplerState);
            _Context.OMSetBlendState(_BlendState);
            _Context.OMSetDepthStencilState(_DepthStencilState, 0);
            _Context.RSSetState(_RasterizerState);
        }

        protected override void _OnAfterDraw()
        {
            _RenderVertexBuffer();
            _ResolveRenderTarget();

            //Now push the frame to the Viewport
            var vSync = CConfig.Config.Graphics.VSync == EOffOn.TR_CONFIG_ON;
            var result = _SwapChain.Present(vSync ? 1u : 0u, !vSync && _TearingSupported ? PresentFlags.AllowTearing : PresentFlags.None);
            if (result.Failure)
            {
                if (result.Code == Vortice.DXGI.ResultCode.DeviceRemoved.Code || result.Code == Vortice.DXGI.ResultCode.DeviceReset.Code)
                {
                    CLog.Error("The graphics device was removed: " + _Device.DeviceRemovedReason);
                }
                else
                {
                    CLog.Error("Failed to present the frame: " + result);
                }
            }

            _HandleEvents();
        }

        private void _ResolveRenderTarget()
        {
            if (_RenderTarget != null)
            {
                _Context.ResolveSubresource(_BackBuffer, 0, _RenderTarget, 0, _BackBufferFormat);
            }
        }

        protected override void _AdjustNewBorders()
        {
            if (_ConstantBuffer == null)
            {
                return;
            }

            const float dx = (float)CSettings.RenderW / 2;
            const float dy = (float)CSettings.RenderH / 2;
            var projection = _CreateOrthoOffCenterLH(
                -dx - _BorderLeft, CSettings.RenderW * CConfig.Config.Graphics.NumScreens - dx + _BorderRight,
                -dy - _BorderBottom, dy + _BorderTop,
                CSettings.ZNear, CSettings.ZFar);

            _Context.UpdateSubresource(in projection, _ConstantBuffer);
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
            _Context?.ClearState();
            _DisposeSizeDependentResources();
            _YuvConstantBuffer?.Dispose();
            _YuvPixelShader?.Dispose();
            _YuvVertexShader?.Dispose();
            _SamplerState?.Dispose();
            _RasterizerState?.Dispose();
            _DepthStencilState?.Dispose();
            _BlendState?.Dispose();
            _ConstantBuffer?.Dispose();
            _IndexBuffer?.Dispose();
            _VertexBuffer?.Dispose();
            _InputLayout?.Dispose();
            _PixelShader?.Dispose();
            _VertexShader?.Dispose();
            _FrameLatencyWaitHandle?.Dispose();
            _SwapChain?.Dispose();
            _Context?.Dispose();
            _Device?.Dispose();
            _Factory?.Dispose();
        }

        /// <summary>
        ///     Gets the current viewport width
        /// </summary>
        /// <returns>The current viewport width</returns>
        public int GetScreenWidth()
        {
            return _W;
        }

        /// <summary>
        ///     Gets the current viewport height
        /// </summary>
        /// <returns>The current viewport height</returns>
        public int GetScreenHeight()
        {
            return _H;
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

            var rotation = _CalculateRotationMatrix(dc.Rotation, dc.Wx1, dc.Wx2, dc.Wy1, dc.Wy2);
            var vert = new STexturedColoredVertex[4];
            vert[0] = new STexturedColoredVertex(Vector3.Transform(new Vector3(dc.Wx1, -dc.Wy1, dc.Wz), rotation), new Vector2(dc.Tx1, dc.Ty1), c);
            vert[1] = new STexturedColoredVertex(Vector3.Transform(new Vector3(dc.Wx1, -dc.Wy2, dc.Wz), rotation), new Vector2(dc.Tx1, dc.Ty2), c2);
            vert[2] = new STexturedColoredVertex(Vector3.Transform(new Vector3(dc.Wx2, -dc.Wy2, dc.Wz), rotation), new Vector2(dc.Tx2, dc.Ty2), c2);
            vert[3] = new STexturedColoredVertex(Vector3.Transform(new Vector3(dc.Wx2, -dc.Wy1, dc.Wz), rotation), new Vector2(dc.Tx2, dc.Ty1), c);
            _AddToVertexBuffer(vert, texture.View);
        }

        /// <summary>
        ///     Adds a quad a list which will be added and rendered to the vertexbuffer when calling RenderToVertexBuffer to reduce vertexbuffer calls each frame to a minimum
        /// </summary>
        /// <param name="vertices">A TexturedColoredVertex array containg 4 vertices</param>
        /// <param name="tex">The texture the vertex should be textured with</param>
        private void _AddToVertexBuffer(STexturedColoredVertex[] vertices, ID3D11ShaderResourceView tex)
        {
            //The vertexbuffer is full, so we need to flush it before we can continue
            if (_Vertices.Count >= CSettings.VertexBufferElements)
            {
                _RenderVertexBuffer();
            }

            _Vertices.AddRange(vertices);
            _VerticesTextures.Add(tex);
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
                    var mapped = _Context.Map(_VertexBuffer, MapMode.WriteDiscard, MapFlags.None);
                    try
                    {
                        var vertexData = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(_Vertices));
                        unsafe
                        {
                            vertexData.CopyTo(new Span<byte>(mapped.DataPointer.ToPointer(), vertexData.Length));
                        }
                    }
                    finally
                    {
                        _Context.Unmap(_VertexBuffer, 0);
                    }

                    var quads = _VerticesTextures.Count;
                    var start = 0;
                    while (start < quads)
                    {
                        var texture = _VerticesTextures[start];
                        var end = start + 1;
                        while (end < quads && _VerticesTextures[end] == texture)
                        {
                            end++;
                        }

                        _Context.PSSetShaderResource(0, texture);
                        _Context.DrawIndexed((uint)(end - start) * 6, (uint)start * 6, 0);
                        start = end;
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
        }
        #endregion main stuff

        /// <summary>
        ///     Removes all textures from the screen
        /// </summary>
        protected override void _ClearScreen()
        {
            if (_RenderTargetView == null)
            {
                return;
            }

            _Context.ClearRenderTargetView(_RenderTargetView, new Color4(0f, 0f, 0f, 1f));
            _Context.ClearDepthStencilView(_DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
        }

        private ID3D11Texture2D _GetCurrentFrame()
        {
            _RenderVertexBuffer();
            _ResolveRenderTarget();
            return _BackBuffer;
        }

        private int[] _ReadBackBuffer(out int width, out int height)
        {
            width = _BufferWidth;
            height = _BufferHeight;
            try
            {
                var frame = _GetCurrentFrame();
                using (var staging = _Device.CreateTexture2D(new Texture2DDescription(_BackBufferFormat, (uint)width, (uint)height, 1, 1, BindFlags.None,
                    ResourceUsage.Staging, CpuAccessFlags.Read, 1, 0, ResourceOptionFlags.None)))
                {
                    _Context.CopyResource(staging, frame);
                    var mapped = _Context.Map(staging, 0, MapMode.Read, MapFlags.None);
                    var pixels = new int[width * height];
                    try
                    {
                        for (var y = 0; y < height; y++)
                        {
                            Marshal.Copy(IntPtr.Add(mapped.DataPointer, y * (int)mapped.RowPitch), pixels, y * width, width);
                        }
                    }
                    finally
                    {
                        _Context.Unmap(staging, 0);
                    }

                    for (var i = 0; i < pixels.Length; i++)
                    {
                        pixels[i] |= unchecked((int)0xFF000000);
                    }

                    return pixels;
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
            try
            {
                var frame = _GetCurrentFrame();
                var width = Math.Min(Math.Min(_W, _BufferWidth - _X), texture.Size.Width);
                var height = Math.Min(Math.Min(_H, _BufferHeight - _Y), texture.Size.Height);
                if (width <= 0 || height <= 0)
                {
                    return;
                }

                _Context.CopySubresourceRegion(texture.D3DTexture, 0, 0, 0, 0, frame, 0, new Box(_X, _Y, 0, _X + width, _Y + height, 1));
                if (texture.HasMips)
                {
                    _Context.GenerateMips(texture.View);
                }
            }
            catch (SharpGenException e)
            {
                CLog.Error(e, "Failed to copy the backbuffer");
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

            using (var screen = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque)))
            {
                for (var y = 0; y < height; y++)
                {
                    Marshal.Copy(pixels, y * width, IntPtr.Add(screen.GetPixels(), y * screen.RowBytes), width);
                }

                using (var stream = File.Create(file))
                {
                    screen.Encode(stream, SKEncodedImageFormat.Png, 100);
                }
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

        public override bool SupportsYuvVideo
        {
            get { return _YuvPixelShader != null; }
        }

        protected override void _WriteYuvDataToTexture(CD3DTexture texture, byte[] data, float[] yuvToRgbRows)
        {
            if (!texture.IsLoaded)
            {
                return;
            }

            var width = texture.DataSize.Width;
            var height = texture.DataSize.Height;
            var planeWidths = new[] { width, (width + 1) / 2, (width + 1) / 2 };
            var planeHeights = new[] { height, (height + 1) / 2, (height + 1) / 2 };
            if (texture.Planes == null)
            {
                texture.Planes = new ID3D11Texture2D[3];
                texture.PlaneViews = new ID3D11ShaderResourceView[3];
                for (var i = 0; i < 3; i++)
                {
                    texture.Planes[i] = _Device.CreateTexture2D(new Texture2DDescription(Format.R8_UNorm, (uint)planeWidths[i], (uint)planeHeights[i], 1, 1,
                        BindFlags.ShaderResource, ResourceUsage.Default, CpuAccessFlags.None, 1, 0, ResourceOptionFlags.None));
                    texture.PlaneViews[i] = _Device.CreateShaderResourceView(texture.Planes[i]);
                }

                texture.TargetView = _Device.CreateRenderTargetView(texture.D3DTexture,
                    new RenderTargetViewDescription(RenderTargetViewDimension.Texture2D, Format.B8G8R8A8_UNorm, 0));
            }

            fixed (byte* source = data)
            {
                var offset = 0;
                for (var i = 0; i < 3; i++)
                {
                    _Context.UpdateSubresource(texture.Planes[i], 0, null, (IntPtr)(source + offset), (uint)planeWidths[i], 0);
                    offset += planeWidths[i] * planeHeights[i];
                }
            }

            var constants = new float[16];
            Array.Copy(yuvToRgbRows, constants, 12);
            constants[12] = width;
            constants[13] = height;
            _Context.UpdateSubresource(constants, _YuvConstantBuffer);

            _Context.OMSetRenderTargets(texture.TargetView, null);
            _Context.RSSetViewport(new Viewport(0, 0, width, height, 0f, 1f));
            _Context.IASetInputLayout(null);
            _Context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            _Context.VSSetShader(_YuvVertexShader);
            _Context.PSSetShader(_YuvPixelShader);
            _Context.PSSetConstantBuffer(0, _YuvConstantBuffer);
            _Context.PSSetShaderResources(0, texture.PlaneViews);
            _Context.PSSetSampler(0, _SamplerState);
            _Context.OMSetBlendState(null);
            _Context.OMSetDepthStencilState(null, 0);
            _Context.Draw(3, 0);
            _Context.PSSetShaderResources(0, new ID3D11ShaderResourceView[3]);
            if (texture.HasMips)
            {
                _Context.GenerateMips(texture.View);
            }

            _ApplyPipeline();
        }

        protected override void _WriteDataToTexture(CD3DTexture texture, byte[] data)
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                _WriteDataToTexture(texture, handle.AddrOfPinnedObject(), data.Length);
            }
            finally
            {
                handle.Free();
            }
        }

        protected override void _WriteDataToTexture(CD3DTexture texture, IntPtr data)
        {
            _WriteDataToTexture(texture, data, 4 * texture.DataSize.Width * texture.DataSize.Height);
        }

        private void _WriteDataToTexture(CD3DTexture texture, IntPtr data, int length)
        {
            if (!texture.IsLoaded)
            {
                return;
            }

            var rowWidth = 4 * texture.DataSize.Width;
            var width = Math.Min(texture.DataSize.Width, texture.Size.Width);
            var height = Math.Min(length / rowWidth, texture.Size.Height);
            if (width <= 0 || height <= 0)
            {
                return;
            }

            _Context.UpdateSubresource(texture.D3DTexture, 0, new Box(0, 0, 0, width, height, 1), data, (uint)rowWidth, 0);
            if (texture.HasMips)
            {
                _Context.GenerateMips(texture.View);
            }
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

        [StructLayout(LayoutKind.Sequential)]
        private struct STexturedColoredVertex
        {
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
        }
    }
}
