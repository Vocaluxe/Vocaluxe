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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VocaluxeLib.Draw;

namespace VocaluxeLib.Songs
{
    public class CSongPointer
    {
        public readonly int SongID;
        public string SortString;
        public bool IsSung;

        public CSongPointer(int id, string sortString)
        {
            SongID = id;
            SortString = sortString;
        }
    }

    [Flags]
    enum EHeaderFlags
    {
        Title = 1,
        Artist = 2,
        MP3 = 4,
        BPM = 8,
        PreviewStart = 16,
        MedleyStartBeat = 32,
        MedleyEndBeat = 64
    }

    public enum EMedleySource
    {
        None = 0,
        Calculated,
        Tag
    }

    public struct SMedley
    {
        public EMedleySource Source;
        public int StartBeat;
        public int EndBeat;
        public float FadeInTime;
        public float FadeOutTime;
    }

    public partial class CSong
    {
        private CTexture _CoverTextureSmall;
        private CTexture _CoverTextureBig;

        public SMedley Medley;

        public bool CalculateMedley = true;
        public float PreviewStart;

        public int ShortEnd;

        public Encoding Encoding = Encoding.Default;
        public string Folder = String.Empty;
        public string FolderName = String.Empty;
        public string FileName = String.Empty;
        public bool Relative;

        public string MP3FileName = String.Empty;
        public string CoverFileName = String.Empty;
        public string BackgroundFileName = String.Empty;
        public string VideoFileName = String.Empty;

        public EAspect VideoAspect = EAspect.Crop;

        public bool NotesLoaded { get; private set; }

        public CTexture CoverTextureSmall
        {
            get
            {
                if (_CoverTextureSmall == null)
                    LoadSmallCover();
                return _CoverTextureSmall;
            }

            set { _CoverTextureSmall = value; }
        }

        public CTexture CoverTextureBig
        {
            get { return _CoverTextureBig ?? _CoverTextureSmall; }
            set { _CoverTextureBig = value; }
        }

        public string Title = String.Empty;
        public string Artist = String.Empty;

        public string TitleSorting = String.Empty;
        public string ArtistSorting = String.Empty;

        /// <summary>
        /// Start of the song in s (s in txt)
        /// </summary>
        public float Start;
        /// <summary>
        /// End of the song in s (ms in txt)
        /// </summary>
        public float Finish;

        public float BPM = 1f;
        /// <summary>
        /// Gap of the mp3 in s (ms in txt)
        /// </summary>
        public float Gap;
        /// <summary>
        /// Gap of the video in s (s in txt)
        /// </summary>
        public float VideoGap;

        private string _Comment = "";

        // Sorting
        public int ID;
        private readonly bool _Visible = true;
        private readonly int _CatIndex = -1;
        private readonly bool _Selected;
        public bool IsDuet
        {
            get { return Notes.VoiceCount > 1; }
        }

        public readonly List<string> Edition = new List<string>();
        public readonly List<string> Genres = new List<string>();
        public string Year = "";

        public readonly List<string> Languages = new List<string>();

        public int DataBaseSongID = -1;
        public string DateAdded = "";
        public int NumPlayed;

        // Notes
        public readonly CNotes Notes = new CNotes();

        public EGameMode[] AvailableGameModes
        {
            get
            {
                var gms = new List<EGameMode> {IsDuet ? EGameMode.TR_GAMEMODE_DUET : EGameMode.TR_GAMEMODE_NORMAL};
                if (Medley.Source != EMedleySource.None)
                    gms.Add(EGameMode.TR_GAMEMODE_MEDLEY);
                gms.Add(EGameMode.TR_GAMEMODE_SHORTSONG);

                return gms.ToArray();
            }
        }

        //No point creating a song without a text file --> Use factory method LoadSong
        private CSong() {}

