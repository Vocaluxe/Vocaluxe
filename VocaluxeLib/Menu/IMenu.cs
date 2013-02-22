using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Menu.SingNotes;
using Vocaluxe.Menu.SongMenu;
using Vocaluxe.PartyModes;

namespace Vocaluxe.Menu
{
    public interface IMenu
    {
        void Initialize();

        void LoadTheme(string XmlPath);
        void SaveTheme();
        void ReloadTextures();
        void UnloadTextures();
        void ReloadTheme(string XmlPath);

        bool HandleInput(KeyEvent KeyEvent);
        bool HandleMouse(MouseEvent MouseEvent);
        bool HandleInputThemeEditor(KeyEvent KeyEvent);
        bool HandleMouseThemeEditor(MouseEvent MouseEvent);

        bool UpdateGame();
        void ApplyVolume();
        void OnShow();
        void OnShowFinish();
        void OnClose();

        bool Draw();
        SRectF GetScreenArea();

        void NextInteraction();
        void PrevInteraction();

        bool NextElement();
        bool PrevElement();

        void ProcessMouseClick(int x, int y);
        void ProcessMouseMove(int x, int y);
    }

    public interface IMenuParty
    {
        void SetPartyModeID(int PartyModeID);
        int GetPartyModeID();

        void AssingPartyMode(IPartyMode PartyMode);
        void DataToScreen(Object Data);
    }

    public interface IConfig
    {
        void SetBackgroundMusicVolume(int NewVolume);
        int GetBackgroundMusicVolume();

        int GetPreviewMusicVolume();

        EOffOn GetVideosToBackground();
        EOffOn GetVideoBackgrounds();
        EOffOn GetVideoPreview();

        ESongMenu GetSongMenuType();

        EOffOn GetDrawNoteLines();
        EOffOn GetDrawToneHelper();

        int GetCoverSize();

        List<string> GetSongFolder();
        ESongSorting GetSongSorting();
        EOffOn GetTabs();
        EOffOn GetIgnoreArticles();

        bool IsMicConfigured(int PlayerNr);
        int GetMaxNumMics();
    }

    public interface ISettings
    {
        int GetRenderW();
        int GetRenderH();
        bool IsTabNavigation();

        float GetZFar();
        float GetZNear();

        EGameState GetGameState();

        int GetToneMin();
        int GetToneMax();

        int GetNumNoteLines();
        int GetMaxNumPlayer();

        float GetDefaultMedleyFadeInTime();
        float GetDefaultMedleyFadeOutTime();
        int GetMedleyMinSeriesLength();
        float GetMedleyMinDuration();
    }

    public interface ITheme
    {
        string GetThemeScreensPath(int PartyModeID);
        int GetSkinIndex(int PartyModeID);
        STexture GetSkinTexture(string TextureName, int PartyModeID);
        STexture GetSkinVideoTexture(string VideoName, int PartyModeID);

        void SkinVideoResume(string VideoName, int PartyModeID);
        void SkinVideoPause(string VideoName, int PartyModeID);

        SColorF GetColor(string ColorName, int PartyModeID);
        bool GetColor(string ColorName, int SkinIndex, ref SColorF Color);
        SColorF GetPlayerColor(int PlayerNr);

        void UnloadSkins();
        void ListSkins();
        void LoadSkins();
        void LoadTheme();
    }

    public interface IHelper
    {
    }

    public interface IBackgroundMusic
    {
        bool IsDisabled();
        bool IsPlaying();
        bool SongHasVideo();
        bool VideoEnabled();

        void SetStatus(bool Disabled);
        void Next();
        void Previous();
        void Pause();
        void Play();

        void ApplyVolume();

        STexture GetVideoTexture();
    }

    public interface IDrawing
    {
        RectangleF GetTextBounds(CText text);
        
