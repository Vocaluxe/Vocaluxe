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
using System.Xml;
using VocaluxeLib.Menu.SingNotes;

namespace VocaluxeLib.Menu
{
    struct SNote
    {
        public string Text;
        public int StartBeat;
        public int EndBeat;
        public int Duration;
        public ENoteType Type;
    }

    struct SThemeLyrics
    {
        public string Name;

        public string ColorName;
        public string SelColorName;
    }

    public class CLyric : IMenuElement
    {
        private readonly int _PartyModeID;
        private SThemeLyrics _Theme;
        private bool _ThemeLoaded;

        private readonly List<SNote> _Notes;
        private CText _Text;

        private float _X;
        private float _Y;
        private float _MaxW;
        private float _Z;
        private float _H;

        private float _Width;

        private float _Alpha = 1f;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public SRectF Rect
        {
            get { return new SRectF(_X, _Y, _MaxW, _H, _Z); }
            private set
            {
                _X = value.X;
                _Y = value.Y;
                _MaxW = value.W;
                _H = value.H;
                _Z = value.Z;
            }
        }

        private SColorF _Color;
        private SColorF _ColorProcessed;

        public bool Selected;
        public bool Visible = true;

        public float Alpha
        {
            get { return _Alpha; }
            set
            {
                _Alpha = value;
                _Text.Alpha = _Alpha;
            }
        }

        public ELyricStyle LyricStyle { get; set; }

        public CLyric(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = new SThemeLyrics();
            _ThemeLoaded = false;
            _Color = new SColorF();
            _ColorProcessed = new SColorF();

            _X = 0f;
            _Y = 0f;
            _Z = 0f;
            _MaxW = 1f;
            _H = 1f;
            _Width = 1f;
            _Notes = new List<SNote>();
            _Text = new CText(_PartyModeID);

            LyricStyle = ELyricStyle.Fill;
        }

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader, int skinIndex)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref _X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref _Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref _Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/W", ref _MaxW);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref _H);

