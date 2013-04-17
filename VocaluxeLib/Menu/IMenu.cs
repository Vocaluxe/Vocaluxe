using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using VocaluxeLib.Menu.SingNotes;
using VocaluxeLib.Menu.SongMenu;
using VocaluxeLib.PartyModes;

namespace VocaluxeLib.Menu
{
    public interface IMenu
    {
        void Init();

        void LoadTheme(string xmlPath);
        void SaveTheme();
        void ReloadTextures();
        void UnloadTextures();
        void ReloadTheme(string xmlPath);

        bool HandleInput(SKeyEvent keyEvent);
        bool HandleMouse(SMouseEvent mouseEvent);
        bool HandleInputThemeEditor(SKeyEvent keyEvent);
        bool HandleMouseThemeEditor(SMouseEvent mouseEvent);

        bool UpdateGame();
        void ApplyVolume();
        void OnShow();
        void OnShowFinish();
        void OnClose();

        bool Draw();
        SRectF ScreenArea { get; }

        void NextInteraction();
        void PrevInteraction();

        bool NextElement();
        bool PrevElement();

        void ProcessMouseClick(int x, int y);
        void ProcessMouseMove(int x, int y);
    }

    public interface IMenuParty
    {
        void SetPartyModeID(int partyModeID);
        int GetPartyModeID();

        void AssingPartyMode(IPartyMode partyMode);
        void DataToScreen(Object data);
    }

    public interface IConfig
    {
        void SetBackgroundMusicVolume(int newVolume);
        int GetBackgroundMusicVolume();

        int GetPreviewMusicVolume();

        EOffOn GetVideosToBackground();
        EOffOn GetVideoBackgrounds();
        EOffOn GetVideoPreview();

        ESongMenu GetSongMenuType();

        EOffOn GetDrawNoteLines();
        EOffOn GetDrawToneHelper();

        int GetCoverSize();

        List<string> GetSongFolder();
        ESongSorting GetSongSorting();
        EOffOn GetTabs();
        EOffOn GetIgnoreArticles();

        bool IsMicConfigured(int playerNr);
        int GetMaxNumMics();
    }

    public interface ISettings
    {
        int GetRenderW();
        int GetRenderH();
        bool IsTabNavigation();

        float GetZFar();
        float GetZNear();

        EGameState GetGameState();

        int GetToneMin();
        int GetToneMax();

        int GetNumNoteLines();
        int GetMaxNumPlayer();

        float GetDefaultMedleyFadeInTime();
        float GetDefaultMedleyFadeOutTime();
        int GetMedleyMinSeriesLength();
        float GetMedleyMinDuration();
    }

    public interface ITheme
    {
        string GetThemeScreensPath(int partyModeID);
        int GetSkinIndex(int partyModeID);
        STexture GetSkinTexture(string textureName, int partyModeID);
        STexture GetSkinVideoTexture(string videoName, int partyModeID);

        void SkinVideoResume(string videoName, int partyModeID);
        void SkinVideoPause(string videoName, int partyModeID);

        SColorF GetColor(string colorName, int partyModeID);
        bool GetColor(string colorName, int skinIndex, out SColorF color);
        SColorF GetPlayerColor(int playerNr);

        void UnloadSkins();
        void ListSkins();
        void LoadSkins();
        void LoadTheme();
    }

    public interface IHelper {}

    public interface IBackgroundMusic
    {
        bool IsDisabled();
        bool IsPlaying();
        bool SongHasVideo();
        bool VideoEnabled();

        void SetStatus(bool disabled);
        void Next();
        void Previous();
        void Pause();
        void Play();

        void ApplyVolume();

        STexture GetVideoTexture();
    }

    public interface IDrawing
    {
        RectangleF GetTextBounds(CText text);

        void DrawTexture(STexture texture, SRectF rect);
        void DrawTexture(STexture texture, SRectF rect, SColorF color);
        void DrawTexture(STexture texture, SRectF rect, SColorF color, SRectF bounds);
        void DrawTexture(STexture texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored);
        void DrawTextureReflection(STexture texture, SRectF rect, SColorF color, SRectF bounds, float reflectionSpace, float reflectionHeight);

        void RemoveTexture(ref STexture texture);

        void DrawColor(SColorF color, SRectF rect);
        void DrawColorReflection(SColorF color, SRectF rect, float space, float height);
    }

    public interface IGraphics
    {
        void ReloadTheme();
        void SaveTheme();
        void FadeTo(EScreens nextScreen);

