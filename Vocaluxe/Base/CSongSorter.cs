using System;
using System.Collections.Generic;
using System.Reflection;
using VocaluxeLib.Menu;
using VocaluxeLib.Menu.SongMenu;

namespace Vocaluxe.Base
{
    class CSongSorter : CObservable
    {
        private SSongPointer[] _SortedSongs = new SSongPointer[0];
        private EOffOn _IgnoreArticles = CConfig.IgnoreArticles;
        private ESongSorting _SongSorting = CConfig.SongSorting;

        public CSongSorter()
        {
            CSongs.Filter.ObjectChanged += _HandleFilteredSongsChanged;
        }

        public SSongPointer[] SortedSongs
        {
            get
            {
                _SortSongs();
                return _SortedSongs;
            }
        }

        public EOffOn IgnoreArticles
        {
            get { return _IgnoreArticles; }
            set
            {
                if (value != _IgnoreArticles)
                {
                    _IgnoreArticles = value;
                    _SetChanged();
                }
            }
        }

        public ESongSorting SongSorting
        {
            get { return _SongSorting; }
            set
            {
                if (value != _SongSorting)
                {
                    _SongSorting = value;
                    _SetChanged();
                }
            }
        }

        public void SetOptions(ESongSorting songSorting, EOffOn ignoreArticles)
        {
            if (songSorting != _SongSorting || ignoreArticles != _IgnoreArticles)
            {
                _SongSorting = songSorting;
                _IgnoreArticles = ignoreArticles;
                _SetChanged();
            }
        }

        private void _HandleFilteredSongsChanged(object sender, EventArgs args)
        {
            _SetChanged();
        }

        private int _SortByFieldArtistTitle(SSongPointer s1, SSongPointer s2)
        {
            int res = s1.SortString.ToUpper().CompareTo(s2.SortString.ToUpper());
            if (res == 0)
            {
                if (_IgnoreArticles == EOffOn.TR_CONFIG_ON)
                {
                    res = CSongs.Songs[s1.SongID].ArtistSorting.ToUpper().CompareTo(CSongs.Songs[s2.SongID].ArtistSorting.ToUpper());
                    if (res == 0)
                        return CSongs.Songs[s1.SongID].TitleSorting.ToUpper().CompareTo(CSongs.Songs[s2.SongID].TitleSorting.ToUpper());
                    return res;
                }
                else
                {
                    res = CSongs.Songs[s1.SongID].Artist.ToUpper().CompareTo(CSongs.Songs[s2.SongID].Artist.ToUpper());
                    if (res == 0)
                        return CSongs.Songs[s1.SongID].Title.ToUpper().CompareTo(CSongs.Songs[s2.SongID].Title.ToUpper());
                    return res;
                }
            }
            return res;
        }

        private int _SortByFieldTitle(SSongPointer s1, SSongPointer s2)
        {
            int res = s1.SortString.ToUpper().CompareTo(s2.SortString.ToUpper());
            if (res == 0)
            {
                if (_IgnoreArticles == EOffOn.TR_CONFIG_ON)
                    return CSongs.Songs[s1.SongID].TitleSorting.ToUpper().CompareTo(CSongs.Songs[s2.SongID].TitleSorting.ToUpper());
                else
                    return CSongs.Songs[s1.SongID].Title.ToUpper().CompareTo(CSongs.Songs[s2.SongID].Title.ToUpper());
            }
            return res;
        }

        private List<SSongPointer> _CreateSortList(string fieldName)
        {
            FieldInfo field = null;
            bool isString = false;
            List<SSongPointer> sortList = new List<SSongPointer>();
            if (fieldName.Length == 0)
                CSongs.Filter.FilteredSongs.ForEach((song) => sortList.Add(new SSongPointer(song.ID, "")));
            else
            {
                field = typeof(CSong).GetField(fieldName, BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
                isString = field.FieldType == typeof(string);
                if (!isString && field.FieldType != typeof(List<String>))
                    throw new Exception("Unkown sort field type");
                foreach (CSong song in CSongs.Filter.FilteredSongs)
                {
                    object value = field.GetValue(song);
                    if (isString)
                        sortList.Add(new SSongPointer(song.ID, (String)value));
                    else
                    {
                        List<String> values = (List<String>)value;
                        if (values.Count == 0)
                            sortList.Add(new SSongPointer(song.ID, ""));
                        else
                        {
                            foreach (String sortString in (List<String>)value)
                                sortList.Add(new SSongPointer(song.ID, sortString));
                        }
                    }
                }
            }
            return sortList;
        }

        private void _SortSongs()
        {
            if (!_Changed)
                return;
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
            List<SSongPointer> sortList = _CreateSortList(fieldName);
            switch (_SongSorting)
            {
                case ESongSorting.TR_CONFIG_ARTIST_LETTER:
                case ESongSorting.TR_CONFIG_ARTIST:
                case ESongSorting.TR_CONFIG_NONE:
                    sortList.Sort(_SortByFieldTitle);
                    break;
                default:
                    sortList.Sort(_SortByFieldArtistTitle);
                    break;
            }
            _SortedSongs = sortList.ToArray();
            _Changed = false;
        }
    }
}