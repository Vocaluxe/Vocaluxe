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
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Serialization;
using VocaluxeLib.Songs;

namespace VocaluxeLib
{
    public enum ECoverGeneratorType
    {
        Default,
        Folder,
        Artist,
        Letter,
        Edition,
        Genre,
        Language,
        Year,
        Decade,
        Date
    }

    public struct SMargin
    {
        public int Default;
        public int Left, Right, Top, Bottom;
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
        public ECoverGeneratorType Type;
        public SThemeCoverGeneratorText Text;
        public SThemeColor BackgroundColor;
        public string Image;
        public float ImageAlpha;
        public bool ShowFirstCover;
    }

    #region Drawing
    public struct SColorF
    {
        [XmlElement("R")] public float R;
        [XmlElement("G")] public float G;
        [XmlElement("B")] public float B;
        [XmlElement("A")] public float A;

        public SColorF(float r, float g, float b, float a)
        {
            Debug.Assert(r.IsInRange(0, 1) && g.IsInRange(0, 1) && b.IsInRange(0, 1));
            Debug.Assert(a.IsInRange(0, 1));
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
        public SColorF Color;
        public string Name;

        //Needed for serialization
        public bool ColorSpecified
        {
            get { return String.IsNullOrEmpty(Name); }
        }
        public bool NameSpecified
        {
            get { return !String.IsNullOrEmpty(Name); }
        }

        public SThemeColor(SThemeColor theme)
        {
            Color = new SColorF(theme.Color);
            Name = theme.Name;
        }

        public bool Get(int partyModeId, out SColorF color)
        {
            if (String.IsNullOrEmpty(Name))
            {
                color = Color;
                return true;
            }
            return CBase.Theme.GetColor(Name, partyModeId, out color);
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

        public float Right
        {
            get { return X + W; }
        }
        public float Bottom
        {
            get { return Y + H; }
        }
        public Size SizeI
        {
            get { return new Size((int)W, (int)H); }
        }
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

        public SRectF(SRectF rect)
        {
            X = rect.X;
            Y = rect.Y;
            W = rect.W;
            H = rect.H;
            Z = rect.Z;
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
        [XmlAttribute(AttributeName = "Enabled")] public bool Enabled;
        public float Height;
        public float Space;

        //Needed for serialization
        public bool HeightSpecified
        {
            get { return Enabled; }
        }
        public bool SpaceSpecified
        {
            get { return Enabled; }
        }

        public SReflection(bool enabled, float height, float space)
        {
            Enabled = enabled;
            Height = height;
            Space = space;
        }

        public SReflection(SReflection refl)
        {
            Enabled = refl.Enabled;
            Height = refl.Height;
            Space = refl.Space;
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