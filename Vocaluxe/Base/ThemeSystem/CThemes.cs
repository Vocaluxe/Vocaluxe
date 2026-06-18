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
using System.IO;
using System.Linq;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Log;

namespace Vocaluxe.Base.ThemeSystem
{
    static class CThemes
    {
        private static readonly List<CTheme> _Themes = new List<CTheme>();
        public static string[] ThemeNames
        {
            get { return _Themes.Where(th => th is CBaseTheme).Select(th => th.Name).Distinct().ToArray(); }
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
            foreach (var theme in _Themes)
            {
                theme.Unload();
            }

            CurrentThemes.Clear();
        }

        /// <summary>
        ///     Loads the currently selected theme trying all others if current theme failed loading<br />
        ///     Therefore CConfig.Theme/Skin might be changed!<br />
        ///     Closes the program on failure!
        /// </summary>
        public static void Load()
        {
            var theme = _Themes.FirstOrDefault(th => th is CBaseTheme && th.Name == CConfig.Config.Theme.Theme) ?? _Themes.FirstOrDefault(th => th is CBaseTheme);
            while (theme != null)
            {
                if (theme.Load())
                {
                    break;
                }

                theme.Unload();
                CLog.Error("Failed to load theme {ThemeName}! Removing...", CLog.Params(theme.Name, theme), true);
                _Themes.Remove(theme);
                theme = _Themes.FirstOrDefault(th => th is CBaseTheme);
            }

            CurrentThemes.Add(-1, theme);
            if (theme == null)
            {
                CLog.Fatal("No themes found! Cannot continue!");
            }
            else
            {
                CConfig.Config.Theme.Theme = theme.Name;
                CConfig.Config.Theme.Skin = theme.CurrentSkin.Name;
                var ids = _Themes.Select(th => th.PartyModeId).Distinct().ToArray();
                foreach (var id in ids.Where(id => id >= 0))
                {
                    LoadPartymodeTheme(id);
                }
            }
        }

        public static bool LoadPartymodeTheme(int partyModeId)
        {
            Debug.Assert(partyModeId >= 0);
            var theme = _Themes.FirstOrDefault(th => th.PartyModeId == partyModeId && th.Name == CConfig.Config.Theme.Theme);
            if (theme != null)
            {
                if (theme.Load())
                {
                    CurrentThemes.Add(partyModeId, theme);
                    return true;
                }

                theme.Unload();
                CLog.Error("Failed to load theme " + theme + " for partymode! Removing...", true);
                _Themes.Remove(theme);
            }

            theme = _Themes.First(th => th.PartyModeId == partyModeId && th.Name == CSettings.DefaultName);
            if (theme.Load())
            {
                CurrentThemes.Add(partyModeId, theme);
                return true;
            }

            CLog.Error("Failed to load default theme for partymode! Unloading partymode!", true);
            foreach (CPartyTheme th in _Themes.Where(th => th.PartyModeId == partyModeId))
            {
                th.Unload();
            }

            _Themes.RemoveAll(th => th.PartyModeId == partyModeId);
            return false;
        }

        public static void Reload()
        {
            Unload();
            Load();
        }

        public static void ReloadSkin()
        {
            foreach (var theme in CurrentThemes.Values)
            {
                theme.ReloadSkin();
            }
        }

        public static bool ReadThemesFromFolder(string path, int partyModeId)
        {
            var files = CHelper.ListFiles(path, "*.xml", false, true);

            var newThemes = new List<CTheme>();
            foreach (var file in files)
            {
                CTheme theme;
                if (partyModeId < 0)
                {
                    theme = new CBaseTheme(file);
                }
                else
                {
                    theme = new CPartyTheme(file, partyModeId);
                }

                if (theme.Init())
                {
                    newThemes.Add(theme);
                }
            }

            if (newThemes.Count == 0)
            {
                CLog.Error("No valid themes found in " + path);
                return false;
            }

            if (partyModeId >= 0 && newThemes.Count(th => th.Name == CSettings.DefaultName) == 0)
            {
                CLog.Error("Partymode misses default theme in " + path);
                return false;
            }

            _Themes.AddRange(newThemes);
            return true;
        }

        public static string GetThemeScreensPath(int partyModeId)
        {
            CTheme theme;
            return CurrentThemes.TryGetValue(partyModeId, out theme) ? theme.GetScreenPath() : null;
        }

        private static void _LogMissingElement(int partyModeId, string elType, string elName)
        {
            CLog.Error("Skin " + CurrentThemes[partyModeId].CurrentSkin + " is missing the " + elType + " \"" + elName + "\"! Expect visual problems!");
        }

        public static CTextureRef GetSkinTexture(string textureName, int partyModeId)
        {
            if (string.IsNullOrEmpty(textureName))
            {
                return null;
            }

            var texture = CurrentThemes[partyModeId].CurrentSkin.GetTexture(textureName);
            if (texture == null)
            {
                _LogMissingElement(partyModeId, "texture", textureName);
            }

            return texture;
        }

        public static CVideoStream GetSkinVideo(string videoName, int partyModeId, bool loop = true)
        {
            Debug.Assert(!string.IsNullOrEmpty(videoName));
            var video = CurrentThemes[partyModeId].CurrentSkin.GetVideo(videoName, loop);
            if (video == null)
            {
                _LogMissingElement(partyModeId, "video", videoName);
            }

            return video;
        }

        public static SThemeCursor GetCursorTheme()
        {
            return ((CBaseTheme)CurrentThemes[-1]).CursorTheme;
        }

        public static bool GetColor(string colorName, int partyModeId, out SColorF color)
        {
            Debug.Assert(!string.IsNullOrEmpty(colorName));
            if (!CurrentThemes[partyModeId].CurrentSkin.GetColor(colorName, out color))
            {
                _LogMissingElement(partyModeId, "color", colorName);
                return false;
            }

            return true;
        }

        public static SColorF GetPlayerColor(int playerNr)
        {
            SColorF color;
            if (!GetColor("Player" + playerNr, -1, out color))
            {
                CLog.Error("Invalid color requested: Color for player " + playerNr + ". Expect visual problems!", true);
            }

            return color;
        }
    }
}