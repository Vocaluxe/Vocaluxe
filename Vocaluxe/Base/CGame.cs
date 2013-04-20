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
using Vocaluxe.GameModes;
using VocaluxeLib.Menu;
using VocaluxeLib.Menu.SingNotes;
using VocaluxeLib.Menu.SongMenu;

namespace Vocaluxe.Base
{
    static class CGame
    {
        private static IGameMode _GameMode;
        private static int _NumPlayer = CConfig.NumPlayer;

        private static int _OldBeatD;
        private static readonly Random _Rand = new Random();

        public static Random Rand
        {
            get { return _Rand; }
        }

        public static float GetBeatFromTime(float time, float bpm, float gap)
        {
            return bpm / 60 * (time - gap);
        }

        public static float GetTimeFromBeats(float beats, float bpm)
        {
            if (bpm > 0f)
                return beats / bpm * 60f;

            return 0f;
        }

        public static int CurrentBeat { get; private set; }

        public static float Beat { get; private set; }

        public static float MidBeatD { get; private set; }

        public static int ActBeatD { get; private set; }

        public static SPlayer[] Player { get; private set; }

        public static void Init()
        {
            _GameMode = new CGameModeNormal();
            _GameMode.Init();
            Player = new SPlayer[CSettings.MaxNumPlayer];
            ResetPlayer();

            CConfig.UsePlayers();
        }

        public static void EnterNormalGame()
        {
            _GameMode = new CGameModeNormal();
            _GameMode.Init();
        }

        public static EGameMode GameMode
        {
            get { return _GameMode.GetCurrentGameMode(); }
        }

        public static bool AddVisibleSong(int visibleIndex, EGameMode gameMode)
        {
            return _GameMode.AddVisibleSong(visibleIndex, gameMode);
        }

        public static bool AddSong(int absoluteIndex, EGameMode gameMode)
        {
            return _GameMode.AddSong(absoluteIndex, gameMode);
        }

        public static bool RemoveVisibleSong(int visibleIndex)
        {
            return _GameMode.RemoveVisibleSong(visibleIndex);
        }

        public static bool RemoveSong(int absoluteIndex)
        {
            return _GameMode.RemoveSong(absoluteIndex);
        }

        public static void ClearSongs()
        {
            _GameMode.ClearSongs();
        }

        public static void Reset()
        {
            _GameMode.Reset();
        }

        public static void Start()
        {
            _GameMode.Start(Player);
        }

        public static void NextRound()
        {
            _GameMode.NextRound(Player);
        }

        public static bool IsFinished()
        {
            return _GameMode.IsFinished();
        }

        public static int RoundNr
        {
            get { return _GameMode.GetCurrentRoundNr(); }
        }

        public static CSong GetSong()
        {
            return _GameMode.GetSong();
        }

        public static CSong GetSong(int num)
        {
            return _GameMode.GetSong(num);
        }

        public static EGameMode GetGameMode(int num)
        {
            return _GameMode.GetGameMode(num);
        }

        public static int GetNumSongs()
        {
            return _GameMode.GetNumSongs();
        }

        public static CPoints GetPoints()
        {
            return _GameMode.GetPoints();
        }

        public static int NumRounds
        {
            get { return _GameMode.GetNumSongs(); }
        }

        public static int NumPlayer
        {
            get { return _NumPlayer; }
            set
            {
                if (value > 0 && value <= CSettings.MaxNumPlayer)
                    _NumPlayer = value;
            }
        }

        public static void ResetPlayer()
        {
            for (int i = 0; i < Player.Length; i++)
            {
                Player[i].Points = 0f;
                Player[i].PointsLineBonus = 0f;
                Player[i].PointsGoldenNotes = 0f;
                Player[i].LineNr = 0;
                Player[i].NoteDiff = 0;
                Player[i].SingLine = new List<CLine>();
                Player[i].CurrentLine = -1;
                Player[i].CurrentNote = -1;
                Player[i].SongID = -1;
                Player[i].Medley = false;
                Player[i].Duet = false;
                Player[i].ShortSong = false;
                Player[i].DateTicks = DateTime.Now.Ticks;
                Player[i].SongFinished = false;
            }
            _OldBeatD = -100;
            Beat = -100;
            CurrentBeat = -100;
            ActBeatD = -100;
        }

