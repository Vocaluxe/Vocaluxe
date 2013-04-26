using Vocaluxe.Base;
using VocaluxeLib.Menu.SongMenu;

namespace Vocaluxe.GameModes
{
    abstract class CGameMode : IGameMode
    {
        private CSong _LastSong;
        private int _LastSongID;

        public CSong GetSong(int songID)
        {
            if (songID != _LastSongID)
            {
                CSong song = CSongs.GetSong(songID);
                _LastSong = _PrepareSong(song);
                _LastSongID = songID;
            }
            return _LastSong;
        }

        protected abstract CSong _PrepareSong(CSong song);
    }
}