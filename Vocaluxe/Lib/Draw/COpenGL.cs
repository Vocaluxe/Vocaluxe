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

using System.Diagnostics;
using System.Drawing.Imaging;
using OpenTK;
using OpenTK.Graphics;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using OpenTK.Graphics.OpenGL;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;
using BeginMode = OpenTK.Graphics.OpenGL.BeginMode;
using BlendingFactorDest = OpenTK.Graphics.OpenGL.BlendingFactorDest;
using BlendingFactorSrc = OpenTK.Graphics.OpenGL.BlendingFactorSrc;
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
    class CFormHook : Form, IFormHook
    {
        public MessageEventHandler OnMessage { private get; set; }

        protected override void WndProc(ref Message m)
        {
            if (OnMessage == null || OnMessage(ref m))
                base.WndProc(ref m);
        }
    }

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

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)OpenTK.Graphics.TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)OpenTK.Graphics.TextureWrapMode.ClampToEdge);
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

    class COpenGL : CDrawBaseWindows<COGLTexture>, IDraw
    {
        private readonly GLControl _Control;
        private int _FBO;

        public COpenGL()
        {
            _Form = new CFormHook {ClientSize = new Size(CConfig.Config.Graphics.ScreenW, CConfig.Config.Graphics.ScreenH)};
            //OpenGL needs that here but D3D needs it in the constructor, so do NOT unify!

            //Check AA Mode
            CConfig.Config.Graphics.AAMode = (EAntiAliasingModes)_CheckAntiAliasingMode((int)CConfig.Config.Graphics.AAMode);

            bool ok = false;
            try
            {
                #if WIN
                var gm = new GraphicsMode(32, 24, 0, (int)CConfig.Config.Graphics.AAMode);
                #else
                var gm = new GraphicsMode(24, 24, 0, (int)CConfig.Config.Graphics.AAMode);
                #endif
                _Control = new GLControl(gm, 2, 1, GraphicsContextFlags.Default);
                if (_Control.GraphicsMode != null)
                    ok = true;
            }
            catch (Exception)
            {
                ok = false;
            }

            if (!ok)
                _Control = new GLControl();

            _Control.MakeCurrent();
            _Control.VSync = CConfig.Config.Graphics.VSync == EOffOn.TR_CONFIG_ON;

            _Form.Controls.Add(_Control);
            _Control.ClientSize = _Form.ClientSize;

            _Control.KeyDown += _OnKeyDown;
            _Control.PreviewKeyDown += _OnPreviewKeyDown;
            _Control.KeyPress += _OnKeyPress;
            _Control.KeyUp += _OnKeyUp;

            _Control.MouseMove += _OnMouseMove;
            _Control.MouseWheel += _OnMouseWheel;
            _Control.MouseDown += _OnMouseDown;
            _Control.MouseUp += _OnMouseUp;
            _Control.MouseLeave += _OnMouseLeave;
            _Control.MouseEnter += _OnMouseEnter;

            _NonPowerOf2TextureSupported = false;
        }

        private static int _CheckAntiAliasingMode(int setValue)
        {
            int samples = 0;

            if (setValue > 32)
                setValue = 32;

            while (samples <= setValue)
            {
                GraphicsMode mode;
                try
                {
                    mode = new GraphicsMode(16, 0, 0, samples);
                }
                catch (Exception)
                {
                    break;
                }

                if (mode.Samples != samples)
                    break;
                if (samples == 0)
                    samples = 2;
                else
                    samples *= 2;
            }

            if (samples == 2)
                return 0;
            return samples / 2;
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

        protected override void _OnResize(object sender, EventArgs e)
        {
            _Control.ClientSize = _Form.ClientSize;
            base._OnResize(sender, e);
        }

        protected override void _DoResize()
        {
            _H = _Control.Height;
            _W = _Control.Width;
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

            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            GL.DepthRange(CSettings.ZFar, CSettings.ZNear);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(Color.Black);

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
            _Control.SwapBuffers();
            Application.DoEvents();
        }

        public int GetScreenWidth()
        {
            return _Control.Width;
        }

        public int GetScreenHeight()
        {
            return _Control.Height;
        }

        protected override void _ClearScreen()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        protected override void _AdjustNewBorders()
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(-CConfig.Config.Graphics.BorderLeft, CConfig.Config.Graphics.BorderRight + CSettings.RenderW, CConfig.Config.Graphics.BorderBottom + CSettings.RenderH,
                     -CConfig.Config.Graphics.BorderTop, CSettings.ZNear, CSettings.ZFar);
        }

        public void MakeScreenShot()
        {
            string file = CHelper.GetUniqueFileName(Path.Combine(CSettings.DataFolder, CSettings.FolderNameScreenshots), "Screenshot.bmp");

            int width = GetScreenWidth();
            int height = GetScreenHeight();

            using (var screen = new Bitmap(width, height))
            {
                BitmapData bmpData = screen.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.ReadPixels(0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);
                screen.UnlockBits(bmpData);

                screen.RotateFlip(RotateFlipType.RotateNoneFlipY);
                screen.Save(file, ImageFormat.Bmp);
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

        public void DrawRect(SColorF color, SRectF rect)
        {
            GL.Enable(EnableCap.Blend);
            GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);

            GL.Begin(PrimitiveType.Quads);
            GL.MatrixMode(MatrixMode.Color);
            GL.PushMatrix();
            if (Math.Abs(rect.Rotation) > 0.001)
            {
                GL.Translate(0.5f, 0.5f, 0);
                GL.Rotate(-rect.Rotation, 0f, 0f, 1f);
                GL.Translate(-0.5f, -0.5f, 0);
            }
            GL.Vertex3(rect.X, rect.Y, rect.Z + CGraphics.ZOffset);
            GL.Vertex3(rect.X, rect.Y + rect.H, rect.Z + CGraphics.ZOffset);
            GL.Vertex3(rect.X + rect.W, rect.Y + rect.H, rect.Z + CGraphics.ZOffset);
            GL.Vertex3(rect.X + rect.W, rect.Y, rect.Z + CGraphics.ZOffset);
            GL.End();
            GL.PopMatrix();
            GL.Disable(EnableCap.Blend);
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
            GL.BindFramebuffer(OpenTK.Graphics.OpenGL.FramebufferTarget.Framebuffer, _FBO);
            GL.FramebufferTexture2D(OpenTK.Graphics.OpenGL.FramebufferTarget.Framebuffer, OpenTK.Graphics.OpenGL.FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D,
                                    texture.Name, 0);
            GL.ClearColor(Color.FromArgb(0));
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.ClearColor(Color.Black);
            GL.BindFramebuffer(OpenTK.Graphics.OpenGL.FramebufferTarget.Framebuffer, 0);
        }

        protected override void _WriteDataToTexture(COGLTexture texture, byte[] data)
        {
            Debug.Assert(texture.Name > 0);
            _ClearTexture(texture);
            GL.BindTexture(TextureTarget.Texture2D, texture.Name);

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, texture.DataSize.Width, texture.DataSize.Height, PixelFormat.Bgra, PixelType.UnsignedByte, data);
            GL.Ext.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        protected override void _WriteDataToTexture(COGLTexture texture, IntPtr data)
        {
            Debug.Assert(texture.Name > 0);
            _ClearTexture(texture);
            GL.BindTexture(TextureTarget.Texture2D, texture.Name);

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, texture.DataSize.Width, texture.DataSize.Height, PixelFormat.Bgra, PixelType.UnsignedByte, data);
            GL.Ext.GenerateMipmap(GenerateMipmapTarget.Texture2D);

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