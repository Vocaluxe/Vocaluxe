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
using System.IO;
using System.Linq;
using System.Text;
using VocaluxeLib.Log;

namespace VocaluxeLib.Songs
{
    /// <summary>
    ///     Part of CSong that is required for note loading
    /// </summary>
    public partial class CSong
    {
        /// <summary>
        ///     Factor the bpm given in the txt is multiplied with
        /// </summary>
        private const int _BPMFactor = 4;

        private class CSongLoader
        {
            private readonly CSong _Song;
            private int _LineNr;

            public CSongLoader(CSong song)
            {
                _Song = song;
            }

            public bool InitPaths(string filePath)
            {
                if (!File.Exists(filePath))
                    return false;

                _Song.Folder = Path.GetDirectoryName(filePath);
                if (_Song.Folder == null)
                    return false;

                foreach (string folder in CBase.Config.GetSongFolders().Where(folder => _Song.Folder.StartsWith(folder)))
                {
                    if (_Song.Folder.Length == folder.Length)
                        _Song.FolderName = "Songs";
                    else
                    {
                        _Song.FolderName = _Song.Folder.Substring(folder.Length + 1);

                        int pos = _Song.FolderName.IndexOf("\\", StringComparison.Ordinal);
                        if (pos >= 0)
                            _Song.FolderName = _Song.FolderName.Substring(0, pos);
                    }
                    break;
                }

                _Song.FileName = Path.GetFileName(filePath);
                return true;
            }
            

