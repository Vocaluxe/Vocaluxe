using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using VocaluxeLib.Menu;
using VocaluxeLib.Menu.SongMenu;

namespace Vocaluxe.Base
{
    public struct SSongPointer
    {
        public int SongID;
        public string SortString;

        public int CatIndex;
        public bool Visible;
        public bool PartyHidden;

        public SSongPointer(int iD, string sortString)
        {
            SongID = iD;
            SortString = sortString;
            CatIndex = -1;
            Visible = false;
            PartyHidden = false;
        }
    }

    static class CSongs
    {
        private static readonly List<CSong> _Songs = new List<CSong>();
        private static readonly List<CSong> _SongsForRandom = new List<CSong>();

        private static bool _SongsLoaded;
        private static bool _CoverLoaded;
        private static int _CoverLoadIndex;
        private static int _CatIndex = -1;
        private static readonly List<CCategory> _CategoriesForRandom = new List<CCategory>();

        private static readonly Stopwatch _CoverLoadTimer = new Stopwatch();

        public static CSongFilter Filter = new CSongFilter();
        public static CSongSorter Sorter = new CSongSorter();
        public static CSongCategorizer Categorizer = new CSongCategorizer();

        private static Thread _CoverLoaderThread = null;

        public static List<CSong> Songs
        {
            get { return _Songs; }
        }

        public static bool SongsLoaded
        {
            get { return _SongsLoaded; }
        }

        public static bool CoverLoaded
        {
            get
            {
                if (_SongsLoaded && NumAllSongs == 0)
                    _CoverLoaded = true;
                return _CoverLoaded;
            }
        }

        public static int NumAllSongs
        {
            get { return _Songs.Count; }
        }

        public static int NumVisibleSongs
        {
            get
            {
                int result = 0;
                foreach (SSongPointer sp in Sorter.SortedSongs)
                {
                    if (sp.Visible)
                        result++;
                }
                return result;
            }
        }

        public static int NumCategories
        {
            get { return Categorizer.Categories.Count; }
        }

        public static int Category
        {
            get { return _CatIndex; }
            set
            {
                if (value >= -1 && value < Categorizer.Categories.Count)
                {
                    _CatIndex = value;

                    for (int i = 0; i < Sorter.SortedSongs.Length; i++)
                        Sorter.SortedSongs[i].Visible = Sorter.SortedSongs[i].CatIndex == _CatIndex && !Sorter.SortedSongs[i].PartyHidden;
                }
            }
        }

        public static bool IsInCategory
        {
            get { return _CatIndex >= 0; }
        }

        /// <summary>
        ///     Returns the number of song in the category specified with CatIndex
        /// </summary>
        /// <param name="catIndex">Category index</param>
        /// <returns></returns>
        public static int NumSongsInCategory(int catIndex)
        {
            if (Categorizer.Categories.Count <= catIndex || catIndex < 0)
                return 0;

            int num = 0;
            for (int i = 0; i < Sorter.SortedSongs.Length; i++)
            {
                if (Sorter.SortedSongs[i].CatIndex == catIndex && !Sorter.SortedSongs[i].PartyHidden)
                    num++;
            }
            return num;
        }

        public static void NextCategory()
        {
            if (Category == Categorizer.Categories.Count - 1)
                Category = 0;
            else
                Category++;
        }

        public static void PrevCategory()
        {
            if (Category == 0)
                Category = Categorizer.Categories.Count - 1;
            else
                Category--;
        }

        public static int GetNextSongWithoutCover(ref CSong song)
        {
            if (!SongsLoaded)
                return -1;

            if (_CoverLoadIndex < _Songs.Count)
            {
                song = _Songs[_CoverLoadIndex];
                _CoverLoadIndex++;
                return _CoverLoadIndex;
            }

            return -2;
        }

        public static int NumSongsWithCoverLoaded
        {
            get { return _CoverLoadIndex; }
        }

        public static void SetCoverSmall(int songIndex, STexture texture)
        {
            if (!_SongsLoaded)
                return;

            if (songIndex < _Songs.Count)
                _Songs[songIndex].CoverTextureSmall = texture;
        }

        public static void SetCoverBig(int songIndex, STexture texture)
        {
            if (!_SongsLoaded)
                return;

            if (songIndex < _Songs.Count)
                _Songs[songIndex].CoverTextureBig = texture;
        }

        public static string GetCurrentCategoryName()
        {
            if ((Categorizer.Categories.Count > 0) && (_CatIndex >= 0) && (Categorizer.Categories.Count > _CatIndex))
                return Categorizer.Categories[_CatIndex].Name;
            else
                return String.Empty;
        }

