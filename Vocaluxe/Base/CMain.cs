using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using Vocaluxe.Menu;

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

        public static Menu.Base Base = new Menu.Base(Config, Settings, Theme, Helper, Log, BackgroundMusic, Draw, Graphics, Fonts, Language, Game, Profiles);
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
    }

    class BTheme : ITheme
    {
        public string GetThemeScreensPath()
        {
            return CTheme.GetThemeScreensPath();
        }

        public int GetSkinIndex()
        {
            return CTheme.GetSkinIndex();
        }

        public STexture GetSkinTexture(string TextureName)
        {
            return CTheme.GetSkinTexture(TextureName);
        }

        public STexture GetSkinVideoTexture(string VideoName)
        {
            return CTheme.GetSkinVideoTexture(VideoName);
        }

        public void SkinVideoResume(string VideoName)
        {
            CTheme.SkinVideoResume(VideoName);
        }

        public void SkinVideoPause(string VideoName)
        {
            CTheme.SkinVideoPause(VideoName);
        }

        public SColorF GetColor(string ColorName)
        {
            return CTheme.GetColor(ColorName);
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

        public void DrawTextureReflection(STexture Texture, SRectF Rect, SColorF Color, SRectF Bounds, float ReflectionSpace, float ReflectionHeight)
        {
            CDraw.DrawTextureReflection(Texture, Rect, Color, Bounds, ReflectionSpace, ReflectionHeight);
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

        public SPlayer[] GetPlayer()
        {
            return CGame.Player;
        }
    }

    class BProfiles : IProfiles
    {
        public SProfile[] GetProfiles()
        {
            return CProfiles.Profiles;
        }
    }
}
