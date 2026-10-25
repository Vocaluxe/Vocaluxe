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
using VocaluxeLib.Log;
using VocaluxeLib.Menu;

namespace Vocaluxe.Base.Web
{
    /// The WebApp queue as the game sees it: a playlist plus requester and singers from the webserver.
    /// Used by the queue screen and by the webserver to start songs.
    static class CWebQueue
    {
        public sealed class CItem
        {
            public int SongId;
            public EGameMode GameMode;
            public EAudioMode AudioMode;
            public string RequestedBy;
            public IList<Guid> PlayerIds = new List<Guid>();
            public IList<string> PlayerNames = new List<string>();
        }

        private static readonly EScreen[] _BusyScreens = { EScreen.Load, EScreen.Sing, EScreen.JukeboxPlayer, EScreen.Party };

        /// Song that was started from the queue; after singing it the game returns to the queue screen.
        private static int _StartedSongId = -1;

        public static int PlaylistId
        {
            get
            {
                var name = CWebServer.GetQueuePlaylistName();
                var playlist = CPlaylists.Playlists.FirstOrDefault(p => p.Name == name);
                return playlist?.Id ?? -1;
            }
        }

        public static bool IsQueuePlaylist(int playlistId)
        {
            return playlistId >= 0 && playlistId == PlaylistId;
        }

        /// The entries in queue order. Without running webserver only the playlist is known (no requesters and singers).
        public static IList<CItem> GetItems()
        {
            var items = CWebServer.GetQueueItems();
            if (items != null)
            {
                return items;
            }

            var playlist = CPlaylists.Get(PlaylistId);
            if (playlist == null)
            {
                return new List<CItem>();
            }

            return playlist.Songs.Select(s => new CItem { SongId = s.SongId, GameMode = s.GameMode, AudioMode = s.AudioMode }).ToList();
        }

        public static bool CanStart()
        {
            return CParty.CurrentPartyModeId == -1 && !_BusyScreens.Contains(CGraphics.CurrentScreenType);
        }

        /// Starts singing the song. With singers and configured microphones the player selection is skipped;
        /// otherwise (or when requested) the player selection is shown with the singers already chosen.
        public static bool Start(int songId, EGameMode gameMode, EAudioMode audioMode, IList<Guid> playerIds, bool showPlayerSelection, out bool singsNow)
        {
            singsNow = false;
            if (!CanStart() || CSongs.GetSong(songId) == null)
            {
                return false;
            }

            CParty.SetNormalGameMode();
            CGame.Reset();
            CGame.ClearSongs();
            CGame.AddSong(songId, gameMode, audioMode);
            if (CGame.GetNumSongs() == 0)
            {
                return false;
            }

            var players = (playerIds ?? new List<Guid>()).Where(CProfiles.IsProfileIdValid).Distinct().Take(CSettings.MaxNumPlayer).ToList();
            if (players.Count > 0)
            {
                CConfig.Config.Game.NumPlayers = players.Count;
                CGame.NumPlayers = players.Count;
                var isDuet = CGame.GetSong(0).IsDuet;
                for (var i = 0; i < CSettings.MaxNumPlayer; i++)
                {
                    CGame.Players[i].ProfileId = i < players.Count ? players[i] : Guid.Empty;
                    CGame.Players[i].VoiceNr = isDuet ? i % 2 : 0;
                }
            }

            _StartedSongId = songId;
            var micsReady = Enumerable.Range(1, players.Count).All(CConfig.IsMicConfig);
            singsNow = players.Count > 0 && micsReady && !showPlayerSelection;
            CLog.Information("WebApp queue: starting song {SongId} with {Players} of {Chosen} chosen singers, microphones ready: {MicsReady}, player selection: {Selection}",
                new object[] { songId, players.Count, playerIds?.Count ?? 0, micsReady, !singsNow });
            if (!singsNow)
            {
                CBackgroundMusic.LoadPreview(CSongs.GetSong(songId));
            }

            CGraphics.FadeTo(singsNow ? EScreen.Sing : EScreen.Names);
            return true;
        }

        /// Called when leaving the score screens: goes back to the queue screen if the song was started from the queue.
        public static bool ReturnAfterSinging()
        {
            var startedFromQueue = _StartedSongId >= 0 && CParty.CurrentPartyModeId == -1 && CGame.GetSong(0)?.Id == _StartedSongId;
            _StartedSongId = -1;
            if (!startedFromQueue)
            {
                return false;
            }

            CGraphics.FadeTo(EScreen.WebQueue);
            return true;
        }

        public static void SetModes(int index, EGameMode gameMode, EAudioMode audioMode)
        {
            var playlist = CPlaylists.Get(PlaylistId);
            if (playlist == null || index < 0 || index >= playlist.Songs.Count)
            {
                return;
            }

            playlist.Songs[index].GameMode = gameMode;
            playlist.Songs[index].AudioMode = audioMode;
            playlist.Save();
            CWebServer.NotifyPlaylistChanged();
        }

        public static void Remove(int index)
        {
            var playlist = CPlaylists.Get(PlaylistId);
            if (playlist == null || index < 0 || index >= playlist.Songs.Count)
            {
                return;
            }

            playlist.DeleteSong(index);
            playlist.Save();
            CWebServer.NotifyPlaylistChanged();
        }
    }
}
