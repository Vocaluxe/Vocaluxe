using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using Vocaluxe.Base;
using Vocaluxe.Menu;

namespace Vocaluxe.Lib.Draw
{
    interface IDraw
    {
        bool Init();
        void MainLoop();
        bool Unload();

        int GetScreenWidth();
        int GetScreenHeight();

        RectangleF GetTextBounds(CText text);
        RectangleF GetTextBounds(CText text, float Height);
       
        // Basic Draw Methods
        void DrawLine(int a, int r, int g, int b, int w, int x1, int y1, int x2, int y2);
        void DrawColor(SColorF color, SRectF rect);
        void DrawColorReflection(SColorF color, SRectF rect, float space, float height);

        void ClearScreen();
        STexture CopyScreen();
        void CopyScreen(ref STexture Texture);
        void MakeScreenShot();

        // Draw Basic Text (must be deleted later)
        void DrawText(string Text, int x, int y, int h);

        STexture AddTexture(string TexturePath);
        STexture AddTexture(Bitmap Bitmap);
        STexture AddTexture(int W, int H, IntPtr Data);
        STexture AddTexture(int W, int H, ref byte[] Data);
        STexture QuequeTexture(int W, int H, ref byte[] Data);
        bool UpdateTexture(ref STexture Texture, ref byte[] Data);
        bool UpdateTexture(ref STexture Texture, IntPtr Data);
        
        void RemoveTexture(ref STexture Texture);
        void DrawTexture(STexture Texture);
        void DrawTexture(STexture Texture, SRectF rect);
        void DrawTexture(STexture Texture, SRectF rect, SColorF color);
        void DrawTexture(STexture Texture, SRectF rect, SColorF color, SRectF bounds);

        void DrawTexture(STexture Texture, SRectF rect, SColorF color, bool mirrored);
        void DrawTexture(STexture Texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored);
        void DrawTexture(STexture Texture, SRectF rect, SColorF color, float begin, float end);

        void DrawTextureReflection(STexture Texture, SRectF rect, SColorF color, SRectF bounds, float space, float height);

        int TextureCount();
    }
}
