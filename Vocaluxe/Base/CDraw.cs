using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;

namespace Vocaluxe.Base
{
    static class CDraw
    {
        private static IDraw _Draw = null;
        
        public static void InitDraw()
        {
            switch (CConfig.Renderer)
            {
                case ERenderer.TR_CONFIG_SOFTWARE:
                    _Draw = new CDrawWinForm();
                    break;

                case ERenderer.TR_CONFIG_OPENGL:
                    try
                    {
                        _Draw = new COpenGL();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message + " - Error in initializing of OpenGL. Please check whether" +
                            " your graphic card drivers are up to date.");
                        CLog.LogError(e.Message + " - Error in initializing of OpenGL. Please check whether" +
                            " your graphic card drivers are up to date.");
                        Environment.Exit(Environment.ExitCode);
                    }
                    break;

#if WIN
                case ERenderer.TR_CONFIG_DIRECT3D:
                    try
                    {
                        _Draw = new CDirect3D();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message + " - Error in initializing of Direct3D. Please check if " +
                            "your DirectX redistributables and graphic card drivers are up to date. You can " +
                            "download the DirectX runtimes at http://www.microsoft.com/download/en/details.aspx?id=8109",
                    CSettings.sProgramName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        CLog.LogError(e.Message + " - Error in initializing of Direct3D. Please check if " +
                            "your DirectX redistributables and graphic card drivers are up to date. You can " +
                            "download the DirectX runtimes at http://www.microsoft.com/download/en/details.aspx?id=8109");
                        Environment.Exit(Environment.ExitCode);
                    }
                    break;
#endif

                default:
                    _Draw = new CDrawWinForm();
                    break;
            }
            _Draw.Init();
        }

        public static void MainLoop()
        {
            CGraphics.InitFirstScreen();
            _Draw.MainLoop();
        }

        public static bool Unload()
        {
            return _Draw.Unload();
        }

        public static int GetScreenWidth()
        {
            return _Draw.GetScreenWidth();
        }

        public static int GetScreenHeight()
        {
            return _Draw.GetScreenHeight();
        }

        public static RectangleF GetTextBounds(CText text)
        {
            return _Draw.GetTextBounds(text);
        }

        public static RectangleF GetTextBounds(CText text, float Height)
        {
            return _Draw.GetTextBounds(text, Height);
        }

        public static void DrawLine(int a, int r, int g, int b, int w, int x1, int y1, int x2, int y2)
        {
            _Draw.DrawLine(a, r, g, b, w, x1, y1, x2, y2);
        }

        public static void DrawRect(SColorF Color, SRectF Rect, float Thickness)
        {
            if (Thickness <= 0f)
                return;

            _Draw.DrawColor(Color, new SRectF(Rect.X - Thickness / 2, Rect.Y - Thickness / 2, Rect.W + Thickness, Thickness, Rect.Z));
            _Draw.DrawColor(Color, new SRectF(Rect.X - Thickness / 2, Rect.Y + Rect.H - Thickness / 2, Rect.W + Thickness, Thickness, Rect.Z));
            _Draw.DrawColor(Color, new SRectF(Rect.X - Thickness / 2, Rect.Y - Thickness / 2, Thickness, Rect.H + Thickness, Rect.Z));
            _Draw.DrawColor(Color, new SRectF(Rect.X + Rect.W - Thickness / 2, Rect.Y - Thickness / 2, Thickness, Rect.H + Thickness, Rect.Z));
        }

        public static void DrawColor(SColorF color, SRectF rect)
        {
            _Draw.DrawColor(color, rect);
        }

        public static void DrawColorReflection(SColorF color, SRectF rect, float space, float height)
        {
            _Draw.DrawColorReflection(color, rect, space, height);
        }

        public static void ClearScreen()
        {
            _Draw.ClearScreen();
        }

        public static STexture CopyScreen()
        {
            return _Draw.CopyScreen();
        }

        public static void CopyScreen(ref STexture Texture)
        {
            _Draw.CopyScreen(ref Texture);
        }

        public static void MakeScreenShot()
        {
            _Draw.MakeScreenShot();
        }
        
