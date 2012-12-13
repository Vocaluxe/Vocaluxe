using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;

namespace Vocaluxe.Menu
{
    interface IMenu
    {
        void Initialize(IConfig Config, ISettings Settings, ITheme Theme, IHelper Helper, ILog Log, IBackgroundMusic BackgroundMusic, IDrawing Draw, IGraphics Graphics);

        void LoadTheme();
        void SaveTheme();
        void ReloadTextures();
        void UnloadTextures();
        void ReloadTheme();

        bool HandleInput(KeyEvent KeyEvent);
        bool HandleMouse(MouseEvent MouseEvent);
        bool HandleInputThemeEditor(KeyEvent KeyEvent);
        bool HandleMouseThemeEditor(MouseEvent MouseEvent);

        bool UpdateGame();
        void ApplyVolume();
        void OnShow();
        void OnShowFinish();
        void OnClose();

        bool Draw();
        SRectF GetScreenArea();

        void NextInteraction();
        void PrevInteraction();

        bool NextElement();
        bool PrevElement();

        void ProcessMouseClick(int x, int y);
        void ProcessMouseMove(int x, int y);
    }

    interface IConfig
    {
        void SetBackgroundMusicVolume(int NewVolume);
        int GetBackgroundMusicVolume();
    }

    interface ISettings
    {
        int GetRenderW();
        int GetRenderH();
        bool IsTabNavigation();

        float GetZFar();
        float GetZNear();

        EGameState GetGameState();
    }

    interface ITheme
    {
        string GetThemeScreensPath();
        int GetSkinIndex();

        void UnloadSkins();
        void ListSkins();
        void LoadSkins();
        void LoadTheme();
    }

    interface IHelper
    {
    }

    interface IBackgroundMusic
    {
        bool IsDisabled();
        bool IsPlaying();
        
        void Next();
        void Previous();
        void Pause();
        void Play();

        void ApplyVolume();
    }

    interface IDrawing
    {
        RectangleF GetTextBounds(CText text);
    }

    interface IGraphics
    {
        void ReloadTheme();
    }

    interface ILog
    {
        void LogError(string ErrorText);
    }

    [Flags]
    public enum EModifier
    {
        None,
        Shift,
        Alt,
        Ctrl
    }

    public enum ESender
    {
        Mouse,
        Keyboard,
        WiiMote,
        Gamepad
    }

    public struct KeyEvent
    {
        public ESender Sender;
        public bool ModALT;
        public bool ModSHIFT;
        public bool ModCTRL;
        public bool KeyPressed;
        public bool Handled;
        public Keys Key;
        public Char Unicode;
        public EModifier Mod;

        public KeyEvent(ESender sender, bool alt, bool shift, bool ctrl, bool pressed, char unicode, Keys key)
        {
            Sender = sender;
            ModALT = alt;
            ModSHIFT = shift;
            ModCTRL = ctrl;
            KeyPressed = pressed;
            Unicode = unicode;
            Key = key;
            Handled = false;

            EModifier mALT = EModifier.None;
            EModifier mSHIFT = EModifier.None;
            EModifier mCTRL = EModifier.None;

            if (alt)
                mALT = EModifier.Alt;

            if (shift)
                mSHIFT = EModifier.Shift;

            if (ctrl)
                mCTRL = EModifier.Ctrl;

            if (!alt && !shift && !ctrl)
                Mod = EModifier.None;
            else
                Mod = mALT | mSHIFT | mCTRL;
        }
    }

    public struct MouseEvent
    {
        public ESender Sender;
        public int X;
        public int Y;
        public bool LB;     //left button click
        public bool LD;     //left button double click
        public bool RB;     //right button click
        public bool MB;     //middle button click

        public bool LBH;    //left button hold (when moving)
        public bool RBH;    //right button hold (when moving)
        public bool MBH;    //middle button hold (when moving)

        public bool ModALT;
        public bool ModSHIFT;
        public bool ModCTRL;

        public EModifier Mod;
        public int Wheel;

        public MouseEvent(ESender sender, bool alt, bool shift, bool ctrl, int x, int y, bool lb, bool ld, bool rb, int wheel, bool lbh, bool rbh, bool mb, bool mbh)
        {
            Sender = sender;
            X = x;
            Y = y;
            LB = lb;
            LD = ld;
            RB = rb;
            MB = mb;

            LBH = lbh;
            RBH = rbh;
            MBH = mbh;

            ModALT = alt;
            ModSHIFT = shift;
            ModCTRL = ctrl;

            EModifier mALT = EModifier.None;
            EModifier mSHIFT = EModifier.None;
            EModifier mCTRL = EModifier.None;

            if (alt)
                mALT = EModifier.Alt;

            if (shift)
                mSHIFT = EModifier.Shift;

            if (ctrl)
                mCTRL = EModifier.Ctrl;

            if (!alt && !shift && !ctrl)
                Mod = EModifier.None;
            else
                Mod = mALT | mSHIFT | mCTRL;

            Wheel = wheel;
        }
    }

    public enum EGameState
    {
        Start,
        Normal,
        EditTheme
    }

    enum EAspect
    {
        Crop,
        LetterBox,
        Stretch
    }

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

    public struct SPoint3f
    {
        public float X;
        public float Y;
        public float Z;
    }

    public struct SPoint3
    {
        public int X;
        public int Y;
        public int Z;
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
}
