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

using System.Drawing.Imaging;
using OpenTK;
using OpenTK.Graphics;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
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

        public COGLTexture(int name, Size dataSize, int texWidth = 0, int texHeight = 0) : base(dataSize, texWidth, texHeight)
        {
            Name = name;
            if (name == 0)
                return;
            GL.BindTexture(TextureTarget.Texture2D, Name);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, W2, H2, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

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

    class COpenGL : CDrawBaseWindows<COGLTexture>, IDraw
    {
        #region private vars
        private readonly GLControl _Control;
        #endregion private vars

        public COpenGL()
        {
            _Form = new CFormHook();

            //Check AA Mode
            CConfig.AAMode = (EAntiAliasingModes)_CheckAntiAliasingMode((int)CConfig.AAMode);

            bool ok = false;
            try
            {
                var gm = new GraphicsMode(32, 24, 0, (int)CConfig.AAMode);
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
            _Control.VSync = CConfig.VSync == EOffOn.TR_CONFIG_ON;

            _Form.Controls.Add(_Control);

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

        #region Helpers
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
        #endregion Helpers

        #region form events
        protected override void _OnResize(object sender, EventArgs e)
        {
            _Control.ClientSize = _Form.ClientSize;
            base._OnResize(sender, e);
        }
        #endregion form events

        protected override void _DoResize()
        {
            _H = _Control.Height;
            _W = _Control.Width;
            _CurrentAlignment = CConfig.ScreenAlignment;

            _AdjustAspect(true);
            _AdjustNewBorders();
            GL.Viewport(_X, _Y, _W, _H);
        }

        #region implementation

        #region main stuff
        public override bool Init()
        {
            if (!base.Init())
                return false;

            //OpenGL needs that here but D3D needs it in the constructor, so do NOT unify!
            _Form.ClientSize = new Size(CConfig.ScreenW, CConfig.ScreenH);

            // Init Texturing
            GL.Enable(EnableCap.Texture2D);

            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            GL.DepthRange(CSettings.ZFar, CSettings.ZNear);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(Color.Black);

            return true;
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
            GL.Ortho(-CConfig.BorderLeft, CConfig.BorderRight + CSettings.RenderW, CConfig.BorderBottom + CSettings.RenderH, -CConfig.BorderTop, CSettings.ZNear, CSettings.ZFar);
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
        #endregion main stuff

        #region Basic Draw Methods
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

            GL.Begin(BeginMode.Quads);
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
            GL.MatrixMode(MatrixMode.Texture);
            GL.PushMatrix();
            if (Math.Abs(rect.Rotation) > 0.001)
            {
                GL.Translate(0.5f, 0.5f, 0);
                GL.Rotate(-rect.Rotation, 0f, 0f, 1f);
                GL.Translate(-0.5f, -0.5f, 0);
            }

            GL.Begin(BeginMode.Quads);

            GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);
            GL.Vertex3(rx2, ry1, rect.Z + CGraphics.ZOffset);

            GL.Color4(color.R, color.G, color.B, 0f);
            GL.Vertex3(rx2, ry2, rect.Z + CGraphics.ZOffset);

            GL.Color4(color.R, color.G, color.B, 0f);
            GL.Vertex3(rx1, ry2, rect.Z + CGraphics.ZOffset);

            GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);
            GL.Vertex3(rx1, ry1, rect.Z + CGraphics.ZOffset);

            GL.End();
            GL.PopMatrix();
            GL.Disable(EnableCap.Blend);
        }
        #endregion Basic Draw Methods

        #region Textures
        protected override COGLTexture _CreateTexture(Size dataSize)
        {
            if (dataSize.Width < 0)
                return new COGLTexture(0, dataSize);
            COGLTexture texture = new COGLTexture(GL.GenTexture(), dataSize, _CheckForNextPowerOf2(dataSize.Width), _CheckForNextPowerOf2(dataSize.Height));

            return texture;
        }

        protected override void _WriteDataToTexture(COGLTexture texture, byte[] data)
        {
            GL.BindTexture(TextureTarget.Texture2D, texture.Name);

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, texture.DataSize.Width, texture.DataSize.Height, PixelFormat.Bgra, PixelType.UnsignedByte, data);
            GL.Ext.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        protected override void _WriteDataToTexture(COGLTexture texture, IntPtr data)
        {
            GL.BindTexture(TextureTarget.Texture2D, texture.Name);

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, texture.DataSize.Width, texture.DataSize.Height, PixelFormat.Bgra, PixelType.UnsignedByte, data);
            GL.Ext.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        #region drawing
        public void DrawTexture(CTextureRef textureRef, SRectF rect, SColorF color, bool mirrored = false)
        {
            COGLTexture texture;
            if (!_GetTexture(textureRef, out texture))
                return;

            if (Math.Abs(rect.W) < 1 || Math.Abs(rect.H) < 1 || Math.Abs(color.A) < 0.01)
                return;

            GL.BindTexture(TextureTarget.Texture2D, texture.Name);

            const float x1 = 0;
            float x2 = texture.WidthRatio;
            float y1 = 0;
            float y2 = texture.HeightRatio;

            float rx1 = rect.X;
            float rx2 = rect.X + rect.W;
            float ry1 = rect.Y;
            float ry2 = rect.Y + rect.H;

            GL.Enable(EnableCap.Blend);
            GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);

            GL.MatrixMode(MatrixMode.Texture);
            GL.PushMatrix();

            if (Math.Abs(rect.Rotation) > float.Epsilon)
            {
                GL.Translate(0.5f, 0.5f, 0);
                GL.Rotate(-rect.Rotation, 0f, 0f, 1f);
                GL.Translate(-0.5f, -0.5f, 0);
            }

            if (mirrored)
            {
                float tmp = y2;
                y2 = y1;
                y1 = tmp;
            }

            GL.Begin(BeginMode.Quads);

            GL.TexCoord2(x1, y1);
            GL.Vertex3(rx1, ry1, rect.Z + CGraphics.ZOffset);

            GL.TexCoord2(x1, y2);
            GL.Vertex3(rx1, ry2, rect.Z + CGraphics.ZOffset);

            GL.TexCoord2(x2, y2);
            GL.Vertex3(rx2, ry2, rect.Z + CGraphics.ZOffset);

            GL.TexCoord2(x2, y1);
            GL.Vertex3(rx2, ry1, rect.Z + CGraphics.ZOffset);

            GL.End();

            GL.PopMatrix();

            GL.Disable(EnableCap.Blend);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void DrawTexture(CTextureRef textureRef, SRectF rect, SColorF color, SRectF bounds, bool mirrored = false)
        {
            COGLTexture texture;
            if (!_GetTexture(textureRef, out texture))
                return;

            if (Math.Abs(rect.W) < 1 || Math.Abs(rect.H) < 1 || Math.Abs(bounds.H) < 1 || Math.Abs(bounds.W) < 1 ||
                Math.Abs(color.A) < 0.01)
                return;

            if (bounds.X > rect.X + rect.W || bounds.X + bounds.W < rect.X)
                return;

            if (bounds.Y > rect.Y + rect.H || bounds.Y + bounds.H < rect.Y)
                return;

            GL.BindTexture(TextureTarget.Texture2D, texture.Name);

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

            GL.Enable(EnableCap.Blend);
            GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);

            GL.MatrixMode(MatrixMode.Texture);
            GL.PushMatrix();

            if (Math.Abs(rect.Rotation) > float.Epsilon)
            {
                GL.Translate(0.5f, 0.5f, 0);
                GL.Rotate(-rect.Rotation, 0f, 0f, 1f);
                GL.Translate(-0.5f, -0.5f, 0);
            }

            if (mirrored)
            {
                float tmp = y2;
                y2 = y1;
                y1 = tmp;
            }

            GL.Begin(BeginMode.Quads);

            GL.TexCoord2(x1, y1);
            GL.Vertex3(rx1, ry1, rect.Z + CGraphics.ZOffset);

            GL.TexCoord2(x1, y2);
            GL.Vertex3(rx1, ry2, rect.Z + CGraphics.ZOffset);

            GL.TexCoord2(x2, y2);
            GL.Vertex3(rx2, ry2, rect.Z + CGraphics.ZOffset);

            GL.TexCoord2(x2, y1);
            GL.Vertex3(rx2, ry1, rect.Z + CGraphics.ZOffset);

            GL.End();

            GL.PopMatrix();

            GL.Disable(EnableCap.Blend);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void DrawTexture(CTextureRef textureRef, SRectF rect, SColorF color, float begin, float end)
        {
            COGLTexture texture;
            if (!_GetTexture(textureRef, out texture))
                return;

            GL.BindTexture(TextureTarget.Texture2D, texture.Name);

            GL.Enable(EnableCap.Blend);
            GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);


            GL.Begin(BeginMode.Quads);

            GL.TexCoord2(0f + begin * texture.WidthRatio, 0f);
            GL.Vertex3(rect.X + begin * rect.W, rect.Y, rect.Z + CGraphics.ZOffset);

            GL.TexCoord2(0f + begin * texture.WidthRatio, texture.HeightRatio);
            GL.Vertex3(rect.X + begin * rect.W, rect.Y + rect.H, rect.Z + CGraphics.ZOffset);

            GL.TexCoord2(texture.WidthRatio * end, texture.HeightRatio);
            GL.Vertex3(rect.X + end * rect.W, rect.Y + rect.H, rect.Z + CGraphics.ZOffset);

            GL.TexCoord2(texture.WidthRatio * end, 0f);
            GL.Vertex3(rect.X + end * rect.W, rect.Y, rect.Z + CGraphics.ZOffset);

            GL.End();

            GL.Disable(EnableCap.Blend);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void DrawTextureReflection(CTextureRef textureRef, SRectF rect, SColorF color, float space, float height)
        {
            COGLTexture texture;
            if (!_GetTexture(textureRef, out texture))
                return;

            if (Math.Abs(rect.W) < float.Epsilon || Math.Abs(rect.H) < float.Epsilon || Math.Abs(color.A) < float.Epsilon || height <= float.Epsilon)
                return;

            if (height > rect.H)
                height = rect.H;

            GL.BindTexture(TextureTarget.Texture2D, texture.Name);

            const float x1 = 0;
            float x2 = texture.WidthRatio;
            float y1 = (rect.H - height) / rect.H * texture.HeightRatio;
            float y2 = texture.HeightRatio;


            float rx1 = rect.X;
            float rx2 = rect.X + rect.W;
            float ry1 = rect.Y + rect.H + space;
            float ry2 = rect.Y + rect.H + space + height;

            GL.Enable(EnableCap.Blend);

            GL.MatrixMode(MatrixMode.Texture);
            GL.PushMatrix();

            if (Math.Abs(rect.Rotation) > float.Epsilon)
            {
                GL.Translate(0.5f, 0.5f, 0);
                GL.Rotate(-rect.Rotation, 0f, 0f, 1f);
                GL.Translate(-0.5f, -0.5f, 0);
            }

            GL.Begin(BeginMode.Quads);

            GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);
            GL.TexCoord2(x2, y2);
            GL.Vertex3(rx2, ry1, rect.Z + CGraphics.ZOffset);

            GL.Color4(color.R, color.G, color.B, 0f);
            GL.TexCoord2(x2, y1);
            GL.Vertex3(rx2, ry2, rect.Z + CGraphics.ZOffset);

            GL.Color4(color.R, color.G, color.B, 0f);
            GL.TexCoord2(x1, y1);
            GL.Vertex3(rx1, ry2, rect.Z + CGraphics.ZOffset);

            GL.Color4(color.R, color.G, color.B, color.A * CGraphics.GlobalAlpha);
            GL.TexCoord2(x1, y2);
            GL.Vertex3(rx1, ry1, rect.Z + CGraphics.ZOffset);

            GL.End();

            GL.PopMatrix();

            GL.Disable(EnableCap.Blend);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
        #endregion drawing

        #endregion Textures

        #endregion implementation
    }
}