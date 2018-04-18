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
using System.Collections.ObjectModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using VocaluxeLib.Draw;
using VocaluxeLib.Game;
using VocaluxeLib.Menu;
using VocaluxeLib.Profile;
using VocaluxeLib.Songs;

namespace VocaluxeLib
{
    public delegate void OnSongMenuChanged();

    public interface IConfig
    {
        EOffOn GetSaveModifiedSongs();

        void SetBackgroundMusicVolume(int newVolume);
        int GetMusicVolume(EMusicType type);

        EBackgroundMusicOffOn GetBackgroundMusicStatus();

        int GetPreviewMusicVolume();

        EOffOn GetVideosToBackground();
        EOffOn GetVideoBackgrounds();
        EOffOn GetVideoPreview();

        ESongMenu GetSongMenuType();

        EHighscoreStyle GetHighscoreStyle();

        EOffOn GetDrawNoteLines();
        EOffOn GetDrawToneHelper();

        int GetCoverSize();

        IEnumerable<string> GetSongFolders();
        ESongSorting GetSongSorting();
        EOffOn GetTabs();
        EOffOn GetAutoplayPreviews();
        int GetAutoplayPreviewDelay();
        EOffOn GetIgnoreArticles();

        bool IsMicConfigured(int playerNr);
        int GetMaxNumMics();

        void AddSongMenuListener(OnSongMenuChanged onSongMenuChanged);
        void RemoveSongMenuListener(OnSongMenuChanged onSongMenuChanged);
    }

    public interface ISettings
    {
        int GetRenderW();
        int GetRenderH();
        SRectF GetRenderRect();
        bool IsTabNavigation();

        float GetZFar();
        float GetZNear();

        EProgramState GetProgramState();

        int GetToneMin();
        int GetToneMax();

        int GetNumNoteLines();
        int GetMaxNumPlayer();
        int GetMaxNumScreens();

        float GetDefaultMedleyFadeInTime();
        float GetDefaultMedleyFadeOutTime();
        int GetMedleyMinSeriesLength();
        float GetMedleyMinDuration();

        string GetFolderProfiles();
        string GetDataPath();

        float GetSlideShowImageTime();
        float GetSlideShowFadeTime();

        float GetSoundPlayerFadeTime();
    }

    public interface IThemes
    {
        string GetThemeScreensPath(int partyModeID);
        CTextureRef GetSkinTexture(string textureName, int partyModeID);
        CVideoStream GetSkinVideo(string videoName, int partyModeID, bool loop);

        bool GetColor(string colorName, int partyModeID, out SColorF color);
        SColorF GetPlayerColor(int playerNr);
        void Reload();
    }

    public interface IBackgroundMusic
    {
        bool IsDisabled();
        bool IsPlaying();
        bool IsPlayingPreview();
        bool SongHasVideo();
        bool VideoEnabled();
        float GetLength();

        void SetDisabled(bool disabled);
        void Next();
        void Previous();
        void Pause();
        void Play();
        void Stop();

        CTextureRef GetVideoTexture();

        void LoadPreview(CSong song, float start = -1f);
        void StopPreview();
        void SetPlayingPreview(bool playPreview);
    }

    public interface IDrawing
    {
        void DrawTexture(CTextureRef texture, SRectF rect);
        void DrawTexture(CTextureRef texture, SRectF rect, SColorF color, bool allMonitors = true);
        void DrawTexture(CTextureRef texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored = false, bool allMonitors = true);
        void DrawTexture(CTextureRef textureRef, SRectF bounds, EAspect aspect);
        void DrawTexture(CTextureRef textureRef, SRectF bounds, EAspect aspect, SColorF color);
        void DrawTextureReflection(CTextureRef texture, SRectF rect, SColorF color, SRectF bounds, float reflectionSpace, float reflectionHeight, bool allMonitors = true);

        CTextureRef AddTexture(string fileName);
        CTextureRef EnqueueTexture(string fileName);
        void RemoveTexture(ref CTextureRef texture);

        void DrawRect(SColorF color, SRectF rect, bool allMonitors = true);
        void DrawRectReflection(SColorF color, SRectF rect, float space, float height);
    }

    public interface IGraphics
    {
        void ReloadTheme();
        void SaveTheme();
        void FadeTo(EScreen nextScreen);
        void FadeTo(IMenu nextScreen);

        float GetGlobalAlpha();

        IMenu GetNextScreen();
        EScreen GetNextScreenType();
        IMenu GetScreen(EScreen screen);
    }

    public interface IFonts
    {
        RectangleF GetTextBounds(CText text);

        void DrawText(string text, CFont font, float x, float y, float z, SColorF color, bool allMonitors = true);
        void DrawTextReflection(string text, CFont font, float x, float y, float z, SColorF color, float reflectionSpace, float reflectionHeight);
        void DrawText(string text, CFont font, float x, float y, float z, SColorF color, float begin, float end);
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
        SPlayer[] GetPlayers();
        CPoints GetPoints();
        float GetMidRecordedBeat();
        int GetRecordedBeat();

