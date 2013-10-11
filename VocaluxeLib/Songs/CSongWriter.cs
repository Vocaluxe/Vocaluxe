using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

            private void _WriteHeaderEntry<T>(StreamWriter sw, string id, T value)
            {
                if (!value.Equals(default(T)))
                    sw.WriteLine("#" + id.ToUpper() + ":" + value);
            }

            private void _WriteHeaderEntry(StreamWriter sw, string id, bool value)
            {
                if (value)
                    _WriteHeaderEntry(sw, id, "YES");
            }

            private void _WriteHeaderEntry<T>(StreamWriter sw, string id, IList<T> value)
            {
                if (value!=null && value.Count>0)
                    foreach (var val in value)
                        _WriteHeaderEntry(sw, id, val);
            }

            private void _WriteHeader(StreamWriter sw)
            {
                _WriteHeaderEntry(sw, "ENCODING", _Song.Encoding);
                _WriteHeaderEntry(sw, "TITLE", _Song.Title);
                _WriteHeaderEntry(sw, "ARTIST", _Song.Artist);
                if (!_Song.Title.Equals(_Song.TitleSorting)) _WriteHeaderEntry(sw, "TITLE-ON-SORTING", _Song.TitleSorting);
                if (!_Song.Artist.Equals(_Song.ArtistSorting)) _WriteHeaderEntry(sw, "ARTIST-ON-SORTING", _Song.ArtistSorting);
                _WriteHeaderEntry(sw, "LANGUAGE", _Song.Languages);
                _WriteHeaderEntry(sw, "EDITION", _Song.Edition);
                _WriteHeaderEntry(sw, "GENRE", _Song.Genres);
                _WriteHeaderEntry(sw, "YEAR", _Song.Year);
                _WriteHeaderEntry(sw, "COMMENT", _Song._Comment);
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
                if(!_Song.CalculateMedley)
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
            }
        }
    }
}