        public CSong(CSong song)
        {
            _CoverTextureSmall = song._CoverTextureSmall;
            _CoverTextureBig = song._CoverTextureBig;

            Medley = new SMedley
                {
                    Source = song.Medley.Source,
                    StartBeat = song.Medley.StartBeat,
                    EndBeat = song.Medley.EndBeat,
                    FadeInTime = song.Medley.FadeInTime,
                    FadeOutTime = song.Medley.FadeOutTime
                };

            CalculateMedley = song.CalculateMedley;
            PreviewStart = song.PreviewStart;

            ShortEnd = song.ShortEnd;

            Encoding = song.Encoding;
            Folder = song.Folder;
            FolderName = song.FolderName;
            FileName = song.FileName;
            Relative = song.Relative;

            MP3FileName = song.MP3FileName;
            CoverFileName = song.CoverFileName;
            BackgroundFileName = song.BackgroundFileName;
            VideoFileName = song.VideoFileName;

            VideoAspect = song.VideoAspect;
            NotesLoaded = song.NotesLoaded;

            Artist = song.Artist;
            Title = song.Title;

            Start = song.Start;
            Finish = song.Finish;

            BPM = song.BPM;
            Gap = song.Gap;
            VideoGap = song.VideoGap;

            _Comment = song._Comment;

            ID = song.ID;
            _Visible = song._Visible;
            _CatIndex = song._CatIndex;
            _Selected = song._Selected;

            Edition = new List<string>(song.Edition);

            Genres = new List<string>(song.Genres);

            Year = song.Year;

            Languages = new List<string>(song.Languages);

            DataBaseSongID = song.DataBaseSongID;
            DateAdded = song.DateAdded;
            NumPlayed = song.NumPlayed;

            Notes = new CNotes(song.Notes);
        }

        public static CSong LoadSong(string filePath)
        {
            var song = new CSong();
            var loader = new CSongLoader(song);
            return loader.InitPaths(filePath) && loader.ReadHeader() ? song : null;
        }

        public bool LoadNotes()
        {
            var loader = new CSongLoader(this);
            return loader.ReadNotes();
        }

        public string GetMP3()
        {
            return Path.Combine(Folder, MP3FileName);
        }

        public string GetVideo()
        {
            return Path.Combine(Folder, VideoFileName);
        }

        public void LoadSmallCover()
        {
            if (_CoverTextureSmall != null)
                return;
            if (CoverFileName != "")
            {
                if (!CBase.DataBase.GetCover(Path.Combine(Folder, CoverFileName), ref _CoverTextureSmall, CBase.Config.GetCoverSize()))
                    _CoverTextureSmall = CBase.Cover.GetNoCover();
            }
            else
                _CoverTextureSmall = CBase.Cover.GetNoCover();
        }

        private void _CheckFiles()
        {
            if (CoverFileName == "")
            {
                List<string> files = CHelper.ListFiles(Folder, "*.jpg");
                files.AddRange(CHelper.ListFiles(Folder, "*.jpeg"));
                files.AddRange(CHelper.ListFiles(Folder, "*.png"));
                foreach (String file in files)
                {
                    if (file.ContainsIgnoreCase("[CO]") &&
                        (file.ContainsIgnoreCase(Title) || file.ContainsIgnoreCase(Artist)))
                        CoverFileName = file;
                }
            }

            if (BackgroundFileName == "")
            {
                List<string> files = CHelper.ListFiles(Folder, "*.jpg");
                files.AddRange(CHelper.ListFiles(Folder, "*.jpeg"));
                files.AddRange(CHelper.ListFiles(Folder, "*.png"));
                foreach (String file in files)
                {
                    if (file.ContainsIgnoreCase("[BG]") &&
                        (file.ContainsIgnoreCase(Title) || file.ContainsIgnoreCase(Artist)))
                        BackgroundFileName = file;
                }
            }
        }

        private void _CheckDuet()
        {
            for (int i = 0; i < Notes.VoiceCount; i++)
            {
                if (!Notes.VoiceNames.IsSet(i))
                    CBase.Log.LogError("Warning: Can't find #P" + (i + 1) + "-tag for duets in \"" + Artist + " - " + Title + "\".");
            }
        }

        private struct SSeries
        {
            public int Start;
            public int End;
            public int Length;
        }

