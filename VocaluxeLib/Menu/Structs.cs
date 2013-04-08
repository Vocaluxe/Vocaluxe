using System;
using System.Collections.Generic;
using System.Windows.Forms;

using VocaluxeLib.Menu.SingNotes;

namespace VocaluxeLib.Menu
{
    #region Drawing
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

        public float w2; //power of 2 width
        public float h2; //power of 2 height
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
    #endregion Drawing

    #region Inputs
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

            Mod = EModifier.None;

            if (alt)
                Mod |= EModifier.Alt;
            if (shift)
                Mod |= EModifier.Shift;
            if (ctrl)
                Mod |= EModifier.Ctrl;
        }
    }

    public struct MouseEvent
    {
        public ESender Sender;
        public bool Handled;
        public int X;
        public int Y;
        public bool LB; //left button click
        public bool LD; //left button double click
        public bool RB; //right button click
        public bool MB; //middle button click

        public bool LBH; //left button hold (when moving)
        public bool RBH; //right button hold (when moving)
        public bool MBH; //middle button hold (when moving)

        public bool ModALT;
        public bool ModSHIFT;
        public bool ModCTRL;

        public EModifier Mod;
        public int Wheel;

        public MouseEvent(ESender sender, bool alt, bool shift, bool ctrl, int x, int y, bool lb, bool ld, bool rb, int wheel, bool lbh, bool rbh, bool mb, bool mbh)
        {
            Sender = sender;
            Handled = false;
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
    #endregion Inputs

    #region Profiles
    public struct SProfile
    {
        public string PlayerName;
        public string ProfileFile;

        public EGameDifficulty Difficulty;
        public SAvatar Avatar;
        public EOffOn GuestProfile;
        public EOffOn Active;
    }

    public struct SAvatar
    {
        public string FileName;
        public STexture Texture;

        public SAvatar(int dummy)
        {
            FileName = String.Empty;
            Texture = new STexture(-1);
        }
    }
    #endregion Profiles

    #region Game
    public struct SPlayer
    {
        public int ProfileID;
        public string Name;
        public EGameDifficulty Difficulty;
        public double Points;
        public double PointsLineBonus;
        public double PointsGoldenNotes;
        public int NoteDiff;
        public int LineNr;
        public List<CLine> SingLine;
        public int CurrentLine;
        public int CurrentNote;

        public int SongID;
        public bool Medley;
        public bool Duet;
        public bool ShortSong;
        public long DateTicks;
        public bool SongFinished;
    }

    public struct SScores
    {
        public string Name;
        public int Score;
        public string Date;
        public EGameDifficulty Difficulty;
        public int LineNr;
        public int ID;
    }

    public struct SPartyModeInfos
    {
        public int PartyModeID;
        public string Name;
        public string Description;
        public string TargetAudience;
        public int MaxPlayers;
        public int MinPlayers;
        public int MaxTeams;
        public int MinTeams;

        public string Author;
        public bool Playable;
        public int VersionMajor;
        public int VersionMinor;
    }
    #endregion Game
}