            public bool ReadHeader(bool useSetEncoding = false)
            {
                string filePath = Path.Combine(_Song.Folder, _Song.FileName);

                if (!File.Exists(filePath))
                    return false;

                _Song.Languages.Clear();
                _Song.Genres.Clear();
                _Song.UnknownTags.Clear();
                _Song._Comment = "";
                _Song.ManualEncoding = false;
                _Song.Medley.Source = EDataSource.None;
                _Song._CalculateMedley = true;
                _Song.Preview.Source = EDataSource.None;
                _Song.ShortEnd.Source = EDataSource.None;

                var headerFlags = new EHeaderFlags();
                StreamReader sr = null;
                _LineNr = 0;
                try
                {
                    sr = new StreamReader(filePath, _Song.Encoding, true);
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        _LineNr++;
                        if (line == "")
                            continue;
                        if (!line[0].Equals('#'))
                            break;

                        int pos = line.IndexOf(":", StringComparison.Ordinal);

                        if (pos <= 1)
                        {
                            _Song.UnknownTags.Add(line);
                            continue;
                        }
                        string identifier = line.Substring(1, pos - 1).Trim().ToUpper();
                        if (identifier.Contains(" "))
                        {
                            _Song.UnknownTags.Add(line);
                            continue;
                        }
                        string value = line.Substring(pos + 1).Trim();

                        if (value == "")
                        {
                            _Song.UnknownTags.Add(line);
                            CLog.CSongLog.Warning("[{SongFileName}] Empty value skipped",  CLog.Params(_Song.FileName));
                            continue;
                        }

                        switch (identifier)
                        {
                            case "ENCODING":
                                Encoding newEncoding = value.GetEncoding();
                                _Song.ManualEncoding = true;
                                if (!newEncoding.Equals(sr.CurrentEncoding))
                                {
                                    if (useSetEncoding)
                                    {
                                        CLog.CSongLog.Warning("[{SongFileName}] Duplicate encoding ignored",  CLog.Params(_Song.FileName));
                                        continue;
                                    }
                                    sr.Dispose();
                                    sr = null;
                                    _Song.Encoding = newEncoding;
                                    return ReadHeader(true);
                                }
                                break;
                            case "TITLE":
                                _Song.Title = value;
                                headerFlags |= EHeaderFlags.Title;
                                break;
                            case "ARTIST":
                                _Song.Artist = value;
                                headerFlags |= EHeaderFlags.Artist;
                                break;
                            case "TITLE-ON-SORTING":
                                _Song.TitleSorting = value;
                                break;
                            case "ARTIST-ON-SORTING":
                                _Song.ArtistSorting = value;
                                break;
                            case "CREATOR":
                            case "AUTHOR":
                            case "AUTOR":
                                _Song.Creator = value;
                                break;
                            case "VERSION":
                                _Song.Version = value;
                                break;
                            case "SOURCE":
                            case "YOUTUBE":
                                _Song.Source = value;
                                break;
                            case "LENGTH":
                                _Song.Length = value;
                                break;
                            case "MP3":
                                if (File.Exists(Path.Combine(_Song.Folder, value)))
                                {
                                    _Song.MP3FileName = value;
                                    headerFlags |= EHeaderFlags.MP3;
                                }
                                else
                                {
                                    CLog.CSongLog.Error("[{SongFileName}] Can't find audio file: {AudioFile}", CLog.Params(_Song.FileName, Path.Combine(_Song.Folder, value)));
                                    return false;
                                }
                                break;
                            case "BPM":
                                if (CHelper.TryParse(value, out _Song.BPM))
                                {
                                    _Song.BPM *= _BPMFactor;
                                    headerFlags |= EHeaderFlags.BPM;
                                }
                                else
                                    CLog.CSongLog.Warning("[{SongFileName}] Invalid BPM value: {Value}", CLog.Params(_Song.FileName, value));
                                break;
                            case "EDITION":
                                if (value.Length > 1)
                                    _Song.Editions.Add(value);
                                else
                                    CLog.CSongLog.Warning("[{SongFileName}] Invalid edition: {Value}", CLog.Params(_Song.FileName, value));
                                break;
                            case "GENRE":
                                if (value.Length > 1)
                                    _Song.Genres.Add(value);
                                else
                                    CLog.CSongLog.Warning("[{SongFileName}] Invalid genre: {Value}", CLog.Params(_Song.FileName, value));
                                break;
                            case "ALBUM":
                                _Song.Album = value;
                                break;
                            case "YEAR":
                                int num;
                                if (value.Length == 4 && int.TryParse(value, out num) && num > 0)
                                    _Song.Year = value;
                                else
                                    CLog.CSongLog.Warning("[{SongFileName}] Invalid year: {Value}", CLog.Params(_Song.FileName, value));
                                break;
                            case "LANGUAGE":
                                if (value.Length > 1)
                                    _Song.Languages.Add(_UnifyLanguage(value));
                                else
                                    CLog.CSongLog.Warning("[{SongFileName}] Invalid language: {Value}", CLog.Params(_Song.FileName, value));
                                break;
                            case "COMMENT":
                                if (!String.IsNullOrEmpty(_Song._Comment))
                                    _Song._Comment += "\r\n";
                                _Song._Comment += value;
                                break;
                            case "GAP":
                                if (CHelper.TryParse(value, out _Song.Gap))
                                    _Song.Gap /= 1000f;
                                else
                                    CLog.CSongLog.Warning("[{SongFileName}] Invalid gap: {Value}", CLog.Params(_Song.FileName, value));
                                break;
                            case "COVER":
                                if (File.Exists(Path.Combine(_Song.Folder, value)))
                                    _Song.CoverFileName = value;
                                else
                                    CLog.CSongLog.Warning("[{SongFileName}] Can't find cover file: {MissingFile}", CLog.Params(_Song.FileName, Path.Combine(_Song.Folder, value)));
                                break;
                            case "BACKGROUND":
                                if (File.Exists(Path.Combine(_Song.Folder, value)))
                                    _Song.BackgroundFileNames.Add(value);
                                else
                                    CLog.CSongLog.Warning("[{SongFileName}] Can't find background file: {MissingFile}", CLog.Params(_Song.FileName, Path.Combine(_Song.Folder, value)));
                                break;
                            case "VIDEO":
                                if (File.Exists(Path.Combine(_Song.Folder, value)))
                                    _Song.VideoFileName = value;
                                else
                                    CLog.CSongLog.Warning("[{SongFileName}] Can't find video file: {MissingFile}", CLog.Params(_Song.FileName, Path.Combine(_Song.Folder, value)));
                                break;
                            case "VIDEOGAP":
                                if (!CHelper.TryParse(value, out _Song.VideoGap))
                                    CLog.CSongLog.Warning("[{SongFileName}] Invalid videogap: {Value}", CLog.Params(_Song.FileName, value));
                                break;
                            case "VIDEOASPECT":
                                if (!CHelper.TryParse(value, out _Song.VideoAspect, true))
                                    CLog.CSongLog.Warning("[{SongFileName}] Invalid videoaspect: {Value}", CLog.Params(_Song.FileName, value));
                                break;
                            case "START":
                                if (!CHelper.TryParse(value, out _Song.Start))
                                    CLog.CSongLog.Warning("[{SongFileName}] Invalid start: {Value}", CLog.Params(_Song.FileName, value));
                                break;
                            case "END":
                                if (CHelper.TryParse(value, out _Song.Finish))
                                    _Song.Finish /= 1000f;
                                else
                                    CLog.CSongLog.Warning("[{SongFileName}] Invalid end: {Value}", CLog.Params(_Song.FileName, value));
                                break;
                            case "PREVIEWSTART":
                                if (CHelper.TryParse(value, out _Song.Preview.StartTime) && _Song.Preview.StartTime >= 0f)
                                    _Song.Preview.Source = EDataSource.Tag;
                                else
                                    CLog.CSongLog.Warning("[{SongFileName}] Invalid previewstart: {Value}", CLog.Params(_Song.FileName, value));
                                break;
                            case "PREVIEW":
                                if (CHelper.TryParse(value, out _Song.Preview.StartTime) && _Song.Preview.StartTime >= 0f)
                                {
                                    //This is stored in ms not like PREVIEWSTART!
                                    _Song.Preview.StartTime /= 1000f;
                                    _Song.Preview.Source = EDataSource.Tag;
                                }
                                else
                                    CLog.CSongLog.Warning("[{SongFileName}] Invalid previewstart: {Value}", CLog.Params(_Song.FileName, value));
                                break;
                            case "MEDLEYSTARTBEAT":
                                if (int.TryParse(value, out _Song.Medley.StartBeat))
                                    headerFlags |= EHeaderFlags.MedleyStartBeat;
                                else
                                    CLog.CSongLog.Warning("[{SongFileName}] Invalid medleystartbeat: {Value}", CLog.Params(_Song.FileName, value));
                                break;
                            case "MEDLEYENDBEAT":
                                if (int.TryParse(value, out _Song.Medley.EndBeat))
                                    headerFlags |= EHeaderFlags.MedleyEndBeat;
                                else
                                    CLog.CSongLog.Warning("[{SongFileName}] Invalid medleyendbeat: {Value}", CLog.Params(_Song.FileName, value));
                                break;
                            case "ENDSHORT":
                                if ((headerFlags & EHeaderFlags.BPM) != 0)
                                {
                                    int endTime;
                                    if (int.TryParse(value, out endTime) || endTime < 0)
                                    {
                                        _Song.ShortEnd.EndBeat = (int)CBase.Game.GetBeatFromTime(endTime / 1000f, _Song.BPM, _Song.Gap);
                                        _Song.ShortEnd.Source = EDataSource.Tag;
                                    }
                                    else
                                        CLog.CSongLog.Warning("[{SongFileName}] Invalid shortendbeat: {Value}", CLog.Params(_Song.FileName, value));
                                }
                                break;
                            case "CALCMEDLEY":
                                if (value.ToUpper() == "OFF")
                                    _Song._CalculateMedley = false;
                                break;
                            case "RELATIVE":
                                if (value.ToUpper() == "YES")
                                    _Song.Relative = true;
                                break;
                            case "RESOLUTION":
                            case "NOTESGAP":
                                //Outdated/not used
                                _Song.UnknownTags.Add(line);
                                break;
                            default:
                                if (identifier.StartsWith("DUETSINGER"))
                                {
                                    identifier = identifier.Substring(10);
                                    if (!identifier.StartsWith("P")) // fix for missing "P"
                                        identifier = "P" + identifier;
                                }
                                if (identifier.StartsWith("P"))
                                {
                                    int player;
                                    if (int.TryParse(identifier.Substring(1).Trim(), out player))
                                    {
                                        foreach (int curPlayer in player.GetSetBits())
                                            _Song.Notes.VoiceNames[curPlayer] = value;
                                    }
                                }
                                else
                                {
                                    _Song.UnknownTags.Add(line);
                                    CLog.CSongLog.Warning("[{SongFileName}] Unknown tag: #{Identifier}", CLog.Params(_Song.FileName, identifier));
                                }

                                break;
                        }
                    } //end of while

                    if (sr.EndOfStream)
                    {
                        //No other data then header
                        CLog.CSongLog.Error("[{SongFileName}] Lyrics/Notes missing",  CLog.Params(_Song.FileName));

                        return false;
                    }

                    if ((headerFlags & EHeaderFlags.Title) == 0)
                    {
                        CLog.CSongLog.Error("[{SongFileName}] Title tag missing",  CLog.Params(_Song.FileName));
                        return false;
                    }

                    if ((headerFlags & EHeaderFlags.Artist) == 0)
                    {
                        CLog.CSongLog.Error("[{SongFileName}] Artist tag missing",  CLog.Params(_Song.FileName));
                        return false;
                    }

                    if ((headerFlags & EHeaderFlags.MP3) == 0)
                    {
                        CLog.CSongLog.Error("[{SongFileName}] MP3 tag missing",  CLog.Params(_Song.FileName));
                        return false;
                    }

                    if ((headerFlags & EHeaderFlags.BPM) == 0)
                    {
                        CLog.CSongLog.Error("[{SongFileName}] BPM tag missing",  CLog.Params(_Song.FileName));
                        return false;
                    }

                    #region check medley tags
                    if ((headerFlags & EHeaderFlags.MedleyStartBeat) != 0 && (headerFlags & EHeaderFlags.MedleyEndBeat) != 0)
                    {
                        if (_Song.Medley.StartBeat > _Song.Medley.EndBeat)
                        {
                            CLog.CSongLog.Error("[{SongFileName}] MedleyStartBeat is bigger than MedleyEndBeat in file: {StartBeat} > {EndBeat}", CLog.Params(_Song.FileName, _Song.Medley.StartBeat > _Song.Medley.EndBeat));
                            headerFlags = headerFlags - EHeaderFlags.MedleyStartBeat - EHeaderFlags.MedleyEndBeat;
                        }
                    }

                    if (_Song.Preview.Source == EDataSource.None)
                    {
                        //PreviewStart is not set or <=0
                        _Song.Preview.StartTime = (headerFlags & EHeaderFlags.MedleyStartBeat) != 0 ? CBase.Game.GetTimeFromBeats(_Song.Medley.StartBeat, _Song.BPM) : 0f;
                        // ReSharper disable CompareOfFloatsByEqualityOperator
                        _Song.Preview.Source = _Song.Preview.StartTime == 0 ? EDataSource.None : EDataSource.Calculated;
                        // ReSharper restore CompareOfFloatsByEqualityOperator
                    }

                    if ((headerFlags & EHeaderFlags.MedleyStartBeat) != 0 && (headerFlags & EHeaderFlags.MedleyEndBeat) != 0)
                    {
                        _Song.Medley.Source = EDataSource.Tag;
                        _Song.Medley.FadeInTime = CBase.Settings.GetDefaultMedleyFadeInTime();
                        _Song.Medley.FadeOutTime = CBase.Settings.GetDefaultMedleyFadeOutTime();
                    }
                    #endregion check medley tags
                }
                catch (Exception e)
                {
                    if (sr != null)
                        sr.Dispose();
                    CLog.CSongLog.Error(e, "[{SongFileName}] Error reading txt header with Message: {ExceptionMessage}", CLog.Params(e.Message, _Song.FileName));
                    return false;
                }
                _Song.Encoding = sr.CurrentEncoding;
                sr.Dispose();
                _Song._CheckFiles();

