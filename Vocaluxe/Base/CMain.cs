using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using Vocaluxe.GameModes;
using Vocaluxe.Menu;
using Vocaluxe.Menu.SongMenu;

namespace Vocaluxe.Base
{
    static class CMain
    {
        public static IConfig Config = new BConfig();
        public static ISettings Settings = new BSettings();
        public static ITheme Theme = new BTheme();
        public static IHelper Helper = new BHelper();
        public static IBackgroundMusic BackgroundMusic = new BBackgroundMusic();
        public static IDrawing Draw = new BDraw();
        public static IGraphics Graphics = new BGraphics();
        public static ILog Log = new BLog();
        public static IFonts Fonts = new BFonts();
        public static ILanguage Language = new BLanguage();
        public static IGame Game = new BGame();
        public static IProfiles Profiles = new BProfiles();
        public static IRecording Record = new BRecord();
        public static ISongs Songs = new BSongs();
        public static IVideo Video = new BVideo();
        public static ISound Sound = new BSound();
        public static ICover Cover = new BCover();
        public static IDataBase DataBase = new BDataBase();
        public static IInputs Input = new BInputs();
        public static IPlaylist Playlist = new BPlaylist();

        public static void Init()
        {
            CBase.Assign(Config, Settings, Theme, Helper, Log, BackgroundMusic, Draw, Graphics, Fonts, Language,
                Game, Profiles, Record, Songs, Video, Sound, Cover, DataBase, Input, Playlist);
        }
    }

