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

        void LoadTheme(string XmlPath);
        void SaveTheme();
        void ReloadTextures();
        void UnloadTextures();
        void ReloadTheme(string XmlPath);

        bool HandleInput(KeyEvent keyEvent);
        bool HandleMouse(MouseEvent mouseEvent);
        bool HandleInputThemeEditor(KeyEvent KeyEvent);
        bool HandleMouseThemeEditor(MouseEvent MouseEvent);

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
        void SetPartyModeID(int PartyModeID);
        int GetPartyModeID();

        void AssingPartyMode(IPartyMode PartyMode);
        void DataToScreen(Object Data);
    }

    public interface IConfig
    {
        void SetBackgroundMusicVolume(int NewVolume);
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

        bool IsMicConfigured(int PlayerNr);
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
        string GetThemeScreensPath(int PartyModeID);
        int GetSkinIndex(int PartyModeID);
        STexture GetSkinTexture(string TextureName, int PartyModeID);
        STexture GetSkinVideoTexture(string VideoName, int PartyModeID);

        void SkinVideoResume(string VideoName, int PartyModeID);
        void SkinVideoPause(string VideoName, int PartyModeID);

        SColorF GetColor(string ColorName, int PartyModeID);
        bool GetColor(string ColorName, int SkinIndex, out SColorF Color);
        SColorF GetPlayerColor(int PlayerNr);

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

        void SetStatus(bool Disabled);
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

        void DrawTexture(STexture Texture, SRectF Rect);
        void DrawTexture(STexture Texture, SRectF Rect, SColorF Color);
        void DrawTexture(STexture Texture, SRectF Rect, SColorF Color, SRectF Bounds);
        void DrawTexture(STexture Texture, SRectF Rect, SColorF Color, SRectF Bounds, bool Mirrored);
        void DrawTextureReflection(STexture Texture, SRectF Rect, SColorF Color, SRectF Bounds, float ReflectionSpace, float ReflectionHeight);

        void RemoveTexture(ref STexture Texture);

        void DrawColor(SColorF Color, SRectF Rect);
        void DrawColorReflection(SColorF Color, SRectF Rect, float Space, float Height);
    }

    public interface IGraphics
    {
        void ReloadTheme();
        void SaveTheme();
        void FadeTo(EScreens NextScreen);

        float GetGlobalAlpha();
    }

    public interface ILog
    {
        void LogError(string ErrorText);
    }

    public interface IFonts
    {
        void SetFont(string FontName);
        void SetStyle(EStyle FontStyle);

        RectangleF GetTextBounds(CText Text, float TextHeight);

        void DrawText(string Text, float TextHeight, float x, float y, float z, SColorF Color);
        void DrawTextReflection(string Text, float TextHeight, float x, float y, float z, SColorF Color, float ReflectionSpace, float ReflectionHeight);
        void DrawText(string Text, float TextHeight, float x, float y, float z, SColorF Color, float Begin, float End);
    }

    public interface ILanguage
    {
        string Translate(string KeyWord);
        string Translate(string KeyWord, int PartyModeID);
        bool TranslationExists(string KeyWord);
    }

    public interface IGame
    {
        int GetNumPlayer();
        void SetNumPlayer(int NumPlayer);
        SPlayer[] GetPlayer();
        CPoints GetPoints();
        float GetMidBeatD();
        int GetCurrentBeatD();

        int GetRandom(int Max);
        double GetRandomDouble();

        float GetTimeFromBeats(float Beat, float BPM);

        void AddSong(int SongID, EGameMode GameMode);
        void Reset();
        void ClearSongs();
        int GetNumSongs();
    }

    public interface IRecording
    {
        int GetToneAbs(int PlayerNr);
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
        int NumSongsInCategory(int CategoryIndex);

        int GetCurrentCategoryIndex();
        EOffOn GetTabs();
        string GetSearchFilter();

        void SetCategory(int CategoryIndex);
        void UpdateRandomSongList();

        CSong GetVisibleSong(int VisibleIndex);
        CSong GetSongByID(int SongID);
        CSong[] GetSongs();
        CSong[] GetSongsNotSung();
        CCategory GetCategory(int Index);

        void AddPartySongSung(int SongID);
        void ResetPartySongSung();
        void ResetPartySongSung(int CatIndex);

        void SortSongs(ESongSorting Sorting, EOffOn Tabs, EOffOn IgnoreArticles, String SearchString, EDuetOptions DuetOptions);

        void NextCategory();
        void PrevCategory();
    }

    public interface IVideo
    {
        int Load(string VideoFileName);
        bool Skip(int VideoStream, float StartPosition, float VideoGap);
        bool GetFrame(int VideoStream, ref STexture VideoTexture, float Time, ref float VideoTime);
        bool IsFinished(int VideoStream);
        bool Close(int VideoStream);
    }

    public interface ISound
    {
        int Load(string SoundFile, bool Prescan);
        void SetPosition(int SoundStream, float NewPosition);
        void Play(int SoundStream);
        void Fade(int SoundStream, float TargetVolume, float Duration);

        bool IsFinished(int SoundStream);
        float GetPosition(int SoundStream);
        float GetLength(int SoundStream);
        void FadeAndStop(int SoundStream, float TargetVolume, float Duration);

        void SetStreamVolume(int SoundStream, float Volume);
        void SetStreamVolumeMax(int SoundStream, float MaxVolume);
    }

    public interface ICover
    {
        STexture GetNoCover();
    }

    public interface IDataBase
    {
        bool GetCover(string FileName, ref STexture Texture, int CoverSize);
    }

    public interface IInputs
    {
        void SetRumble(float Duration);
    }

    public interface IPlaylist
    {
        string GetPlaylistName(int PlaylistID);
        string[] GetPlaylistNames();

        void SetPlaylistName(int PlaylistID, string Name);
        void DeletePlaylist(int PlaylistID);
        void SavePlaylist(int PlaylistID);
        int GetNumPlaylists();

        void AddPlaylistSong(int PlaylistID, int SongID);
        void AddPlaylistSong(int PlaylistID, int SongID, EGameMode GameMode);
        void InsertPlaylistSong(int PlaylistID, int PositionIndex, int SongID, EGameMode GameMode);

        void MovePlaylistSong(int PlaylistID, int SourceIndex, int DestIndex);
        void MovePlaylistSongDown(int PlaylistID, int SongIndex);
        void MovePlaylistSongUp(int PlaylistID, int SongIndex);
        void DeletePlaylistSong(int PlaylistID, int SongIndex);

        int GetPlaylistSongCount(int PlaylistID);
        CPlaylistSong GetPlaylistSong(int PlaylistID, int SongIndex);
    }
}