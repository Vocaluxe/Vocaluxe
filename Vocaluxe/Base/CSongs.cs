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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VocaluxeLib;
using VocaluxeLib.Log;
using VocaluxeLib.Songs;

namespace Vocaluxe.Base
{
    static class CSongs
    {
        public delegate void CategoryChangedHandler();

        private static readonly List<CSong> _SongsForRandom = new List<CSong>();

        private static bool _CoverLoaded;
        private static int _CatIndex = -1;
        private static readonly List<CCategory> _CategoriesForRandom = new List<CCategory>();

        public static readonly CSongFilter Filter = new CSongFilter();
        public static readonly CSongSorter Sorter = new CSongSorter();
        public static readonly CSongCategorizer Categorizer = new CSongCategorizer();

        private static Thread _CoverLoaderThread;
        public static event CategoryChangedHandler OnCategoryChanged;

        public static List<CSong> Songs { get; } = new List<CSong>();

        public static bool SongsLoaded { get; private set; }

        public static bool CoverLoaded
        {
            get
            {
                if (SongsLoaded && NumAllSongs == 0)
                {
                    _CoverLoaded = true;
                }

                return _CoverLoaded;
            }
        }

        public static int NumAllSongs
        {
            get { return Songs.Count; }
        }

        public static int NumSongsVisible
        {
            get { return VisibleSongs.Count; }
        }

        public static int NumCategories
        {
            get { return Categories.Count; }
        }

        public static int Category
        {
            get
            {
                if (_CatIndex >= Categories.Count)
                {
                    _CatIndex = -1;
                }

                return _CatIndex;
            }
            set
            {
                if (value == _CatIndex)
                {
                    return;
                }

                if (value == -1 || _IsCatIndexValid(value))
                {
                    _CatIndex = value;
                    if (OnCategoryChanged != null)
                    {
                        OnCategoryChanged();
                    }
                }
            }
        }

        public static bool IsInCategory
        {
            get { return _IsCatIndexValid(_CatIndex); }
        }

        private static bool _IsCatIndexValid(int catIndex)
        {
            return catIndex >= 0 && catIndex < Categories.Count;
        }

        /// <summary>
        ///     Returns the number of song in the category specified with CatIndex
        /// </summary>
        /// <param name="catIndex">Category index</param>
        /// <returns></returns>
        public static int GetNumSongsInCategory(int catIndex)
        {
            return _IsCatIndexValid(catIndex) ? Categories[catIndex].Songs.Count : 0;
        }

        /// <summary>
        ///     Returns the number of song that are not sung in the category specified with CatIndex
        /// </summary>
        /// <param name="catIndex">Category index</param>
        /// <returns>Number of visible songs in that category</returns>
        public static int GetNumSongsNotSungInCategory(int catIndex)
        {
            return _IsCatIndexValid(catIndex) ? Categories[catIndex].GetNumSongsNotSung() : 0;
        }

        public static void NextCategory()
        {
            if (Category == Categories.Count - 1)
            {
                Category = 0;
            }
            else
            {
                Category++;
            }
        }

        public static void PrevCategory()
        {
            if (Category == 0)
            {
                Category = Categories.Count - 1;
            }
            else
            {
                Category--;
            }
        }

        private static int _NumSongsWithCoverLoaded;
        public static int NumSongsWithCoverLoaded
        {
            get { return _NumSongsWithCoverLoaded; }
            private set { _NumSongsWithCoverLoaded = value; }
        }

        private static int _NumSongsLoaded;
        public static int NumSongsLoaded
        {
            get { return _NumSongsLoaded; }
        }

        public static string GetCurrentCategoryName()
        {
            return _IsCatIndexValid(_CatIndex) ? Categories[_CatIndex].Name : "";
        }

        public static CSong GetSong(int songId)
        {
            return songId >= 0 && songId < Songs.Count ? Songs[songId] : null;
        }