    class BConfig : IConfig
    {
        public void SetBackgroundMusicVolume(int NewVolume)
        {
            if (NewVolume < 0)
                CConfig.BackgroundMusicVolume = 0;
            else if (NewVolume > 100)
                CConfig.BackgroundMusicVolume = 100;
            else
                CConfig.BackgroundMusicVolume = NewVolume;

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

        public bool IsMicConfigured(int PlayerNr)
        {
            return CConfig.IsMicConfig(PlayerNr);
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

    class BSettings : ISettings
    {
        public int GetRenderW()
        {
            return CSettings.iRenderW;
        }

        public int GetRenderH()
        {
            return CSettings.iRenderH;
        }

        public bool IsTabNavigation()
        {
            return CSettings.TabNavigation;
        }

        public float GetZFar()
        {
            return CSettings.zFar;
        }

        public float GetZNear()
        {
            return CSettings.zNear;
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

    class BTheme : ITheme
    {
        public string GetThemeScreensPath(int PartyModeID)
        {
            return CTheme.GetThemeScreensPath(PartyModeID);
        }

        public int GetSkinIndex(int PartyModeID)
        {
            return CTheme.GetSkinIndex(PartyModeID);
        }

        public STexture GetSkinTexture(string TextureName, int PartyModeID)
        {
            return CTheme.GetSkinTexture(TextureName, PartyModeID);
        }

        public STexture GetSkinVideoTexture(string VideoName, int PartyModeID)
        {
            return CTheme.GetSkinVideoTexture(VideoName, PartyModeID);
        }

        public void SkinVideoResume(string VideoName, int PartyModeID)
        {
            CTheme.SkinVideoResume(VideoName, PartyModeID);
        }

        public void SkinVideoPause(string VideoName, int PartyModeID)
        {
            CTheme.SkinVideoPause(VideoName, PartyModeID);
        }

        public SColorF GetColor(string ColorName, int PartyModeID)
        {
            return CTheme.GetColor(ColorName, PartyModeID);
        }

        public bool GetColor(string ColorName, int SkinIndex, ref SColorF Color)
        {
            return CTheme.GetColor(ColorName, SkinIndex, ref Color);
        }

        public SColorF GetPlayerColor(int PlayerNr)
        {
            return CTheme.GetPlayerColor(PlayerNr);
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

    class BHelper : IHelper
    {
    }

    class BBackgroundMusic : IBackgroundMusic
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

        public void SetStatus(bool Disabled)
        {
            CBackgroundMusic.Disabled = Disabled;
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

    class BDraw : IDrawing
    {
        public RectangleF GetTextBounds(CText text)
        {
            return CDraw.GetTextBounds(text);
        }

        public void DrawTexture(STexture Texture, SRectF Rect)
        {
            CDraw.DrawTexture(Texture, Rect);
        }

        public void DrawTexture(STexture Texture, SRectF Rect, SColorF Color)
        {
            CDraw.DrawTexture(Texture, Rect, Color);
        }

        public void DrawTexture(STexture Texture, SRectF Rect, SColorF Color, SRectF Bounds)
        {
            CDraw.DrawTexture(Texture, Rect, Color, Bounds);
        }

        public void DrawTexture(STexture Texture, SRectF Rect, SColorF Color, SRectF Bounds, bool Mirrored)
        {
            CDraw.DrawTexture(Texture, Rect, Color, Bounds, Mirrored);
        }

        public void DrawTextureReflection(STexture Texture, SRectF Rect, SColorF Color, SRectF Bounds, float ReflectionSpace, float ReflectionHeight)
        {
            CDraw.DrawTextureReflection(Texture, Rect, Color, Bounds, ReflectionSpace, ReflectionHeight);
        }

        public void RemoveTexture(ref STexture Texture)
        {
            CDraw.RemoveTexture(ref Texture);
        }

        public void DrawColor(SColorF Color, SRectF Rect)
        {
            CDraw.DrawColor(Color, Rect);
        }

        public void DrawColorReflection(SColorF Color, SRectF Rect, float Space, float Height)
        {
            CDraw.DrawColorReflection(Color, Rect, Space, Height);
        }
    }

    class BGraphics : IGraphics
    {
        public void ReloadTheme()
        {
            CGraphics.ReloadTheme();
        }

        public void SaveTheme()
        {
            CGraphics.SaveTheme();
        }

        public void FadeTo(EScreens NextScreen)
        {
            CGraphics.FadeTo(NextScreen);
        }

        public float GetGlobalAlpha()
        {
            return CGraphics.GlobalAlpha;
        }
    }

    class BLog : ILog
    {
        public void LogError(string ErrorText)
        {
            CLog.LogError(ErrorText);
        }
    }

    class BFonts : IFonts
    {
        public void SetFont(string FontName)
        {
            CFonts.SetFont(FontName);
        }

        public void SetStyle(EStyle FontStyle)
        {
            CFonts.Style = FontStyle;
        }

        public RectangleF GetTextBounds(CText Text, float TextHeight)
        {
            return CFonts.GetTextBounds(Text, TextHeight);
        }

        public void DrawText(string Text, float TextHeight, float x, float y, float z, SColorF Color)
        {
            CFonts.DrawText(Text, TextHeight, x, y, z, Color);
        }

        public void DrawTextReflection(string Text, float TextHeight, float x, float y, float z, SColorF Color, float ReflectionSpace, float ReflectionHeight)
        {
            CFonts.DrawTextReflection(Text, TextHeight, x, y, z, Color, ReflectionSpace, ReflectionHeight);
        }

        public void DrawText(string Text, float TextHeight, float x, float y, float z, SColorF Color, float Begin, float End)
        {
            CFonts.DrawText(Text, TextHeight, x, y, z, Color, Begin, End);
        }
    }

    class BLanguage : ILanguage
    {
        public string Translate(string KeyWord)
        {
            return CLanguage.Translate(KeyWord);
        }

        public string Translate(string KeyWord, int PartyModeID)
        {
            return CLanguage.Translate(KeyWord, PartyModeID);
        }

        public bool TranslationExists(string KeyWord)
        {
            return CLanguage.TranslationExists(KeyWord);
        }
    }

    class BGame : IGame
    {
        public int GetNumPlayer()
        {
            return CGame.NumPlayer;
        }

        public void SetNumPlayer(int NumPlayer)
        {
            CGame.NumPlayer = NumPlayer;
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

        public int GetRandom(int Max)
        {
            return CGame.Rand.Next(Max);
        }

        public double GetRandomDouble()
        {
            return CGame.Rand.NextDouble();
        }

        public float GetTimeFromBeats(float Beat, float BPM)
        {
            return CGame.GetTimeFromBeats(Beat, BPM);
        }

        public void AddSong(int SongID, EGameMode GameMode)
        {
            CGame.AddSong(SongID, GameMode);
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

    class BProfiles : IProfiles
    {
        public SProfile[] GetProfiles()
        {
            return CProfiles.Profiles;
        }
    }

    class BRecord : IRecording
    {
        public int GetToneAbs(int PlayerNr)
        {
            return CSound.RecordGetToneAbs(PlayerNr);
        }
    }

    class BSongs : ISongs
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

        public int NumSongsInCategory(int CategoryIndex)
        {
            return CSongs.NumSongsInCategory(CategoryIndex);
        }

        public int GetCurrentCategoryIndex()
        {
            return CSongs.Category;
        }

        public EOffOn GetTabs()
        {
            return CSongs.Tabs;
        }

        public string GetSearchFilter()
        {
            return CSongs.SearchFilter;
        }

        public void SetCategory(int CategoryIndex)
        {
            CSongs.Category = CategoryIndex;
        }

        public void UpdateRandomSongList()
        {
            CSongs.UpdateRandomSongList();
        }

        public CSong GetVisibleSong(int VisibleIndex)
        {
            if (VisibleIndex >= CSongs.NumVisibleSongs)
                return null;

            CSong song = CSongs.VisibleSongs[VisibleIndex];
            //Flamefire: Why copy the song-instance??? Cover and stuff will be loaded twice!
            //And for unknown reason causes no-big-cover bug if covers are not loaded on startup
            return song;
            //if (song == null)
            //    return null;

            //return new CSong(song);
        }

        public CSong GetSongByID(int SongID)
        {
            //Flamefire: Why copy?
            //return new CSong(CSongs.GetSong(SongID));
            return CSongs.GetSong(SongID);
        }

        public CSong[] GetSongs()
        {
            return CSongs.AllSongs;
        }

        public CSong[] GetSongsNotSung()
        {
            return CSongs.SongsNotSung;
        }

        public CCategory GetCategory(int Index)
        {
            if (Index >= CSongs.NumCategories)
                return null;

            return new CCategory(CSongs.Categories[Index]);
        }

        public void AddPartySongSung(int SongID)
        {
            CSongs.AddPartySongSung(SongID);
        }

        public void ResetPartySongSung()
        {
            CSongs.ResetPartySongSung();
        }

        public void ResetPartySongSung(int CatIndex)
        {
            CSongs.ResetPartySongSung(CatIndex);
        }

        public void SortSongs(ESongSorting Sorting, EOffOn Tabs, EOffOn IgnoreArticles, String SearchString, bool ShowDuetSongs)
        {
            CSongs.Sort(Sorting, Tabs, IgnoreArticles, SearchString, ShowDuetSongs);
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

    class BVideo : IVideo
    {
        public int Load(string VideoFileName)
        {
            return CVideo.VdLoad(VideoFileName);
        }

        public bool Skip(int VideoStream, float StartPosition, float VideoGap)
        {
            return CVideo.VdSkip(VideoStream, StartPosition, VideoGap);
        }

        public bool GetFrame(int VideoStream, ref STexture VideoTexture, float Time, ref float VideoTime)
        {
            return CVideo.VdGetFrame(VideoStream, ref VideoTexture, Time, ref VideoTime);
        }

        public bool IsFinished(int VideoStream)
        {
            return CVideo.VdFinished(VideoStream);
        }

        public bool Close(int VideoStream)
        {
            return CVideo.VdClose(VideoStream);
        }

    }

    class BSound : ISound
    {
        public int Load(string SoundFile, bool Prescan)
        {
            return CSound.Load(SoundFile, Prescan);
        }

        public void SetPosition(int SoundStream, float NewPosition)
        {
            CSound.SetPosition(SoundStream, NewPosition);
        }

        public void Play(int SoundStream)
        {
            CSound.Play(SoundStream);
        }

        public void Fade(int SoundStream, float TargetVolume, float Duration)
        {
            CSound.Fade(SoundStream, TargetVolume, Duration);
        }

        public bool IsFinished(int SoundStream)
        {
            return CSound.IsFinished(SoundStream);
        }

        public float GetPosition(int SoundStream)
        {
            return CSound.GetPosition(SoundStream);
        }

        public float GetLength(int SoundStream)
        {
            return CSound.GetLength(SoundStream);
        }

        public void FadeAndStop(int SoundStream, float TargetVolume, float Duration)
        {
            CSound.FadeAndStop(SoundStream, TargetVolume, Duration);
        }

        public void SetStreamVolume(int SoundStream, float Volume)
        {
            CSound.SetStreamVolume(SoundStream, Volume);
        }

        public void SetStreamVolumeMax(int SoundStream, float MaxVolume)
        {
            CSound.SetStreamVolumeMax(SoundStream, MaxVolume);
        }
    }

    class BCover : ICover
    {
        public STexture GetNoCover()
        {
            return CCover.NoCover;
        }
    }

    class BDataBase : IDataBase
    {
        public bool GetCover(string FileName, ref STexture Texture, int CoverSize)
        {
            return CDataBase.GetCover(FileName, ref Texture, CoverSize);
        }
    }

    class BInputs : IInputs
    {
        public void SetRumble(float Duration)
        {
            CInput.SetRumble(Duration);
        }
    }

    class BPlaylist : IPlaylist
    {
        public void SetPlaylistName(int PlaylistID, string Name)
        {
            CPlaylists.SetPlaylistName(PlaylistID, Name);
        }

        public string[] GetPlaylistNames()
        {
            return CPlaylists.GetPlaylistNames();
        }

        public string GetPlaylistName(int PlaylistID)
        {
            return CPlaylists.GetPlaylistName(PlaylistID);
        }

        public void DeletePlaylist(int PlaylistID)
        {
            CPlaylists.DeletePlaylist(PlaylistID);
        }

        public void SavePlaylist(int PlaylistID)
        {
            CPlaylists.SavePlaylist(PlaylistID);
        }

        public int GetNumPlaylists()
        {
            return CPlaylists.NumPlaylists;
        }



        public void AddPlaylistSong(int PlaylistID, int SongID)
        {
            CPlaylists.AddPlaylistSong(PlaylistID, SongID);
        }

        public void AddPlaylistSong(int PlaylistID, int SongID, EGameMode GameMode)
        {
            CPlaylists.AddPlaylistSong(PlaylistID, SongID, GameMode);
        }

        public void InsertPlaylistSong(int PlaylistID, int PositionIndex, int SongID, EGameMode GameMode)
        {
            CPlaylists.InsertPlaylistSong(PlaylistID, PositionIndex, SongID, GameMode);
        }

        public void MovePlaylistSong(int PlaylistID, int SourceIndex, int DestIndex)
        {
            CPlaylists.MovePlaylistSong(PlaylistID, SourceIndex, DestIndex);
        }

        public void MovePlaylistSongDown(int PlaylistID, int SongIndex)
        {
            CPlaylists.MovePlaylistSongDown(PlaylistID, SongIndex);
        }

        public void MovePlaylistSongUp(int PlaylistID, int SongIndex)
        {
            CPlaylists.MovePlaylistSongUp(PlaylistID, SongIndex);
        }

        public void DeletePlaylistSong(int PlaylistID, int SongIndex)
        {
            CPlaylists.DeletePlaylistSong(PlaylistID, SongIndex);
        }

        public int GetPlaylistSongCount(int PlaylistID)
        {
            return CPlaylists.GetPlaylistSongCount(PlaylistID);
        }

        public CPlaylistSong GetPlaylistSong(int PlaylistID, int SongIndex)
        {
            return CPlaylists.GetPlaylistSong(PlaylistID, SongIndex);
        }
    }
}
