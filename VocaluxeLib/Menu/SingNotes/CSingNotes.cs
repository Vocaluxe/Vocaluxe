using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace Vocaluxe.Menu.SingNotes
{
    struct SPlayerNotes
    {
        public int ID;
        public int PlayerNr;
        public SRectF Rect;
        public SColorF Color;
        public float Alpha;
        public CLine[] Lines;
        public int LineNr;
        public Stopwatch Timer;
        public List<CParticleEffect> GoldenStars;
        public List<CParticleEffect> Flares;
        public List<CParticleEffect> PerfectNoteEffect;
        public List<CParticleEffect> PerfectLineTwinkle;
    }

    struct SThemeSingBar
    {
        public string Name;

        public string SkinLeftName;
        public string SkinMiddleName;
        public string SkinRightName;

        public string SkinBackgroundLeftName;
        public string SkinBackgroundMiddleName;
        public string SkinBackgroundRightName;

        public string SkinGoldenStarName;
        public string SkinToneHelperName;
        public string SkinPerfectNoteStarName;
    }

    public abstract class CSingNotes : ISingNotes, IMenuElement
    {
        protected int _PartyModeID;
        private Basic _Base;
        private SThemeSingBar _Theme;
        private bool _ThemeLoaded;

        private List<SPlayerNotes> _PlayerNotes;
        private int _ActID;

        private SRectF[,] _BarPos;

        /// <summary>
        /// Player bar positions
        /// </summary>
        /// <remarks>
        /// first index = player number; second index = num players seen on screen
        /// </remarks>
        public SRectF[,] BarPos
        {
            get { return _BarPos; }
            set { _BarPos = value; }
        }

        public CSingNotes(Basic Base, int PartyModeID)
        {
            _PartyModeID = PartyModeID;
            _Base = Base;
            _Theme = new SThemeSingBar();
            _ThemeLoaded = false;

            _PlayerNotes = new List<SPlayerNotes>();
            _ActID = 0;
        }

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool LoadTheme(string XmlPath, string ElementName, XPathNavigator navigator, int SkinIndex)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinLeft", navigator, ref _Theme.SkinLeftName, String.Empty);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinMiddle", navigator, ref _Theme.SkinMiddleName, String.Empty);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinRight", navigator, ref _Theme.SkinRightName, String.Empty);

            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinBackgroundLeft", navigator, ref _Theme.SkinBackgroundLeftName, String.Empty);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinBackgroundMiddle", navigator, ref _Theme.SkinBackgroundMiddleName, String.Empty);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinBackgroundRight", navigator, ref _Theme.SkinBackgroundRightName, String.Empty);

            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinGoldenStar", navigator, ref _Theme.SkinGoldenStarName, String.Empty);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinToneHelper", navigator, ref _Theme.SkinToneHelperName, String.Empty);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/SkinPerfectNoteStar", navigator, ref _Theme.SkinPerfectNoteStarName, String.Empty);

            _BarPos = new SRectF[_Base.Settings.GetMaxNumPlayer(), _Base.Settings.GetMaxNumPlayer()];
            for (int numplayer = 0; numplayer < _Base.Settings.GetMaxNumPlayer(); numplayer++)
            {
                for (int player = 0; player < _Base.Settings.GetMaxNumPlayer(); player++)
                {
                    if (player <= numplayer)
                    {
                        _BarPos[player, numplayer] = new SRectF();
                        string target = "/BarPositions/P" + (player + 1).ToString() + "N" + (numplayer + 1).ToString();
                        _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + target + "X", navigator, ref _BarPos[player, numplayer].X);
                        _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + target + "Y", navigator, ref _BarPos[player, numplayer].Y);
                        _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + target + "Z", navigator, ref _BarPos[player, numplayer].Z);
                        _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + target + "W", navigator, ref _BarPos[player, numplayer].W);
                        _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + target + "H", navigator, ref _BarPos[player, numplayer].H);
                    }
                }
            }

            if (_ThemeLoaded)
            {
                _Theme.Name = ElementName;
                LoadTextures();
            }

            return _ThemeLoaded;
        }

        public bool SaveTheme(XmlWriter writer)
        {
            if (_ThemeLoaded)
            {
                writer.WriteStartElement(_Theme.Name);

                writer.WriteComment("<SkinLeft>: Texture name of note begin");
                writer.WriteElementString("SkinLeft", _Theme.SkinLeftName);

                writer.WriteComment("<SkinMiddle>: Texture name of note middle");
                writer.WriteElementString("SkinMiddle", _Theme.SkinMiddleName);

                writer.WriteComment("<SkinRight>: Texture name of note end");
                writer.WriteElementString("SkinRight", _Theme.SkinRightName);

                writer.WriteComment("<SkinBackgroundLeft>: Texture name of note background begin");
                writer.WriteElementString("SkinBackgroundLeft", _Theme.SkinBackgroundLeftName);

                writer.WriteComment("<SkinBackgroundMiddle>: Texture name of note background middle");
                writer.WriteElementString("SkinBackgroundMiddle", _Theme.SkinBackgroundMiddleName);

                writer.WriteComment("<SkinBackgroundRight>: Texture name of note background right");
                writer.WriteElementString("SkinBackgroundRight", _Theme.SkinBackgroundRightName);

                writer.WriteComment("<SkinGoldenStar>: Texture name of golden star");
                writer.WriteElementString("SkinGoldenStar", _Theme.SkinGoldenStarName);

                writer.WriteComment("<SkinToneHelper>: Texture name of tone helper");
                writer.WriteElementString("SkinToneHelper", _Theme.SkinToneHelperName);

                writer.WriteComment("<SkinPerfectNoteStar>: Texture name of perfect star");
                writer.WriteElementString("SkinPerfectNoteStar", _Theme.SkinPerfectNoteStarName);

                writer.WriteComment("<BarPositions>");
                writer.WriteComment("  <P$N$X>, <P$N$Y>, <P$N$Z>, <P$N$W>, <P$N$H>");
                writer.WriteComment("  position, width and height of note bars: first index = player number; second index = num players seen on screen");
                writer.WriteStartElement("BarPositions");
                for (int numplayer = 0; numplayer < _Base.Settings.GetMaxNumPlayer(); numplayer++)
                {
                    for (int player = 0; player < _Base.Settings.GetMaxNumPlayer(); player++)
                    {
                        if (player <= numplayer)
                        {
                            string target = "P" + (player + 1).ToString() + "N" + (numplayer + 1).ToString();

                            writer.WriteElementString(target + "X", _BarPos[player, numplayer].X.ToString("#0"));
                            writer.WriteElementString(target + "Y", _BarPos[player, numplayer].Y.ToString("#0"));
                            writer.WriteElementString(target + "Z", _BarPos[player, numplayer].Z.ToString("#0.00"));
                            writer.WriteElementString(target + "W", _BarPos[player, numplayer].W.ToString("#0"));
                            writer.WriteElementString(target + "H", _BarPos[player, numplayer].H.ToString("#0"));
                        }
                    }
                }
                writer.WriteEndElement(); //BarPositions

                writer.WriteEndElement();

                return true;
            }

            return false;
        }
        public virtual void Reset()
        {
            _PlayerNotes.Clear();
            _ActID = 0;
        }

        public virtual int AddPlayer(SRectF Rect, SColorF Color, int PlayerNr)
        {
            SPlayerNotes notes = new SPlayerNotes();

            notes.Rect = Rect;
            notes.Color = Color;
            notes.Alpha = 1f;
            notes.ID = ++_ActID;
            notes.Lines = null;
            notes.LineNr = -1;
            notes.PlayerNr = PlayerNr;
            notes.Timer = new Stopwatch();
            notes.GoldenStars = new List<CParticleEffect>();
            notes.Flares = new List<CParticleEffect>();
            notes.PerfectNoteEffect = new List<CParticleEffect>();
            notes.PerfectLineTwinkle = new List<CParticleEffect>();
            _PlayerNotes.Add(notes);

            return notes.ID;
        }

        public virtual void RemovePlayer(int ID)
        {

        }

        public virtual void AddLine(int ID, CLine[] Line, int LineNr, int Player)
        {
            if (Line == null)
                return;

            int n = FindPlayerLine(ID);
            if (n == -1)
                return;

            if (LineNr == _PlayerNotes[n].LineNr)
                return;

            SPlayerNotes notes = _PlayerNotes[n];

            if (Line.Length == 0 || Line.Length <= LineNr)
                return;

            notes.Lines = Line;
            notes.LineNr = LineNr;
            notes.GoldenStars.Clear();
            notes.Flares.Clear();
            notes.PerfectNoteEffect.Clear();

            _PlayerNotes.RemoveAt(n);
            _PlayerNotes.Add(notes);
        }

        public virtual void RemoveLine(int ID)
        {

        }

        public virtual void AddNote(int ID, CNote Note)
        {

        }

        public virtual void SetAlpha(int ID, float Alpha)
        {
            int n = FindPlayerLine(ID);
            if (n == -1)
                return;

            SPlayerNotes pn = _PlayerNotes[n];
            pn.Alpha = Alpha;
            _PlayerNotes[n] = pn;
        }

        public virtual float GetAlpha(int ID)
        {
            int n = FindPlayerLine(ID);
            if (n == -1)
                return 0f;

            return _PlayerNotes[n].Alpha;
        }
        
        public virtual void Draw(int ID, int Player)
        {
            Draw(ID, null, Player);            
        }

        public virtual void Draw(int ID, List<CLine> SingLine, int Player)
        {
            int n = FindPlayerLine(ID);
            if (n == -1)
                return;

            if (_PlayerNotes[n].LineNr == -1)
                return;

            if (_PlayerNotes[n].Lines == null)
                return;

            if (_PlayerNotes[n].Lines.Length <= _PlayerNotes[n].LineNr)
                return;

            CLine Line = _PlayerNotes[n].Lines[_PlayerNotes[n].LineNr];

            if (_Base.Config.GetDrawNoteLines() == EOffOn.TR_CONFIG_ON)
            {
                DrawNoteLines(_PlayerNotes[n].Rect, new SColorF(0.5f, 0.5f, 0.5f, 0.5f * _PlayerNotes[n].Alpha));
            }

            if (Line.NoteCount == 0)
                return;

            float w = _PlayerNotes[n].Rect.W;
            float h = _PlayerNotes[n].Rect.H;
            float dh = h / _Base.Settings.GetNumNoteLines() * (2f - (int)_Base.Game.GetPlayer()[_PlayerNotes[n].PlayerNr].Difficulty) / 4f;

            float beats = Line.LastNoteBeat - Line.FirstNoteBeat + 1;

            if (beats == 0)
                return;

            SColorF color = new SColorF(
                        _PlayerNotes[n].Color.R,
                        _PlayerNotes[n].Color.G,
                        _PlayerNotes[n].Color.B,
                        _PlayerNotes[n].Color.A * _PlayerNotes[n].Alpha);

            float BaseLine = Line.BaseLine;
            int Nr = 1;
            foreach (CNote note in Line.Notes)
            {
                if (note.NoteType != ENoteType.Freestyle)
                {
                    float width = note.Duration / beats * w;

                    SRectF rect = new SRectF(
                        _PlayerNotes[n].Rect.X + (note.StartBeat - Line.FirstNoteBeat) / beats * w,
                        _PlayerNotes[n].Rect.Y + (_Base.Settings.GetNumNoteLines() - 1 - (note.Tone - BaseLine)/2) / _Base.Settings.GetNumNoteLines() * h - dh,
                        width,
                        h / _Base.Settings.GetNumNoteLines() + 2 * dh,
                        _PlayerNotes[n].Rect.Z
                        );

                    DrawNoteBG(rect, color, 1f, _PlayerNotes[n].Timer);
                    DrawNote(rect, new SColorF(5f, 5f, 5f, 0.7f * _PlayerNotes[n].Alpha), 0.7f);

                    if (note.NoteType == ENoteType.Golden)
                    {
                        AddGoldenNote(rect, n, Nr);
                        Nr++;
                    }
                } 
            }

            if (_Base.Config.GetDrawToneHelper() == EOffOn.TR_CONFIG_ON)
            {
                DrawToneHelper(n, (int)BaseLine, (_Base.Game.GetMidBeatD() - Line.FirstNoteBeat) / beats * w);
            }

            int i = 0;
            while (i < _PlayerNotes[n].PerfectLineTwinkle.Count)
            {
                _PlayerNotes[n].PerfectLineTwinkle[i].Update();
                if (!_PlayerNotes[n].PerfectLineTwinkle[i].IsAlive)
                {
                    _PlayerNotes[n].PerfectLineTwinkle.RemoveAt(i);
                }
                else
                    i++;
            }

            foreach (CParticleEffect perfline in _PlayerNotes[n].PerfectLineTwinkle)
            {
                perfline.Draw();
            }

            if (SingLine == null || SingLine.Count == 0 || _Base.Game.GetPlayer()[Player].CurrentLine == -1 || SingLine.Count <= _Base.Game.GetPlayer()[Player].CurrentLine)
            {
                foreach (CParticleEffect stars in _PlayerNotes[n].GoldenStars)
                {
                    stars.Update();
                    stars.Alpha = _PlayerNotes[n].Alpha;
                    stars.Draw();
                }
                return;
            }

            foreach (CNote note in SingLine[_Base.Game.GetPlayer()[Player].CurrentLine].Notes)
            {
                if (note.StartBeat >= Line.FirstNoteBeat && note.EndBeat <= Line.LastNoteBeat)
                {
                    float width = note.Duration / beats * w;

                    if (note.EndBeat == _Base.Game.GetCurrentBeatD())
                        width -= (1 - (_Base.Game.GetMidBeatD() - _Base.Game.GetCurrentBeatD())) / beats * w;

                    SRectF rect = new SRectF(
                        _PlayerNotes[n].Rect.X + (note.StartBeat - Line.FirstNoteBeat) / beats * w,
                        _PlayerNotes[n].Rect.Y + (_Base.Settings.GetNumNoteLines() - 1 - (note.Tone - BaseLine)/2) / _Base.Settings.GetNumNoteLines() * h - dh,
                        width,
                        h / _Base.Settings.GetNumNoteLines() + 2 * dh,
                        _PlayerNotes[n].Rect.Z
                        );

                    float f = 0.7f;
                    if (!note.Hit)
                        f = 0.4f;

                    DrawNote(rect, color, f);

                    if (note.EndBeat >= _Base.Game.GetCurrentBeatD() && note.Hit && note.NoteType == ENoteType.Golden)
                    {
                        SRectF re = new SRectF(rect);
                        re.W = (_Base.Game.GetMidBeatD() - note.StartBeat) / beats * w;
                        AddFlare(re, n);
                    }

                    if (note.Perfect && note.EndBeat < _Base.Game.GetCurrentBeatD())
                    {
                        AddPerfectNote(rect, n);
                        note.Perfect = false;
                    }
                }
            }

            int currentLine = _Base.Game.GetPlayer()[Player].SingLine.Count - 1;
            if (currentLine > 0)
            {
                if (_Base.Game.GetPlayer()[Player].SingLine[currentLine - 1].PerfectLine)
                {
                    AddPerfectLine(n);
                    _Base.Game.GetPlayer()[Player].SingLine[currentLine - 1].PerfectLine = false;
                }
            }

            i = 0;
            while (i < _PlayerNotes[n].Flares.Count)
            {
                _PlayerNotes[n].Flares[i].Update();
                if (!_PlayerNotes[n].Flares[i].IsAlive)
                {
                    _PlayerNotes[n].Flares.RemoveAt(i);
                }
                else
                    i++;
            }

            i = 0;
            while (i < _PlayerNotes[n].PerfectNoteEffect.Count)
            {
                _PlayerNotes[n].PerfectNoteEffect[i].Update();
                if (!_PlayerNotes[n].PerfectNoteEffect[i].IsAlive)
                {
                    _PlayerNotes[n].PerfectNoteEffect.RemoveAt(i);
                }
                else
                    i++;
            }

            


            foreach (CParticleEffect stars in _PlayerNotes[n].GoldenStars)
            {
                stars.Update();
                stars.Alpha = _PlayerNotes[n].Alpha;
                stars.Draw();
            }

            foreach (CParticleEffect flare in _PlayerNotes[n].Flares)
            {
                flare.Draw();
            }

            foreach (CParticleEffect perfnote in _PlayerNotes[n].PerfectNoteEffect)
            {
                perfnote.Draw();
            }
        }

        public void UnloadTextures()
        {
        }

        public void LoadTextures()
        {
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();            
        }

        private int FindPlayerLine(int ID)
        {
            if (ID < 0 || ID > _PlayerNotes.Count)
                return -1;

            int n = -1;
            for (int i = 0; i < _PlayerNotes.Count; i++)
            {
                if (_PlayerNotes[i].ID == ID)
                {
                    n = i;
                    break;
                }
            }

            return n;
        }

        private void DrawNote(SRectF Rect, SColorF Color)
        {
            DrawNote(Rect, Color, 1f);
        }

        private void DrawNote(SRectF Rect, SColorF Color, float factor)
        {
            const int spacing = 0;

            float d = (1f - factor) / 2 * Rect.H;
            float dw = d;

            if (2 * dw > Rect.W)
                dw = Rect.W / 2;

            SRectF r = new SRectF(
                Rect.X + dw + spacing,
                Rect.Y + d + spacing,
                Rect.W - 2 * dw - 2 * spacing,
                Rect.H - 2 * d - 2 * spacing,
                Rect.Z
                );

            STexture NoteBegin = _Base.Theme.GetSkinTexture(_Theme.SkinLeftName);
            STexture NoteMiddle = _Base.Theme.GetSkinTexture(_Theme.SkinMiddleName);
            STexture NoteEnd = _Base.Theme.GetSkinTexture(_Theme.SkinRightName);

            float dx = NoteBegin.width * r.H / NoteBegin.height;
            if (2 * dx > r.W)
                dx = r.W / 2;


            _Base.Drawing.DrawTexture(NoteBegin, new SRectF(r.X, r.Y, dx, r.H, r.Z), Color);
            _Base.Drawing.DrawTexture(NoteMiddle, new SRectF(r.X + dx, r.Y, r.W - 2 * dx, r.H, r.Z), Color);
            _Base.Drawing.DrawTexture(NoteEnd, new SRectF(r.X + r.W - dx, r.Y, dx, r.H, r.Z), Color);
        }

        private void DrawNoteBG(SRectF Rect, SColorF Color, float factor, Stopwatch Timer)
        {
            const int spacing = 0;
            const float period = 1.5f; //[s]

            if (!Timer.IsRunning)
                Timer.Start();

            if (Timer.ElapsedMilliseconds / 1000f > period)
            {
                Timer.Reset();
                Timer.Start();
            }

            float alpha = (float)((Math.Cos((Timer.ElapsedMilliseconds / 1000f) / period * Math.PI * 2) + 1) / 2.0) / 2f + 0.5f;
            float d = (1f - factor) / 2 * Rect.H;
            float dw = d;
            if (2 * dw > Rect.W)
                dw = Rect.W / 2;

            SRectF r = new SRectF(
                Rect.X + dw + spacing,
                Rect.Y + d + spacing,
                Rect.W - 2 * dw - 2 * spacing,
                Rect.H - 2 * d - 2 * spacing,
                Rect.Z
                );

            STexture NoteBackgroundBegin = _Base.Theme.GetSkinTexture(_Theme.SkinBackgroundLeftName);
            STexture NoteBackgroundMiddle = _Base.Theme.GetSkinTexture(_Theme.SkinBackgroundMiddleName);
            STexture NoteBackgroundEnd = _Base.Theme.GetSkinTexture(_Theme.SkinBackgroundRightName);

            float dx = NoteBackgroundBegin.width * r.H / NoteBackgroundBegin.height;
            if (2 * dx > r.W)
                dx = r.W / 2;

            SColorF col = new SColorF(Color.R, Color.G, Color.B, Color.A * alpha);

            _Base.Drawing.DrawTexture(NoteBackgroundBegin, new SRectF(r.X, r.Y, dx, r.H, r.Z), col);
            _Base.Drawing.DrawTexture(NoteBackgroundMiddle, new SRectF(r.X + dx, r.Y, r.W - 2 * dx, r.H, r.Z), col);
            _Base.Drawing.DrawTexture(NoteBackgroundEnd, new SRectF(r.X + r.W - dx, r.Y, dx, r.H, r.Z), col);
        }

        protected void DrawNoteLines(SRectF Rect, SColorF Color)
        {
            for (int i = 0; i < _Base.Settings.GetNumNoteLines() - 1; i++)
            {
                float y = Rect.Y + Rect.H / _Base.Settings.GetNumNoteLines() * (i + 1);
                _Base.Drawing.DrawColor(Color, new SRectF(Rect.X, y, Rect.W, 1, -1.0f));
            }
        }

        private void AddGoldenNote(SRectF Rect, int n, int Nr)
        {
            AddGoldenNote(Rect, n, Nr, 1f);
        }

        private void AddGoldenNote(SRectF Rect, int n, int Nr, float factor)
        {
            const int spacing = 0;

            if ( Nr > _PlayerNotes[n].GoldenStars.Count)
            {
                float d = (1f - factor) / 2 * Rect.H;
                float dw = d;

                if (2 * dw > Rect.W)
                    dw = Rect.W / 2;

                SRectF r = new SRectF(
                    Rect.X + dw + spacing,
                    Rect.Y + d + spacing,
                    Rect.W - 2 * dw - 2 * spacing,
                    Rect.H - 2 * d - 2 * spacing,
                    Rect.Z
                    );

                int numstars = (int)(r.W * 0.25f);
                CParticleEffect stars = new CParticleEffect(_Base, _PartyModeID, numstars, new SColorF(1f, 1f, 0f, 1f), r, _Theme.SkinGoldenStarName, 20, EParticeType.Star);
                _PlayerNotes[n].GoldenStars.Add(stars);
            }
        }

        private void AddFlare(SRectF Rect, int n)
        {
            AddFlare(Rect, n, 1f);
        }

        private void AddFlare(SRectF Rect, int n, float factor)
        {
            const int spacing = 0;

            float d = (1f - factor) / 2 * Rect.H;
            float dw = d;

            if (2 * dw > Rect.W)
                dw = Rect.W / 2;

            SRectF r = new SRectF(
                Rect.X + dw + spacing + Rect.W - 2 * dw - 2 * spacing,
                Rect.Y + d + spacing,
                0f,
                Rect.H - 2 * d - 2 * spacing,
                Rect.Z
                );

            CParticleEffect flares = new CParticleEffect(_Base, _PartyModeID, 15, new SColorF(1f, 1f, 1f, 1f), r, _Theme.SkinGoldenStarName, 20, EParticeType.Flare);
            _PlayerNotes[n].Flares.Add(flares);
        }

        private void AddPerfectNote(SRectF Rect, int n)
        {
            AddPerfectNote(Rect, n, 1f);
        }

        private void AddPerfectNote(SRectF Rect, int n, float factor)
        {
            const int spacing = 0;

            float d = (1f - factor) / 2 * Rect.H;
            float dw = d;

            if (2 * dw > Rect.W)
                dw = Rect.W / 2;

            SRectF r = new SRectF(
                Rect.X + dw + spacing,
                Rect.Y + d + spacing,
                Rect.W - 2 * dw - 2 * spacing,
                Rect.H - 2 * d - 2 * spacing,
                Rect.Z
                );

            STexture NoteBegin = _Base.Theme.GetSkinTexture(_Theme.SkinLeftName);
            float dx = NoteBegin.width * r.H / NoteBegin.height;
            if (2 * dx > r.W)
                dx = r.W / 2;

            r = new SRectF(
                r.X + r.W - dx,
                r.Y,
                dx * 0.5f,
                dx * 0.2f,
                Rect.Z
                );

            CParticleEffect stars = new CParticleEffect(_Base, _PartyModeID, _Base.Game.GetRandom(2) + 1, new SColorF(1f, 1f, 1f, 1f), r, _Theme.SkinPerfectNoteStarName, 35, EParticeType.PerfNoteStar);
            _PlayerNotes[n].PerfectNoteEffect.Add(stars);
        }

        private void AddPerfectLine(int n)
        {
            CParticleEffect twinkle = new CParticleEffect(_Base, _PartyModeID, 200, _PlayerNotes[n].Color, _PlayerNotes[n].Rect, _Theme.SkinGoldenStarName, 25, EParticeType.Twinkle);
            _PlayerNotes[n].PerfectLineTwinkle.Add(twinkle);
        }

        private void DrawToneHelper(int n, int BaseLine, float XOffset)
        {
            int TonePlayer = _Base.Record.GetToneAbs(_PlayerNotes[n].PlayerNr);

            SRectF Rect = _PlayerNotes[n].Rect;
            
            while (TonePlayer - BaseLine < 0)
                TonePlayer += 12;

            while (TonePlayer - BaseLine > 12)
                TonePlayer -= 12;

            if (XOffset < 0f)
                XOffset = 0f;

            if (XOffset > Rect.W)
                XOffset = Rect.W;

            float dy = Rect.H / (_Base.Settings.GetNumNoteLines());
            SRectF rect = new SRectF(
                        Rect.X - dy + XOffset,
                        Rect.Y + dy * (_Base.Settings.GetNumNoteLines() - 1 - (TonePlayer - BaseLine) / 2f),
                        dy,
                        dy,
                        Rect.Z
                        );

            SColorF color = new SColorF(
                        _PlayerNotes[n].Color.R,
                        _PlayerNotes[n].Color.G,
                        _PlayerNotes[n].Color.B,
                        _PlayerNotes[n].Color.A * _PlayerNotes[n].Alpha);

            STexture ToneHelper = _Base.Theme.GetSkinTexture(_Theme.SkinToneHelperName);
            _Base.Drawing.DrawTexture(ToneHelper, rect, color);


            while (TonePlayer - BaseLine < 12)
                TonePlayer += 12;

            while (TonePlayer - BaseLine > 24)
                TonePlayer -= 12;

            rect = new SRectF(
                Rect.X - dy + XOffset,
                Rect.Y + dy * (_Base.Settings.GetNumNoteLines() - 1 - (TonePlayer - BaseLine) / 2f),
                dy,
                dy,
                Rect.Z
                );

            _Base.Drawing.DrawTexture(ToneHelper, rect, color);
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
        }

        public void ResizeElement(int stepW, int stepH)
        {
        }
        #endregion ThemeEdit
    }
}
