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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using SDL;
using SkiaSharp;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Log;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;
using Size = System.Drawing.Size;

namespace Vocaluxe.Lib.Draw
{
    class CSdlBindingsContext : IBindingsContext
    {
        public IntPtr GetProcAddress(string procName)
        {
            return SDL3.SDL_GL_GetProcAddress(procName);
        }
    }

    class COGLTexture : CTextureBase
    {
        //The texture "name" according to the specs
        public readonly int Name;
        public int[] Planes;

        public COGLTexture(int name, Size dataSize, int texWidth = 0, int texHeight = 0) : base(dataSize, new Size(texWidth, texHeight))
        {
            Name = name;
            if (name == 0)
            {
                return;
            }

            GL.BindTexture(TextureTarget.Texture2D, Name);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, texWidth, texHeight, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public override bool IsLoaded
        {
            get { return Name != 0; }
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            base.Dispose();
            if (Planes != null)
            {
                GL.DeleteTextures(Planes.Length, Planes);
            }

            if (Name != 0)
            {
                GL.DeleteTexture(Name);
            }
        }
    }

    unsafe class COpenGL : CDrawBaseSDL<COGLTexture>, IDraw
    {
        private const string _VertexShaderSource = @"#version 330 core
uniform mat4 Projection;
layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 TexCoord;
layout(location = 2) in vec4 Color;
out vec2 vTexCoord;
out vec4 vColor;
void main()
{
    gl_Position = Projection * vec4(Position, 1.0);
    vTexCoord = TexCoord;
    vColor = Color.bgra;
}";
        private const string _YuvVertexShaderSource = @"#version 330 core
void main()
{
    vec2 position = vec2((gl_VertexID << 1) & 2, gl_VertexID & 2);
    gl_Position = vec4(position * 2.0 - 1.0, 0.0, 1.0);
}";
        private const string _YuvFragmentShaderSource = @"#version 330 core
uniform sampler2D TexY;
uniform sampler2D TexU;
uniform sampler2D TexV;
uniform vec4 RowR;
uniform vec4 RowG;
uniform vec4 RowB;
uniform vec2 Size;
out vec4 FragColor;
void main()
{
    vec2 uv = gl_FragCoord.xy / Size;
    vec4 yuv = vec4(texture(TexY, uv).r, texture(TexU, uv).r, texture(TexV, uv).r, 1.0);
    FragColor = vec4(clamp(dot(RowR, yuv), 0.0, 1.0), clamp(dot(RowG, yuv), 0.0, 1.0), clamp(dot(RowB, yuv), 0.0, 1.0), 1.0);
}";
        private const string _FragmentShaderSource = @"#version 330 core
uniform sampler2D Tex;
in vec2 vTexCoord;
in vec4 vColor;
out vec4 FragColor;
void main()
{
    FragColor = texture(Tex, vTexCoord) * vColor;
}";

        private readonly SDL_GLContextState* _Context;
        private int _FBO;
        private int _ResolveFBO;
        private int _ResolveTexture;
        private Size _ResolveSize;
        private int _Program;
        private int _YuvProgram;
        private int _ProjectionLocation;
        // projection: virtual coordinates (1920x1080 per screen) -> pixels of the viewport, for the pixel alignment of textures
        private float _ProjectionLeft, _ProjectionTop;
        private float _PixelsPerUnitX = 1f, _PixelsPerUnitY = 1f;
        private int _VertexArray;
        private int _VertexBuffer;
        private int _IndexBuffer;
        private CTextureRef _BlankTexture;

        private readonly List<STexturedColoredVertex> _Vertices = new List<STexturedColoredVertex>();
        private readonly List<int> _VerticesTextures = new List<int>();

        public COpenGL()
        {
            //Check AA Mode
            var samples = (int)CConfig.Config.Graphics.AAMode;
            if (samples > 16)
            {
                samples = 16;
            }

            _SetAttributes(samples);
            if (!_CreateWindow(SDL_WindowFlags.SDL_WINDOW_OPENGL) && samples > 0)
            {
                samples = 0;
                _SetAttributes(samples);
                _CreateWindow(SDL_WindowFlags.SDL_WINDOW_OPENGL);
            }

            if (_Window == null)
            {
                throw new Exception("Could not create an OpenGL window: " + SDL3.SDL_GetError());
            }

            CConfig.Config.Graphics.AAMode = (EAntiAliasingModes)samples;

            _Context = SDL3.SDL_GL_CreateContext(_Window);
            if (_Context == null)
            {
                throw new Exception("Could not create an OpenGL 3.3 context: " + SDL3.SDL_GetError());
            }

            SDL3.SDL_GL_MakeCurrent(_Window, _Context);
            GL.LoadBindings(new CSdlBindingsContext());
            SDL3.SDL_GL_SetSwapInterval(CConfig.Config.Graphics.VSync == EOffOn.TR_CONFIG_ON ? 1 : 0);

            _NonPowerOf2TextureSupported = true;
        }

