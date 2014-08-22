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
using System.Xml;
using VocaluxeLib.Draw;
using VocaluxeLib.Game;
using VocaluxeLib.Menu;
using VocaluxeLib.Profile;
using VocaluxeLib.Songs;

namespace VocaluxeLib
{
    public interface IConfig
    {
        EOffOn GetSaveModifiedSongs();

        void SetBackgroundMusicVolume(int newVolume);
        int GetBackgroundMusicVolume();

        EOffOn GetBackgroundMusicStatus();

        int GetPreviewMusicVolume();

        EOffOn GetVideosToBackground();
        EOffOn GetVideoBackgrounds();
        EOffOn GetVideoPreview();

        ESongMenu GetSongMenuType();

        EOffOn GetDrawNoteLines();
        EOffOn GetDrawToneHelper();

        int GetCoverSize();

        IEnumerable<string> GetSongFolders();
        ESongSorting GetSongSorting();
        EOffOn GetTabs();
        EOffOn GetIgnoreArticles();

        bool IsMicConfigured(int playerNr);
        int GetMaxNumMics();

        /// <summary>
        ///     Get the uniform settings for writing XML files. ALWAYS use this!
        /// </summary>
        XmlWriterSettings GetXMLSettings();
    }

    public interface ISettings
    {
        int GetRenderW();
        int GetRenderH();
        bool IsTabNavigation();

        float GetZFar();
        float GetZNear();

        EProgramState GetProgramState();

        int GetToneMin();
        int GetToneMax();

        int GetNumNoteLines();
        int GetMaxNumPlayer();

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

    public interface ITheme
    {
        string GetThemeScreensPath(int partyModeID);
        int GetSkinIndex(int partyModeID);
        CTexture GetSkinTexture(string textureName, int partyModeID);
        CTexture GetSkinVideoTexture(string videoName, int partyModeID);

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

        CTexture GetVideoTexture();

        void LoadPreview(CSong song, float start = 0f);
        void PlayPreview(float start = -1f);
        void StopPreview();

    }

    public interface IDrawing
    {
        void DrawTexture(CTexture texture, SRectF rect);
        void DrawTexture(CTexture texture, SRectF rect, SColorF color);
        void DrawTexture(CTexture texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored = false);
        void DrawTextureReflection(CTexture texture, SRectF rect, SColorF color, SRectF bounds, float reflectionSpace, float reflectionHeight);

        CTexture AddTexture(string fileName);
        void RemoveTexture(ref CTexture texture);

        void DrawColor(SColorF color, SRectF rect);
        void DrawColorReflection(SColorF color, SRectF rect, float space, float height);
    }

    public interface IGraphics
    {
        void ReloadTheme();
        void SaveTheme();
        void FadeTo(EScreens nextScreen);

        float GetGlobalAlpha();

        EScreens GetNextScreen();
    }

    public interface ILog
    {
        void LogError(string errorText);
        void LogDebug(string text);
        void LogSongInfo(string text);
    }

    public interface IFonts
    {
        void SetFont(string fontName);
        void SetStyle(EStyle fontStyle);

        RectangleF GetTextBounds(CText text);
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
        int GetToneAbs(int playerNr);
    }

    public interface IProfiles
    {
        CProfile[] GetProfiles();
        EGameDifficulty GetDifficulty(int profileID);
        string GetPlayerName(int profileID, int playerNum = 0);
        CTexture GetAvatar(int profileID);
        CAvatar GetAvatarByFilename(string fileName);
        bool IsProfileIDValid(int profileID);
        bool IsGuest(int profileID);
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
        ReadOnlyCollection<CSong> GetVisibleSongs();
        CCategory GetCategory(int index);

        void AddPartySongSung(int songID);
        void ResetSongSung(int catIndex = -1);

        void SortSongs(ESongSorting sorting, EOffOn tabs, EOffOn ignoreArticles, String searchString, EDuetOptions duetOptions);

        void NextCategory();
        void PrevCategory();
    }

    public interface IVideo
    {
        int Load(string videoFileName);
        bool Skip(int streamID, float startPosition, float videoGap);
        bool GetFrame(int streamID, ref CTexture videoTexture, float time, out float videoTime);
        bool IsFinished(int streamID);
        bool Close(int streamID);
        void SetLoop(int streamID, bool loop = true);
        void Resume(int streamID);
        void Pause(int streamID);
    }

    public interface ISound
    {
        int Load(string soundFile, bool loop = false, bool prescan = false);
        void SetPosition(int soundStream, float newPosition);
        void Play(int soundStream);
        void Fade(int soundStream, float targetVolume, float duration, EStreamAction afterFadeAction = EStreamAction.Nothing);
        void Close(int soundStream);

        bool IsFinished(int soundStream);
        float GetPosition(int soundStream);
        float GetLength(int soundStream);

        void SetStreamVolume(int soundStream, float volume);
    }

    public interface ICover
    {
        CTexture GetNoCover();
    }

    public interface IDataBase
    {
        bool GetCover(string fileName, ref CTexture texture, int coverSize);
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
    }

    public interface IPreviewPlayer
    {
        void Play(float start = -1);
        void Load(CSong song, float start = 0f);
        void Stop();
        void TogglePause();
        CTexture GetCover();
        CTexture GetVideoTexture();
        bool IsPlaying();
        float GetLength();

    }
}