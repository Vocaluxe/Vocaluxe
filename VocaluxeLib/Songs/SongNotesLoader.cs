using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VocaluxeLib.Songs
{
    /// <summary>
    /// Part of CSong that is required for note loading
    /// </summary>
    public partial class CSong
    {
        private class CSongLoader
        {
            private readonly CSong _Song;

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

            private void _LogReadError(string error, int lineNr)
            {
                CBase.Log.LogError(error + " in line #" + lineNr + "(" + Path.Combine(_Song.Folder, _Song.FileName) + ")");
            }

            public bool ReadHeader(Encoding encoding = null)
            {
                string filePath = Path.Combine(_Song.Folder, _Song.FileName);

                if (!File.Exists(filePath))
                    return false;

                _Song.Language.Clear();
                _Song.Genre.Clear();
                _Song._Comment.Clear();

                var headerFlags = new EHeaderFlags();
                StreamReader sr = null;
                int lineNr = 0;
                try
                {
                    sr = new StreamReader(filePath, Encoding.Default, true);
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        lineNr++;
                        if (line == "")
                            continue;
                        if (!line[0].Equals('#'))
                            break;

                        int pos = line.IndexOf(":", StringComparison.Ordinal);

                        if (pos <= 1)
                            continue;
                        string identifier = line.Substring(1, pos - 1).Trim().ToUpper();
                        string value = line.Substring(pos + 1).Trim();

                        if (value == "")
                        {
                            _LogReadError("Warning: Empty value skipped", lineNr);
                            continue;
                        }

                        switch (identifier)
                        {
                            case "ENCODING":
                                if (encoding != null)
                                {
                                    _LogReadError("Warning: Duplicate encoding ignored", lineNr);
                                    continue;
                                }
                                Encoding newEncoding = CEncoding.GetEncoding(value);
                                if (!newEncoding.Equals(sr.CurrentEncoding))
                                {
                                    sr.Dispose();
                                    sr = null;
                                    return ReadHeader(_Song.Encoding);
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
                            case "DUETSINGERP1":
                            case "P1":
                                _Song.Notes.VoiceNames[0] = value;
                                break;
                            case "DUETSINGERP2":
                            case "P2":
                                _Song.Notes.VoiceNames[1] = value;
                                break;
                            case "MP3":
                                if (File.Exists(Path.Combine(_Song.Folder, value)))
                                {
                                    _Song.MP3FileName = value;
                                    headerFlags |= EHeaderFlags.MP3;
                                }
                                else
                                {
                                    _LogReadError("Error: Can't find audio file: " + Path.Combine(_Song.Folder, value), lineNr);
                                    return false;
                                }
                                break;
                            case "BPM":
                                if (CHelper.TryParse(value, out _Song.BPM))
                                {
                                    _Song.BPM *= 4;
                                    headerFlags |= EHeaderFlags.BPM;
                                }
                                else
                                    _LogReadError("Warning: Invalid BPM value", lineNr);
                                break;
                            case "EDITION":
                                if (value.Length > 1)
                                    _Song.Edition.Add(value);
                                else
                                    _LogReadError("Warning: Invalid edition", lineNr);
                                break;
                            case "GENRE":
                                if (value.Length > 1)
                                    _Song.Genre.Add(value);
                                else
                                    _LogReadError("Warning: Invalid genre", lineNr);
                                break;
                            case "YEAR":
                                int num;
                                if (value.Length == 4 && int.TryParse(value, out num) && num > 0)
                                    _Song.Year = value;
                                else
                                    _LogReadError("Warning: Invalid year", lineNr);
                                break;
                            case "LANGUAGE":
                                if (value.Length > 1)
                                    _Song.Language.Add(value);
                                else
                                    _LogReadError("Warning: Invalid language", lineNr);
                                break;
                            case "COMMENT":
                                if (value.Length > 1)
                                    _Song._Comment.Add(value);
                                else
                                    _LogReadError("Warning: Invalid comment", lineNr);
                                break;
                            case "GAP":
                                if (CHelper.TryParse(value, out _Song.Gap))
                                    _Song.Gap /= 1000f;
                                else
                                    _LogReadError("Warning: Invalid gap", lineNr);
                                break;
                            case "COVER":
                                if (File.Exists(Path.Combine(_Song.Folder, value)))
                                    _Song.CoverFileName = value;
                                else
                                    _LogReadError("Warning: Can't find cover file: " + Path.Combine(_Song.Folder, value), lineNr);
                                break;
                            case "BACKGROUND":
                                if (File.Exists(Path.Combine(_Song.Folder, value)))
                                    _Song.BackgroundFileName = value;
                                else
                                    _LogReadError("Warning: Can't find background file: " + Path.Combine(_Song.Folder, value), lineNr);
                                break;
                            case "VIDEO":
                                if (File.Exists(Path.Combine(_Song.Folder, value)))
                                    _Song.VideoFileName = value;
                                else
                                    CBase.Log.LogError("Warning: Can't find video file: " + Path.Combine(_Song.Folder, value));
                                break;
                            case "VIDEOGAP":
                                if (!CHelper.TryParse(value, out _Song.VideoGap))
                                    _LogReadError("Warning: Invalid videogap", lineNr);
                                break;
                            case "VIDEOASPECT":
                                if (!CHelper.TryParse(value, out _Song.VideoAspect, true))
                                    _LogReadError("Warning: Invalid videoaspect", lineNr);
                                break;
                            case "START":
                                if (!CHelper.TryParse(value, out _Song.Start))
                                    _LogReadError("Warning: Invalid start", lineNr);
                                break;
                            case "END":
                                if (CHelper.TryParse(value, out _Song.Finish))
                                    _Song.Finish /= 1000f;
                                else
                                    _LogReadError("Warning: Invalid end", lineNr);
                                break;
                            case "PREVIEWSTART":
                                if (CHelper.TryParse(value, out _Song.PreviewStart) && _Song.PreviewStart >= 0f)
                                    headerFlags |= EHeaderFlags.PreviewStart;
                                else
                                    _LogReadError("Warning: Invalid previewstart", lineNr);
                                break;
                            case "MEDLEYSTARTBEAT":
                                if (int.TryParse(value, out _Song.Medley.StartBeat))
                                    headerFlags |= EHeaderFlags.MedleyStartBeat;
                                else
                                    _LogReadError("Warning: Invalid medleystartbeat", lineNr);
                                break;
                            case "MEDLEYENDBEAT":
                                if (int.TryParse(value, out _Song.Medley.EndBeat))
                                    headerFlags |= EHeaderFlags.MedleyEndBeat;
                                else
                                    _LogReadError("Warning: Invalid medleyendbeat", lineNr);
                                break;
                            case "CALCMEDLEY":
                                if (value.ToUpper() == "OFF")
                                    _Song.CalculateMedley = false;
                                break;
                            case "RELATIVE":
                                if (value.ToUpper() == "YES")
                                    _Song.Relative = true;
                                break;
                        }
                    } //end of while

                    if (sr.EndOfStream)
                    {
                        //No other data then header
                        CBase.Log.LogError("Lyrics/Notes missing: " + filePath);
                        return false;
                    }

                    if ((headerFlags & EHeaderFlags.Title) == 0)
                    {
                        CBase.Log.LogError("Title tag missing: " + filePath);
                        return false;
                    }

                    if ((headerFlags & EHeaderFlags.Artist) == 0)
                    {
                        CBase.Log.LogError("Artist tag missing: " + filePath);
                        return false;
                    }

                    if ((headerFlags & EHeaderFlags.MP3) == 0)
                    {
                        CBase.Log.LogError("MP3 tag missing: " + filePath);
                        return false;
                    }

                    if ((headerFlags & EHeaderFlags.BPM) == 0)
                    {
                        CBase.Log.LogError("BPM tag missing: " + filePath);
                        return false;
                    }

                    #region check medley tags
                    if ((headerFlags & EHeaderFlags.MedleyStartBeat) != 0 && (headerFlags & EHeaderFlags.MedleyEndBeat) != 0)
                    {
                        if (_Song.Medley.StartBeat > _Song.Medley.EndBeat)
                        {
                            CBase.Log.LogError("MedleyStartBeat is bigger than MedleyEndBeat in file: " + filePath);
                            headerFlags = headerFlags - EHeaderFlags.MedleyStartBeat - EHeaderFlags.MedleyEndBeat;
                        }
                    }

                    if ((headerFlags & EHeaderFlags.PreviewStart) == 0 || _Song.PreviewStart < 0)
                    {
                        //PreviewStart is not set or <=0
                        _Song.PreviewStart = (headerFlags & EHeaderFlags.MedleyStartBeat) != 0 ? CBase.Game.GetTimeFromBeats(_Song.Medley.StartBeat, _Song.BPM) : 0f;
                    }

                    if ((headerFlags & EHeaderFlags.MedleyStartBeat) != 0 && (headerFlags & EHeaderFlags.MedleyEndBeat) != 0)
                    {
                        _Song.Medley.Source = EMedleySource.Tag;
                        _Song.Medley.FadeInTime = CBase.Settings.GetDefaultMedleyFadeInTime();
                        _Song.Medley.FadeOutTime = CBase.Settings.GetDefaultMedleyFadeOutTime();
                    }
                    #endregion check medley tags

                    _Song.Encoding = sr.CurrentEncoding;
                }
                catch (Exception e)
                {
                    if (sr != null)
                        sr.Dispose();
                    _LogReadError("Error reading txt header" + e.Message, lineNr);
                    return false;
                }
                sr.Dispose();
                _Song._CheckFiles();

                CBase.DataBase.GetDataBaseSongInfos(_Song.Artist, _Song.Title, out _Song.NumPlayed, out _Song.DateAdded, out _Song.DataBaseSongID);

                //Before saving this tags to .txt: Check, if ArtistSorting and Artist are equal, then don't save this tag.
                if (_Song.ArtistSorting == "")
                    _Song.ArtistSorting = _Song.Artist;

                if (_Song.TitleSorting == "")
                    _Song.TitleSorting = _Song.Title;

                return true;
            }

            public bool ReadNotes()
            {
                return _ReadNotes();
            }

            private bool _ReadNotes(bool forceReload = false)
            {
                //Skip loading if already done and no reload is forced
                if (_Song.NotesLoaded && !forceReload)
                    return true;

                string filePath = Path.Combine(_Song.Folder, _Song.FileName);

                if (!File.Exists(filePath))
                {
                    CBase.Log.LogError("Error loading song. The file does not exist: " + filePath);
                    return false;
                }

                int currentBeat = 0; //Used for relative songs
                int lastNoteEnd = 0;
                bool endFound = false;

                int player = 1;
                int lineNr = 0;

                char[] trimChars = {' ', ':'};
                char[] splitChars = {' '};

                StreamReader sr = null;
                try
                {
                    sr = new StreamReader(filePath, _Song.Encoding);

                    _Song.Notes.Reset();

                    //Search for Note Beginning
                    while (!sr.EndOfStream && !endFound)
                    {
                        string line = sr.ReadLine();
                        lineNr++;

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
                                    _LogReadError("Error: Wrong or missing number after \"P\"", lineNr);
                                    return false;
                                }
                                sr.ReadLine();
                                break;
                            case ':':
                            case '*':
                            case 'F':
                                string[] noteData = line.Split(splitChars, 4);
                                if (noteData.Length < 4)
                                {
                                    if (noteData.Length == 3)
                                    {
                                        _LogReadError("Warning: Ignored note without text", lineNr);
                                        continue;
                                    }
                                    _LogReadError("Error: Invalid note found", lineNr);
                                    sr.Dispose();
                                    return false;
                                }
                                int tone;
                                if (!int.TryParse(noteData[0], out beat) || !int.TryParse(noteData[1], out length) || !int.TryParse(noteData[2], out tone))
                                {
                                    _LogReadError("Error: Invalid note found (non-numeric values)", lineNr);
                                    sr.Dispose();
                                    return false;
                                }
                                string text = noteData[3];
                                if (text.Trim() == "")
                                {
                                    _LogReadError("Warning: Ignored note without text", lineNr);
                                    continue;
                                }
                                if (length < 1)
                                    _LogReadError("Warning: Ignored note with length < 1", lineNr);
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

                                    int curPlayer = 0;
                                    int tmpPlayer = player;
                                    //Evaluate as bitset
                                    while (tmpPlayer > 0)
                                    {
                                        if ((tmpPlayer & 1) != 0)
                                        {
                                            if (!_ParseNote(curPlayer, noteType, beat, length, tone, text))
                                                _LogReadError("Warning: Ignored note for player " + (curPlayer + 1) + " because it overlaps with other note", lineNr);
                                        }
                                        tmpPlayer >>= 1;
                                        curPlayer++;
                                    }
                                }
                                lastNoteEnd = beat + length;
                                break;
                            case '-':
                                string[] lineBreakData = line.Split(splitChars);
                                if (lineBreakData.Length < 1)
                                {
                                    _LogReadError("Error: Invalid line break found (No beat)", lineNr);
                                    sr.Dispose();
                                    return false;
                                }
                                if (!int.TryParse(lineBreakData[0], out beat))
                                {
                                    _LogReadError("Error: Invalid line break found (Non-numeric value)", lineNr);
                                    sr.Dispose();
                                    return false;
                                }

                                if (_Song.Relative)
                                {
                                    beat += currentBeat;
                                    if (lineBreakData.Length < 2 || !int.TryParse(lineBreakData[1], out length))
                                        _LogReadError("Warning: Missing line break length", lineNr);
                                    else
                                        currentBeat += length;
                                }

                                if (beat < lastNoteEnd)
                                {
                                    _LogReadError("Warning: Line break is before previous note end. Adjusted it (might not work for relative songs)", lineNr);
                                    beat = lastNoteEnd;
                                }

                                if (beat < 1)
                                    _LogReadError("Warning: Ignored line break because position is < 1", lineNr);
                                else
                                {
                                    int curPlayer = 0;
                                    int tmpPlayer = player;
                                    //Evaluate as bitset
                                    while (tmpPlayer > 0)
                                    {
                                        if ((tmpPlayer & 1) != 0)
                                        {
                                            if (_NewSentence(curPlayer, beat))
                                                _LogReadError("Warning: Ignored line break for player " + (curPlayer + 1) + " (Overlapping or duplicate)", lineNr);
                                        }
                                        tmpPlayer >>= 1;
                                        curPlayer++;
                                    }
                                }
                                break;
                            default:
                                _LogReadError("Error loading song. Unexpected or missing character (" + tag + ")", lineNr);
                                return false;
                        }
                    }

                    foreach (CVoice voice in _Song.Notes.Voices)
                        voice.UpdateTimings();
                }
                catch (Exception e)
                {
                    _LogReadError("Error: An unhandled exception occured (" + e.Message + ")", lineNr);
                    if (sr != null)
                        sr.Dispose();
                    return false;
                }
                sr.Dispose();
                try
                {
                    _Song._FindRefrain();
                    _Song._FindShortEnd();
                    _Song.NotesLoaded = true;
                    if (_Song.IsDuet)
                        _Song._CheckDuet();
                }
                catch (Exception e)
                {
                    CBase.Log.LogError("Error loading song. An unhandled exception occured (" + e.Message + "): " + filePath);
                    return false;
                }
                return true;
            }

            private bool _ParseNote(int player, ENoteType noteType, int start, int length, int tone, string text)
            {
                var note = new CSongNote(start, length, tone, text, noteType);
                CVoice voice = _Song.Notes.GetVoice(player);
                return voice.AddNote(note, false);
            }

            private bool _NewSentence(int player, int start)
            {
                CVoice voice = _Song.Notes.GetVoice(player);
                return voice.AddLine(start);
            }
        }
    }
}