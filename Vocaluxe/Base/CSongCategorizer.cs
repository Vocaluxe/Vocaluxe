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
using System.Text;
using VocaluxeLib;
using VocaluxeLib.Songs;

namespace Vocaluxe.Base
{
    class CSongCategorizer : CObservable
    {
        private readonly List<CCategory> _Categories = new List<CCategory>();
        private EOffOn _Tabs = CConfig.Tabs;

        public CSongCategorizer()
        {
            CSongs.Sorter.ObjectChanged += _HandleSortedSongsChanged;
        }

        public List<CCategory> Categories
        {
            get
            {
                _FillCategories();
                return _Categories;
            }
        }

        public EOffOn Tabs
        {
            get { return _Tabs; }
            set
            {
                if (value == _Tabs)
                    return;
                _Tabs = value;
                if (_Tabs == EOffOn.TR_CONFIG_ON)
                    CSongs.Category = -1;
                _SetChanged();
            }
        }

        private void _HandleSortedSongsChanged(object sender, EventArgs args)
        {
            _SetChanged();
        }

        private void _CreateCategoriesLetter()
        {
            CCategory lastCategory = null;
            CCategory notLetterCat = null;
            foreach (CSongPointer songPointer in CSongs.Sorter.SortedSongs)
            {
                Char firstLetter = Char.ToUpper(songPointer.SortString.Normalize(NormalizationForm.FormD)[0]);
                if (Char.IsLetter(firstLetter))
                {
                    if (lastCategory == null || firstLetter.ToString() != lastCategory.Name)
                    {
                        lastCategory = new CCategory(firstLetter.ToString());
                        _Categories.Add(lastCategory);
                    }
                    lastCategory.Songs.Add(songPointer);
                }
                else
                {
                    if (notLetterCat == null)
                    {
                        notLetterCat = new CCategory("#");
                        _Categories.Add(notLetterCat);
                    }
                    notLetterCat.Songs.Add(songPointer);
                }
            }
        }

        private void _CreateCategoriesNormal(string noCategoryName)
        {
            CCategory lastCategory = null;
            CCategory noCategory = null;
            foreach (CSongPointer songPointer in CSongs.Sorter.SortedSongs)
            {
                if (songPointer.SortString != "")
                {
                    if (lastCategory == null || songPointer.SortString != lastCategory.Name)
                    {
                        lastCategory = new CCategory(songPointer.SortString);
                        _Categories.Add(lastCategory);
                    }
                    lastCategory.Songs.Add(songPointer);
                }
                else
                {
                    if (noCategory == null)
                    {
                        noCategory = new CCategory(noCategoryName);
                        _Categories.Add(noCategory);
                    }
                    noCategory.Songs.Add(songPointer);
                }
            }
        }

        private void _CreateCategories()
        {
            string noCategoryName = "";

            ESongSorting sorting = CSongs.Sorter.SongSorting;

            switch (sorting)
            {
                case ESongSorting.TR_CONFIG_EDITION:
                    noCategoryName = CLanguage.Translate("TR_SCREENSONG_NOEDITION");
                    break;
                case ESongSorting.TR_CONFIG_GENRE:
                    noCategoryName = CLanguage.Translate("TR_SCREENSONG_NOGENRE");
                    break;
                case ESongSorting.TR_CONFIG_DECADE:
                case ESongSorting.TR_CONFIG_YEAR:
                    noCategoryName = CLanguage.Translate("TR_SCREENSONG_NOYEAR");
                    break;
                case ESongSorting.TR_CONFIG_LANGUAGE:
                    noCategoryName = CLanguage.Translate("TR_SCREENSONG_NOLANGUAGE");
                    break;
                case ESongSorting.TR_CONFIG_NONE:
                    noCategoryName = CLanguage.Translate("TR_SCREENSONG_ALLSONGS");
                    break;
            }
            if (sorting == ESongSorting.TR_CONFIG_ARTIST_LETTER || sorting == ESongSorting.TR_CONFIG_TITLE_LETTER)
                _CreateCategoriesLetter();
            else
            {
                if (sorting == ESongSorting.TR_CONFIG_DECADE)
                {
                    foreach (CSongPointer songPointer in CSongs.Sorter.SortedSongs)
                    {
                        string year = songPointer.SortString;
                        if (year != "")
                        {
                            year = year.Substring(0, 3);
                            songPointer.SortString = year + "0 - " + year + "9";
                        }
                    }
                }
                _CreateCategoriesNormal(noCategoryName);
            }
        }

        private void _FillCategories()
        {
            if (!_Changed)
                return;

            _Categories.Clear();

            if (_Tabs != EOffOn.TR_CONFIG_OFF)
                _CreateCategories();
            else
            {
                //No categories. So don't create them!
                _Categories.Add(new CCategory(""));
                _Categories[0].Songs.AddRange(CSongs.Sorter.SortedSongs);
            }

            foreach (CCategory cat in _Categories)
                cat.CoverTextureSmall = CCover.Cover(cat.Name);
            _Changed = false;
        }
    }
}