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
using System.IO;
using Vocaluxe.Base.Fonts;
using Vocaluxe.Base.ThemeSystem;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Game;
using VocaluxeLib.Log;
using VocaluxeLib.Menu;
using VocaluxeLib.Profile;
using VocaluxeLib.Songs;

namespace Vocaluxe.Base
{
    static class CMain
    {
        private static readonly IConfig _Config = new CBconfig();
        private static readonly ISettings _Settings = new CBsettings();
        private static readonly IThemes _Themes = new CBtheme();
        private static readonly IBackgroundMusic _BackgroundMusic = new CBbackgroundMusic();
        private static readonly IDrawing _Draw = new CBdraw();
        private static readonly IGraphics _Graphics = new CBGraphics();
        private static readonly IFonts _Fonts = new CBfonts();
        private static readonly ILanguage _Language = new CBlanguage();
        private static readonly IGame _Game = new CBGame();
        private static readonly IProfiles _Profiles = new CBprofiles();
        private static readonly IRecording _Record = new CBrecord();
        private static readonly ISongs _Songs = new CBsongs();
        private static readonly IVideo _Video = new CBvideo();
        private static readonly ISound _Sound = new CBsound();
        private static readonly ICover _Cover = new CBcover();
        private static readonly IDataBase _DataBase = new CBdataBase();
        private static readonly IControllers _Controller = new CBcontrollers();
        private static readonly IPlaylist _Playlist = new CBplaylist();

        public static void Init()
        {
            CBase.Assign(_Config, _Settings, _Themes, _BackgroundMusic, _Draw, _Graphics, _Fonts, _Language,
                _Game, _Profiles, _Record, _Songs, _Video, _Sound, _Cover, _DataBase, _Controller, _Playlist);
        }
    }

    class CBconfig : IConfig
    {
        public EOffOn GetSaveModifiedSongs()
        {
            return CConfig.Config.Debug.SaveModifiedSongs;
        }

        public void SetBackgroundMusicVolume(int newVolume)
        {
            CConfig.BackgroundMusicVolume = newVolume;
        }

        public int GetMusicVolume(EMusicType type)
        {
            return CConfig.GetVolumeByType(type);
        }

        public EBackgroundMusicOffOn GetBackgroundMusicStatus()
        {
            return CConfig.Config.Sound.BackgroundMusic;
        }

        public int GetPreviewMusicVolume()
        {
            return CConfig.PreviewMusicVolume;
        }

        public ESongMenu GetSongMenuType()
        {
            return CConfig.SongMenu;
        }

        public EHighscoreStyle GetHighscoreStyle()
        {
            return CConfig.Config.Game.HighscoreStyle;
        }

        public EOffOn GetVideosToBackground()
        {
            return CConfig.Config.Video.VideosToBackground;
        }

        public EOffOn GetVideoBackgrounds()
        {
            return CConfig.Config.Video.VideoBackgrounds;
        }

        public EOffOn GetVideoPreview()
        {
            return CConfig.Config.Video.VideoPreview;
        }

        public EOffOn GetDrawNoteLines()
        {
            return CConfig.Config.Theme.DrawNoteLines;
        }

        public EOffOn GetDrawToneHelper()
        {
            return CConfig.Config.Theme.DrawToneHelper;
        }

        public int GetCoverSize()
        {
            return CConfig.Config.Graphics.CoverSize;
        }

        public IEnumerable<string> GetSongFolders()
        {
            return CConfig.SongFolders;
        }

        public ESongSorting GetSongSorting()
        {
            return CConfig.Config.Game.SongSorting;
        }

        public EOffOn GetTabs()
        {
            return CConfig.Config.Game.Tabs;
        }

        public EOffOn GetAutoplayPreviews()
        {
            return CConfig.Config.Game.AutoplayPreviews;
        }

        public int GetAutoplayPreviewDelay()
        {
            return CConfig.Config.Game.AutoplayPreviewDelay;
        }

        public EOffOn GetIgnoreArticles()
        {
            return CConfig.Config.Game.IgnoreArticles;
        }

        public bool IsMicConfigured(int playerNr)
        {
            return CConfig.IsMicConfig(playerNr);
        }

