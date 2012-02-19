using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Lib.Song;

namespace Vocaluxe.Menu
{
    enum ELyricStyle
    {
        Fill,
        Jump,
        Slide
    }

    struct SNote
    {
        public string Text;
        public int StartBeat;
        public int EndBeat;
        public ENoteType Type;
    }

    struct SThemeLyrics
    {
        public string Name;

        public string ColorName;
        public string SColorName;
    }

    class CLyric : IMenuElement
    {
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

        public CLyric()
        {
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
            _Text = new CText();

            _Style = ELyricStyle.Slide;
        }

        public bool LoadTheme(string XmlPath, string ElementName, XPathNavigator navigator, int SkinIndex)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/X", navigator, ref _X);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Y", navigator, ref _Y);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Z", navigator, ref _Z);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/W", navigator, ref _MaxW);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/H", navigator, ref _H);

            if (CHelper.GetValueFromXML(item + "/Color", navigator, ref _Theme.ColorName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.ColorName, SkinIndex, ref Color);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/R", navigator, ref Color.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/G", navigator, ref Color.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/B", navigator, ref Color.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/A", navigator, ref Color.A);
            }

            if (CHelper.GetValueFromXML(item + "/SColor", navigator, ref _Theme.SColorName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.SColorName, SkinIndex, ref ColorProcessed);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SR", navigator, ref ColorProcessed.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SG", navigator, ref ColorProcessed.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SB", navigator, ref ColorProcessed.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SA", navigator, ref ColorProcessed.A);
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
                n.Type = note.NoteType;

                _Text.Text = note.Text;
                _Text.Style = EStyle.Bold;

                if (n.Type == ENoteType.Freestyle)
                    _Text.Style = EStyle.BoldItalic;
                
                RectangleF rect = CDraw.GetTextBounds(_Text);
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

        public void Draw(float ActualBeat)
        {
            if (Visible || CSettings.GameState == EGameState.EditTheme)
                DrawSlide(ActualBeat);
        }

        private void DrawSlide(float CurrentBeat)
        {
            float x = _X - _width / 2;
            
            foreach (SNote note in _Notes)
            {
                _Text.X = x;
                _Text.Style = EStyle.Bold;

                if (note.Type == ENoteType.Freestyle)
                    _Text.Style = EStyle.BoldItalic;

                if (CurrentBeat >= note.StartBeat)
                {
                    if (CurrentBeat <= note.EndBeat)
                    {
                        _Text.Color = ColorProcessed;
                        _Text.Text = note.Text;

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
                        _Text.Text = note.Text;
                        _Text.Draw();
                    }

                }
                else
                {
                    _Text.Color = Color;
                    _Text.Text = note.Text;
                    _Text.Draw();
                }
                RectangleF rect = CDraw.GetTextBounds(_Text);
                x += rect.Width;
            }
        }

        public void UnloadTextures()
        {
        }

        public void LoadTextures()
        {
            if (_Theme.ColorName != String.Empty)
                Color = CTheme.GetColor(_Theme.ColorName);

            if (_Theme.SColorName != String.Empty)
                ColorProcessed = CTheme.GetColor(_Theme.SColorName);
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
