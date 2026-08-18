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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VocaluxeLib.Log;
using SkiaSharp;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;
using ClearBufferMask = OpenTK.Graphics.OpenGL.ClearBufferMask;
using DepthFunction = OpenTK.Graphics.OpenGL.DepthFunction;
using EnableCap = OpenTK.Graphics.OpenGL.EnableCap;
using GL = OpenTK.Graphics.OpenGL.GL;
using GenerateMipmapTarget = OpenTK.Graphics.OpenGL.GenerateMipmapTarget;
using MatrixMode = OpenTK.Graphics.OpenGL.MatrixMode;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
using PixelInternalFormat = OpenTK.Graphics.OpenGL.PixelInternalFormat;
using PixelStoreParameter = OpenTK.Graphics.OpenGL.PixelStoreParameter;
using PixelType = OpenTK.Graphics.OpenGL.PixelType;
using TextureMagFilter = OpenTK.Graphics.OpenGL.TextureMagFilter;
using TextureMinFilter = OpenTK.Graphics.OpenGL.TextureMinFilter;
using TextureParameterName = OpenTK.Graphics.OpenGL.TextureParameterName;
using TextureTarget = OpenTK.Graphics.OpenGL.TextureTarget;

namespace Vocaluxe.Lib.Draw
{
    class COGLTexture : CTextureBase
    {
        //The texture "name" according to the specs
        public readonly int Name;

        public COGLTexture(int name, Size dataSize, int texWidth = 0, int texHeight = 0) : base(dataSize, new Size(texWidth, texHeight))
        {
            Name = name;
            if (name == 0)
                return;
            GL.BindTexture(TextureTarget.Texture2D, Name);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, texWidth, texHeight, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

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
            base.Dispose();
            if (Name != 0)
                GL.DeleteTexture(Name);
        }
    }

    class COpenGL : CDrawBaseGlfw<COGLTexture>, IDraw
    {
        private int _FBO;

        public COpenGL()
        {
            int w = CConfig.Config.Graphics.ScreenW * CConfig.Config.Graphics.NumScreens;
            int h = CConfig.Config.Graphics.ScreenH;

            var settings = new NativeWindowSettings
            {
                ClientSize = new Vector2i(w, h),
                Title = CSettings.GetFullVersionText(),
                // GL 2.1 with ContextProfile.Any -> a legacy/compatibility context that keeps the
                // fixed-function pipeline (immediate mode) the renderer relies on. GLFW only allows an
                // explicit Core/Compatibility profile for GL >= 3.2, so we must not set one here.
                Profile = ContextProfile.Any,
                APIVersion = new Version(2, 1),
                Flags = ContextFlags.Default,
                NumberOfSamples = (int)CConfig.Config.Graphics.AAMode,
                StartVisible = false,
                Vsync = CConfig.Config.Graphics.VSync == EOffOn.TR_CONFIG_ON ? VSyncMode.On : VSyncMode.Off
            };

            // OpenTK's default GLFW error handler turns EVERY GLFW error into an exception -
            // including harmless "feature not available on this platform" reports. On Wayland,
            // querying the window position is such a case (the protocol deliberately does not
            // expose it to clients), which killed startup during "Init Draw". Log instead of throw.
            GLFWProvider.SetErrorCallback((errorCode, description) =>
                CLog.Error("GLFW: {ErrorCode} - {Description}",
                    CLog.Params(errorCode, description)));

            _Window = new NativeWindow(settings);
            _Window.MakeCurrent();

            _NonPowerOf2TextureSupported = false;
        }

        /*
        private int _CheckColorDepth(int setValue)
        {
            int result = 16;

            if (setValue > 32)
                setValue = 32;

            while (result <= setValue)
            {
                GraphicsMode mode;
                try
                {
                    mode = new GraphicsMode(result, 0, 0, 0);
                }
                catch (Exception)
                {
                    break;
                }
                if (mode.ColorFormat != result)
                    break;
                result += 8;
            }

            return result - 8;
        }
*/

        protected override void _DoResize()
        {
            _H = _Window.ClientSize.Y;
            _W = _Window.ClientSize.X;
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
                return false;

            // Init Texturing
            GL.Enable(EnableCap.Texture2D);

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            GL.DepthRange(CSettings.ZFar, CSettings.ZNear);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(0f, 0f, 0f, 1f);

            GL.GenFramebuffers(1, out _FBO);

            return true;
        }

        public override void Close()
        {
            base.Close();
            GL.DeleteFramebuffers(1, ref _FBO);
        }

