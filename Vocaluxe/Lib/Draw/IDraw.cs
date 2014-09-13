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
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Draw
{
    interface IDraw
    {
        bool Init();
        void MainLoop();
        void Unload();

        int GetScreenWidth();
        int GetScreenHeight();

        int GetTextureCount();

        void ClearScreen();
        CTextureRef CopyScreen();
        void CopyScreen(ref CTextureRef texture);
        void MakeScreenShot();

        CTextureRef AddTexture(string texturePath);
        CTextureRef AddTexture(Bitmap bitmap);
        CTextureRef AddTexture(int w, int h, byte[] data);
        CTextureRef EnqueueTexture(int w, int h, byte[] data);
        bool UpdateTexture(CTextureRef texture, int w, int h, byte[] data);
        bool UpdateOrAddTexture(ref CTextureRef texture, int w, int h, byte[] data);
        void RemoveTexture(ref CTextureRef texture);

        // Basic Draw Methods
        void DrawLine(int a, int r, int g, int b, int w, int x1, int y1, int x2, int y2);
        void DrawColor(SColorF color, SRectF rect);
        void DrawColorReflection(SColorF color, SRectF rect, float space, float height);

        void DrawTexture(CTextureRef texture);
        void DrawTexture(CTextureRef texture, SRectF rect);
        void DrawTexture(CTextureRef texture, SRectF rect, SColorF color, bool mirrored = false);
        void DrawTexture(CTextureRef texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored = false);
        void DrawTexture(CTextureRef texture, SRectF rect, SColorF color, float begin, float end);

        void DrawTextureReflection(CTextureRef texture, SRectF rect, SColorF color, SRectF bounds, float space, float height);
    }
}