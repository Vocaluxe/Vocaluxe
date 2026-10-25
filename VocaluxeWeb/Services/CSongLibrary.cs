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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VocaluxeWeb.Contracts;
using VocaluxeWeb.Dispatching;
using VocaluxeWeb.Remote;

namespace VocaluxeWeb.Services
{
    public sealed class CSongQuery
    {
        public string Text;
        public string Language;
        public string Genre;
        public int? Decade;
        public bool? Duet;
        public ESongSort Sort;
        public int Page;
        public int PageSize = 50;
    }

    /// Copy of the game's song list for searching outside the main thread.

    /// It is refreshed when the game reports a new library version (checked at most every few seconds).
    public sealed class CSongLibrary
    {
        private static readonly TimeSpan _VersionCheckInterval = TimeSpan.FromSeconds(5);

        private sealed class CEntry
        {
            public RemoteSong Song;
            public SongDto Dto;
            public string SearchText;
            public int? Decade;
        }

        private readonly IVocaluxeRemote _Remote;
        private readonly CMainThreadDispatcher _Dispatcher;
        private readonly SemaphoreSlim _RefreshLock = new SemaphoreSlim(1, 1);
        private IReadOnlyList<CEntry> _Entries = new CEntry[0];
        private Dictionary<int, CEntry> _ById = new Dictionary<int, CEntry>();
        private int _Version = -1;
        private DateTime _LastCheck = DateTime.MinValue;

        public CSongLibrary(IVocaluxeRemote remote, CMainThreadDispatcher dispatcher)
        {
            _Remote = remote;
            _Dispatcher = dispatcher;
        }

        public async Task<SongPageDto> SearchAsync(CSongQuery query)
        {
            await _EnsureFreshAsync();
            IEnumerable<CEntry> result = _Entries;

            var words = _Normalize(query.Text).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 0)
            {
                result = result.Where(e => words.All(w => e.SearchText.Contains(w)));
            }