        protected override void _OnBeforeDraw()
        {
            //Nothing to do
        }

        protected override void _OnAfterDraw()
        {
            _Window.Context.SwapBuffers();
            NativeWindow.ProcessWindowEvents(false);
            // Reliably end the main loop when the user closes the window. The Closing event alone
            // is not dependable with a manually-driven OpenTK 4 NativeWindow, so also poll the GLFW
            // "should close" flag every frame. Once _Run is false, CDraw.MainLoop returns and
            // Program._CloseProgram shuts everything down (Environment.Exit).
            if (_Window.IsExiting)
                _Run = false;
        }

        public int GetScreenWidth()
        {
            return _Window.ClientSize.X;
        }

        public int GetScreenHeight()
        {
            return _Window.ClientSize.Y;
        }

        protected override void _ClearScreen()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        protected override void _AdjustNewBorders()
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(-CConfig.Config.Graphics.BorderLeft, CConfig.Config.Graphics.BorderRight + CSettings.RenderW * CConfig.Config.Graphics.NumScreens, CConfig.Config.Graphics.BorderBottom + CSettings.RenderH,
                     -CConfig.Config.Graphics.BorderTop, CSettings.ZNear, CSettings.ZFar);
        }

        public void MakeScreenShot()
        {
            string file = CHelper.GetUniqueFileName(Path.Combine(CSettings.DataFolder, CSettings.FolderNameScreenshots), "Screenshot.png");

            int width = GetScreenWidth();
            int height = GetScreenHeight();

            byte[] data = new byte[width * height * 4];
            GL.ReadPixels(0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte, data);

            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using (var bmp = new SKBitmap(info))
            {
                // OpenGL's origin is bottom-left, so flip the rows while copying into the bitmap.
                int stride = width * 4;
                IntPtr basePtr = bmp.GetPixels();
                for (int y = 0; y < height; y++)
                    Marshal.Copy(data, (height - 1 - y) * stride, IntPtr.Add(basePtr, y * stride), stride);

                using (SKImage image = SKImage.FromBitmap(bmp))
                using (SKData enc = image.Encode(SKEncodedImageFormat.Png, 100))
                using (FileStream fs = File.OpenWrite(file))
                    enc.SaveTo(fs);
            }
        }

        public CTextureRef CopyScreen()
        {
            //TODO: Check if _W,_H needs to be used or not
            Size size = new Size(GetScreenWidth(), GetScreenHeight());
            COGLTexture texture = _CreateTexture(size);

            GL.BindTexture(TextureTarget.Texture2D, texture.Name);
            GL.CopyTexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 0, 0, size.Width, size.Height); //TODO: Use _X,_Y and _W,_H?
            GL.BindTexture(TextureTarget.Texture2D, 0);

