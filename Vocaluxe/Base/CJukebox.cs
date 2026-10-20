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
using System.Linq;
using VocaluxeLib;
using VocaluxeLib.Songs;

namespace Vocaluxe.Base
{
    public enum EJukeboxOrder
    {
        TR_JUKEBOX_ORDER_RANDOM,
        TR_JUKEBOX_ORDER_SORTED
    }

    public enum EJukeboxSection
    {
        TR_JUKEBOX_SECTION_FULL,
        TR_JUKEBOX_SECTION_MEDLEY,
        TR_JUKEBOX_SECTION_30,
        TR_JUKEBOX_SECTION_60,
        TR_JUKEBOX_SECTION_90
    }

    public class CJukeboxEntry
    {
        public readonly int SongId;
        public readonly int OriginalIndex;

        public CJukeboxEntry(int songId, int originalIndex)
        {
            SongId = songId;
            OriginalIndex = originalIndex;
        }

        public CSong Song
        {
            get { return CSongs.GetSong(SongId); }
        }
    }

    static class CJukebox
    {
        private static readonly List<CJukeboxEntry> _Queue = new List<CJukeboxEntry>();

        public static ESongSource Source { get; set; } = ESongSource.TR_SONGSOURCE_ALLSONGS;
        public static int PlaylistId { get; set; } = -1;
        public static ESongSorting Sorting { get; set; } = ESongSorting.TR_CONFIG_GENRE;
        public static int Category { get; set; }
        public static EJukeboxOrder Order { get; set; } = EJukeboxOrder.TR_JUKEBOX_ORDER_RANDOM;
        public static EJukeboxSection Section { get; set; } = EJukeboxSection.TR_JUKEBOX_SECTION_FULL;
        public static bool ShowLyrics { get; set; } = true;
        public static bool ShowVideo { get; set; } = true;
        public static EAudioMode AudioMode { get; set; } = EAudioMode.TR_AUDIOMODE_NORMAL;

        public static bool Shuffle { get; private set; }
        public static bool Repeat { get; set; }
        public static int Current { get; private set; }
        public static bool SingRequested { get; private set; }
        public static bool ResumeAfterSinging { get; private set; }

        public static IList<CJukeboxEntry> Queue
        {
            get { return _Queue.AsReadOnly(); }
        }

        public static CJukeboxEntry CurrentEntry
        {
            get { return Current >= 0 && Current < _Queue.Count ? _Queue[Current] : null; }
        }

        public static CJukeboxEntry NextEntry
        {
            get
            {
                var next = _GetNextIndex();
                return next >= 0 ? _Queue[next] : null;
            }
        }

        public static List<int> GetSongIds()
        {
            var ids = new List<int>();
            switch (Source)
            {
                case ESongSource.TR_SONGSOURCE_PLAYLIST:
                    if (PlaylistId >= 0)
                    {
                        for (var i = 0; i < CPlaylists.GetSongCount(PlaylistId); i++)
                        {
                            ids.Add(CPlaylists.GetSong(PlaylistId, i).SongId);
                        }
                    }

                    break;

                case ESongSource.TR_SONGSOURCE_CATEGORY:
                    if (Category >= 0 && Category < CSongs.NumCategories)
                    {
                        ids.AddRange(CSongs.GetCategoryByIndex(Category).Songs.Select(pointer => pointer.SongId));
                    }

                    break;

                default:
                    ids.AddRange(CSongs.AllSongs.OrderBy(song => song.Artist, StringComparer.CurrentCultureIgnoreCase)
                                       .ThenBy(song => song.Title, StringComparer.CurrentCultureIgnoreCase)
                                       .Select(song => song.Id));
                    break;
            }

            return ids.Where(id => CSongs.GetSong(id) != null).ToList();
        }

        public static bool Start()
        {
            _Queue.Clear();
            var ids = GetSongIds();
            for (var i = 0; i < ids.Count; i++)
            {
                _Queue.Add(new CJukeboxEntry(ids[i], i));
            }

            Shuffle = Order == EJukeboxOrder.TR_JUKEBOX_ORDER_RANDOM;
            if (Shuffle)
            {
                _ShuffleRange(0);
            }

            Current = 0;
            Repeat = false;
            SingRequested = false;
            ResumeAfterSinging = false;
            return _Queue.Count > 0;
        }

        public static void SetShuffle(bool shuffle)
        {
            if (Shuffle == shuffle)
            {
                return;
            }

            Shuffle = shuffle;
            if (shuffle)
            {
                _ShuffleRange(Current + 1);
            }
            else
            {
                var upcoming = _Queue.Skip(Current + 1).OrderBy(entry => entry.OriginalIndex).ToList();
                _Queue.RemoveRange(Current + 1, upcoming.Count);
                _Queue.AddRange(upcoming);
            }
        }

        public static bool Next()
        {
            var next = _GetNextIndex();
            if (next < 0)
            {
                return false;
            }

            if (next <= Current && Shuffle)
            {
                var current = _Queue[Current];
                _ShuffleRange(0);
                if (_Queue.Count > 1 && _Queue[0] == current)
                {
                    _Queue.RemoveAt(0);
                    _Queue.Add(current);
                }

                next = 0;
            }

            Current = next;
            return true;
        }

