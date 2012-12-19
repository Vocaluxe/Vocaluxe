using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

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

        public SongPointer(int ID, string sortString)
        {
            SongID = ID;
            SortString = sortString;
            CatIndex = -1;
            Visible = false;
        }
    }

    static class CSongs
    {
        private static List<CSong> _Songs = new List<CSong>();
        private static SongPointer[] _SongsSortList = new SongPointer[0];
        private static List<CSong> _SongsForRandom = new List<CSong>();

        private static CHelper Helper = new CHelper();
        private static bool _SongsLoaded = false;
        private static bool _CoverLoaded = false;
        private static int _CoverLoadIndex = -1;
        private static int _CatIndex = -1;
        private static List<CCategory> _Categories = new List<CCategory>();
        private static List<CCategory> _CategoriesForRandom = new List<CCategory>();

        private static Stopwatch _CoverLoadTimer = new Stopwatch();

        private static string _SearchFilter = String.Empty;
        private static EOffOn _Tabs = CConfig.Tabs;
        private static EOffOn _IgnoreArticles = CConfig.IgnoreArticles;
        private static ESongSorting _SongSorting = CConfig.SongSorting;
        private static bool _ShowDuetSongs = true;

        private static Thread _CoverLoaderThread = new Thread(new ThreadStart(_LoadCover));
                    
        public static string SearchFilter
        {
            get { return _SearchFilter; }
            set
            {
                if (value.Length > 0)
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
                        _SongsSortList[i].Visible = (_SongsSortList[i].CatIndex == _CatIndex);
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
                if (_SongsSortList[i].CatIndex == CatIndex)
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

            if (SongIndex <= _Songs.Count)
            {
                _Songs[SongIndex].CoverTextureSmall = Texture;
                //_Songs[SongIndex].CoverSmallLoaded = true;
            }

            if (SongIndex == _Songs.Count - 1)
                _CoverLoaded = true;
        }
        public static void SetCoverBig(int SongIndex, STexture Texture)
        {
            if (!_SongsLoaded)
                return;

            if (SongIndex <= _Songs.Count)
            {
                _Songs[SongIndex].CoverTextureBig = Texture;
                _Songs[SongIndex].CoverBigLoaded = true;
            }

            if (SongIndex == _Songs.Count - 1)
                _CoverLoaded = true;
        }

        public static string GetActualCategoryName()
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

            _Categories.Clear();
            string category = String.Empty;

            _IgnoreArticles = IgnoreArticles;
            _SongSorting = Sorting;
            _Tabs = Tabs;
            _ShowDuetSongs = ShowDuetSongs;

            _SearchFilter = SearchString;

            List<SongPointer> _SortList = new List<SongPointer>();
            List<CSong> _SongList = new List<CSong>();

            foreach (CSong song in _Songs)
            {
                if (!song.IsDuet || _ShowDuetSongs)
                {
                    if (_SearchFilter == String.Empty)
                        _SongList.Add(song);
                    else
                    {
                        if (song.Title.ToUpper().Contains(_SearchFilter.ToUpper()) || song.Artist.ToUpper().Contains(_SearchFilter.ToUpper()))
                            _SongList.Add(song);
                    }
                }
            }

            switch (_SongSorting)
            {
                case ESongSorting.TR_CONFIG_EDITION:
                    foreach (CSong song in _SongList)
                    {
                        if (song.Edition.Count == 0)
                            _SortList.Add(new SongPointer(song.ID, String.Empty));
                        else
                        {
                            for (int i = 0; i < song.Edition.Count; i++)
                            {
                                _SortList.Add(new SongPointer(song.ID, song.Edition[i]));
                            }
                        }
                    }

                    _SortList.Sort(delegate(SongPointer s1, SongPointer s2) 
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
                    });

                    _SongsSortList = _SortList.ToArray();
                    _Categories.Clear();
                    for (int i = 0; i < _SongsSortList.Length; i++ )
                    {
                        if (_SongsSortList[i].SortString.Length > 0)
                        {
                            if (_SongsSortList[i].SortString != category)
                            {
                                category = _SongsSortList[i].SortString;
                                _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                            }
                            _SongsSortList[i].CatIndex = _Categories.Count - 1;
                        }
                        else
                        {
                            if (CLanguage.Translate("TR_SCREENSONG_NOEDITION") != category)
                            {
                                category = CLanguage.Translate("TR_SCREENSONG_NOEDITION");
                                _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                            }
                            _SongsSortList[i].CatIndex = _Categories.Count - 1;
                        }
                    }
                    break;

                case ESongSorting.TR_CONFIG_GENRE:
                    foreach (CSong song in _SongList)
                    {
                        if (song.Genre.Count == 0)
                            _SortList.Add(new SongPointer(song.ID, String.Empty));
                        else
                        {
                            for (int i = 0; i < song.Genre.Count; i++)
                            {
                                _SortList.Add(new SongPointer(song.ID, song.Genre[i]));
                            }
                        }
                    }

                    _SortList.Sort(delegate(SongPointer s1, SongPointer s2)
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
                    });

                    _SongsSortList = _SortList.ToArray();
                    _Categories.Clear();
                    for (int i = 0; i < _SongsSortList.Length; i++)
                    {
                        if (_SongsSortList[i].SortString.Length > 0)
                        {

                            if (_SongsSortList[i].SortString != category)
                            {
                                category = _SongsSortList[i].SortString;
                                _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                            }
                            _SongsSortList[i].CatIndex = _Categories.Count - 1;
                        }
                        else
                        {
                            if (CLanguage.Translate("TR_SCREENSONG_NOGENRE") != category)
                            {
                                category = CLanguage.Translate("TR_SCREENSONG_NOGENRE");
                                _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                            }
                            _SongsSortList[i].CatIndex = _Categories.Count - 1;
                        }
                    }
                    break;

                case ESongSorting.TR_CONFIG_NONE:
                    foreach (CSong song in _SongList)
                    { 
                        _SortList.Add(new SongPointer(song.ID, String.Empty));  
                    }

                    _SortList.Sort(delegate(SongPointer s1, SongPointer s2)
                    {
                        int res = s1.SortString.ToUpper().CompareTo(s2.SortString.ToUpper());
                        if (res == 0)
                        {
                            return _Songs[s1.SongID].Title.ToUpper().CompareTo(_Songs[s2.SongID].Title.ToUpper());
                        }
                        return res;
                    });

                    _SongsSortList = _SortList.ToArray();
                    _Categories.Clear();
                    for (int i = 0; i < _SongsSortList.Length; i++)
                    {
                        _SongsSortList[i].CatIndex = 0;
                    }
                    category = CLanguage.Translate("TR_SCREENSONG_ALLSONGS");
                    _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                    break;

                case ESongSorting.TR_CONFIG_FOLDER:
                    foreach (CSong song in _SongList)
                    {
                        _SortList.Add(new SongPointer(song.ID, song.FolderName));
                    }

                    _SortList.Sort(delegate(SongPointer s1, SongPointer s2) 
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
                    });

                    _SongsSortList = _SortList.ToArray();
                    _Categories.Clear();
                    for (int i = 0; i < _SongsSortList.Length; i++)
                    {
                        if (_SongsSortList[i].SortString != category)
                        {
                            category = _SongsSortList[i].SortString;
                            _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                        }
                        _SongsSortList[i].CatIndex = _Categories.Count - 1;
                    }
                    break;

                case ESongSorting.TR_CONFIG_ARTIST:
                    foreach (CSong song in _SongList)
                    {
                            _SortList.Add(new SongPointer(song.ID, song.Artist));
                    }

                    _SortList.Sort(delegate(SongPointer s1, SongPointer s2)
                    {
                        if (_IgnoreArticles == EOffOn.TR_CONFIG_ON)
                        {
                            int res = _Songs[s1.SongID].ArtistSorting.ToUpper().CompareTo(_Songs[s2.SongID].ArtistSorting.ToUpper());
                            if (res == 0)
                            {
                                return _Songs[s1.SongID].TitleSorting.ToUpper().CompareTo(_Songs[s2.SongID].TitleSorting.ToUpper());
                            }
                            return res;
                        }
                        else
                        {
                            int res = s1.SortString.ToUpper().CompareTo(s2.SortString.ToUpper());
                            if (res == 0)
                            {
                                return _Songs[s1.SongID].Title.ToUpper().CompareTo(_Songs[s2.SongID].Title.ToUpper());
                            }
                            return res;
                        }
                    });

                    _SongsSortList = _SortList.ToArray();
                    _Categories.Clear();
                    for (int i = 0; i < _SongsSortList.Length; i++)
                    {
                        if (_SongsSortList[i].SortString != category)
                        {
                            category = _SongsSortList[i].SortString;
                            _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                        }
                        _SongsSortList[i].CatIndex = _Categories.Count - 1;
                    }
                    break;

                case ESongSorting.TR_CONFIG_ARTIST_LETTER:
                    foreach (CSong song in _SongList)
                    {
                        if (_IgnoreArticles == EOffOn.TR_CONFIG_ON)
                            _SortList.Add(new SongPointer(song.ID, song.ArtistSorting));
                        else
                            _SortList.Add(new SongPointer(song.ID, song.Artist));
                    }

                    _SortList.Sort(delegate(SongPointer s1, SongPointer s2)
                    {
                        int res = s1.SortString.ToUpper().CompareTo(s2.SortString.ToUpper());
                        if (res == 0)
                        {
                            if (_IgnoreArticles == EOffOn.TR_CONFIG_ON)
                            {
                                return _Songs[s1.SongID].TitleSorting.ToUpper().CompareTo(_Songs[s2.SongID].TitleSorting.ToUpper());
                            }
                            else
                            {
                                return _Songs[s1.SongID].Title.ToUpper().CompareTo(_Songs[s2.SongID].Title.ToUpper());
                            }
                        }
                        return res;
                    });

                    _SongsSortList = _SortList.ToArray();
                    _Categories.Clear();

                    int NotLetterCat = -1;
                    for (int i=0; i < _SongsSortList.Length; i++)
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
                                _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));

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
                    break;

                case ESongSorting.TR_CONFIG_TITLE_LETTER:
                    foreach (CSong song in _SongList)
                    {
                        if (_IgnoreArticles == EOffOn.TR_CONFIG_ON)
                            _SortList.Add(new SongPointer(song.ID, song.TitleSorting));
                        else
                            _SortList.Add(new SongPointer(song.ID, song.Title));
                    }

                    _SortList.Sort(delegate(SongPointer s1, SongPointer s2)
                    {
                        int res = s1.SortString.ToUpper().CompareTo(s2.SortString.ToUpper());
                        if (res == 0)
                        {
                            if (_IgnoreArticles == EOffOn.TR_CONFIG_ON)
                            {
                                return _Songs[s1.SongID].ArtistSorting.ToUpper().CompareTo(_Songs[s2.SongID].ArtistSorting.ToUpper());
                            }
                            else
                            {
                                return _Songs[s1.SongID].Artist.ToUpper().CompareTo(_Songs[s2.SongID].Artist.ToUpper());
                            }
                        }
                        return res;
                    });

                    _SongsSortList = _SortList.ToArray();
                    _Categories.Clear();

                    NotLetterCat = -1;
                    for (int i=0; i < _SongsSortList.Length; i++)
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
                                _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));

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
                    break;

                case ESongSorting.TR_CONFIG_DECADE:
                    foreach (CSong song in _SongList)
                    {
                        _SortList.Add(new SongPointer(song.ID, song.Year));
                    }

                    _SortList.Sort(delegate(SongPointer s1, SongPointer s2)
                    {
                        int res = s1.SortString.CompareTo(s2.SortString);
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
                    });

                    _SongsSortList = _SortList.ToArray();
                    _Categories.Clear();
                    for (int i=0; i < _SongsSortList.Length; i++)
                    {
                        if (_SongsSortList[i].SortString.Length > 0 && !_SongsSortList[i].SortString.Equals("0000"))
                        {
                            String decade = _SongsSortList[i].SortString.Substring(0, 3) + "0 - " + _SongsSortList[i].SortString.Substring(0, 3) + "9";
                            if (decade != category)
                            {
                                category = decade;
                                _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                            }
                            _SongsSortList[i].CatIndex = _Categories.Count - 1;
                        }
                        else
                        {
                            if (CLanguage.Translate("TR_SCREENSONG_NOYEAR") != category)
                            {
                                category = CLanguage.Translate("TR_SCREENSONG_NOYEAR");
                                _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                            }
                            _SongsSortList[i].CatIndex = _Categories.Count - 1;
                        }
                    }
                    break;

                case ESongSorting.TR_CONFIG_YEAR:
                    foreach (CSong song in _SongList)
                    {
                        _SortList.Add(new SongPointer(song.ID, song.Year));
                    }

                    _SortList.Sort(delegate(SongPointer s1, SongPointer s2)
                    {
                        int res = s1.SortString.CompareTo(s2.SortString);
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
                    });

                    _SongsSortList = _SortList.ToArray();
                    _Categories.Clear();
                    for (int i=0; i < _SongsSortList.Length; i++)
                    {
                        if (_SongsSortList[i].SortString.Length > 0 && !_SongsSortList[i].SortString.Equals("0000"))
                        {
                            if (_SongsSortList[i].SortString != category)
                            {
                                category = _SongsSortList[i].SortString;
                                _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                            }
                            _SongsSortList[i].CatIndex = _Categories.Count - 1;
                        }
                        else
                        {
                            if (CLanguage.Translate("TR_SCREENSONG_NOYEAR") != category)
                            {
                                category = CLanguage.Translate("TR_SCREENSONG_NOYEAR");
                                _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                            }
                            _SongsSortList[i].CatIndex = _Categories.Count - 1;
                        }
                    }
                    break;

                case ESongSorting.TR_CONFIG_LANGUAGE:
                    foreach (CSong song in _SongList)
                    {
                        if (song.Language.Count == 0)
                            _SortList.Add(new SongPointer(song.ID, String.Empty));
                        else
                        {
                            for (int i = 0; i < song.Language.Count; i++)
                            {
                                _SortList.Add(new SongPointer(song.ID, song.Language[i]));
                            }
                        }
                    }

                    _SortList.Sort(delegate(SongPointer s1, SongPointer s2)
                    {
                        int res = s1.SortString.CompareTo(s2.SortString);
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
                    });

                    _SongsSortList = _SortList.ToArray();
                    _Categories.Clear();
                    for (int i=0; i < _SongsSortList.Length; i++)
                    {
                        if (_SongsSortList[i].SortString.Length > 0)
                        {
                            if (_SongsSortList[i].SortString != category)
                            {
                                category = _SongsSortList[i].SortString;
                                _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                            }
                            _SongsSortList[i].CatIndex = _Categories.Count - 1;
                        }
                        else
                        {
                            if (CLanguage.Translate("TR_SCREENSONG_NOLANGUAGE") != category)
                            {
                                category = CLanguage.Translate("TR_SCREENSONG_NOLANGUAGE");
                                _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                            }
                            _SongsSortList[i].CatIndex = _Categories.Count - 1;
                        }
                    }
                    break;
                default:
                    break;
            }


            if (_Tabs == EOffOn.TR_CONFIG_OFF)
            {
                _Categories.Clear();
                _Categories.Add(new CCategory("", new STexture(-1), new STexture(-1)));
                for (int i = 0; i < _SongsSortList.Length; i++)
                {
                    _SongsSortList[i].CatIndex = 0;
                }
            }

            foreach (CCategory cat in _Categories)
            {
                STexture cover = CCover.Cover(cat.Name);
                cat.CoverTextureSmall = cover;
            }
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
                files.AddRange(Helper.ListFiles(path, "*.txt", true, true));
                files.AddRange(Helper.ListFiles(path, "*.txd", true, true));
            }
            CLog.StopBenchmark(2, "List Songs");

            CLog.StartBenchmark(2, "Read TXTs");
            foreach (string file in files)
            {
                CSong Song = new CSong(CMain.Base);
                if (Song.ReadTXTSong(file))
                {
                    Song.ID = _Songs.Count;
                    _Songs.Add(Song);
                }
            }
            CLog.StopBenchmark(2, "Read TXTs");

            CLog.StartBenchmark(2, "Sort Songs");
            _Sort(CConfig.SongSorting, CConfig.Tabs, CConfig.IgnoreArticles, String.Empty, true, true);
            CLog.StopBenchmark(2, "Sort Songs");
            Category = -1;
            _SongsLoaded = true;

            if (CConfig.Renderer != ERenderer.TR_CONFIG_SOFTWARE && CConfig.CoverLoading == ECoverLoading.TR_CONFIG_COVERLOADING_ATSTART)
            {
                CLog.StartBenchmark(2, "Load Cover");
                for (int i = 0; i < _Songs.Count; i++)
                {
                    CSong song = _Songs[i];

                    song.ReadNotes();
                    STexture texture = song.CoverTextureSmall;
                    song.CoverTextureBig = texture;
                    _CoverLoadIndex++;
                }

                _CoverLoaded = true;
                CDataBase.CommitCovers();
                CLog.StopBenchmark(2, "Load Cover");
            }
            CLog.StopBenchmark(1, "Load Songs ");
        }

        public static void LoadCover(long WaitTime, int NumLoads)
        {
            if (CConfig.Renderer == ERenderer.TR_CONFIG_SOFTWARE)
                return; //should be removed as soon as the other renderer are ready for queque

            if (!SongsLoaded)
                return;

            if (CoverLoaded)
                return;

            if (_CoverLoaderThread.ThreadState == System.Threading.ThreadState.Unstarted)
            {
                _CoverLoaderThread.Name = "CoverLoader";
                _CoverLoaderThread.Priority = ThreadPriority.BelowNormal;
                _CoverLoaderThread.IsBackground = true;
                _CoverLoaderThread.Start();
            }

            /*
            

            if (!_CoverLoadTimer.IsRunning)
            {
                _CoverLoadTimer.Reset();
                _CoverLoadTimer.Start();
            }

            STexture texture = new STexture(-1);
            if (_CoverLoadTimer.ElapsedMilliseconds >= WaitTime)
            {
                for (int i = 0; i < NumLoads; i++)
                {
                    CSong song = new CSong();
                    int n = GetNextSongWithoutCover(ref song);

                    if (n < 0)
                        return;

                    song.ReadNotes();
                    texture = song.CoverTextureSmall;

                    SetCoverSmall(n, texture);
                    SetCoverBig(n, texture);

                    if (CoverLoaded)
                        CDataBase.CommitCovers();

                    _CoverLoadTimer.Reset();
                    _CoverLoadTimer.Start();
                }
            }
             * */
        }

        private static void _LoadCover()
        {
            for (int i = 0; i < _Songs.Count; i++)
            {
                CSong song = _Songs[i];

                song.ReadNotes();
                STexture texture = song.CoverTextureSmall;
                song.CoverTextureBig = texture;
                _CoverLoadIndex++;
            }

            _CoverLoaded = true;
            CDataBase.CommitCovers();
        }
    }
}
