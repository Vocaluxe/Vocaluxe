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
using System.Linq;
using VocaluxeLib;
using VocaluxeLib.Draw;
using System.Diagnostics;

namespace Vocaluxe.Base.ThemeSystem
{
    static class CThemes
    {
        private static readonly List<CTheme> _Themes = new List<CTheme>();
        public static string[] ThemeNames
        {
            get { return _Themes.Where(th => th.PartyModeID == -1).Select(th => th.Name).ToArray(); }
        }

        public static string[] SkinNames
        {
            get { return _GetTheme(-1).SkinNames; }
        }

        public static bool Init()
        {
            return ReadThemesFromFolder(Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameThemes), -1);
        }

        public static void Close()
        {
            Unload();
            _Themes.Clear();
        }

        public static void Unload()
        {
            foreach (CTheme theme in _Themes)
                theme.Unload();
        }

        /// <summary>
        ///     Loads the currently selected theme trying all others if current theme failed loading<br />
        ///     Therefore CConfig.Theme/Skin might be changed!<br />
        ///     Closes the program on failure!
        /// </summary>
        public static void Load()
        {
            CTheme theme = _GetTheme(-1) ?? _Themes.FirstOrDefault(th => th.PartyModeID == -1);
            while (theme != null)
            {
                if (theme.Load())
                    break;
                theme.Unload();
                CLog.LogError("Failed to load theme " + theme + "! Removing...", true);
                _Themes.Remove(theme);
                theme = _Themes.FirstOrDefault(th => th.PartyModeID == -1);
            }
            if (theme == null)
                CLog.LogError("No themes found! Cannot continue!", true, true);
            else
            {
                CConfig.Theme = theme.Name;
                CConfig.Skin = theme.CurrentSkin.Name;
                int[] ids = _Themes.Select(th => th.PartyModeID).Distinct().ToArray();
                foreach (int id in ids.Where(id => id >= 0))
                    LoadPartymodeTheme(id);
            }
        }

        public static bool LoadPartymodeTheme(int partyModeID)
        {
            CTheme theme = _GetTheme(partyModeID);
            if (theme.Load())
                return true;
            theme.Unload();
            if (theme.Name != CSettings.DefaultName)
            {
                CLog.LogError("Failed to load theme " + theme + " for partymode! Removing...", true);
                _Themes.Remove(theme);
                theme = _GetTheme(partyModeID);
                if (theme.Load())
                    return true;
            }
            CLog.LogError("Failed to load default theme for partymode! Unloading partymode!", true);
            foreach (CTheme th in _Themes.Where(th => th.PartyModeID == partyModeID))
                th.Unload();
            _Themes.RemoveAll(th => th.PartyModeID == partyModeID);
            return false;
        }

        public static void Reload()
        {
            Unload();
            Load();
        }

        public static void ReloadSkin()
        {
            _GetTheme(-1).ReloadSkin();
        }

        public static bool ReadThemesFromFolder(string path, int partyModeID)
        {
            Debug.Assert(_Themes.Count == 0);

            List<string> files = CHelper.ListFiles(path, "*.xml", false, true);

            List<CTheme> newThemes = new List<CTheme>();
            foreach (string file in files)
            {
                CTheme theme = new CTheme(file, partyModeID);
                if (theme.Init())
                    newThemes.Add(theme);
            }
            if (newThemes.Count == 0)
            {
                CLog.LogError("No valid themes found in " + path);
                return false;
            }
            if (partyModeID >= 0 && newThemes.Count(th => th.Name == CSettings.DefaultName) == 0)
            {
                CLog.LogError("Partymode misses default theme in " + path);
                return false;
            }

            _Themes.AddRange(newThemes);
            return true;
        }

        private static CTheme _GetTheme(int partyModeID)
        {
            foreach (CTheme theme in _Themes)
            {
                if (theme.PartyModeID == partyModeID && theme.Name == CConfig.Theme)
                    return theme;
            }
            if (partyModeID >= 0)
            {
                foreach (CTheme theme in _Themes)
                {
                    if (theme.PartyModeID == partyModeID && theme.Name == CSettings.DefaultName)
                        return theme;
                }
                Debug.Assert(false, "Partymode misses default theme, this should be checked during partymode loading!");
            }
            return null;
        }

        public static string GetThemeScreensPath(int partyModeID)
        {
            return _GetTheme(partyModeID).GetScreenPath();
        }

        private static void _LogMissingElement(CTheme theme, string elType, string elName)
        {
            CLog.LogError("Skin " + theme + ":" + theme.CurrentSkin + " is missing the " + elType + " \"" + elName + "\"! Expect visual problems!");
        }

        public static CTextureRef GetSkinTexture(string textureName, int partyModeID)
        {
            Debug.Assert(!String.IsNullOrEmpty(textureName));
            CTheme theme = _GetTheme(partyModeID);
            CTextureRef texture = theme.CurrentSkin.GetTexture(textureName);
            if (texture == null && partyModeID >= 0)
                texture = _GetTheme(-1).CurrentSkin.GetTexture(textureName);
            if (texture == null)
                _LogMissingElement(theme, "texture", textureName);
            return texture;
        }

        public static CVideoStream GetSkinVideo(string videoName, int partyModeID, bool loop = true)
        {
            Debug.Assert(!String.IsNullOrEmpty(videoName));
            CTheme theme = _GetTheme(partyModeID);
            CVideoStream video = theme.CurrentSkin.GetVideo(videoName, loop);
            if (video == null && partyModeID >= 0)
                video = _GetTheme(-1).CurrentSkin.GetVideo(videoName, loop);
            if (video == null)
                _LogMissingElement(theme, "video", videoName);
            return video;
        }

        public static SThemeCursor GetCursorTheme()
        {
            return _GetTheme(-1).CursorTheme;
        }

        public static bool GetColor(string colorName, int partyModeID, out SColorF color)
        {
            Debug.Assert(!String.IsNullOrEmpty(colorName));
            CTheme theme = _GetTheme(partyModeID);
            if (!theme.CurrentSkin.GetColor(colorName, out color) && (partyModeID == -1 || !_GetTheme(-1).CurrentSkin.GetColor(colorName, out color)))
            {
                _LogMissingElement(theme, "color", colorName);
                return false;
            }
            return true;
        }

        public static SColorF GetPlayerColor(int playerNr)
        {
            SColorF color;
            if (!_GetTheme(-1).CurrentSkin.GetPlayerColor(playerNr, out color))
                CLog.LogError("Invalid color requested: Color for player " + playerNr + ". Expect visual problems!", true);
            return color;
        }
    }
}