        public int GetMaxNumMics()
        {
            return CConfig.GetMaxNumMics();
        }

        public void AddSongMenuListener(OnSongMenuChanged onSongMenuChanged)
        {
            CConfig.SongMenuChanged += onSongMenuChanged;
        }

        public void RemoveSongMenuListener(OnSongMenuChanged onSongMenuChanged)
        {
            CConfig.SongMenuChanged -= onSongMenuChanged;
        }

        public int GetNumScreens()
        {
            return CConfig.GetNumScreens();
        }
    }

    class CBsettings : ISettings
    {
        public int GetRenderW()
        {
            return CSettings.RenderW;
        }

        public int GetRenderH()
        {
            return CSettings.RenderH;
        }

        public SRectF GetRenderRect()
        {
            return CSettings.RenderRect;
        }

        public bool IsTabNavigation()
        {
            return CSettings.TabNavigation;
        }

        public float GetZFar()
        {
            return CSettings.ZFar;
        }

        public float GetZNear()
        {
            return CSettings.ZNear;
        }

        public EProgramState GetProgramState()
        {
            return CSettings.ProgramState;
        }

        public int GetToneMin()
        {
            return CSettings.ToneMin;
        }

        public int GetToneMax()
        {
            return CSettings.ToneMax;
        }

        public int GetNumNoteLines()
        {
            return CSettings.NumNoteLines;
        }

        public int GetMaxNumPlayer()
        {
            return CSettings.MaxNumPlayer;
        }

        public int GetMaxNumScreens()
        {
            return CSettings.MaxNumScreens;
        }

        public float GetDefaultMedleyFadeInTime()
        {
            return CSettings.DefaultMedleyFadeInTime;
        }

        public float GetDefaultMedleyFadeOutTime()
        {
            return CSettings.DefaultMedleyFadeOutTime;
        }

        public int GetMedleyMinSeriesLength()
        {
            return CSettings.MedleyMinSeriesLength;
        }

        public float GetMedleyMinDuration()
        {
            return CSettings.MedleyMinDuration;
        }

        public string GetFolderProfiles()
        {
            return CConfig.ProfileFolders[0];
        }

        public string GetDataPath()
        {
            return CSettings.DataFolder;
        }

        public float GetSlideShowImageTime()
        {
            return CSettings.SlideShowImageTime;
        }

        public float GetSlideShowFadeTime()
        {
            return CSettings.SlideShowFadeTime;
        }

        public float GetSoundPlayerFadeTime()
        {
            return CSettings.SoundPlayerFadeTime;
        }
    }

    class CBtheme : IThemes
    {
        public string GetThemeScreensPath(int partyModeId)
        {
            return CThemes.GetThemeScreensPath(partyModeId);
        }

        public CTextureRef GetSkinTexture(string textureName, int partyModeId)
        {
            return CThemes.GetSkinTexture(textureName, partyModeId);
        }

        public CVideoStream GetSkinVideo(string videoName, int partyModeId, bool loop)
        {
            return CThemes.GetSkinVideo(videoName, partyModeId, loop);
        }

        public bool GetColor(string colorName, int partyModeId, out SColorF color)
        {
            return CThemes.GetColor(colorName, partyModeId, out color);
        }

        public SColorF GetPlayerColor(int playerNr)
        {
            return CThemes.GetPlayerColor(playerNr);
        }

        public void Reload()
        {
            CThemes.Reload();
        }
    }

    class CBbackgroundMusic : IBackgroundMusic
    {
        public bool IsDisabled()
        {
            return CBackgroundMusic.Disabled;
        }

        public bool IsPlaying()
        {
            return CBackgroundMusic.IsPlaying;
        }

        public bool SongHasVideo()
        {
            return CBackgroundMusic.SongHasVideo;
        }

        public bool VideoEnabled()
        {
            return CBackgroundMusic.VideoEnabled;
        }

        public bool IsPlayingPreview()
        {
            return CBackgroundMusic.IsPlayingPreview;
        }

        public void SetDisabled(bool disabled)
        {
            CBackgroundMusic.Disabled = disabled;
        }

        public float GetLength()
        {
            return CBackgroundMusic.Length;
        }

