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
using VocaluxeLib.Log;

namespace VocaluxeLib.Songs
{
    public partial class CSong
    {
        private class CSongWriter
        {
            private readonly CSong _Song;
            private TextWriter _Tw;

            public CSongWriter(CSong song)
            {
                _Song = song;
            }

            private void _WriteHeaderEntry(string id, string value)
            {
                if (!String.IsNullOrEmpty(value))
                    _Tw.WriteLine("#" + id.ToUpper() + ":" + value);
            }

            private void _WriteHeaderEntry(string id, bool value)
            {
                if (value)
                    _WriteHeaderEntry(id, "YES");
            }

            private void _WriteHeaderEntry(string id, float value, float def = 0f)
            {
                if (Math.Abs(value - def) > 0.0001)
                    _WriteHeaderEntry(id, value.ToInvariantString());
            }

            private void _WriteHeaderEntry(string id, int value, int def = 0)
            {
                if (value != def)
                    _WriteHeaderEntry(id, value.ToString());
            }

            private void _WriteHeaderEntrys(string id, ICollection<string> value)
            {
                if (value == null || value.Count <= 0)
                    return;
                foreach (String val in value)
                    _WriteHeaderEntry(id, val);
            }

            private void _WriteHeader()
            {
                if (_Song.ManualEncoding)
                    _WriteHeaderEntry("ENCODING", _Song.Encoding.GetEncodingName());
                _WriteHeaderEntry("CREATOR", _Song.Creator);
                _WriteHeaderEntry("VERSION", _Song.Version);
                _WriteHeaderEntry("LENGTH", _Song.Length);
                _WriteHeaderEntry("SOURCE", _Song.Source);
                if (!String.IsNullOrEmpty(_Song._Comment))
                {
                    string comment = _Song._Comment.Replace("\r\n", "\n").Replace('\r', '\n');
                    char[] splitChar = {'\n'};
                    _WriteHeaderEntrys("COMMENT", comment.Split(splitChar));
                }
                _WriteHeaderEntry("TITLE", _Song.Title);
                _WriteHeaderEntry("ARTIST", _Song.Artist);
                if (!_Song.Title.Equals(_Song.TitleSorting))
                    _WriteHeaderEntry("TITLE-ON-SORTING", _Song.TitleSorting);
                if (!_Song.Artist.Equals(_Song.ArtistSorting))
                    _WriteHeaderEntry("ARTIST-ON-SORTING", _Song.ArtistSorting);
                _WriteHeaderEntrys("EDITION", _Song.Editions);
                _WriteHeaderEntrys("GENRE", _Song.Genres);
                _WriteHeaderEntrys("LANGUAGE", _Song.Languages);
                _WriteHeaderEntry("ALBUM", _Song.Album);
                _WriteHeaderEntry("YEAR", _Song.Year);
                _WriteHeaderEntry("MP3", _Song.MP3FileName);
                _WriteHeaderEntry("COVER", _Song.CoverFileName);
                _WriteHeaderEntrys("BACKGROUND", _Song.BackgroundFileNames);
                _WriteHeaderEntry("VIDEO", _Song.VideoFileName);
                _WriteHeaderEntry("VIDEOGAP", _Song.VideoGap);
                if (_Song.VideoAspect != EAspect.Crop)
                    _WriteHeaderEntry("VIDEOASPECT", _Song.VideoAspect.ToString());
                _WriteHeaderEntry("RELATIVE", _Song.Relative);
                _WriteHeaderEntry("BPM", _Song.BPM / _BPMFactor);
                _WriteHeaderEntry("GAP", (int)(_Song.Gap * 1000f));
                if (_Song.Preview.Source == EDataSource.Tag)
                    _WriteHeaderEntry("PREVIEWSTART", _Song.Preview.StartTime);
                _WriteHeaderEntry("START", _Song.Start);
                _WriteHeaderEntry("END", (int)(_Song.Finish * 1000f));
                if (_Song.ShortEnd.Source == EDataSource.Tag)
                    _WriteHeaderEntry("ENDSHORT", (int)(CBase.Game.GetTimeFromBeats(_Song.ShortEnd.EndBeat, _Song.BPM) + _Song.Gap) * 1000);
                if (!_Song._CalculateMedley)
                    _WriteHeaderEntry("CALCMEDLEY", "OFF");
                if (_Song.Medley.Source == EDataSource.Tag)
                {
                    _WriteHeaderEntry("MEDLEYSTARTBEAT", _Song.Medley.StartBeat);
                    _WriteHeaderEntry("MEDLEYENDBEAT", _Song.Medley.EndBeat);
                }
                for (int i = 0; i < _Song.Notes.VoiceCount; i++)
                {
                    if (_Song.Notes.VoiceNames.IsSet(i))
                        _WriteHeaderEntry("P" + i, _Song.Notes.VoiceNames[i]);
                }
                foreach (string addLine in _Song.UnknownTags)
                    _Tw.WriteLine(addLine);
            }

