#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
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

        public Color AsColor()
        {
            return Color.FromArgb((int)(A * 255), (int)(R * 255), (int)(G * 255), (int)(B * 255));
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

        /// <summary>
        /// Original size (e.g. of bmp)
        /// </summary>
        public readonly Size OrigSize;
        public float OrigAspect
        {
            get { return (float)OrigSize.Width / OrigSize.Height; }
        }

        /// <summary>
        /// Current size when drawn
        /// </summary>
        public SRectF Rect;

        public SColorF Color;

        private int _W2, _H2;
        private float _WidthRatio, _HeightRatio;
        private bool _UseFullTexture;
        /// <summary>
        /// Internal texture width (on device), a power of 2 if necessary
        /// </summary>
        public int W2
        {
            get { return _W2; }
            set
            {
                _W2 = value;
                if (!_UseFullTexture)
                    WidthRatio = (float)OrigSize.Width / _W2;
            }
        }
        /// <summary>
        /// Internal texture height (on device), a power of 2 if necessary
        /// </summary>
        public int H2
        {
            get { return _H2; }
            set
            {
                _H2 = value;
                if (!_UseFullTexture)
                    HeightRatio = (float)OrigSize.Height / _H2;
            }
        }

        /// <summary>
        /// Internal use. Specifies which part of texture memory is actually used
        /// </summary>
        public float WidthRatio
        {
            get { return _WidthRatio; }
            private set { _WidthRatio = value; }
        }
        /// <summary>
        /// Internal use. Specifies which part of texture memory is actually used
        /// </summary>
        public float HeightRatio
        {
            get { return _HeightRatio; }
            private set { _HeightRatio = value; }
        }

        /// <summary>
        /// Internal use. Specifies if full texture memory should be used
        /// Set to true if you resized the original image to the maximum
        /// </summary>
        public bool UseFullTexture
        {
            get { return _UseFullTexture; }
            set
            {
                _UseFullTexture = value;
                if (value)
                {
                    WidthRatio = 1;
                    HeightRatio = 1;
                }
                else
                {
                    WidthRatio = (float)OrigSize.Width / _W2;
                    HeightRatio = (float)OrigSize.Height / _H2;
                }
            }
        }

        public STexture(int index, int origWidth = 1, int origHeight = 1)
        {
            Index = index;
            PBO = 0;
            ID = -1;
            TexturePath = String.Empty;

            OrigSize = new Size(origWidth, origHeight);
            Rect = new SRectF(0f, 0f, origWidth, origHeight, 0f);

            Color = new SColorF(1f, 1f, 1f, 1f);

            _W2 = origWidth;
            _H2 = origHeight;
            _WidthRatio = 1;
            _HeightRatio = 1;
            _UseFullTexture = false;
        }
    }
    #endregion Drawing

    #region Inputs
    public struct SKeyEvent
    {
        public readonly ESender Sender;
        public readonly bool ModAlt;
        public readonly bool ModShift;
        public readonly bool ModCtrl;
        public readonly bool KeyPressed;
        public bool Handled;
        public Keys Key;
        public readonly Char Unicode;
        public readonly EModifier Mod;

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
        public readonly ESender Sender;
        public bool Handled;
        public readonly int X;
        public readonly int Y;
        public readonly bool LB; //left button click
        public readonly bool LD; //left button double click
        public readonly bool RB; //right button click
        public readonly bool MB; //middle button click

        public readonly bool LBH; //left button hold (when moving)
        // ReSharper disable MemberCanBePrivate.Global
        public readonly bool RBH; //right button hold (when moving)
        public readonly bool MBH; //middle button hold (when moving)
        // ReSharper restore MemberCanBePrivate.Global

        public readonly EModifier Mod;
        public readonly int Wheel;

        public SMouseEvent(ESender sender, EModifier mod, int x, int y, bool lb, bool ld, bool rb, int wheel, bool lbh, bool rbh, bool mb, bool mbh)
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

            Mod = mod;

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

        // ReSharper disable UnusedParameter.Local
        public SAvatar(int dummy)
            // ReSharper restore UnusedParameter.Local
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