        float GetGlobalAlpha();
    }

    public interface ILog
    {
        void LogError(string errorText);
    }

    public interface IFonts
    {
        void SetFont(string fontName);
        void SetStyle(EStyle fontStyle);

        RectangleF GetTextBounds(CText text, float textHeight);

        void DrawText(string text, float textHeight, float x, float y, float z, SColorF color);
        void DrawTextReflection(string text, float textHeight, float x, float y, float z, SColorF color, float reflectionSpace, float reflectionHeight);
        void DrawText(string text, float textHeight, float x, float y, float z, SColorF color, float begin, float end);
    }

    public interface ILanguage
    {
        string Translate(string keyWord);
        string Translate(string keyWord, int partyModeID);
        bool TranslationExists(string keyWord);
    }

    public interface IGame
    {
        int GetNumPlayer();
        void SetNumPlayer(int numPlayer);
        SPlayer[] GetPlayer();
        CPoints GetPoints();
        float GetMidBeatD();
        int GetCurrentBeatD();

        int GetRandom(int max);
        double GetRandomDouble();

        float GetTimeFromBeats(float beat, float bpm);

        void AddSong(int songID, EGameMode gameMode);
        void Reset();
        void ClearSongs();
        int GetNumSongs();
    }

    public interface IRecording
    {
        int GetToneAbs(int playerNr);
    }

    public interface IProfiles
    {
        SProfile[] GetProfiles();
    }

    public interface ISongs
    {
        int GetNumSongs();
        int GetNumVisibleSongs();
        int GetNumCategories();
        int NumSongsInCategory(int categoryIndex);

        int GetCurrentCategoryIndex();
        EOffOn GetTabs();
        string GetSearchFilter();

        void SetCategory(int categoryIndex);
        void UpdateRandomSongList();

        CSong GetVisibleSong(int visibleIndex);
        CSong GetSongByID(int songID);
        CSong[] GetSongs();
        CSong[] GetSongsNotSung();
        CCategory GetCategory(int index);

        void AddPartySongSung(int songID);
        void ResetPartySongSung();
        void ResetPartySongSung(int catIndex);

        void SortSongs(ESongSorting sorting, EOffOn tabs, EOffOn ignoreArticles, String searchString, EDuetOptions duetOptions);

        void NextCategory();
        void PrevCategory();
    }

    public interface IVideo
    {
        int Load(string videoFileName);
        bool Skip(int videoStream, float startPosition, float videoGap);
        bool GetFrame(int videoStream, ref STexture videoTexture, float time, ref float videoTime);
        bool IsFinished(int videoStream);
        bool Close(int videoStream);
    }

    public interface ISound
    {
        int Load(string soundFile, bool prescan);
        void SetPosition(int soundStream, float newPosition);
        void Play(int soundStream);
        void Fade(int soundStream, float targetVolume, float duration);

        bool IsFinished(int soundStream);
        float GetPosition(int soundStream);
        float GetLength(int soundStream);
        void FadeAndStop(int soundStream, float targetVolume, float duration);

        void SetStreamVolume(int soundStream, float volume);
        void SetStreamVolumeMax(int soundStream, float maxVolume);
    }

    public interface ICover
    {
        STexture GetNoCover();
    }

    public interface IDataBase
    {
        bool GetCover(string fileName, ref STexture texture, int coverSize);
    }

    public interface IInputs
    {
        void SetRumble(float duration);
    }

    public interface IPlaylist
    {
        string GetPlaylistName(int playlistID);
        string[] GetPlaylistNames();

        void SetPlaylistName(int playlistID, string name);
        void DeletePlaylist(int playlistID);
        void SavePlaylist(int playlistID);
        int GetNumPlaylists();

        void AddPlaylistSong(int playlistID, int songID);
        void AddPlaylistSong(int playlistID, int songID, EGameMode gameMode);
        void InsertPlaylistSong(int playlistID, int positionIndex, int songID, EGameMode gameMode);

        void MovePlaylistSong(int playlistID, int sourceIndex, int destIndex);
        void MovePlaylistSongDown(int playlistID, int songIndex);
        void MovePlaylistSongUp(int playlistID, int songIndex);
        void DeletePlaylistSong(int playlistID, int songIndex);

        int GetPlaylistSongCount(int playlistID);
        CPlaylistSong GetPlaylistSong(int playlistID, int songIndex);
    }
}