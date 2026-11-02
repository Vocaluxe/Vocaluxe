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
using System.Drawing;
using System.IO;
using SDL;
using SkiaSharp;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Draw
{
    class CSoftwareTexture : CTextureBase
    {
        public readonly SKBitmap Bitmap;
        public readonly SKImage Image;

        public CSoftwareTexture(Size dataSize) : base(dataSize, dataSize)
        {
            if (dataSize.Width <= 0 || dataSize.Height <= 0)
            {
                return;
            }

            Bitmap = new SKBitmap(new SKImageInfo(dataSize.Width, dataSize.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul));
            Image = SKImage.FromPixels(Bitmap.PeekPixels());
        }

        public override bool IsLoaded
        {
            get { return Bitmap != null; }
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            base.Dispose();
            Image?.Dispose();
            Bitmap?.Dispose();
        }
    }

    unsafe class CDrawSoftware : CDrawBaseSDL<CSoftwareTexture>, IDraw
    {
        private readonly SKBitmap _Backbuffer;
        private readonly SKCanvas _Canvas;
        private readonly SKColor _ClearColor = new SKColor(0, 0, 139);
        private CTextureRef _BlankTexture;

        public CDrawSoftware()
        {
            _CreateWindow(0);
            _Backbuffer = new SKBitmap(CSettings.RenderW * CConfig.Config.Graphics.NumScreens, CSettings.RenderH, SKColorType.Bgra8888, SKAlphaType.Premul);
            _Canvas = new SKCanvas(_Backbuffer);
            _Canvas.Clear(_ClearColor);
            _NonPowerOf2TextureSupported = true;
        }

        public override bool Init()
        {
            if (!base.Init())
            {
                return false;
            }

            _BlankTexture = AddTexture(1, 1, new byte[] { 255, 255, 255, 255 });
            return true;
        }

        public override void Close()
        {
            base.Close();
            _Canvas.Dispose();
            _Backbuffer.Dispose();
        }

        protected override void _DoResize()
        {
            var clientSize = _GetClientSize();
            if (clientSize.Width == 0 || clientSize.Height == 0)
            {
                return;
            }

            _CurrentAlignment = CConfig.Config.Graphics.ScreenAlignment;
            _W = clientSize.Width;
            _H = clientSize.Height;
            _X = 0;
            _Y = 0;
            _AdjustAspect(false);
        }

        protected override void _AdjustNewBorders() { }

        protected override void _OnBeforeDraw() { }

        protected override void _OnAfterDraw()
        {
            var surface = SDL3.SDL_GetWindowSurface(_Window);
            if (surface != null)
            {
                var colorType = surface->format == SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888 || surface->format == SDL_PixelFormat.SDL_PIXELFORMAT_XBGR8888
                                    ? SKColorType.Rgba8888 : SKColorType.Bgra8888;
                var info = new SKImageInfo(surface->w, surface->h, colorType, SKAlphaType.Premul);
                using (var windowSurface = SKSurface.Create(info, surface->pixels, surface->pitch))
                using (var image = SKImage.FromBitmap(_Backbuffer))
                {
                    if (windowSurface != null)
                    {
                        windowSurface.Canvas.Clear(SKColors.Black);
                        windowSurface.Canvas.DrawImage(image, SKRect.Create(_X, _Y, _W, _H), new SKSamplingOptions(SKFilterMode.Linear));
                        windowSurface.Canvas.Flush();
                    }
                }

                SDL3.SDL_UpdateWindowSurface(_Window);
            }

            _HandleEvents();
        }

        protected override void _ClearScreen()
        {
            _Canvas.Clear(_ClearColor);
        }

        public int GetScreenWidth()
        {
            return _W;
        }

        public int GetScreenHeight()
        {
            return _H;
        }

        public CTextureRef CopyScreen()
        {
            var texture = _CreateTexture(new Size(_Backbuffer.Width, _Backbuffer.Height));
            _Backbuffer.CopyPixelsTo(texture.Bitmap);
            return _GetTextureReference(_Backbuffer.Width, _Backbuffer.Height, texture);
        }

        public void CopyScreen(ref CTextureRef texture)
        {
            CSoftwareTexture softwareTexture;
            if (!_GetTexture(texture, out softwareTexture) || softwareTexture.DataSize.Width != _Backbuffer.Width || softwareTexture.DataSize.Height != _Backbuffer.Height)
            {
                RemoveTexture(ref texture);
                texture = CopyScreen();
            }
            else
            {
                _Backbuffer.CopyPixelsTo(softwareTexture.Bitmap);
            }
        }

        public void MakeScreenShot()
        {
            var file = CHelper.GetUniqueFileName(Path.Combine(CSettings.DataFolder, CSettings.FolderNameScreenshots), "Screenshot.png");
            using (var stream = File.Create(file))
            {
                _Backbuffer.Encode(stream, SKEncodedImageFormat.Png, 100);
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

        protected override CSoftwareTexture _CreateTexture(Size dataSize)
        {
            return new CSoftwareTexture(dataSize);
        }

        protected override void _WriteDataToTexture(CSoftwareTexture texture, byte[] data)
        {
            if (!texture.IsLoaded)
            {
                return;
            }

            var length = Math.Min(data.Length, texture.Bitmap.ByteCount);
            System.Runtime.InteropServices.Marshal.Copy(data, 0, texture.Bitmap.GetPixels(), length);
            texture.Bitmap.NotifyPixelsChanged();
        }

        protected override void _DrawTexture(CSoftwareTexture texture, SDrawCoords dc, SColorF color, bool isReflection = false)
        {
            if (!texture.IsLoaded)
            {
                return;
            }

            var alpha = color.A * CGraphics.GlobalAlpha;
            var width = texture.Bitmap.Width;
            var height = texture.Bitmap.Height;
            var source = new SKRect(Math.Min(dc.Tx1, dc.Tx2) * width, Math.Min(dc.Ty1, dc.Ty2) * height, Math.Max(dc.Tx1, dc.Tx2) * width,
                Math.Max(dc.Ty1, dc.Ty2) * height);
            var destination = new SKRect(dc.Wx1, dc.Wy1, dc.Wx2, dc.Wy2);
            if (source.Width <= 0 || source.Height <= 0 || destination.Width <= 0 || destination.Height <= 0)
            {
                return;
            }

            var matrix = new[]
            {
                color.R, 0, 0, 0, 0,
                0, color.G, 0, 0, 0,
                0, 0, color.B, 0, 0,
                0, 0, 0, alpha, 0
            };

            _Canvas.Save();
            if (Math.Abs(dc.Rotation) > float.Epsilon)
            {
                _Canvas.RotateDegrees(dc.Rotation, destination.MidX, destination.MidY);
            }

            if (dc.Ty1 > dc.Ty2)
            {
                _Canvas.Scale(1, -1, destination.MidX, destination.MidY);
            }

            using (var filter = SKColorFilter.CreateColorMatrix(matrix))
            using (var paint = new SKPaint { ColorFilter = filter, IsAntialias = true })
            {
                if (isReflection)
                {
                    using (var mask = SKShader.CreateLinearGradient(new SKPoint(0, dc.Ty1 > dc.Ty2 ? destination.Bottom : destination.Top),
                        new SKPoint(0, dc.Ty1 > dc.Ty2 ? destination.Top : destination.Bottom), new[] { SKColors.White, SKColors.Transparent }, SKShaderTileMode.Clamp))
                    using (var maskPaint = new SKPaint { Shader = mask, BlendMode = SKBlendMode.DstIn })
                    {
                        _Canvas.SaveLayer(destination, null);
                        _Canvas.DrawImage(texture.Image, source, destination, new SKSamplingOptions(SKFilterMode.Linear), paint);
                        _Canvas.DrawRect(destination, maskPaint);
                        _Canvas.Restore();
                    }
                }
                else
                {
                    _Canvas.DrawImage(texture.Image, source, destination, new SKSamplingOptions(SKFilterMode.Linear), paint);
                }
            }

            _Canvas.Restore();
        }
    }
}
