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
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Base
{
    struct SCover
    {
        public string Name;
        public CTexture Texture;
    }

    struct SCoverTheme
    {
        public string Name;
        public string Folder;
        public string File;
    }

    abstract class CCover
    {
        private static readonly XmlWriterSettings _Settings = new XmlWriterSettings();
        private static readonly List<SCover> _Cover = new List<SCover>();
        private static readonly List<SCoverTheme> _CoverThemes = new List<SCoverTheme>();

        private static readonly Object _MutexCover = new Object();

        public static CTexture NoCover { get; private set; }

        public static void Init()
        {
            _Settings.Indent = true;
            _Settings.Encoding = Encoding.UTF8;
            _Settings.ConformanceLevel = ConformanceLevel.Document;

            _LoadCoverThemes();
            _LoadCover(CConfig.CoverTheme);
        }

        /// <summary>
        ///     Returns an array of CoverThemes-Names
        /// </summary>
        public static string[] CoverThemes
        {
            get
            {
                List<string> coverThemes = new List<string>();
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
            lock (_MutexCover)
            {
                foreach (SCover cover in _Cover)
                {
                    if (cover.Name == name)
                        return cover.Texture;
                }
            }
            return NoCover;
        }

        /// <summary>
        ///     Returns true if a cover with the given name exists.
        /// </summary>
        private static bool _CoverExists(string name)
        {
            lock (_MutexCover)
            {
                return _Cover.Any(cover => cover.Name == name);
            }
        }

        /// <summary>
        ///     Reloads cover to use a new cover-theme
        /// </summary>
        public static void ReloadCover()
        {
            foreach (SCover cover in _Cover)
            {
                CTexture texture = cover.Texture;
                CDraw.RemoveTexture(ref texture);
            }
            _Cover.Clear();
            _LoadCover(CConfig.CoverTheme);
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

            const string path = CSettings.FolderCover;
            List<string> files = CHelper.ListFiles(path, "*.xml");

            foreach (string file in files)
            {
                CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(path, file));

                if (xmlReader != null)
                {
                    SCoverTheme coverTheme = new SCoverTheme();

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
        private static void _LoadCover(string coverThemeName)
        {
            SCoverTheme coverTheme = _CoverTheme(coverThemeName);

            if (String.IsNullOrEmpty(coverTheme.Name))
                return;
            CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(CSettings.FolderCover, coverTheme.File));

            if (xmlReader != null)
            {
                lock (_MutexCover)
                {
                    _Cover.Clear();
                    List<string> cover = xmlReader.GetValues("Cover");
                    foreach (string coverName in cover)
                    {
                        string name;
                        string filePath;
                        xmlReader.GetValue("//root/Cover/" + coverName + "/Name", out name, String.Empty);
                        xmlReader.GetValue("//root/Cover/" + coverName + "/Path", out filePath, String.Empty);
                        string coverFilePath = Path.Combine(CSettings.FolderCover, Path.Combine(coverTheme.Folder, filePath));
                        if (!File.Exists(coverFilePath))
                            continue;
                        SCover sk = new SCover {Name = name, Texture = CDraw.AddTexture(coverFilePath)};

                        _Cover.Add(sk);

                        if (sk.Name == "NoCover")
                            NoCover = sk.Texture;
                    }
                }
            }

            List<string> files = new List<string>();

            string coverPath = Path.Combine(CSettings.FolderCover, coverTheme.Folder);
            files.AddRange(CHelper.ListFiles(coverPath, "*.png", true, true));
            files.AddRange(CHelper.ListFiles(coverPath, "*.jpg", true, true));
            files.AddRange(CHelper.ListFiles(coverPath, "*.jpeg", true, true));
            files.AddRange(CHelper.ListFiles(coverPath, "*.bmp", true, true));


            foreach (string file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);

                if (_CoverExists(name))
                    continue;
                // ReSharper disable AssignNullToNotNullAttribute
                SCover sk = new SCover {Name = name, Texture = CDraw.AddTexture(file)};
                // ReSharper restore AssignNullToNotNullAttribute

                _Cover.Add(sk);
            }
        }
    }
}