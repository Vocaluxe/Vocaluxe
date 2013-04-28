using VocaluxeLib.Menu.SongMenu;

namespace Vocaluxe.GameModes
{
    interface IGameMode
    {
        CSong GetSong(int songID);
    }
}