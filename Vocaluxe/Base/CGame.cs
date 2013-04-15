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
        private static SPlayer[] _Player;
        private static int _NumPlayer = CConfig.NumPlayer;

        private static float _Beat;
        private static int _CurrentBeat;

        private static float _MidBeatD;
        private static int _CurrentBeatD;
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

        public static int CurrentBeat
        {
            get { return _CurrentBeat; }
        }

        public static float Beat
        {
            get { return _Beat; }
        }

        public static float MidBeatD
        {
            get { return _MidBeatD; }
        }

        public static int ActBeatD
        {
            get { return _CurrentBeatD; }
        }

        public static SPlayer[] Player
        {
            get { return _Player; }
        }

        public static void Init()
        {
            _GameMode = new CGameModeNormal();
            _GameMode.Init();
            _Player = new SPlayer[CSettings.MaxNumPlayer];
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
            _GameMode.Start(_Player);
        }

        public static void NextRound()
        {
            _GameMode.NextRound(_Player);
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
            for (int i = 0; i < _Player.Length; i++)
            {
                _Player[i].Points = 0f;
                _Player[i].PointsLineBonus = 0f;
                _Player[i].PointsGoldenNotes = 0f;
                _Player[i].LineNr = 0;
                _Player[i].NoteDiff = 0;
                _Player[i].SingLine = new List<CLine>();
                _Player[i].CurrentLine = -1;
                _Player[i].CurrentNote = -1;
                _Player[i].SongID = -1;
                _Player[i].Medley = false;
                _Player[i].Duet = false;
                _Player[i].ShortSong = false;
                _Player[i].DateTicks = DateTime.Now.Ticks;
                _Player[i].SongFinished = false;
            }
            _OldBeatD = -100;
            _Beat = -100;
            _CurrentBeat = -100;
            _CurrentBeatD = -100;
        }

        public static void UpdatePoints(float time)
        {
            bool debugHit = false;

            CSong song = _GameMode.GetSong();

            if (song == null)
                return;

            float b = GetBeatFromTime(time, song.BPM, song.Gap);
            if (b <= _Beat)
                return;

            _Beat = b;
            _CurrentBeat = (int)Math.Floor(_Beat);

            _MidBeatD = -0.5f + GetBeatFromTime(time, song.BPM, song.Gap + CConfig.MicDelay / 1000f);
            _CurrentBeatD = (int)Math.Floor(_MidBeatD);

            for (int p = 0; p < _NumPlayer; p++)
                CSound.AnalyzeBuffer(p);

            if (_OldBeatD >= _CurrentBeatD)
                return;

            for (int p = 0; p < _NumPlayer; p++)
            {
                for (int beat = _OldBeatD + 1; beat <= _CurrentBeatD; beat++)
                {
                    if ((_GameMode.GetCurrentGameMode() == EGameMode.TR_GAMEMODE_MEDLEY && song.Medley.EndBeat == beat) ||
                        (_GameMode.GetCurrentGameMode() == EGameMode.TR_GAMEMODE_SHORTSONG && song.ShortEnd == beat))
                        _Player[p].SongFinished = true;

                    CLine[] lines = song.Notes.GetLines(_Player[p].LineNr).Line;
                    int line = -1;

                    for (int j = 0; j < lines.Length; j++)
                    {
                        if (beat >= lines[j].StartBeat && beat <= lines[j].EndBeat)
                        {
                            line = j;
                            break;
                        }
                    }

                    if (line >= 0)
                    {
                        if (line != _Player[p].CurrentLine)
                            _Player[p].CurrentNote = -1;

                        _Player[p].CurrentLine = line;

                        while (_Player[p].SingLine.Count <= line)
                            _Player[p].SingLine.Add(new CLine());

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
                            _Player[p].CurrentNote = note;

                            if (line == lines.Length - 1)
                            {
                                if (note == lines[line].NoteCount - 1)
                                {
                                    if (notes[note].EndBeat == beat)
                                        _Player[p].SongFinished = true;
                                }
                            }

                            if (notes[note].PointsForBeat > 0 && (CSound.RecordToneValid(p) || debugHit))
                            {
                                int tone = notes[note].Tone;
                                int tonePlayer = CSound.RecordGetTone(p);

                                while (tonePlayer - tone > 6)
                                    tonePlayer -= 12;

                                while (tonePlayer - tone < -6)
                                    tonePlayer += 12;

                                if (debugHit)
                                    tonePlayer = tone;

                                _Player[p].NoteDiff = Math.Abs(tone - tonePlayer);
                                bool hit = _Player[p].NoteDiff <= (2 - (int)_Player[p].Difficulty);
                                if (hit)
                                {
                                    // valid
                                    //CSound.RecordSetTone(p, Tone);
                                    double points = (CSettings.MaxScore - CSettings.LinebonusScore) * (double)notes[note].PointsForBeat /
                                                    song.Notes.GetLines(_Player[p].LineNr).Points;
                                    if (notes[note].NoteType == ENoteType.Golden)
                                        _Player[p].PointsGoldenNotes += points;

                                    _Player[p].Points += points;

                                    // update player notes (sung notes)
                                    if (_Player[p].SingLine[line].NoteCount > 0)
                                    {
                                        CNote nt = _Player[p].SingLine[line].LastNote;
                                        if (notes[note].StartBeat == beat || nt.EndBeat + 1 != beat || nt.Tone != tone || !nt.Hit)
                                            _Player[p].SingLine[line].AddNote(new CNote(beat, 1, tone, String.Empty, true, notes[note].NoteType));
                                        else
                                            _Player[p].SingLine[line].IncLastNoteLength();
                                    }
                                    else
                                        _Player[p].SingLine[line].AddNote(new CNote(beat, 1, tone, String.Empty, true, notes[note].NoteType));

                                    _Player[p].SingLine[line].LastNote.IsPerfect(notes[note]);
                                    _Player[p].SingLine[line].IsPerfect(lines[line]);
                                }
                                else
                                {
                                    if (_Player[p].SingLine[line].NoteCount > 0)
                                    {
                                        CNote nt = _Player[p].SingLine[line].LastNote;
                                        if (nt.Tone == tonePlayer && nt.EndBeat + 1 == beat && !nt.Hit)
                                            _Player[p].SingLine[line].IncLastNoteLength();
                                        else
                                            _Player[p].SingLine[line].AddNote(new CNote(beat, 1, tonePlayer, String.Empty, false, ENoteType.Freestyle));
                                    }
                                    else
                                        _Player[p].SingLine[line].AddNote(new CNote(beat, 1, tonePlayer, String.Empty, false, ENoteType.Freestyle));
                                }
                            }

                            // Line Bonus
                            int numLinesWithPoints = song.Notes.GetNumLinesWithPoints(_Player[p].LineNr);
                            if (note == lines[line].NoteCount - 1 && numLinesWithPoints > 0)
                            {
                                if (notes[note].EndBeat == beat && lines[line].Points > 0f)
                                {
                                    double factor = _Player[p].SingLine[line].Points / (double)lines[line].Points;
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
                                    _Player[p].Points += points;
                                    _Player[p].PointsLineBonus += points;
                                }
                            }
                        }
                    }
                }
            }
            _OldBeatD = _CurrentBeatD;
        }
    }
}