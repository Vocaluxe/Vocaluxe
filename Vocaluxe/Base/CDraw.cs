using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Vocaluxe.Lib.Draw;
using VocaluxeLib.Menu;

namespace Vocaluxe.Base
{
    static class CDraw
    {
        private static IDraw _Draw;

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
                                        CSettings.ProgramName, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            bool result = _Draw.Unload();
            _Draw = null;
            return result;
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

        public static RectangleF GetTextBounds(CText text, float height)
        {
            return _Draw.GetTextBounds(text, height);
        }

        public static void DrawLine(int a, int r, int g, int b, int w, int x1, int y1, int x2, int y2)
        {
            _Draw.DrawLine(a, r, g, b, w, x1, y1, x2, y2);
        }

        public static void DrawRect(SColorF color, SRectF rect, float thickness)
        {
            if (thickness <= 0f)
                return;

            _Draw.DrawColor(color, new SRectF(rect.X - thickness / 2, rect.Y - thickness / 2, rect.W + thickness, thickness, rect.Z));
            _Draw.DrawColor(color, new SRectF(rect.X - thickness / 2, rect.Y + rect.H - thickness / 2, rect.W + thickness, thickness, rect.Z));
            _Draw.DrawColor(color, new SRectF(rect.X - thickness / 2, rect.Y - thickness / 2, thickness, rect.H + thickness, rect.Z));
            _Draw.DrawColor(color, new SRectF(rect.X + rect.W - thickness / 2, rect.Y - thickness / 2, thickness, rect.H + thickness, rect.Z));
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

        public static void CopyScreen(ref STexture texture)
        {
            _Draw.CopyScreen(ref texture);
        }

        public static void MakeScreenShot()
        {
            _Draw.MakeScreenShot();
        }

        // Draw Basic Text (must be deleted later)
        public static void DrawText(string text, int x, int y, int h)
        {
            _Draw.DrawText(text, x, y, h);
        }

        public static STexture AddTexture(Bitmap bitmap)
        {
            return _Draw.AddTexture(bitmap);
        }

        public static STexture AddTexture(string texturePath)
        {
            return _Draw.AddTexture(texturePath);
        }

        public static STexture AddTexture(string texturePath, int maxSize)
        {
            if (maxSize == 0)
                return _Draw.AddTexture(texturePath);

            if (!File.Exists(texturePath))
                return new STexture(-1);

            using (Bitmap origin = new Bitmap(texturePath))
            {
                int w = maxSize;
                int h = maxSize;

                if (origin.Width >= origin.Height && origin.Width > w)
                    h = (int)Math.Round((float)w / origin.Width * origin.Height);
                else if (origin.Height > origin.Width && origin.Height > h)
                    w = (int)Math.Round((float)h / origin.Height * origin.Width);

                using (Bitmap bmp = new Bitmap(origin, w, h))
                {
                    STexture tex = _Draw.AddTexture(bmp);
                    return tex;
                }
            }
        }

        public static STexture AddTexture(int w, int h, IntPtr data)
        {
            return _Draw.AddTexture(w, h, data);
        }

        public static STexture AddTexture(int w, int h, ref byte[] data)
        {
            return _Draw.AddTexture(w, h, ref data);
        }

        public static STexture QuequeTexture(int w, int h, ref byte[] data)
        {
            return _Draw.QuequeTexture(w, h, ref data);
        }

        public static bool UpdateTexture(ref STexture texture, ref byte[] data)
        {
            return _Draw.UpdateTexture(ref texture, ref data);
        }

        public static bool UpdateTexture(ref STexture texture, IntPtr data)
        {
            return _Draw.UpdateTexture(ref texture, data);
        }

        public static void RemoveTexture(ref STexture texture)
        {
            _Draw.RemoveTexture(ref texture);
        }

        public static void DrawTexture(STexture texture)
        {
            _Draw.DrawTexture(texture);
        }

        public static void DrawTexture(STexture texture, SRectF rect)
        {
            _Draw.DrawTexture(texture, rect);
        }

        public static void DrawTexture(STexture texture, SRectF rect, SColorF color)
        {
            _Draw.DrawTexture(texture, rect, color);
        }

        public static void DrawTexture(STexture texture, SRectF rect, SColorF color, SRectF bounds)
        {
            _Draw.DrawTexture(texture, rect, color, bounds);
        }

        public static void DrawTexture(STexture texture, SRectF rect, SColorF color, bool mirrored)
        {
            _Draw.DrawTexture(texture, rect, color, mirrored);
        }

        public static void DrawTexture(STexture texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored)
        {
            _Draw.DrawTexture(texture, rect, color, bounds, mirrored);
        }

        public static void DrawTexture(STexture texture, SRectF rect, SColorF color, float begin, float end)
        {
            _Draw.DrawTexture(texture, rect, color, begin, end);
        }

        public static void DrawTexture(CStatic staticBounds, STexture texture, EAspect aspect)
        {
            RectangleF bounds = new RectangleF(staticBounds.Rect.X, staticBounds.Rect.Y, staticBounds.Rect.W, staticBounds.Rect.H);
            RectangleF rect = new RectangleF(0f, 0f, texture.Width, texture.Height);

            if (rect.Height <= 0f)
                return;

            CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, aspect);
            DrawTexture(texture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, staticBounds.Rect.Z),
                        texture.Color, new SRectF(bounds.X, bounds.Y, bounds.Width, bounds.Height, 0f), false);
        }

        public static void DrawTextureReflection(STexture texture, SRectF rect, SColorF color, SRectF bounds, float space, float height)
        {
            _Draw.DrawTextureReflection(texture, rect, color, bounds, space, height);
        }

        public static int TextureCount()
        {
            return _Draw.TextureCount();
        }
    }
}