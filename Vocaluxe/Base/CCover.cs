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
using System.IO;
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Base
{
    struct SCoverTheme
    {
        public string Name;
        public string Folder;
        public string File;
    }

    static class CCover
    {
        private static readonly Dictionary<string, CTexture> _Covers = new Dictionary<string, CTexture>();
        private static readonly List<SCoverTheme> _CoverThemes = new List<SCoverTheme>();

        public static CTexture NoCover { get; private set; }

        public static void Init()
        {
            _LoadCoverThemes();
            _LoadCovers(CConfig.CoverTheme);
        }

        public static void Close()
        {
            _UnloadCovers();
            _CoverThemes.Clear();
        }

        /// <summary>
        ///     Returns an array of CoverThemes-Names
        /// </summary>
        public static string[] CoverThemes
        {
            get
            {
                var coverThemes = new List<string>();
                for (int i = 0; i < _CoverThemes.Count; i++)
                    coverThemes.Add(_CoverThemes[i].Name);

                return coverThemes.ToArray();
            }
        }

        /// <summary>
        ///     Returns the current cover theme index
        /// </summary>
        /// <returns></returns>
        public static int GetCoverThemeIndex()
        {
            for (int i = 0; i < _CoverThemes.Count; i++)
            {
                if (_CoverThemes[i].Name == CConfig.CoverTheme)
                    return i;
            }
            return -1;
        }

        /// <summary>
        ///     Returns a STexture for a given cover name. Returns "NoCover" if the cover does not exist.
        /// </summary>
        public static CTexture Cover(string name)
        {
            lock (_Covers)
            {
                if (!_CoverExists(name))
                    return NoCover;

                return _Covers[name];
            }
        }

        /// <summary>
        ///     Returns true if a cover with the given name exists.
        ///     MUST HOLD _Covers lock at this point
        /// </summary>
        private static bool _CoverExists(string name)
        {
            return _Covers.ContainsKey(name);
        }

        /// <summary>
        ///     Reloads cover to use a new cover-theme
        /// </summary>
        public static void ReloadCovers()
        {
            _UnloadCovers();
            _LoadCovers(CConfig.CoverTheme);
        }

        private static void _UnloadCovers()
        {
            lock (_Covers)
            {
                foreach (string key in _Covers.Keys)
                {
                    CTexture texture = _Covers[key];
                    CDraw.RemoveTexture(ref texture);
                }
                _Covers.Clear();
            }
        }

        /// <summary>
        ///     Returns a SCoverTheme by cover-theme-name
        /// </summary>
        private static SCoverTheme _CoverTheme(string coverThemeName)
        {
            for (int i = 0; i < _CoverThemes.Count; i++)
            {
                if (_CoverThemes[i].Name == coverThemeName)
                    return _CoverThemes[i];
            }
            return new SCoverTheme();
        }

        /// <summary>
        ///     Loads all cover-themes to list.
        /// </summary>
        private static void _LoadCoverThemes()
        {
            _CoverThemes.Clear();

            string path = Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameCover);
            List<string> files = CHelper.ListFiles(path, "*.xml");

            foreach (string file in files)
            {
                CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(path, file));

                if (xmlReader != null)
                {
                    var coverTheme = new SCoverTheme();

                    xmlReader.GetValue("//root/Info/Name", out coverTheme.Name, String.Empty);
                    xmlReader.GetValue("//root/Info/Folder", out coverTheme.Folder, String.Empty);

                    if (coverTheme.Folder != "" && coverTheme.Name != "")
                    {
                        coverTheme.File = file;

                        _CoverThemes.Add(coverTheme);
                    }
                }
            }
        }

        /// <summary>
        ///     Loads all cover which are defined in the cover config file.
        /// </summary>
        private static void _LoadCovers(string coverThemeName)
        {
            SCoverTheme coverTheme = _CoverTheme(coverThemeName);

            if (String.IsNullOrEmpty(coverTheme.Name))
                return;

            string coverPath = Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameCover, coverTheme.Folder);
            List<string> files = CHelper.ListImageFiles(coverPath, true, true);

            lock (_Covers)
            {
                foreach (string file in files)
                    _AddCover(Path.GetFileNameWithoutExtension(file), file);
                if (_CoverExists("No Cover"))
                    NoCover = _Covers["No Cover"];
            }
        }

        /// <summary>
        ///     Ads a cover with the given name if it does not exist yet
        /// </summary>
        /// <param name="name">Name of the cover</param>
        /// <param name="file">Filename of image file</param>
        private static void _AddCover(string name, string file)
        {
            lock (_Covers)
            {
                if (!_CoverExists(name))
                    _Covers.Add(name, CDraw.AddTexture(file));
            }
        }
    }
}