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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;
using VocaluxeLib.Xml;

namespace Vocaluxe.Base
{
    static class CCover
    {
        private const string _NoCoverName = "No Cover";
        private const string _NoCoverNameAlt = "NoCover";
        private static readonly Dictionary<string, CTextureRef> _Covers = new Dictionary<string, CTextureRef>();
        private static readonly Dictionary<ECoverGeneratorType, CCoverGenerator> _CoverGenerators = new Dictionary<ECoverGeneratorType, CCoverGenerator>();
        private static readonly List<SThemeCover> _CoverThemes = new List<SThemeCover>();
        private static readonly CancellationTokenSource _CancelToken = new CancellationTokenSource();

        public static CTextureRef NoCover { get; private set; }

        public static void Init()
        {
            _LoadCoverThemes();
            _LoadCovers(CConfig.Config.Theme.CoverTheme);
        }

        public static void Close()
        {
            _CancelToken.Cancel();
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
                if (_CoverThemes[i].Name == CConfig.Config.Theme.CoverTheme)
                    return i;
            }
            return -1;
        }

        /// <summary>
        ///     Returns a Texture reference for a given cover name. Returns "NoCover" if the cover does not exist.
        /// </summary>
        public static CTextureRef Cover(string name)
        {
            lock (_Covers)
            {
                if (!_CoverExists(name))
                    return NoCover;

                return _Covers[name];
            }
        }

        public static CTextureRef GenerateCover(string text, ECoverGeneratorType type, CSong firstSong)
        {
            CTextureRef texture = CDraw.CopyTexture(NoCover);
            Task.Factory.StartNew(() =>
                {
                    _CancelToken.Token.ThrowIfCancellationRequested();
                    Bitmap coverBmp = !_CoverGenerators.ContainsKey(type)
                                          ? null : _CoverGenerators[type].GetCover(text, firstSong != null ? Path.Combine(firstSong.Folder, firstSong.CoverFileName) : null);
                    _CancelToken.Token.ThrowIfCancellationRequested();
                    if (coverBmp == null && _CoverGenerators.ContainsKey(ECoverGeneratorType.Default))
                        coverBmp = _CoverGenerators[ECoverGeneratorType.Default].GetCover(text, firstSong != null ? Path.Combine(firstSong.Folder, firstSong.CoverFileName) : null);
                    _CancelToken.Token.ThrowIfCancellationRequested();
                    if (coverBmp != null)
                        CDraw.EnqueueTextureUpdate(texture, coverBmp);
                    _CancelToken.Token.ThrowIfCancellationRequested();
                }, _CancelToken.Token);
            lock (_Covers)
            {
                _Covers.Add(text, texture);
            }
            return texture;
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
            _LoadCovers(CConfig.Config.Theme.CoverTheme);
        }

        private static void _UnloadCovers()
        {
            lock (_Covers)
            {
                foreach (string key in _Covers.Keys)
                {
                    CTextureRef texture = _Covers[key];
                    CDraw.RemoveTexture(ref texture);
                }
                _Covers.Clear();
            }
            lock (_CoverGenerators)
            {
                _CoverGenerators.Clear();
            }
        }

        /// <summary>
        ///     Returns a SCoverTheme by cover-theme-name
        /// </summary>
        private static SThemeCover _CoverTheme(string coverThemeName)
        {
            for (int i = 0; i < _CoverThemes.Count; i++)
            {
                if (_CoverThemes[i].Name == coverThemeName)
                    return _CoverThemes[i];
            }
            return new SThemeCover();
        }

        private static void Foo(SThemeScreen a, SThemeScreen b)
        {
            a = b;
        }