        void DrawTexture(STexture Texture, SRectF Rect);
        void DrawTexture(STexture Texture, SRectF Rect, SColorF Color);
        void DrawTexture(STexture Texture, SRectF Rect, SColorF Color, SRectF Bounds);
        void DrawTexture(STexture Texture, SRectF Rect, SColorF Color, SRectF Bounds, bool Mirrored);
        void DrawTextureReflection(STexture Texture, SRectF Rect, SColorF Color, SRectF Bounds, float ReflectionSpace, float ReflectionHeight);

        void RemoveTexture(ref STexture Texture);

        void DrawColor(SColorF Color, SRectF Rect);
        void DrawColorReflection(SColorF Color, SRectF Rect, float Space, float Height);
    }

    public interface IGraphics
    {
        void ReloadTheme();
        void SaveTheme();
        void FadeTo(EScreens NextScreen);

        float GetGlobalAlpha();
    }

    public interface ILog
    {
        void LogError(string ErrorText);
    }

    public interface IFonts
    {
        void SetFont(string FontName);
        void SetStyle(EStyle FontStyle);

        RectangleF GetTextBounds(CText Text, float TextHeight);

        void DrawText(string Text, float TextHeight, float x, float y, float z, SColorF Color);
        void DrawTextReflection(string Text, float TextHeight, float x, float y, float z, SColorF Color, float ReflectionSpace, float ReflectionHeight);
        void DrawText(string Text, float TextHeight, float x, float y, float z, SColorF Color, float Begin, float End);
    }

    public interface ILanguage
    {
        string Translate(string KeyWord);
        string Translate(string KeyWord, int PartyModeID);
        bool TranslationExists(string KeyWord);
    }

    public interface IGame
    {
        int GetNumPlayer();
        void SetNumPlayer(int NumPlayer);
        SPlayer[] GetPlayer();
        CPoints GetPoints();
        float GetMidBeatD();
        int GetCurrentBeatD();

        int GetRandom(int Max);
        double GetRandomDouble();

        float GetTimeFromBeats(float Beat, float BPM);

        void AddSong(int SongID, EGameMode GameMode);
        void Reset();
        void ClearSongs();
        int GetNumSongs();
    }

    public interface IRecording
    {
        int GetToneAbs(int PlayerNr);
    }

    public interface IProfiles
    {
        SProfile[] GetProfiles();
    }

    public interface ISongs
    {
        int GetNumSongs();
        int GetNumVisibleSongs();
        int GetNumCategories();
        int NumSongsInCategory(int CategoryIndex);

        int GetCurrentCategoryIndex();
        EOffOn GetTabs();
        string GetSearchFilter();

        void SetCategory(int CategoryIndex);
        void UpdateRandomSongList();

        CSong GetVisibleSong(int VisibleIndex);
        CSong GetSongByID(int SongID);
        CSong[] GetSongs();
        CSong[] GetSongsNotSung();
        CCategory GetCategory(int Index);

        void AddPartySongSung(int SongID);
        void ResetPartySongSung();
        void ResetPartySongSung(int CatIndex);

        void SortSongs(ESongSorting Sorting, EOffOn Tabs, EOffOn IgnoreArticles, String SearchString, bool ShowDuetSongs);

        void NextCategory();
        void PrevCategory();
    }

    public interface IVideo
    {
        int Load(string VideoFileName);
        bool Skip(int VideoStream, float StartPosition, float VideoGap);
        bool GetFrame(int VideoStream, ref STexture VideoTexture, float Time, ref float VideoTime);
        bool IsFinished(int VideoStream);
        bool Close(int VideoStream);
    }

    public interface ISound
    {
        int Load(string SoundFile, bool Prescan);
        void SetPosition(int SoundStream, float NewPosition);
        void Play(int SoundStream);
        void Fade(int SoundStream, float TargetVolume, float Duration);

        bool IsFinished(int SoundStream);
        float GetPosition(int SoundStream);
        float GetLength(int SoundStream);
        void FadeAndStop(int SoundStream, float TargetVolume, float Duration);

        void SetStreamVolume(int SoundStream, float Volume);
        void SetStreamVolumeMax(int SoundStream, float MaxVolume);
    }

    public interface ICover
    {
        STexture GetNoCover();
    }

