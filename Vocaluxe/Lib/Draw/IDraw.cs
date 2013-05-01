#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System;
using System.Drawing;
using VocaluxeLib.Menu;

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
        STexture CopyScreen();
        void CopyScreen(ref STexture texture);
        void MakeScreenShot();

        STexture AddTexture(string texturePath);
        STexture AddTexture(Bitmap bitmap);
        STexture AddTexture(int w, int h, ref byte[] data);
        STexture EnqueueTexture(int w, int h, ref byte[] data);
        bool UpdateTexture(ref STexture texture, ref byte[] data);
        void RemoveTexture(ref STexture texture);

        // Basic Draw Methods
        void DrawLine(int a, int r, int g, int b, int w, int x1, int y1, int x2, int y2);
        void DrawColor(SColorF color, SRectF rect);
        void DrawColorReflection(SColorF color, SRectF rect, float space, float height);

        void DrawTexture(STexture texture);
        void DrawTexture(STexture texture, SRectF rect);
        void DrawTexture(STexture texture, SRectF rect, SColorF color, bool mirrored = false);
        void DrawTexture(STexture texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored = false);
        void DrawTexture(STexture texture, SRectF rect, SColorF color, float begin, float end);

        void DrawTextureReflection(STexture texture, SRectF rect, SColorF color, SRectF bounds, float space, float height);
    }
}