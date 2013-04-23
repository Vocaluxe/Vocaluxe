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
using VocaluxeLib.Menu;
using VocaluxeLib.Menu.SongMenu;

namespace Vocaluxe.Base
{
    class CSongCategorizer : CObservable
    {
        private readonly List<CCategory> _Categories = new List<CCategory>();
        private EOffOn _Tabs = CConfig.Tabs;

        public CSongCategorizer()
        {
            CSongs.Filter.ObjectChanged += _HandleSortedSongsChanged;
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
                if (value != _Tabs)
                {
                    _Tabs = value;
                    _SetChanged();
                }
            }
        }

        private void _HandleSortedSongsChanged(object sender, EventArgs args)
        {
            _SetChanged();
        }

        private void _CreateCategoriesLetter()
        {
            string category = "";
            int notLetterCat = -1;
            for (int i = 0; i < CSongs.Sorter.SortedSongs.Length; i++)
            {
                Char firstLetter = Char.ToUpper(CSongs.Sorter.SortedSongs[i].SortString.Normalize(NormalizationForm.FormD)[0]);

                if (!Char.IsLetter(firstLetter))
                    firstLetter = '#';
                if (firstLetter.ToString() != category)
                {
                    if (firstLetter != '#' || notLetterCat == -1)
                    {
                        category = firstLetter.ToString();
                        _Categories.Add(new CCategory(category));

                        CSongs.Sorter.SortedSongs[i].CatIndex = _Categories.Count - 1;

                        if (firstLetter == '#')
                            notLetterCat = CSongs.Sorter.SortedSongs[i].CatIndex;
                    }
                    else
                        CSongs.Sorter.SortedSongs[i].CatIndex = notLetterCat;
                }
                else
                    CSongs.Sorter.SortedSongs[i].CatIndex = _Categories.Count - 1;
            }
        }

        private void _CreateCategoriesNormal(string noCategoryName)
        {
            string category = "";
            int noCategoryIndex = -1;
            for (int i = 0; i < CSongs.Sorter.SortedSongs.Length; i++)
            {
                if (CSongs.Sorter.SortedSongs[i].SortString != "")
                {
                    if (CSongs.Sorter.SortedSongs[i].SortString != category)
                    {
                        category = CSongs.Sorter.SortedSongs[i].SortString;
                        _Categories.Add(new CCategory(category));
                    }
                    CSongs.Sorter.SortedSongs[i].CatIndex = _Categories.Count - 1;
                }
                else
                {
                    if (noCategoryIndex < 0)
                    {
                        category = noCategoryName;
                        _Categories.Add(new CCategory(category));
                        noCategoryIndex = _Categories.Count - 1;
                    }
                    CSongs.Sorter.SortedSongs[i].CatIndex = noCategoryIndex;
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
                    for (int i = 0; i < CSongs.Sorter.SortedSongs.Length; i++)
                    {
                        string year = CSongs.Sorter.SortedSongs[i].SortString;
                        if (year != "")
                        {
                            year = year.Substring(0, 3);
                            CSongs.Sorter.SortedSongs[i].SortString = year + "0 - " + year + "9";
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

            if (_Tabs == EOffOn.TR_CONFIG_OFF)
            {
                //No categories. So don't create them!
                _Categories.Add(new CCategory(""));
                for (int i = 0; i < CSongs.Sorter.SortedSongs.Length; i++)
                    CSongs.Sorter.SortedSongs[i].CatIndex = 0;
            }
            else
                _CreateCategories();

            foreach (CCategory cat in _Categories)
                cat.CoverTextureSmall = CCover.Cover(cat.Name);
            _Changed = false;
        }
    }
}