    public interface IDataBase
    {
        bool GetCover(string FileName, ref STexture Texture, int CoverSize);
    }

    public interface IInputs
    {
        void SetRumble(float Duration);
    }

    public interface IPlaylist
    {
        string GetPlaylistName(int PlaylistID);
        string[] GetPlaylistNames();

        void SetPlaylistName(int PlaylistID, string Name);
        void DeletePlaylist(int PlaylistID);
        void SavePlaylist(int PlaylistID);
        int GetNumPlaylists();
        
        void AddPlaylistSong(int PlaylistID, int SongID);
        void AddPlaylistSong(int PlaylistID, int SongID, EGameMode GameMode);
        void InsertPlaylistSong(int PlaylistID, int PositionIndex, int SongID, EGameMode GameMode);

        void MovePlaylistSong(int PlaylistID, int SourceIndex, int DestIndex);
        void MovePlaylistSongDown(int PlaylistID, int SongIndex);
        void MovePlaylistSongUp(int PlaylistID, int SongIndex);
        void DeletePlaylistSong(int PlaylistID, int SongIndex);

        int GetPlaylistSongCount(int PlaylistID);
        CPlaylistSong GetPlaylistSong(int PlaylistID, int SongIndex);
    }

    public interface IPlaylists
    {
    }

    [Flags]
    public enum EModifier
    {
        None,
        Shift,
        Alt,
        Ctrl
    }

    public enum ESender
    {
        Mouse,
        Keyboard,
        WiiMote,
        Gamepad
    }

    public struct KeyEvent
    {
        public ESender Sender;
        public bool ModALT;
        public bool ModSHIFT;
        public bool ModCTRL;
        public bool KeyPressed;
        public bool Handled;
        public Keys Key;
        public Char Unicode;
        public EModifier Mod;

        public KeyEvent(ESender sender, bool alt, bool shift, bool ctrl, bool pressed, char unicode, Keys key)
        {
            Sender = sender;
            ModALT = alt;
            ModSHIFT = shift;
            ModCTRL = ctrl;
            KeyPressed = pressed;
            Unicode = unicode;
            Key = key;
            Handled = false;

            EModifier mALT = EModifier.None;
            EModifier mSHIFT = EModifier.None;
            EModifier mCTRL = EModifier.None;

            if (alt)
                mALT = EModifier.Alt;

            if (shift)
                mSHIFT = EModifier.Shift;

            if (ctrl)
                mCTRL = EModifier.Ctrl;

            if (!alt && !shift && !ctrl)
                Mod = EModifier.None;
            else
                Mod = mALT | mSHIFT | mCTRL;
        }
    }

    public struct MouseEvent
    {
        public ESender Sender;
        public bool Handled;
        public int X;
        public int Y;
        public bool LB;     //left button click
        public bool LD;     //left button double click
        public bool RB;     //right button click
        public bool MB;     //middle button click

        public bool LBH;    //left button hold (when moving)
        public bool RBH;    //right button hold (when moving)
        public bool MBH;    //middle button hold (when moving)

        public bool ModALT;
        public bool ModSHIFT;
        public bool ModCTRL;

        public EModifier Mod;
        public int Wheel;

        public MouseEvent(ESender sender, bool alt, bool shift, bool ctrl, int x, int y, bool lb, bool ld, bool rb, int wheel, bool lbh, bool rbh, bool mb, bool mbh)
        {
            Sender = sender;
            Handled = false;
            X = x;
            Y = y;
            LB = lb;
            LD = ld;
            RB = rb;
            MB = mb;

            LBH = lbh;
            RBH = rbh;
            MBH = mbh;

            ModALT = alt;
            ModSHIFT = shift;
            ModCTRL = ctrl;

            EModifier mALT = EModifier.None;
            EModifier mSHIFT = EModifier.None;
            EModifier mCTRL = EModifier.None;

            if (alt)
                mALT = EModifier.Alt;

            if (shift)
                mSHIFT = EModifier.Shift;

            if (ctrl)
                mCTRL = EModifier.Ctrl;

            if (!alt && !shift && !ctrl)
                Mod = EModifier.None;
            else
                Mod = mALT | mSHIFT | mCTRL;

            Wheel = wheel;
        }
    }

