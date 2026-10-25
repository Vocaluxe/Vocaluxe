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

using System.Linq;

namespace VocaluxeWeb.Services
{
    /// Short, readable device description from a browser's user agent, e.g. "iPhone · Safari".
    public static class CUserAgent
    {
        /// Checked in this order: the first entry whose markers appear in the user agent wins.
        private static readonly (string Name, string[] Markers)[] _Devices =
        {
            ("iPhone", new[] { "iPhone" }),
            ("iPad", new[] { "iPad" }),
            ("Android phone", new[] { "Android", "Mobile" }),
            ("Android tablet", new[] { "Android" }),
            ("Windows", new[] { "Windows" }),
            ("Mac", new[] { "Mac OS X" }),
            ("Linux", new[] { "Linux" })
        };

        /// Checked in this order, because most browsers also name the engines of others (e.g. Edge contains "Chrome/" and "Safari/").
        private static readonly (string Name, string[] Markers)[] _Browsers =
        {
            ("Edge", new[] { "Edg/" }),
            ("Firefox", new[] { "Firefox/" }),
            ("Firefox", new[] { "FxiOS" }),
            ("Samsung Internet", new[] { "SamsungBrowser" }),
            ("Chrome", new[] { "Chrome/" }),
            ("Chrome", new[] { "CriOS" }),
            ("Safari", new[] { "Safari/" })
        };

        public static string Describe(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                return null;
            }

            var device = _Find(_Devices, userAgent);
            var browser = _Find(_Browsers, userAgent);
            if (device == null && browser == null)
            {
                return userAgent.Length > 40 ? userAgent.Substring(0, 40) + "…" : userAgent;
            }

            return device != null && browser != null ? device + " · " + browser : device ?? browser;
        }

        private static string _Find((string Name, string[] Markers)[] candidates, string userAgent)
        {
            return candidates.Where(c => c.Markers.All(userAgent.Contains)).Select(c => c.Name).FirstOrDefault();
        }
    }
}
