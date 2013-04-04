using System;
using System.Collections.Generic;
using VocaluxeLib.Menu;
using VocaluxeLib.Menu.SongMenu;

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
            if (_SearchString.Length > 0)
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

                        bool contains = true;

                        foreach (string str in searchStrings)
                            contains &= search.Contains(str);
                        if (contains)
                            _FilteredSongs.Add(song);
                    }
                }
            }
            _Changed = false;
        }
    }
}