        public static void AddPartySongSung(int songId)
        {
            foreach (var category in Categories)
            {
                foreach (var song in category.Songs.Where(song => song.SongId == songId))
                {
                    song.IsSung = true;
                    if (category.GetNumSongsNotSung() == 0)
                    {
                        ResetPartySongSung(_GetCategoryNumber(category));
                    }

                    return;
                }
            }
        }

        public static void ResetPartySongSung()
        {
            foreach (var song in Sorter.SortedSongs)
            {
                song.IsSung = false;
            }
        }

        public static void ResetPartySongSung(int catIndex)
        {
            if (_IsCatIndexValid(catIndex))
            {
                foreach (var song in Categories[catIndex].Songs)
                {
                    song.IsSung = false;
                }
            }
        }

        public static int GetVisibleSongNumber(int songId)
        {
            var i = -1;
            foreach (var song in VisibleSongs)
            {
                i++;
                if (song.Id == songId)
                {
                    return i;
                }
            }

            return i;
        }

        public static int GetRandomSong()
        {
            if (_SongsForRandom.Count == 0)
            {
                UpdateRandomSongList();
            }

            if (_SongsForRandom.Count == 0)
            {
                return -1;
            }

            var song = _SongsForRandom[CGame.Rand.Next(0, _SongsForRandom.Count - 1)];
            _SongsForRandom.Remove(song);
            return GetVisibleSongNumber(song.Id);
        }

        public static void UpdateRandomSongList()
        {
            _SongsForRandom.Clear();
            var visibleSongs = VisibleSongs.ToList();
            if (visibleSongs.Count == 0)
            {
                return;
            }

            // Calc average sing-count
            var totalCounts = visibleSongs.Sum(song => song.NumPlayedSession);
            var averageCount = totalCounts / visibleSongs.Count;

            foreach (var song in visibleSongs)
            {
                if (song.NumPlayedSession <= averageCount)
                {
                    _SongsForRandom.Add(song);
                }
            }

            if (_SongsForRandom.Count == 0)
            {
                _SongsForRandom.AddRange(visibleSongs);
            }
        }

        public static int GetRandomCategory()
        {
            if (_CategoriesForRandom.Count == 0)
            {
                UpdateRandomCategoryList();
            }

            if (_CategoriesForRandom.Count == 0)
            {
                return -1;
            }

            var category = _CategoriesForRandom[CGame.Rand.Next(0, _CategoriesForRandom.Count - 1)];
            _CategoriesForRandom.Remove(category);
            return _GetCategoryNumber(category);
        }

        public static void UpdateRandomCategoryList()
        {
            _CategoriesForRandom.Clear();
            _CategoriesForRandom.AddRange(Categories);
        }

        private static int _GetCategoryNumber(CCategory category)
        {
            for (var i = 0; i < Categories.Count; i++)
            {
                if (Categories[i] == category)
                {
                    return i;
                }
            }

            return -1;
        }

        public static ReadOnlyCollection<CSong> AllSongs
        {
            get { return Songs.AsReadOnly(); }
        }

        public static ReadOnlyCollection<CSong> VisibleSongs
        {
            get
            {
                var songs = new List<CSong>();
                if (_IsCatIndexValid(_CatIndex))
                {
                    // ReSharper disable LoopCanBeConvertedToQuery
                    foreach (var sp in Categories[_CatIndex].Songs)
                        // ReSharper restore LoopCanBeConvertedToQuery
                    {
                        if (!sp.IsSung)
                        {
                            songs.Add(Songs[sp.SongId]);
                        }
                    }
                }

                return songs.AsReadOnly();
            }
        }

        public static ReadOnlyCollection<CCategory> Categories
        {
            get { return Categorizer.Categories.AsReadOnly(); }
        }

        /// <summary>
        ///     Gets category with given index or null for invalid index
        /// </summary>
        /// <param name="index"></param>
        /// <returns>category with given index or null for invalid index</returns>
        public static CCategory GetCategoryByIndex(int index)
        {
            if (!_IsCatIndexValid(index))
            {
                return null;
            }

            return Categorizer.Categories[index];
        }

