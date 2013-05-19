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

namespace VocaluxeLib
{
    public static class CBase
    {
        public static IConfig Config;
        public static ISettings Settings;
        public static ITheme Theme;
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
        public static IControllers Controller;
        public static IPlaylist Playlist;

        public static void Assign(IConfig config, ISettings settings, ITheme theme, ILog log, IBackgroundMusic backgroundMusic,
                                  IDrawing draw, IGraphics graphics, IFonts fonts, ILanguage language, IGame game, IProfiles profiles, IRecording record,
                                  ISongs songs, IVideo video, ISound sound, ICover cover, IDataBase dataBase, IControllers controller, IPlaylist playlist)
        {
            Config = config;
            Settings = settings;
            Theme = theme;
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
            Controller = controller;
            Playlist = playlist;
        }
    }
}