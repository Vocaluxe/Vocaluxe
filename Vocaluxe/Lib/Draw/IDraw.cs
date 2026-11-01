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

using System.Drawing;
using SkiaSharp;
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Draw
{
    interface IDraw
    {
        bool Init();
        void MainLoop();
        void Close();

        int GetScreenWidth();
        int GetScreenHeight();

        int GetTextureCount();

        CTextureRef CopyScreen();
        void CopyScreen(ref CTextureRef texture);
        void MakeScreenShot();

        CTextureRef AddTexture(string texturePath);
        CTextureRef AddTexture(SKBitmap bitmap);
        CTextureRef AddTexture(int w, int h, byte[] data);
        void UpdateTexture(CTextureRef texture, SKBitmap bmp);
        void UpdateTexture(CTextureRef texture, int w, int h, byte[] data);
        CTextureRef EnqueueTexture(string texturePath);
        CTextureRef EnqueueTexture(SKBitmap bmp);
        CTextureRef EnqueueTexture(int w, int h, byte[] data);
        void EnqueueTextureUpdate(CTextureRef textureRef, SKBitmap bmp);
        CTextureRef CopyTexture(CTextureRef textureRef);
        bool SupportsYuvVideo { get; }
        void UpdateYuvTexture(ref CTextureRef textureRef, int w, int h, byte[] data, float[] yuvToRgbRows);
        void RemoveTexture(ref CTextureRef texture);

        // Basic Draw Methods
        void DrawRect(SColorF color, SRectF rect, bool allMonitors = true);
        void DrawRectReflection(SColorF color, SRectF rect, float space, float height);

        void DrawTexture(CTextureRef texture, SRectF rect, SColorF color, bool mirrored = false, bool allMonitors = true);
        void DrawTexture(CTextureRef texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored = false, bool allMonitors = true);
        void DrawTexture(CTextureRef texture, SRectF rect, SColorF color, float begin, float end, bool allMonitors = true);

        void DrawTextureReflection(CTextureRef texture, SRectF rect, SColorF color, SRectF bounds, float space, float height, bool allMonitors = true);
    }
}