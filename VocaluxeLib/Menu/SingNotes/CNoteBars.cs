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
using System.Drawing;
using System.Linq;
using VocaluxeLib.Draw;
using VocaluxeLib.Songs;

namespace VocaluxeLib.Menu.SingNotes
{
    public class CNoteBars
    {
        private readonly SThemeSingBar _Theme;
        private readonly int _PartyModeID;
        private readonly int _Player;
        public readonly SRectF Rect;
        private readonly SColorF _Color;
        public float Alpha = 1;
        private readonly CSongLine[] _Lines;

        /// <summary>
        ///     Height of one line (tone)
        /// </summary>
        private readonly float _NoteLineHeight;
        /// <summary>
        ///     Additional height of a note according to selected difficulty
        /// </summary>
        private readonly float _AddNoteHeight;

        private int _CurrentLine = -1;

        private readonly Stopwatch _Timer = new Stopwatch();
        private readonly List<CParticleEffect> _GoldenStars = new List<CParticleEffect>();
        private readonly List<CParticleEffect> _Flares = new List<CParticleEffect>();
        private readonly List<CParticleEffect> _PerfectNoteEffect = new List<CParticleEffect>();
        private readonly List<CParticleEffect> _PerfectLineTwinkle = new List<CParticleEffect>();

        public CNoteBars(int partyModeID, int player, SRectF rect, SThemeSingBar theme)
        {
            _Player = player;
            _Theme = theme;
            _PartyModeID = partyModeID;
            Rect = rect;

            _Color = CBase.Themes.GetPlayerColor(player + 1);

            SPlayer playerData = CBase.Game.GetPlayers()[player];
            _Lines = CBase.Game.GetSong().Notes.GetVoice(playerData.VoiceNr).Lines;
            _NoteLineHeight = Rect.H / CBase.Settings.GetNumNoteLines();
            _AddNoteHeight = _NoteLineHeight / 2f * (2f - (int)CBase.Profiles.GetDifficulty(playerData.ProfileID));
        }

        public void SetLine(int line)
        {
            if (_CurrentLine == line)
                return;
            _CurrentLine = line;
            _GoldenStars.Clear();
            _Flares.Clear();
            _PerfectNoteEffect.Clear();

            foreach (CSongNote note in _Lines[_CurrentLine].Notes.Where(note => note.Type == ENoteType.Golden))
                _AddGoldenNote(_GetNoteRect(note));
        }