        public void Next()
        {
            CBackgroundMusic.Next();
        }

        public void Previous()
        {
            CBackgroundMusic.Previous();
        }

        public void Pause()
        {
            CBackgroundMusic.Pause();
        }

        public void Play()
        {
            CBackgroundMusic.Play();
        }

        public void Stop()
        {
            CBackgroundMusic.Stop();
        }

        public void LoadPreview(CSong song, float start = -1f)
        {
            CBackgroundMusic.LoadPreview(song, start);
        }

        public void StopPreview()
        {
            CBackgroundMusic.StopPreview();
        }

        public void SetPlayingPreview(bool playPreview)
        {
            CBackgroundMusic.IsPlayingPreview = playPreview;
        }

        public CTextureRef GetVideoTexture()
        {
            return CBackgroundMusic.GetVideoTexture();
        }
    }

    class CBdraw : IDrawing
    {
        public void DrawTexture(CTextureRef texture, SRectF rect)
        {
            CDraw.DrawTexture(texture, rect);
        }

        public void DrawTexture(CTextureRef texture, SRectF rect, SColorF color, bool allMonitors = true)
        {
            CDraw.DrawTexture(texture, rect, color, false, allMonitors);
        }

        public void DrawTexture(CTextureRef texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored = false, bool allMonitors = true)
        {
            CDraw.DrawTexture(texture, rect, color, bounds, mirrored, allMonitors);
        }

        public void DrawTexture(CTextureRef textureRef, SRectF bounds, EAspect aspect)
        {
            CDraw.DrawTexture(textureRef, bounds, aspect);
        }

        public void DrawTexture(CTextureRef textureRef, SRectF bounds, EAspect aspect, SColorF color)
        {
            CDraw.DrawTexture(textureRef, bounds, aspect, color);
        }

        public void DrawTextureReflection(CTextureRef texture, SRectF rect, SColorF color, SRectF bounds, float reflectionSpace, float reflectionHeight, bool allMonitors = true)
        {
            CDraw.DrawTextureReflection(texture, rect, color, bounds, reflectionSpace, reflectionHeight, allMonitors);
        }

        public CTextureRef AddTexture(string fileName)
        {
            return CDraw.AddTexture(fileName);
        }

        public CTextureRef EnqueueTexture(string fileName)
        {
            return CDraw.EnqueueTexture(fileName);
        }

        public void RemoveTexture(ref CTextureRef texture)
        {
            CDraw.RemoveTexture(ref texture);
        }

        public void DrawRect(SColorF color, SRectF rect, bool allMonitors = true)
        {
            CDraw.DrawRect(color, rect, allMonitors);
        }

        public void DrawRectReflection(SColorF color, SRectF rect, float space, float height)
        {
            CDraw.DrawRectReflection(color, rect, space, height);
        }
    }

    class CBGraphics : IGraphics
    {
        public void ReloadTheme()
        {
            CGraphics.ReloadTheme();
        }

        public void SaveTheme()
        {
            CGraphics.SaveTheme();
        }

        public void FadeTo(EScreen nextScreen)
        {
            CGraphics.FadeTo(nextScreen);
        }

        public void FadeTo(IMenu nextScreen)
        {
            CGraphics.FadeTo(nextScreen);
        }

        public float GetGlobalAlpha()
        {
            return CGraphics.GlobalAlpha;
        }

        public IMenu GetNextScreen()
        {
            return CGraphics.NextScreen;
        }

        public EScreen GetNextScreenType()
        {
            return CGraphics.NextScreenType;
        }

        public IMenu GetScreen(EScreen screen)
        {
            return CGraphics.GetScreen(screen);
        }
    }

    class CBfonts : IFonts
    {
        public RectangleF GetTextBounds(CText text)
        {
            return CFonts.GetTextBounds(text);
        }

        public void DrawText(string text, CFont font, float x, float y, float z, SColorF color, bool allMonitors = true)
        {
            CFonts.DrawText(text, font, x, y, z, color, allMonitors);
        }

        public void DrawTextReflection(string text, CFont font, float x, float y, float z, SColorF color, float reflectionSpace, float reflectionHeight)
        {
            CFonts.DrawTextReflection(text, font, x, y, z, color, reflectionSpace, reflectionHeight);
        }

