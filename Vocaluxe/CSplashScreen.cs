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
using System.IO;
using SDL;
using SkiaSharp;
using Vocaluxe.Base;
using Vocaluxe.Lib;
using VocaluxeLib.Log;

namespace Vocaluxe
{
    unsafe class CSplashScreen
    {
        private readonly SKBitmap _Logo;
        private SDL_Window* _Window;

        public CSplashScreen()
        {
            var path = Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameGraphics, CSettings.FileNameLogo);
            if (File.Exists(path))
            {
                try
                {
                    _Logo = SKBitmap.Decode(path);
                }
                catch (Exception e)
                {
                    CLog.Error("Error loading logo: " + e.Message);
                }
            }
            else
            {
                CLog.Error("Can't find " + path);
            }

            if (_Logo == null)
            {
                return;
            }

            _Window = SDL3.SDL_CreateWindow(CSettings.ProgramName, _Logo.Width, _Logo.Height, SDL_WindowFlags.SDL_WINDOW_BORDERLESS | SDL_WindowFlags.SDL_WINDOW_HIDDEN);
            if (_Window == null)
            {
                CLog.Error("Error creating the splash screen: " + SDL3.SDL_GetError());
                return;
            }

            CSdl.SetWindowIcon(_Window, Path.Combine(CSettings.ProgramFolder, CSettings.FileNameIcon));
            SDL3.SDL_SetWindowPosition(_Window, (int)SDL3.SDL_WINDOWPOS_CENTERED, (int)SDL3.SDL_WINDOWPOS_CENTERED);
            SDL3.SDL_ShowWindow(_Window);
            _Paint();
        }

        private void _Paint()
        {
            var surface = SDL3.SDL_GetWindowSurface(_Window);
            if (surface == null)
            {
                return;
            }

            var colorType = surface->format == SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888 || surface->format == SDL_PixelFormat.SDL_PIXELFORMAT_XBGR8888
                                ? SKColorType.Rgba8888 : SKColorType.Bgra8888;
            using (var target = SKSurface.Create(new SKImageInfo(surface->w, surface->h, colorType, SKAlphaType.Premul), surface->pixels, surface->pitch))
            using (var image = SKImage.FromBitmap(_Logo))
            {
                if (target != null)
                {
                    target.Canvas.Clear(SKColors.Black);
                    target.Canvas.DrawImage(image, SKRect.Create(surface->w, surface->h), new SKSamplingOptions(SKCubicResampler.Mitchell));
                    target.Canvas.Flush();
                }
            }

            SDL3.SDL_UpdateWindowSurface(_Window);
        }

        public void Close()
        {
            if (_Window != null)
            {
                SDL3.SDL_DestroyWindow(_Window);
                _Window = null;
            }

            _Logo?.Dispose();
        }
    }
}