        private static void _SetAttributes(int samples)
        {
            SDL3.SDL_GL_ResetAttributes();
            SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
            SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MINOR_VERSION, 3);
            SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_PROFILE_MASK, SDL3.SDL_GL_CONTEXT_PROFILE_CORE);
            SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_FLAGS, SDL3.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG);
            SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_DOUBLEBUFFER, 1);
            SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_DEPTH_SIZE, 24);
            SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_MULTISAMPLEBUFFERS, samples > 0 ? 1 : 0);
            SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_MULTISAMPLESAMPLES, samples);
        }

        protected override void _DoResize()
        {
            var clientSize = _GetClientSize();
            if (clientSize.Width == 0 || clientSize.Height == 0)
            {
                return;
            }

            _H = clientSize.Height;
            _W = clientSize.Width;
            _X = 0;
            _Y = 0;
            _CurrentAlignment = CConfig.Config.Graphics.ScreenAlignment;

            if (CConfig.Config.Graphics.Stretch != EOffOn.TR_CONFIG_ON)
            {
                _AdjustAspect(true);
            }

            _AdjustNewBorders();
            GL.Viewport(_X, _Y, _W, _H);
        }

        public override bool Init()
        {
            if (!base.Init())
            {
                return false;
            }

            try
            {
                _Program = _CreateProgram(_VertexShaderSource, _FragmentShaderSource);
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error during OpenGL initialization.");
                return false;
            }

            try
            {
                _YuvProgram = _CreateProgram(_YuvVertexShaderSource, _YuvFragmentShaderSource);
                GL.UseProgram(_YuvProgram);
                GL.Uniform1(GL.GetUniformLocation(_YuvProgram, "TexY"), 0);
                GL.Uniform1(GL.GetUniformLocation(_YuvProgram, "TexU"), 1);
                GL.Uniform1(GL.GetUniformLocation(_YuvProgram, "TexV"), 2);
            }
            catch (Exception e)
            {
                CLog.Error(e, "[OpenGL] Unable to compile the YUV shader, using CPU conversion for videos");
                _YuvProgram = 0;
            }

            _ProjectionLocation = GL.GetUniformLocation(_Program, "Projection");
            GL.UseProgram(_Program);
            GL.Uniform1(GL.GetUniformLocation(_Program, "Tex"), 0);

            _VertexArray = GL.GenVertexArray();
            GL.BindVertexArray(_VertexArray);

            var vertexSize = Marshal.SizeOf<STexturedColoredVertex>();
            _VertexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _VertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, CSettings.VertexBufferElements * 4 * vertexSize, IntPtr.Zero, BufferUsageHint.StreamDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, 0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, vertexSize, sizeof(float) * 3);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, vertexSize, sizeof(float) * 5);
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);

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

            _IndexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _IndexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(ushort), indices, BufferUsageHint.StaticDraw);

            GL.Enable(EnableCap.Blend);
            GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.Zero, BlendingFactorDest.One);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            GL.DepthFunc(DepthFunction.Lequal);
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(0f, 0f, 0f, 1f);

            _FBO = GL.GenFramebuffer();
            _ResolveFBO = GL.GenFramebuffer();

            _BlankTexture = AddTexture(1, 1, new byte[] { 255, 255, 255, 255 });

            return true;
        }

        private static int _CreateProgram(string vertexShaderSource, string fragmentShaderSource)
        {
            var vertexShader = _CompileShader(ShaderType.VertexShader, vertexShaderSource);
            var fragmentShader = _CompileShader(ShaderType.FragmentShader, fragmentShaderSource);
            var program = GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            int status;
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out status);
            if (status == 0)
            {
                throw new Exception("Shader linking failed: " + GL.GetProgramInfoLog(program));
            }

            return program;
        }

        private static int _CompileShader(ShaderType type, string source)
        {
            var shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            int status;
            GL.GetShader(shader, ShaderParameter.CompileStatus, out status);
            if (status == 0)
            {
                throw new Exception("Shader compilation failed: " + GL.GetShaderInfoLog(shader));
            }

            return shader;
        }

        public override void Close()
        {
            base.Close();
            if (_Context == null)
            {
                return;
            }

            GL.DeleteFramebuffer(_FBO);
            GL.DeleteFramebuffer(_ResolveFBO);
            if (_ResolveTexture != 0)
            {
                GL.DeleteTexture(_ResolveTexture);
            }

            GL.DeleteBuffer(_VertexBuffer);
            GL.DeleteBuffer(_IndexBuffer);
            GL.DeleteVertexArray(_VertexArray);
            GL.DeleteProgram(_Program);
            if (_YuvProgram != 0)
            {
                GL.DeleteProgram(_YuvProgram);
            }
            SDL3.SDL_GL_DestroyContext(_Context);
        }

        protected override void _OnBeforeDraw()
        {
            GL.UseProgram(_Program);
            GL.BindVertexArray(_VertexArray);
            GL.ActiveTexture(TextureUnit.Texture0);
        }

        protected override void _OnAfterDraw()
        {
            _RenderVertexBuffer();
            SDL3.SDL_GL_SwapWindow(_Window);
            _HandleEvents();
        }

        public int GetScreenWidth()
        {
            return _GetClientSize().Width;
        }

        public int GetScreenHeight()
        {
            return _GetClientSize().Height;
        }

        protected override void _ClearScreen()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        protected override void _AdjustNewBorders()
        {
            float left = -CConfig.Config.Graphics.BorderLeft;
            float right = CConfig.Config.Graphics.BorderRight + CSettings.RenderW * CConfig.Config.Graphics.NumScreens;
            float bottom = CConfig.Config.Graphics.BorderBottom + CSettings.RenderH;
            float top = -CConfig.Config.Graphics.BorderTop;
            float zNear = CSettings.ZNear;
            float zFar = CSettings.ZFar;
            _ProjectionLeft = left;
            _ProjectionTop = top;
            _PixelsPerUnitX = _W > 0 ? _W / (right - left) : 1f;
            _PixelsPerUnitY = _H > 0 ? _H / (bottom - top) : 1f;
            var projection = new Matrix4x4(
                2f / (right - left), 0f, 0f, 0f,
                0f, 2f / (top - bottom), 0f, 0f,
                0f, 0f, 2f / (zFar - zNear), 0f,
                -(right + left) / (right - left), -(top + bottom) / (top - bottom), -(zFar + zNear) / (zFar - zNear), 1f);

            GL.UseProgram(_Program);
            GL.UniformMatrix4(_ProjectionLocation, 1, false, &projection.M11);
        }

        public void MakeScreenShot()
        {
            _RenderVertexBuffer();
            var file = CHelper.GetUniqueFileName(Path.Combine(CSettings.DataFolder, CSettings.FolderNameScreenshots), "Screenshot.png");

            var size = _GetClientSize();
            using (var screen = new SKBitmap(new SKImageInfo(size.Width, size.Height, SKColorType.Bgra8888, SKAlphaType.Opaque)))
            {
                var pixels = new byte[size.Width * size.Height * 4];
                GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
                GL.ReadPixels(0, 0, size.Width, size.Height, PixelFormat.Bgra, PixelType.UnsignedByte, pixels);

                var rowLength = size.Width * 4;
                var target = screen.GetPixels();
                for (var y = 0; y < size.Height; y++)
                {
                    Marshal.Copy(pixels, (size.Height - 1 - y) * rowLength, IntPtr.Add(target, y * screen.RowBytes), rowLength);
                }

                using (var stream = File.Create(file))
                {
                    screen.Encode(stream, SKEncodedImageFormat.Png, 100);
                }
            }
        }

        private void _CopyFrameToTexture(COGLTexture texture)
        {
            _RenderVertexBuffer();

            if (_ResolveTexture == 0 || _ResolveSize.Width != _W || _ResolveSize.Height != _H)
            {
                if (_ResolveTexture != 0)
                {
                    GL.DeleteTexture(_ResolveTexture);
                }

                _ResolveSize = new Size(_W, _H);
                _ResolveTexture = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, _ResolveTexture);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, _W, _H, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, _ResolveFBO);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _ResolveTexture, 0);
            }

            var width = Math.Min(_W, texture.Size.Width);
            var height = Math.Min(_H, texture.Size.Height);

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _ResolveFBO);
            GL.BlitFramebuffer(_X, _Y, _X + _W, _Y + _H, 0, 0, _W, _H, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _FBO);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texture.Name, 0);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _ResolveFBO);
            GL.BlitFramebuffer(0, _H - height, width, _H, 0, height, width, 0, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            GL.BindTexture(TextureTarget.Texture2D, texture.Name);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public CTextureRef CopyScreen()
        {
            var size = new Size(_W, _H);
            var texture = _CreateTexture(size);
            _CopyFrameToTexture(texture);

            return _GetTextureReference(size, texture);
        }

        public void CopyScreen(ref CTextureRef textureRef)
        {
            COGLTexture texture;
            //Check for actual texture sizes as it may be downsized compared to OrigSize
            if (!_GetTexture(textureRef, out texture) || texture.DataSize.Width != _W || texture.DataSize.Height != _H)
            {
                RemoveTexture(ref textureRef);
                textureRef = CopyScreen();
            }
            else
            {
                _CopyFrameToTexture(texture);
            }
        }

        public void DrawRect(SColorF color, SRectF rect, bool allMonitors = true)
        {
            DrawTexture(_BlankTexture, rect, color, false, allMonitors);
        }

        public void DrawRectReflection(SColorF color, SRectF rect, float space, float height)
        {
            DrawTextureReflection(_BlankTexture, rect, color, rect, space, height);
        }

        protected override COGLTexture _CreateTexture(Size dataSize)
        {
            if (dataSize.Width < 0)
            {
                return new COGLTexture(0, dataSize);
            }

            var texture = new COGLTexture(GL.GenTexture(), dataSize, _CheckForNextPowerOf2(dataSize.Width), _CheckForNextPowerOf2(dataSize.Height));

            return texture;
        }

        private void _ClearTexture(COGLTexture texture)
        {
            if (texture.DataSize.Equals(texture.Size))
            {
                return;
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _FBO);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texture.Name, 0);
            GL.ClearColor(0f, 0f, 0f, 0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.ClearColor(0f, 0f, 0f, 1f);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public override bool SupportsYuvVideo
        {
            get { return _YuvProgram != 0; }
        }

        protected override void _WriteYuvDataToTexture(COGLTexture texture, byte[] data, float[] yuvToRgbRows)
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
                texture.Planes = new int[3];
                GL.GenTextures(3, texture.Planes);
                for (var i = 0; i < 3; i++)
                {
                    GL.BindTexture(TextureTarget.Texture2D, texture.Planes[i]);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, planeWidths[i], planeHeights[i], 0, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                }
            }

            fixed (byte* source = data)
            {
                var offset = 0;
                for (var i = 0; i < 3; i++)
                {
                    GL.BindTexture(TextureTarget.Texture2D, texture.Planes[i]);
                    GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, planeWidths[i], planeHeights[i], PixelFormat.Red, PixelType.UnsignedByte, (IntPtr)(source + offset));
                    offset += planeWidths[i] * planeHeights[i];
                }
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _FBO);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texture.Name, 0);
            GL.Viewport(0, 0, width, height);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.UseProgram(_YuvProgram);
            GL.Uniform4(GL.GetUniformLocation(_YuvProgram, "RowR"), yuvToRgbRows[0], yuvToRgbRows[1], yuvToRgbRows[2], yuvToRgbRows[3]);
            GL.Uniform4(GL.GetUniformLocation(_YuvProgram, "RowG"), yuvToRgbRows[4], yuvToRgbRows[5], yuvToRgbRows[6], yuvToRgbRows[7]);
            GL.Uniform4(GL.GetUniformLocation(_YuvProgram, "RowB"), yuvToRgbRows[8], yuvToRgbRows[9], yuvToRgbRows[10], yuvToRgbRows[11]);
            GL.Uniform2(GL.GetUniformLocation(_YuvProgram, "Size"), (float)width, height);
            for (var i = 0; i < 3; i++)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + i);
                GL.BindTexture(TextureTarget.Texture2D, texture.Planes[i]);
            }

            GL.BindVertexArray(_VertexArray);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            for (var i = 2; i >= 0; i--)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + i);
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(_X, _Y, _W, _H);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
            GL.UseProgram(_Program);

            GL.BindTexture(TextureTarget.Texture2D, texture.Name);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        protected override void _WriteDataToTexture(COGLTexture texture, byte[] data)
        {
            Debug.Assert(texture.Name > 0);
            _ClearTexture(texture);
            GL.BindTexture(TextureTarget.Texture2D, texture.Name);

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, texture.DataSize.Width, texture.DataSize.Height, PixelFormat.Bgra, PixelType.UnsignedByte, data);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        protected override void _WriteDataToTexture(COGLTexture texture, IntPtr data)
        {
            Debug.Assert(texture.Name > 0);
            _ClearTexture(texture);
            GL.BindTexture(TextureTarget.Texture2D, texture.Name);

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, texture.DataSize.Width, texture.DataSize.Height, PixelFormat.Bgra, PixelType.UnsignedByte, data);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        protected override void _DrawTexture(COGLTexture texture, SDrawCoords dc, SColorF color, bool isReflection = false)
        {
            // Align textures to full pixels to reduce artefacts. The pixels of the viewport are used, not the virtual units:
            // on high resolution screens (e.g. Retina) a unit is several pixels, so rounding to units made scaled texts
            // (e.g. the pulse of selected buttons) jitter. With one pixel per unit (1920x1080) nothing changes.
            dc.Wx1 = _AlignToPixel(dc.Wx1, _ProjectionLeft, _PixelsPerUnitX);
            dc.Wy1 = _AlignToPixel(dc.Wy1, _ProjectionTop, _PixelsPerUnitY);
            dc.Wx2 = _AlignToPixel(dc.Wx2, _ProjectionLeft, _PixelsPerUnitX);
            dc.Wy2 = _AlignToPixel(dc.Wy2, _ProjectionTop, _PixelsPerUnitY);

            color.A *= CGraphics.GlobalAlpha;
            var c = color.AsColor().ToArgb();
            var c2 = c;
            if (isReflection)
            {
                color.A = 0;
                c2 = color.AsColor().ToArgb();
            }

            var rotation = Matrix4x4.Identity;
            if (Math.Abs(dc.Rotation) > float.Epsilon)
            {
                var center = new Vector3((dc.Wx1 + dc.Wx2) / 2f, (dc.Wy1 + dc.Wy2) / 2f, 0);
                rotation = Matrix4x4.CreateTranslation(-center) * Matrix4x4.CreateRotationZ(dc.Rotation * (float)Math.PI / 180) * Matrix4x4.CreateTranslation(center);
            }

            if (_Vertices.Count >= CSettings.VertexBufferElements)
            {
                _RenderVertexBuffer();
            }

            _Vertices.Add(new STexturedColoredVertex(Vector3.Transform(new Vector3(dc.Wx1, dc.Wy1, dc.Wz), rotation), new Vector2(dc.Tx1, dc.Ty1), c));
            _Vertices.Add(new STexturedColoredVertex(Vector3.Transform(new Vector3(dc.Wx1, dc.Wy2, dc.Wz), rotation), new Vector2(dc.Tx1, dc.Ty2), c2));
            _Vertices.Add(new STexturedColoredVertex(Vector3.Transform(new Vector3(dc.Wx2, dc.Wy2, dc.Wz), rotation), new Vector2(dc.Tx2, dc.Ty2), c2));
            _Vertices.Add(new STexturedColoredVertex(Vector3.Transform(new Vector3(dc.Wx2, dc.Wy1, dc.Wz), rotation), new Vector2(dc.Tx2, dc.Ty1), c));
            _VerticesTextures.Add(texture.Name);
        }

        private static float _AlignToPixel(float value, float origin, float pixelsPerUnit)
        {
            return origin + (float)Math.Round((value - origin) * pixelsPerUnit) / pixelsPerUnit;
        }

        private void _RenderVertexBuffer()
        {
            if (_Vertices.Count <= 0)
            {
                return;
            }

            var vertexData = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(_Vertices));
            GL.BindBuffer(BufferTarget.ArrayBuffer, _VertexBuffer);
            fixed (byte* data = vertexData)
            {
                GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertexData.Length, (IntPtr)data);
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

                GL.BindTexture(TextureTarget.Texture2D, texture);
                GL.DrawElements(PrimitiveType.Triangles, (end - start) * 6, DrawElementsType.UnsignedShort, start * 6 * sizeof(ushort));
                start = end;
            }

            GL.BindTexture(TextureTarget.Texture2D, 0);
            _Vertices.Clear();
            _VerticesTextures.Clear();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct STexturedColoredVertex
        {
            private Vector3 _Position;
            private Vector2 _Texture;
            private readonly int _Color;

            public STexturedColoredVertex(Vector3 position, Vector2 texture, int color)
            {
                _Position = position;
                _Texture = texture;
                _Color = color;
            }
        }
    }
}