        private static void _TestNewLoad(string path)
        {
            SThemeCover themeCover;
            var xml = new CXmlSerializer();
            themeCover = xml.Deserialize<SThemeCover>(path);
            string cover = themeCover.Info.Author;
            SThemeScreen theme = new SThemeScreen();
            SThemeScreen theme2 = new SThemeScreen();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < 1; i++)
                theme = xml.Deserialize<SThemeScreen>(Path.Combine(CBase.Themes.GetThemeScreensPath(-1), "ScreenMain.xml"));
            watch.Stop();
            xml.Serialize(Path.Combine(CBase.Themes.GetThemeScreensPath(-1), "ScreenMain2.xml"), theme);
            return;
            Stopwatch watch2 = new Stopwatch();
            watch2.Start();
            for (int i = 0; i < 100; i++)
            {
                using (TextReader textReader = new StreamReader(Path.Combine(CBase.Themes.GetThemeScreensPath(-1), "ScreenMain.xml")))
                {
                    XmlSerializer deserializer = new XmlSerializer(typeof(STheme));

                    theme2 = (SThemeScreen)deserializer.Deserialize(textReader);
                }
            }
            watch2.Stop();
            Foo(theme, theme2);
            MessageBox.Show(watch.ElapsedMilliseconds + "ms/5000=" + watch.ElapsedMilliseconds / 1000 + "ms vs " + watch2.ElapsedMilliseconds / 100);
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
                _TestNewLoad(Path.Combine(path, file));
                CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(path, file));

                if (xmlReader != null)
                {
                    var coverTheme = new SThemeCover();

                    xmlReader.GetValue("//root/Info/Name", out coverTheme.Name, String.Empty);
                    xmlReader.GetValue("//root/Info/Folder", out coverTheme.Folder, String.Empty);

                    if (coverTheme.Folder != "" && coverTheme.Name != "")
                    {
                        coverTheme.FilePath = xmlReader.FilePath;

                        _CoverThemes.Add(coverTheme);
                    }
                }
            }
        }

        /// <summary>
        ///     Loads all covers from the theme
        /// </summary>
        private static void _LoadCovers(string coverThemeName)
        {
            SThemeCover coverTheme = _CoverTheme(coverThemeName);

            if (String.IsNullOrEmpty(coverTheme.Name))
                return;

            string coverPath = Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameCover, coverTheme.Folder);
            List<string> files = CHelper.ListImageFiles(coverPath, true, true);

            lock (_Covers)
            {
                foreach (string file in files)
                    _AddCover(Path.GetFileNameWithoutExtension(file), file);
                if (_CoverExists(_NoCoverName))
                    NoCover = _Covers[_NoCoverName];
                else if (_CoverExists(_NoCoverNameAlt))
                    NoCover = _Covers[_NoCoverNameAlt];
                else
                    CBase.Log.LogError("Covertheme \"" + coverThemeName + "\" does not include a cover file named \"" + _NoCoverName + "\" and cannot be used!", true, true);
            }
            _LoadCoverGenerators(coverTheme);
        }

        public static ECoverGeneratorType _SongSortingToType(ESongSorting sorting)
        {
            switch (sorting)
            {
                case ESongSorting.TR_CONFIG_NONE:
                    return ECoverGeneratorType.Default;
                case ESongSorting.TR_CONFIG_FOLDER:
                    return ECoverGeneratorType.Folder;
                case ESongSorting.TR_CONFIG_ARTIST:
                    return ECoverGeneratorType.Artist;
                case ESongSorting.TR_CONFIG_ARTIST_LETTER:
                case ESongSorting.TR_CONFIG_TITLE_LETTER:
                    return ECoverGeneratorType.Letter;
                case ESongSorting.TR_CONFIG_EDITION:
                    return ECoverGeneratorType.Edition;
                case ESongSorting.TR_CONFIG_GENRE:
                    return ECoverGeneratorType.Genre;
                case ESongSorting.TR_CONFIG_LANGUAGE:
                    return ECoverGeneratorType.Language;
                case ESongSorting.TR_CONFIG_YEAR:
                    return ECoverGeneratorType.Year;
                case ESongSorting.TR_CONFIG_DECADE:
                    return ECoverGeneratorType.Decade;
                case ESongSorting.TR_CONFIG_DATEADDED:
                    return ECoverGeneratorType.Date;
                default:
                    throw new ArgumentOutOfRangeException("sorting");
            }
        }

        private static void _LoadCoverGenerators(SThemeCover coverTheme)
        {
            CXMLReader xmlReader = CXMLReader.OpenFile(coverTheme.FilePath);
            string coverPath = Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameCover, coverTheme.Folder);
            lock (_CoverGenerators)
            {
                int i = 1;
                while (xmlReader.ItemExists("//root/CoverGenerator" + i))
                {
                    SThemeCoverGenerator theme;
                    if (xmlReader.Read("//root/CoverGenerator" + i, out theme))
                    {
                        if (_CoverGenerators.ContainsKey(theme.Type))
                            continue;
                        CCoverGenerator el = new CCoverGenerator(theme, coverPath);
                        _CoverGenerators.Add(theme.Type, el);
                    }
                    i++;
                }
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