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
using VocaluxeLib.Draw;
using VocaluxeLib.Log;
using VocaluxeLib.Songs.Sources;
using VocaluxeLib.Utils.Player;

namespace VocaluxeLib.Songs
{
    public class CSongPointer
    {
        public readonly int SongId;
        public string SortString;
        public bool IsSung;

        public CSongPointer(int id, string sortString)
        {
            SongId = id;
            SortString = sortString;
        }
    }

    [Flags]
    enum EHeaderFlags
    {
        Title = 1,
        Artist = 2,
        MP3 = 4,
        Instrumental = 5,
        Vocals = 6,
        Bpm = 8,
        MedleyStartBeat = 16,
        MedleyEndBeat = 32
    }

    public enum EDataSource
    {
        None = 0,
        Calculated,
        Tag
    }

    public struct SMedley
    {
        public EDataSource Source;
        public int StartBeat;
        public int EndBeat;
        public float FadeInTime;
        public float FadeOutTime;
    }

    public struct SShortEnd
    {
        public EDataSource Source;
        public int EndBeat;
    }

    public struct SPreview
    {
        public EDataSource Source;
        public float StartTime;
    }

    public partial class CSong : IEquatable<CSong>
    {
        private object _CoverLock = new();
        private CTextureRef _CoverTexture;

        public SMedley Medley;

        private bool _CalculateMedley = true;
        public SPreview Preview;

        public SShortEnd ShortEnd;

        public Encoding Encoding = new UTF8Encoding();
        public bool ManualEncoding;
        public string Folder = string.Empty;
        public string FolderName = string.Empty;
        public string FileName = string.Empty;
        public bool Relative;

        public string AudioFileName = string.Empty;
        public string InstrumentalFileName = string.Empty;
        public string VocalsFileName = string.Empty;
        public string CoverFileName = string.Empty;
        public readonly List<string> BackgroundFileNames = [];
        public string VideoFileName = string.Empty;

        public EAspect VideoAspect = EAspect.Automatic;

        public bool NotesLoaded { get; private set; }

        public CTextureRef CoverTexture
        {
            get
            {
                LoadAndCacheCoverIfNeeded();
                return _CoverTexture;
            }
        }

        public string Title = string.Empty;
        public string Artist = string.Empty;

        public string TitleSorting = string.Empty;
        public string ArtistSorting = string.Empty;

        public string Version = "";
        public string Length = ""; //Length set in song file, SHOULD match actual song length but is more a hint
        public string Source = "";
        public readonly List<string> UnknownTags = [];

        /// <summary>
        ///     Start of the song in s (s in txt)
        /// </summary>
        public float Start;
        /// <summary>
        ///     End of the song in s (ms in txt)
        /// </summary>
        public float End;

        public float Bpm = 1f;
        /// <summary>
        ///     Gap of the mp3 in s (ms in txt)
        /// </summary>
        public float Gap;
        /// <summary>
        ///     Gap of the video in s (s in txt)
        /// </summary>
        public float VideoGap;

        private string _Comment = "";

        // Sorting
        public int Id;
        private readonly bool _Visible = true;
        private readonly int _CatIndex = -1;
        private readonly bool _Selected;
        public bool IsDuet => Notes.VoiceCount > 1;
        public bool IsRap = false;

        public readonly List<string> Creators = [];
        public readonly List<string> Editions = [];
        public readonly List<string> Genres = [];
        public readonly List<string> Tags = [];
        public readonly List<string> Languages = [];
        public string Album = "";
        public string Year = "";

        public int DataBaseSongId = -1;
        public DateTime DateAdded = DateTime.Today;
        public int NumPlayed;
        public int NumPlayedSession;

        // Notes
        public readonly CNotes Notes = new();