        // Draw Basic Text (must be deleted later)
        public static void DrawText(string Text, int x, int y, int h)
        {
            _Draw.DrawText(Text, x, y, h);
        }

        public static STexture AddTexture(Bitmap Bitmap)
        {
            return _Draw.AddTexture(Bitmap);
        }

        public static STexture AddTexture(string TexturePath)
        {
            return _Draw.AddTexture(TexturePath);
        }

        public static STexture AddTexture(string TexturePath, int MaxSize)
        {
            if (MaxSize == 0)
                return _Draw.AddTexture(TexturePath);

            if (!System.IO.File.Exists(TexturePath))
                return new STexture(-1);

            Bitmap origin = new Bitmap(TexturePath);
            int w = MaxSize;
            int h = MaxSize;

            if (origin.Width >= origin.Height && origin.Width > w)
                h = (int)Math.Round((float)w / origin.Width * origin.Height);
            else if (origin.Height > origin.Width && origin.Height > h)
                w = (int)Math.Round((float)h / origin.Height * origin.Width);

            Bitmap bmp = new Bitmap(origin, w, h);
            STexture tex = _Draw.AddTexture(bmp);
            bmp.Dispose();
            origin.Dispose();
            return tex;
        }

        public static STexture AddTexture(int W, int H, IntPtr Data)
        {
            return _Draw.AddTexture(W, H, Data);
        }

        public static STexture AddTexture(int W, int H, ref byte[] Data)
        {
            return _Draw.AddTexture(W, H, ref Data);
        }

        public static STexture QuequeTexture(int W, int H, ref byte[] Data)
        {
            return _Draw.QuequeTexture(W, H, ref Data);
        }

        public static bool UpdateTexture(ref STexture Texture, ref byte[] Data)
        {
            return _Draw.UpdateTexture(ref Texture, ref Data);
        }

        public static bool UpdateTexture(ref STexture Texture, IntPtr Data)
        {
            return _Draw.UpdateTexture(ref Texture, Data);
        }

        public static void RemoveTexture(ref STexture Texture)
        {
            _Draw.RemoveTexture(ref Texture);
        }

        public static void DrawTexture(STexture Texture)
        {
            _Draw.DrawTexture(Texture);
        }

        public static void DrawTexture(STexture Texture, SRectF rect)
        {
            _Draw.DrawTexture(Texture, rect);
        }

        public static void DrawTexture(STexture Texture, SRectF rect, SColorF color)
        {
            _Draw.DrawTexture(Texture, rect, color);
        }

        public static void DrawTexture(STexture Texture, SRectF rect, SColorF color, SRectF bounds)
        {
            _Draw.DrawTexture(Texture, rect, color, bounds);
        }

        public static void DrawTexture(STexture Texture, SRectF rect, SColorF color, bool mirrored)
        {
            _Draw.DrawTexture(Texture, rect, color, mirrored);
        }

        public static void DrawTexture(STexture Texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored)
        {
            _Draw.DrawTexture(Texture, rect, color, bounds, mirrored);
        }

        public static void DrawTexture(STexture Texture, SRectF rect, SColorF color, float begin, float end)
        {
            _Draw.DrawTexture(Texture, rect, color, begin, end);
        }

        public static void DrawTexture(CStatic StaticBounds, STexture Texture, EAspect Aspect)
        {
            RectangleF bounds = new RectangleF(StaticBounds.Rect.X, StaticBounds.Rect.Y, StaticBounds.Rect.W, StaticBounds.Rect.H);
            RectangleF rect = new RectangleF(0f, 0f, Texture.width, Texture.height);

            if (rect.Height <= 0f)
                return;

            CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, Aspect);
            DrawTexture(Texture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, StaticBounds.Rect.Z),
                    Texture.color, new SRectF(bounds.X, bounds.Y, bounds.Width, bounds.Height, 0f), false);
        }

        public static void DrawTextureReflection(STexture Texture, SRectF rect, SColorF color, SRectF bounds, float space, float height)
        {
            _Draw.DrawTextureReflection(Texture, rect, color, bounds, space, height);
        }

        public static int TextureCount()
        {
            return _Draw.TextureCount();
        }
    }
}
