﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using VocaluxeLib.Menu;

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
        private static readonly XmlWriterSettings _Settings = new XmlWriterSettings();
        private static readonly List<SCover> _Cover = new List<SCover>();
        private static readonly List<SCoverTheme> _CoverThemes = new List<SCoverTheme>();
        private static STexture _NoCover = new STexture(-1);

        private static readonly Object _MutexCover = new Object();

        public static STexture NoCover
        {
            get { return _NoCover; }
        }

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
        public static STexture Cover(string name)
        {
            STexture cov = _NoCover;
            lock (_MutexCover)
            {
                foreach (SCover cover in _Cover)
                {
                    if (cover.Name == name)
                    {
                        cov = cover.Texture;
                        break;
                    }
                }
            }
            return cov;
        }

        /// <summary>
        ///     Returns true if a cover with the given name exists.
        /// </summary>
        public static bool CoverExists(string name)
        {
            STexture cov = _NoCover;
            lock (_MutexCover)
            {
                foreach (SCover cover in _Cover)
                {
                    if (cover.Name == name)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Reloads cover to use a new cover-theme
        /// </summary>
        public static void ReloadCover()
        {
            foreach (SCover cover in _Cover)
            {
                STexture texture = cover.Texture;
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
        ///     Loads all cover-themes to list.
        /// </summary>
        private static void _LoadCoverThemes()
        {
            _CoverThemes.Clear();

            string path = CSettings.FolderCover;
            List<string> files = CHelper.ListFiles(path, "*.xml", false);

            foreach (string file in files)
            {
                CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(path, file));

                if (xmlReader != null)
                {
                    SCoverTheme coverTheme = new SCoverTheme();

                    xmlReader.GetValue("//root/Info/Name", ref coverTheme.Name, String.Empty);
                    xmlReader.GetValue("//root/Info/Folder", ref coverTheme.Folder, String.Empty);

                    if (coverTheme.Folder.Length > 0 && coverTheme.Name.Length > 0)
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
            SCoverTheme coverTheme = new SCoverTheme();

            coverTheme = _CoverTheme(coverThemeName);

            if (coverTheme.Name.Length > 0)
            {
                CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(CSettings.FolderCover, coverTheme.File));

                if (xmlReader != null)
                {
                    lock (_MutexCover)
                    {
                        _Cover.Clear();
                        List<string> cover = xmlReader.GetValues("Cover");
                        for (int i = 0; i < cover.Count; i++)
                        {
                            SCover sk = new SCover();
                            string name = String.Empty;
                            string value = String.Empty;
                            xmlReader.GetValue("//root/Cover/" + cover[i] + "/Name", ref name, String.Empty);
                            xmlReader.GetValue("//root/Cover/" + cover[i] + "/Path", ref value, String.Empty);
                            sk.Name = name;
                            sk.Value = Path.Combine(coverTheme.Folder, value);
                            if (File.Exists(Path.Combine(CSettings.FolderCover, Path.Combine(coverTheme.Folder, value))))
                                sk.Texture = CDraw.AddTexture(Path.Combine(CSettings.FolderCover, Path.Combine(coverTheme.Folder, value)));
                            else
                                sk.Texture = _NoCover;

                            _Cover.Add(sk);

                            if (sk.Name == "NoCover")
                                _NoCover = sk.Texture;
                        }
                    }
                }

                List<string> files = new List<string>();

                files.AddRange(CHelper.ListFiles(Path.Combine(CSettings.FolderCover, coverTheme.Folder), "*.png", true, true));
                files.AddRange(CHelper.ListFiles(Path.Combine(CSettings.FolderCover, coverTheme.Folder), "*.jpg", true, true));
                files.AddRange(CHelper.ListFiles(Path.Combine(CSettings.FolderCover, coverTheme.Folder), "*.jpeg", true, true));
                files.AddRange(CHelper.ListFiles(Path.Combine(CSettings.FolderCover, coverTheme.Folder), "*.bmp", true, true));


                foreach (string file in files)
                {
                    string name = Path.GetFileNameWithoutExtension(file);

                    if (!CoverExists(name))
                    {
                        SCover sk = new SCover();

                        string value = String.Empty;

                        sk.Name = name;
                        sk.Value = Path.Combine(coverTheme.Folder, Path.GetFileName(file));

                        sk.Texture = CDraw.AddTexture(Path.Combine(CSettings.FolderCover, sk.Value));

                        _Cover.Add(sk);
                    }
                }
            }
        }
    }
}