        public static void UpdatePoints(float time)
        {
            CSong song = _GameMode.GetSong();

            if (song == null)
                return;

            float b = GetBeatFromTime(time, song.BPM, song.Gap);
            if (b <= Beat)
                return;

            Beat = b;
            CurrentBeat = (int)Math.Floor(Beat);

            MidBeatD = -0.5f + GetBeatFromTime(time, song.BPM, song.Gap + CConfig.MicDelay / 1000f);
            ActBeatD = (int)Math.Floor(MidBeatD);

            for (int p = 0; p < _NumPlayer; p++)
                CSound.AnalyzeBuffer(p);

            if (_OldBeatD >= ActBeatD)
                return;

            for (int p = 0; p < _NumPlayer; p++)
            {
                for (int beat = _OldBeatD + 1; beat <= ActBeatD; beat++)
                {
                    if ((_GameMode.GetCurrentGameMode() == EGameMode.TR_GAMEMODE_MEDLEY && song.Medley.EndBeat == beat) ||
                        (_GameMode.GetCurrentGameMode() == EGameMode.TR_GAMEMODE_SHORTSONG && song.ShortEnd == beat))
                        Player[p].SongFinished = true;

                    CLine[] lines = song.Notes.GetLines(Player[p].LineNr).Line;
                    int line = -1;

                    for (int j = 0; j < lines.Length; j++)
                    {
                        if (beat >= lines[j].StartBeat && beat <= lines[j].EndBeat)
                        {
                            line = j;
                            break;
                        }
                    }

                    if (line < 0)
                        continue;
                    if (line != Player[p].CurrentLine)
                        Player[p].CurrentNote = -1;

                    Player[p].CurrentLine = line;

                    while (Player[p].SingLine.Count <= line)
                        Player[p].SingLine.Add(new CLine());

                    CNote[] notes = lines[line].Notes;
                    int note = -1;
                    for (int j = 0; j < notes.Length; j++)
                    {
                        if (beat >= notes[j].StartBeat && beat <= notes[j].EndBeat)
                        {
                            note = j;
                            break;
                        }
                    }

                    if (note >= 0)
                    {
                        Player[p].CurrentNote = note;

                        if (line == lines.Length - 1)
                        {
                            if (note == lines[line].NoteCount - 1)
                            {
                                if (notes[note].EndBeat == beat)
                                    Player[p].SongFinished = true;
                            }
                        }

                        if (notes[note].PointsForBeat > 0 && CSound.RecordToneValid(p))
                        {
                            int tone = notes[note].Tone;
                            int tonePlayer = CSound.RecordGetTone(p);

                            while (tonePlayer - tone > 6)
                                tonePlayer -= 12;

                            while (tonePlayer - tone < -6)
                                tonePlayer += 12;

                            Player[p].NoteDiff = Math.Abs(tone - tonePlayer);
                            bool hit = Player[p].NoteDiff <= (2 - (int)Player[p].Difficulty);
                            if (hit)
                            {
                                // valid
                                //CSound.RecordSetTone(p, Tone);
                                double points = (CSettings.MaxScore - CSettings.LinebonusScore) * (double)notes[note].PointsForBeat /
                                                song.Notes.GetLines(Player[p].LineNr).Points;
                                if (notes[note].NoteType == ENoteType.Golden)
                                    Player[p].PointsGoldenNotes += points;

                                Player[p].Points += points;

                                // update player notes (sung notes)
                                if (Player[p].SingLine[line].NoteCount > 0)
                                {
                                    CNote nt = Player[p].SingLine[line].LastNote;
                                    if (notes[note].StartBeat == beat || nt.EndBeat + 1 != beat || nt.Tone != tone || !nt.Hit)
                                        Player[p].SingLine[line].AddNote(new CNote(beat, 1, tone, String.Empty, true, notes[note].NoteType));
                                    else
                                        Player[p].SingLine[line].IncLastNoteLength();
                                }
                                else
                                    Player[p].SingLine[line].AddNote(new CNote(beat, 1, tone, String.Empty, true, notes[note].NoteType));

                                Player[p].SingLine[line].LastNote.IsPerfect(notes[note]);
                                Player[p].SingLine[line].IsPerfect(lines[line]);
                            }
                            else
                            {
                                if (Player[p].SingLine[line].NoteCount > 0)
                                {
                                    CNote nt = Player[p].SingLine[line].LastNote;
                                    if (nt.Tone == tonePlayer && nt.EndBeat + 1 == beat && !nt.Hit)
                                        Player[p].SingLine[line].IncLastNoteLength();
                                    else
                                        Player[p].SingLine[line].AddNote(new CNote(beat, 1, tonePlayer, String.Empty, false, ENoteType.Freestyle));
                                }
                                else
                                    Player[p].SingLine[line].AddNote(new CNote(beat, 1, tonePlayer, String.Empty, false, ENoteType.Freestyle));
                            }
                        }

                        // Line Bonus
                        int numLinesWithPoints = song.Notes.GetNumLinesWithPoints(Player[p].LineNr);
                        if (note == lines[line].NoteCount - 1 && numLinesWithPoints > 0)
                        {
                            if (notes[note].EndBeat == beat && lines[line].Points > 0f)
                            {
                                double factor = Player[p].SingLine[line].Points / (double)lines[line].Points;
                                if (factor < 0.4)
                                    factor = 0.0;
                                else if (factor > 0.9)
                                    factor = 1.0;
                                else
                                {
                                    factor -= 0.4;
                                    factor *= 2;
                                    factor *= factor;
                                }

                                double points = CSettings.LinebonusScore * factor * 1f / numLinesWithPoints;
                                Player[p].Points += points;
                                Player[p].PointsLineBonus += points;
                            }
                        }
                    }
                }
            }
            _OldBeatD = ActBeatD;
        }
    }
}