    public enum EGameState
    {
        Start,
        Normal,
        EditTheme
    }

    public enum EAspect
    {
        Crop,
        LetterBox,
        Stretch
    }

    #region Structs
    public struct SColorF
    {
        public float R;
        public float G;
        public float B;
        public float A;

        public SColorF(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public SColorF(SColorF Color)
        {
            R = Color.R;
            G = Color.G;
            B = Color.B;
            A = Color.A;
        }
    }

    public struct SRectF
    {
        public float X;
        public float Y;
        public float W;
        public float H;
        public float Z;
        public float Rotation; //0..360°

        public SRectF(float x, float y, float w, float h, float z)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
            Z = z;
            Rotation = 0f;
        }

        public SRectF(SRectF rect)
        {
            X = rect.X;
            Y = rect.Y;
            W = rect.W;
            H = rect.H;
            Z = rect.Z;
            Rotation = 0f;
        }
    }

    public struct SPoint3f
    {
        public float X;
        public float Y;
        public float Z;
    }

    public struct SPoint3
    {
        public int X;
        public int Y;
        public int Z;
    }

    public struct STexture
    {
        public int index;
        public int PBO;
        public int ID;

        public string TexturePath;

        public float width;
        public float height;
        public SRectF rect;

        public float w2;    //power of 2 width
        public float h2;    //power of 2 height
        public float width_ratio;
        public float height_ratio;

        public SColorF color;

        public STexture(int Index)
        {
            index = Index;
            PBO = 0;
            ID = -1;
            TexturePath = String.Empty;

            width = 1f;
            height = 1f;
            rect = new SRectF(0f, 0f, 1f, 1f, 0f);

            w2 = 2f;
            h2 = 2f;
            width_ratio = 0.5f;
            height_ratio = 0.5f;

            color = new SColorF(1f, 1f, 1f, 1f);
        }
    }
    #endregion Structs