        public void DrawText(string text, CFont font, float x, float y, float z, SColorF color, float begin, float end)
        {
            CFonts.DrawText(text, font, x, y, z, color, begin, end);
        }
    }

    class CBlanguage : ILanguage
    {
        public string Translate(string keyWord)
        {
            return CLanguage.Translate(keyWord);
        }

        public string Translate(string keyWord, int partyModeId)
        {
            return CLanguage.Translate(keyWord, partyModeId);
        }

        public bool TranslationExists(string keyWord)
        {
            return CLanguage.TranslationExists(keyWord);
        }
    }

    class CBGame : IGame
    {
        public int GetNumPlayer()
        {
            return CGame.NumPlayers;
        }

        public void SetNumPlayer(int numPlayer)
        {
            CGame.NumPlayers = numPlayer;
        }

        public SPlayer[] GetPlayers()
        {
            return CGame.Players;
        }

        public CPoints GetPoints()
        {
            return CGame.GetPoints();
        }

        public int GetCurrentBeat()
        {
            return CGame.CurrentBeat;
        }

        public float GetMidRecordedBeat()
        {
            return CGame.MidRecordedBeat;
        }

        public int GetRecordedBeat()
        {
            return CGame.RecordedBeat;
        }

        public int GetRandom(int max)
        {
            return CGame.Rand.Next(max);
        }

        public double GetRandomDouble()
        {
            return CGame.Rand.NextDouble();
        }

        public float GetTimeFromBeats(float beat, float bpm)
        {
            return CGame.GetTimeFromBeats(beat, bpm);
        }

        public float GetBeatFromTime(float time, float bpm, float gap)
        {
            return CGame.GetBeatFromTime(time, bpm, gap);
        }

        public void AddSong(int songId, EGameMode gameMode)
        {
            CGame.AddSong(songId, gameMode);
        }

        public void Reset()
        {
            CGame.Reset();
        }

        public void ClearSongs()
        {
            CGame.ClearSongs();
        }

        public int GetNumSongs()
        {
            return CGame.GetNumSongs();
        }

        public CSong GetSong()
        {
            return CGame.GetSong();
        }

        public CSong GetSong(int round)
        {
            return CGame.GetSong(round);
        }
    }

    class CBprofiles : IProfiles
    {
        public CProfile[] GetProfiles()
        {
            return CProfiles.GetProfiles();
        }

        public int GetNum()
        {
            return CProfiles.NumProfiles;
        }

        public EGameDifficulty GetDifficulty(Guid profileId)
        {
            return CProfiles.GetDifficulty(profileId);
        }

        public string GetPlayerName(Guid profileId, int playerNum = 0)
        {
            return CProfiles.GetPlayerName(profileId, playerNum);
        }

        public CTextureRef GetAvatar(Guid profileId)
        {
            return CProfiles.GetAvatarTextureFromProfile(profileId);
        }

        public CAvatar GetAvatarByFilename(string fileName)
        {
            return CProfiles.GetAvatarByFilename(fileName);
        }

        public bool IsProfileIdValid(Guid profileId)
        {
            return CProfiles.IsProfileIdValid(profileId);
        }

        public bool IsGuest(Guid profileId)
        {
            return CProfiles.IsGuestProfile(profileId);
        }

        public void AddProfileChangedCallback(ProfileChangedCallback notification)
        {
            CProfiles.AddProfileChangedCallback(notification);
        }
    }

    class CBrecord : IRecording
    {
        public int GetToneAbs(int player)
        {
            return CRecord.GetToneAbs(player);
        }

        public void AnalyzeBuffer(int player)
        {
            CRecord.AnalyzeBuffer(player);
        }

        public bool Start()
        {
            return CRecord.Start();
        }

        public bool Stop()
        {
            return CRecord.Stop();
        }

        public float GetMaxVolume(int player)
        {
            return CRecord.GetMaxVolume(player);
        }

        public float[] ToneWeigth(int player)
        {
            return CRecord.ToneWeigth(player);
        }
    }

    class CBsongs : ISongs
    {
        public int GetNumSongs()
        {
            return CSongs.NumAllSongs;
        }