                CBase.DataBase.GetDataBaseSongInfos(_Song.Artist, _Song.Title, out _Song.NumPlayed, out _Song.DateAdded, out _Song.DataBaseSongID);

                //Before saving this tags to .txt: Check, if ArtistSorting and Artist are equal, then don't save this tag.
                if (String.IsNullOrEmpty(_Song.ArtistSorting))
                    _Song.ArtistSorting = _Song.Artist;

                if (String.IsNullOrEmpty(_Song.TitleSorting))
                    _Song.TitleSorting = _Song.Title;

                return true;
            }

            private static string _UnifyLanguage(string lang)
            {
                if (lang != "")
                {
                    lang = Char.ToUpperInvariant(lang[0]) + lang.Substring(1).ToLowerInvariant();
                    switch (lang)
                    {
                        case "Englisch":
                            lang = "English";
                            break;
                        case "Deutsch":
                            lang = "German";
                            break;
                        case "Spanisch":
                            lang = "Spanish";
                            break;
                    }
                }
                return lang;
            }

            private enum ENoteReadMode
            {
                Normal,
                ZeroBased,
                OneBased
            }

            private class CAutoChanges
            {
                public int ZeroLengthNoteCt;
                public int OverlapNoteCt;
                public int NoTextNoteCt;
                public int NoLengthBreakCt;
                public int AjustedBreakCt;
                public int InvalidPosBreakCt;