        public void Draw()
        {
            if (_CurrentLine == -1 || _CurrentLine >= _Lines.Length)
                return;

            CSongLine line = _Lines[_CurrentLine];

            if (CBase.Config.GetDrawNoteLines() == EOffOn.TR_CONFIG_ON)
                _DrawNoteLines(new SColorF(Color.Gray, 0.5f * Alpha));

            float beats = line.LastNoteBeat - line.FirstNoteBeat + 1;

            var color = new SColorF(_Color, _Color.A * Alpha);

            float baseLine = line.BaseLine;
            foreach (CSongNote note in line.Notes)
            {
                if (note.Type != ENoteType.Freestyle)
                {
                    SRectF rect = _GetNoteRect(note);

                    _DrawNoteBG(rect, color);
                    _DrawNote(rect, new SColorF(Color.White, 0.7f * Alpha), 0.7f);
                }
            }

            if (CBase.Config.GetDrawToneHelper() == EOffOn.TR_CONFIG_ON)
                _DrawToneHelper((int)baseLine, (CBase.Game.GetMidRecordedBeat() - line.FirstNoteBeat) / beats * Rect.W);

            List<CSungLine> sungLines = CBase.Game.GetPlayers()[_Player].SungLines;
            if (_CurrentLine >= 0 && _CurrentLine < sungLines.Count)
            {
                foreach (CSungNote note in sungLines[_CurrentLine].Notes)
                {
                    SRectF rect = _GetNoteRect(note);
                    if (note.EndBeat == CBase.Game.GetRecordedBeat())
                        rect.W -= (1 - (CBase.Game.GetMidRecordedBeat() - CBase.Game.GetRecordedBeat())) * Rect.W / beats;

                    float factor = (note.Hit) ? 0.7f : 0.4f;

                    _DrawNote(rect, color, factor);

                    if (note.EndBeat >= CBase.Game.GetRecordedBeat() && note.Hit && note.HitNote.Type == ENoteType.Golden)
                    {
                        SRectF re = rect;
                        re.W = (CBase.Game.GetMidRecordedBeat() - note.StartBeat) / beats * Rect.W;
                        _AddFlare(re);
                    }

                    if (note.Perfect && !note.PerfectDrawn && note.EndBeat < CBase.Game.GetRecordedBeat())
                    {
                        _AddPerfectNote(rect);
                        note.PerfectDrawn = true;
                    }
                }
            }

            if (_CurrentLine > 0 && sungLines.Count >= _CurrentLine && sungLines[_CurrentLine - 1].PerfectLine)
            {
                _AddPerfectLine();
                sungLines[_CurrentLine - 1].PerfectLine = false;
            }

            _Flares.RemoveAll(el => !el.IsAlive);
            _PerfectNoteEffect.RemoveAll(el => !el.IsAlive);
            _PerfectLineTwinkle.RemoveAll(el => !el.IsAlive);

            foreach (CParticleEffect perfline in _PerfectLineTwinkle)
                perfline.Draw();

            foreach (CParticleEffect stars in _GoldenStars)
            {
                stars.Alpha = Alpha;
                stars.Draw();
            }

            foreach (CParticleEffect flare in _Flares)
                flare.Draw();

            foreach (CParticleEffect perfnote in _PerfectNoteEffect)
                perfnote.Draw();
        }

        private SRectF _GetNoteRect(CBaseNote note)
        {
            CSongLine line = _Lines[_CurrentLine];
            float beats = line.LastNoteBeat - line.FirstNoteBeat + 1;

            float width = note.Duration * Rect.W / beats;

            var noteRect = new SRectF(
                Rect.X + (note.StartBeat - line.FirstNoteBeat) * Rect.W / beats,
                Rect.Y + (CBase.Settings.GetNumNoteLines() - 1 - (note.Tone - line.BaseLine) / 2f) * _NoteLineHeight - _AddNoteHeight / 2,
                width,
                _NoteLineHeight + _AddNoteHeight,
                Rect.Z
                );
            return noteRect;
        }

        private void _DrawNoteLines(SColorF color)
        {
            SRectF lineRect = Rect;
            lineRect.H = 1.5f;
            for (int i = 0; i < CBase.Settings.GetNumNoteLines(); i++)
            {
                lineRect.Y = Rect.Y + Rect.H / CBase.Settings.GetNumNoteLines() * (i + 1);
                CBase.Drawing.DrawRect(color, lineRect);
            }
        }

        private void _DrawToneHelper(int baseLine, float offsetX)
        {
            int tonePlayer = CBase.Record.GetToneAbs(_Player);

            while (tonePlayer - baseLine < 0)
                tonePlayer += 12;

            while (tonePlayer - baseLine > 12)
                tonePlayer -= 12;

            if (offsetX < 0f)
                offsetX = 0f;

            if (offsetX > Rect.W)
                offsetX = Rect.W;

            var drawRect = new SRectF(
                Rect.X - _NoteLineHeight + offsetX,
                Rect.Y + _NoteLineHeight * (CBase.Settings.GetNumNoteLines() - 1 - (tonePlayer - baseLine) / 2f),
                _NoteLineHeight,
                _NoteLineHeight,
                Rect.Z
                );

            var color = new SColorF(_Color, _Color.A * Alpha);

            CTextureRef toneHelper = CBase.Themes.GetSkinTexture(_Theme.SkinToneHelper, _PartyModeID);
            CBase.Drawing.DrawTexture(toneHelper, drawRect, color);
        }

        private void _DrawNote(SRectF rect, SColorF color, float factor)
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

