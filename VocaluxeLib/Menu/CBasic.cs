using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.Menu
{
    public static class CBase
    {
        public static Basic Base;
    }

    public class Basic
    {
        public IConfig Config;
        public ISettings Settings;
        public ITheme Theme;
        public IHelper Helper;
        public ILog Log;
        public IBackgroundMusic BackgroundMusic;
        public IDrawing Drawing;
        public IGraphics Graphics;
        public IFonts Fonts;
        public ILanguage Language;
        public IGame Game;
        public IProfiles Profiles;
        public IRecording Record;
        public ISongs Songs;
        public IVideo Video;
        public ISound Sound;
        public ICover Cover;
        public IDataBase DataBase;
        public IInputs Input;
        public IPlaylist Playlist;

        public Basic(IConfig Config, ISettings Settings, ITheme Theme, IHelper Helper, ILog Log, IBackgroundMusic BackgroundMusic,
            IDrawing Draw, IGraphics Graphics, IFonts Fonts, ILanguage Language, IGame Game, IProfiles Profiles, IRecording Record,
            ISongs Songs, IVideo Video, ISound Sound, ICover Cover, IDataBase DataBase, IInputs Input, IPlaylist Playlist)
        {
            this.Config = Config;
            this.Settings = Settings;
            this.Theme = Theme;
            this.Helper = Helper;
            this.Log = Log;
            this.BackgroundMusic = BackgroundMusic;
            this.Drawing = Draw;
            this.Graphics = Graphics;
            this.Fonts = Fonts;
            this.Language = Language;
            this.Game = Game;
            this.Profiles = Profiles;
            this.Record = Record;
            this.Songs = Songs;
            this.Video = Video;
            this.Sound = Sound;
            this.Cover = Cover;
            this.DataBase = DataBase;
            this.Input = Input;
            this.Playlist = Playlist;
        }
    }
}