            if (xmlReader.GetValue(item + "/Color", out _Theme.ColorName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorName, skinIndex, out _Color);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref _Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref _Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref _Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref _Color.A);
            }

            if (xmlReader.GetValue(item + "/SColor", out _Theme.SelColorName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.SelColorName, skinIndex, out _ColorProcessed);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SR", ref _ColorProcessed.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SG", ref _ColorProcessed.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SB", ref _ColorProcessed.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SA", ref _ColorProcessed.A);
            }

            if (_ThemeLoaded)
            {
                _Theme.Name = elementName;
                LoadTextures();
                _Text = new CText(_X, _Y, _Z, _H, _MaxW, EAlignment.Left, EStyle.Bold, "Normal", _Color, String.Empty);
            }
            return _ThemeLoaded;
        }

        public bool SaveTheme(XmlWriter writer)
        {
            if (_ThemeLoaded)
            {
                writer.WriteStartElement(_Theme.Name);

                writer.WriteComment("<X>, <Y>, <Z>, <W>, <H>: Lyric position, width and height");
                writer.WriteElementString("X", _X.ToString("#0"));
                writer.WriteElementString("Y", _Y.ToString("#0"));
                writer.WriteElementString("Z", _Z.ToString("#0.00"));
                writer.WriteElementString("W", _MaxW.ToString("#0"));
                writer.WriteElementString("H", _H.ToString("#0"));

                writer.WriteComment("<Color>: Lyric text color from ColorScheme (high priority)");
                writer.WriteComment("or <R>, <G>, <B>, <A> (lower priority)");
                if (_Theme.ColorName != "")
                    writer.WriteElementString("Color", _Theme.ColorName);
                else
                {
                    writer.WriteElementString("R", _Color.R.ToString("#0.00"));
                    writer.WriteElementString("G", _Color.G.ToString("#0.00"));
                    writer.WriteElementString("B", _Color.B.ToString("#0.00"));
                    writer.WriteElementString("A", _Color.A.ToString("#0.00"));
                }

                writer.WriteComment("<SColor>: Highlighted lyric color from ColorScheme (high priority)");
                writer.WriteComment("or <SR>, <SG>, <SB>, <SA> (lower priority)");
                if (_Theme.SelColorName != "")
                    writer.WriteElementString("SColor", _Theme.SelColorName);
                else
                {
                    writer.WriteElementString("SR", _ColorProcessed.R.ToString("#0.00"));
                    writer.WriteElementString("SG", _ColorProcessed.G.ToString("#0.00"));
                    writer.WriteElementString("SB", _ColorProcessed.B.ToString("#0.00"));
                    writer.WriteElementString("SA", _ColorProcessed.A.ToString("#0.00"));
                }

                writer.WriteEndElement();

                return true;
            }
            return false;
        }

        public void SetLine(CLine line)
        {
            _Notes.Clear();

            _Width = 0f;
            foreach (CNote note in line.Notes)
            {
                SNote n = new SNote {Text = note.Text, StartBeat = note.StartBeat, EndBeat = note.EndBeat, Duration = note.Duration, Type = note.NoteType};

                _Text.Text = note.Text;
                _Text.Style = EStyle.Bold;

                if (n.Type == ENoteType.Freestyle)
                    _Text.Style = EStyle.BoldItalic;

                RectangleF rect = CBase.Drawing.GetTextBounds(_Text);
                _Width += rect.Width;
                _Notes.Add(n);
            }
        }

        public void Clear()
        {
            _Notes.Clear();
        }

        public float GetCurrentLyricPosX()
        {
            return _X - _Width / 2;
        }

        #region draw
        public void Draw(float actualBeat)
        {
            if (Visible || CBase.Settings.GetGameState() == EGameState.EditTheme)
            {
                switch (LyricStyle)
                {
                    case ELyricStyle.Fill:
                        _DrawFill(actualBeat);
                        break;
                    case ELyricStyle.Jump:
                        _DrawJump(actualBeat);
                        break;
                    case ELyricStyle.Slide:
                        _DrawSlide(actualBeat);
                        break;
                    case ELyricStyle.Zoom:
                        _DrawZoom(actualBeat);
                        break;
                    default:
                        _DrawSlide(actualBeat);
                        break;
                }
            }
        }

        private void _DrawSlide(float currentBeat)
        {
            float x = _X - _Width / 2;

            foreach (SNote note in _Notes)
            {
                _Text.X = x;
                _Text.Style = EStyle.Bold;
                _Text.Text = note.Text;
                RectangleF rect = CBase.Drawing.GetTextBounds(_Text);

                if (note.Type == ENoteType.Freestyle)
                    _Text.Style = EStyle.BoldItalic;

                if (currentBeat >= note.StartBeat)
                {
                    if (currentBeat <= note.EndBeat)
                    {
                        _Text.Color = _ColorProcessed;


                        float diff = note.EndBeat - note.StartBeat;
                        if (Math.Abs(diff) < float.Epsilon)
                            _Text.Draw(0f, 1f);
                        else
                        {
                            _Text.Draw(0f, (currentBeat - note.StartBeat) / diff);
                            _Text.Color = _Color;
                            _Text.Draw((currentBeat - note.StartBeat) / diff, 1f);
                        }
                    }
                    else
                    {
                        _Text.Color = _ColorProcessed;
                        _Text.Draw();
                    }
                }
                else
                {
                    _Text.Color = _Color;
                    _Text.Draw();
                }

                x += rect.Width;
            }
        }

        private void _DrawZoom(float currentBeat)
        {
            float x = _X - _Width / 2; // most left position

            //find last active note
            int lastNote = -1;
            for (int i = 0; i < _Notes.Count; i++)
            {
                if (currentBeat >= _Notes[i].StartBeat)
                    lastNote = i;
            }

            int zoomNote = -1;
            int endBeat = -1;
            float zoomx = 0f;

            for (int note = 0; note < _Notes.Count; note++)
            {
                _Text.X = x;
                _Text.Style = EStyle.Bold;
                _Text.Text = _Notes[note].Text;
                RectangleF rect = CBase.Drawing.GetTextBounds(_Text);

                if (_Notes[note].Type == ENoteType.Freestyle)
                    _Text.Style = EStyle.BoldItalic;

                if (currentBeat >= _Notes[note].StartBeat)
                {
                    int endbeat = _Notes[note].EndBeat;
                    if (note < _Notes.Count - 1)
                        endbeat = _Notes[note + 1].StartBeat - 1;

                    if (currentBeat <= endbeat)
                    {
                        zoomNote = note;
                        endBeat = endbeat;
                        zoomx = _Text.X;
                    }
                    else
                    {
                        // already passed
                        _Text.Color = note == lastNote ? _ColorProcessed : _Color;

                        _Text.Draw();
                    }
                }
                else
                {
                    // not passed
                    _Text.Color = _Color;
                    _Text.Draw();
                }

                x += rect.Width;
            }

            if (zoomNote > -1)
            {
                if (_Notes[zoomNote].Duration == 0)
                    return;

                _Text.X = zoomx;
                _Text.Text = _Notes[zoomNote].Text;
                _Text.Color = _ColorProcessed;
                _Text.Style = EStyle.Bold;

                if (_Notes[zoomNote].Type == ENoteType.Freestyle)
                    _Text.Style = EStyle.BoldItalic;

                RectangleF rect = CBase.Drawing.GetTextBounds(_Text);

                float diff = endBeat - _Notes[zoomNote].StartBeat;
                if (diff <= 0f)
                    diff = 1f;

                float p = (currentBeat - _Notes[zoomNote].StartBeat) / diff;
                if (p > 1f)
                    p = 1f;

                p = 1f - p;

                float ty = _Text.Y;
                float tx = _Text.X;
                float th = _Text.Height;
                float tz = _Text.Z;

                _Text.Height += _Text.Height * p * 0.4f;
                RectangleF rectz = CBase.Drawing.GetTextBounds(_Text);
                _Text.X -= (rectz.Width - rect.Width) / 2f;
                _Text.Y -= (rectz.Height - rect.Height) / 2f;
                _Text.Z -= 0.1f;

                _Text.Draw();

                _Text.Y = ty;
                _Text.X = tx;
                _Text.Height = th;
                _Text.Z = tz;
            }
        }

        private void _DrawFill(float currentBeat)
        {
            float x = _X - _Width / 2; // most left position

            foreach (SNote note in _Notes)
            {
                _Text.X = x;
                _Text.Style = EStyle.Bold;
                _Text.Text = note.Text;
                RectangleF rect = CBase.Drawing.GetTextBounds(_Text);

                if (note.Type == ENoteType.Freestyle)
                    _Text.Style = EStyle.BoldItalic;

                if (currentBeat >= note.StartBeat)
                {
                    _Text.Color = _ColorProcessed;
                    _Text.Draw();
                }
                else
                {
                    // not passed
                    _Text.Color = _Color;
                    _Text.Draw();
                }

                x += rect.Width;
            }
        }

        private void _DrawJump(float currentBeat)
        {
            float x = _X - _Width / 2; // most left position

            //find last active note
            int lastNote = -1;
            for (int i = 0; i < _Notes.Count; i++)
            {
                if (currentBeat >= _Notes[i].StartBeat)
                    lastNote = i;
            }

            int jumpNote = -1;
            float jumpx = 0f;

            for (int note = 0; note < _Notes.Count; note++)
            {
                _Text.X = x;
                _Text.Style = EStyle.Bold;
                _Text.Text = _Notes[note].Text;
                RectangleF rect = CBase.Drawing.GetTextBounds(_Text);

                if (_Notes[note].Type == ENoteType.Freestyle)
                    _Text.Style = EStyle.BoldItalic;

                if (currentBeat >= _Notes[note].StartBeat)
                {
                    if (currentBeat <= _Notes[note].EndBeat)
                    {
                        jumpNote = note;
                        jumpx = _Text.X;
                    }
                    else
                    {
                        // already passed
                        _Text.Color = note == lastNote ? _ColorProcessed : _Color;

                        _Text.Draw();
                    }
                }
                else
                {
                    // not passed
                    _Text.Color = _Color;
                    _Text.Draw();
                }

                x += rect.Width;
            }

            if (jumpNote < 0)
                return;
            if (_Notes[jumpNote].Duration == 0)
                return;

            _Text.X = jumpx;
            _Text.Text = _Notes[jumpNote].Text;
            _Text.Color = _ColorProcessed;
            _Text.Style = EStyle.Bold;

            if (_Notes[jumpNote].Type == ENoteType.Freestyle)
                _Text.Style = EStyle.BoldItalic;

            int diff = _Notes[jumpNote].EndBeat - _Notes[jumpNote].StartBeat;
            if (diff <= 0)
                diff = 1;

            float p = 1f - (currentBeat - _Notes[jumpNote].StartBeat) / diff;

            if (Math.Abs(p) < float.Epsilon)
                _Text.Draw();
            else
            {
                float y = _Text.Y;
                _Text.Y -= _Text.Height * 0.1f * p;
                _Text.Draw();
                _Text.Y = y;
            }
        }
        #endregion draw

        public void UnloadTextures() {}

        public void LoadTextures()
        {
            if (_Theme.ColorName != "")
                _Color = CBase.Theme.GetColor(_Theme.ColorName, _PartyModeID);

            if (_Theme.SelColorName != "")
                _ColorProcessed = CBase.Theme.GetColor(_Theme.SelColorName, _PartyModeID);
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            SRectF rect = Rect;
            rect.X += stepX;
            rect.Y += stepY;
            Rect = rect;
        }

        public void ResizeElement(int stepW, int stepH)
        {
            SRectF rect = Rect;
            rect.W += stepW;
            if (rect.W <= 0)
                rect.W = 1;

            rect.H += stepH;
            if (rect.H <= 0)
                rect.H = 1;

            Rect = rect;
        }
        #endregion ThemeEdit
    }
}