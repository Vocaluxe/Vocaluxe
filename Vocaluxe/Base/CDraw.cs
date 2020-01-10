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
using Vocaluxe.Lib.Draw;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Log;

namespace Vocaluxe.Base
{
    static class CDraw
    {
        private static IDraw _Draw;

        /// <summary>
        ///     Initializes the Driver selected in the config
        /// </summary>
        /// <returns></returns>
        public static bool Init()
        {
            if (_Draw != null)
                return false;
            switch (CConfig.Config.Graphics.Renderer)
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
                        CLog.Fatal(e, "Error in initializing of OpenGL. Please check whether your graphic card drivers are up to date.");
                        return false;
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
                        CLog.Fatal(e, "Error in initializing of Direct3D. Please check if your DirectX redistributables and graphic card drivers are up to date. You can " +
                                      "download the DirectX runtimes at http://www.microsoft.com/download/en/details.aspx?id=8109");
                        return false;
                    }
                    break;
#endif

                default:
                    _Draw = new CDrawWinForm();
                    break;
            }
            return _Draw.Init();
        }

        /// <summary>
        ///     Has to be called to start rendering <br />
        ///     Will only return if program is closed <br />
        ///     Also inits the first screen
        /// </summary>
        public static void MainLoop()
        {
            CGraphics.InitFirstScreen();
            _Draw.MainLoop();
        }

        /// <summary>
        ///     Unloads the current driver, e.g. to switch to another one
        /// </summary>
        public static void Close()
        {
            if (_Draw != null)
            {
                _Draw.Close();
                _Draw = null;
            }
        }

        /// <summary>
        ///     Gets the width of the rendering area
        /// </summary>
        /// <returns></returns>
        public static int GetScreenWidth()
        {
            return _Draw.GetScreenWidth();
        }

        /// <summary>
        ///     Gets the height of the rendering area
        /// </summary>
        /// <returns></returns>
        public static int GetScreenHeight()
        {
            return _Draw.GetScreenHeight();
        }

        /// <summary>
        ///     Takes a screenshot and saves it to the Screenshot folder
        /// </summary>
        public static void MakeScreenShot()
        {
            _Draw.MakeScreenShot();
        }

        /// <summary>
        ///     Gets the current screen content as a texture
        /// </summary>
        /// <returns></returns>
        public static CTextureRef CopyScreen()
        {
            return _Draw.CopyScreen();
        }

        /// <summary>
        ///     Gets the current screen content updating the given texture
        /// </summary>
        /// <param name="texture"></param>
        public static void CopyScreen(ref CTextureRef texture)
        {
            _Draw.CopyScreen(ref texture);
        }

        /// <summary>
        ///     Adds a texture from a Bitmap and returns a reference to it<br />
        ///     Must be called from main thread!
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns>Reference to texture</returns>
        public static CTextureRef AddTexture(Bitmap bitmap)
        {
            return _Draw.AddTexture(bitmap);
        }

        /// <summary>
        ///     Adds a texture from an image file given by its file path<br />
        ///     Must be called from main thread!
        /// </summary>
        /// <param name="texturePath"></param>
        /// <returns>Reference to texture or null on error</returns>
        public static CTextureRef AddTexture(string texturePath)
        {
            return _Draw.AddTexture(texturePath);
        }

        /// <summary>
        ///     Adds a texture from a byte array<br />
        ///     Must be called from main thread!
        /// </summary>
        /// <param name="w">Width of the image</param>
        /// <param name="h">Height of the image</param>
        /// <param name="data">Array of 4 byte values for each pixel</param>
        /// <returns>Reference to texture</returns>
        public static CTextureRef AddTexture(int w, int h, byte[] data)
        {
            return _Draw.AddTexture(w, h, data);
        }

        /// <summary>
        ///     Requests aadding a texture from a byte array <br />
        ///     Use this if you add textures from another thread or don't need it immediatelly
        /// </summary>
        /// <param name="w">Width of the image</param>
        /// <param name="h">Height of the image</param>
        /// <param name="data">Array of 4 byte values for each pixel</param>
        /// <returns>Reference to texture</returns>
        public static CTextureRef EnqueueTexture(int w, int h, byte[] data)
        {
            return _Draw.EnqueueTexture(w, h, data);
        }

        /// <summary>
        ///     Requests adding a texture from a bitmap <br />
        ///     Use this if you add textures from another thread or don't need it immediatelly
        /// </summary>
        /// <param name="bmp">Bitmap to add, gets disposed after adding!</param>
        /// <returns>Reference to texture</returns>
        public static CTextureRef EnqueueTexture(Bitmap bmp)
        {
            return _Draw.EnqueueTexture(bmp);
        }

        /// <summary>
        ///     Requests adding a texture from a file <br />
        ///     Use this if you add textures from another thread or don't need it immediatelly
        /// </summary>
        /// <param name="texturePath">Full path to the image file</param>
        /// <returns>Reference to texture</returns>
        public static CTextureRef EnqueueTexture(String texturePath)
        {
            return _Draw.EnqueueTexture(texturePath);
        }

        /// <summary>
        ///     Requests updating a texture from a bitmap<br />
        ///     Use this if you add textures from another thread or don't need it immediatelly <br />
        ///     Bitmap is freed after use
        /// </summary>
        /// <param name="textureRef">Reference to the texture to update</param>
        /// <param name="bmp"></param>
        public static void EnqueueTextureUpdate(CTextureRef textureRef, Bitmap bmp)
        {
            _Draw.EnqueueTextureUpdate(textureRef, bmp);
        }

        /// <summary>
        ///     Creates a copy of the texture <br />
        ///     Updates to the copy do not affect the original texture
        /// </summary>
        /// <param name="textureRef">Original texture reference</param>
        /// <returns>New texture reference of the copy</returns>
        public static CTextureRef CopyTexture(CTextureRef textureRef)
        {
            return _Draw.CopyTexture(textureRef);
        }

        /// <summary>
        ///     Updates a texture, filling it with the new data
        /// </summary>
        /// <param name="textureRef"></param>
        /// <param name="w">Width of the image</param>
        /// <param name="h">Height of the image</param>
        /// <param name="data">Array of 4 byte values for each pixel</param>
        public static void UpdateTexture(CTextureRef textureRef, int w, int h, byte[] data)
        {
            _Draw.UpdateTexture(textureRef, w, h, data);
        }

        /// <summary>
        ///     Updates a texture, filling it with the new data from the bitmap
        /// </summary>
        /// <param name="textureRef"></param>
        /// <param name="bmp"></param>
        public static void UpdateTexture(CTextureRef textureRef, Bitmap bmp)
        {
            _Draw.UpdateTexture(textureRef, bmp);
        }

        /// <summary>
        ///     Removes a texture from the VRAM and sets it to null to avoid accidential usage <br />
        ///     Note: You can also use its Dispose method to do the same as this gets automaticly called from the Dispose method and destructor of a texture reference
        /// </summary>
        /// <param name="texture"></param>
        public static void RemoveTexture(ref CTextureRef texture)
        {
            // Disposed textures call this, possibly at program end!
            if (_Draw != null)
                _Draw.RemoveTexture(ref texture);
        }

        /// <summary>
        ///     Returns the number of textures in the VRAM (textures might get reused so this might not be the actual number of textures in use)
        /// </summary>
        /// <returns></returns>
        public static int TextureCount()
        {
            return _Draw.GetTextureCount();
        }

        /// <summary>
        ///     Draws a simple (filled) rectangle with the given color
        /// </summary>
        /// <param name="color"></param>
        /// <param name="rect"></param>
        /// <param name="allMonitors">Render on all monitors</param>
        public static void DrawRect(SColorF color, SRectF rect, bool allMonitors = true)
        {
            _Draw.DrawRect(color, rect, allMonitors);
        }

        /// <summary>
        ///     Draws the reflection of a filled rectangle with the given color
        /// </summary>
        /// <param name="color"></param>
        /// <param name="rect">(Original) rectangle to draw the reflection for</param>
        /// <param name="space">Spacing between the rect and the reflection</param>
        /// <param name="height">Height of the reflection</param>
        public static void DrawRectReflection(SColorF color, SRectF rect, float space, float height)
        {
            _Draw.DrawRectReflection(color, rect, space, height);
        }

        /// <summary>
        ///     Draws a texture at its position and with its color
        /// </summary>
        /// <param name="texture">The texture to be drawn</param>
        public static void DrawTexture(CTextureRef texture)
        {
            DrawTexture(texture, texture.Rect);
        }

        /// <summary>
        ///     Draws a texture with its color at the given position
        /// </summary>
        /// <param name="textureRef">The texture to be drawn</param>
        /// <param name="rect">A rectangle with the destination coordinates</param>
        public static void DrawTexture(CTextureRef textureRef, SRectF rect)
        {
            DrawTexture(textureRef, rect, textureRef.Color);
        }

        /// <summary>
        ///     Draws a texture at the given position with the given color
        /// </summary>
        /// <param name="textureRef"></param>
        /// <param name="rect"></param>
        /// <param name="color"></param>
        /// <param name="mirrored">True to mirror the texture on the y axis</param>
        /// <param name="allMonitors">Render on all monitors</param>
        public static void DrawTexture(CTextureRef textureRef, SRectF rect, SColorF color, bool mirrored = false, bool allMonitors = true)
        {
            _Draw.DrawTexture(textureRef, rect, color, mirrored, allMonitors);
        }

        /// <summary>
        ///     Draws a texture at the given position with the given color
        /// </summary>
        /// <param name="textureRef"></param>
        /// <param name="rect"></param>
        /// <param name="color"></param>
        /// <param name="bounds">Bounds to draw the texture in (rest gets cropped off)</param>
        /// <param name="mirrored">True to mirror the texture on the y axis</param>
        /// <param name="allMonitors">Render on all monitors</param>
        public static void DrawTexture(CTextureRef textureRef, SRectF rect, SColorF color, SRectF bounds, bool mirrored = false, bool allMonitors = true)
        {
            _Draw.DrawTexture(textureRef, rect, color, bounds, mirrored, allMonitors);
        }

        /// <summary>
        ///     Draws a part of a texture at the given position with the given color
        /// </summary>
        /// <param name="textureRef"></param>
        /// <param name="rect"></param>
        /// <param name="color"></param>
        /// <param name="begin">Normalized start x-coordinate (0..1)</param>
        /// <param name="end">Normalized end x-coordinate (0..1)</param>
        /// <param name="allMonitors">Render on all monitors</param>
        public static void DrawTexture(CTextureRef textureRef, SRectF rect, SColorF color, float begin, float end, bool allMonitors = true)
        {
            _Draw.DrawTexture(textureRef, rect, color, begin, end, allMonitors);
        }

        /// <summary>
        ///     Draws a texture fitting it within the given bounds with the method specified by aspect
        /// </summary>
        /// <param name="textureRef"></param>
        /// <param name="bounds">Where to draw the texture</param>
        /// <param name="aspect">How to fit a texture in the bounds if the size differs</param>
        public static void DrawTexture(CTextureRef textureRef, SRectF bounds, EAspect aspect)
        {
            DrawTexture(textureRef, bounds, aspect, textureRef.Color);
        }

        /// <summary>
        ///     Draws a texture fitting it within the given color and bounds with the method specified by aspect
        /// </summary>
        /// <param name="textureRef"></param>
        /// <param name="bounds">Where to draw the texture</param>
        /// <param name="aspect">How to fit a texture in the bounds if the size differs</param>
        /// <param name="color">Color to use</param>
        public static void DrawTexture(CTextureRef textureRef, SRectF bounds, EAspect aspect, SColorF color)
        {
            if (textureRef == null)
                return;

            SRectF rect = CHelper.FitInBounds(bounds, textureRef.OrigAspect, aspect);
            DrawTexture(textureRef, rect, color, bounds);
        }

        /// <summary>
        ///     Draws the reflection of a texture at the given position and with the given color
        /// </summary>
        /// <param name="textureRef"></param>
        /// <param name="rect">(Original) rectangle to draw the reflection for</param>
        /// <param name="color"></param>
        /// <param name="bounds"></param>
        /// <param name="space">Spacing between the rect and the reflection</param>
        /// <param name="height">Height of the reflection</param>
        public static void DrawTextureReflection(CTextureRef textureRef, SRectF rect, SColorF color, SRectF bounds, float space, float height, bool allMonitors = true)
        {
            _Draw.DrawTextureReflection(textureRef, rect, color, bounds, space, height, allMonitors);
        }
    }
}