        private void _DrawNoteBG(SRectF noteRect, SColorF color)
        {
            const float period = 1500; //[ms]

            if (!_Timer.IsRunning)
                _Timer.Start();

            if (_Timer.ElapsedMilliseconds > period)
                _Timer.Restart();

            float alpha = (float)(Math.Cos(_Timer.ElapsedMilliseconds / period * Math.PI * 2) + 1) / 4 + 0.5f;

            CTextureRef noteBackgroundBegin = CBase.Themes.GetSkinTexture(_Theme.SkinBackgroundLeft, _PartyModeID);
            CTextureRef noteBackgroundMiddle = CBase.Themes.GetSkinTexture(_Theme.SkinBackgroundMiddle, _PartyModeID);
            CTextureRef noteBackgroundEnd = CBase.Themes.GetSkinTexture(_Theme.SkinBackgroundRight, _PartyModeID);

            float dx = noteRect.H * noteBackgroundBegin.OrigAspect;
            if (2 * dx > noteRect.W)
                dx = noteRect.W / 2;

            var col = new SColorF(color, color.A * alpha);

            CBase.Drawing.DrawTexture(noteBackgroundBegin, new SRectF(noteRect.X, noteRect.Y, dx, noteRect.H, noteRect.Z), col);

            if (noteRect.W - 2 * dx >= 2 * dx)
                CBase.Drawing.DrawTexture(noteBackgroundMiddle, new SRectF(noteRect.X + dx, noteRect.Y, noteRect.W - 2 * dx, noteRect.H, noteRect.Z), col);
            else
            {
                CBase.Drawing.DrawTexture(noteBackgroundMiddle, new SRectF(noteRect.X + dx, noteRect.Y, 2 * dx, noteRect.H, noteRect.Z), col,
                                          new SRectF(noteRect.X + dx, noteRect.Y, noteRect.W - 2 * dx, noteRect.H, noteRect.Z));
            }

            CBase.Drawing.DrawTexture(noteBackgroundEnd, new SRectF(noteRect.X + noteRect.W - dx, noteRect.Y, dx, noteRect.H, noteRect.Z), col);
        }

        private void _AddGoldenNote(SRectF noteRect)
        {
            var numstars = (int)(noteRect.W * 0.25f);
            var stars = new CParticleEffect(_PartyModeID, numstars, new SColorF(Color.Yellow), noteRect, _Theme.SkinGoldenStar, 20, EParticleType.Star);
            _GoldenStars.Add(stars);
        }

        private void _AddFlare(SRectF noteRect)
        {
            var rect = new SRectF(noteRect.Right, noteRect.Y, 0f, noteRect.H, noteRect.Z);

            var flares = new CParticleEffect(_PartyModeID, 15, new SColorF(Color.White), rect, _Theme.SkinGoldenStar, 20, EParticleType.Flare);
            _Flares.Add(flares);
        }

        private void _AddPerfectNote(SRectF noteRect)
        {
            CTextureRef noteBegin = CBase.Themes.GetSkinTexture(_Theme.SkinRight, _PartyModeID);
            float dx = noteRect.H * noteBegin.OrigAspect;
            if (2 * dx > noteRect.W)
                dx = noteRect.W / 2;

            SRectF r = new SRectF(noteRect.Right - dx, noteRect.Y, dx * 0.5f, dx * 0.2f, noteRect.Z);

            var stars = new CParticleEffect(_PartyModeID, CBase.Game.GetRandom(2) + 1, new SColorF(Color.White), r, _Theme.SkinPerfectNoteStart, 35,
                                            EParticleType.PerfNoteStar);
            _PerfectNoteEffect.Add(stars);
        }

        private void _AddPerfectLine()
        {
            var twinkle = new CParticleEffect(_PartyModeID, 200, _Color, Rect, _Theme.SkinGoldenStar, 25, EParticleType.Twinkle);
            _PerfectLineTwinkle.Add(twinkle);
        }
    }
}