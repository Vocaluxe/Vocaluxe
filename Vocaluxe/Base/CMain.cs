#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Xml;
using Vocaluxe.Base.Fonts;
using VocaluxeLib;
using VocaluxeLib.Game;
using VocaluxeLib.Draw;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;
using VocaluxeLib.Profile;

namespace Vocaluxe.Base
{
    static class CMain
    {
        private static readonly IConfig _Config = new CBconfig();
        private static readonly ISettings _Settings = new CBsettings();
        private static readonly ITheme _Theme = new CBtheme();
        private static readonly IBackgroundMusic _BackgroundMusic = new CBbackgroundMusic();
        private static readonly IDrawing _Draw = new CBdraw();
        private static readonly IGraphics _Graphics = new CBGraphics();
        private static readonly ILog _Log = new CBlog();
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
            CBase.Assign(_Config, _Settings, _Theme, _Log, _BackgroundMusic, _Draw, _Graphics, _Fonts, _Language,
                         _Game, _Profiles, _Record, _Songs, _Video, _Sound, _Cover, _DataBase, _Controller, _Playlist);
        }
    }

    class CBconfig : IConfig
    {
        public void SetBackgroundMusicVolume(int newVolume)
        {
            CConfig.BackgroundMusicVolume = newVolume;
        }

        public int GetBackgroundMusicVolume()
        {
            return CConfig.BackgroundMusicVolume;
        }

        public int GetPreviewMusicVolume()
        {
            return CConfig.PreviewMusicVolume;
        }

        public ESongMenu GetSongMenuType()
        {
            return CConfig.SongMenu;
        }

        public EOffOn GetVideosToBackground()
        {
            return CConfig.VideosToBackground;
        }

        public EOffOn GetVideoBackgrounds()
        {
            return CConfig.VideoBackgrounds;
        }

        public EOffOn GetVideoPreview()
        {
            return CConfig.VideoPreview;
        }

        public EOffOn GetDrawNoteLines()
        {
            return CConfig.DrawNoteLines;
        }

        public EOffOn GetDrawToneHelper()
        {
            return CConfig.DrawToneHelper;
        }

        public int GetCoverSize()
        {
            return CConfig.CoverSize;
        }

        public IEnumerable<string> GetSongFolder()
        {
            return CConfig.SongFolder;
        }

        public ESongSorting GetSongSorting()
        {
            return CConfig.SongSorting;
        }

        public EOffOn GetTabs()
        {
            return CConfig.Tabs;
        }

        public EOffOn GetIgnoreArticles()
        {
            return CConfig.IgnoreArticles;
        }

        public bool IsMicConfigured(int playerNr)
        {
            return CConfig.IsMicConfig(playerNr);
        }

        public int GetMaxNumMics()
        {
            return CConfig.GetMaxNumMics();
        }

        public XmlWriterSettings GetXMLSettings()
        {
            return CConfig.XMLSettings;
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

        public EGameState GetGameState()
        {
            return CSettings.GameState;
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
            return CSettings.FolderProfiles;
        }

        public string GetDataPath()
        {
            return CSettings.DataPath;
        }
    }

    class CBtheme : ITheme
    {
        public string GetThemeScreensPath(int partyModeID)
        {
            return CTheme.GetThemeScreensPath(partyModeID);
        }

        public int GetSkinIndex(int partyModeID)
        {
            return CTheme.GetSkinIndex(partyModeID);
        }

        public CTexture GetSkinTexture(string textureName, int partyModeID)
        {
            return CTheme.GetSkinTexture(textureName, partyModeID);
        }

        public CTexture GetSkinVideoTexture(string videoName, int partyModeID)
        {
            return CTheme.GetSkinVideoTexture(videoName, partyModeID);
        }

        public void SkinVideoResume(string videoName, int partyModeID)
        {
            CTheme.SkinVideoResume(videoName, partyModeID);
        }

        public void SkinVideoPause(string videoName, int partyModeID)
        {
            CTheme.SkinVideoPause(videoName, partyModeID);
        }

        public SColorF GetColor(string colorName, int partyModeID)
        {
            return CTheme.GetColor(colorName, partyModeID);
        }

        public bool GetColor(string colorName, int skinIndex, out SColorF color)
        {
            return CTheme.GetColor(colorName, skinIndex, out color);
        }

        public SColorF GetPlayerColor(int playerNr)
        {
            return CTheme.GetPlayerColor(playerNr);
        }

        public void UnloadSkins()
        {
            CTheme.UnloadSkins();
        }

        public void ListSkins()
        {
            CTheme.ListSkins();
        }

        public void LoadSkins()
        {
            CTheme.LoadSkins();
        }

        public void LoadTheme()
        {
            CTheme.LoadTheme();
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

        public void SetStatus(bool disabled)
        {
            CBackgroundMusic.Disabled = disabled;
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

        public void ApplyVolume()
        {
            CBackgroundMusic.ApplyVolume();
        }

        public CTexture GetVideoTexture()
        {
            return CBackgroundMusic.GetVideoTexture();
        }
    }

    class CBdraw : IDrawing
    {
        public void DrawTexture(CTexture texture, SRectF rect)
        {
            CDraw.DrawTexture(texture, rect);
        }

        public void DrawTexture(CTexture texture, SRectF rect, SColorF color)
        {
            CDraw.DrawTexture(texture, rect, color);
        }

        public void DrawTexture(CTexture texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored = false)
        {
            CDraw.DrawTexture(texture, rect, color, bounds, mirrored);
        }

        public void DrawTextureReflection(CTexture texture, SRectF rect, SColorF color, SRectF bounds, float reflectionSpace, float reflectionHeight)
        {
            CDraw.DrawTextureReflection(texture, rect, color, bounds, reflectionSpace, reflectionHeight);
        }

        public CTexture AddTexture(string fileName)
        {
            return CDraw.AddTexture(fileName);
        }

        public void RemoveTexture(ref CTexture texture)
        {
            CDraw.RemoveTexture(ref texture);
        }

        public void DrawColor(SColorF color, SRectF rect)
        {
            CDraw.DrawColor(color, rect);
        }

        public void DrawColorReflection(SColorF color, SRectF rect, float space, float height)
        {
            CDraw.DrawColorReflection(color, rect, space, height);
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

        public void FadeTo(EScreens nextScreen)
        {
            CGraphics.FadeTo(nextScreen);
        }

        public float GetGlobalAlpha()
        {
            return CGraphics.GlobalAlpha;
        }
    }

    class CBlog : ILog
    {
        public void LogError(string errorText)
        {
            CLog.LogError(errorText);
        }
    }

    class CBfonts : IFonts
    {
        public void SetFont(string fontName)
        {
            CFonts.SetFont(fontName);
        }

        public void SetStyle(EStyle fontStyle)
        {
            CFonts.Style = fontStyle;
        }

        public RectangleF GetTextBounds(CText text)
        {
            return CFonts.GetTextBounds(text);
        }

        public RectangleF GetTextBounds(CText text, float textHeight)
        {
            return CFonts.GetTextBounds(text, textHeight);
        }

        public void DrawText(string text, float textHeight, float x, float y, float z, SColorF color)
        {
            CFonts.DrawText(text, textHeight, x, y, z, color);
        }

        public void DrawTextReflection(string text, float textHeight, float x, float y, float z, SColorF color, float reflectionSpace, float reflectionHeight)
        {
            CFonts.DrawTextReflection(text, textHeight, x, y, z, color, reflectionSpace, reflectionHeight);
        }

        public void DrawText(string text, float textHeight, float x, float y, float z, SColorF color, float begin, float end)
        {
            CFonts.DrawText(text, textHeight, x, y, z, color, begin, end);
        }
    }

    class CBlanguage : ILanguage
    {
        public string Translate(string keyWord)
        {
            return CLanguage.Translate(keyWord);
        }

        public string Translate(string keyWord, int partyModeID)
        {
            return CLanguage.Translate(keyWord, partyModeID);
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
            return CGame.NumPlayer;
        }

        public void SetNumPlayer(int numPlayer)
        {
            CGame.NumPlayer = numPlayer;
        }

        public SPlayer[] GetPlayers()
        {
            return CGame.Players;
        }

        public CPoints GetPoints()
        {
            return CGame.GetPoints();
        }

        public float GetMidBeatD()
        {
            return CGame.MidBeatD;
        }

        public int GetCurrentBeatD()
        {
            return CGame.ActBeatD;
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

        public void AddSong(int songID, EGameMode gameMode)
        {
            CGame.AddSong(songID, gameMode);
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
    }

    class CBprofiles : IProfiles
    {
        public CProfile[] GetProfiles()
        {
            return CProfiles.GetProfiles();
        }

        public EGameDifficulty GetDifficulty(int profileID)
        {
            return CProfiles.GetDifficulty(profileID);
        }

        public string GetPlayerName(int profileID, int playerNum = 0)
        {
            return CProfiles.GetPlayerName(profileID, playerNum);
        }

        public CTexture GetAvatar(int profileID)
        {
            return CProfiles.GetAvatarTextureFromProfile(profileID);
        }
    }

    class CBrecord : IRecording
    {
        public int GetToneAbs(int playerNr)
        {
            return CSound.RecordGetToneAbs(playerNr);
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

        public CSong GetSongByID(int songID)
        {
            return CSongs.GetSong(songID);
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

        public void AddPartySongSung(int songID)
        {
            CSongs.AddPartySongSung(songID);
        }

        public void ResetSongSung()
        {
            CSongs.ResetPartySongSung();
        }

        public void ResetSongSung(int catIndex)
        {
            CSongs.ResetPartySongSung(catIndex);
        }

        public void SortSongs(ESongSorting sorting, EOffOn tabs, EOffOn ignoreArticles, String searchString, EDuetOptions duetOptions)
        {
            CSongs.Sort(sorting, tabs, ignoreArticles, searchString, duetOptions);
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
        public int Load(string videoFileName)
        {
            return CVideo.Load(videoFileName);
        }

        public bool Skip(int videoStream, float startPosition, float videoGap)
        {
            return CVideo.Skip(videoStream, startPosition, videoGap);
        }

        public bool GetFrame(int videoStream, ref CTexture videoTexture, float time, out float videoTime)
        {
            return CVideo.GetFrame(videoStream, ref videoTexture, time, out videoTime);
        }

        public bool IsFinished(int videoStream)
        {
            return CVideo.Finished(videoStream);
        }

        public bool Close(int videoStream)
        {
            return CVideo.Close(videoStream);
        }
    }

    class CBsound : ISound
    {
        public int Load(string soundFile, bool prescan)
        {
            return CSound.Load(soundFile, prescan);
        }

        public void SetPosition(int soundStream, float newPosition)
        {
            CSound.SetPosition(soundStream, newPosition);
        }

        public void Play(int soundStream)
        {
            CSound.Play(soundStream);
        }

        public void Fade(int soundStream, float targetVolume, float duration)
        {
            CSound.Fade(soundStream, targetVolume, duration);
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

        public void FadeAndStop(int soundStream, float targetVolume, float duration)
        {
            CSound.FadeAndStop(soundStream, targetVolume, duration);
        }

        public void SetStreamVolume(int soundStream, float volume)
        {
            CSound.SetStreamVolume(soundStream, volume);
        }

        public void SetStreamVolumeMax(int soundStream, float maxVolume)
        {
            CSound.SetStreamVolumeMax(soundStream, maxVolume);
        }
    }

    class CBcover : ICover
    {
        public CTexture GetNoCover()
        {
            return CCover.NoCover;
        }
    }

    class CBdataBase : IDataBase
    {
        public bool GetCover(string fileName, ref CTexture texture, int coverSize)
        {
            return CDataBase.GetCover(fileName, ref texture, coverSize);
        }

        public bool GetDataBaseSongInfos(string artist, string title, out int numPlayed, out string dateAdded, out int highscoreID)
        {
            return CDataBase.GetDataBaseSongInfos(artist, title, out numPlayed, out dateAdded, out highscoreID);
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
        public void SetPlaylistName(int playlistID, string name)
        {
            CPlaylists.SetPlaylistName(playlistID, name);
        }

        public List<string> GetPlaylistNames()
        {
            return CPlaylists.PlaylistNames;
        }

        public string GetPlaylistName(int playlistID)
        {
            return CPlaylists.GetPlaylistName(playlistID);
        }

        public void DeletePlaylist(int playlistID)
        {
            CPlaylists.DeletePlaylist(playlistID);
        }

        public void SavePlaylist(int playlistID)
        {
            CPlaylists.SavePlaylist(playlistID);
        }

        public int GetNumPlaylists()
        {
            return CPlaylists.NumPlaylists;
        }

        public void AddPlaylistSong(int playlistID, int songID)
        {
            CPlaylists.AddPlaylistSong(playlistID, songID);
        }

        public void AddPlaylistSong(int playlistID, int songID, EGameMode gameMode)
        {
            CPlaylists.AddPlaylistSong(playlistID, songID, gameMode);
        }

        public void InsertPlaylistSong(int playlistID, int positionIndex, int songID, EGameMode gameMode)
        {
            CPlaylists.InsertPlaylistSong(playlistID, positionIndex, songID, gameMode);
        }

        public void MovePlaylistSong(int playlistID, int sourceIndex, int destIndex)
        {
            CPlaylists.MovePlaylistSong(playlistID, sourceIndex, destIndex);
        }

        public void MovePlaylistSongDown(int playlistID, int songIndex)
        {
            CPlaylists.MovePlaylistSongDown(playlistID, songIndex);
        }

        public void MovePlaylistSongUp(int playlistID, int songIndex)
        {
            CPlaylists.MovePlaylistSongUp(playlistID, songIndex);
        }

        public void DeletePlaylistSong(int playlistID, int songIndex)
        {
            CPlaylists.DeletePlaylistSong(playlistID, songIndex);
        }

        public int GetPlaylistSongCount(int playlistID)
        {
            return CPlaylists.GetPlaylistSongCount(playlistID);
        }

        public CPlaylistSong GetPlaylistSong(int playlistID, int songIndex)
        {
            return CPlaylists.GetPlaylistSong(playlistID, songIndex);
        }
    }
}