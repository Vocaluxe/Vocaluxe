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
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Serialization;
using VocaluxeLib.Songs;
using VocaluxeLib.Xml;

namespace VocaluxeLib
{
    public struct SMargin
    {
        [XmlAttribute] public int Default;
        public int? Left, Right, Top, Bottom;
        [XmlIgnore] public bool LeftSpecified;
        [XmlIgnore] public bool RightSpecified;
        [XmlIgnore] public bool TopSpecified;
        [XmlIgnore] public bool BottomSpecified;
    }

    public struct SThemeCoverGeneratorText
    {
        public string Text;
        public SThemeFont Font;
        public SThemeColor Color;
        public SMargin Margin;
        public int Indent;
    }

    public struct SThemeCoverGenerator
    {
        [XmlAttribute] public ECoverGeneratorType Type;
        public SThemeCoverGeneratorText Text;
        public SThemeColor BackgroundColor;
        [XmlElement(IsNullable = true)] public string Image;
        [DefaultValue(0.5f)] public float ImageAlpha;
        [DefaultValue(false)] public bool ShowFirstCover;
    }

    public struct SThemeCoverInfo
    {
        public string Name;
        public string Folder;
        [XmlElement(IsNullable = true)] public string Author;
    }

    [XmlRoot("root")]
    public struct SThemeCover
    {
        [XmlIgnore] public string FolderPath;
        public SThemeCoverInfo Info;
        [XmlElement("CoverGenerator", IsNullable = false)] public List<SThemeCoverGenerator> CoverGenerators;
    }

    #region Drawing
    public struct SColorF
    {
        [XmlNormalized] public float R, G, B, A;

        public SColorF(float r, float g, float b, float a)
        {
            Debug.Assert(r.IsInRange(0, 1) && g.IsInRange(0, 1) && b.IsInRange(0, 1));
            Debug.Assert(a.IsInRange(0, 1));
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public SColorF(SColorF color, float a) : this(color.R, color.G, color.B, a) {}
        public SColorF(Color color, float a) : this(color.R / 255f, color.G / 255f, color.B / 255f, a) {}

        public SColorF(Color color)
        {
            R = color.R / 255f;
            G = color.G / 255f;
            B = color.B / 255f;
            A = color.A / 255f;
        }

        [Pure]
        public Color AsColor()
        {
            return Color.FromArgb((int)(A * 255), (int)(R * 255), (int)(G * 255), (int)(B * 255));
        }
    }

    //Use for holding theme-colors
    [XmlRoot("Color")]
    public struct SThemeColor
    {
        [DefaultValue(null), XmlAttribute] public string Name;
        [XmlNormalized] public float? R;
        [XmlNormalized] public float? G;
        [XmlNormalized] public float? B;
        [XmlNormalized] public float? A;

        //Needed for serialization
        public bool NameSpecified
        {
            get { return !String.IsNullOrEmpty(Name); }
        }
        public bool RSpecified
        {
            get { return R.HasValue; }
        }
        public bool GSpecified
        {
            get { return G.HasValue; }
        }
        public bool BSpecified
        {
            get { return B.HasValue; }
        }
        public bool ASpecified
        {
            get { return A.HasValue; }
        }

        public bool Get(int partyModeId, out SColorF color)
        {
            bool ok;
            if (!String.IsNullOrEmpty(Name))
                ok = CBase.Themes.GetColor(Name, partyModeId, out color);
            else
            {
                Debug.Assert(R.HasValue || G.HasValue || B.HasValue);
                ok = true;
                color = new SColorF(1, 1, 1, 1);
            }
            if (R.HasValue)
                color.R = R.Value;
            if (G.HasValue)
                color.G = G.Value;
            if (B.HasValue)
                color.B = B.Value;
            if (A.HasValue)
                color.A = A.Value;
            return ok;
        }
    }

    public struct SRectF
    {
        public float X;
        public float Y;
        public float W;
        public float H;
        public float Z;
        [XmlIgnore] public float Rotation; //0..360°

        [XmlIgnore]
        public float Right
        {
            get { return X + W; }
            set
            {
                W = value - X;
                Debug.Assert(W >= 0);
            }
        }

        [XmlIgnore]
        public float Bottom
        {
            get { return Y + H; }
            set
            {
                H = value - Y;
                Debug.Assert(H >= 0);
            }
        }

        [XmlIgnore]
        public Size SizeI
        {
            get { return new Size((int)W, (int)H); }
        }

        [XmlIgnore]
        public SizeF Size
        {
            get { return new SizeF(W, H); }
        }

        public SRectF(float x, float y, float w, float h, float z)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
            Z = z;
            Rotation = 0f;
        }

        public SRectF(Rectangle rect)
        {
            X = rect.X;
            Y = rect.Y;
            W = rect.Width;
            H = rect.Height;
            Z = 0;
            Rotation = 0;
        }
    }

    [XmlRoot("Reflection")]
    public struct SReflection
    {
        public float Height;
        public float Space;

        public SReflection(float height, float space)
        {
            Height = height;
            Space = space;
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

        /// <summary>
        ///     Returns true if this is about an arrow key (left, right, up down)
        /// </summary>
        /// <returns>True if Key is an arrow key</returns>
        public bool IsArrowKey()
        {
            return Key == Keys.Left || Key == Keys.Right || Key == Keys.Up || Key == Keys.Down;
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

    #region Game
    public struct SPlayer
    {
        public int ProfileID;
        public double Points;
        public double PointsLineBonus;
        public double PointsGoldenNotes;
        public double Rating;
        public int NoteDiff;
        public int VoiceNr;
        public List<CSungLine> SungLines;
        public int CurrentLine;
        public int CurrentNote;

        public int SongID;
        public EGameMode GameMode;
        public long DateTicks;
        public bool SongFinished;
    }

    public struct SDBScoreEntry
    {
        public string Name;
        public int Score;
        public string Date;
        public EGameDifficulty Difficulty;
        public int VoiceNr;
        public int ID;
    }
    #endregion Game
}