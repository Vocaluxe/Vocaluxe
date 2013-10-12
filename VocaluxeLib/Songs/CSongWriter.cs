using System;
using System.Collections.Generic;
using System.IO;

namespace VocaluxeLib.Songs
{
    public partial class CSong
    {
        private class CSongWriter
        {
            private readonly CSong _Song;

            public CSongWriter(CSong song)
            {
                _Song = song;
            }

            private void _WriteHeaderEntry<T>(TextWriter sw, string id, T value)
            {
                if (!value.Equals(default(T)))
                    sw.WriteLine("#" + id.ToUpper() + ":" + value);
            }

            private void _WriteHeaderEntry(TextWriter sw, string id, bool value)
            {
                if (value)
                    _WriteHeaderEntry(sw, id, "YES");
            }

            private void _WriteHeaderEntrys<T>(TextWriter sw, string id, IList<T> value)
            {
                if (value != null && value.Count > 0)
                {
                    foreach (T val in value)
                        _WriteHeaderEntry(sw, id, val);
                }
            }

            private void _WriteHeader(TextWriter sw)
            {
                _WriteHeaderEntry(sw, "ENCODING", _Song.Encoding);
                _WriteHeaderEntry(sw, "CREATOR", _Song.Creator);
                _WriteHeaderEntry(sw, "VERSION", _Song.Version);
                _WriteHeaderEntry(sw, "SOURCE", _Song.Source);
                if (!String.IsNullOrEmpty(_Song._Comment))
                {
                    string comment = _Song._Comment.Replace("\r\n", "\n").Replace('\r', '\n');
                    char[] splitChar = {'\n'};
                    _WriteHeaderEntrys(sw, "COMMENT", comment.Split(splitChar));
                }
                _WriteHeaderEntry(sw, "TITLE", _Song.Title);
                _WriteHeaderEntry(sw, "ARTIST", _Song.Artist);
                if (!_Song.Title.Equals(_Song.TitleSorting))
                    _WriteHeaderEntry(sw, "TITLE-ON-SORTING", _Song.TitleSorting);
                if (!_Song.Artist.Equals(_Song.ArtistSorting))
                    _WriteHeaderEntry(sw, "ARTIST-ON-SORTING", _Song.ArtistSorting);
                _WriteHeaderEntrys(sw, "EDITION", _Song.Editions);
                _WriteHeaderEntrys(sw, "GENRE", _Song.Genres);
                _WriteHeaderEntrys(sw, "LANGUAGE", _Song.Languages);
                _WriteHeaderEntry(sw, "ALBUM", _Song.Album);
                _WriteHeaderEntry(sw, "YEAR", _Song.Year);
                _WriteHeaderEntry(sw, "MP3", _Song.MP3FileName);
                _WriteHeaderEntry(sw, "COVER", _Song.CoverFileName);
                _WriteHeaderEntry(sw, "BACKGROUND", _Song.BackgroundFileName);
                _WriteHeaderEntry(sw, "VIDEO", _Song.VideoFileName);
                _WriteHeaderEntry(sw, "VIDEOGAP", _Song.VideoGap);
                _WriteHeaderEntry(sw, "VIDEOASPECT", _Song.VideoAspect);
                _WriteHeaderEntry(sw, "RELATIVE", _Song.Relative);
                _WriteHeaderEntry(sw, "BPM", _Song.BPM);
                _WriteHeaderEntry(sw, "GAP", _Song.Gap);
                _WriteHeaderEntry(sw, "PREVIEWSTART", _Song.PreviewStart);
                _WriteHeaderEntry(sw, "START", _Song.Start);
                _WriteHeaderEntry(sw, "END", _Song.Finish);
                if (!_Song._CalculateMedley)
                    _WriteHeaderEntry(sw, "CALCMEDLEY", "OFF");
                if (_Song.Medley.Source == EMedleySource.Tag)
                {
                    _WriteHeaderEntry(sw, "MEDLEYSTARTBEAT", _Song.Medley.StartBeat);
                    _WriteHeaderEntry(sw, "MEDLEYENDBEAT", _Song.Medley.EndBeat);
                }
                for (int i = 0; i < _Song.Notes.VoiceCount; i++)
                {
                    if (_Song.Notes.VoiceNames.IsSet(i))
                        _WriteHeaderEntry(sw, "P" + i, _Song.Notes.VoiceNames[i]);
                }
                foreach (string addLine in _Song.UnknownTags)
                    sw.WriteLine(addLine);
            }

            private void _WriteNotes(TextWriter sw)
            {
                for (int i = 0; i < _Song.Notes.VoiceCount; i++)
                {
                    CVoice voice = _Song.Notes.GetVoice(i);
                    if (_Song.Notes.VoiceCount > 1)
                        sw.WriteLine("P" + Math.Pow(2, i));
                    bool firstLine = true;
                    int currentBeat = 0;
                    foreach (CSongLine line in voice.Lines)
                    {
                        if (!firstLine)
                        {
                            string lineTxt = "- " + (line.StartBeat - currentBeat);
                            if (_Song.Relative)
                            {
                                lineTxt += " " + (line.FirstNoteBeat - currentBeat);
                                currentBeat = line.FirstNoteBeat;
                            }
                            sw.WriteLine(lineTxt);
                        }
                        else
                            firstLine = false;
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
                            sw.WriteLine(tag + " " + (note.StartBeat - currentBeat) + " " + note.Duration + " " + note.Tone + " " + note.Text);
                        }
                    }
                }
            }

            public bool SaveFile(string filePath)
            {
                try
                {
                    TextWriter sw = new StreamWriter(filePath, false, _Song.Encoding);
                    _WriteHeader(sw);
                    _WriteNotes(sw);
                }
                catch (Exception e)
                {
                    CBase.Log.LogError("Unhandled exception while writing " + filePath + ": " + e);
                    return false;
                }
                return true;
            }
        }
    }
}