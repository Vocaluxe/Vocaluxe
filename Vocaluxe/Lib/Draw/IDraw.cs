using System;
using System.Drawing;
using VocaluxeLib.Menu;

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
        RectangleF GetTextBounds(CText text, float height);

        // Basic Draw Methods
        void DrawLine(int a, int r, int g, int b, int w, int x1, int y1, int x2, int y2);
        void DrawColor(SColorF color, SRectF rect);
        void DrawColorReflection(SColorF color, SRectF rect, float space, float height);

        void ClearScreen();
        STexture CopyScreen();
        void CopyScreen(ref STexture texture);
        void MakeScreenShot();

        // Draw Basic Text (must be deleted later)
        void DrawText(string text, int x, int y, int h);

        STexture AddTexture(string texturePath);
        STexture AddTexture(Bitmap bitmap);
        STexture AddTexture(int w, int h, IntPtr data);
        STexture AddTexture(int w, int h, ref byte[] data);
        STexture QuequeTexture(int w, int h, ref byte[] data);
        bool UpdateTexture(ref STexture texture, ref byte[] data);
        bool UpdateTexture(ref STexture texture, IntPtr data);

        void RemoveTexture(ref STexture texture);
        void DrawTexture(STexture texture);
        void DrawTexture(STexture texture, SRectF rect);
        void DrawTexture(STexture texture, SRectF rect, SColorF color);
        void DrawTexture(STexture texture, SRectF rect, SColorF color, SRectF bounds);

        void DrawTexture(STexture texture, SRectF rect, SColorF color, bool mirrored);
        void DrawTexture(STexture texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored);
        void DrawTexture(STexture texture, SRectF rect, SColorF color, float begin, float end);

        void DrawTextureReflection(STexture texture, SRectF rect, SColorF color, SRectF bounds, float space, float height);

        int TextureCount();
    }
}