        /// <summary>
        ///     Gets visible song with given index or null for invalid index or song not visible
        /// </summary>
        /// <param name="index"></param>
        /// <returns>visible song with given index or null for invalid index or song not visible</returns>
        public static CSong GetVisibleSongByIndex(int index)
        {
            if (index < 0)
            {
                return null;
            }

            var visSongs = VisibleSongs;
            return index < visSongs.Count ? visSongs[index] : null;
        }

        private static void _HandleCategoriesChanged(object sender, EventArgs args)
        {
            _CategoriesForRandom.Clear();
        }

        public static void Sort(ESongSorting sorting, EOffOn tabs, EOffOn ignoreArticles, String searchString, EDuetOptions duetOptions, int playlistId)
        {
            Filter.SetOptions(searchString, duetOptions, playlistId);
            Sorter.SetOptions(sorting, ignoreArticles);
            Categorizer.Tabs = tabs;
        }

        public static void LoadSongs()
        {
            using (CBenchmark.Time("Load Songs"))
            {
                SongsLoaded = false;
                Songs.Clear();
                _NumSongsLoaded = 0;

                IEnumerable<string> files;

                using (CBenchmark.Time("List Songs"))
                {
                    var fileSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var path in CConfig.SongFolders)
                    {
                        if (!Directory.Exists(path))
                        {
                            continue;
                        }

                        foreach (var file in CHelper.ListTextFiles(path, true, true))
                        {
                            fileSet.Add(file);
                        }
                    }

                    files = fileSet;
                }

                using (CBenchmark.Time("Read TXTs"))
                {
                    var fileList = files.ToList();
                    var bag = new ConcurrentBag<CSong>();
                    var options = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount)
                    };

                    Parallel.ForEach(fileList, options, file =>
                    {
                        var song = CSong.LoadSong(file);
                        if (song == null)
                        {
                            return;
                        }

                        if (song.LoadNotes())
                        {
                            bag.Add(song);
                            Interlocked.Increment(ref _NumSongsLoaded);
                        }
                    });

                    var id = 0;
                    foreach (var song in bag.OrderBy(s => s.Folder).ThenBy(s => s.FileName))
                    {
                        song.Id = id++;
                        Songs.Add(song);
                    }
                }
            }

            using (CBenchmark.Time("Sorted Songs"))
            {
                Sorter.SongSorting = CConfig.Config.Game.SongSorting;
                Sorter.IgnoreArticles = CConfig.Config.Game.IgnoreArticles;
                Categorizer.Tabs = CConfig.Config.Game.Tabs;
                Categorizer.ObjectChanged += _HandleCategoriesChanged;
            }

            Category = -1;
            SongsLoaded = true;

            switch (CConfig.Config.Theme.CoverLoading)
            {
                case ECoverLoading.TR_CONFIG_COVERLOADING_ATSTART:
                    _LoadCovers();
                    break;
                case ECoverLoading.TR_CONFIG_COVERLOADING_DYNAMIC:
                    _LoadCoversAsync();
                    break;
            }
        }

        private static void _LoadCoversAsync()
        {
            if (!SongsLoaded || CoverLoaded)
            {
                return;
            }

            if (_CoverLoaderThread != null)
            {
                return;
            }

            _CoverLoaderThread = new Thread(_LoadCovers) { Name = "CoverLoader", Priority = ThreadPriority.BelowNormal, IsBackground = true };
            _CoverLoaderThread.Start();
        }

        private static void _LoadCovers()
        {
            using (CBenchmark.Time("Loaded Covers"))
            {
                var songCount = Songs.Count;
                var ev = new AutoResetEvent(songCount == 0);

                NumSongsWithCoverLoaded = 0;
                foreach (var song in Songs)
                {
                    var tmp = song;
                    Task.Factory.StartNew(() =>
                    {
                        tmp.LoadAndCacheCoverIfNeeded();
                        if (Interlocked.Increment(ref _NumSongsWithCoverLoaded) >= songCount)
                        {
                            ev.Set();
                        }
                    });
                }

                ev.WaitOne();
                _CoverLoaded = true;
                CDataBase.CommitCovers();
            }
        }
    }
}