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

namespace VocaluxeLib
{
    public enum ECoverGeneratorType
    {
        Default,
        Song,
        Folder,
        Artist,
        Letter,
        Edition,
        Genre,
        Language,
        Year,
        Decade,
        Date
    }

    public enum EDirection
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }

    #region Inputs
    [Flags]
    public enum EModifier
    {
        None = 0,
        Shift = 1,
        Alt = 2,
        Ctrl = 4
    }

    public enum ESender
    {
        Mouse,
        Keyboard,
        WiiMote,
        Gamepad
    }
    #endregion Inputs

    #region Fonts
    public enum EAspect
    {
        Crop,
        LetterBox,
        Stretch,
        Zoom1,
        Zoom2
    }

    public enum EGeneralAlignment
    {
        Middle,
        Start,
        End
    }

    public enum EAlignment
    {
        Left,
        Center,
        Right
    }

    public enum EHAlignment
    {
        Top,
        Center,
        Bottom
    }

    public enum EStyle
    {
        Normal,
        Italic,
        Bold,
        BoldItalic
    }
    #endregion Fonts

    #region Config
    public enum ERenderer
    {
        // ReSharper disable InconsistentNaming
#if WIN
        TR_CONFIG_DIRECT3D,
#endif
        TR_CONFIG_OPENGL,
        TR_CONFIG_SOFTWARE
        // ReSharper restore InconsistentNaming
    }

    public enum EAntiAliasingModes
    {
        X0 = 0,
        X2 = 2,
        X4 = 4,
        X8 = 8,
        X16 = 16,
        X32 = 32
    }

    public enum ETextureQuality
    {
        // ReSharper disable InconsistentNaming
        TR_CONFIG_TEXTURE_LOWEST,
        TR_CONFIG_TEXTURE_LOW,
        TR_CONFIG_TEXTURE_MEDIUM,
        TR_CONFIG_TEXTURE_HIGH,
        TR_CONFIG_TEXTURE_HIGHEST
        // ReSharper restore InconsistentNaming
    }

    public enum EOffOn
    {
        // ReSharper disable InconsistentNaming
        TR_CONFIG_OFF,
        TR_CONFIG_ON
        // ReSharper restore InconsistentNaming
    }

    public enum EDebugLevel
    {
        // ReSharper disable InconsistentNaming
        // don't change the order!
        TR_CONFIG_OFF, //no debug infos
        // ReSharper disable UnusedMember.Global
        TR_CONFIG_ONLY_FPS,
        // ReSharper restore UnusedMember.Global
        TR_CONFIG_LEVEL1,
        TR_CONFIG_LEVEL2,
        TR_CONFIG_LEVEL3,
        TR_CONFIG_LEVEL_MAX //all debug infos
        // ReSharper restore InconsistentNaming
    }

    public enum EBufferSize
    {
        // ReSharper disable UnusedMember.Global
        B0 = 0,
        B512 = 512,
        B1024 = 1024,
        B1536 = 1536,
        B2048 = 2048,
        B2560 = 2560,
        B3072 = 3072,
        B3584 = 3584,
        B4096 = 4096
        // ReSharper restore UnusedMember.Global
    }

    public enum EPlaybackLib
    {
        PortAudio,
        OpenAL,
        GstreamerSharp
    }

    public enum EWebcamLib
    {
        AForgeNet
    }

    public enum ERecordLib
    {
#if WIN
        DirectSound,
#endif
        PortAudio
    }

    public enum EVideoDecoder
    {
        FFmpeg
    }

    public enum ESongMenu
    {
        // ReSharper disable InconsistentNaming
        TR_CONFIG_LIST,       // list of covers and artist - title
        //TR_CONFIG_DREIDEL,  //as in ultrastar deluxe
        TR_CONFIG_TILE_BOARD, //chessboard like
        //TR_CONFIG_BOOK      //for playlists
        // ReSharper restore InconsistentNaming
    }

    public enum EDuetOptions
    {
        All,
        Duets,
        NoDuets
    }

    public enum ESongSource
    {
        TR_SONGSOURCE_ALLSONGS,
        TR_SONGSOURCE_CATEGORY,
        TR_SONGSOURCE_PLAYLIST
    }

    public enum ESongSorting
    {
        // ReSharper disable InconsistentNaming
        TR_CONFIG_NONE,
        TR_CONFIG_FOLDER,
        TR_CONFIG_ARTIST,
        TR_CONFIG_ARTIST_LETTER,
        TR_CONFIG_TITLE_LETTER,
        TR_CONFIG_EDITION,
        TR_CONFIG_GENRE,
        TR_CONFIG_LANGUAGE,
        TR_CONFIG_YEAR,
        TR_CONFIG_DECADE,
        TR_CONFIG_DATEADDED
        // ReSharper restore InconsistentNaming
    }

    public enum ECoverLoading
    {
        // ReSharper disable InconsistentNaming
        // ReSharper disable UnusedMember.Global
        TR_CONFIG_COVERLOADING_ONDEMAND,
        // ReSharper restore UnusedMember.Global
        TR_CONFIG_COVERLOADING_ATSTART,
        TR_CONFIG_COVERLOADING_DYNAMIC
        // ReSharper restore InconsistentNaming
    }

    public enum EGameDifficulty
    {
        // ReSharper disable InconsistentNaming
        // ReSharper disable UnusedMember.Global
        TR_CONFIG_EASY,
        TR_CONFIG_NORMAL,
        TR_CONFIG_HARD
        // ReSharper restore UnusedMember.Global
        // ReSharper restore InconsistentNaming
    }

    public enum ETimerMode
    {
        // ReSharper disable InconsistentNaming
        TR_CONFIG_TIMERMODE_CURRENT,
        TR_CONFIG_TIMERMODE_REMAINING,
        TR_CONFIG_TIMERMODE_TOTAL
        // ReSharper restore InconsistentNaming
    }

    public enum ETimerLook
    {
        // ReSharper disable InconsistentNaming
        TR_CONFIG_TIMERLOOK_NORMAL,
        TR_CONFIG_TIMERLOOK_EXPANDED
        // ReSharper restore InconsistentNaming
    }

    public enum EBackgroundMusicSource
    {
        // ReSharper disable InconsistentNaming
        TR_CONFIG_NO_OWN_MUSIC,
        TR_CONFIG_OWN_MUSIC,
        TR_CONFIG_ONLY_OWN_MUSIC
        // ReSharper restore InconsistentNaming
    }

    public enum EBackgroundMusicOffOn
    {
        // ReSharper disable InconsistentNaming
        TR_CONFIG_OFF,
        TR_CONFIG_ONLY_SONG,
        TR_CONFIG_ON
        // ReSharper restore InconsistentNaming
    }

    public enum EPlayerInfo
    {
        // ReSharper disable InconsistentNaming
        TR_CONFIG_PLAYERINFO_BOTH,
        TR_CONFIG_PLAYERINFO_NAME,
        TR_CONFIG_PLAYERINFO_AVATAR,
        // ReSharper disable UnusedMember.Global
        TR_CONFIG_PLAYERINFO_OFF
        // ReSharper restore UnusedMember.Global
        // ReSharper restore InconsistentNaming
    }

    public enum EFadePlayerInfo
    {
        // ReSharper disable InconsistentNaming
        TR_CONFIG_FADEPLAYERINFO_ALL,
        TR_CONFIG_FADEPLAYERINFO_INFO,
        TR_CONFIG_FADEPLAYERINFO_OFF
        // ReSharper restore InconsistentNaming
    }

    public enum ELyricsPosition
    {
        // ReSharper disable InconsistentNaming
        TR_CONFIG_LYRICSPOSITION_BOTTOM,
        TR_CONFIG_LYRICSPOSITION_TOP,
        TR_CONFIG_LYRICSPOSITION_BOTH,
        TR_CONFIG_LYRICSPOSITION_DYNAMIC
        // ReSharper restore InconsistentNaming
    }

    public enum ELyricStyle
    {
        TR_CONFIG_LYRICSTYLE_FILL,
        TR_CONFIG_LYRICSTYLE_JUMP,
        TR_CONFIG_LYRICSTYLE_SLIDE,
        TR_CONFIG_LYRICSTYLE_ZOOM
    }
    #endregion Config

    public enum EStreamAction
    {
        Nothing,
        Pause,
        Stop,
        Close
    }

    public enum EProgramState
    {
        Start,
        Normal,
        EditTheme
    }

    public enum EGameMode
    {
        // ReSharper disable InconsistentNaming
        TR_GAMEMODE_NORMAL,
        TR_GAMEMODE_MEDLEY,
        TR_GAMEMODE_DUET,
        TR_GAMEMODE_SHORTSONG
        // ReSharper restore InconsistentNaming
    }

    public enum ENoteType
    {
        Normal,
        Golden,
        Freestyle
    }

    public enum EMusicType
    {
        None,
        Background,
        Preview,
        BackgroundPreview,
        Game
    }

    public enum EHighscoreStyle
    {
        TR_CONFIG_HIGHSCORE_LIST_ALL,
        TR_CONFIG_HIGHSCORE_LIST_BEST
    }

    /// <summary>
    ///     Base screens
    /// </summary>
    public enum EScreen
    {
        Unknown = -1,

        Test,
        Load,
        Main,
        Song,
        Options,
        Sing,
        Profiles,
        Score,
        Highscore,

        OptionsGame,
        OptionsSound,
        OptionsRecord,
        OptionsVideo,
        OptionsVideoAdjustments,
        OptionsLyrics,
        OptionsTheme,

        Names,
        Credits,
        Party,

        CountEntry //Leave this as last entry and never use it!
    }

    public enum EPopupScreens
    {
        PopupPlayerControl = 0,
        PopupVolumeControl = 1,
        PopupServerQR = 2,

        NoPopup = -1
    }

    [Flags]
    public enum EUserRole
    {
        // ReSharper disable InconsistentNaming
        TR_USERROLE_GUEST = 0,
        TR_USERROLE_NORMAL = 1,
        TR_USERROLE_ADMIN = 2,
        TR_USERROLE_KEYBOARDUSER = 4,
        TR_USERROLE_ADDSONGSUSER = 8,
        TR_USERROLE_PLAYLISTEDITOR = 16,
        TR_USERROLE_PROFILEEDITOR = 32
        // ReSharper restore InconsistentNaming
    }
}