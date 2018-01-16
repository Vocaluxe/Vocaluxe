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
using VocaluxeLib.Log;

namespace Vocaluxe.Base.ThemeSystem
{
    static class CThemes
    {
        private static readonly List<CTheme> _Themes = new List<CTheme>();
        public static string[] ThemeNames
        {
            get { return _Themes.Where(th=>th is CBaseTheme).Select(th => th.Name).Distinct().ToArray(); }
        }

        public static string[] SkinNames
        {
            get { return CurrentThemes[-1].SkinNames; }
        }

        public static bool Init()
        {
            return CSkin.InitRequiredElements() && ReadThemesFromFolder(Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameThemes), -1);
        }

        /// <summary>
        ///     Currently loaded themes. Only for internal use methods of this static class for getting values from the themes.
        /// </summary>
        public static readonly Dictionary<int, CTheme> CurrentThemes = new Dictionary<int, CTheme>();

        public static void Close()
        {
            Unload();
            _Themes.Clear();
            CSkin.Close();
        }

        public static void Unload()
        {
            foreach (CTheme theme in _Themes)
                theme.Unload();
            CurrentThemes.Clear();
        }

        /// <summary>
        ///     Loads the currently selected theme trying all others if current theme failed loading<br />
        ///     Therefore CConfig.Theme/Skin might be changed!<br />
        ///     Closes the program on failure!
        /// </summary>
        public static void Load()
        {
            CTheme theme = _Themes.FirstOrDefault(th => th is CBaseTheme && th.Name == CConfig.Config.Theme.Theme) ?? _Themes.FirstOrDefault(th => th is CBaseTheme);
            while (theme != null)
            {
                if (theme.Load())
                    break;
                theme.Unload();
                CLog.Error("Failed to load theme {ThemeName}! Removing...", CLog.Params(theme.Name, theme), true);
                _Themes.Remove(theme);
                theme = _Themes.FirstOrDefault(th => th is CBaseTheme);
            }
            CurrentThemes.Add(-1, theme);
            if (theme == null)
                CLog.Fatal("No themes found! Cannot continue!");
            else
            {
                CConfig.Config.Theme.Theme = theme.Name;
                CConfig.Config.Theme.Skin = theme.CurrentSkin.Name;
                int[] ids = _Themes.Select(th => th.PartyModeID).Distinct().ToArray();
                foreach (int id in ids.Where(id => id >= 0))
                    LoadPartymodeTheme(id);
            }
        }

        public static bool LoadPartymodeTheme(int partyModeID)
        {
            Debug.Assert(partyModeID >= 0);
            CTheme theme = _Themes.FirstOrDefault(th => th.PartyModeID == partyModeID && th.Name == CConfig.Config.Theme.Theme);
            if (theme != null)
            {
                if (theme.Load())
                {
                    CurrentThemes.Add(partyModeID, theme);
                    return true;
                }
                theme.Unload();
                CLog.Error("Failed to load theme " + theme + " for partymode! Removing...", true);
                _Themes.Remove(theme);
            }
            theme = _Themes.First(th => th.PartyModeID == partyModeID && th.Name == CSettings.DefaultName);
            if (theme.Load())
            {
                CurrentThemes.Add(partyModeID, theme);
                return true;
            }
            CLog.Error("Failed to load default theme for partymode! Unloading partymode!", true);
            foreach (CPartyTheme th in _Themes.Where(th => th.PartyModeID == partyModeID))
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
            foreach (CTheme theme in CurrentThemes.Values)
                theme.ReloadSkin();
        }

        public static bool ReadThemesFromFolder(string path, int partyModeID)
        {
            List<string> files = CHelper.ListFiles(path, "*.xml", false, true);

            List<CTheme> newThemes = new List<CTheme>();
            foreach (string file in files)
            {
                CTheme theme;
                if (partyModeID < 0)
                    theme = new CBaseTheme(file);
                else
                    theme = new CPartyTheme(file, partyModeID);
                if (theme.Init())
                    newThemes.Add(theme);
            }
            if (newThemes.Count == 0)
            {
                CLog.Error("No valid themes found in " + path);
                return false;
            }
            if (partyModeID >= 0 && newThemes.Count(th => th.Name == CSettings.DefaultName) == 0)
            {
                CLog.Error("Partymode misses default theme in " + path);
                return false;
            }

            _Themes.AddRange(newThemes);
            return true;
        }

        public static string GetThemeScreensPath(int partyModeID)
        {
            CTheme theme;
            return CurrentThemes.TryGetValue(partyModeID, out theme) ? theme.GetScreenPath() : null;
        }

        private static void _LogMissingElement(int partyModeID, string elType, string elName)
        {
            CLog.Error("Skin " + CurrentThemes[partyModeID].CurrentSkin + " is missing the " + elType + " \"" + elName + "\"! Expect visual problems!");
        }

        public static CTextureRef GetSkinTexture(string textureName, int partyModeID)
        {
            if (String.IsNullOrEmpty(textureName))
                return null;
            CTextureRef texture = CurrentThemes[partyModeID].CurrentSkin.GetTexture(textureName);
            if (texture == null)
                _LogMissingElement(partyModeID, "texture", textureName);
            return texture;
        }

        public static CVideoStream GetSkinVideo(string videoName, int partyModeID, bool loop = true)
        {
            Debug.Assert(!String.IsNullOrEmpty(videoName));
            CVideoStream video = CurrentThemes[partyModeID].CurrentSkin.GetVideo(videoName, loop);
            if (video == null)
                _LogMissingElement(partyModeID, "video", videoName);
            return video;
        }

        public static SThemeCursor GetCursorTheme()
        {
            return ((CBaseTheme)CurrentThemes[-1]).CursorTheme;
        }

        public static bool GetColor(string colorName, int partyModeID, out SColorF color)
        {
            Debug.Assert(!String.IsNullOrEmpty(colorName));
            if (!CurrentThemes[partyModeID].CurrentSkin.GetColor(colorName, out color))
            {
                _LogMissingElement(partyModeID, "color", colorName);
                return false;
            }
            return true;
        }

        public static SColorF GetPlayerColor(int playerNr)
        {
            SColorF color;
            if (!GetColor("Player" + playerNr, -1, out color))
                CLog.Error("Invalid color requested: Color for player " + playerNr + ". Expect visual problems!", true);
            return color;
        }
    }
}