            return _GetTextureReference(size, texture);
        }

        public void CopyScreen(ref CTextureRef textureRef)
        {
            COGLTexture texture;
            //Check for actual texture sizes as it may be downsized compared to OrigSize
            if (!_GetTexture(textureRef, out texture) || texture.DataSize.Width != GetScreenWidth() || texture.DataSize.Height != GetScreenHeight())
            {
                RemoveTexture(ref textureRef);
                textureRef = CopyScreen();
            }
            else
            {
                GL.BindTexture(TextureTarget.Texture2D, texture.Name);
                GL.CopyTexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 0, 0, GetScreenWidth(), GetScreenHeight());
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
        }

        public void DrawRect(SColorF color, SRectF rect, bool allMonitors = true)
        {
            int loops = 1;
            if (allMonitors)
                loops = CConfig.Config.Graphics.NumScreens;
            for (int i = 0; i < loops; i++)
            {
                SRectF newrect = rect;
                newrect.X += CSettings.RenderW * i;

                GL.Enable(EnableCap.Blend);
                GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);

                GL.Begin(PrimitiveType.Quads);
                GL.MatrixMode(MatrixMode.Color);
                GL.PushMatrix();
                if (Math.Abs(newrect.Rotation) > 0.001)
                {
                    GL.Translate(0.5f, 0.5f, 0);
                    GL.Rotate(-newrect.Rotation, 0f, 0f, 1f);
                    GL.Translate(-0.5f, -0.5f, 0);
                }
                GL.Vertex3(newrect.X, newrect.Y, newrect.Z + CGraphics.ZOffset);
                GL.Vertex3(newrect.X, newrect.Y + newrect.H, newrect.Z + CGraphics.ZOffset);
                GL.Vertex3(newrect.X + newrect.W, newrect.Y + newrect.H, newrect.Z + CGraphics.ZOffset);
                GL.Vertex3(newrect.X + newrect.W, newrect.Y, newrect.Z + CGraphics.ZOffset);
                GL.End();
                GL.PopMatrix();
                GL.Disable(EnableCap.Blend);
            }
        }

        public void DrawRectReflection(SColorF color, SRectF rect, float space, float height)
        {
            if (rect.H < height)
                height = rect.H;

            float rx1 = rect.X;
            float rx2 = rect.X + rect.W;
            float ry1 = rect.Y + rect.H + space;
            float ry2 = rect.Y + rect.H + space + height;

            if (rx1 < rect.X)
                rx1 = rect.X;

            if (rx2 > rect.X + rect.W)
                rx2 = rect.X + rect.W;

            if (ry1 < rect.Y + space)
                ry1 = rect.Y + space;

            if (ry2 > rect.Y + rect.H + space + height)
                ry2 = rect.Y + rect.H + space + height;


            GL.Enable(EnableCap.Blend);
            GL.MatrixMode(MatrixMode.Color);
            GL.PushMatrix();
            if (Math.Abs(rect.Rotation) > 0.001)
            {
                GL.Translate(0.5f, 0.5f, 0);
                GL.Rotate(-rect.Rotation, 0f, 0f, 1f);
                GL.Translate(-0.5f, -0.5f, 0);
            }

            GL.Begin(PrimitiveType.Quads);

            GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);
            GL.Vertex3(rx2, ry1, rect.Z + CGraphics.ZOffset);

            GL.Color4(color.R, color.G, color.B, 0f);
            GL.Vertex3(rx2, ry2, rect.Z + CGraphics.ZOffset);
            GL.Vertex3(rx1, ry2, rect.Z + CGraphics.ZOffset);

            GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);
            GL.Vertex3(rx1, ry1, rect.Z + CGraphics.ZOffset);

            GL.End();
            GL.PopMatrix();
            GL.Disable(EnableCap.Blend);
        }

        protected override COGLTexture _CreateTexture(Size dataSize)
        {
            if (dataSize.Width < 0)
                return new COGLTexture(0, dataSize);
            COGLTexture texture = new COGLTexture(GL.GenTexture(), dataSize, _CheckForNextPowerOf2(dataSize.Width), _CheckForNextPowerOf2(dataSize.Height));

            return texture;
        }

        private void _ClearTexture(COGLTexture texture)
        {
            if (texture.DataSize.Equals(texture.Size))
                return;
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _FBO);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D,
                                    texture.Name, 0);
            GL.ClearColor(0f, 0f, 0f, 0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.ClearColor(0f, 0f, 0f, 1f);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
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
            // Align textures to full pixels to reduce artefacts
            dc.Wx1 = (float)Math.Round(dc.Wx1);
            dc.Wy1 = (float)Math.Round(dc.Wy1);
            dc.Wx2 = (float)Math.Round(dc.Wx2);
            dc.Wy2 = (float)Math.Round(dc.Wy2);

            GL.BindTexture(TextureTarget.Texture2D, texture.Name);

            GL.Enable(EnableCap.Blend);

            GL.MatrixMode(MatrixMode.Texture);
            GL.PushMatrix();

            if (Math.Abs(dc.Rotation) > float.Epsilon)
            {
                GL.Translate(0.5f, 0.5f, 0);
                GL.Rotate(-dc.Rotation, 0f, 0f, 1f);
                GL.Translate(-0.5f, -0.5f, 0);
            }

            GL.Begin(PrimitiveType.Quads);

            GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);
            GL.TexCoord2(dc.Tx1, dc.Ty1);
            GL.Vertex3(dc.Wx1, dc.Wy1, dc.Wz);

            if (isReflection)
                GL.Color4(color.R, color.G, color.B, 0);
            GL.TexCoord2(dc.Tx1, dc.Ty2);
            GL.Vertex3(dc.Wx1, dc.Wy2, dc.Wz);

            GL.TexCoord2(dc.Tx2, dc.Ty2);
            GL.Vertex3(dc.Wx2, dc.Wy2, dc.Wz);

            if (isReflection)
                GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);
            GL.TexCoord2(dc.Tx2, dc.Ty1);
            GL.Vertex3(dc.Wx2, dc.Wy1, dc.Wz);

            GL.End();

            GL.PopMatrix();

            GL.Disable(EnableCap.Blend);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
    }
}