    #region EnumsConfig
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
        TR_CONFIG_OFF,		    //no debug infos
        TR_CONFIG_ONLY_FPS,
        TR_CONFIG_LEVEL1,
        TR_CONFIG_LEVEL2,
        TR_CONFIG_LEVEL3,
        TR_CONFIG_LEVEL_MAX	    //all debug infos
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
        OpenAL
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
        FFmpeg
    }

    public enum ESongMenu
    {
        //TR_CONFIG_LIST,		    //a simple list
        //TR_CONFIG_DREIDEL,	    //as in ultrastar deluxe
        TR_CONFIG_TILE_BOARD,	//chessboard like
        //TR_CONFIG_BOOK          //for playlists
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

    #endregion EnumsConfig

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

    public enum ENoteType
    {
        Normal,
        Golden,
        Freestyle
    }

    public struct SProfile
    {
        public string PlayerName;
        public string ProfileFile;

        public EGameDifficulty Difficulty;
        public SAvatar Avatar;
        public EOffOn GuestProfile;
        public EOffOn Active;
    }

    public struct SAvatar
    {
        public string FileName;
        public STexture Texture;

        public SAvatar(int dummy)
        {
            FileName = String.Empty;
            Texture = new STexture(-1);
        }
    }

    public struct SPlayer
    {
        public int ProfileID;
        public string Name;
        public EGameDifficulty Difficulty;
        public double Points;
        public double PointsLineBonus;
        public double PointsGoldenNotes;
        public int NoteDiff;
        public int LineNr;
        public List<CLine> SingLine;
        public int CurrentLine;
        public int CurrentNote;

        public int SongID;
        public bool Medley;
        public bool Duet;
        public bool ShortSong;
        public long DateTicks;
        public bool SongFinished;
    }

    public class CPoints
    {
        private SPlayer[,] _Rounds;

        public CPoints(int NumRounds, SPlayer[] Player)
        {
            _Rounds = new SPlayer[NumRounds, Player.Length];

            for (int round = 0; round < NumRounds; round++)
            {
                for (int player = 0; player < Player.Length; player++)
                {
                    _Rounds[round, player].ProfileID = Player[player].ProfileID;
                    _Rounds[round, player].Name = Player[player].Name;
                    _Rounds[round, player].Difficulty = Player[player].Difficulty;
                    _Rounds[round, player].Points = 0f;
                    _Rounds[round, player].PointsGoldenNotes = 0f;
                    _Rounds[round, player].PointsLineBonus = 0f;
                    _Rounds[round, player].Medley = false;
                    _Rounds[round, player].Duet = false;
                    _Rounds[round, player].ShortSong = false;
                    _Rounds[round, player].SongFinished = false;
                }
            }
        }

        public void SetPoints(int Round, int SongID, SPlayer[] Player, bool Medley, bool Duet, bool ShortSong)
        {
            long DateTicks = DateTime.Now.Ticks;
            for (int player = 0; player < Player.Length; player++)
            {
                _Rounds[Round, player].SongID = SongID;
                _Rounds[Round, player].LineNr = Player[player].LineNr;
                _Rounds[Round, player].Points = Player[player].Points;
                _Rounds[Round, player].PointsGoldenNotes = Player[player].PointsGoldenNotes;
                _Rounds[Round, player].PointsLineBonus = Player[player].PointsLineBonus;
                _Rounds[Round, player].Medley = Medley;
                _Rounds[Round, player].Duet = Duet;
                _Rounds[Round, player].ShortSong = ShortSong;
                _Rounds[Round, player].DateTicks = DateTicks;
                _Rounds[Round, player].SongFinished = Player[player].SongFinished;
            }
        }

        public int NumRounds
        {
            get { return _Rounds.GetLength(0); }
        }

        public int NumPlayer
        {
            get { return _Rounds.GetLength(1); }
        }

        public SPlayer[] GetPlayer(int Round, int numPlayer)
        {
            if (NumPlayer == 0)
                return new SPlayer[1];
            if (Round >= NumRounds)
                return new SPlayer[1];

            SPlayer[] player = new SPlayer[numPlayer];

            for (int p = 0; p < player.Length; p++)
            {
                player[p].Name = _Rounds[Round, p].Name;
                player[p].Points = _Rounds[Round, p].Points;
                player[p].PointsGoldenNotes = _Rounds[Round, p].PointsGoldenNotes;
                player[p].PointsLineBonus = _Rounds[Round, p].PointsLineBonus;
                player[p].SongID = _Rounds[Round, p].SongID;
                player[p].LineNr = _Rounds[Round, p].LineNr;
                player[p].Difficulty = _Rounds[Round, p].Difficulty;
                player[p].Medley = _Rounds[Round, p].Medley;
                player[p].Duet = _Rounds[Round, p].Duet;
                player[p].ShortSong = _Rounds[Round, p].ShortSong;
                player[p].DateTicks = _Rounds[Round, p].DateTicks;
                player[p].SongFinished = _Rounds[Round, p].SongFinished;
                player[p].ProfileID = _Rounds[Round, p].ProfileID;
            }
            return player;
        }
    }

    public struct SScores
    {
        public string Name;
        public int Score;
        public string Date;
        public EGameDifficulty Difficulty;
        public int LineNr;
        public int ID;
    }

    public enum EGameMode
    {
        TR_GAMEMODE_NORMAL,
        TR_GAMEMODE_MEDLEY,
        TR_GAMEMODE_DUET,
        TR_GAMEMODE_SHORTSONG
    }

    public struct SPartyModeInfos
    {
        public int PartyModeID;
        public string Name;
        public string Description;
        public string TargetAudience;
        public int MaxPlayers;
        public int MinPlayers;
        public int MaxTeams;
        public int MinTeams;

        public string Author;
        public bool Playable;
        public int VersionMajor;
        public int VersionMinor;
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
