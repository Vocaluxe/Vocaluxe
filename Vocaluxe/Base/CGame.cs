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

//Uncomment to make the engine hit every note
//#define DEBUG_HIT
//Uncomment to only hit notes for player one
//#define DEBUG_HIT_1

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
        private static int _NumPlayers = CConfig.Config.Game.NumPlayers;

        /// <summary>
        ///     Last beat that has been evaluated
        /// </summary>
        private static int _LastEvalBeat;
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

        /// <summary>
        ///     Currently played beat in song. This is floor(CurrentBeatF)
        /// </summary>
        public static int CurrentBeat
        {
            get { return (int)Math.Floor(CurrentBeatF); }
        }
        /// <summary>
        ///     Currently played beat in song. A value of 1.5 indicates that the song is in the middle of 1st and 2nd beat
        /// </summary>
        public static float CurrentBeatF { get; private set; }

        /// <summary>
        ///     Middle of the beat that got just recorded (CurrentBeat-MicDelayBeats-0.5)
        ///     A value of 2 indicates that beat 2 was recorded and is ready for evaluation.
        ///     A value of 2.5 indicates that beat 3 is half way there.
        /// </summary>
        public static float MidRecordedBeat { get; private set; }
        /// <summary>
        ///     Beat that got just recorded and can be evaluated. This is floor(MidRecordedBeat)
        /// </summary>
        public static int RecordedBeat
        {
            get { return (int)Math.Floor(MidRecordedBeat); }
        }

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
            _SongQueue.StartNextRound(Players);
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

        public static CSong GetSong(int round)
        {
            return _SongQueue.GetSong(round);
        }

        public static EGameMode GetGameMode(int round)
        {
            return _SongQueue.GetGameMode(round);
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

        public static int NumPlayers
        {
            get { return _NumPlayers; }
            set
            {
                if (value > 0 && value <= CSettings.MaxNumPlayer)
                    _NumPlayers = value;
            }
        }

        public static void GotoNameSelection()
        {
            CGraphics.FadeTo(EScreen.Names);
        }

        public static void ResetPlayer()
        {
            for (int i = 0; i < Players.Length; i++)
            {
                Players[i].Points = 0f;
                Players[i].PointsLineBonus = 0f;
                Players[i].PointsGoldenNotes = 0f;
                Players[i].Rating = 0.5f;
                Players[i].VoiceNr = 0;
                Players[i].NoteDiff = 0;
                Players[i].SungLines = new List<CSungLine>();
                Players[i].CurrentLine = -1;
                Players[i].CurrentNote = -1;
                Players[i].SongID = -1;
                Players[i].GameMode = EGameMode.TR_GAMEMODE_NORMAL;
                Players[i].DateTicks = DateTime.Now.Ticks;
                Players[i].SongFinished = false;
            }
            _LastEvalBeat = -100;
            CurrentBeatF = -100;
            MidRecordedBeat = -100;
        }

        public static void UpdatePoints(float time)
        {
            CSong song = _SongQueue.GetSong();

            if (song == null)
                return;

            float b = GetBeatFromTime(time, song.BPM, song.Gap);
            if (b <= CurrentBeatF)
                return;

            CurrentBeatF = b;

            MidRecordedBeat = -0.5f + GetBeatFromTime(time, song.BPM, song.Gap + CConfig.Config.Record.MicDelay / 1000f);

            for (int p = 0; p < _NumPlayers; p++)
                CRecord.AnalyzeBuffer(p);

            if (_LastEvalBeat >= RecordedBeat)
                return;

            for (int p = 0; p < _NumPlayers; p++)
            {
                for (int beat = _LastEvalBeat + 1; beat <= RecordedBeat; beat++)
                {
                    if ((_SongQueue.GetCurrentGameMode() == EGameMode.TR_GAMEMODE_MEDLEY && song.Medley.EndBeat == beat) ||
                        (_SongQueue.GetCurrentGameMode() == EGameMode.TR_GAMEMODE_SHORTSONG && song.ShortEnd.EndBeat == beat))
                        Players[p].SongFinished = true;

                    CSongLine[] lines = song.Notes.GetVoice(Players[p].VoiceNr).Lines;
                    int line = song.Notes.GetVoice(Players[p].VoiceNr).FindPreviousLine(beat);
                    if (line < 0 || lines[line].EndBeat < beat)
                        continue;

                    //Check for already sung
                    if (line < Players[p].SungLines.Count - 1)
                        continue; // Already sung whole line
                    if (line == Players[p].SungLines.Count - 1)
                    {
                        //We are in the last line
                        if (beat <= Players[p].SungLines[line].LastNoteBeat)
                            continue; //We already have something that ends with/after that beat
                    }

                    if (line != Players[p].CurrentLine)
                        Players[p].CurrentNote = -1;

                    Players[p].CurrentLine = line;

                    while (Players[p].SungLines.Count <= line)
                        Players[p].SungLines.Add(new CSungLine());

                    CSongNote[] notes = lines[line].Notes;
                    int note = lines[line].FindPreviousNote(beat);
                    if (note < 0 || notes[note].EndBeat < beat)
                        continue;

                    Players[p].CurrentNote = note;

                    if (line == lines.Length - 1 && beat == lines[line].LastNoteBeat)
                        Players[p].SongFinished = true;

                    if (notes[note].PointsForBeat > 0 && (CRecord.ToneValid(p)
#if DEBUG_HIT
                        || true
#elif DEBUG_HIT_1
                        || (true && p == 0)
#endif
                                                         ))
                    {
                        int tone = notes[note].Tone;
                        int tonePlayer = CRecord.GetTone(p);

                        while (tonePlayer - tone > 6)
                            tonePlayer -= 12;

                        while (tonePlayer - tone < -6)
                            tonePlayer += 12;

#if DEBUG_HIT
                            tonePlayer = tone;
#elif DEBUG_HIT_1
                            if(p == 0) tonePlayer = tone;
#endif

                        Players[p].NoteDiff = Math.Abs(tone - tonePlayer);
                        bool hit = Players[p].NoteDiff <= (2 - (int)CProfiles.GetDifficulty(Players[p].ProfileID));

                        if (hit)
                        {
                            // valid
                            //CRecord.RecordSetTone(p, Tone);
                            double points = (CSettings.MaxScore - CSettings.LinebonusScore) * (double)notes[note].PointsForBeat /
                                            song.Notes.GetVoice(Players[p].VoiceNr).Points;
                            if (notes[note].Type == ENoteType.Golden)
                                Players[p].PointsGoldenNotes += points;

                            Players[p].Points += points;

                            // update player notes (sung notes)
                            if (Players[p].SungLines[line].NoteCount > 0)
                            {
                                CSungNote lastNote = Players[p].SungLines[line].LastNote;

                                if (notes[note].StartBeat == beat || lastNote.EndBeat + 1 != beat || lastNote.Tone != tone || !lastNote.Hit)
                                    Players[p].SungLines[line].AddNote(new CSungNote(beat, 1, tone, notes[note], points));
                                else
                                {
                                    Players[p].SungLines[line].IncLastNoteLength();
                                    Players[p].SungLines[line].LastNote.Points += points;
                                }
                            }
                            else
                                Players[p].SungLines[line].AddNote(new CSungNote(beat, 1, tone, notes[note], points));

                            Players[p].SungLines[line].LastNote.CheckPerfect();
                            Players[p].SungLines[line].IsPerfect(lines[line]);
                        }
                        else
                        {
                            if (Players[p].SungLines[line].NoteCount > 0)
                            {
                                CSungNote lastNote = Players[p].SungLines[line].LastNote;
                                if (lastNote.Tone != tonePlayer || lastNote.EndBeat + 1 != beat || lastNote.Hit)
                                    Players[p].SungLines[line].AddNote(new CSungNote(beat, 1, tonePlayer));
                                else
                                    Players[p].SungLines[line].IncLastNoteLength();
                            }
                            else
                                Players[p].SungLines[line].AddNote(new CSungNote(beat, 1, tonePlayer));
                        }
                    }

                    // Check if line ended
                    int numLinesWithPoints = song.Notes.GetNumLinesWithPoints(Players[p].VoiceNr);
                    if (beat == lines[line].LastNoteBeat && lines[line].Points > 0 && numLinesWithPoints > 0)
                    {
                        // Line Bonus
                        double factor = Players[p].SungLines[line].Points / (double)lines[line].Points;
                        if (factor <= 0.4)
                            factor = 0.0;
                        else if (factor >= 0.9)
                            factor = 1.0;
                        else
                        {
                            factor -= 0.4;
                            factor *= 2;
                            factor *= factor;
                        }

                        double points = CSettings.LinebonusScore * factor / numLinesWithPoints;
                        Players[p].Points += points;
                        Players[p].PointsLineBonus += points;
                        Players[p].SungLines[line].BonusPoints += points;

                        //Calculate rating
                        //Shift fraction of correct sung notes to [-0.1, 0.1], player needs to sing five lines fully correctly to get highest ranking
                        double current = Players[p].SungLines[line].Points / (double)lines[line].Points;
                        Players[p].Rating = (Players[p].Rating + (current * 0.2 - 0.1)).Clamp(0, 1);
                    }
                }
            }
            _LastEvalBeat = RecordedBeat;
        }

        public static void ResetToLastLine(int soundStream, CVideoStream vidStream)
        {
            float[] time = _GetLastSungLineStart();
            ResetToTime(time[0], time[1], soundStream, vidStream);
        }

        public static void ResetToTime(float time, float nextStart, int soundStream, CVideoStream vidStream)
        {
            if (time < 0)
                time = 0;

            CurrentBeatF = GetBeatFromTime(time, GetSong().BPM, GetSong().Gap);

            for (int p = 0; p < _NumPlayers; p++)
            {
                Players[p].Points = 0;
                Players[p].PointsGoldenNotes = 0;
                Players[p].PointsLineBonus = 0;
                int l = 0;
                int deleteLine = 0;
                foreach (CSungLine line in Players[p].SungLines)
                {
                    int n = -1;
                    int deleteNote = 0;
                    Players[p].PointsLineBonus += line.BonusPoints;
                    foreach (CSungNote note in line.Notes)
                    {
                        if (note.StartBeat < nextStart)
                        {
                            if (note.Hit && note.HitNote.Type == ENoteType.Golden)
                                Players[p].PointsGoldenNotes += note.Points;
                            Players[p].Points += note.Points;
                        }
                        else if (deleteNote != -1)
                            deleteNote = n;
                        n++;
                    }
                    while (line.NoteCount > n && n >= 0)
                    {
                        if (line.Notes[n].Hit && line.Notes[n].HitNote.Type == ENoteType.Golden)
                            Players[p].PointsGoldenNotes -= line.Notes[n].Points;
                        Players[p].Points -= line.Notes[n].Points;
                        line.DeleteNote(n);
                    }

                    if (line.LastNoteBeat > CurrentBeat && deleteLine == 0)
                        deleteLine = l;
                    l++;
                }
                Players[p].SungLines.RemoveRange(deleteLine, Players[p].SungLines.Count - deleteLine);
            }

            CSound.SetPosition(soundStream, time);
            CVideo.Skip(vidStream, time, GetSong().VideoGap);
        }

        private static float[] _GetLastSungLineStart()
        {
            return _GetNoteTimeBeforeBeat(CurrentBeat);
        }

        private static float[] _GetNoteTimeBeforeBeat(int beat)
        {
            CSong song = GetSong();
            int startBeat = (int)Math.Floor(beat - GetBeatFromTime(CSettings.PauseResetTime, song.BPM, 0f));
            int lastStart = 0;
            int nextStart = 0;
            foreach (CVoice voice in song.Notes.Voices)
            {
                int lastEnd = 0;
                int voiceStart = 0;
                int nextStartNote = 0;
                foreach (CSongLine line in voice.Lines)
                {
                    foreach (CSongNote note in line.Notes)
                    {
                        if (note.StartBeat > startBeat)
                        {
                            nextStartNote = note.StartBeat;
                            break;
                        }
                        voiceStart = note.StartBeat;
                        lastEnd = note.EndBeat;
                    }
                    if (nextStartNote > 0)
                        break;
                }
                if (nextStartNote - beat > CSettings.PauseResetTime)
                    lastStart = beat;
                else if (nextStartNote - lastEnd > CSettings.PauseResetTime)
                    lastStart = nextStartNote - (int)Math.Floor(GetBeatFromTime(CSettings.PauseResetTime, song.BPM, 0f));
                if (voiceStart > lastStart)
                {
                    lastStart = voiceStart;
                    nextStart = nextStartNote;
                }
            }

            return new float[] {GetTimeFromBeats(lastStart, GetSong().BPM), nextStart};
        }
    }
}