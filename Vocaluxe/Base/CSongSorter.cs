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
using System.Reflection;
using VocaluxeLib;
using VocaluxeLib.Songs;

namespace Vocaluxe.Base
{
    class CSongSorter : CObservable
    {
        private CSongPointer[] _SortedSongs = new CSongPointer[0];
        private EOffOn _IgnoreArticles = CConfig.IgnoreArticles;
        private ESongSorting _SongSorting = CConfig.SongSorting;

        public CSongSorter()
        {
            CSongs.Filter.ObjectChanged += _HandleFilteredSongsChanged;
        }

        public CSongPointer[] SortedSongs
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
                if (value == _IgnoreArticles)
                    return;
                _IgnoreArticles = value;
                _SetChanged();
            }
        }

        public ESongSorting SongSorting
        {
            get { return _SongSorting; }
            set
            {
                if (value == _SongSorting)
                    return;
                _SongSorting = value;
                _SetChanged();
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

        private int _SortByFieldArtistTitle(CSongPointer s1, CSongPointer s2)
        {
            int res = String.Compare(s1.SortString, s2.SortString, StringComparison.CurrentCultureIgnoreCase);
            if (res == 0)
            {
                if (_IgnoreArticles == EOffOn.TR_CONFIG_ON)
                {
                    res = String.Compare(CSongs.Songs[s1.SongID].ArtistSorting, CSongs.Songs[s2.SongID].ArtistSorting, StringComparison.CurrentCultureIgnoreCase);
                    return res != 0 ? res : String.Compare(CSongs.Songs[s1.SongID].TitleSorting, CSongs.Songs[s2.SongID].TitleSorting, StringComparison.CurrentCultureIgnoreCase);
                }
                res = String.Compare(CSongs.Songs[s1.SongID].Artist, CSongs.Songs[s2.SongID].Artist, StringComparison.CurrentCultureIgnoreCase);
                return res != 0 ? res : String.Compare(CSongs.Songs[s1.SongID].Title, CSongs.Songs[s2.SongID].Title, StringComparison.CurrentCultureIgnoreCase);
            }
            return res;
        }

        private int _SortByFieldTitle(CSongPointer s1, CSongPointer s2)
        {
            int res = String.Compare(s1.SortString, s2.SortString, StringComparison.CurrentCultureIgnoreCase);
            if (res == 0)
            {
                return _IgnoreArticles == EOffOn.TR_CONFIG_ON
                           ? String.Compare(CSongs.Songs[s1.SongID].TitleSorting, CSongs.Songs[s2.SongID].TitleSorting, StringComparison.CurrentCultureIgnoreCase) :
                           String.Compare(CSongs.Songs[s1.SongID].Title, CSongs.Songs[s2.SongID].Title, StringComparison.CurrentCultureIgnoreCase);
            }
            return res;
        }

        private List<CSongPointer> _CreateSortList(string fieldName)
        {
            List<CSongPointer> sortList = new List<CSongPointer>();
            if (fieldName == "")
                CSongs.Filter.FilteredSongs.ForEach(song => sortList.Add(new CSongPointer(song.ID, "")));
            else
            {
                FieldInfo field = typeof(CSong).GetField(fieldName, BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
                if (field == null)
                {
                    CLog.LogError("Unknow sorting field: " + fieldName);
                    return _CreateSortList("");
                }
                bool isString = field.FieldType == typeof(string);
                if (!isString && field.FieldType != typeof(List<String>))
                    throw new Exception("Unkown sort field type");
                foreach (CSong song in CSongs.Filter.FilteredSongs)
                {
                    object value = field.GetValue(song);
                    if (isString)
                        sortList.Add(new CSongPointer(song.ID, (String)value));
                    else
                    {
                        List<String> values = (List<String>)value;
                        if (values.Count == 0)
                            sortList.Add(new CSongPointer(song.ID, ""));
                        else
                        {
                            // ReSharper disable LoopCanBeConvertedToQuery
                            foreach (String sortString in values)
                                // ReSharper restore LoopCanBeConvertedToQuery
                                sortList.Add(new CSongPointer(song.ID, sortString));
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
                    fieldName = _IgnoreArticles == EOffOn.TR_CONFIG_ON ? "ArtistSorting" : "Artist";
                    break;
                case ESongSorting.TR_CONFIG_TITLE_LETTER:
                    fieldName = _IgnoreArticles == EOffOn.TR_CONFIG_ON ? "TitleSorting" : "Title";
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
            List<CSongPointer> sortList = _CreateSortList(fieldName);
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