        public IList<EGameMode> AvailableGameModes
        {
            get
            {
                var gms = new List<EGameMode> { IsDuet ? EGameMode.TR_GAMEMODE_DUET : EGameMode.TR_GAMEMODE_NORMAL };
                if (Medley.Source != EDataSource.None)
                {
                    gms.Add(EGameMode.TR_GAMEMODE_MEDLEY);
                }

                if (ShortEnd.Source != EDataSource.None)
                {
                    gms.Add(EGameMode.TR_GAMEMODE_SHORTSONG);
                }

                return gms;
            }
        }

        /// <summary>
        ///     Returns true if the requested game mode is available
        /// </summary>
        /// <param name="gameMode"></param>
        /// <returns>true if the requested game mode is available</returns>
        public bool IsGameModeAvailable(EGameMode gameMode)
        {
            return AvailableGameModes.Any(gm => gm == gameMode);
        }

        //No point creating a song without a text file --> Use factory method LoadSong
        private CSong() { }

        public CSong(CSong song)
        {
            _CoverTexture = song._CoverTexture;

            Medley = song.Medley;

            _CalculateMedley = song._CalculateMedley;
            Preview = song.Preview;

            ShortEnd = song.ShortEnd;

            Encoding = song.Encoding;
            ManualEncoding = song.ManualEncoding;
            Folder = song.Folder;
            FolderName = song.FolderName;
            FileName = song.FileName;
            Relative = song.Relative;

            AudioFileName = song.AudioFileName;
            InstrumentalFileName = song.InstrumentalFileName;
            VocalsFileName = song.VocalsFileName;
            CoverFileName = song.CoverFileName;
            BackgroundFileNames = song.BackgroundFileNames;
            VideoFileName = song.VideoFileName;

            VideoAspect = song.VideoAspect;
            NotesLoaded = song.NotesLoaded;

            Artist = song.Artist;
            Title = song.Title;
            ArtistSorting = song.ArtistSorting;
            TitleSorting = song.TitleSorting;

            Version = song.Version;
            Length = song.Length;
            Source = song.Source;
            UnknownTags = new List<string>(song.UnknownTags);

            Start = song.Start;
            End = song.End;

            Bpm = song.Bpm;
            Gap = song.Gap;
            VideoGap = song.VideoGap;

            _Comment = song._Comment;

            Id = song.Id;
            _Visible = song._Visible;
            _CatIndex = song._CatIndex;
            _Selected = song._Selected;

            Creators = new List<string>(song.Creators);
            Editions = new List<string>(song.Editions);
            Genres = new List<string>(song.Genres);
            Tags = new List<string>(song.Tags);
            Languages = new List<string>(song.Languages);
            Album = song.Album;
            Year = song.Year;

            DataBaseSongId = song.DataBaseSongId;
            DateAdded = song.DateAdded;
            NumPlayed = song.NumPlayed;
            NumPlayedSession = song.NumPlayedSession;

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

        public bool Save()
        {
            return Save(Path.Combine(Folder, FileName));
        }

        public bool Save(string filePath)
        {
            var writer = new CSongWriter(this);
            return writer.SaveFile(filePath);
        }

        public ISoundSource GetAudio()
        {
            return new CSongFileSource(this, AudioFileName);
        }

        public ISoundSource GetInstrumental()
        {
            return new CSongFileSource(this, InstrumentalFileName);
        }

        public bool HasInstrumental => _FileExist(InstrumentalFileName);

        public ISoundSource GetVocals()
        {
            return new CSongFileSource(this, VocalsFileName);
        }

        public bool HasVocals => _FileExist(VocalsFileName);

        public bool HasVideo => _FileExist(VideoFileName);

        private bool _FileExist(string fileName)
        {
            return !string.IsNullOrEmpty(_GetFilePathIfExist(fileName));
        }

        private string _GetFilePathIfExist(string fileName)
        {
            if (string.IsNullOrEmpty(Folder) || string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            var filePath = Path.Combine(Folder, fileName);
            return File.Exists(filePath) ? filePath : null;
        }

        public Stream GetVideoStream()
        {
            var videoPath = _GetFilePathIfExist(VideoFileName);
            if (!string.IsNullOrEmpty(videoPath))
            {
                return new FileStream(videoPath, FileMode.Open, FileAccess.Read);
            }

            CLog.Error($"Video file {videoPath} doesn't exist");
            return null;
        }

        public void LoadAndCacheCoverIfNeeded()
        {
            lock (_CoverLock)
            {
                if (_CoverTexture != null)
                {
                    // Already loaded
                    return;
                }

                if (!string.IsNullOrEmpty(CoverFileName))
                {
                    var coverPath = Path.Combine(Folder, CoverFileName);
                    _CoverTexture = CBase.DataBase.GetCover(coverPath);
                    if (_CoverTexture != null)
                    {
                        // Cover loaded from DB cache
                        return;
                    }

                    if (File.Exists(coverPath))
                    {
                        // Generate the cover then enqueue it in the CoverDB transaction
                        using var bitmap = CHelper.LoadBitmap(coverPath);
                        var coverData = CBase.Cover.GenerateCoverData(bitmap, out var finalSize);
                        CBase.DataBase.EnqueueCoverToTransaction(coverPath, finalSize, coverData);
                    }
                }

                // Fallback on the default cover
                _CoverTexture ??= CBase.Cover.GenerateCover(Title, ECoverGeneratorType.Song, null);
            }
        }

        private void _CheckFiles()
        {
            if (CoverFileName == "")
            {
                var files = CHelper.ListImageFiles(Folder);
                foreach (var file in files)
                {
                    if (file.ContainsIgnoreCase("[CO]") &&
                        (file.ContainsIgnoreCase(Title) || file.ContainsIgnoreCase(Artist)))
                    {
                        CoverFileName = file;
                    }
                }
            }

            if (BackgroundFileNames.Count == 0)
            {
                var files = CHelper.ListImageFiles(Folder);
                foreach (var file in files)
                {
                    if (file.ContainsIgnoreCase("[BG]") &&
                        (file.ContainsIgnoreCase(Title) || file.ContainsIgnoreCase(Artist)))
                    {
                        BackgroundFileNames.Add(file);
                    }
                }
            }
        }

        private void _CheckDuet()
        {
            for (var i = 0; i < Notes.VoiceCount; i++)
            {
                if (!Notes.VoiceNames.IsSet(i))
                {
                    CLog.Error("Warning: Can't find #P" + (i + 1) + "-tag for duets in \"" + Artist + " - " + Title + "\".");
                }
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
            var voice = Notes.GetVoice(0);

            if (voice.NumLines == 0)
            {
                return null;
            }

            // build sentences list
            var sentences = voice.Lines.Select(line => line.Points != 0 ? line.Lyrics : string.Empty).ToList();

            // find equal sentences series
            var series = new List<SSeries>();
            for (var i = 0; i < voice.NumLines - 1; i++)
            {
                for (var j = i + 1; j < voice.NumLines; j++)
                {
                    if (sentences[i] != sentences[j] || sentences[i] == "")
                    {
                        continue;
                    }

                    var tempSeries = new SSeries { Start = i, End = i };

                    int max;
                    if (j + j - i > voice.NumLines)
                    {
                        max = voice.NumLines - 1 - j;
                    }
                    else
                    {
                        max = j - i - 1;
                    }

                    for (var k = 1; k <= max; k++)
                    {
                        if (sentences[i + k] == sentences[j + k] && sentences[i + k] != "")
                        {
                            tempSeries.End = i + k;
                        }
                        else
                        {
                            break;
                        }
                    }

                    tempSeries.Length = tempSeries.End - tempSeries.Start + 1;
                    series.Add(tempSeries);
                }
            }

            return series;
        }

        private void _CalcMedley()
        {
            if (IsDuet)
            {
                Medley.Source = EDataSource.None;
                return;
            }

            if (!_CalculateMedley || Medley.Source != EDataSource.None)
            {
                return;
            }

            var series = _GetSeries();
            if (series == null)
            {
                return;
            }

            // search for longest series
            var longest = 0;
            for (var i = 0; i < series.Count; i++)
            {
                if (series[i].Length > series[longest].Length)
                {
                    longest = i;
                }
            }

            var voice = Notes.GetVoice(0);

            // set medley vars
            if (series.Count > 0 && series[longest].Length > CBase.Settings.GetMedleyMinSeriesLength())
            {
                Medley.StartBeat = voice.Lines[series[longest].Start].FirstNoteBeat;
                Medley.EndBeat = voice.Lines[series[longest].End].LastNoteBeat;

                var foundEnd = CBase.Game.GetTimeFromBeats(Medley.EndBeat, Bpm) - CBase.Game.GetTimeFromBeats(Medley.StartBeat, Bpm) < CBase.Settings.GetMedleyMinDuration();

                // set end if duration < MedleyMinDuration

                if (!foundEnd)
                {
                    for (var i = series[longest].End + 1; i < voice.NumLines - 1; i++)
                    {
                        if (CBase.Game.GetTimeFromBeats(voice.Lines[i].LastNoteBeat, Bpm) - CBase.Game.GetTimeFromBeats(Medley.StartBeat, Bpm) <
                            CBase.Settings.GetMedleyMinDuration())
                        {
                            foundEnd = true;
                            Medley.EndBeat = voice.Lines[i].LastNoteBeat;
                            break;
                        }
                    }
                }

                if (foundEnd)
                {
                    Medley.Source = EDataSource.Calculated;
                    Medley.FadeInTime = CBase.Settings.GetDefaultMedleyFadeInTime();
                    Medley.FadeOutTime = CBase.Settings.GetDefaultMedleyFadeOutTime();
                }
            }
        }

        private void _CheckPreview()
        {
            if (Preview.Source != EDataSource.None)
            {
                return;
            }

            if (Medley.Source != EDataSource.None)
            {
                Preview.StartTime = CBase.Game.GetTimeFromBeats(Medley.StartBeat, Bpm);
                Preview.Source = EDataSource.Calculated;
            }
        }

        private void _FindShortEnd()
        {
            if (ShortEnd.Source != EDataSource.None)
            {
                return;
            }

            var series = _GetSeries();
            if (series == null)
            {
                return;
            }

            var voice = Notes.GetVoice(0);

            //Calculate length of singing
            var stop = (voice.Lines[voice.Lines.Length - 1].LastNoteBeat - voice.Lines[0].FirstNote.StartBeat) / 2 + voice.Lines[0].FirstNote.StartBeat;

            //Check if stop is in series
            for (var i = 0; i < series.Count; i++)
            {
                if (voice.Lines[series[i].Start].FirstNoteBeat < stop && voice.Lines[series[i].End].LastNoteBeat > stop)
                {
                    if (stop < voice.Lines[series[i].Start].FirstNoteBeat + (voice.Lines[series[i].End].LastNoteBeat - voice.Lines[series[i].Start].FirstNoteBeat) / 2)
                    {
                        ShortEnd.EndBeat = voice.Lines[series[i].Start - 1].LastNote.EndBeat;
                        ShortEnd.Source = EDataSource.Calculated;
                        return;
                    }

                    ShortEnd.EndBeat = voice.Lines[series[i].End].LastNote.EndBeat;
                    ShortEnd.Source = EDataSource.Calculated;
                    return;
                }
            }

            //Check if stop is in line
            foreach (var line in voice.Lines)
            {
                if (line.FirstNoteBeat < stop && line.LastNoteBeat > stop)
                {
                    ShortEnd.EndBeat = line.LastNoteBeat;
                    ShortEnd.Source = EDataSource.Calculated;
                    return;
                }
            }

            ShortEnd.EndBeat = stop;
            ShortEnd.Source = EDataSource.Calculated;
        }

        public bool Equals(CSong other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((CSong)obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}