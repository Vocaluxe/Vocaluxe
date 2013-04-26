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
using System.Diagnostics;
using System.Xml;

namespace VocaluxeLib.Menu.SingNotes
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

    public abstract class CSingNotes : IMenuElement
    {
        protected readonly int _PartyModeID;
        private SThemeSingBar _Theme;
        private bool _ThemeLoaded;

        private readonly List<SPlayerNotes> _PlayerNotes;
        private int _ActID;

        /// <summary>
        ///     Player bar positions
        /// </summary>
        /// <remarks>
        ///     first index = player number; second index = num players seen on screen
        /// </remarks>
        public SRectF[,] BarPos { get; set; }

        protected CSingNotes(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = new SThemeSingBar();
            _ThemeLoaded = false;

            _PlayerNotes = new List<SPlayerNotes>();
            _ActID = 0;
        }

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader, int skinIndex)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinLeft", out _Theme.SkinLeftName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinMiddle", out _Theme.SkinMiddleName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinRight", out _Theme.SkinRightName, String.Empty);

            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinBackgroundLeft", out _Theme.SkinBackgroundLeftName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinBackgroundMiddle", out _Theme.SkinBackgroundMiddleName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinBackgroundRight", out _Theme.SkinBackgroundRightName, String.Empty);

            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinGoldenStar", out _Theme.SkinGoldenStarName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinToneHelper", out _Theme.SkinToneHelperName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinPerfectNoteStar", out _Theme.SkinPerfectNoteStarName, String.Empty);

            BarPos = new SRectF[CBase.Settings.GetMaxNumPlayer(),CBase.Settings.GetMaxNumPlayer()];
            for (int numplayer = 0; numplayer < CBase.Settings.GetMaxNumPlayer(); numplayer++)
            {
                for (int player = 0; player < CBase.Settings.GetMaxNumPlayer(); player++)
                {
                    if (player <= numplayer)
                    {
                        BarPos[player, numplayer] = new SRectF();
                        string target = "/BarPositions/P" + (player + 1) + "N" + (numplayer + 1);
                        _ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "X", ref BarPos[player, numplayer].X);
                        _ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "Y", ref BarPos[player, numplayer].Y);
                        _ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "Z", ref BarPos[player, numplayer].Z);
                        _ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "W", ref BarPos[player, numplayer].W);
                        _ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "H", ref BarPos[player, numplayer].H);
                    }
                }
            }

            if (_ThemeLoaded)
            {
                _Theme.Name = elementName;
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
                for (int numplayer = 0; numplayer < CBase.Settings.GetMaxNumPlayer(); numplayer++)
                {
                    for (int player = 0; player < CBase.Settings.GetMaxNumPlayer(); player++)
                    {
                        if (player > numplayer)
                            continue;
                        string target = "P" + (player + 1) + "N" + (numplayer + 1);

                        writer.WriteElementString(target + "X", BarPos[player, numplayer].X.ToString("#0"));
                        writer.WriteElementString(target + "Y", BarPos[player, numplayer].Y.ToString("#0"));
                        writer.WriteElementString(target + "Z", BarPos[player, numplayer].Z.ToString("#0.00"));
                        writer.WriteElementString(target + "W", BarPos[player, numplayer].W.ToString("#0"));
                        writer.WriteElementString(target + "H", BarPos[player, numplayer].H.ToString("#0"));
                    }
                }
                writer.WriteEndElement(); //BarPositions

                writer.WriteEndElement();

                return true;
            }

            return false;
        }

        public void Reset()
        {
            _PlayerNotes.Clear();
            _ActID = 0;
        }

        public int AddPlayer(SRectF rect, SColorF color, int playerNr)
        {
            SPlayerNotes notes = new SPlayerNotes
                {
                    Rect = rect,
                    Color = color,
                    Alpha = 1f,
                    ID = ++_ActID,
                    Lines = null,
                    LineNr = -1,
                    PlayerNr = playerNr,
                    Timer = new Stopwatch(),
                    GoldenStars = new List<CParticleEffect>(),
                    Flares = new List<CParticleEffect>(),
                    PerfectNoteEffect = new List<CParticleEffect>(),
                    PerfectLineTwinkle = new List<CParticleEffect>()
                };

            _PlayerNotes.Add(notes);

            return notes.ID;
        }

        public void SetLines(int id, CLine[] lines, int lineNr)
        {
            if (lines == null)
                return;

            int n = _FindPlayerNotes(id);
            if (n == -1)
                return;

            if (lineNr == _PlayerNotes[n].LineNr)
                return;

            if (lines.Length <= lineNr)
                return;

            SPlayerNotes notes = _PlayerNotes[n];

            notes.Lines = lines;
            notes.LineNr = lineNr;
            notes.GoldenStars.Clear();
            notes.Flares.Clear();
            notes.PerfectNoteEffect.Clear();

            _PlayerNotes.RemoveAt(n);
            _PlayerNotes.Add(notes);
        }

        public void SetAlpha(int id, float alpha)
        {
            int n = _FindPlayerNotes(id);
            if (n == -1)
                return;

            SPlayerNotes pn = _PlayerNotes[n];
            pn.Alpha = alpha;
            _PlayerNotes[n] = pn;
        }

        public float GetAlpha(int id)
        {
            int n = _FindPlayerNotes(id);
            if (n == -1)
                return 0f;

            return _PlayerNotes[n].Alpha;
        }

        public void Draw(int id, int player)
        {
            Draw(id, null, player);
        }

        public void Draw(int id, List<CLine> singLines, int player)
        {
            int n = _FindPlayerNotes(id);
            if (n == -1)
                return;

            if (_PlayerNotes[n].LineNr == -1)
                return;

            if (_PlayerNotes[n].Lines == null)
                return;

            if (_PlayerNotes[n].Lines.Length <= _PlayerNotes[n].LineNr)
                return;

            CLine line = _PlayerNotes[n].Lines[_PlayerNotes[n].LineNr];

            if (CBase.Config.GetDrawNoteLines() == EOffOn.TR_CONFIG_ON)
                _DrawNoteLines(_PlayerNotes[n].Rect, new SColorF(0.5f, 0.5f, 0.5f, 0.5f * _PlayerNotes[n].Alpha));

            if (line.NoteCount == 0)
                return;

            float w = _PlayerNotes[n].Rect.W;
            float h = _PlayerNotes[n].Rect.H;
            int profileID = CBase.Game.GetPlayers()[_PlayerNotes[n].PlayerNr].ProfileID;
            float dh = h / CBase.Settings.GetNumNoteLines() * (2f - (int)CBase.Profiles.GetDifficulty(profileID)) / 4f;

            float beats = line.LastNoteBeat - line.FirstNoteBeat + 1;

            if (beats < 1)
                return;

            SColorF color = new SColorF(
                _PlayerNotes[n].Color.R,
                _PlayerNotes[n].Color.G,
                _PlayerNotes[n].Color.B,
                _PlayerNotes[n].Color.A * _PlayerNotes[n].Alpha);

            float baseLine = line.BaseLine;
            int nr = 1;
            foreach (CNote note in line.Notes)
            {
                if (note.NoteType != ENoteType.Freestyle)
                {
                    float width = note.Duration / beats * w;

                    SRectF rect = new SRectF(
                        _PlayerNotes[n].Rect.X + (note.StartBeat - line.FirstNoteBeat) / beats * w,
                        _PlayerNotes[n].Rect.Y + (CBase.Settings.GetNumNoteLines() - 1 - (note.Tone - baseLine) / 2) / CBase.Settings.GetNumNoteLines() * h - dh,
                        width,
                        h / CBase.Settings.GetNumNoteLines() + 2 * dh,
                        _PlayerNotes[n].Rect.Z
                        );

                    _DrawNoteBG(rect, color, 1f, _PlayerNotes[n].Timer);
                    _DrawNote(rect, new SColorF(5f, 5f, 5f, 0.7f * _PlayerNotes[n].Alpha), 0.7f);

                    if (note.NoteType == ENoteType.Golden)
                    {
                        _AddGoldenNote(rect, n, nr);
                        nr++;
                    }
                }
            }

            if (CBase.Config.GetDrawToneHelper() == EOffOn.TR_CONFIG_ON)
                _DrawToneHelper(n, (int)baseLine, (CBase.Game.GetMidBeatD() - line.FirstNoteBeat) / beats * w);

            int i = 0;
            while (i < _PlayerNotes[n].PerfectLineTwinkle.Count)
            {
                if (!_PlayerNotes[n].PerfectLineTwinkle[i].IsAlive)
                    _PlayerNotes[n].PerfectLineTwinkle.RemoveAt(i);
                else
                    i++;
            }

            foreach (CParticleEffect perfline in _PlayerNotes[n].PerfectLineTwinkle)
                perfline.Draw();

            if (singLines == null || singLines.Count == 0 || CBase.Game.GetPlayers()[player].CurrentLine == -1 || singLines.Count <= CBase.Game.GetPlayers()[player].CurrentLine)
            {
                foreach (CParticleEffect stars in _PlayerNotes[n].GoldenStars)
                {
                    stars.Alpha = _PlayerNotes[n].Alpha;
                    stars.Draw();
                }
                return;
            }

            foreach (CNote note in singLines[CBase.Game.GetPlayers()[player].CurrentLine].Notes)
            {
                if (note.StartBeat >= line.FirstNoteBeat && note.EndBeat <= line.LastNoteBeat)
                {
                    float width = note.Duration / beats * w;

                    if (note.EndBeat == CBase.Game.GetCurrentBeatD())
                        width -= (1 - (CBase.Game.GetMidBeatD() - CBase.Game.GetCurrentBeatD())) / beats * w;

                    SRectF rect = new SRectF(
                        _PlayerNotes[n].Rect.X + (note.StartBeat - line.FirstNoteBeat) / beats * w,
                        _PlayerNotes[n].Rect.Y + (CBase.Settings.GetNumNoteLines() - 1 - (note.Tone - baseLine) / 2) / CBase.Settings.GetNumNoteLines() * h - dh,
                        width,
                        h / CBase.Settings.GetNumNoteLines() + 2 * dh,
                        _PlayerNotes[n].Rect.Z
                        );

                    float f = 0.7f;
                    if (!note.Hit)
                        f = 0.4f;

                    _DrawNote(rect, color, f);

                    if (note.EndBeat >= CBase.Game.GetCurrentBeatD() && note.Hit && note.NoteType == ENoteType.Golden)
                    {
                        SRectF re = new SRectF(rect) {W = (CBase.Game.GetMidBeatD() - note.StartBeat) / beats * w};
                        _AddFlare(re, n);
                    }

                    if (note.Perfect && note.EndBeat < CBase.Game.GetCurrentBeatD())
                    {
                        _AddPerfectNote(rect, n);
                        note.Perfect = false;
                    }
                }
            }

            int currentLine = CBase.Game.GetPlayers()[player].SingLine.Count - 1;
            if (currentLine > 0)
            {
                if (CBase.Game.GetPlayers()[player].SingLine[currentLine - 1].PerfectLine)
                {
                    _AddPerfectLine(n);
                    CBase.Game.GetPlayers()[player].SingLine[currentLine - 1].PerfectLine = false;
                }
            }

            i = 0;
            while (i < _PlayerNotes[n].Flares.Count)
            {
                if (!_PlayerNotes[n].Flares[i].IsAlive)
                    _PlayerNotes[n].Flares.RemoveAt(i);
                else
                    i++;
            }

            i = 0;
            while (i < _PlayerNotes[n].PerfectNoteEffect.Count)
            {
                if (!_PlayerNotes[n].PerfectNoteEffect[i].IsAlive)
                    _PlayerNotes[n].PerfectNoteEffect.RemoveAt(i);
                else
                    i++;
            }


            foreach (CParticleEffect stars in _PlayerNotes[n].GoldenStars)
            {
                stars.Alpha = _PlayerNotes[n].Alpha;
                stars.Draw();
            }

            foreach (CParticleEffect flare in _PlayerNotes[n].Flares)
                flare.Draw();

            foreach (CParticleEffect perfnote in _PlayerNotes[n].PerfectNoteEffect)
                perfnote.Draw();
        }

        public void UnloadTextures() {}

        public void LoadTextures() {}

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        private int _FindPlayerNotes(int id)
        {
            for (int i = 0; i < _PlayerNotes.Count; i++)
            {
                if (_PlayerNotes[i].ID == id)
                    return i;
            }

            return -1;
        }

        private void _DrawNote(SRectF rect, SColorF color, float factor = 1f)
        {
            const int spacing = 0;

            float d = (1f - factor) / 2 * rect.H;
            float dw = d;

            if (2 * dw > rect.W)
                dw = rect.W / 2;

            SRectF r = new SRectF(
                rect.X + dw + spacing,
                rect.Y + d + spacing,
                rect.W - 2 * dw - 2 * spacing,
                rect.H - 2 * d - 2 * spacing,
                rect.Z
                );

            STexture noteBegin = CBase.Theme.GetSkinTexture(_Theme.SkinLeftName, _PartyModeID);
            STexture noteMiddle = CBase.Theme.GetSkinTexture(_Theme.SkinMiddleName, _PartyModeID);
            STexture noteEnd = CBase.Theme.GetSkinTexture(_Theme.SkinRightName, _PartyModeID);

            float dx = noteBegin.Width * r.H / noteBegin.Height;
            if (2 * dx > r.W)
                dx = r.W / 2;


            CBase.Drawing.DrawTexture(noteBegin, new SRectF(r.X, r.Y, dx, r.H, r.Z), color);

            if (r.W - 2 * dx >= 2 * dx)
                CBase.Drawing.DrawTexture(noteMiddle, new SRectF(r.X + dx, r.Y, r.W - 2 * dx, r.H, r.Z), color);
            else
                CBase.Drawing.DrawTexture(noteMiddle, new SRectF(r.X + dx, r.Y, 2 * dx, r.H, r.Z), color, new SRectF(r.X + dx, r.Y, r.W - 2 * dx, r.H, r.Z));

            CBase.Drawing.DrawTexture(noteEnd, new SRectF(r.X + r.W - dx, r.Y, dx, r.H, r.Z), color);
        }

        private void _DrawNoteBG(SRectF rect, SColorF color, float factor, Stopwatch timer)
        {
            const int spacing = 0;
            const float period = 1.5f; //[s]

            if (!timer.IsRunning)
                timer.Start();

            if (timer.ElapsedMilliseconds / 1000f > period)
            {
                timer.Reset();
                timer.Start();
            }

            float alpha = (float)((Math.Cos((timer.ElapsedMilliseconds / 1000f) / period * Math.PI * 2) + 1) / 2.0) / 2f + 0.5f;
            float d = (1f - factor) / 2 * rect.H;
            float dw = d;
            if (2 * dw > rect.W)
                dw = rect.W / 2;

            SRectF r = new SRectF(
                rect.X + dw + spacing,
                rect.Y + d + spacing,
                rect.W - 2 * dw - 2 * spacing,
                rect.H - 2 * d - 2 * spacing,
                rect.Z
                );

            STexture noteBackgroundBegin = CBase.Theme.GetSkinTexture(_Theme.SkinBackgroundLeftName, _PartyModeID);
            STexture noteBackgroundMiddle = CBase.Theme.GetSkinTexture(_Theme.SkinBackgroundMiddleName, _PartyModeID);
            STexture noteBackgroundEnd = CBase.Theme.GetSkinTexture(_Theme.SkinBackgroundRightName, _PartyModeID);

            float dx = noteBackgroundBegin.Width * r.H / noteBackgroundBegin.Height;
            if (2 * dx > r.W)
                dx = r.W / 2;

            SColorF col = new SColorF(color.R, color.G, color.B, color.A * alpha);

            CBase.Drawing.DrawTexture(noteBackgroundBegin, new SRectF(r.X, r.Y, dx, r.H, r.Z), col);

            if (r.W - 2 * dx >= 2 * dx)
                CBase.Drawing.DrawTexture(noteBackgroundMiddle, new SRectF(r.X + dx, r.Y, r.W - 2 * dx, r.H, r.Z), col);
            else
                CBase.Drawing.DrawTexture(noteBackgroundMiddle, new SRectF(r.X + dx, r.Y, 2 * dx, r.H, r.Z), col, new SRectF(r.X + dx, r.Y, r.W - 2 * dx, r.H, r.Z));

            CBase.Drawing.DrawTexture(noteBackgroundEnd, new SRectF(r.X + r.W - dx, r.Y, dx, r.H, r.Z), col);
        }

        protected void _DrawNoteLines(SRectF rect, SColorF color)
        {
            for (int i = 0; i < CBase.Settings.GetNumNoteLines() - 1; i++)
            {
                float y = rect.Y + rect.H / CBase.Settings.GetNumNoteLines() * (i + 1);
                CBase.Drawing.DrawColor(color, new SRectF(rect.X, y, rect.W, 1, -1.0f));
            }
        }

        private void _AddGoldenNote(SRectF rect, int n, int nr, float factor = 1f)
        {
            const int spacing = 0;

            if (nr > _PlayerNotes[n].GoldenStars.Count)
            {
                float d = (1f - factor) / 2 * rect.H;
                float dw = d;

                if (2 * dw > rect.W)
                    dw = rect.W / 2;

                SRectF r = new SRectF(
                    rect.X + dw + spacing,
                    rect.Y + d + spacing,
                    rect.W - 2 * dw - 2 * spacing,
                    rect.H - 2 * d - 2 * spacing,
                    rect.Z
                    );

                int numstars = (int)(r.W * 0.25f);
                CParticleEffect stars = new CParticleEffect(_PartyModeID, numstars, new SColorF(1f, 1f, 0f, 1f), r, _Theme.SkinGoldenStarName, 20, EParticleType.Star);
                _PlayerNotes[n].GoldenStars.Add(stars);
            }
        }

        private void _AddFlare(SRectF rect, int n, float factor = 1f)
        {
            const int spacing = 0;

            float d = (1f - factor) / 2 * rect.H;
            float dw = d;

            if (2 * dw > rect.W)
                dw = rect.W / 2;

            SRectF r = new SRectF(
                rect.X + dw + spacing + rect.W - 2 * dw - 2 * spacing,
                rect.Y + d + spacing,
                0f,
                rect.H - 2 * d - 2 * spacing,
                rect.Z
                );

            CParticleEffect flares = new CParticleEffect(_PartyModeID, 15, new SColorF(1f, 1f, 1f, 1f), r, _Theme.SkinGoldenStarName, 20, EParticleType.Flare);
            _PlayerNotes[n].Flares.Add(flares);
        }

        private void _AddPerfectNote(SRectF rect, int n, float factor = 1f)
        {
            const int spacing = 0;

            float d = (1f - factor) / 2 * rect.H;
            float dw = d;

            if (2 * dw > rect.W)
                dw = rect.W / 2;

            SRectF r = new SRectF(
                rect.X + dw + spacing,
                rect.Y + d + spacing,
                rect.W - 2 * dw - 2 * spacing,
                rect.H - 2 * d - 2 * spacing,
                rect.Z
                );

            STexture noteBegin = CBase.Theme.GetSkinTexture(_Theme.SkinLeftName, _PartyModeID);
            float dx = noteBegin.Width * r.H / noteBegin.Height;
            if (2 * dx > r.W)
                dx = r.W / 2;

            r = new SRectF(
                r.X + r.W - dx,
                r.Y,
                dx * 0.5f,
                dx * 0.2f,
                rect.Z
                );

            CParticleEffect stars = new CParticleEffect(_PartyModeID, CBase.Game.GetRandom(2) + 1, new SColorF(1f, 1f, 1f, 1f), r, _Theme.SkinPerfectNoteStarName, 35,
                                                        EParticleType.PerfNoteStar);
            _PlayerNotes[n].PerfectNoteEffect.Add(stars);
        }

        private void _AddPerfectLine(int n)
        {
            CParticleEffect twinkle = new CParticleEffect(_PartyModeID, 200, _PlayerNotes[n].Color, _PlayerNotes[n].Rect, _Theme.SkinGoldenStarName, 25, EParticleType.Twinkle);
            _PlayerNotes[n].PerfectLineTwinkle.Add(twinkle);
        }

        private void _DrawToneHelper(int n, int baseLine, float offsetX)
        {
            int tonePlayer = CBase.Record.GetToneAbs(_PlayerNotes[n].PlayerNr);

            SRectF noteBounds = _PlayerNotes[n].Rect;

            while (tonePlayer - baseLine < 0)
                tonePlayer += 12;

            while (tonePlayer - baseLine > 12)
                tonePlayer -= 12;

            if (offsetX < 0f)
                offsetX = 0f;

            if (offsetX > noteBounds.W)
                offsetX = noteBounds.W;

            float dy = noteBounds.H / CBase.Settings.GetNumNoteLines();
            SRectF drawRect = new SRectF(
                noteBounds.X - dy + offsetX,
                noteBounds.Y + dy * (CBase.Settings.GetNumNoteLines() - 1 - (tonePlayer - baseLine) / 2f),
                dy,
                dy,
                noteBounds.Z
                );

            SColorF color = new SColorF(
                _PlayerNotes[n].Color.R,
                _PlayerNotes[n].Color.G,
                _PlayerNotes[n].Color.B,
                _PlayerNotes[n].Color.A * _PlayerNotes[n].Alpha);

            STexture toneHelper = CBase.Theme.GetSkinTexture(_Theme.SkinToneHelperName, _PartyModeID);
            CBase.Drawing.DrawTexture(toneHelper, drawRect, color);


            while (tonePlayer - baseLine < 12)
                tonePlayer += 12;

            while (tonePlayer - baseLine > 24)
                tonePlayer -= 12;

            drawRect = new SRectF(
                noteBounds.X - dy + offsetX,
                noteBounds.Y + dy * (CBase.Settings.GetNumNoteLines() - 1 - (tonePlayer - baseLine) / 2f),
                dy,
                dy,
                noteBounds.Z
                );

            CBase.Drawing.DrawTexture(toneHelper, drawRect, color);
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY) {}

        public void ResizeElement(int stepW, int stepH) {}
        #endregion ThemeEdit
    }
}