        public int GetNumSongsVisible()
        {
            return CSongs.NumSongsVisible;
        }

        public int GetNumCategories()
        {
            return CSongs.NumCategories;
        }

        public int GetNumSongsInCategory(int categoryIndex)
        {
            return CSongs.GetNumSongsInCategory(categoryIndex);
        }

        public int GetNumSongsNotSungInCategory(int categoryIndex)
        {
            return CSongs.GetNumSongsNotSungInCategory(categoryIndex);
        }

        public bool IsInCategory()
        {
            return CSongs.IsInCategory;
        }

        public int GetCurrentCategoryIndex()
        {
            return CSongs.Category;
        }

        public EOffOn GetTabs()
        {
            return CSongs.Categorizer.Tabs;
        }

        public string GetSearchFilter()
        {
            return CSongs.Filter.SearchString;
        }

        public void SetCategory(int categoryIndex)
        {
            CSongs.Category = categoryIndex;
        }

        public void UpdateRandomSongList()
        {
            CSongs.UpdateRandomSongList();
        }

        public CSong GetVisibleSong(int visibleIndex)
        {
            return CSongs.GetVisibleSongByIndex(visibleIndex);
        }

        public CSong GetSongById(int songId)
        {
            return CSongs.GetSong(songId);
        }

        public ReadOnlyCollection<CSong> GetSongs()
        {
            return CSongs.AllSongs;
        }

        public ReadOnlyCollection<CSong> GetVisibleSongs()
        {
            return CSongs.VisibleSongs;
        }

        public CCategory GetCategory(int index)
        {
            return CSongs.GetCategoryByIndex(index);
        }

        public void AddPartySongSung(int songId)
        {
            CSongs.AddPartySongSung(songId);
        }

        public void ResetSongSung()
        {
            CSongs.ResetPartySongSung();
        }

        public void ResetSongSung(int catIndex)
        {
            CSongs.ResetPartySongSung(catIndex);
        }

        public void SortSongs(ESongSorting sorting, EOffOn tabs, EOffOn ignoreArticles, String searchString, EDuetOptions duetOptions, int playlistId)
        {
            CSongs.Sort(sorting, tabs, ignoreArticles, searchString, duetOptions, playlistId);
        }

        public void NextCategory()
        {
            CSongs.NextCategory();
        }

        public void PrevCategory()
        {
            CSongs.PrevCategory();
        }
    }

    class CBvideo : IVideo
    {
        public CVideoStream LoadStream(Stream stream)
        {
            return CVideo.LoadStream(stream);
        }

        public bool Skip(CVideoStream stream, float startPosition, float videoGap)
        {
            return CVideo.Skip(stream, startPosition, videoGap);
        }

        public bool GetFrame(CVideoStream stream, float time)
        {
            return CVideo.GetFrame(stream, time);
        }

        public bool IsFinished(CVideoStream stream)
        {
            return CVideo.Finished(stream);
        }

        public void Close(ref CVideoStream stream)
        {
            try
            {
                CVideo.Close(ref stream);
            }
            catch (NotSupportedException e)
            {
                CLog.Error($"Clould not close the background video: {e.Message}");
            }
        }

        public void SetLoop(CVideoStream stream, bool loop = true)
        {
            CVideo.SetLoop(stream, loop);
        }

        public void Pause(CVideoStream stream)
        {
            try
            {
                CVideo.Pause(stream);
            }
            catch (NotSupportedException e)
            {
                CLog.Error($"Clould not pause the background video: {e.Message}");
            }
        }

        public void Resume(CVideoStream stream)
        {
            try
            {
                CVideo.Resume(stream);
            }
            catch (NotSupportedException e)
            {
                CLog.Error($"Clould not resume the background video: {e.Message}");
            }
        }
    }

    class CBsound : ISound
    {
        public int Load(string uri, bool loop = false, bool prescan = false)
        {
            return CSound.Load(uri, loop, prescan);
        }

        public void SetPosition(int soundStream, float newPosition)
        {
            CSound.SetPosition(soundStream, newPosition);
        }

        public void Play(int soundStream)
        {
            CSound.Play(soundStream);
        }

