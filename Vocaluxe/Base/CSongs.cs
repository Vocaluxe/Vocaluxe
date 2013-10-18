#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using VocaluxeLib;
using VocaluxeLib.Songs;

namespace Vocaluxe.Base
{
    static class CSongs
    {
        private static readonly List<CSong> _Songs = new List<CSong>();
        private static readonly List<CSong> _SongsForRandom = new List<CSong>();

        private static bool _CoverLoaded;
        private static int _CatIndex = -1;
        private static readonly List<CCategory> _CategoriesForRandom = new List<CCategory>();

        public static readonly CSongFilter Filter = new CSongFilter();
        public static readonly CSongSorter Sorter = new CSongSorter();
        public static readonly CSongCategorizer Categorizer = new CSongCategorizer();

        private static Thread _CoverLoaderThread;

        public static List<CSong> Songs
        {
            get { return _Songs; }
        }

        public static bool SongsLoaded { get; private set; }

        public static bool CoverLoaded
        {
            get
            {
                if (SongsLoaded && NumAllSongs == 0)
                    _CoverLoaded = true;
                return _CoverLoaded;
            }
        }

        public static int NumAllSongs
        {
            get { return _Songs.Count; }
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
                    _CatIndex = -1;
                return _CatIndex;
            }
            set
            {
                if (value == -1 || _IsCatIndexValid(value))
                    _CatIndex = value;
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
                Category = 0;
            else
                Category++;
        }

        public static void PrevCategory()
        {
            if (Category == 0)
                Category = Categories.Count - 1;
            else
                Category--;
        }

        public static int GetNextSongWithoutCover(ref CSong song)
        {
            if (!SongsLoaded)
                return -1;

            if (NumSongsWithCoverLoaded < _Songs.Count)
            {
                song = _Songs[NumSongsWithCoverLoaded];
                NumSongsWithCoverLoaded++;
                return NumSongsWithCoverLoaded;
            }

            return -2;
        }

        public static int NumSongsWithCoverLoaded { get; private set; }

        public static string GetCurrentCategoryName()
        {
            return _IsCatIndexValid(_CatIndex) ? Categories[_CatIndex].Name : "";
        }

        public static CSong GetSong(int songID)
        {
            return _Songs.FirstOrDefault(song => song.ID == songID);
        }

        public static void AddPartySongSung(int songID)
        {
            foreach (CCategory category in Categories)
            {
                foreach (CSongPointer song in category.Songs.Where(song => song.SongID == songID))
                {
                    song.IsSung = true;
                    if (category.GetNumSongsNotSung() == 0)
                        ResetPartySongSung(_GetCategoryNumber(category));
                    return;
                }
            }
        }

        public static void ResetPartySongSung()
        {
            foreach (CSongPointer song in Sorter.SortedSongs)
                song.IsSung = false;
        }

        public static void ResetPartySongSung(int catIndex)
        {
            if (_IsCatIndexValid(catIndex))
            {
                foreach (CSongPointer song in Categories[catIndex].Songs)
                    song.IsSung = false;
            }
        }

        public static int GetVisibleSongNumber(int songID)
        {
            int i = -1;
            foreach (CSong song in VisibleSongs)
            {
                i++;
                if (song.ID == songID)
                    return i;
            }
            return i;
        }

        public static int GetRandomSong()
        {
            if (_SongsForRandom.Count == 0)
                UpdateRandomSongList();

            if (_SongsForRandom.Count == 0)
                return -1;

            CSong song = _SongsForRandom[CGame.Rand.Next(0, _SongsForRandom.Count - 1)];
            _SongsForRandom.Remove(song);
            return GetVisibleSongNumber(song.ID);
        }

        public static void UpdateRandomSongList()
        {
            _SongsForRandom.Clear();
            _SongsForRandom.AddRange(VisibleSongs);
        }

        public static int GetRandomCategory()
        {
            if (_CategoriesForRandom.Count == 0)
                UpdateRandomCategoryList();

            if (_CategoriesForRandom.Count == 0)
                return -1;

            CCategory category = _CategoriesForRandom[CGame.Rand.Next(0, _CategoriesForRandom.Count - 1)];
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
            for (int i = 0; i < Categories.Count; i++)
            {
                if (Categories[i] == category)
                    return i;
            }
            return -1;
        }

        public static ReadOnlyCollection<CSong> AllSongs
        {
            get { return _Songs.AsReadOnly(); }
        }

