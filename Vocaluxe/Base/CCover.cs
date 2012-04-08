﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using Vocaluxe.Lib.Draw;

namespace Vocaluxe.Base
{
    struct SCover
    {
        public string Name;
        public string Value;
        public STexture Texture;
    }

    struct SCoverTheme
    {
        public string Name;
        public string Folder;
        public string File;
    }

    abstract class CCover
    {
        private static XmlWriterSettings _settings = new XmlWriterSettings();
        private static List<SCover> _Cover = new List<SCover>();
        private static List<SCoverTheme> _CoverThemes = new List<SCoverTheme>();
        private static STexture _NoCover = new STexture(-1);

        private static Object _MutexCover = new Object();

        public static STexture NoCover
        {
            get { return _NoCover; }
        }

        public static void Init()
        {
            _settings.Indent = true;
            _settings.Encoding = Encoding.UTF8;
            _settings.ConformanceLevel = ConformanceLevel.Document;

            LoadCoverThemes();
            LoadCover(CConfig.CoverTheme);
        }

        /// <summary>
        /// Returns an array of CoverThemes-Names
        /// </summary>
        public static string[] CoverThemes
        {
            get
            {
                List<string> CoverThemes = new List<string>();
                for (int i = 0; i < _CoverThemes.Count; i++)
                {
                    CoverThemes.Add(_CoverThemes[i].Name);
                }

                return CoverThemes.ToArray();
            }
        }

        /// <summary>
        /// Returns the current cover theme index
        /// </summary>
        /// <returns></returns>
        public static int GetCoverThemeIndex()
        {
            for (int i = 0; i < _CoverThemes.Count; i++)
            {
                if (_CoverThemes[i].Name == CConfig.CoverTheme)
                {
                    return i;
                }
            }
            return -1;
        }

		/// <summary>
		/// Returns a STexture for a given cover name. Returns "NoCover" if the cover does not exist.
		/// </summary>
        public static STexture Cover(string Name)
        {
            STexture cov = _NoCover;
            lock (_MutexCover)
            {
                foreach (SCover cover in _Cover)
                {
                    if (cover.Name == Name)
                    {
                        cov = cover.Texture;
                        break;
                    }
                }
            }
            return cov;
        }

