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
using System.Xml.Serialization;
using VocaluxeLib.Songs;
using VocaluxeLib.Xml;

namespace VocaluxeLib.Menu
{
    [XmlType("Lyric")]
    public struct SThemeLyrics
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;

        public SRectF Rect;

        public SThemeColor Color;
        public SThemeColor SColor;
    }

    public class CLyric : IMenuElement
    {
        private readonly int _PartyModeID;
        private SThemeLyrics _Theme;
        private bool _ThemeLoaded;

        /// <summary>
        ///     Holds a reference to Songline. DO NOT MODIFY
        /// </summary>
        private CSongLine _Line;
        private CText _Text;

        private float _X;
        private float _Y;
        private float _MaxW;
        private float _Z;
        private float _H;

        private float _Width;

        private float _Alpha = 1f;

        private float _CurrentBeat = -1;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded
        {
            get { return _ThemeLoaded; }
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
            _Line = new CSongLine();
            _Text = new CText(_PartyModeID);

            LyricStyle = ELyricStyle.Fill;
        }

        public CLyric(SThemeLyrics theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;

            _Line = new CSongLine();
            _Text = new CText(_PartyModeID);
            _Width = 1f;

            LyricStyle = ELyricStyle.Fill;

            LoadSkin();
        }

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref _Theme.Rect.X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref _Theme.Rect.Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref _Theme.Rect.Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/W", ref _Theme.Rect.W);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref _Theme.Rect.H);

            if (xmlReader.GetValue(item + "/Color", out _Theme.Color.Name, String.Empty))
                _ThemeLoaded &= _Theme.Color.Get(_PartyModeID, out _Color);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref _Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref _Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref _Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref _Color.A);
            }

            if (xmlReader.GetValue(item + "/SColor", out _Theme.SColor.Name, String.Empty))
                _ThemeLoaded &= _Theme.SColor.Get(_PartyModeID, out _ColorProcessed);
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
                _Theme.Color.Color = _Color;
                _Theme.SColor.Color = _ColorProcessed;
                LoadSkin();
                _Text = new CText(_X, _Y, _Z, _H, _MaxW, EAlignment.Left, EStyle.Bold, "Normal", _Color, String.Empty);
            }
            return _ThemeLoaded;
        }

        public void SetLine(CSongLine line)
        {
            _Line = line;
            _Width = 0f;
            foreach (CSongNote note in line.Notes)
            {
                _SetText(note);
                _Width += _Text.Rect.W;
            }
        }

        private void _SetText(CSongNote note)
        {
            _Text.Text = note.Text;
            _Text.Font.Style = (note.Type == ENoteType.Freestyle) ? EStyle.BoldItalic : EStyle.Bold;
        }

        public void Clear()
        {
            _Line = new CSongLine();
        }

        public float GetCurrentLyricPosX()
        {
            return _X - _Width / 2;
        }

        public void Update(float currentBeat)
        {
            _CurrentBeat = currentBeat;
        }

        #region draw
        public void Draw()
        {
            if (Visible || CBase.Settings.GetProgramState() == EProgramState.EditTheme)
            {
                switch (LyricStyle)
                {
                    case ELyricStyle.Fill:
                        _DrawFill();
                        break;
                    case ELyricStyle.Jump:
                        _DrawJump();
                        break;
                    case ELyricStyle.Slide:
                        _DrawSlide();
                        break;
                    case ELyricStyle.Zoom:
                        _DrawZoom();
                        break;
                    default:
                        _DrawSlide();
                        break;
                }
            }
        }

        private void _DrawSlide()
        {
            float x = _X - _Width / 2;

            foreach (CSongNote note in _Line.Notes)
            {
                _Text.X = x;
                _SetText(note);

                if (_CurrentBeat >= note.StartBeat)
                {
                    if (_CurrentBeat <= note.EndBeat)
                    {
                        _Text.Color = _ColorProcessed;

                        int diff = note.EndBeat - note.StartBeat;
                        if (diff <= 0)
                            _Text.Draw(0f, 1f);
                        else
                        {
                            _Text.Draw(0f, (_CurrentBeat - note.StartBeat) / diff);
                            _Text.Color = _Color;
                            _Text.Draw((_CurrentBeat - note.StartBeat) / diff, 1f);
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

                x += _Text.Rect.W;
            }
        }

        private void _DrawZoom()
        {
            float x = _X - _Width / 2; // most left position

            //find last active note
            int lastNote = _Line.FindPreviousNote((int)_CurrentBeat);

            int zoomNote = -1;
            int endBeat = -1;
            float zoomx = 0f;

            for (int note = 0; note < _Line.Notes.Length; note++)
            {
                _Text.X = x;
                _SetText(_Line.Notes[note]);

                if (_CurrentBeat >= _Line.Notes[note].StartBeat)
                {
                    int curEndBeat = _Line.Notes[note].EndBeat;
                    if (note < _Line.Notes.Length - 1)
                        curEndBeat = _Line.Notes[note + 1].StartBeat - 1;

                    if (_CurrentBeat <= curEndBeat)
                    {
                        zoomNote = note;
                        endBeat = curEndBeat;
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

                x += _Text.Rect.W;
            }

            if (zoomNote > -1)
            {
                _Text.X = zoomx;
                _SetText(_Line.Notes[zoomNote]);

                float diff = endBeat - _Line.Notes[zoomNote].StartBeat;
                if (diff <= 0f)
                    diff = 1f;

                float p = (_CurrentBeat - _Line.Notes[zoomNote].StartBeat) / diff;
                if (p > 1f)
                    p = 1f;

                p = 1f - p;

                float ty = _Text.Y;
                float th = _Text.Font.Height;
                float tz = _Text.Z;

                SRectF normalRect = _Text.Rect;
                _Text.Font.Height *= 1f + p * 0.4f;
                _Text.X -= (_Text.Rect.W - normalRect.W) / 2f;
                _Text.Y -= (_Text.Rect.H - normalRect.H) / 2f;
                _Text.Z -= 0.1f;
                _Text.Color = _ColorProcessed;


                _Text.Draw();

                _Text.Y = ty;
                _Text.Font.Height = th;
                _Text.Z = tz;
            }
        }

        private void _DrawFill()
        {
            float x = _X - _Width / 2; // most left position

            foreach (CSongNote note in _Line.Notes)
            {
                _Text.X = x;
                _SetText(note);

                if (_CurrentBeat >= note.StartBeat)
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

                x += _Text.Rect.W;
            }
        }

        private void _DrawJump()
        {
            float x = _X - _Width / 2; // most left position

            //find last active note
            int lastNote = _Line.FindPreviousNote((int)_CurrentBeat);

            int jumpNote = -1;
            float jumpx = 0f;

            for (int note = 0; note < _Line.Notes.Length; note++)
            {
                _Text.X = x;
                _SetText(_Line.Notes[note]);

                if (_CurrentBeat >= _Line.Notes[note].StartBeat)
                {
                    int curEndBeat = _Line.Notes[note].EndBeat;
                    if (note < _Line.Notes.Length - 1)
                        curEndBeat = _Line.Notes[note + 1].StartBeat - 1;

                    if (_CurrentBeat <= curEndBeat)
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

                x += _Text.Rect.W;
            }

            if (jumpNote < 0)
                return;

            _Text.X = jumpx;
            _Text.Color = _ColorProcessed;
            _SetText(_Line.Notes[jumpNote]);

            int diff = _Line.Notes[jumpNote].EndBeat - _Line.Notes[jumpNote].StartBeat;
            if (diff <= 0)
                diff = 1;

            float p = 1f - (_CurrentBeat - _Line.Notes[jumpNote].StartBeat) / diff;

            if (Math.Abs(p) < float.Epsilon)
                _Text.Draw();
            else
            {
                float y = _Text.Y;
                _Text.Y -= _Text.Font.Height * 0.1f * p;
                _Text.Draw();
                _Text.Y = y;
            }
        }
        #endregion draw

        public void UnloadSkin() {}

        public void LoadSkin()
        {
            _Theme.Color.Get(_PartyModeID, out _Color);
            _Theme.SColor.Get(_PartyModeID, out _ColorProcessed);

            Rect = _Theme.Rect;
            _Text = new CText(_X, _Y, _Z, _H, _MaxW, EAlignment.Left, EStyle.Bold, "Normal", _Color, String.Empty);
        }

        public void ReloadSkin()
        {
            UnloadSkin();
            LoadSkin();
        }

        public SThemeLyrics GetTheme()
        {
            return _Theme;
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            SRectF rect = Rect;
            rect.X += stepX;
            rect.Y += stepY;
            Rect = rect;
            _Theme.Rect = rect;
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
            _Theme.Rect = rect;
        }
        #endregion ThemeEdit
    }
}