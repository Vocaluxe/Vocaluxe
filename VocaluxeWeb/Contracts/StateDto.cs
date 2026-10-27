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

using System.ComponentModel.DataAnnotations;

namespace VocaluxeWeb.Contracts
{
    public sealed class StateDto
    {
        /// Name of the screen that is currently shown, e.g. "Main", "Song", "Sing".
        [Required] public string Screen { get; set; }

        /// The song that is currently loaded, or null.
        public SongDto CurrentSong { get; set; }
    }

    public sealed class ServerInfoDto
    {
        [Required] public string Name { get; set; }
        [Required] public string Version { get; set; }
        [Required] public bool AllowProfileCreation { get; set; }

        /// Name of the playlist that is the queue in the game.
        [Required] public string QueuePlaylistName { get; set; }
    }
}
