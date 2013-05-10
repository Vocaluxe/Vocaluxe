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
using Vocaluxe.SongQueue;
using VocaluxeLib;
using VocaluxeLib.Game;
using VocaluxeLib.Songs;

namespace Vocaluxe.Base
{
    static class CGame
    {
        private static ISongQueue _SongQueue;
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

        public static SPlayer[] Players { get; private set; }

        public static void Init()
        {
            _SongQueue = new CSongQueue();
            _SongQueue.Init();
            Players = new SPlayer[CSettings.MaxNumPlayer];
            ResetPlayer();
        }

        public static EGameMode GameMode
        {
            get { return _SongQueue.GetCurrentGameMode(); }
        }

        public static bool AddVisibleSong(int visibleIndex, EGameMode gameMode)
        {
            return _SongQueue.AddVisibleSong(visibleIndex, gameMode);
        }

        public static bool AddSong(int absoluteIndex, EGameMode gameMode)
        {
            return _SongQueue.AddSong(absoluteIndex, gameMode);
        }

        public static bool RemoveVisibleSong(int visibleIndex)
        {
            return _SongQueue.RemoveVisibleSong(visibleIndex);
        }

        public static bool RemoveSong(int absoluteIndex)
        {
            return _SongQueue.RemoveSong(absoluteIndex);
        }

        public static void ClearSongs()
        {
            _SongQueue.ClearSongs();
        }

        public static void Reset()
        {
            _SongQueue.Reset();
        }

        public static void Start()
        {
            _SongQueue.Start(Players);
        }

        public static void NextRound()
        {
            _SongQueue.NextRound(Players);
        }

        public static bool IsFinished()
        {
            return _SongQueue.IsFinished();
        }

        public static int RoundNr
        {
            get { return _SongQueue.GetCurrentRoundNr(); }
        }

        public static CSong GetSong()
        {
            return _SongQueue.GetSong();
        }

        public static CSong GetSong(int num)
        {
            return _SongQueue.GetSong(num);
        }

        public static EGameMode GetGameMode(int num)
        {
            return _SongQueue.GetGameMode(num);
        }

        public static int GetNumSongs()
        {
            return _SongQueue.GetNumSongs();
        }

        public static CPoints GetPoints()
        {
            return _SongQueue.GetPoints();
        }

        public static int NumRounds
        {
            get { return _SongQueue.GetNumSongs(); }
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
            for (int i = 0; i < Players.Length; i++)
            {
                Players[i].Points = 0f;
                Players[i].PointsLineBonus = 0f;
                Players[i].PointsGoldenNotes = 0f;
                Players[i].LineNr = 0;
                Players[i].NoteDiff = 0;
                Players[i].SingLine = new List<CLine>();
                Players[i].CurrentLine = -1;
                Players[i].CurrentNote = -1;
                Players[i].SongID = -1;
                Players[i].Medley = false;
                Players[i].Duet = false;
                Players[i].ShortSong = false;
                Players[i].DateTicks = DateTime.Now.Ticks;
                Players[i].SongFinished = false;
            }
            _OldBeatD = -100;
            Beat = -100;
            CurrentBeat = -100;
            ActBeatD = -100;
        }

        public static void UpdatePoints(float time)
        {
            CSong song = _SongQueue.GetSong();

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
                    if ((_SongQueue.GetCurrentGameMode() == EGameMode.TR_GAMEMODE_MEDLEY && song.Medley.EndBeat == beat) ||
                        (_SongQueue.GetCurrentGameMode() == EGameMode.TR_GAMEMODE_SHORTSONG && song.ShortEnd == beat))
                        Players[p].SongFinished = true;

                    CLine[] lines = song.Notes.GetVoice(Players[p].LineNr).Lines;
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
                    if (line != Players[p].CurrentLine)
                        Players[p].CurrentNote = -1;

                    Players[p].CurrentLine = line;

                    while (Players[p].SingLine.Count <= line)
                        Players[p].SingLine.Add(new CLine());

                    CNote[] notes = lines[line].Notes;
                    int note = lines[line].FindPreviousNote(beat);
                    if (note >= 0 && notes[note].EndBeat < beat)
                        note = -1;

                    if (note >= 0)
                    {
                        Players[p].CurrentNote = note;

                        if (line == lines.Length - 1)
                        {
                            if (note == lines[line].NoteCount - 1)
                            {
                                if (notes[note].EndBeat == beat)
                                    Players[p].SongFinished = true;
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

                            Players[p].NoteDiff = Math.Abs(tone - tonePlayer);
                            bool hit = Players[p].NoteDiff <= (2 - (int)CProfiles.GetDifficulty(Players[p].ProfileID));
                            if (hit)
                            {
                                // valid
                                //CSound.RecordSetTone(p, Tone);
                                double points = (CSettings.MaxScore - CSettings.LinebonusScore) * (double)notes[note].PointsForBeat /
                                                song.Notes.GetVoice(Players[p].LineNr).Points;
                                if (notes[note].NoteType == ENoteType.Golden)
                                    Players[p].PointsGoldenNotes += points;

                                Players[p].Points += points;

                                // update player notes (sung notes)
                                if (Players[p].SingLine[line].NoteCount > 0)
                                {
                                    CNote lastNote = Players[p].SingLine[line].LastNote;
                                    if (notes[note].StartBeat == beat || lastNote.EndBeat + 1 != beat || lastNote.Tone != tone || !lastNote.Hit)
                                        Players[p].SingLine[line].AddNote(new CNote(beat, 1, tone, String.Empty, true, notes[note].NoteType));
                                    else
                                        Players[p].SingLine[line].IncLastNoteLength();
                                }
                                else
                                    Players[p].SingLine[line].AddNote(new CNote(beat, 1, tone, String.Empty, true, notes[note].NoteType));

                                Players[p].SingLine[line].LastNote.IsPerfect(notes[note]);
                                Players[p].SingLine[line].IsPerfect(lines[line]);
                            }
                            else
                            {
                                if (Players[p].SingLine[line].NoteCount > 0)
                                {
                                    CNote lastNote = Players[p].SingLine[line].LastNote;
                                    if (lastNote.Tone == tonePlayer && lastNote.EndBeat + 1 == beat && !lastNote.Hit)
                                        Players[p].SingLine[line].IncLastNoteLength();
                                    else
                                        Players[p].SingLine[line].AddNote(new CNote(beat, 1, tonePlayer, String.Empty, false, ENoteType.Freestyle));
                                }
                                else
                                    Players[p].SingLine[line].AddNote(new CNote(beat, 1, tonePlayer, String.Empty, false, ENoteType.Freestyle));
                            }
                        }

                        // Line Bonus
                        int numLinesWithPoints = song.Notes.GetNumLinesWithPoints(Players[p].LineNr);
                        if (note == lines[line].NoteCount - 1 && numLinesWithPoints > 0)
                        {
                            if (notes[note].EndBeat == beat && lines[line].Points > 0f)
                            {
                                double factor = Players[p].SingLine[line].Points / (double)lines[line].Points;
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
                                Players[p].Points += points;
                                Players[p].PointsLineBonus += points;
                            }
                        }
                    }
                }
            }
            _OldBeatD = ActBeatD;
        }
    }
}