        /// <summary>
        /// Returns true if a cover with the given name exists.
        /// </summary>
        public static bool CoverExists(string Name)
        {
            STexture cov = _NoCover;
            lock (_MutexCover)
            {
                foreach (SCover cover in _Cover)
                {
                    if (cover.Name == Name)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Reloads cover to use a new cover-theme
        /// </summary>
        public static void ReloadCover()
        {
            foreach (SCover cover in _Cover)
            {
                STexture texture = cover.Texture;
                CDraw.RemoveTexture(ref texture);
            }
            _Cover.Clear();
            LoadCover(CConfig.CoverTheme);
        }

        /// <summary>
        /// Returns a SCoverTheme by cover-theme-name
        /// </summary>
        private static SCoverTheme CoverTheme(string coverThemeName)
        {
            SCoverTheme coverTheme = new SCoverTheme();
            for (int i = 0; i < _CoverThemes.Count; i++)
            {
                if (_CoverThemes[i].Name == coverThemeName)
                {
                    coverTheme = _CoverThemes[i];
                    break;
                }
            }
            return coverTheme;
        }

        /// <summary>
        /// Loads all cover-themes to list.
        /// </summary>
        private static void LoadCoverThemes()
        {
            _CoverThemes.Clear();

            CHelper Helper = new CHelper();
            string path = CSettings.sFolderCover;
            List<string> files = Helper.ListFiles(path, "*.xml", false);

            foreach (string file in files)
            {
                bool loaded = false;
                XPathDocument xPathDoc = null;
                XPathNavigator navigator = null;

                try
                {
                    xPathDoc = new XPathDocument(Path.Combine(path, file));
                    navigator = xPathDoc.CreateNavigator();
                    loaded = true;
                }
                catch (Exception)
                {
                    loaded = false;
                    if (navigator != null)
                        navigator = null;

                    if (xPathDoc != null)
                        xPathDoc = null;
                }

                if (loaded)
                {
                    SCoverTheme coverTheme = new SCoverTheme();

                    CHelper.GetValueFromXML("//root/Info/Name", navigator, ref coverTheme.Name, String.Empty);
                    CHelper.GetValueFromXML("//root/Info/Folder", navigator, ref coverTheme.Folder, String.Empty);

                    if (coverTheme.Folder != String.Empty && coverTheme.Name != String.Empty)
                    {
                        coverTheme.File = file;

                        _CoverThemes.Add(coverTheme);
                    }
                }
            }
        }
	
		/// <summary>
		/// Loads all cover which are defined in the cover config file.
		/// </summary>
        private static void LoadCover(string coverThemeName)
        {
            SCoverTheme coverTheme = new SCoverTheme();

            coverTheme = CoverTheme(coverThemeName);

            if (coverTheme.Name != String.Empty)
            {

                bool loaded = false;
                XPathDocument xPathDoc = null;
                XPathNavigator navigator = null;

                try
                {
                    xPathDoc = new XPathDocument(Path.Combine(CSettings.sFolderCover, coverTheme.File));
                    navigator = xPathDoc.CreateNavigator();
                    loaded = true;
                }
                catch (Exception)
                {
                    loaded = false;
                    if (navigator != null)
                        navigator = null;

                    if (xPathDoc != null)
                        xPathDoc = null;
                }

                if (loaded)
                {
                    lock (_MutexCover)
                    {
                        _Cover.Clear();
                        List<string> cover = CHelper.GetValuesFromXML("Cover", navigator);
                        for (int i = 0; i < cover.Count; i++)
                        {
                            SCover sk = new SCover();
                            string name = String.Empty;
                            string value = String.Empty;
                            CHelper.GetValueFromXML("//root/Cover/" + cover[i] + "/Name", navigator, ref name, String.Empty);
                            CHelper.GetValueFromXML("//root/Cover/" + cover[i] + "/Path", navigator, ref value, String.Empty);
                            sk.Name = name;
                            sk.Value = Path.Combine(coverTheme.Folder,value);
                            if (File.Exists(Path.Combine(CSettings.sFolderCover, Path.Combine(coverTheme.Folder, value))))
                            {
                                sk.Texture = CDraw.AddTexture(Path.Combine(CSettings.sFolderCover, Path.Combine(coverTheme.Folder, value)));
                            }
                            else
                            {
                                sk.Texture = _NoCover;
                            }

                            _Cover.Add(sk);

                            if (sk.Name == "NoCover")
                            {
                                _NoCover = sk.Texture;
                            }
                        }
                    }
                }

                CHelper Helper = new CHelper();
                List<string> files = new List<string>();

                files.AddRange(Helper.ListFiles(Path.Combine(CSettings.sFolderCover, coverTheme.Folder), "*.png", true, true));
                files.AddRange(Helper.ListFiles(Path.Combine(CSettings.sFolderCover, coverTheme.Folder), "*.jpg", true, true));
                files.AddRange(Helper.ListFiles(Path.Combine(CSettings.sFolderCover, coverTheme.Folder), "*.jpeg", true, true));
                files.AddRange(Helper.ListFiles(Path.Combine(CSettings.sFolderCover, coverTheme.Folder), "*.bmp", true, true));


                foreach (string file in files)
                {
                    string name = Path.GetFileNameWithoutExtension(file);

                    if (!CoverExists(name))
                    {
                        SCover sk = new SCover();

                        string value = String.Empty;

                        sk.Name = name;
                        sk.Value = Path.Combine(coverTheme.Folder,Path.GetFileName(file));

                        sk.Texture = CDraw.AddTexture(Path.Combine(CSettings.sFolderCover, sk.Value));

                        _Cover.Add(sk);
                    }
                }
            }
        }
    }
}
