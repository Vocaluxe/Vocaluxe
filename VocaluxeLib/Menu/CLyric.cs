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

namespace VocaluxeLib.Menu
{
    [XmlType("Lyric")]
    public struct SThemeLyrics
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;

        public SRectF Rect;

        public SThemeColor Color;
        public SThemeColor ProcessedColor;
    }

    public class CLyric : CMenuElementBase, IMenuElement, IThemeable
    {
        private readonly int _PartyModeID;
        private SThemeLyrics _Theme;

        /// <summary>
        ///     Holds a reference to Songline. DO NOT MODIFY
        /// </summary>
        private CSongLine _Line;
        private CText _Text;

        private float _Width;

        private float _Alpha = 1f;

        private float _CurrentBeat = -1;

        public bool Selectable
        {
            get { return false; }
        }

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded { get; private set; }

        private SColorF _Color;
        private SColorF _ColorProcessed;

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
            ThemeLoaded = false;
            _Color = new SColorF();
            _ColorProcessed = new SColorF();

            _Line = new CSongLine();
            _Text = new CText(_PartyModeID);

            LyricStyle = ELyricStyle.TR_CONFIG_LYRICSTYLE_FILL;
        }

        public CLyric(SThemeLyrics theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;

            _Line = new CSongLine();
            _Text = new CText(_PartyModeID);
            _Width = 1f;

            LyricStyle = ELyricStyle.TR_CONFIG_LYRICSTYLE_FILL;

            ThemeLoaded = true;
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
            return X - _Width / 2;
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
                    case ELyricStyle.TR_CONFIG_LYRICSTYLE_FILL:
                        _DrawFill();
                        break;
                    case ELyricStyle.TR_CONFIG_LYRICSTYLE_SLIDE:
                        _DrawSlide();
                        break;
                    case ELyricStyle.TR_CONFIG_LYRICSTYLE_ZOOM:
                    case ELyricStyle.TR_CONFIG_LYRICSTYLE_JUMP:
                        _DrawZoomOrJump();
                        break;
                    default:
                        _DrawSlide();
                        break;
                }
            }
        }

        private void _DrawSlide()
        {
            float x = X - _Width / 2;

            foreach (CSongNote note in _Line.Notes)
            {
                _Text.X = x;
                _SetText(note);

                if (_CurrentBeat >= note.StartBeat)
                {
                    if (_CurrentBeat <= note.EndBeat)
                    {
                        _Text.Color = _ColorProcessed;

                        int diff = note.Duration;
                        if (diff <= 0)
                            _Text.Draw(0f, 1f);
                        else
                        {
                            float p = (_CurrentBeat - note.StartBeat) / diff;
                            _Text.Draw(0f, p);
                            _Text.Color = _Color;
                            _Text.Draw(p, 1f);
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

        private void _DrawFill()
        {
            float x = X - _Width / 2; // most left position

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

        private void _DrawZoomedNote(CBaseNote zoomNote, int endBeat)
        {
            float diff = endBeat - zoomNote.StartBeat;
            if (diff <= 0f)
                diff = 1f;

            float p = 1f - (_CurrentBeat - zoomNote.StartBeat) / diff;
            if (p < 0)
                p = 0;

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

        private void _DrawJumpingNode(CBaseNote jumpNote)
        {
            _Text.Color = _ColorProcessed;

            int diff = jumpNote.Duration;
            if (diff <= 0)
                diff = 1;

            float p = 1f - (_CurrentBeat - jumpNote.StartBeat) / diff;

            if (p < 0.001)
                _Text.Draw();
            else
            {
                float y = _Text.Y;
                _Text.Y -= _Text.Font.Height * 0.1f * p;
                _Text.Draw();
                _Text.Y = y;
            }
        }

        private void _DrawZoomOrJump()
        {
            float x = X - _Width / 2; // most left position

            int lastNote = _Line.FindPreviousNote((int)_CurrentBeat);
            CSongNote highlightNote = null;
            int hEndBeat = 0;
            float hX = 0;

            for (int note = 0; note < _Line.Notes.Length; note++)
            {
                _Text.X = x;
                CSongNote curNote = _Line.Notes[note];
                _SetText(curNote);

                if (_CurrentBeat >= curNote.StartBeat)
                {
                    int curEndBeat = curNote.EndBeat;
                    if (note < _Line.Notes.Length - 1)
                        curEndBeat = _Line.Notes[note + 1].StartBeat - 1;

                    if (_CurrentBeat <= curEndBeat)
                    {
                        highlightNote = curNote;
                        hEndBeat = curEndBeat;
                        hX = x;
                    }
                    else
                    {
                        // already passed
                        _Text.Color = lastNote == note ? _ColorProcessed : _Color;
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

            // Draw the highlighted note after all others because we want it to be above those! (transparency won't work well otherwhise)
            if (highlightNote == null)
                return;
            _SetText(highlightNote);
            _Text.X = hX;
            if (LyricStyle == ELyricStyle.TR_CONFIG_LYRICSTYLE_JUMP)
                _DrawJumpingNode(highlightNote);
            else
                _DrawZoomedNote(highlightNote, hEndBeat);
        }
        #endregion draw

        public void UnloadSkin() {}

        public void LoadSkin()
        {
            _Theme.Color.Get(_PartyModeID, out _Color);
            _Theme.ProcessedColor.Get(_PartyModeID, out _ColorProcessed);

            MaxRect = _Theme.Rect;
            _Text = new CText(X, Y, Z, H, W, EAlignment.Left, EStyle.Bold, "Normal", _Color, String.Empty);
        }

        public void ReloadSkin()
        {
            UnloadSkin();
            LoadSkin();
        }

        public object GetTheme()
        {
            return _Theme;
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            X += stepX;
            Y += stepY;
            _Theme.Rect.X += stepX;
            _Theme.Rect.Y += stepY;
        }

        public void ResizeElement(int stepW, int stepH)
        {
            W += stepW;
            if (W <= 0)
                W = 1;

            H += stepH;
            if (H <= 0)
                H = 1;

            _Theme.Rect.W = Rect.W;
            _Theme.Rect.H = Rect.H;
        }
        #endregion ThemeEdit
    }
}