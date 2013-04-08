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
            CSongs.Filter.ObjectChanged += HandleSortedSongsChanged;
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

        private void HandleSortedSongsChanged(object sender, EventArgs args)
        {
            _SetChanged();
        }

        private void _CreateCategoriesLetter()
        {
            string category = "";
            int NotLetterCat = -1;
            for (int i = 0; i < CSongs.Sorter.SortedSongs.Length; i++)
            {
                Char firstLetter = Char.ToUpper(CSongs.Sorter.SortedSongs[i].SortString.Normalize(NormalizationForm.FormD)[0]);

                if (!Char.IsLetter(firstLetter))
                    firstLetter = '#';
                if (firstLetter.ToString() != category)
                {
                    if (firstLetter != '#' || NotLetterCat == -1)
                    {
                        category = firstLetter.ToString();
                        _Categories.Add(new CCategory(category));

                        CSongs.Sorter.SortedSongs[i].CatIndex = _Categories.Count - 1;

                        if (firstLetter == '#')
                            NotLetterCat = CSongs.Sorter.SortedSongs[i].CatIndex;
                    }
                    else
                        CSongs.Sorter.SortedSongs[i].CatIndex = NotLetterCat;
                }
                else
                    CSongs.Sorter.SortedSongs[i].CatIndex = _Categories.Count - 1;
            }
        }

        private void _CreateCategoriesNormal(string NoCategoryName)
        {
            string category = "";
            int NoCategoryIndex = -1;
            for (int i = 0; i < CSongs.Sorter.SortedSongs.Length; i++)
            {
                if (CSongs.Sorter.SortedSongs[i].SortString.Length > 0)
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
                    if (NoCategoryIndex < 0)
                    {
                        category = NoCategoryName;
                        _Categories.Add(new CCategory(category));
                        NoCategoryIndex = _Categories.Count - 1;
                    }
                    CSongs.Sorter.SortedSongs[i].CatIndex = NoCategoryIndex;
                }
            }
        }

        private void _CreateCategories()
        {
            string NoCategoryName = "";

            ESongSorting Sorting = CSongs.Sorter.SongSorting;

            switch (Sorting)
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
            if (Sorting == ESongSorting.TR_CONFIG_ARTIST_LETTER || Sorting == ESongSorting.TR_CONFIG_TITLE_LETTER)
                _CreateCategoriesLetter();
            else
            {
                if (Sorting == ESongSorting.TR_CONFIG_DECADE)
                {
                    for (int i = 0; i < CSongs.Sorter.SortedSongs.Length; i++)
                    {
                        string Year = CSongs.Sorter.SortedSongs[i].SortString;
                        if (Year.Length > 0)
                        {
                            Year = Year.Substring(0, 3);
                            CSongs.Sorter.SortedSongs[i].SortString = Year + "0 - " + Year + "9";
                        }
                    }
                }
                _CreateCategoriesNormal(NoCategoryName);
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