            /// <summary>
            ///     Gets a good beat for a line break that is &gt;=firstPossibleBeat and &lt;firstNoteBeat
            ///     Based on algorithm used by YASS: http://www.yass-along.com/errors.html
            /// </summary>
            /// <param name="firstPossibleBeat">First beat that is possible for a line break (equal (just after) previous line note end)</param>
            /// <param name="firstNoteBeat">First note beat on current line</param>
            /// <returns>A good beat for the line break</returns>
            private int _GetBreakBeat(int firstPossibleBeat, int firstNoteBeat)
            {
                int breakBeat;
                int diff = firstNoteBeat - firstPossibleBeat;
                float timeDiff = CBase.Game.GetTimeFromBeats(diff, _Song.BPM);
                if (timeDiff > 4f)
                    breakBeat = firstPossibleBeat + (int)CBase.Game.GetBeatFromTime(2f, _Song.BPM, 0f);
                else if (timeDiff > 2f)
                    breakBeat = firstPossibleBeat + (int)CBase.Game.GetBeatFromTime(1f, _Song.BPM, 0f);
                else if (diff < 2)
                    breakBeat = firstPossibleBeat;
                else if (diff < 9)
                    breakBeat = firstNoteBeat - 2;
                else if (diff < 13)
                    breakBeat = firstNoteBeat - 3;
                else if (diff < 17)
                    breakBeat = firstNoteBeat - 4;
                else
                    breakBeat = firstPossibleBeat + 12;
                return breakBeat;
            }

            private void _WriteNotes()
            {
                for (int i = 0; i < _Song.Notes.VoiceCount; i++)
                {
                    CVoice voice = _Song.Notes.GetVoice(i);
                    if (_Song.Notes.VoiceCount > 1)
                        _Tw.WriteLine("P" + Math.Pow(2, i));
                    int currentBeat = 0;
                    CSongLine lastLine = null;
                    foreach (CSongLine line in voice.Lines)
                    {
                        if (lastLine != null)
                        {
                            string lineTxt = "- " + (_GetBreakBeat(lastLine.EndBeat + 1, line.FirstNoteBeat) - currentBeat);
                            if (_Song.Relative)
                            {
                                lineTxt += " " + (line.FirstNoteBeat - currentBeat);
                                currentBeat = line.FirstNoteBeat;
                            }
                            _Tw.WriteLine(lineTxt);
                        }
                        foreach (CSongNote note in line.Notes)
                        {
                            string tag;
                            switch (note.Type)
                            {
                                case ENoteType.Normal:
                                    tag = ":";
                                    break;
                                case ENoteType.Golden:
                                    tag = "*";
                                    break;
                                case ENoteType.Freestyle:
                                    tag = "F";
                                    break;
                                default:
                                    throw new NotImplementedException("Note type " + note.Type);
                            }
                            _Tw.WriteLine(tag + " " + (note.StartBeat - currentBeat) + " " + note.Duration + " " + note.Tone + " " + note.Text);
                        }
                        lastLine = line;
                    }
                }
                _Tw.WriteLine("E");
            }

            public bool SaveFile(string filePath)
            {
                try
                {
                    _Tw = new StreamWriter(filePath, false, _Song.Encoding);
                    _WriteHeader();
                    _WriteNotes();
                }
                catch (UnauthorizedAccessException)
                {
                    CLog.Error("Cannot write " + filePath + ". Directory might be readonly or requires admin rights.");
                    return false;
                }
                catch (Exception e)
                {
                    CLog.Error("Unhandled exception while writing " + filePath + ": " + e);
                    return false;
                }
                finally
                {
                    if (_Tw != null)
                        _Tw.Dispose();
                }
                return true;
            }
        }
    }
}