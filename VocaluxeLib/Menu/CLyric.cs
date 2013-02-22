using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Xml;

using Vocaluxe.Menu.SingNotes;

namespace Vocaluxe.Menu
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
        public string SColorName;
    }

    public class CLyric : IMenuElement
    {
        private int _PartyModeID;
        private SThemeLyrics _Theme;
        private bool _ThemeLoaded;

        private List<SNote> _Notes;
        private CText _Text;

        private float _X;
        private float _Y;
        private float _MaxW;
        private float _Z;
        private float _H;

        private float _width;
        private ELyricStyle _Style;

        private float _Alpha = 1f;

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public SRectF Rect
        {
            get { return new SRectF(_X, _Y, _MaxW, _H, _Z); }
            set
            {
                _X = value.X;
                _Y = value.Y;
                _MaxW = value.W;
                _H = value.H;
                _Z = value.Z;
            }
        }

        public SColorF Color;
        public SColorF ColorProcessed;

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

        public ELyricStyle LyricStyle
        {
            get { return _Style; }
            set { _Style = value; }
        }

        public CLyric(int PartyModeID)
        {
            _PartyModeID = PartyModeID;
            _Theme = new SThemeLyrics();
            _ThemeLoaded = false;
            Color = new SColorF();
            ColorProcessed = new SColorF();

            _X = 0f;
            _Y = 0f;
            _Z = 0f;
            _MaxW = 1f;
            _H = 1f;
            _width = 1f;
            _Notes = new List<SNote>();
            _Text = new CText(_PartyModeID);

            _Style = ELyricStyle.Fill;
        }

        public bool LoadTheme(string XmlPath, string ElementName, CXMLReader xmlReader, int SkinIndex)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/X", ref _X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Y", ref _Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/Z", ref _Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/W", ref _MaxW);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/H", ref _H);

            if (xmlReader.GetValue(item + "/Color", ref _Theme.ColorName, String.Empty))
            {
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorName, SkinIndex, ref Color);
            }
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref Color.A);
            }

            if (xmlReader.GetValue(item + "/SColor", ref _Theme.SColorName, String.Empty))
            {
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.SColorName, SkinIndex, ref ColorProcessed);
            }
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SR", ref ColorProcessed.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SG", ref ColorProcessed.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SB", ref ColorProcessed.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SA", ref ColorProcessed.A);
            }

            if (_ThemeLoaded)
            {
                _Theme.Name = ElementName;
                LoadTextures();
                _Text = new CText(_X, _Y, _Z, _H, _MaxW, EAlignment.Left, EStyle.Bold, "Normal", Color, String.Empty);
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
                if (_Theme.ColorName != String.Empty)
                {
                    writer.WriteElementString("Color", _Theme.ColorName);
                }
                else
                {
                    writer.WriteElementString("R", Color.R.ToString("#0.00"));
                    writer.WriteElementString("G", Color.G.ToString("#0.00"));
                    writer.WriteElementString("B", Color.B.ToString("#0.00"));
                    writer.WriteElementString("A", Color.A.ToString("#0.00"));
                }

                writer.WriteComment("<SColor>: Highlighted lyric color from ColorScheme (high priority)");
                writer.WriteComment("or <SR>, <SG>, <SB>, <SA> (lower priority)");
                if (_Theme.SColorName != String.Empty)
                {
                    writer.WriteElementString("SColor", _Theme.SColorName);
                }
                else
                {
                    writer.WriteElementString("SR", ColorProcessed.R.ToString("#0.00"));
                    writer.WriteElementString("SG", ColorProcessed.G.ToString("#0.00"));
                    writer.WriteElementString("SB", ColorProcessed.B.ToString("#0.00"));
                    writer.WriteElementString("SA", ColorProcessed.A.ToString("#0.00"));
                }

                writer.WriteEndElement();

                return true;
            }
            return false;
        }

        public void SetLine(CLine Line)
        {
            _Notes.Clear();

            _width = 0f;
            foreach (CNote note in Line.Notes)
            {
                SNote n = new SNote();

                n.Text = note.Text;
                n.StartBeat = note.StartBeat;
                n.EndBeat = note.EndBeat;
                n.Duration = note.Duration;
                n.Type = note.NoteType;

                _Text.Text = note.Text;
                _Text.Style = EStyle.Bold;

                if (n.Type == ENoteType.Freestyle)
                    _Text.Style = EStyle.BoldItalic;
                
                RectangleF rect = CBase.Drawing.GetTextBounds(_Text);
                _width += rect.Width;
                _Notes.Add(n);
            }
        }

        public void Clear()
        {
            _Notes.Clear();
        }

        public float GetCurrentLyricPosX()
        {
            return _X - _width / 2;  
        }

        #region draw
        public void Draw(float ActualBeat)
        {
            if (Visible || CBase.Settings.GetGameState() == EGameState.EditTheme)
            {
                switch (_Style)
                {
                    case ELyricStyle.Fill:
                        DrawFill(ActualBeat);
                        break;
                    case ELyricStyle.Jump:
                        DrawJump(ActualBeat);
                        break;
                    case ELyricStyle.Slide:
                        DrawSlide(ActualBeat);
                        break;
                    case ELyricStyle.Zoom:
                        DrawZoom(ActualBeat);
                        break;
                    default:
                        DrawSlide(ActualBeat);
                        break;
                }            
            }
        }

        private void DrawSlide(float CurrentBeat)
        {
            float x = _X - _width / 2;
            
            foreach (SNote note in _Notes)
            {
                _Text.X = x;
                _Text.Style = EStyle.Bold;
                _Text.Text = note.Text;
                RectangleF rect = CBase.Drawing.GetTextBounds(_Text);

                if (note.Type == ENoteType.Freestyle)
                    _Text.Style = EStyle.BoldItalic;

                if (CurrentBeat >= note.StartBeat)
                {
                    if (CurrentBeat <= note.EndBeat)
                    {
                        _Text.Color = ColorProcessed;
                       

                        float diff = note.EndBeat - note.StartBeat;
                        if (diff == 0)
                            _Text.Draw(0f, 1f);
                        else
                        {
                            _Text.Draw(0f, (CurrentBeat - note.StartBeat) / diff);
                            _Text.Color = Color;
                            _Text.Draw((CurrentBeat - note.StartBeat) / diff, 1f);
                        }
                    }
                    else
                    {
                        _Text.Color = ColorProcessed;                 
                        _Text.Draw();
                    }

                }
                else
                {
                    _Text.Color = Color;                   
                    _Text.Draw();
                }
                
                x += rect.Width;
            }
        }

        private void DrawZoom(float CurrentBeat)
        {
            float x = _X - _width / 2; // most left position

            //find last active note
            int last_note = -1;
            for (int i = 0; i < _Notes.Count; i++)
            {
                if (CurrentBeat >= _Notes[i].StartBeat)
                    last_note = i;
            }

            int zoom_note = -1;
            int end_beat = -1;
            float zoomx = 0f;

            for (int note = 0; note < _Notes.Count; note++)
            {
                _Text.X = x;
                _Text.Style = EStyle.Bold;
                _Text.Text = _Notes[note].Text;
                RectangleF rect = CBase.Drawing.GetTextBounds(_Text);

                if (_Notes[note].Type == ENoteType.Freestyle)
                    _Text.Style = EStyle.BoldItalic;

                if (CurrentBeat >= _Notes[note].StartBeat)
                {
                    bool last = note == _Notes.Count -1;
                    int endbeat = _Notes[note].EndBeat;
                    if (note < _Notes.Count -1)
                        endbeat = _Notes[note + 1].StartBeat - 1;

                    if (CurrentBeat <= endbeat)
                    {
                        zoom_note = note;
                        end_beat = endbeat;
                        zoomx = _Text.X;
                    }
                    else
                    {
                        // already passed
                        if (note == last_note)
                            _Text.Color = ColorProcessed;
                        else
                            _Text.Color = Color;

                        _Text.Draw();
                    }

                }
                else
                {
                    // not passed
                    _Text.Color = Color;
                    _Text.Draw();
                }
                
                x += rect.Width;
            }

            if (zoom_note > -1)
            {
                if (_Notes[zoom_note].Duration == 0)
                    return;

                _Text.X = zoomx;
                _Text.Text = _Notes[zoom_note].Text;
                _Text.Color = ColorProcessed;
                _Text.Style = EStyle.Bold;

                if (_Notes[zoom_note].Type == ENoteType.Freestyle)
                    _Text.Style = EStyle.BoldItalic;

                RectangleF rect = CBase.Drawing.GetTextBounds(_Text);

                float diff = end_beat - _Notes[zoom_note].StartBeat;
                if (diff <= 0f)
                    diff = 1f;

                float p = (CurrentBeat - _Notes[zoom_note].StartBeat) / diff;
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

        private void DrawFill(float CurrentBeat)
        {
            float x = _X - _width / 2; // most left position

            foreach (SNote note in _Notes)
            {
                _Text.X = x;
                _Text.Style = EStyle.Bold;
                _Text.Text = note.Text;
                RectangleF rect = CBase.Drawing.GetTextBounds(_Text);

                if (note.Type == ENoteType.Freestyle)
                    _Text.Style = EStyle.BoldItalic;

                if (CurrentBeat >= note.StartBeat)
                {
                    _Text.Color = ColorProcessed;                
                    _Text.Draw();
                }
                else
                {
                    // not passed
                    _Text.Color = Color;                    
                    _Text.Draw();
                }
                
                x += rect.Width;
            }
        }

        private void DrawJump(float CurrentBeat)
        {
            float x = _X - _width / 2; // most left position

            //find last active note
            int last_note = -1;
            for (int i = 0; i < _Notes.Count; i++)
            {
                if (CurrentBeat >= _Notes[i].StartBeat)
                    last_note = i;
            }

            int jump_note = -1;
            int end_beat = -1;
            float jumpx = 0f;

            for (int note = 0; note < _Notes.Count; note++)
            {
                _Text.X = x;
                _Text.Style = EStyle.Bold;
                _Text.Text = _Notes[note].Text;
                RectangleF rect = CBase.Drawing.GetTextBounds(_Text);

                if (_Notes[note].Type == ENoteType.Freestyle)
                    _Text.Style = EStyle.BoldItalic;

                if (CurrentBeat >= _Notes[note].StartBeat)
                {
                    bool last = note == _Notes.Count - 1;
                    int endbeat = _Notes[note].EndBeat;
                    if (note < _Notes.Count - 1)
                        endbeat = _Notes[note + 1].StartBeat - 1;

                    if (CurrentBeat <= _Notes[note].EndBeat)
                    {
                        jump_note = note;
                        end_beat = endbeat;
                        jumpx = _Text.X;
                    }
                    else
                    {
                        // already passed
                        if (note == last_note)
                            _Text.Color = ColorProcessed;
                        else
                            _Text.Color = Color;

                        _Text.Draw();
                    }

                }
                else
                {
                    // not passed
                    _Text.Color = Color;                   
                    _Text.Draw();
                }
                
                x += rect.Width;
            }

            if (jump_note > -1)
            {
                if (_Notes[jump_note].Duration == 0)
                    return;

                _Text.X = jumpx;
                _Text.Text = _Notes[jump_note].Text;
                _Text.Color = ColorProcessed;
                _Text.Style = EStyle.Bold;

                if (_Notes[jump_note].Type == ENoteType.Freestyle)
                    _Text.Style = EStyle.BoldItalic;

                float diff = _Notes[jump_note].EndBeat - _Notes[jump_note].StartBeat;
                if (diff <= 0f)
                    diff = 1f;

                float p = (CurrentBeat - _Notes[jump_note].StartBeat) / diff;
                if (p > 1f)
                    p = 1f;

                p = 1f - p;

                if (diff == 1)
                    _Text.Draw();
                else
                {
                    float y = _Text.Y;
                    _Text.Y -= _Text.Height * 0.1f * p;
                    _Text.Draw();
                    _Text.Y = y;
                }
            }
        }
        #endregion draw

        public void UnloadTextures()
        {
        }

        public void LoadTextures()
        {
            if (_Theme.ColorName != String.Empty)
                Color = CBase.Theme.GetColor(_Theme.ColorName, _PartyModeID);

            if (_Theme.SColorName != String.Empty)
                ColorProcessed = CBase.Theme.GetColor(_Theme.SColorName, _PartyModeID);
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
