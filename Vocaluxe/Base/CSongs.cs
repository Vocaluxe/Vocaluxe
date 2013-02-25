using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Reflection;

using Vocaluxe.Lib.Draw;

using Vocaluxe.Menu;
using Vocaluxe.Menu.SongMenu;

namespace Vocaluxe.Base
{
    public struct SongPointer
    {
        public int SongID;
        public string SortString;

        public int CatIndex;
        public bool Visible;
        public bool PartyHidden;

        public SongPointer(int ID, string sortString)
        {
            SongID = ID;
            SortString = sortString;
            CatIndex = -1;
            Visible = false;
            PartyHidden = false;
        }
    }

    static class CSongs
    {
        private static List<CSong> _Songs = new List<CSong>();
        private static List<CSong> _FilteredSongs = new List<CSong>();
        private static SongPointer[] _SongsSortList = new SongPointer[0];
        private static List<CSong> _SongsForRandom = new List<CSong>();

        private static bool _SongsLoaded = false;
        private static bool _CoverLoaded = false;
        private static int _CoverLoadIndex = -1;
        private static int _CatIndex = -1;
        private static bool _Init = false;
        private static List<CCategory> _Categories = new List<CCategory>();
        private static List<CCategory> _CategoriesForRandom = new List<CCategory>();

        private static Stopwatch _CoverLoadTimer = new Stopwatch();

        private static string _SearchFilter = String.Empty;
        private static EOffOn _Tabs = CConfig.Tabs;
        private static EOffOn _IgnoreArticles = CConfig.IgnoreArticles;
        private static ESongSorting _SongSorting = CConfig.SongSorting;
        private static bool _ShowDuetSongs = true;

        private static Thread _CoverLoaderThread = null;
                    
        public static string SearchFilter
        {
            get { return _SearchFilter; }
            set
            {
                if (value != String.Empty)
                {
                    _Sort(_SongSorting, EOffOn.TR_CONFIG_OFF, _IgnoreArticles, value, false, _ShowDuetSongs);
                }
                else
                {
                    _Sort(_SongSorting, _Tabs, _IgnoreArticles, value, false, _ShowDuetSongs);
                }
            }
        }

        public static EOffOn Tabs
        {
            get { return _Tabs; }
        }

        public static EOffOn IgnoreArticles
        {
            get { return _IgnoreArticles; }
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
                int Result = 0;
                foreach (SongPointer sp in _SongsSortList)
                {
                    if (sp.Visible)
                        Result++;
                }
                return Result;
            }
        }

        public static int NumCategories
        {
            get { return _Categories.Count; }
        }

