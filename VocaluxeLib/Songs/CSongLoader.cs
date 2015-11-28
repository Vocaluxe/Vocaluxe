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

            /// <summary>
            ///     Logs a given message with file name and line#
            /// </summary>
            /// <param name="msg">Message</param>
            /// <param name="error">True prepends "Error: "; False prepends "Warning: "</param>
            /// <param name="withLineNr">Adds line#</param>
            private void _LogMsg(string msg, bool error, bool withLineNr)
            {
                msg = (error ? "Error: " : "Warning: ") + msg;
                if (withLineNr)
                    msg += " in line #" + _LineNr;
                CBase.Log.LogSongInfo(msg + " (" + Path.Combine(_Song.Folder, _Song.FileName) + ")");
            }

            private void _LogError(string msg, bool withLineNr = true)
            {
                _LogMsg(msg, true, withLineNr);
            }

            private void _LogWarning(string msg, bool withLineNr = true)
            {
                _LogMsg(msg, false, withLineNr);
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
                            _LogWarning("Empty value skipped");
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
                                        _LogWarning("Duplicate encoding ignored");
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
                                    _LogError("Can't find audio file: " + Path.Combine(_Song.Folder, value));
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
                                    _LogWarning("Invalid BPM value");
                                break;
                            case "EDITION":
                                if (value.Length > 1)
                                    _Song.Editions.Add(value);
                                else
                                    _LogWarning("Invalid edition");
                                break;
                            case "GENRE":
                                if (value.Length > 1)
                                    _Song.Genres.Add(value);
                                else
                                    _LogWarning("Invalid genre");
                                break;
                            case "ALBUM":
                                _Song.Album = value;
                                break;
                            case "YEAR":
                                int num;
                                if (value.Length == 4 && int.TryParse(value, out num) && num > 0)
                                    _Song.Year = value;
                                else
                                    _LogWarning("Invalid year");
                                break;
                            case "LANGUAGE":
                                if (value.Length > 1)
                                    _Song.Languages.Add(_UnifyLanguage(value));
                                else
                                    _LogWarning("Invalid language");
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
                                    _LogWarning("Invalid gap");
                                break;
                            case "COVER":
                                if (File.Exists(Path.Combine(_Song.Folder, value)))
                                    _Song.CoverFileName = value;
                                else
                                    _LogWarning("Can't find cover file: " + Path.Combine(_Song.Folder, value));
                                break;
                            case "BACKGROUND":
                                if (File.Exists(Path.Combine(_Song.Folder, value)))
                                    _Song.BackgroundFileNames.Add(value);
                                else
                                    _LogWarning("Can't find background file: " + Path.Combine(_Song.Folder, value));
                                break;
                            case "VIDEO":
                                if (File.Exists(Path.Combine(_Song.Folder, value)))
                                    _Song.VideoFileName = value;
                                else
                                    _LogWarning("Can't find video file: " + Path.Combine(_Song.Folder, value));
                                break;
                            case "VIDEOGAP":
                                if (!CHelper.TryParse(value, out _Song.VideoGap))
                                    _LogWarning("Invalid videogap");
                                break;
                            case "VIDEOASPECT":
                                if (!CHelper.TryParse(value, out _Song.VideoAspect, true))
                                    _LogWarning("Invalid videoaspect");
                                break;
                            case "START":
                                if (!CHelper.TryParse(value, out _Song.Start))
                                    _LogWarning("Invalid start");
                                break;
                            case "END":
                                if (CHelper.TryParse(value, out _Song.Finish))
                                    _Song.Finish /= 1000f;
                                else
                                    _LogWarning("Invalid end");
                                break;
                            case "PREVIEWSTART":
                                if (CHelper.TryParse(value, out _Song.Preview.StartTime) && _Song.Preview.StartTime >= 0f)
                                    _Song.Preview.Source = EDataSource.Tag;
                                else
                                    _LogWarning("Invalid previewstart");
                                break;
                            case "PREVIEW":
                                if (CHelper.TryParse(value, out _Song.Preview.StartTime) && _Song.Preview.StartTime >= 0f)
                                {
                                    //This is stored in ms not like PREVIEWSTART!
                                    _Song.Preview.StartTime /= 1000f;
                                    _Song.Preview.Source = EDataSource.Tag;
                                }
                                else
                                    _LogWarning("Invalid previewstart");
                                break;
                            case "MEDLEYSTARTBEAT":
                                if (int.TryParse(value, out _Song.Medley.StartBeat))
                                    headerFlags |= EHeaderFlags.MedleyStartBeat;
                                else
                                    _LogWarning("Invalid medleystartbeat");
                                break;
                            case "MEDLEYENDBEAT":
                                if (int.TryParse(value, out _Song.Medley.EndBeat))
                                    headerFlags |= EHeaderFlags.MedleyEndBeat;
                                else
                                    _LogWarning("Invalid medleyendbeat");
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
                                        _LogWarning("Invalid shortendbeat");
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
                                    _LogWarning("Unknown tag: #" + identifier);
                                }

                                break;
                        }
                    } //end of while

                    if (sr.EndOfStream)
                    {
                        //No other data then header
                        _LogError("Lyrics/Notes missing", false);
                        return false;
                    }

                    if ((headerFlags & EHeaderFlags.Title) == 0)
                    {
                        _LogError("Title tag missing", false);
                        return false;
                    }

                    if ((headerFlags & EHeaderFlags.Artist) == 0)
                    {
                        _LogError("Artist tag missing", false);
                        return false;
                    }

                    if ((headerFlags & EHeaderFlags.MP3) == 0)
                    {
                        _LogError("MP3 tag missing", false);
                        return false;
                    }

                    if ((headerFlags & EHeaderFlags.BPM) == 0)
                    {
                        _LogError("BPM tag missing", false);
                        return false;
                    }

                    #region check medley tags
                    if ((headerFlags & EHeaderFlags.MedleyStartBeat) != 0 && (headerFlags & EHeaderFlags.MedleyEndBeat) != 0)
                    {
                        if (_Song.Medley.StartBeat > _Song.Medley.EndBeat)
                        {
                            _LogError("MedleyStartBeat is bigger than MedleyEndBeat in file", false);
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
                    _LogError("Error reading txt header" + e.Message, false);
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
                    _LogError("The file does not exist", false);
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
                                    _LogError("Wrong or missing number after \"P\"");
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
                                        _LogWarning("Ignored note without text");
                                        changesMade.NoTextNoteCt++;
                                        continue;
                                    }
                                    _LogError("Invalid note found");
                                    sr.Dispose();
                                    return false;
                                }
                                int tone;
                                if (!int.TryParse(noteData[0], out beat) || !int.TryParse(noteData[1], out length) || !int.TryParse(noteData[2], out tone))
                                {
                                    _LogError("Invalid note found (non-numeric values)");
                                    sr.Dispose();
                                    return false;
                                }
                                string text = noteData[3].TrimMultipleWs();
                                if (text == "")
                                {
                                    _LogWarning("Ignored note without text");
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
                                        _LogWarning("Found more than " + _MaxZeroNoteCt + " note with length < 1. Trying alternative read mode.");
                                        _CurrentReadMode = ENoteReadMode.ZeroBased;
                                        sr.Dispose();
                                        return ReadNotes(true);
                                    }
                                    _LogWarning("Ignored note with length < 1");
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
                                                    _LogWarning("Found more than " + _MaxOverlapNoteCt + " overlapping notes. Using standard mode.");
                                                    _CurrentReadMode = ENoteReadMode.OneBased;
                                                    sr.Dispose();
                                                    return ReadNotes(true);
                                                }
                                            }
                                            _LogWarning("Ignored note for player " + (curPlayer + 1) + " because it overlaps with other note");
                                        }
                                    }
                                }
                                break;
                            case '-':
                                string[] lineBreakData = line.Split(splitChars);
                                if (lineBreakData.Length < 1)
                                {
                                    _LogError("Invalid line break found (No beat)");
                                    sr.Dispose();
                                    return false;
                                }
                                if (!int.TryParse(lineBreakData[0], out beat))
                                {
                                    _LogError("Invalid line break found (Non-numeric value)");
                                    sr.Dispose();
                                    return false;
                                }

                                if (_Song.Relative)
                                {
                                    beat += currentBeat;
                                    if (lineBreakData.Length < 2 || !int.TryParse(lineBreakData[1], out length))
                                    {
                                        _LogWarning("Missing line break length");
                                        changesMade.NoLengthBreakCt++;
                                        currentBeat = beat;
                                    }
                                    else
                                        currentBeat += length;
                                }

                                if (lastNote != null && beat <= lastNote.EndBeat)
                                {
                                    _LogWarning("Line break is before previous note end. Adjusted.");
                                    changesMade.AjustedBreakCt++;
                                    if (_Song.Relative)
                                        currentBeat += lastNote.EndBeat - beat + 1;
                                    beat = lastNote.EndBeat + 1;
                                }

                                if (beat < 1)
                                {
                                    _LogWarning("Ignored line break because position is < 1");
                                    changesMade.InvalidPosBreakCt++;
                                }
                                else
                                {
                                    foreach (int curPlayer in player.GetSetBits())
                                    {
                                        if (!_NewSentence(curPlayer, beat))
                                            _LogWarning("Ignored line break for player " + (curPlayer + 1) + " (Overlapping or duplicate)");
                                    }
                                }
                                break;
                            default:
                                _LogError("Unexpected or missing character (" + tag + ")");
                                return false;
                        }
                    }

                    for (int i = 0; i < _Song.Notes.VoiceCount; i++)
                    {
                        CVoice voice = _Song.Notes.GetVoice(i);
                        int emptyLines = voice.RemoveEmptyLines();
                        if (emptyLines > 0)
                            _LogWarning("Removed " + emptyLines + " empty lines from P" + ". This often indicates a problem with the line breaks in the file", false);
                        voice.UpdateTimings();
                    }
                }
                catch (Exception e)
                {
                    _LogError("An unhandled exception occured (" + e.Message + ")");
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
                    _LogError("An unhandled exception occured (" + e.Message + ")", false);
                    return false;
                }

                if (changesMade.IsModified)
                {
                    string msg = "Automatic changes have been made to " + filePath + " Please check result!\r\n" + changesMade;
                    CBase.Log.LogError("Warning: " + msg);
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