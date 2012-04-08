using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using Vocaluxe.Base;
using Vocaluxe.Menu;

namespace Vocaluxe.Lib.Draw
{
    #region Structs
    public struct SColorF
    {
        public float R;
        public float G;
        public float B;
        public float A;

        public SColorF(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public SColorF(SColorF Color)
        {
            R = Color.R;
            G = Color.G;
            B = Color.B;
            A = Color.A;
        }
    }

    public struct SRectF
    {
        public float X;
        public float Y;
        public float W;
        public float H;
        public float Z;
        public float Rotation; //0..360°

        public SRectF(float x, float y, float w, float h, float z)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
            Z = z;
            Rotation = 0f;
        }

        public SRectF(SRectF rect)
        {
            X = rect.X;
            Y = rect.Y;
            W = rect.W;
            H = rect.H;
            Z = rect.Z;
            Rotation = 0f;
        }
    }

    public struct STexture
    {
        public int index;
        public int PBO;
        public int ID;
        
        public string TexturePath;

        public float width;
        public float height;
        public SRectF rect;
        
        public float w2;    //power of 2 width
        public float h2;    //power of 2 height
        public float width_ratio;
        public float height_ratio;
        
        public SColorF color;

        public STexture(int Index)
        {
            index = Index;
            PBO = 0;
            ID = -1;
            TexturePath = String.Empty;

            width = 1f;
            height = 1f;
            rect = new SRectF(0f, 0f, 1f, 1f, 0f);

            w2 = 2f;    
            h2 = 2f;    
            width_ratio = 0.5f;
            height_ratio = 0.5f;

            color = new SColorF(1f, 1f, 1f, 1f);
        }
    }
    #endregion Structs

    enum EAspect
    {
        Crop,
        LetterBox,
        Stretch
    }

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