        public static int Category
        {
            get { return _CatIndex; }
            set
            {
                if ((_Categories.Count > value) && (value >= -1))
                {
                    _CatIndex = value;

                    for (int i = 0; i < _SongsSortList.Length; i++)
                    {
                        _SongsSortList[i].Visible = (_SongsSortList[i].CatIndex == _CatIndex && !_SongsSortList[i].PartyHidden);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the number of song in the category specified with CatIndex
        /// </summary>
        /// <param name="CatIndex">Category index</param>
        /// <returns></returns>
        public static int NumSongsInCategory(int CatIndex)
        {
            if (_Categories.Count <= CatIndex || CatIndex < 0)
                return 0;

            int num = 0;
            for (int i = 0; i < _SongsSortList.Length; i++)
            {
                if (_SongsSortList[i].CatIndex == CatIndex && !_SongsSortList[i].PartyHidden)
                    num++;
            }
            return num;
        }

        public static void NextCategory()
        {
            if (Category == _Categories.Count - 1)
                Category = 0;
            else
                Category++;
        }
        public static void PrevCategory()
        {
            if (Category == 0)
                Category = _Categories.Count - 1;
            else
                Category--;
        }

        public static int GetNextSongWithoutCover(ref CSong Song)
        {
            if (!SongsLoaded)
                return -1;

            if (_Songs.Count > _CoverLoadIndex + 1)
            {
                _CoverLoadIndex++;
                Song = _Songs[_CoverLoadIndex];
                return _CoverLoadIndex;
            }

            return -2;
        }
        public static int NumSongsWithCoverLoaded
        {
            get { return _CoverLoadIndex + 1; }
        }

        public static void SetCoverSmall(int SongIndex, STexture Texture)
        {
            if (!_SongsLoaded)
                return;

            if (SongIndex < _Songs.Count)
            {
                _Songs[SongIndex].CoverTextureSmall = Texture;
                if (SongIndex == _Songs.Count - 1)
                    _CoverLoaded = true;
            }

        }
        public static void SetCoverBig(int SongIndex, STexture Texture)
        {
            if (!_SongsLoaded)
                return;

            if (SongIndex < _Songs.Count)
            {
                _Songs[SongIndex].CoverTextureBig = Texture;
                if (SongIndex == _Songs.Count - 1)
                    _CoverLoaded = true;
            }

        }

        public static string GetCurrentCategoryName()
        {
            if ((_Categories.Count > 0) && (_CatIndex >= 0) && (_Categories.Count > _CatIndex))
                return _Categories[_CatIndex].Name;
            else
                return String.Empty;
        }

        public static CSong GetSong(int SongID)
        {
            foreach (CSong song in _Songs)
            {
                if (song.ID == SongID)
                    return song;
            }
            return null;
        }

        public static void AddPartySongSung(int SongID)
        {
            int cat = -1;
            for (int i = 0; i < _SongsSortList.Length; i++)
            {
                if (SongID == _SongsSortList[i].SongID)
                {
                    _SongsSortList[i].PartyHidden = true;
                    _SongsSortList[i].Visible = false;
                    cat = _SongsSortList[i].CatIndex;
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
            for (int i = 0; i < _SongsSortList.Length; i++)
            {
                _SongsSortList[i].PartyHidden = false;
                _SongsSortList[i].Visible = (_SongsSortList[i].CatIndex == _CatIndex && !_SongsSortList[i].PartyHidden);
            }
        }

        public static void ResetPartySongSung(int CatIndex)
        {
            for (int i = 0; i < _SongsSortList.Length; i++)
            {
                if (_SongsSortList[i].CatIndex == CatIndex)
                {
                    _SongsSortList[i].PartyHidden = false;
                    _SongsSortList[i].Visible = (_SongsSortList[i].CatIndex == _CatIndex && !_SongsSortList[i].PartyHidden);
                }
            }
        }

        public static int GetVisibleSongNumber(int SongID)
        {
            int i = -1;
            foreach (CSong song in VisibleSongs)
            {
                i++;
                if (song.ID == SongID)
                    return i;
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
                return -1;

            CSong song = _SongsForRandom[CGame.Rand.Next(0, _SongsForRandom.Count-1)];
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
            return GetCategoryNumber(category);
        }

        public static void UpdateRandomCategoryList()
        {
            _CategoriesForRandom.Clear();
            _CategoriesForRandom.AddRange(Categories);
        }

        private static int GetCategoryNumber(CCategory category)
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
                foreach (SongPointer sp in _SongsSortList)
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
                foreach (SongPointer sp in _SongsSortList)
                {
                    if (sp.Visible)
                        songs.Add(_Songs[sp.SongID]);
                }
                return songs.ToArray();
            }
        }

        public static CCategory[] Categories
        {
            get { return _Categories.ToArray(); }
        }

        private static void _FilterSongs(String SearchFilter, bool ShowDuetSongs)
        {
            if (_Init && _SearchFilter == SearchFilter && _ShowDuetSongs == ShowDuetSongs)
                return;

            _Init = true;
            _SearchFilter = SearchFilter;
            _ShowDuetSongs = ShowDuetSongs;
            _FilteredSongs.Clear();

            string[] searchStrings = null;
            if (_SearchFilter != String.Empty)
                searchStrings = _SearchFilter.ToUpper().Split(new char[] { ' ' });

            foreach (CSong song in _Songs)
            {
                if (!song.IsDuet || _ShowDuetSongs)
                {
                    if (_SearchFilter == String.Empty)
                        _FilteredSongs.Add(song);
                    else if (searchStrings != null)
                    {
                        string search = song.Title.ToUpper() + " " + song.Artist.ToUpper() + " " + song.FolderName.ToUpper() + " " + song.FileName.ToUpper();

                        bool contains = true;

                        foreach (string str in searchStrings)
                        {
                            contains &= search.Contains(str);
                        }
                        if (contains)
                            _FilteredSongs.Add(song);
                    }
                }
            }
        }

        private static int _SortByFieldArtistTitle(SongPointer s1, SongPointer s2)
        {
            int res = s1.SortString.ToUpper().CompareTo(s2.SortString.ToUpper());
            if (res == 0)
            {
                if (_IgnoreArticles == EOffOn.TR_CONFIG_ON)
                {
                    res = _Songs[s1.SongID].ArtistSorting.ToUpper().CompareTo(_Songs[s2.SongID].ArtistSorting.ToUpper());
                    if (res == 0)
                    {
                        return _Songs[s1.SongID].TitleSorting.ToUpper().CompareTo(_Songs[s2.SongID].TitleSorting.ToUpper());
                    }
                    return res;
                }
                else
                {
                    res = _Songs[s1.SongID].Artist.ToUpper().CompareTo(_Songs[s2.SongID].Artist.ToUpper());
                    if (res == 0)
                    {
                        return _Songs[s1.SongID].Title.ToUpper().CompareTo(_Songs[s2.SongID].Title.ToUpper());
                    }
                    return res;
                }
            }
            return res;
        }

        private static int _SortByFieldTitle(SongPointer s1, SongPointer s2)
        {
            int res = s1.SortString.ToUpper().CompareTo(s2.SortString.ToUpper());
            if (res == 0)
            {
                if (_IgnoreArticles == EOffOn.TR_CONFIG_ON)
                    return _Songs[s1.SongID].TitleSorting.ToUpper().CompareTo(_Songs[s2.SongID].TitleSorting.ToUpper());
                else
                    return _Songs[s1.SongID].Title.ToUpper().CompareTo(_Songs[s2.SongID].Title.ToUpper());
            }
            return res;
        }

        private static List<SongPointer> _CreateSortList(string fieldName)
        {
            FieldInfo field = null;
            bool isString = false;
            List<SongPointer> SortList = new List<SongPointer>();
            if (fieldName == String.Empty)
                _FilteredSongs.ForEach((song) => SortList.Add(new SongPointer(song.ID, "")));
            {
                field = Type.GetType("Vocaluxe.Menu.SongMenu.CSong,VocaluxeLib").GetField(fieldName, BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
                isString = field.FieldType == typeof(string);
                if (!isString && field.FieldType != typeof(List<String>))
                    throw new Exception("Unkown sort field type");
                foreach (CSong song in _FilteredSongs)
                {
                    object value = field.GetValue(song);
                    if (isString)
                        SortList.Add(new SongPointer(song.ID, (String)value));
                    else
                    {
                        List<String> values = (List<String>)value;
                        if (values.Count == 0)
                        {
                            SortList.Add(new SongPointer(song.ID, ""));
                        }
                        else
                        {
                            foreach (String sortString in (List<String>)value)
                            {
                                SortList.Add(new SongPointer(song.ID, sortString));
                            }
                        }
                    }
                }
            }
            return SortList;
        }
        
        private static void SortSongs()
        {
            String fieldName;
            switch (_SongSorting)
            {
                case ESongSorting.TR_CONFIG_EDITION:
                    fieldName = "Edition";
                    break;
                case ESongSorting.TR_CONFIG_GENRE:
                    fieldName = "Genre";
                    break;
                case ESongSorting.TR_CONFIG_FOLDER:
                    fieldName = "FolderName";
                    break;
                case ESongSorting.TR_CONFIG_ARTIST_LETTER:
                case ESongSorting.TR_CONFIG_ARTIST:
                    if (_IgnoreArticles == EOffOn.TR_CONFIG_ON)
                        fieldName = "ArtistSorting";
                    else
                        fieldName = "Artist";
                    break;
                case ESongSorting.TR_CONFIG_TITLE_LETTER:
                    if (_IgnoreArticles == EOffOn.TR_CONFIG_ON)
                        fieldName = "TitleSorting";
                    else
                        fieldName = "Title";
                    break;
                case ESongSorting.TR_CONFIG_YEAR:
                case ESongSorting.TR_CONFIG_DECADE:
                    fieldName = "Year";
                    break;
                case ESongSorting.TR_CONFIG_LANGUAGE:
                    fieldName = "Language";
                    break;
                default:
                    fieldName = "";
                    break;
            }
            List<SongPointer> SortList = _CreateSortList(fieldName);
            switch (_SongSorting)
            {
                case ESongSorting.TR_CONFIG_ARTIST_LETTER:
                case ESongSorting.TR_CONFIG_ARTIST:
                case ESongSorting.TR_CONFIG_NONE:
                    SortList.Sort(_SortByFieldTitle);
                    break;
                default:
                    SortList.Sort(_SortByFieldArtistTitle);
                    break;
            }
            _SongsSortList = SortList.ToArray();
        }

        private static void _CreateCategoriesLetter()
        {
            string category = "";
            int NotLetterCat = -1;
            for (int i = 0; i < _SongsSortList.Length; i++)
            {
                Char firstLetter = Char.ToUpper(_SongsSortList[i].SortString.Normalize(NormalizationForm.FormD)[0]);

                if (!Char.IsLetter(firstLetter))
                {
                    firstLetter = '#';
                }
                if (firstLetter.ToString() != category)
                {
                    if (firstLetter != '#' || NotLetterCat == -1)
                    {
                        category = firstLetter.ToString();
                        _Categories.Add(new CCategory(category));

                        _SongsSortList[i].CatIndex = _Categories.Count - 1;

                        if (firstLetter == '#')
                            NotLetterCat = _SongsSortList[i].CatIndex;
                    }
                    else
                        _SongsSortList[i].CatIndex = NotLetterCat;
                }
                else
                    _SongsSortList[i].CatIndex = _Categories.Count - 1;
            }
        }

        private static void _CreateCategoriesNormal(string NoCategoryName)
        {
            string category = "";
            int NoCategoryIndex = -1;
            for (int i = 0; i < _SongsSortList.Length; i++)
            {
                if (_SongsSortList[i].SortString.Length > 0)
                {
                    if (_SongsSortList[i].SortString != category)
                    {
                        category = _SongsSortList[i].SortString;
                        _Categories.Add(new CCategory(category));
                    }
                    _SongsSortList[i].CatIndex = _Categories.Count - 1;
                }
                else
                {
                    if (NoCategoryIndex < 0)
                    {
                        category = NoCategoryName;
                        _Categories.Add(new CCategory(category));
                        NoCategoryIndex = _Categories.Count - 1;
                    }
                    _SongsSortList[i].CatIndex = NoCategoryIndex;
                }
            }

        }

        private static void _FillCategories()
        {
            string NoCategoryName = "";

            switch (_SongSorting)
            {
                case ESongSorting.TR_CONFIG_EDITION:
                    NoCategoryName = CLanguage.Translate("TR_SCREENSONG_NOEDITION");
                    break;
                case ESongSorting.TR_CONFIG_GENRE:
                    NoCategoryName = CLanguage.Translate("TR_SCREENSONG_NOGENRE");
                    break;
                case ESongSorting.TR_CONFIG_DECADE:
                case ESongSorting.TR_CONFIG_YEAR:
                    NoCategoryName = CLanguage.Translate("TR_SCREENSONG_NOYEAR");
                    break;
                case ESongSorting.TR_CONFIG_LANGUAGE:
                    NoCategoryName = CLanguage.Translate("TR_SCREENSONG_NOLANGUAGE");
                    break;
                case ESongSorting.TR_CONFIG_NONE:
                    NoCategoryName = CLanguage.Translate("TR_SCREENSONG_ALLSONGS");
                    break;
            }
            if (_SongSorting == ESongSorting.TR_CONFIG_ARTIST_LETTER || _SongSorting == ESongSorting.TR_CONFIG_TITLE_LETTER)
                _CreateCategoriesLetter();
            else
            {
                if(_SongSorting==ESongSorting.TR_CONFIG_DECADE)
                    for (int i = 0; i < _SongsSortList.Length; i++)
                    {
                        string Year = _SongsSortList[i].SortString;
                        if (Year != "")
                        {
                            Year = Year.Substring(0, 3);
                            _SongsSortList[i].SortString = Year + "0 - " + Year + "9";
                        }
                    }
                _CreateCategoriesNormal(NoCategoryName);
            }
                
        }

        public static void Sort(ESongSorting Sorting, EOffOn Tabs, EOffOn IgnoreArticles, String SearchString)
        {
            _Sort(Sorting, Tabs, IgnoreArticles, SearchString, false, _ShowDuetSongs);
        }

        public static void Sort(ESongSorting Sorting, EOffOn Tabs, EOffOn IgnoreArticles, String SearchString, bool ShowDuetSongs)
        {
            _Sort(Sorting, Tabs, IgnoreArticles, SearchString, false, ShowDuetSongs);
        }

        private static void _Sort(ESongSorting Sorting, EOffOn Tabs, EOffOn IgnoreArticles, string SearchString, bool force, bool ShowDuetSongs)
        {
            if (_Songs.Count == 0)
                return;

            if (!force && Sorting == _SongSorting && Tabs == _Tabs && IgnoreArticles == _IgnoreArticles && SearchString == _SearchFilter && ShowDuetSongs == _ShowDuetSongs)
                return; //nothing to do

            _IgnoreArticles = IgnoreArticles;
            _SongSorting = Sorting;
            _Tabs = Tabs;

            _FilterSongs(SearchString, ShowDuetSongs);
            SortSongs();
            _Categories.Clear();         

            if (_Tabs == EOffOn.TR_CONFIG_OFF)
            {
                //No categories. So don't create them!
                _Categories.Add(new CCategory(""));
                for (int i = 0; i < _SongsSortList.Length; i++)
                {
                    _SongsSortList[i].CatIndex = 0;
                }
            }else
                _FillCategories();

            foreach (CCategory cat in _Categories)
            {
                cat.CoverTextureSmall = CCover.Cover(cat.Name);
            }
            Category = _CatIndex;
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
                CSong Song = CSong.LoadSong(file);
                if (Song != null)
                {
                    Song.ID = _Songs.Count;
                    _Songs.Add(Song);
                    //Workaround to load notes if they are not loaded with the covers as there is no seperate progress indicator
                    if (CConfig.CoverLoading != ECoverLoading.TR_CONFIG_COVERLOADING_ATSTART)
                        Song.ReadNotes();
                }
            }
            CLog.StopBenchmark(2, "Read TXTs");

            CLog.StartBenchmark(2, "Sort Songs");
            _Sort(CConfig.SongSorting, CConfig.Tabs, CConfig.IgnoreArticles, String.Empty, true, true);
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
                return;

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
        }

        private static void _LoadCover()
        {
            foreach(CSong song in _Songs)
            {
                song.ReadNotes();
                song.LoadSmallCover();
                _CoverLoadIndex++;
            }
            GC.Collect();
            _CoverLoaded = true;
            CDataBase.CommitCovers();
        }
    }
}
