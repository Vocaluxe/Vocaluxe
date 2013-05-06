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
                searchStrings = _SearchString.ToUpper().Split(new char[] {' '});

            foreach (CSong song in CSongs.Songs)
            {
                if ((song.IsDuet && _DuetOptions != EDuetOptions.NoDuets) || (!song.IsDuet && _DuetOptions != EDuetOptions.Duets))
                {
                    if (searchStrings == null)
                        _FilteredSongs.Add(song);
                    else
                    {
                        string search = song.Title.ToUpper() + " " + song.Artist.ToUpper() + " " + song.FolderName.ToUpper() + " " + song.FileName.ToUpper();

                        if (searchStrings.All(search.Contains))
                            _FilteredSongs.Add(song);
                    }
                }
            }
            _Changed = false;
        }
    }
}