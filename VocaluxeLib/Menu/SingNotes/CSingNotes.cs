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
using System.Xml.Serialization;
using VocaluxeLib.Draw;
using VocaluxeLib.Songs;
using VocaluxeLib.Xml;

namespace VocaluxeLib.Menu.SingNotes
{
    struct SPlayerNotes
    {
        public int ID;
        public int PlayerNr;
        public SRectF Rect;
        public SColorF Color;
        public float Alpha;
        public CSongLine[] Lines;
        public int LineNr;
        public Stopwatch Timer;
        public List<CParticleEffect> GoldenStars;
        public List<CParticleEffect> Flares;
        public List<CParticleEffect> PerfectNoteEffect;
        public List<CParticleEffect> PerfectLineTwinkle;
    }

    [XmlType("Position")]
    public struct SBarPosition
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;
        public SRectF Rect;
    }

    [XmlType("SingBar")]
    public struct SThemeSingBar
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;

        public string SkinLeft;
        public string SkinMiddle;
        public string SkinRight;

        public string SkinBackgroundLeft;
        public string SkinBackgroundMiddle;
        public string SkinBackgroundRight;

        public string SkinGoldenStar;
        public string SkinToneHelper;
        public string SkinPerfectNoteStart;

        [XmlArray("BarPositions")] public SBarPosition[] BarPos;
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
            _Theme.BarPos = new SBarPosition[CHelper.Sum(CBase.Settings.GetMaxNumPlayer())];
            _ThemeLoaded = false;

            _PlayerNotes = new List<SPlayerNotes>();
            _ActID = 0;
        }

        protected CSingNotes(SThemeSingBar theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Theme = theme;

            _PlayerNotes = new List<SPlayerNotes>();
            _ActID = 0;

            BarPos = new SRectF[CBase.Settings.GetMaxNumPlayer(),CBase.Settings.GetMaxNumPlayer()];

            LoadTextures();
        }

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool ThemeLoaded
        {
            get { return _ThemeLoaded; }
        }

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinLeft", out _Theme.SkinLeft, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinMiddle", out _Theme.SkinMiddle, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinRight", out _Theme.SkinRight, String.Empty);

            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinBackgroundLeft", out _Theme.SkinBackgroundLeft, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinBackgroundMiddle", out _Theme.SkinBackgroundMiddle, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinBackgroundRight", out _Theme.SkinBackgroundRight, String.Empty);

            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinGoldenStar", out _Theme.SkinGoldenStar, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinToneHelper", out _Theme.SkinToneHelper, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/SkinPerfectNoteStar", out _Theme.SkinPerfectNoteStart, String.Empty);

            int i = 0;
            BarPos = new SRectF[CBase.Settings.GetMaxNumPlayer(),CBase.Settings.GetMaxNumPlayer()];
            for (int numplayer = 0; numplayer < CBase.Settings.GetMaxNumPlayer(); numplayer++)
            {
                for (int player = 0; player <= numplayer; player++)
                {
                    BarPos[player, numplayer] = new SRectF();
                    string target = "/BarPositions/P" + (player + 1) + "N" + (numplayer + 1);
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "X", ref BarPos[player, numplayer].X);
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "Y", ref BarPos[player, numplayer].Y);
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "Z", ref BarPos[player, numplayer].Z);
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "W", ref BarPos[player, numplayer].W);
                    _ThemeLoaded &= xmlReader.TryGetFloatValue(item + target + "H", ref BarPos[player, numplayer].H);
                    _Theme.BarPos[i].Name = "P" + (player + 1) + "N" + (numplayer + 1);
                    _Theme.BarPos[i].Rect = new SRectF(BarPos[player, numplayer]);
                    i++;
                }
            }

            if (_ThemeLoaded)
            {
                _Theme.Name = elementName;
                LoadTextures();
            }

            return _ThemeLoaded;
        }

        public void Reset()
        {
            _PlayerNotes.Clear();
            _ActID = 0;
        }

        public int AddPlayer(SRectF rect, SColorF color, int playerNr)
        {
            var notes = new SPlayerNotes
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

        public void SetLines(int id, CSongLine[] lines, int lineNr)
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

        public void Draw(int id, List<CSungLine> sungLines, int player)
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

            CSongLine line = _PlayerNotes[n].Lines[_PlayerNotes[n].LineNr];

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

            var color = new SColorF(
                _PlayerNotes[n].Color.R,
                _PlayerNotes[n].Color.G,
                _PlayerNotes[n].Color.B,
                _PlayerNotes[n].Color.A * _PlayerNotes[n].Alpha);

            float baseLine = line.BaseLine;
            int nr = 1;
            foreach (CSongNote note in line.Notes)
            {
                if (note.Type != ENoteType.Freestyle)
                {
                    float width = note.Duration / beats * w;

                    var rect = new SRectF(
                        _PlayerNotes[n].Rect.X + (note.StartBeat - line.FirstNoteBeat) / beats * w,
                        _PlayerNotes[n].Rect.Y + (CBase.Settings.GetNumNoteLines() - 1 - (note.Tone - baseLine) / 2) / CBase.Settings.GetNumNoteLines() * h - dh,
                        width,
                        h / CBase.Settings.GetNumNoteLines() + 2 * dh,
                        _PlayerNotes[n].Rect.Z
                        );

                    _DrawNoteBG(rect, color, 1f, _PlayerNotes[n].Timer);
                    _DrawNote(rect, new SColorF(1f, 1f, 1f, 0.7f * _PlayerNotes[n].Alpha), 0.7f);

                    if (note.Type == ENoteType.Golden)
                    {
                        _AddGoldenNote(rect, n, nr);
                        nr++;
                    }
                }
            }

            if (CBase.Config.GetDrawToneHelper() == EOffOn.TR_CONFIG_ON)
                _DrawToneHelper(n, (int)baseLine, (CBase.Game.GetMidRecordedBeat() - line.FirstNoteBeat) / beats * w);

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

            if (sungLines == null || sungLines.Count == 0 || CBase.Game.GetPlayers()[player].CurrentLine == -1 || sungLines.Count <= CBase.Game.GetPlayers()[player].CurrentLine)
            {
                foreach (CParticleEffect stars in _PlayerNotes[n].GoldenStars)
                {
                    stars.Alpha = _PlayerNotes[n].Alpha;
                    stars.Draw();
                }
                return;
            }

            foreach (CSungNote note in sungLines[CBase.Game.GetPlayers()[player].CurrentLine].Notes)
            {
                float width = note.Duration / beats * w;

                if (note.EndBeat == CBase.Game.GetRecordedBeat())
                    width -= (1 - (CBase.Game.GetMidRecordedBeat() - CBase.Game.GetRecordedBeat())) / beats * w;

                var rect = new SRectF(
                    _PlayerNotes[n].Rect.X + (note.StartBeat - line.FirstNoteBeat) / beats * w,
                    _PlayerNotes[n].Rect.Y + (CBase.Settings.GetNumNoteLines() - 1 - (note.Tone - baseLine) / 2) / CBase.Settings.GetNumNoteLines() * h - dh,
                    width,
                    h / CBase.Settings.GetNumNoteLines() + 2 * dh,
                    _PlayerNotes[n].Rect.Z
                    );

                float f = (note.Hit) ? 0.7f : 0.4f;

                _DrawNote(rect, color, f);

                if (note.EndBeat >= CBase.Game.GetRecordedBeat() && note.Hit && note.HitNote.Type == ENoteType.Golden)
                {
                    var re = new SRectF(rect) {W = (CBase.Game.GetMidRecordedBeat() - note.StartBeat) / beats * w};
                    _AddFlare(re, n);
                }

                if (note.Perfect && !note.PerfectDrawn && note.EndBeat < CBase.Game.GetRecordedBeat())
                {
                    _AddPerfectNote(rect, n);
                    note.PerfectDrawn = true;
                }
            }

            int currentLine = CBase.Game.GetPlayers()[player].SungLines.Count - 1;
            if (currentLine > 0)
            {
                if (CBase.Game.GetPlayers()[player].SungLines[currentLine - 1].PerfectLine)
                {
                    _AddPerfectLine(n);
                    CBase.Game.GetPlayers()[player].SungLines[currentLine - 1].PerfectLine = false;
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

        public void LoadTextures()
        {
            foreach (SBarPosition bp in _Theme.BarPos)
            {
                int n = Int32.Parse(bp.Name.Substring(3, 1)) - 1;
                int p = Int32.Parse(bp.Name.Substring(1, 1)) - 1;

                BarPos[p, n] = bp.Rect;
            }
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        public SThemeSingBar GetTheme()
        {
            return _Theme;
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
            if (factor <= 0)
                return;

            float dh = (1f - factor) * rect.H / 2;
            float dw = Math.Min(dh, rect.W / 2);

            var noteRect = new SRectF(rect.X + dw, rect.Y + dh, rect.W - 2 * dw, rect.H - 2 * dh, rect.Z);

            CTextureRef noteBegin = CBase.Themes.GetSkinTexture(_Theme.SkinLeft, _PartyModeID);
            CTextureRef noteMiddle = CBase.Themes.GetSkinTexture(_Theme.SkinMiddle, _PartyModeID);
            CTextureRef noteEnd = CBase.Themes.GetSkinTexture(_Theme.SkinRight, _PartyModeID);

            //Width of each of the ends (round parts)
            //Need 2 of them so use minimum
            float endsW = Math.Min(noteRect.H * noteBegin.OrigAspect, noteRect.W / 2);

            CBase.Drawing.DrawTexture(noteBegin, new SRectF(noteRect.X, noteRect.Y, endsW, noteRect.H, noteRect.Z), color);

            var middleRect = new SRectF(noteRect.X + endsW, noteRect.Y, noteRect.W - 2 * endsW, noteRect.H, noteRect.Z);
            if (noteRect.W >= 4 * endsW)
                CBase.Drawing.DrawTexture(noteMiddle, middleRect, color);
            else
                CBase.Drawing.DrawTexture(noteMiddle, new SRectF(middleRect.X, middleRect.Y, 2 * endsW, middleRect.H, middleRect.Z), color, middleRect);

            CBase.Drawing.DrawTexture(noteEnd, new SRectF(noteRect.X + noteRect.W - endsW, noteRect.Y, endsW, noteRect.H, noteRect.Z), color);
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

            var r = new SRectF(
                rect.X + dw + spacing,
                rect.Y + d + spacing,
                rect.W - 2 * dw - 2 * spacing,
                rect.H - 2 * d - 2 * spacing,
                rect.Z
                );

            CTextureRef noteBackgroundBegin = CBase.Themes.GetSkinTexture(_Theme.SkinBackgroundLeft, _PartyModeID);
            CTextureRef noteBackgroundMiddle = CBase.Themes.GetSkinTexture(_Theme.SkinBackgroundMiddle, _PartyModeID);
            CTextureRef noteBackgroundEnd = CBase.Themes.GetSkinTexture(_Theme.SkinBackgroundRight, _PartyModeID);

            float dx = r.H * noteBackgroundBegin.OrigAspect;
            if (2 * dx > r.W)
                dx = r.W / 2;

            var col = new SColorF(color.R, color.G, color.B, color.A * alpha);

            CBase.Drawing.DrawTexture(noteBackgroundBegin, new SRectF(r.X, r.Y, dx, r.H, r.Z), col);

            if (r.W - 2 * dx >= 2 * dx)
                CBase.Drawing.DrawTexture(noteBackgroundMiddle, new SRectF(r.X + dx, r.Y, r.W - 2 * dx, r.H, r.Z), col);
            else
                CBase.Drawing.DrawTexture(noteBackgroundMiddle, new SRectF(r.X + dx, r.Y, 2 * dx, r.H, r.Z), col, new SRectF(r.X + dx, r.Y, r.W - 2 * dx, r.H, r.Z));

            CBase.Drawing.DrawTexture(noteBackgroundEnd, new SRectF(r.X + r.W - dx, r.Y, dx, r.H, r.Z), col);
        }

        private static void _DrawNoteLines(SRectF rect, SColorF color)
        {
            SRectF lineRect = new SRectF(rect) {H = 1.5f};
            for (int i = 0; i < CBase.Settings.GetNumNoteLines(); i++)
            {
                lineRect.Y = rect.Y + rect.H / CBase.Settings.GetNumNoteLines() * (i + 1);
                CBase.Drawing.DrawRect(color, lineRect);
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

                var r = new SRectF(
                    rect.X + dw + spacing,
                    rect.Y + d + spacing,
                    rect.W - 2 * dw - 2 * spacing,
                    rect.H - 2 * d - 2 * spacing,
                    rect.Z
                    );

                var numstars = (int)(r.W * 0.25f);
                var stars = new CParticleEffect(_PartyModeID, numstars, new SColorF(1f, 1f, 0f, 1f), r, _Theme.SkinGoldenStar, 20, EParticleType.Star);
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

            var r = new SRectF(
                rect.X + dw + spacing + rect.W - 2 * dw - 2 * spacing,
                rect.Y + d + spacing,
                0f,
                rect.H - 2 * d - 2 * spacing,
                rect.Z
                );

            var flares = new CParticleEffect(_PartyModeID, 15, new SColorF(1f, 1f, 1f, 1f), r, _Theme.SkinGoldenStar, 20, EParticleType.Flare);
            _PlayerNotes[n].Flares.Add(flares);
        }

        private void _AddPerfectNote(SRectF rect, int n, float factor = 1f)
        {
            const int spacing = 0;

            float d = (1f - factor) / 2 * rect.H;
            float dw = d;

            if (2 * dw > rect.W)
                dw = rect.W / 2;

            var r = new SRectF(
                rect.X + dw + spacing,
                rect.Y + d + spacing,
                rect.W - 2 * dw - 2 * spacing,
                rect.H - 2 * d - 2 * spacing,
                rect.Z
                );

            CTextureRef noteBegin = CBase.Themes.GetSkinTexture(_Theme.SkinLeft, _PartyModeID);
            float dx = r.H * noteBegin.OrigAspect;
            if (2 * dx > r.W)
                dx = r.W / 2;

            r = new SRectF(
                r.X + r.W - dx,
                r.Y,
                dx * 0.5f,
                dx * 0.2f,
                rect.Z
                );

            var stars = new CParticleEffect(_PartyModeID, CBase.Game.GetRandom(2) + 1, new SColorF(1f, 1f, 1f, 1f), r, _Theme.SkinPerfectNoteStart, 35,
                                            EParticleType.PerfNoteStar);
            _PlayerNotes[n].PerfectNoteEffect.Add(stars);
        }

        private void _AddPerfectLine(int n)
        {
            var twinkle = new CParticleEffect(_PartyModeID, 200, _PlayerNotes[n].Color, _PlayerNotes[n].Rect, _Theme.SkinGoldenStar, 25, EParticleType.Twinkle);
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
            var drawRect = new SRectF(
                noteBounds.X - dy + offsetX,
                noteBounds.Y + dy * (CBase.Settings.GetNumNoteLines() - 1 - (tonePlayer - baseLine) / 2f),
                dy,
                dy,
                noteBounds.Z
                );

            var color = new SColorF(
                _PlayerNotes[n].Color.R,
                _PlayerNotes[n].Color.G,
                _PlayerNotes[n].Color.B,
                _PlayerNotes[n].Color.A * _PlayerNotes[n].Alpha);

            CTextureRef toneHelper = CBase.Themes.GetSkinTexture(_Theme.SkinToneHelper, _PartyModeID);
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