                public bool IsModified
                {
                    get { return ZeroLengthNoteCt + OverlapNoteCt + NoTextNoteCt + NoLengthBreakCt + AjustedBreakCt + InvalidPosBreakCt > 0; }
                }

                public override string ToString()
                {
                    string result = "";
                    int skippedNotesCt = ZeroLengthNoteCt + OverlapNoteCt + NoTextNoteCt;
                    if (skippedNotesCt > 0)
                        result += "Skipped " + skippedNotesCt + " notes (0-Length: " + ZeroLengthNoteCt + ", Overlapping: " + OverlapNoteCt + ", No text: " + NoTextNoteCt + ")\r\n";
                    if (InvalidPosBreakCt > 0)
                        result += "Skipped " + InvalidPosBreakCt + " line breaks (Invalid position)\r\n";
                    int adjustedBreakCt = AjustedBreakCt + NoLengthBreakCt;
                    if (adjustedBreakCt > 0)
                        result += "Adjusted " + adjustedBreakCt + " line breaks (Overlapping previous note: " + AjustedBreakCt + ", No length: " + NoLengthBreakCt + ")\r\n";
                    return result;
                }
            }

            private ENoteReadMode _CurrentReadMode = ENoteReadMode.Normal;
            private const int _MaxZeroNoteCt = 1;
            private const int _MaxOverlapNoteCt = 3;