        public static bool Previous()
        {
            if (Current <= 0)
            {
                return false;
            }

            Current--;
            return true;
        }

        public static void Play(int index)
        {
            if (index >= 0 && index < _Queue.Count)
            {
                Current = index;
            }
        }

        public static void PlayNext(int index)
        {
            if (index < 0 || index >= _Queue.Count || index == Current)
            {
                return;
            }

            var entry = _Queue[index];
            _Queue.RemoveAt(index);
            if (index < Current)
            {
                Current--;
            }

            _Queue.Insert(Current + 1, entry);
        }

        public static void Remove(int index)
        {
            if (index < 0 || index >= _Queue.Count || index == Current)
            {
                return;
            }

            _Queue.RemoveAt(index);
            if (index < Current)
            {
                Current--;
            }
        }

        public static bool Move(int index, int direction)
        {
            var target = index + direction;
            if (index < 0 || index >= _Queue.Count || target < 0 || target >= _Queue.Count)
            {
                return false;
            }

            var entry = _Queue[index];
            _Queue[index] = _Queue[target];
            _Queue[target] = entry;
            if (Current == index)
            {
                Current = target;
            }
            else if (Current == target)
            {
                Current = index;
            }

            return true;
        }

        public static void RequestSinging()
        {
            SingRequested = true;
            ResumeAfterSinging = true;
        }

        public static bool ReturnAfterSinging()
        {
            if (!SingRequested)
            {
                return false;
            }

            SingRequested = false;
            CGraphics.FadeTo(EScreen.JukeboxPlayer);
            return true;
        }

        public static bool TakeResume()
        {
            var resume = ResumeAfterSinging;
            ResumeAfterSinging = false;
            return resume;
        }

        public static EAudioMode GetAudioMode(CJukeboxEntry entry)
        {
            var song = entry == null ? null : entry.Song;
            return song != null && song.HasInstrumental() && AudioMode == EAudioMode.TR_AUDIOMODE_INSTRUMENTAL
                       ? EAudioMode.TR_AUDIOMODE_INSTRUMENTAL
                       : EAudioMode.TR_AUDIOMODE_NORMAL;
        }

        public static void GetSection(CSong song, float length, out float start, out float end)
        {
            start = 0f;
            end = length;
            if (song == null)
            {
                return;
            }

            switch (Section)
            {
                case EJukeboxSection.TR_JUKEBOX_SECTION_MEDLEY:
                    if (song.Medley.Source != EDataSource.None)
                    {
                        start = Math.Max(0f, CGame.GetTimeFromBeats(song.Medley.StartBeat, song.Bpm) + song.Gap - song.Medley.FadeInTime);
                        end = Math.Min(length, CGame.GetTimeFromBeats(song.Medley.EndBeat, song.Bpm) + song.Gap + song.Medley.FadeOutTime);
                    }

                    break;

                case EJukeboxSection.TR_JUKEBOX_SECTION_30:
                case EJukeboxSection.TR_JUKEBOX_SECTION_60:
                case EJukeboxSection.TR_JUKEBOX_SECTION_90:
                    var duration = _GetSectionDuration();
                    start = song.Preview.Source != EDataSource.None ? song.Preview.StartTime : Math.Max(0f, GetFirstNoteTime(song) - 5f);
                    if (length > 0f && start + duration > length)
                    {
                        start = Math.Max(0f, length - duration);
                    }

                    end = length > 0f ? Math.Min(length, start + duration) : start + duration;
                    break;
            }

            if (end <= start)
            {
                start = 0f;
                end = length;
            }
        }

        public static float GetFirstNoteTime(CSong song)
        {
            if (song == null)
            {
                return 0f;
            }

            var firstBeat = int.MaxValue;
            for (var voice = 0; voice < song.Notes.VoiceCount; voice++)
            {
                var lines = song.Notes.GetVoice(voice).Lines;
                if (lines.Length > 0)
                {
                    firstBeat = Math.Min(firstBeat, lines[0].FirstNoteBeat);
                }
            }

            return firstBeat == int.MaxValue ? 0f : CGame.GetTimeFromBeats(firstBeat, song.Bpm) + song.Gap;
        }

        private static float _GetSectionDuration()
        {
            switch (Section)
            {
                case EJukeboxSection.TR_JUKEBOX_SECTION_30:
                    return 30f;
                case EJukeboxSection.TR_JUKEBOX_SECTION_60:
                    return 60f;
                default:
                    return 90f;
            }
        }

        private static int _GetNextIndex()
        {
            if (_Queue.Count == 0)
            {
                return -1;
            }

            if (Current + 1 < _Queue.Count)
            {
                return Current + 1;
            }

            return Repeat ? 0 : -1;
        }

        private static void _ShuffleRange(int start)
        {
            for (var n = _Queue.Count - 1; n > start; n--)
            {
                var k = CGame.Rand.Next(start, n + 1);
                var entry = _Queue[k];
                _Queue[k] = _Queue[n];
                _Queue[n] = entry;
            }
        }
    }
}
