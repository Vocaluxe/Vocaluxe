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
using System.Collections.Generic;
using System.Linq;
using VocaluxeLib;
using VocaluxeLib.Songs;

namespace Vocaluxe.Base
{
    class CSongFilter : CObservable
    {
        private readonly List<CSong> _FilteredSongs = new List<CSong>();

        private String _SearchString = String.Empty;
        private EDuetOptions _DuetOptions = EDuetOptions.All;

        public List<CSong> FilteredSongs
        {
            get
            {
                _FilterSongs();
                return _FilteredSongs;
            }
        }

        public String SearchString
        {
            get { return _SearchString; }
            set
            {
                if (value != _SearchString)
                {
                    _SetChanged();
                    _SearchString = value;
                }
            }
        }

        public EDuetOptions DuetOptions
        {
            get { return _DuetOptions; }
            set
            {
                if (value != _DuetOptions)
                {
                    _SetChanged();
                    _DuetOptions = value;
                }
            }
        }

        public void SetOptions(String searchString, EDuetOptions duetOptions)
        {
            if (searchString != _SearchString || duetOptions != _DuetOptions)
            {
                _SearchString = searchString;
                _DuetOptions = duetOptions;
                _SetChanged();
            }
        }

        private void _FilterSongs()
        {
            if (!_Changed)
                return;

            _FilteredSongs.Clear();

            string[] searchStrings = null;
            if (_SearchString != "")
                searchStrings = _SearchString.ToUpper().Split(new char[] { ' ' });

            String searchForArtist = null;      // a:
            String searchForTitle = null;       // t:
            String searchForGenre = null;       // g:
            String searchForYear = null;        // y:
            String searchForLanguage = null;    // l:
            String searchForCreator = null;     // c:
            String searchForEdition = null;     // e:
            String searchForAlbum = null;       // al:
            String searchForFileName = null;    // fi:
            String searchForFolderName = null;  // fo:
            bool expertSearch = false;

            if (searchStrings != null)
            {
                foreach (String searchToken in searchStrings)
                {
                    if (searchToken.Length < 3)
                        continue;

                    String temp = searchToken.Substring(0, 2);
                    bool foundIt = true;

                    switch (temp)
                    {
                        case "A:":
                            searchForArtist = searchToken.Substring(2);
                            break;
                        case "T:":
                            searchForTitle = searchToken.Substring(2);
                            break;
                        case "G:":
                            searchForGenre = searchToken.Substring(2);
                            break;
                        case "Y:":
                            searchForYear = searchToken.Substring(2);
                            break;
                        case "L:":
                            searchForLanguage = searchToken.Substring(2);
                            break;
                        case "C:":
                            searchForCreator = searchToken.Substring(2);
                            break;
                        case "E:":
                            searchForEdition = searchToken.Substring(2);
                            break;
                        default:
                            foundIt = false;
                            break;
                    }

                    if (foundIt)
                    {
                        if (!expertSearch) expertSearch = true;
                        continue;
                    }

                    if (searchToken.Length < 4)
                        continue;

                    foundIt = true;
                    temp = searchToken.Substring(0, 3);

                    switch (temp)
                    {
                        case "AL:":
                            searchForAlbum = searchToken.Substring(3);
                            break;
                        case "FI:":
                            searchForFileName = searchToken.Substring(3);
                            break;
                        case "FO:":
                            searchForFolderName = searchToken.Substring(3);
                            break;
                        default:
                            foundIt = false;
                            break;
                    }

                    if (foundIt)
                        if (!expertSearch) expertSearch = true;
                }
            }

            foreach (CSong song in CSongs.Songs)
            {
                if ((song.IsDuet && _DuetOptions != EDuetOptions.NoDuets) || (!song.IsDuet && _DuetOptions != EDuetOptions.Duets))
                {
                    if (searchStrings == null)
                        _FilteredSongs.Add(song);
                    else if (expertSearch)
                    {
                        // Stefan1200: Stop at a maximum of 200 search result to prevent performance issues
                        if (_FilteredSongs.Count >= 200)
                            break;

                        if (searchForAlbum != null && song.Album.ToUpper().Contains(searchForAlbum))
                            _FilteredSongs.Add(song);
                        if (searchForArtist != null && song.Artist.ToUpper().Contains(searchForArtist))
                            _FilteredSongs.Add(song);
                        if (searchForTitle != null && song.Title.ToUpper().Contains(searchForTitle))
                            _FilteredSongs.Add(song);
                        if (searchForCreator != null && song.Creator.ToUpper().Contains(searchForCreator))
                            _FilteredSongs.Add(song);
                        if (searchForFileName != null && song.FileName.ToUpper().Contains(searchForFileName))
                            _FilteredSongs.Add(song);
                        if (searchForFolderName != null && song.FolderName.ToUpper().Contains(searchForFolderName))
                            _FilteredSongs.Add(song);

                        if (searchForGenre != null)
                        {
                            foreach (String genre in song.Genres)
                            {
                                if (genre.ToUpper().Contains(searchForGenre))
                                    _FilteredSongs.Add(song);
                            }
                        }

                        if (searchForLanguage != null)
                        {
                            foreach (String language in song.Languages)
                            {
                                if (language.ToUpper().Contains(searchForLanguage))
                                    _FilteredSongs.Add(song);
                            }
                        }

                        if (searchForEdition != null)
                        {
                            foreach (String edition in song.Editions)
                            {
                                if (edition.ToUpper().Contains(searchForEdition))
                                    _FilteredSongs.Add(song);
                            }
                        }

                        if (searchForYear != null)
                        {
                            int pos = searchForYear.IndexOf("-");
                            if (pos == -1)
                            {
                                if (song.Year.Contains(searchForYear))
                                    _FilteredSongs.Add(song);
                            }
                            else if (searchForYear.Length == 9)
                            {
                                int yearStart = -1;
                                int yearEnd = -1;
                                if (!Int32.TryParse(searchForYear.Substring(0, pos), out yearStart))
                                    continue;
                                if (!Int32.TryParse(searchForYear.Substring(pos + 1), out yearEnd))
                                    continue;

                                // Stefan1200: Prevent searches like 0000-9999, limit for a max distance of 100 years
                                if (yearEnd - yearStart > 100)
                                    continue;

                                int yearSearch = -1;
                                if (!Int32.TryParse(song.Year, out yearSearch))
                                    continue;

                                if (yearSearch >= yearStart && yearSearch <= yearEnd)
                                    _FilteredSongs.Add(song);
                            }
                        }
                    }
                    else
                    {
                        string search = song.Title.ToUpper() + " " + song.Artist.ToUpper();
                        
                        if (searchStrings.All(search.Contains))
                            _FilteredSongs.Add(song);
                    }
                }
            }
            _Changed = false;
        }
    }
}
