using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.Menu
{
    public static class CBase
    {
        public static IConfig Config;
        public static ISettings Settings;
        public static ITheme Theme;
        public static IHelper Helper;
        public static ILog Log;
        public static IBackgroundMusic BackgroundMusic;
        public static IDrawing Drawing;
        public static IGraphics Graphics;
        public static IFonts Fonts;
        public static ILanguage Language;
        public static IGame Game;
        public static IProfiles Profiles;
        public static IRecording Record;
        public static ISongs Songs;
        public static IVideo Video;
        public static ISound Sound;
        public static ICover Cover;
        public static IDataBase DataBase;
        public static IInputs Input;
        public static IPlaylist Playlist;

        public static void Assign(IConfig config, ISettings settings, ITheme theme, IHelper helper, ILog log, IBackgroundMusic backgroundMusic,
            IDrawing draw, IGraphics graphics, IFonts fonts, ILanguage language, IGame game, IProfiles profiles, IRecording record,
            ISongs songs, IVideo video, ISound sound, ICover cover, IDataBase dataBase, IInputs input, IPlaylist playlist)
        {
            Config = config;
            Settings = settings;
            Theme = theme;
            Helper = helper;
            Log = log;
            BackgroundMusic = backgroundMusic;
            Drawing = draw;
            Graphics = graphics;
            Fonts = fonts;
            Language = language;
            Game = game;
            Profiles = profiles;
            Record = record;
            Songs = songs;
            Video = video;
            Sound = sound;
            Cover = cover;
            DataBase = dataBase;
            Input = input;
            Playlist = playlist;
        }
    }
}