        public static ReadOnlyCollection<CSong> VisibleSongs
        {
            get
            {
                var songs = new List<CSong>();
                if (_IsCatIndexValid(_CatIndex))
                {
                    // ReSharper disable LoopCanBeConvertedToQuery
                    foreach (CSongPointer sp in Categories[_CatIndex].Songs)
                        // ReSharper restore LoopCanBeConvertedToQuery
                    {
                        if (!sp.IsSung)
                            songs.Add(_Songs[sp.SongID]);
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
        /// Gets category with given index or null for invalid index
        /// </summary>
        /// <param name="index"></param>
        /// <returns>category with given index or null for invalid index</returns>
        public static CCategory GetCategoryByIndex(int index)
        {
            if (!_IsCatIndexValid(index))
                return null;

            return Categorizer.Categories[index];
        }

        /// <summary>
        /// Gets visible song with given index or null for invalid index or song not visible
        /// </summary>
        /// <param name="index"></param>
        /// <returns>visible song with given index or null for invalid index or song not visible</returns>
        public static CSong GetVisibleSongByIndex(int index)
        {
            if (index < 0)
                return null;
            ReadOnlyCollection<CSong> visSongs = VisibleSongs;
            return (index < visSongs.Count) ? visSongs[index] : null;
        }

        private static void _HandleCategoriesChanged(object sender, EventArgs args)
        {
            _CategoriesForRandom.Clear();
        }

        public static void Sort(ESongSorting sorting, EOffOn tabs, EOffOn ignoreArticles, String searchString, EDuetOptions duetOptions)
        {
            Filter.SetOptions(searchString, duetOptions);
            Sorter.SetOptions(sorting, ignoreArticles);
            Categorizer.Tabs = tabs;
        }

        public static void LoadSongs()
        {
            CLog.StartBenchmark(1, "Load Songs");
            SongsLoaded = false;
            _Songs.Clear();

            CLog.StartBenchmark(2, "List Songs");
            var files = new List<string>();
            foreach (string p in CConfig.SongFolder)
            {
                string path = p;
                files.AddRange(CHelper.ListFiles(path, "*.txt", true, true));
                files.AddRange(CHelper.ListFiles(path, "*.txd", true, true));
            }
            CLog.StopBenchmark(2, "List Songs");

            CLog.StartBenchmark(2, "Read TXTs");
            foreach (string file in files)
            {
                CSong song = CSong.LoadSong(file);
                if (song == null)
                    continue;
                song.ID = _Songs.Count;
                if (song.ReadNotes())
                    _Songs.Add(song);
            }
            CLog.StopBenchmark(2, "Read TXTs");

            CLog.StartBenchmark(2, "Sort Songs");
            Sorter.SongSorting = CConfig.SongSorting;
            Sorter.IgnoreArticles = CConfig.IgnoreArticles;
            Categorizer.Tabs = CConfig.Tabs;
            Categorizer.ObjectChanged += _HandleCategoriesChanged;
            CLog.StopBenchmark(2, "Sort Songs");
            Category = -1;
            SongsLoaded = true;

            if (CConfig.CoverLoading == ECoverLoading.TR_CONFIG_COVERLOADING_ATSTART)
            {
                CLog.StartBenchmark(2, "Load Covers/Notes");
                _LoadCover();
                CLog.StopBenchmark(2, "Load Covers/Notes");
            }
            CLog.StopBenchmark(1, "Load Songs ");
            GC.Collect();
        }

        public static void LoadCover()
        {
            if (CConfig.Renderer == ERenderer.TR_CONFIG_SOFTWARE)
                return; //should be removed as soon as the other renderer are ready for queue

            if (!SongsLoaded || CoverLoaded)
                return;

            if (_CoverLoaderThread != null)
                return;
            _CoverLoaderThread = new Thread(_LoadCover) {Name = "CoverLoader", Priority = ThreadPriority.BelowNormal, IsBackground = true};
            _CoverLoaderThread.Start();

            /*
            if (_CoverLoadTimer.ElapsedMilliseconds >= WaitTime)
            {
                for (int i = 0; i < NumLoads; i++)
                {
                    CSong song = null;
                    int n = GetNextSongWithoutCover(ref song);

                    if (n < 0)
                        return;

                    song.LoadSmallCover();

                    if (n == NumAllSongs)
                        CDataBase.CommitCovers();
                }
                _CoverLoadTimer.Reset();
                _CoverLoadTimer.Start();
            }
             * */
        }

        private static void _LoadCover()
        {
            foreach (CSong song in _Songs)
            {
                song.LoadSmallCover();
                NumSongsWithCoverLoaded++;
            }
            GC.Collect();
            _CoverLoaded = true;
            CDataBase.CommitCovers();
        }
    }
}