        private List<SSeries> _GetSeries()
        {
            CVoice voice = Notes.GetVoice(0);

            if (voice.NumLines == 0)
                return null;

            // build sentences list
            List<string> sentences = voice.Lines.Select(line => line.Points != 0 ? line.Lyrics : String.Empty).ToList();

            // find equal sentences series
            var series = new List<SSeries>();
            for (int i = 0; i < voice.NumLines - 1; i++)
            {
                for (int j = i + 1; j < voice.NumLines; j++)
                {
                    if (sentences[i] != sentences[j] || sentences[i] == "")
                        continue;
                    var tempSeries = new SSeries {Start = i, End = i};

                    int max;
                    if (j + j - i > voice.NumLines)
                        max = voice.NumLines - 1 - j;
                    else
                        max = j - i - 1;

                    for (int k = 1; k <= max; k++)
                    {
                        if (sentences[i + k] == sentences[j + k] && sentences[i + k] != "")
                            tempSeries.End = i + k;
                        else
                            break;
                    }

                    tempSeries.Length = tempSeries.End - tempSeries.Start + 1;
                    series.Add(tempSeries);
                }
            }
            return series;
        }

        private void _FindRefrain()
        {
            if (IsDuet)
            {
                Medley.Source = EMedleySource.None;
                return;
            }

            if (Medley.Source == EMedleySource.Tag)
                return;

            if (!CalculateMedley)
                return;

            List<SSeries> series = _GetSeries();
            if (series == null)
                return;

            // search for longest series
            int longest = 0;
            for (int i = 0; i < series.Count; i++)
            {
                if (series[i].Length > series[longest].Length)
                    longest = i;
            }

            CVoice voice = Notes.GetVoice(0);

            // set medley vars
            if (series.Count > 0 && series[longest].Length > CBase.Settings.GetMedleyMinSeriesLength())
            {
                Medley.StartBeat = voice.Lines[series[longest].Start].FirstNoteBeat;
                Medley.EndBeat = voice.Lines[series[longest].End].LastNoteBeat;

                bool foundEnd = CBase.Game.GetTimeFromBeats(Medley.StartBeat, BPM) + CBase.Settings.GetMedleyMinDuration() >
                                CBase.Game.GetTimeFromBeats(Medley.EndBeat, BPM);

                // set end if duration > MedleyMinDuration

                if (!foundEnd)
                {
                    for (int i = series[longest].Start + 1; i < voice.NumLines - 1; i++)
                    {
                        if (CBase.Game.GetTimeFromBeats(Medley.StartBeat, BPM) + CBase.Settings.GetMedleyMinDuration() >
                            CBase.Game.GetTimeFromBeats(voice.Lines[i].LastNoteBeat, BPM))
                        {
                            foundEnd = true;
                            Medley.EndBeat = voice.Lines[i].LastNoteBeat;
                        }
                    }
                }

                if (foundEnd)
                {
                    Medley.Source = EMedleySource.Calculated;
                    Medley.FadeInTime = CBase.Settings.GetDefaultMedleyFadeInTime();
                    Medley.FadeOutTime = CBase.Settings.GetDefaultMedleyFadeOutTime();
                }
            }

            if (Math.Abs(PreviewStart) < 0.001)
            {
                if (Medley.Source == EMedleySource.Calculated)
                    PreviewStart = CBase.Game.GetTimeFromBeats(Medley.StartBeat, BPM);
            }
        }

        private void _FindShortEnd()
        {
            List<SSeries> series = _GetSeries();
            if (series == null)
                return;

            CVoice voice = Notes.GetVoice(0);

            //Calculate length of singing
            int stop = (voice.Lines[voice.Lines.Length - 1].LastNoteBeat - voice.Lines[0].FirstNote.StartBeat) / 2 + voice.Lines[0].FirstNote.StartBeat;

            //Check if stop is in series
            for (int i = 0; i < series.Count; i++)
            {
                if (voice.Lines[series[i].Start].FirstNoteBeat < stop && voice.Lines[series[i].End].LastNoteBeat > stop)
                {
                    if (stop < (voice.Lines[series[i].Start].FirstNoteBeat + ((voice.Lines[series[i].End].LastNoteBeat - voice.Lines[series[i].Start].FirstNoteBeat) / 2)))
                    {
                        ShortEnd = voice.Lines[series[i].Start - 1].LastNote.EndBeat;
                        return;
                    }
                    ShortEnd = voice.Lines[series[i].End].LastNote.EndBeat;
                    return;
                }
            }

            //Check if stop is in line
            foreach (CSongLine line in voice.Lines)
            {
                if (line.FirstNoteBeat < stop && line.LastNoteBeat > stop)
                {
                    ShortEnd = line.LastNoteBeat;
                    return;
                }
            }

            ShortEnd = stop;
        }
    }
}