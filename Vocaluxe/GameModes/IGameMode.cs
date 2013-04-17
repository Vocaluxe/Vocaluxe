using VocaluxeLib.Menu;
using VocaluxeLib.Menu.SongMenu;

namespace Vocaluxe.GameModes
{
    interface IGameMode
    {
        void Init();

        EGameMode GetCurrentGameMode();

        bool AddVisibleSong(int visibleIndex, EGameMode gameMode);
        bool AddSong(int absoluteIndex, EGameMode gameMode);
        bool RemoveVisibleSong(int visibleIndex);
        bool RemoveSong(int absoluteIndex);
        void ClearSongs();

        void Reset();
        void Start(SPlayer[] player);
        void NextRound(SPlayer[] player);
        bool IsFinished();
        int GetCurrentRoundNr();

        CPoints GetPoints();

        int GetNumSongs();
        CSong GetSong();
        CSong GetSong(int num);
        EGameMode GetGameMode(int num);
    }
}