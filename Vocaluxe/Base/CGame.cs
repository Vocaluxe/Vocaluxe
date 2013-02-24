using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using Vocaluxe.GameModes;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;
using Vocaluxe.Menu.SingNotes;
using Vocaluxe.Menu.SongMenu;

namespace Vocaluxe.Base
{
    static class CGame
    {
        private static IGameMode _GameMode;
        private static SPlayer[] _Player;
        private static int _NumPlayer = CConfig.NumPlayer;

        private static float _Beat = 0f;
        private static int _CurrentBeat = 0;

        private static float _MidBeatD = 0f;
        private static int _CurrentBeatD = 0;
        private static int _OldBeatD = 0;
        private static Random _Rand = new Random();

        public static Random Rand
        {
            get { return _Rand; }
        }

        public static float GetBeatFromTime(float Time, float BPM, float Gap)
        {
            return BPM / 60 * (Time - Gap);
        }

        public static float GetTimeFromBeats(float Beats, float BPM)
        {
            if (BPM > 0f)
                return Beats / BPM * 60f;

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

        public static bool AddVisibleSong(int VisibleIndex, EGameMode GameMode)
        {
            return _GameMode.AddVisibleSong(VisibleIndex, GameMode);
        }

        public static bool AddSong(int AbsoluteIndex, EGameMode GameMode)
        {
            return _GameMode.AddSong(AbsoluteIndex, GameMode);
        }

        public static bool RemoveVisibleSong(int VisibleIndex)
        {
            return _GameMode.RemoveVisibleSong(VisibleIndex);
        }

        public static bool RemoveSong(int AbsoluteIndex)
        {
            return _GameMode.RemoveSong(AbsoluteIndex);
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

        public static CSong GetSong(int Num)
        {
            return _GameMode.GetSong(Num);
        }

        public static EGameMode GetGameMode(int Num)
        {
            return _GameMode.GetGameMode(Num);
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

        public static void UpdatePoints(float Time)
        {
            bool DEBUG_HIT = false;

            CSong song = _GameMode.GetSong();

            if (song == null)
                return;

            float b = GetBeatFromTime(Time, song.BPM, song.Gap);
            if (b <= _Beat)
                return;

            _Beat = b;
            _CurrentBeat = (int)Math.Floor(_Beat);

            _MidBeatD = -0.5f + GetBeatFromTime(Time, song.BPM, song.Gap + CConfig.MicDelay/1000f);
            _CurrentBeatD = (int)Math.Floor(_MidBeatD);

            for (int p = 0; p < _NumPlayer; p++)
            {
                CSound.AnalyzeBuffer(p);
            }

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
                    int Line = -1;

                    for (int j = 0; j < lines.Length; j++)
                    {
                        if (beat >= lines[j].StartBeat && beat <= lines[j].EndBeat)
                        {
                            Line = j;
                            break;
                        }
                    }

                    if (Line >= 0)
                    {
                        if (Line != _Player[p].CurrentLine)
                            _Player[p].CurrentNote = -1;

                        _Player[p].CurrentLine = Line;

                        while (_Player[p].SingLine.Count <= Line)
                        {
                            _Player[p].SingLine.Add(new CLine());
                        }

                        CNote[] notes = lines[Line].Notes;
                        int Note = -1;
                        for (int j = 0; j < notes.Length; j++)
                        {
                            if (beat >= notes[j].StartBeat && beat <= notes[j].EndBeat)
                            {
                                Note = j;
                                break;
                            }
                        }

                        if (Note >= 0)
                        {
                            _Player[p].CurrentNote = Note;

                            if (Line == lines.Length - 1)
                            {
                                if (Note == lines[Line].NoteCount - 1)
                                {
                                    if (notes[Note].EndBeat == beat)
                                        _Player[p].SongFinished = true;
                                }
                            }

                            if (notes[Note].PointsForBeat > 0 && (CSound.RecordToneValid(p) || DEBUG_HIT))
                            {
                                int Tone = notes[Note].Tone;
                                int TonePlayer = CSound.RecordGetTone(p);

                                while (TonePlayer - Tone > 6)
                                    TonePlayer -= 12;

                                while (TonePlayer - Tone < -6)
                                    TonePlayer += 12;

                                if (DEBUG_HIT)
                                    TonePlayer = Tone;

                                _Player[p].NoteDiff = Math.Abs(Tone - TonePlayer);
                                bool Hit=_Player[p].NoteDiff <= (2 - (int)_Player[p].Difficulty);
                                if (Hit)
                                {
                                    // valid
                                    //CSound.RecordSetTone(p, Tone);
                                    double points = (CSettings.MaxScore - CSettings.LinebonusScore) * (double)notes[Note].PointsForBeat / (double)song.Notes.GetLines(_Player[p].LineNr).Points;
                                    if (notes[Note].NoteType == ENoteType.Golden)
                                        _Player[p].PointsGoldenNotes += points;

                                    _Player[p].Points += points;

                                    // update player notes (sung notes)
                                    if (_Player[p].SingLine[Line].NoteCount > 0)
                                    {
                                        CNote nt = _Player[p].SingLine[Line].LastNote;
                                        if (notes[Note].StartBeat == beat || nt.EndBeat + 1 != beat || nt.Tone != Tone || !nt.Hit)
                                            _Player[p].SingLine[Line].AddNote(new CNote(beat, 1, Tone, String.Empty, true, notes[Note].NoteType));
                                        else
                                        {
                                            _Player[p].SingLine[Line].IncLastNoteLength();
                                        }
                                    }
                                    else
                                    {
                                        _Player[p].SingLine[Line].AddNote(new CNote(beat, 1, Tone, String.Empty, true, notes[Note].NoteType));
                                    }

                                    _Player[p].SingLine[Line].LastNote.IsPerfect(notes[Note]);
                                    _Player[p].SingLine[Line].IsPerfect(lines[Line]);
                                }
                                else
                                {
                                    if (_Player[p].SingLine[Line].NoteCount > 0)
                                    {
                                        CNote nt = _Player[p].SingLine[Line].LastNote;
                                        if (nt.Tone == TonePlayer && nt.EndBeat + 1 == beat && !nt.Hit)
                                        {
                                            _Player[p].SingLine[Line].IncLastNoteLength();
                                        }
                                        else
                                        {
                                            _Player[p].SingLine[Line].AddNote(new CNote(beat, 1, TonePlayer, String.Empty, false, ENoteType.Freestyle));
                                        }
                                    }
                                    else
                                    {
                                        _Player[p].SingLine[Line].AddNote(new CNote(beat, 1, TonePlayer, String.Empty, false, ENoteType.Freestyle));
                                    }

                                }
                            }

                            // Line Bonus
                            int NumLinesWithPoints = song.Notes.GetNumLinesWithPoints(_Player[p].LineNr);
                            if (Note == lines[Line].NoteCount - 1 && NumLinesWithPoints > 0)
                            {
                                if (notes[Note].EndBeat == beat && lines[Line].Points > 0f)
                                {
                                    double factor = (double)_Player[p].SingLine[Line].Points / (double)lines[Line].Points;
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

                                    double points = CSettings.LinebonusScore * factor * 1f / NumLinesWithPoints;                                    
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