            /// <summary>
            ///     Read notes. First try to read notes normally (assume standard)<br />
            ///     If there are more than _MaxZeroNoteCt with length &lt; 1,  try to read notes adding 1 to length<br />
            ///     Then if there are more than _MaxOverlapNoteCt fallback to first version ignoring notes with length &lt; 1
            /// </summary>
            /// <param name="forceReload"></param>
            /// <returns></returns>
            public bool ReadNotes(bool forceReload = false)
            {
                //Skip loading if already done and no reload is forced
                if (_Song.NotesLoaded && !forceReload)
                    return true;

                string filePath = Path.Combine(_Song.Folder, _Song.FileName);

                if (!File.Exists(filePath))
                {
                    CLog.CSongLog.Error("[{SongFileName}] The file songfile does not exist",  CLog.Params(_Song.FileName));
                    return false;
                }

                int currentBeat = 0; //Used for relative songs
                CSongNote lastNote = null; //Holds last parsed note. Get's reset on player change
                bool endFound = false; // True if end tag was found

                int player = 1;
                _LineNr = 0;

                char[] trimChars = {' ', ':'};
                char[] splitChars = {' '};

                var changesMade = new CAutoChanges();

                StreamReader sr = null;
                try
                {
                    sr = new StreamReader(filePath, _Song.Encoding, true);

                    _Song.Notes.Reset();

                    //Search for Note Beginning
                    while (!sr.EndOfStream && !endFound)
                    {
                        string line = sr.ReadLine();
                        _LineNr++;

                        if (String.IsNullOrEmpty(line))
                            continue;

                        char tag = line[0];
                        //Remove tag and potential space
                        line = (line.Length >= 2 && line[1] == ' ') ? line.Substring(2) : line.Substring(1);

                        int beat, length;
                        switch (tag)
                        {
                            case '#':
                                continue;
                            case 'E':
                                endFound = true;
                                break;
                            case 'P':
                                line = line.Trim(trimChars);

                                if (!int.TryParse(line, out player))
                                {
                                    CLog.CSongLog.Error("[{SongFileName}] Wrong or missing number after \"P\" (in line {LineNr})", CLog.Params(_Song.FileName, _LineNr));
                                    return false;
                                }
                                currentBeat = 0;
                                lastNote = null;
                                break;
                            case ':':
                            case '*':
                            case 'F':
                                string[] noteData = line.Split(splitChars, 4);
                                if (noteData.Length < 4)
                                {
                                    if (noteData.Length == 3)
                                    {
                                        CLog.CSongLog.Warning("[{SongFileName}] Ignored note without text (in line {LineNr})", CLog.Params(_Song.FileName, _LineNr));
                                        changesMade.NoTextNoteCt++;
                                        continue;
                                    }
                                    CLog.CSongLog.Error("[{SongFileName}] Invalid note found (in line {LineNr}): {noteData}", CLog.Params(_Song.FileName, _LineNr, noteData));
                                    sr.Dispose();
                                    return false;
                                }
                                int tone;
                                if (!int.TryParse(noteData[0], out beat) || !int.TryParse(noteData[1], out length) || !int.TryParse(noteData[2], out tone))
                                {
                                    CLog.CSongLog.Error("[{SongFileName}] Invalid note found (non-numeric values) (in line {LineNr}): {noteData}", CLog.Params(_Song.FileName, _LineNr, noteData));
                                    sr.Dispose();
                                    return false;
                                }
                                string text = noteData[3].TrimMultipleWs();
                                if (text == "")
                                {
                                    CLog.CSongLog.Warning("[{SongFileName}] Ignored note without text (in line {LineNr})", CLog.Params(_Song.FileName, _LineNr));
                                    changesMade.NoTextNoteCt++;
                                    continue;
                                }
                                if (_CurrentReadMode == ENoteReadMode.ZeroBased)
                                    length++;
                                if (length < 1)
                                {
                                    changesMade.ZeroLengthNoteCt++;
                                    if (_CurrentReadMode == ENoteReadMode.Normal && changesMade.ZeroLengthNoteCt > _MaxZeroNoteCt && changesMade.OverlapNoteCt <= _MaxOverlapNoteCt)
                                    {
                                        CLog.CSongLog.Warning("[{SongFileName}] Found more than {MaxZeroNoteCt} notes with length < 1. Trying alternative read mode.", CLog.Params(_Song.FileName, _MaxZeroNoteCt));
                                        _CurrentReadMode = ENoteReadMode.ZeroBased;
                                        sr.Dispose();
                                        return ReadNotes(true);
                                    }
                                    CLog.CSongLog.Warning("[{SongFileName}] Ignored note with length < 1 (in line {LineNr})", CLog.Params(_Song.FileName, _LineNr));
                                }
                                else
                                {
                                    ENoteType noteType;

                                    if (tag.Equals('*'))
                                        noteType = ENoteType.Golden;
                                    else if (tag.Equals('F'))
                                        noteType = ENoteType.Freestyle;
                                    else
                                        noteType = ENoteType.Normal;

                                    if (_Song.Relative)
                                        beat += currentBeat;

                                    bool ignored = false;
                                    foreach (int curPlayer in player.GetSetBits())
                                    {
                                        //Create the note here as we want independent instances in the lines. Otherwhise we can't modify them later
                                        lastNote = new CSongNote(beat, length, tone, text, noteType);
                                        if (!_AddNote(curPlayer, lastNote))
                                        {
                                            if (!ignored)
                                            {
                                                ignored = true;
                                                changesMade.OverlapNoteCt++;
                                                if (changesMade.OverlapNoteCt > _MaxOverlapNoteCt && _CurrentReadMode == ENoteReadMode.ZeroBased)
                                                {
                                                    CLog.CSongLog.Warning("[{SongFileName}] Found more than {MaxOverlapNoteCt} overlapping notes. Using standard mode.", CLog.Params(_Song.FileName, _MaxOverlapNoteCt));
                                                    _CurrentReadMode = ENoteReadMode.OneBased;
                                                    sr.Dispose();
                                                    return ReadNotes(true);
                                                }
                                            }
                                            CLog.CSongLog.Warning("[{SongFileName}] Ignored note for player {CurrentPlayerNumber} because it overlaps with other note (in line {LineNr})", CLog.Params(_Song.FileName, (curPlayer + 1), _LineNr));
                                        }
                                    }
                                }
                                break;
                            case '-':
                                string[] lineBreakData = line.Split(splitChars);
                                if (lineBreakData.Length < 1)
                                {
                                    CLog.CSongLog.Error("[{SongFileName}] Invalid line break found (No beat) (in line {LineNr}): {LineBreakData}", CLog.Params(_Song.FileName, _LineNr, lineBreakData));
                                    sr.Dispose();
                                    return false;
                                }
                                if (!int.TryParse(lineBreakData[0], out beat))
                                {
                                    CLog.CSongLog.Error("[{SongFileName}] Invalid line break found (Non-numeric value) (in line {LineNr}): {LineBreakData}", CLog.Params(_Song.FileName, _LineNr, lineBreakData));
                                    sr.Dispose();
                                    return false;
                                }

                                if (_Song.Relative)
                                {
                                    beat += currentBeat;
                                    if (lineBreakData.Length < 2 || !int.TryParse(lineBreakData[1], out length))
                                    {
                                        CLog.CSongLog.Warning("[{SongFileName}] Missing line break length (in line {LineNr}):{LineBreakData}", CLog.Params(_Song.FileName, _LineNr, lineBreakData));
                                        changesMade.NoLengthBreakCt++;
                                        currentBeat = beat;
                                    }
                                    else
                                        currentBeat += length;
                                }

                                if (lastNote != null && beat <= lastNote.EndBeat)
                                {
                                    CLog.CSongLog.Warning("[{SongFileName}] Line break is before previous note end. Adjusted. (in line {LineNr})", CLog.Params(_Song.FileName, _LineNr));
                                    
                                    changesMade.AjustedBreakCt++;
                                    if (_Song.Relative)
                                        currentBeat += lastNote.EndBeat - beat + 1;
                                    beat = lastNote.EndBeat + 1;
                                }

                                if (beat < 1)
                                {
                                    CLog.CSongLog.Warning("[{SongFileName}] Ignored line break because position is < 1 (in line {LineNr})", CLog.Params(_Song.FileName, _LineNr));
                                    changesMade.InvalidPosBreakCt++;
                                }
                                else
                                {
                                    foreach (int curPlayer in player.GetSetBits())
                                    {
                                        if (!_NewSentence(curPlayer, beat))
                                            CLog.CSongLog.Warning("[{SongFileName}] Ignored line break for player {CurPlayerNr} (Overlapping or duplicate) (in line {LineNr})", CLog.Params(_Song.FileName, (curPlayer + 1) , _LineNr));
                                    }
                                }
                                break;
                            default:
                                CLog.CSongLog.Error("[{SongFileName}] Unexpected or missing character ({Tag})", CLog.Params(_Song.FileName, tag));
                                return false;
                        }
                    }

                    for (int i = 0; i < _Song.Notes.VoiceCount; i++)
                    {
                        CVoice voice = _Song.Notes.GetVoice(i);
                        int emptyLines = voice.RemoveEmptyLines();
                        if (emptyLines > 0)
                            CLog.CSongLog.Warning("[{SongFileName}] Removed {NumEmptyLines} empty lines from P .This often indicates a problem with the line breaks in the file", CLog.Params(_Song.FileName, emptyLines));
                        voice.UpdateTimings();
                    }
                }
                catch (Exception e)
                {
                    CLog.CSongLog.Error(e, "[{SongFileName}] An unhandled exception occured: {ExceptionMessage}", CLog.Params(_Song.FileName, e.Message));
                    if (sr != null)
                        sr.Dispose();
                    return false;
                }
                sr.Dispose();
                try
                {
                    _Song._CalcMedley();
                    _Song._CheckPreview();
                    _Song._FindShortEnd();
                    _Song.NotesLoaded = true;
                    if (_Song.IsDuet)
                        _Song._CheckDuet();
                }
                catch (Exception e)
                {
                    CLog.CSongLog.Error(e, "[{SongFileName}] An unhandled exception occured: {ExceptionMessage}", CLog.Params(_Song.FileName, e.Message));
                    return false;
                }

                if (changesMade.IsModified)
                {
                    CLog.Warning("Automatic changes have been made to {FilePath} Please check result!\r\n{ChangesMade}" , CLog.Params(filePath, changesMade));
                    if (CBase.Config.GetSaveModifiedSongs() == EOffOn.TR_CONFIG_ON)
                    {
                        string name = Path.GetFileNameWithoutExtension(_Song.FileName);
                        _Song.Save(Path.Combine(_Song.Folder, name + ".fix"));
                    }
                }
                return true;
            }

            private bool _AddNote(int player, CSongNote note)
            {
                CVoice voice = _Song.Notes.GetVoice(player, true);
                return voice.AddNote(note, false);
            }

            private bool _NewSentence(int player, int start)
            {
                CVoice voice = _Song.Notes.GetVoice(player, true);
                return voice.AddLine(start);
            }
        }
    }
}