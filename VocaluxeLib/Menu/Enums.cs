using System;

namespace VocaluxeLib.Menu
{
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
        Stretch
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
#if WIN
        TR_CONFIG_DIRECT3D,
#endif
        TR_CONFIG_OPENGL,
        TR_CONFIG_SOFTWARE
    }

    public enum EAntiAliasingModes
    {
        x0 = 0,
        x2 = 2,
        x4 = 4,
        x8 = 8,
        x16 = 16,
        x32 = 32
    }

    public enum EColorDeep
    {
        Bit8 = 8,
        Bit16 = 16,
        Bit24 = 24,
        Bit32 = 32
    }

    public enum ETextureQuality
    {
        TR_CONFIG_TEXTURE_LOWEST,
        TR_CONFIG_TEXTURE_LOW,
        TR_CONFIG_TEXTURE_MEDIUM,
        TR_CONFIG_TEXTURE_HIGH,
        TR_CONFIG_TEXTURE_HIGHEST
    }

    public enum EOffOn
    {
        TR_CONFIG_OFF,
        TR_CONFIG_ON
    }

    public enum EDebugLevel
    {
        // don't change the order!
        TR_CONFIG_OFF, //no debug infos
        TR_CONFIG_ONLY_FPS,
        TR_CONFIG_LEVEL1,
        TR_CONFIG_LEVEL2,
        TR_CONFIG_LEVEL3,
        TR_CONFIG_LEVEL_MAX //all debug infos
    }

    public enum EBufferSize
    {
        b0 = 0,
        b512 = 512,
        b1024 = 1024,
        b1536 = 1536,
        b2048 = 2048,
        b2560 = 2560,
        b3072 = 3072,
        b3584 = 3584,
        b4096 = 4096
    }

    public enum EPlaybackLib
    {
        PortAudio,
        OpenAL,
        Gstreamer
    }

    public enum EWebcamLib
    {
        OpenCV,
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
        FFmpeg,
        Gstreamer
    }

    public enum ESongMenu
    {
        //TR_CONFIG_LIST,		    //a simple list
        //TR_CONFIG_DREIDEL,	    //as in ultrastar deluxe
        TR_CONFIG_TILE_BOARD, //chessboard like
        //TR_CONFIG_BOOK          //for playlists
    }

    public enum EDuetOptions
    {
        All,
        Duets,
        NoDuets
    }

    public enum ESongSorting
    {
        TR_CONFIG_NONE,
        //TR_CONFIG_RANDOM,
        TR_CONFIG_FOLDER,
        TR_CONFIG_ARTIST,
        TR_CONFIG_ARTIST_LETTER,
        TR_CONFIG_TITLE_LETTER,
        TR_CONFIG_EDITION,
        TR_CONFIG_GENRE,
        TR_CONFIG_LANGUAGE,
        TR_CONFIG_YEAR,
        TR_CONFIG_DECADE
    }

    public enum ECoverLoading
    {
        TR_CONFIG_COVERLOADING_ONDEMAND,
        TR_CONFIG_COVERLOADING_ATSTART,
        TR_CONFIG_COVERLOADING_DYNAMIC
    }

    public enum EGameDifficulty
    {
        TR_CONFIG_EASY,
        TR_CONFIG_NORMAL,
        TR_CONFIG_HARD
    }

    public enum ETimerMode
    {
        TR_CONFIG_TIMERMODE_CURRENT,
        TR_CONFIG_TIMERMODE_REMAINING,
        TR_CONFIG_TIMERMODE_TOTAL
    }

    public enum ETimerLook
    {
        TR_CONFIG_TIMERLOOK_NORMAL,
        TR_CONFIG_TIMERLOOK_EXPANDED
    }

    public enum EBackgroundMusicSource
    {
        TR_CONFIG_NO_OWN_MUSIC,
        TR_CONFIG_OWN_MUSIC,
        TR_CONFIG_ONLY_OWN_MUSIC
    }

    public enum EPlayerInfo
    {
        TR_CONFIG_PLAYERINFO_BOTH,
        TR_CONFIG_PLAYERINFO_NAME,
        TR_CONFIG_PLAYERINFO_AVATAR,
        TR_CONFIG_PLAYERINFO_OFF
    }

    public enum EFadePlayerInfo
    {
        TR_CONFIG_FADEPLAYERINFO_ALL,
        TR_CONFIG_FADEPLAYERINFO_INFO,
        TR_CONFIG_FADEPLAYERINFO_OFF
    }

    public enum ELyricStyle
    {
        Fill,
        Jump,
        Slide,
        Zoom
    }
    #endregion Config

    public enum EGameState
    {
        Start,
        Normal,
        EditTheme
    }

    public enum EGameMode
    {
        TR_GAMEMODE_NORMAL,
        TR_GAMEMODE_MEDLEY,
        TR_GAMEMODE_DUET,
        TR_GAMEMODE_SHORTSONG
    }

    public enum ENoteType
    {
        Normal,
        Golden,
        Freestyle
    }

    public enum EScreens
    {
        ScreenTest = 0,
        ScreenLoad = 1,
        ScreenMain = 2,
        ScreenSong = 3,
        ScreenOptions = 4,
        ScreenSing = 5,
        ScreenProfiles = 6,
        ScreenScore = 7,
        ScreenHighscore = 8,

        ScreenOptionsGame = 9,
        ScreenOptionsSound = 10,
        ScreenOptionsRecord = 11,
        ScreenOptionsVideo = 12,
        ScreenOptionsLyrics = 13,
        ScreenOptionsTheme = 14,

        ScreenNames = 15,
        ScreenCredits = 16,
        ScreenParty = 17,
        ScreenPartyDummy = 18,

        ScreenNull = -1
    }
}