        int GetRandom(int max);
        double GetRandomDouble();

        float GetTimeFromBeats(float beat, float bpm);
        float GetBeatFromTime(float time, float bpm, float gap);

        void AddSong(int songID, EGameMode gameMode);
        void Reset();
        void ClearSongs();
        int GetNumSongs();
        CSong GetSong();
        CSong GetSong(int round);
    }

    public interface IRecording
    {
        int GetToneAbs(int player);
    }

    public interface IProfiles
    {
        CProfile[] GetProfiles();
        int GetNum();
        EGameDifficulty GetDifficulty(Guid profileID);
        string GetPlayerName(Guid profileID, int playerNum = 0);
        CTextureRef GetAvatar(Guid profileID);
        CAvatar GetAvatarByFilename(string fileName);
        bool IsProfileIDValid(Guid profileID);
        bool IsGuest(Guid profileID);
        void AddProfileChangedCallback(ProfileChangedCallback notification);
    }

    public interface ISongs
    {
        int GetNumSongs();
        int GetNumSongsVisible();
        int GetNumCategories();
        int GetNumSongsInCategory(int categoryIndex);
        int GetNumSongsNotSungInCategory(int categoryIndex);
        bool IsInCategory();

        int GetCurrentCategoryIndex();
        EOffOn GetTabs();
        string GetSearchFilter();

        void SetCategory(int categoryIndex);
        void UpdateRandomSongList();

        CSong GetVisibleSong(int visibleIndex);
        CSong GetSongByID(int songID);
        ReadOnlyCollection<CSong> GetSongs();
        ReadOnlyCollection<CSong> GetVisibleSongs();
        CCategory GetCategory(int index);

        void AddPartySongSung(int songID);
        void ResetSongSung(int catIndex = -1);

        void SortSongs(ESongSorting sorting, EOffOn tabs, EOffOn ignoreArticles, String searchString, EDuetOptions duetOptions, int playlistID);

        void NextCategory();
        void PrevCategory();
    }

    public interface IVideo
    {
        CVideoStream Load(string videoFileName);
        bool Skip(CVideoStream stream, float startPosition, float videoGap);
        bool GetFrame(CVideoStream stream, float time);
        bool IsFinished(CVideoStream stream);
        void Close(ref CVideoStream stream);
        void SetLoop(CVideoStream stream, bool loop = true);
        void Resume(CVideoStream stream);
        void Pause(CVideoStream stream);
    }

    public interface ISound
    {
        int Load(string soundFile, bool loop = false, bool prescan = false);
        void SetPosition(int streamID, float newPosition);
        void Play(int streamID);
        void Fade(int streamID, int targetVolume, float duration, EStreamAction afterFadeAction = EStreamAction.Nothing);
        void Close(int streamID);

        bool IsFinished(int streamID);
        float GetPosition(int streamID);
        float GetLength(int streamID);

        void SetStreamVolume(int streamID, int volume);
        void SetGlobalVolume(int volume);
        bool IsPaused(int streamID);
    }

    public interface ICover
    {
        CTextureRef GetNoCover();
        CTextureRef GenerateCover(string text, ECoverGeneratorType type, CSong firstSong);
    }

    public interface IDataBase
    {
        bool GetCover(string fileName, ref CTextureRef texture, int coverSize);
        bool GetDataBaseSongInfos(string artist, string title, out int numPlayed, out DateTime dateAdded, out int highscoreID);
    }

    public interface IControllers
    {
        void SetRumble(float duration);
    }

    public interface IPlaylist
    {
        bool Exists(int playlistID);
        string GetName(int playlistID);
        List<string> GetNames();

        void SetName(int playlistID, string name);
        void Delete(int playlistID);
        void Save(int playlistID);
        int GetNumPlaylists();

        void AddSong(int playlistID, int songID);
        void AddSong(int playlistID, int songID, EGameMode gameMode);
        void InsertSong(int playlistID, int positionIndex, int songID, EGameMode gameMode);

        void MoveSong(int playlistID, int sourceIndex, int destIndex);
        void MoveSongDown(int playlistID, int songIndex);
        void MoveSongUp(int playlistID, int songIndex);
        void DeleteSong(int playlistID, int songIndex);

        int GetSongCount(int playlistID);
        CPlaylistSong GetSong(int playlistID, int songIndex);
        bool ContainsSong(int playlistID, int songIndex);
    }

    public interface IPreviewPlayer
    {
        void Play(float start = -1);
        void Load(CSong song, float start = 0f);
        void Stop();
        void TogglePause();
        CTextureRef GetCover();
        CTextureRef GetVideoTexture();
        bool IsPlaying();
        float GetLength();
    }
}