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
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SDL;
using SkiaSharp;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib
{
    static unsafe class CSdl
    {
        private static bool _Initialized;
        private static string _FolderDialogResult;
        private static int _FolderDialogDone;

        public static SDL_Window* MainWindow { get; set; }

        public static bool Init()
        {
            if (_Initialized)
            {
                return true;
            }

            SDL3.SDL_SetHint(SDL3.SDL_HINT_APP_NAME, CSettings.ProgramName);
#if ARCH_X86
            SDL3.SDL_SetHint(SDL3.SDL_HINT_JOYSTICK_GAMEINPUT, "0");
            SDL3.SDL_SetHint(SDL3.SDL_HINT_JOYSTICK_GAMEINPUT_RAW, "0");
            SDL3.SDL_SetHint(SDL3.SDL_HINT_WINDOWS_GAMEINPUT, "0");
#endif
            if (!SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_GAMEPAD))
            {
                CLog.Error("Failed to initialize SDL: " + SDL3.SDL_GetError());
                return false;
            }

            _Initialized = true;
            return true;
        }

        public static void Close()
        {
            if (!_Initialized)
            {
                return;
            }

            SDL3.SDL_Quit();
            _Initialized = false;
        }

        public static void DoEvents()
        {
            if (_Initialized)
            {
                SDL3.SDL_PumpEvents();
            }
        }

        public static void ShowMessage(string title, string message, bool error = false)
        {
            SDL3.SDL_ShowSimpleMessageBox(error ? SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR : SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION, title, message, MainWindow);
        }

        public static int ShowMessage(string title, string message, bool error, params string[] buttons)
        {
            var allocated = new List<IntPtr>();
            try
            {
                var buttonData = stackalloc SDL_MessageBoxButtonData[buttons.Length];
                for (var i = 0; i < buttons.Length; i++)
                {
                    buttonData[i].buttonID = i;
                    buttonData[i].text = _ToUtf8(buttons[i], allocated);
                    buttonData[i].flags = i == 0 ? SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_RETURNKEY_DEFAULT : 0;
                }

                var data = new SDL_MessageBoxData
                {
                    flags = (error ? SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR : SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION) | SDL_MessageBoxFlags.SDL_MESSAGEBOX_BUTTONS_LEFT_TO_RIGHT,
                    window = MainWindow,
                    title = _ToUtf8(title, allocated),
                    message = _ToUtf8(message, allocated),
                    numbuttons = buttons.Length,
                    buttons = buttonData
                };

                int buttonId;
                if (!SDL3.SDL_ShowMessageBox(&data, &buttonId))
                {
                    CLog.Error("Failed to show message box: " + SDL3.SDL_GetError());
                    return -1;
                }

                return buttonId;
            }
            finally
            {
                foreach (var ptr in allocated)
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }

        private static byte* _ToUtf8(string text, List<IntPtr> allocated)
        {
            var bytes = Encoding.UTF8.GetBytes((text ?? "") + "\0");
            var ptr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            allocated.Add(ptr);
            return (byte*)ptr;
        }

        public static string ShowFolderDialog(string title)
        {
            _FolderDialogResult = null;
            Interlocked.Exchange(ref _FolderDialogDone, 0);

            var props = SDL3.SDL_CreateProperties();
            SDL3.SDL_SetStringProperty(props, SDL3.SDL_PROP_FILE_DIALOG_TITLE_STRING, title);
            SDL3.SDL_SetPointerProperty(props, SDL3.SDL_PROP_FILE_DIALOG_WINDOW_POINTER, (IntPtr)MainWindow);
            SDL3.SDL_SetBooleanProperty(props, SDL3.SDL_PROP_FILE_DIALOG_MANY_BOOLEAN, false);
            SDL3.SDL_ShowFileDialogWithProperties(SDL_FileDialogType.SDL_FILEDIALOG_OPENFOLDER, &_OnFolderSelected, IntPtr.Zero, props);
            SDL3.SDL_DestroyProperties(props);

            while (Volatile.Read(ref _FolderDialogDone) == 0)
            {
                SDL3.SDL_PumpEvents();
                Thread.Sleep(10);
            }

            return _FolderDialogResult;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void _OnFolderSelected(IntPtr userdata, byte** fileList, int filter)
        {
            if (fileList != null && fileList[0] != null)
            {
                _FolderDialogResult = Marshal.PtrToStringUTF8((IntPtr)fileList[0]);
            }

            Volatile.Write(ref _FolderDialogDone, 1);
        }

        public static void OpenUrl(string url)
        {
            SDL3.SDL_OpenURL(url);
        }

        public static void SetClipboardText(string text)
        {
            SDL3.SDL_SetClipboardText(text);
        }

        public static int GetDisplayCount()
        {
            using (var displays = SDL3.SDL_GetDisplays())
            {
                return displays == null ? 0 : displays.Count;
            }
        }

        public static System.Drawing.Size GetPrimaryDisplaySize()
        {
            SDL_Rect bounds;
            if (!SDL3.SDL_GetDisplayBounds(SDL3.SDL_GetPrimaryDisplay(), &bounds))
            {
                return System.Drawing.Size.Empty;
            }

            return new System.Drawing.Size(bounds.w, bounds.h);
        }

        public static void SetWindowIcon(SDL_Window* window, string iconPath)
        {
            if (!File.Exists(iconPath))
            {
                CLog.Error("Can't find " + iconPath);
                return;
            }

            try
            {
                using (var icon = SKBitmap.Decode(iconPath))
                using (var converted = new SKBitmap(new SKImageInfo(icon.Width, icon.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul)))
                {
                    icon.CopyPixelsTo(converted);
                    var surface = SDL3.SDL_CreateSurfaceFrom(converted.Width, converted.Height, SDL_PixelFormat.SDL_PIXELFORMAT_ARGB8888, converted.GetPixels(),
                        converted.RowBytes);
                    if (surface == null)
                    {
                        return;
                    }

                    SDL3.SDL_SetWindowIcon(window, surface);
                    SDL3.SDL_DestroySurface(surface);
                }
            }
            catch (Exception e)
            {
                CLog.Error("Error loading icon: " + e.Message);
            }
        }

        public static Keys GetModifiers(SDL_Keymod mod)
        {
            var result = Keys.None;
            if ((mod & SDL_Keymod.SDL_KMOD_SHIFT) != 0)
            {
                result |= Keys.Shift;
            }

            if ((mod & SDL_Keymod.SDL_KMOD_CTRL) != 0)
            {
                result |= Keys.Control;
            }

            if ((mod & SDL_Keymod.SDL_KMOD_ALT) != 0)
            {
                result |= Keys.Alt;
            }

            return result;
        }

        public static Keys GetModifiers()
        {
            return GetModifiers(SDL3.SDL_GetModState());
        }

        public static Keys GetKey(SDL_Keycode key)
        {
            if (key >= SDL_Keycode.SDLK_A && key <= SDL_Keycode.SDLK_Z)
            {
                return Keys.A + (int)(key - SDL_Keycode.SDLK_A);
            }

            if (key >= SDL_Keycode.SDLK_0 && key <= SDL_Keycode.SDLK_9)
            {
                return Keys.D0 + (int)(key - SDL_Keycode.SDLK_0);
            }

            if (key >= SDL_Keycode.SDLK_F1 && key <= SDL_Keycode.SDLK_F12)
            {
                return Keys.F1 + (int)(key - SDL_Keycode.SDLK_F1);
            }

            if (key >= SDL_Keycode.SDLK_KP_1 && key <= SDL_Keycode.SDLK_KP_9)
            {
                return Keys.NumPad1 + (int)(key - SDL_Keycode.SDLK_KP_1);
            }

            switch (key)
            {
                case SDL_Keycode.SDLK_BACKSPACE:
                    return Keys.Back;
                case SDL_Keycode.SDLK_TAB:
                    return Keys.Tab;
                case SDL_Keycode.SDLK_RETURN:
                case SDL_Keycode.SDLK_KP_ENTER:
                    return Keys.Enter;
                case SDL_Keycode.SDLK_ESCAPE:
                    return Keys.Escape;
                case SDL_Keycode.SDLK_SPACE:
                    return Keys.Space;
                case SDL_Keycode.SDLK_PAGEUP:
                    return Keys.PageUp;
                case SDL_Keycode.SDLK_PAGEDOWN:
                    return Keys.PageDown;
                case SDL_Keycode.SDLK_END:
                    return Keys.End;
                case SDL_Keycode.SDLK_HOME:
                    return Keys.Home;
                case SDL_Keycode.SDLK_LEFT:
                    return Keys.Left;
                case SDL_Keycode.SDLK_UP:
                    return Keys.Up;
                case SDL_Keycode.SDLK_RIGHT:
                    return Keys.Right;
                case SDL_Keycode.SDLK_DOWN:
                    return Keys.Down;
                case SDL_Keycode.SDLK_PRINTSCREEN:
                    return Keys.PrintScreen;
                case SDL_Keycode.SDLK_INSERT:
                    return Keys.Insert;
                case SDL_Keycode.SDLK_DELETE:
                    return Keys.Delete;
                case SDL_Keycode.SDLK_PAUSE:
                    return Keys.Pause;
                case SDL_Keycode.SDLK_CAPSLOCK:
                    return Keys.CapsLock;
                case SDL_Keycode.SDLK_NUMLOCKCLEAR:
                    return Keys.NumLock;
                case SDL_Keycode.SDLK_SCROLLLOCK:
                    return Keys.Scroll;
                case SDL_Keycode.SDLK_KP_0:
                    return Keys.NumPad0;
                case SDL_Keycode.SDLK_KP_MULTIPLY:
                    return Keys.Multiply;
                case SDL_Keycode.SDLK_KP_PLUS:
                    return Keys.Add;
                case SDL_Keycode.SDLK_KP_MINUS:
                    return Keys.Subtract;
                case SDL_Keycode.SDLK_KP_PERIOD:
                case SDL_Keycode.SDLK_KP_DECIMAL:
                    return Keys.Decimal;
                case SDL_Keycode.SDLK_KP_DIVIDE:
                    return Keys.Divide;
                case SDL_Keycode.SDLK_LSHIFT:
                case SDL_Keycode.SDLK_RSHIFT:
                    return Keys.ShiftKey;
                case SDL_Keycode.SDLK_LCTRL:
                case SDL_Keycode.SDLK_RCTRL:
                    return Keys.ControlKey;
                case SDL_Keycode.SDLK_LALT:
                case SDL_Keycode.SDLK_RALT:
                    return Keys.Menu;
                case SDL_Keycode.SDLK_LGUI:
                    return Keys.LWin;
                case SDL_Keycode.SDLK_RGUI:
                    return Keys.RWin;
                case SDL_Keycode.SDLK_APPLICATION:
                    return Keys.Apps;
                case SDL_Keycode.SDLK_MEDIA_NEXT_TRACK:
                    return Keys.MediaNextTrack;
                case SDL_Keycode.SDLK_MEDIA_PREVIOUS_TRACK:
                    return Keys.MediaPreviousTrack;
                case SDL_Keycode.SDLK_MEDIA_STOP:
                    return Keys.MediaStop;
                case SDL_Keycode.SDLK_MEDIA_PLAY_PAUSE:
                case SDL_Keycode.SDLK_MEDIA_PLAY:
                case SDL_Keycode.SDLK_MEDIA_PAUSE:
                    return Keys.MediaPlayPause;
                case SDL_Keycode.SDLK_MUTE:
                    return Keys.VolumeMute;
                case SDL_Keycode.SDLK_VOLUMEDOWN:
                    return Keys.VolumeDown;
                case SDL_Keycode.SDLK_VOLUMEUP:
                    return Keys.VolumeUp;
                case SDL_Keycode.SDLK_SEMICOLON:
                    return Keys.OemSemicolon;
                case SDL_Keycode.SDLK_PLUS:
                case SDL_Keycode.SDLK_EQUALS:
                    return Keys.Oemplus;
                case SDL_Keycode.SDLK_COMMA:
                    return Keys.Oemcomma;
                case SDL_Keycode.SDLK_MINUS:
                    return Keys.OemMinus;
                case SDL_Keycode.SDLK_PERIOD:
                    return Keys.OemPeriod;
                case SDL_Keycode.SDLK_SLASH:
                    return Keys.OemQuestion;
                case SDL_Keycode.SDLK_GRAVE:
                    return Keys.Oemtilde;
                case SDL_Keycode.SDLK_LEFTBRACKET:
                    return Keys.OemOpenBrackets;
                case SDL_Keycode.SDLK_BACKSLASH:
                    return Keys.OemPipe;
                case SDL_Keycode.SDLK_RIGHTBRACKET:
                    return Keys.OemCloseBrackets;
                case SDL_Keycode.SDLK_APOSTROPHE:
                    return Keys.OemQuotes;
            }

            return Keys.None;
        }
    }
}
