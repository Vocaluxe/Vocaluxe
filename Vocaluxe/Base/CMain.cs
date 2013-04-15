using System;
using System.Collections.Generic;
using System.Drawing;
using VocaluxeLib.Menu;
using VocaluxeLib.Menu.SongMenu;

namespace Vocaluxe.Base
{
    static class CMain
    {
        public static IConfig Config = new CBconfig();
        public static ISettings Settings = new CBsettings();
        public static ITheme Theme = new CBtheme();
        public static IHelper Helper = new CBhelper();
        public static IBackgroundMusic BackgroundMusic = new CBbackgroundMusic();
        public static IDrawing Draw = new CBdraw();
        public static IGraphics Graphics = new CBGraphics();
        public static ILog Log = new CBlog();
        public static IFonts Fonts = new CBfonts();
        public static ILanguage Language = new CBlanguage();
        public static IGame Game = new CBGame();
        public static IProfiles Profiles = new CBprofiles();
        public static IRecording Record = new CBrecord();
        public static ISongs Songs = new CBsongs();
        public static IVideo Video = new CBvideo();
        public static ISound Sound = new CBsound();
        public static ICover Cover = new CBcover();
        public static IDataBase DataBase = new CBdataBase();
        public static IInputs Input = new CBinputs();
        public static IPlaylist Playlist = new CBplaylist();

        public static void Init()
        {
            CBase.Assign(Config, Settings, Theme, Helper, Log, BackgroundMusic, Draw, Graphics, Fonts, Language,
                         Game, Profiles, Record, Songs, Video, Sound, Cover, DataBase, Input, Playlist);
        }
    }

    class CBconfig : IConfig
    {
        public void SetBackgroundMusicVolume(int newVolume)
        {
            if (newVolume < 0)
                CConfig.BackgroundMusicVolume = 0;
            else if (newVolume > 100)
                CConfig.BackgroundMusicVolume = 100;
            else
                CConfig.BackgroundMusicVolume = newVolume;

            CConfig.SaveConfig();
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

        public List<string> GetSongFolder()
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
            int max = 0;
            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
            {
                if (CConfig.IsMicConfig(i + 1))
                    max = i + 1;
                else
                    break;
            }
            return max;
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

        public STexture GetSkinTexture(string textureName, int partyModeID)
        {
            return CTheme.GetSkinTexture(textureName, partyModeID);
        }

        public STexture GetSkinVideoTexture(string videoName, int partyModeID)
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

    class CBhelper : IHelper {}

    class CBbackgroundMusic : IBackgroundMusic
    {
        public bool IsDisabled()
        {
            return CBackgroundMusic.Disabled;
        }

        public bool IsPlaying()
        {
            return CBackgroundMusic.Playing;
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

        public STexture GetVideoTexture()
        {
            return CBackgroundMusic.GetVideoTexture();
        }
    }

    class CBdraw : IDrawing
    {
        public RectangleF GetTextBounds(CText text)
        {
            return CDraw.GetTextBounds(text);
        }

        public void DrawTexture(STexture texture, SRectF rect)
        {
            CDraw.DrawTexture(texture, rect);
        }

        public void DrawTexture(STexture texture, SRectF rect, SColorF color)
        {
            CDraw.DrawTexture(texture, rect, color);
        }

        public void DrawTexture(STexture texture, SRectF rect, SColorF color, SRectF bounds)
        {
            CDraw.DrawTexture(texture, rect, color, bounds);
        }

        public void DrawTexture(STexture texture, SRectF rect, SColorF color, SRectF bounds, bool mirrored)
        {
            CDraw.DrawTexture(texture, rect, color, bounds, mirrored);
        }

        public void DrawTextureReflection(STexture texture, SRectF rect, SColorF color, SRectF bounds, float reflectionSpace, float reflectionHeight)
        {
            CDraw.DrawTextureReflection(texture, rect, color, bounds, reflectionSpace, reflectionHeight);
        }

        public void RemoveTexture(ref STexture texture)
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

        public SPlayer[] GetPlayer()
        {
            return CGame.Player;
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
        public SProfile[] GetProfiles()
        {
            return CProfiles.Profiles;
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

        public int GetNumVisibleSongs()
        {
            return CSongs.NumVisibleSongs;
        }

        public int GetNumCategories()
        {
            return CSongs.NumCategories;
        }

        public int NumSongsInCategory(int categoryIndex)
        {
            return CSongs.NumSongsInCategory(categoryIndex);
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

        public CSong[] GetSongs()
        {
            return CSongs.AllSongs;
        }

        public CSong[] GetSongsNotSung()
        {
            return CSongs.SongsNotSung;
        }

        public CCategory GetCategory(int index)
        {
            if (index >= CSongs.NumCategories)
                return null;

            return new CCategory(CSongs.Categories[index]);
        }

        public void AddPartySongSung(int songID)
        {
            CSongs.AddPartySongSung(songID);
        }

        public void ResetPartySongSung()
        {
            CSongs.ResetPartySongSung();
        }

        public void ResetPartySongSung(int catIndex)
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
            return CVideo.VdLoad(videoFileName);
        }

        public bool Skip(int videoStream, float startPosition, float videoGap)
        {
            return CVideo.VdSkip(videoStream, startPosition, videoGap);
        }

        public bool GetFrame(int videoStream, ref STexture videoTexture, float time, ref float videoTime)
        {
            return CVideo.VdGetFrame(videoStream, ref videoTexture, time, ref videoTime);
        }

        public bool IsFinished(int videoStream)
        {
            return CVideo.VdFinished(videoStream);
        }

        public bool Close(int videoStream)
        {
            return CVideo.VdClose(videoStream);
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
        public STexture GetNoCover()
        {
            return CCover.NoCover;
        }
    }

    class CBdataBase : IDataBase
    {
        public bool GetCover(string fileName, ref STexture texture, int coverSize)
        {
            return CDataBase.GetCover(fileName, ref texture, coverSize);
        }
    }

    class CBinputs : IInputs
    {
        public void SetRumble(float duration)
        {
            CInput.SetRumble(duration);
        }
    }

    class CBplaylist : IPlaylist
    {
        public void SetPlaylistName(int playlistID, string name)
        {
            CPlaylists.SetPlaylistName(playlistID, name);
        }

        public string[] GetPlaylistNames()
        {
            return CPlaylists.GetPlaylistNames();
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