        public void Fade(int soundStream, int targetVolume, float duration, EStreamAction afterFadeAction = EStreamAction.Nothing)
        {
            CSound.Fade(soundStream, targetVolume, duration, afterFadeAction);
        }

        public void Close(int soundStream)
        {
            CSound.Close(soundStream);
        }

        public bool IsFinished(int soundStream)
        {
            return CSound.IsFinished(soundStream);
        }

        public float GetPosition(int soundStream)
        {
            return CSound.GetPosition(soundStream);
        }

        public float GetLength(int soundStream)
        {
            return CSound.GetLength(soundStream);
        }

        public void SetStreamVolume(int soundStream, int volume)
        {
            CSound.SetStreamVolume(soundStream, volume);
        }

        public void SetGlobalVolume(int volume)
        {
            CSound.SetGlobalVolume(volume);
        }

        public bool IsPaused(int streamId)
        {
            return CSound.IsPaused(streamId);
        }
    }

    class CBcover : ICover
    {
        public CTextureRef GetNoCover()
        {
            return CCover.NoCover;
        }

        public CTextureRef GenerateCover(string text, ECoverGeneratorType type, CSong firstSong)
        {
            return CCover.GenerateCover(text, type, firstSong);
        }
    }

    class CBdataBase : IDataBase
    {
        public bool GetCover(string fileName, ref CTextureRef texture, int coverSize)
        {
            return CDataBase.GetCover(fileName, ref texture, coverSize);
        }

        public bool GetDataBaseSongInfos(string artist, string title, out int numPlayed, out DateTime dateAdded, out int highscoreId)
        {
            return CDataBase.GetDataBaseSongInfos(artist, title, out numPlayed, out dateAdded, out highscoreId);
        }
    }

    class CBcontrollers : IControllers
    {
        public void SetRumble(float duration)
        {
            CController.SetRumble(duration);
        }
    }

    class CBplaylist : IPlaylist
    {
        public void SetName(int playlistId, string name)
        {
            CPlaylists.SetName(playlistId, name);
        }

        public List<int> GetIds()
        {
            return CPlaylists.Ids;
        }

        public List<string> GetNames()
        {
            return CPlaylists.Names;
        }

        public bool Exists(int playlistId)
        {
            return CPlaylists.Get(playlistId) != null;
        }

        public string GetName(int playlistId)
        {
            return CPlaylists.GetName(playlistId);
        }

        public void Delete(int playlistId)
        {
            CPlaylists.Delete(playlistId);
        }

        public void Save(int playlistId)
        {
            CPlaylists.Save(playlistId);
        }

        public int GetNumPlaylists()
        {
            return CPlaylists.NumPlaylists;
        }

        public void AddSong(int playlistId, int songId)
        {
            CPlaylists.AddSong(playlistId, songId);
        }

        public void AddSong(int playlistId, int songId, EGameMode gameMode)
        {
            CPlaylists.AddSong(playlistId, songId, gameMode);
        }

        public void InsertSong(int playlistId, int positionIndex, int songId, EGameMode gameMode)
        {
            CPlaylists.InsertSong(playlistId, positionIndex, songId, gameMode);
        }

        public void MoveSong(int playlistId, int sourceIndex, int destIndex)
        {
            CPlaylists.MoveSong(playlistId, sourceIndex, destIndex);
        }

        public void MoveSongDown(int playlistId, int songIndex)
        {
            CPlaylists.MovePSongDown(playlistId, songIndex);
        }

        public void MoveSongUp(int playlistId, int songIndex)
        {
            CPlaylists.MoveSongUp(playlistId, songIndex);
        }

        public void DeleteSong(int playlistId, int songIndex)
        {
            CPlaylists.DeleteSong(playlistId, songIndex);
        }

        public int GetSongCount(int playlistId)
        {
            return CPlaylists.GetSongCount(playlistId);
        }

        public CPlaylistSong GetSong(int playlistId, int songIndex)
        {
            return CPlaylists.GetSong(playlistId, songIndex);
        }

        public bool ContainsSong(int playlistId, int songId)
        {
            return CPlaylists.ContainsSong(playlistId, songId);
        }
    }
}