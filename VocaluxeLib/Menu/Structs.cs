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

        public SColorF(SColorF color)
        {
            R = color.R;
            G = color.G;
            B = color.B;
            A = color.A;
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

    public struct SPoint3F
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
        public int Index;
        public int PBO;
        public int ID;

        public string TexturePath;

        public float Width;
        public float Height;
        public SRectF Rect;

        public float W2; //power of 2 width
        public float H2; //power of 2 height
        public float WidthRatio;
        public float HeightRatio;

        public SColorF Color;

        public STexture(int index)
        {
            Index = index;
            PBO = 0;
            ID = -1;
            TexturePath = String.Empty;

            Width = 1f;
            Height = 1f;
            Rect = new SRectF(0f, 0f, 1f, 1f, 0f);

            W2 = 2f;
            H2 = 2f;
            WidthRatio = 0.5f;
            HeightRatio = 0.5f;

            Color = new SColorF(1f, 1f, 1f, 1f);
        }
    }
    #endregion Drawing

    #region Inputs
    public struct SKeyEvent
    {
        public ESender Sender;
        public bool ModAlt;
        public bool ModShift;
        public bool ModCtrl;
        public bool KeyPressed;
        public bool Handled;
        public Keys Key;
        public Char Unicode;
        public EModifier Mod;

        public SKeyEvent(ESender sender, bool alt, bool shift, bool ctrl, bool pressed, char unicode, Keys key)
        {
            Sender = sender;
            ModAlt = alt;
            ModShift = shift;
            ModCtrl = ctrl;
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

    public struct SMouseEvent
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

        public bool ModAlt;
        public bool ModShift;
        public bool ModCtrl;

        public EModifier Mod;
        public int Wheel;

        public SMouseEvent(ESender sender, bool alt, bool shift, bool ctrl, int x, int y, bool lb, bool ld, bool rb, int wheel, bool lbh, bool rbh, bool mb, bool mbh)
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

            ModAlt = alt;
            ModShift = shift;
            ModCtrl = ctrl;

            EModifier mAlt = EModifier.None;
            EModifier mShift = EModifier.None;
            EModifier mCtrl = EModifier.None;

            if (alt)
                mAlt = EModifier.Alt;

            if (shift)
                mShift = EModifier.Shift;

            if (ctrl)
                mCtrl = EModifier.Ctrl;

            if (!alt && !shift && !ctrl)
                Mod = EModifier.None;
            else
                Mod = mAlt | mShift | mCtrl;

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