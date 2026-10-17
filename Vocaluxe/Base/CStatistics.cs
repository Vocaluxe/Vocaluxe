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
using System.Linq;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Songs;

namespace Vocaluxe.Base
{
    public struct SStatisticsCategory
    {
        public string Name;
        public int NumSongs;
        public int NumSung;

        public int Coverage
        {
            get { return NumSongs == 0 ? 0 : (int)Math.Round(NumSung * 100.0 / NumSongs); }
        }
    }

    static class CStatistics
    {
        public static int NumSongs
        {
            get { return CSongs.AllSongs.Count; }
        }

        public static int NumSungSongs
        {
            get { return CSongs.AllSongs.Count(song => song.NumPlayed > 0); }
        }

        public static int NumPlayed
        {
            get { return CSongs.AllSongs.Sum(song => song.NumPlayed); }
        }

        public static int Coverage
        {
            get { return NumSongs == 0 ? 0 : (int)Math.Round(NumSungSongs * 100.0 / NumSongs); }
        }

        public static int NumPlayers
        {
            get { return CProfiles.GetProfiles().Count(profile => profile.Active == EOffOn.TR_CONFIG_ON && !CProfiles.IsGuestProfile(profile.Id)); }
        }

        public static List<CSong> GetMostSung(int count)
        {
            return CSongs.AllSongs.Where(song => song.NumPlayed > 0).OrderByDescending(song => song.NumPlayed).ThenBy(song => song.Artist).Take(count).ToList();
        }

        public static List<CSong> GetNeverSung(int count)
        {
            return CSongs.AllSongs.Where(song => song.NumPlayed == 0).OrderBy(song => CGame.Rand.Next()).Take(count).ToList();
        }

        public static List<SStatisticsCategory> GetGenres()
        {
            return _GetCategories(song => song.Genres);
        }

        public static List<SStatisticsCategory> GetLanguages()
        {
            return _GetCategories(song => song.Languages);
        }

        private static List<SStatisticsCategory> _GetCategories(Func<CSong, IEnumerable<string>> selector)
        {
            var categories = new Dictionary<string, SStatisticsCategory>(StringComparer.OrdinalIgnoreCase);
            foreach (var song in CSongs.AllSongs)
            {
                foreach (var name in selector(song).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    SStatisticsCategory category;
                    if (!categories.TryGetValue(name, out category))
                    {
                        category = new SStatisticsCategory { Name = name.Trim() };
                    }

                    category.NumSongs++;
                    if (song.NumPlayed > 0)
                    {
                        category.NumSung++;
                    }

                    categories[name] = category;
                }
            }

            return categories.Values.OrderByDescending(category => category.NumSongs).ThenBy(category => category.Name).ToList();
        }

        public static string FormatDate(DateTime date, bool full)
        {
            if (date.Date == DateTime.Today)
            {
                return CLanguage.Translate("TR_SCREENSTATISTICS_TODAY");
            }

            if (date.Date == DateTime.Today.AddDays(-1))
            {
                return CLanguage.Translate("TR_SCREENSTATISTICS_YESTERDAY");
            }

            return date.ToString(full ? "dd.MM.yyyy" : "dd.MM.");
        }

        public static CTextureRef GetAvatar(string playerName)
        {
            var profile = CProfiles.GetProfiles().FirstOrDefault(pr => pr.PlayerName == playerName && pr.Avatar != null);
            return profile == null ? null : profile.Avatar.Texture;
        }
    }
}
