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

using System.Drawing;
using VocaluxeLib;

namespace Vocaluxe.Base
{
    /// <summary>
    ///     Generates fallback covers for songs that have none.
    ///     TODO(linux-port): the original implementation composed the cover with GDI+
    ///     (System.Drawing Bitmap/Graphics/Font), which is Windows-only. It is temporarily
    ///     stubbed out (returns null -> the caller keeps the themed "NoCover" texture) and
    ///     needs a SkiaSharp re-implementation as the final S5 step.
    /// </summary>
    class CCoverGenerator
    {
        // ReSharper disable UnusedParameter.Local
        public CCoverGenerator(SThemeCoverGenerator theme, string basePath) {}
        // ReSharper restore UnusedParameter.Local

        public Bitmap GetCover(string text, string firstCoverPath)
        {
            return null;
        }
    }
}