            if (!string.IsNullOrEmpty(query.Language))
            {
                result = result.Where(e => e.Song.Languages.Contains(query.Language, StringComparer.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(query.Genre))
            {
                result = result.Where(e => e.Song.Genres.Contains(query.Genre, StringComparer.OrdinalIgnoreCase));
            }

            if (query.Decade.HasValue)
            {
                result = result.Where(e => e.Decade == query.Decade);
            }

            if (query.Duet.HasValue)
            {
                result = result.Where(e => e.Song.IsDuet == query.Duet.Value);
            }

            var sorted = _Sort(result, query.Sort).ToList();
            var pageSize = Math.Max(1, Math.Min(query.PageSize, 200));
            var page = Math.Max(0, query.Page);
            return new SongPageDto
            {
                Items = sorted.Skip(page * pageSize).Take(pageSize).Select(e => e.Dto).ToList(),
                Total = sorted.Count,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<SongFacetsDto> GetFacetsAsync()
        {
            await _EnsureFreshAsync();
            var entries = _Entries;
            return new SongFacetsDto
            {
                Languages = _Count(entries.SelectMany(e => e.Song.Languages)),
                Genres = _Count(entries.SelectMany(e => e.Song.Genres)),
                Decades = entries.Where(e => e.Decade.HasValue)
                                 .GroupBy(e => e.Decade.Value)
                                 .OrderByDescending(g => g.Key)
                                 .Select(g => new FacetValueDto { Value = g.Key.ToString(CultureInfo.InvariantCulture), Count = g.Count() })
                                 .ToList()
            };
        }

        public async Task<RemoteSong> GetAsync(int songId)
        {
            await _EnsureFreshAsync();
            return _ById.TryGetValue(songId, out var entry) ? entry.Song : null;
        }

        /// Song for display; songs that are no longer known get a placeholder.
        public async Task<SongDto> GetDtoAsync(int songId)
        {
            await _EnsureFreshAsync();
            return _ById.TryGetValue(songId, out var entry)
                ? entry.Dto
                : new SongDto { Id = songId, Title = "?", Artist = "?", GameModes = new List<ESongGameMode>(), AudioModes = new List<ESongAudioMode>() };
        }

        public async Task<StateDto> CreateStateAsync(RemoteState state)
        {
            return new StateDto { Screen = state.Screen, CurrentSong = state.SongId.HasValue ? await GetDtoAsync(state.SongId.Value) : null };
        }

        private async Task _EnsureFreshAsync()
        {
            if (DateTime.UtcNow - _LastCheck < _VersionCheckInterval && _Version >= 0)
            {
                return;
            }

            await _RefreshLock.WaitAsync();
            try
            {
                if (DateTime.UtcNow - _LastCheck < _VersionCheckInterval && _Version >= 0)
                {
                    return;
                }

                var version = await _Dispatcher.InvokeAsync(() => _Remote.SongLibraryVersion);
                if (version != _Version)
                {
                    var songs = await _Dispatcher.InvokeAsync(() => _Remote.GetSongs());
                    var entries = songs.Select(_CreateEntry).ToList();
                    _ById = entries.GroupBy(e => e.Song.Id).ToDictionary(g => g.Key, g => g.First());
                    _Entries = entries;
                    _Version = version;
                }

                _LastCheck = DateTime.UtcNow;
            }
            finally
            {
                _RefreshLock.Release();
            }
        }

        private static CEntry _CreateEntry(RemoteSong song)
        {
            return new CEntry
            {
                Song = song,
                Dto = ToDto(song),
                SearchText = _Normalize(song.Artist + " " + song.Title + " " + string.Join(" ", song.Editions ?? new string[0])),
                Decade = _ParseDecade(song.Year)
            };
        }

        public static SongDto ToDto(RemoteSong song)
        {
            return new SongDto
            {
                Id = song.Id,
                Title = song.Title,
                Artist = song.Artist,
                Year = string.IsNullOrEmpty(song.Year) ? null : song.Year,
                Language = song.Languages?.FirstOrDefault(),
                Genre = song.Genres?.FirstOrDefault(),
                IsDuet = song.IsDuet,
                HasCover = !string.IsNullOrEmpty(song.CoverPath),
                HasAudio = !string.IsNullOrEmpty(song.AudioPath),
                PreviewStart = song.PreviewStart,
                GameModes = (song.GameModes ?? new int[0]).Select(m => (ESongGameMode)m).ToList(),
                AudioModes = (song.AudioModes ?? new int[0]).Select(m => (ESongAudioMode)m).ToList()
            };
        }

        private static IEnumerable<CEntry> _Sort(IEnumerable<CEntry> entries, ESongSort sort)
        {
            var culture = StringComparer.Create(CultureInfo.InvariantCulture, true);
            switch (sort)
            {
                case ESongSort.Title:
                    return entries.OrderBy(e => e.Song.Title, culture).ThenBy(e => e.Song.Artist, culture);
                case ESongSort.Year:
                    return entries.OrderByDescending(e => e.Song.Year).ThenBy(e => e.Song.Artist, culture);
                default:
                    return entries.OrderBy(e => e.Song.Artist, culture).ThenBy(e => e.Song.Title, culture);
            }
        }

        private static IList<FacetValueDto> _Count(IEnumerable<string> values)
        {
            return values.Where(v => !string.IsNullOrWhiteSpace(v))
                         .GroupBy(v => v.Trim(), StringComparer.OrdinalIgnoreCase)
                         .OrderByDescending(g => g.Count())
                         .ThenBy(g => g.Key)
                         .Select(g => new FacetValueDto { Value = g.First().Trim(), Count = g.Count() })
                         .ToList();
        }

        private static int? _ParseDecade(string year)
        {
            if (string.IsNullOrEmpty(year) || year.Length < 4 || !int.TryParse(year.Substring(0, 4), out var value) || value < 1900)
            {
                return null;
            }

            return value / 10 * 10;
        }

        /// Lower case without accents, so "beyonce" finds "Beyoncé".
        private static string _Normalize(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            var builder = new StringBuilder(text.Length);
            foreach (var c in text.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(char.ToLowerInvariant(c));
                }
            }

            return builder.ToString();
        }
    }
}