        public static CSong GetSong(int songID)
        {
            foreach (CSong song in _Songs)
            {
                if (song.ID == songID)
                    return song;
            }
            return null;
        }

        public static void AddPartySongSung(int songID)
        {
            int cat = -1;
            for (int i = 0; i < Sorter.SortedSongs.Length; i++)
            {
                if (songID == Sorter.SortedSongs[i].SongID)
                {
                    Sorter.SortedSongs[i].PartyHidden = true;
                    Sorter.SortedSongs[i].Visible = false;
                    cat = Sorter.SortedSongs[i].CatIndex;
                    break;
                }
            }

            if (cat != -1)
            {
                if (NumSongsInCategory(cat) == 0)
                    ResetPartySongSung(cat);
            }
        }

        public static void ResetPartySongSung()
        {
            for (int i = 0; i < Sorter.SortedSongs.Length; i++)
            {
                Sorter.SortedSongs[i].PartyHidden = false;
                Sorter.SortedSongs[i].Visible = Sorter.SortedSongs[i].CatIndex == _CatIndex && !Sorter.SortedSongs[i].PartyHidden;
            }
        }

        public static void ResetPartySongSung(int catIndex)
        {
            for (int i = 0; i < Sorter.SortedSongs.Length; i++)
            {
                if (Sorter.SortedSongs[i].CatIndex == catIndex)
                {
                    Sorter.SortedSongs[i].PartyHidden = false;
                    Sorter.SortedSongs[i].Visible = Sorter.SortedSongs[i].CatIndex == _CatIndex && !Sorter.SortedSongs[i].PartyHidden;
                }
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
            for (int i = 0; i < Categories.Length; i++)
            {
                if (Categories[i] == category)
                    return i;
            }
            return -1;
        }

        public static CSong[] AllSongs
        {
            get { return _Songs.ToArray(); }
        }

        public static CSong[] SongsNotSung
        {
            get
            {
                List<CSong> songs = new List<CSong>();
                foreach (SSongPointer sp in Sorter.SortedSongs)
                {
                    if (sp.Visible)
                        songs.Add(_Songs[sp.SongID]);
                }
                return songs.ToArray();
            }
        }

        public static CSong[] VisibleSongs
        {
            get
            {
                List<CSong> songs = new List<CSong>();
                foreach (SSongPointer sp in Sorter.SortedSongs)
                {
                    if (sp.Visible)
                        songs.Add(_Songs[sp.SongID]);
                }
                return songs.ToArray();
            }
        }

        public static CCategory[] Categories
        {
            get { return Categorizer.Categories.ToArray(); }
        }

        public static CSong GetVisibleSongByIndex(int index)
        {
            if (index < 0)
                return null;

            foreach (SSongPointer sp in Sorter.SortedSongs)
            {
                if (sp.Visible)
                {
                    if (index == 0)
                        return _Songs[sp.SongID];
                    index--;
                }
            }
            return null;
        }

        private static void _HandleCategoriesChanged(object sender, EventArgs args)
        {
            _CategoriesForRandom.Clear();
            Category = _CatIndex;
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
            _SongsLoaded = false;
            _Songs.Clear();

            CLog.StartBenchmark(2, "List Songs");
            List<string> files = new List<string>();
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
                if (song != null)
                {
                    song.ID = _Songs.Count;
                    if(song.ReadNotes())
                        _Songs.Add(song);
                }
            }
            CLog.StopBenchmark(2, "Read TXTs");

            CLog.StartBenchmark(2, "Sort Songs");
            Sorter.SongSorting = CConfig.SongSorting;
            Sorter.IgnoreArticles = CConfig.IgnoreArticles;
            Categorizer.Tabs = CConfig.Tabs;
            Categorizer.ObjectChanged += _HandleCategoriesChanged;
            CLog.StopBenchmark(2, "Sort Songs");
            Category = -1;
            _SongsLoaded = true;

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
                return; //should be removed as soon as the other renderer are ready for queque

            if (!SongsLoaded || CoverLoaded)
                return;

            if (_CoverLoaderThread == null)
            {
                _CoverLoaderThread = new Thread(new ThreadStart(_LoadCover));
                _CoverLoaderThread.Name = "CoverLoader";
                _CoverLoaderThread.Priority = ThreadPriority.BelowNormal;
                _CoverLoaderThread.IsBackground = true;
                _CoverLoaderThread.Start();
            }

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
                _CoverLoadIndex++;
            }
            GC.Collect();
            _CoverLoaded = true;
            CDataBase.CommitCovers();
        }
    }
}