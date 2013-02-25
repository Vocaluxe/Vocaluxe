using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Vocaluxe.Menu;
using Vocaluxe.Menu.SingNotes;

namespace Vocaluxe.Menu.SongMenu
{
    [Flags]
    enum EHeaderFlags
    {
        Title = 1,
        Artist = 2,
        MP3 = 4,
        BPM = 8,
        PreviewStart = 16,
        MedleyStartBeat = 32,
        MedleyEndBeat = 64
    }

    public enum EMedleySource
    {
        None,
        Calculated,
        Tag
    }

    public class CCategory
    {
        public string Name = String.Empty;
        public STexture CoverTextureSmall = new STexture(-1);
        public STexture CoverTextureBig = new STexture(-1);

        public CCategory(string name)
        {
            Name = name;
        }

        public CCategory(string name, STexture CoverSmall, STexture CoverBig)
        {
            Name = name;
            CoverTextureSmall = CoverSmall;
            CoverTextureBig = CoverBig;
        }

        public CCategory(CCategory cat)
        {
            Name = cat.Name;
            CoverTextureSmall = cat.CoverTextureSmall;
            CoverTextureBig = cat.CoverTextureBig;
        }
    }

    public struct SMedley
    {
        public EMedleySource Source;
        public int StartBeat;
        public int EndBeat;
        public float FadeInTime;
        public float FadeOutTime;

        public SMedley(int dummy)
        {
            Source = EMedleySource.None;
            StartBeat = 0;
            EndBeat = 0;
            FadeInTime = 0f;
            FadeOutTime = 0f;
        }
    }

    public class CSong
    {
        private bool _CoverSmallLoaded = false;
        private bool _CoverBigLoaded = false;
        private bool _NotesLoaded = false;
        private STexture _CoverTextureSmall = new STexture(-1);
        private STexture _CoverTextureBig = new STexture(-1);

        public SMedley Medley = new SMedley(0);

        public bool CalculateMedley = true;
        public float PreviewStart = 0f;

        public int ShortEnd = 0;

        public Encoding Encoding = Encoding.Default;
        public string Folder = String.Empty;
        public string FolderName = String.Empty;
        public string FileName = String.Empty;
        public EOffOn Relative = EOffOn.TR_CONFIG_OFF;

        public string MP3FileName = String.Empty;
        public string CoverFileName = String.Empty;
        public string BackgroundFileName = String.Empty;
        public string VideoFileName = String.Empty;

        public EAspect VideoAspect = EAspect.Crop;

        public bool CoverSmallLoaded
        {
            get { return _CoverSmallLoaded; }
        }
        public bool CoverBigLoaded
        {
            get { return _CoverBigLoaded; }
        }
        public bool NotesLoaded
        {
            get { return _NotesLoaded; }
        }

        public STexture CoverTextureSmall
        {
            get
            {
                if (!_CoverSmallLoaded)
                {
                    LoadSmallCover();
                }
                return _CoverTextureSmall;
            }

            set
            {
                _CoverTextureSmall = value;
                _CoverSmallLoaded = true;
            }
        }

        public STexture CoverTextureBig
        {
            get
            {
                if (_CoverBigLoaded)
                    return _CoverTextureBig;
                else 
                    return _CoverTextureSmall;
            }
            set {
                _CoverTextureBig = value;
                _CoverBigLoaded = true;
            }
        }

        public string Title = String.Empty;
        public string Artist = String.Empty;

        public string TitleSorting = String.Empty;
        public string ArtistSorting = String.Empty;

        public float Start = 0f;
        public float Finish = 0f;
        
        public float BPM = 1f;
        public float Gap = 0f;
        public float VideoGap = 0f;

        public string DuetPart1 = String.Empty;
        public string DuetPart2 = String.Empty;

        public List<string> Comment = new List<string>();

        // Sorting
        public int ID;
        public bool Visible = true;
        public int CatIndex = -1;
        public bool Selected = false;
        public bool IsDuet
        {
            get { return Notes.Lines.Length > 1; }
        }

        public List<string> Edition = new List<string>();
        public List<string> Genre = new List<string>();
        public string Year = "";

        public List<string> Language = new List<string>();

        // Notes
        public CNotes Notes = new CNotes();

        public EGameMode[] AvailableGameModes
        {
            get 
            {
                List<EGameMode> gms = new List<EGameMode>();
                if (IsDuet)
                    gms.Add(EGameMode.TR_GAMEMODE_DUET);
                else
                    gms.Add(EGameMode.TR_GAMEMODE_NORMAL);
                if (Medley.Source != EMedleySource.None)
                    gms.Add(EGameMode.TR_GAMEMODE_MEDLEY);
                gms.Add(EGameMode.TR_GAMEMODE_SHORTSONG);

                return gms.ToArray();
            }
        }

        //No point creating a song without a text file --> Use factory method LoadSong
        private CSong()
        {
        }

        public static CSong LoadSong(string FilePath)
        {
            CSong Song = new CSong();
            if (Song.ReadHeader(FilePath))
                return Song;
            return null;
        }

        public CSong(CSong song)
        {
            this._CoverTextureSmall = song._CoverTextureSmall;
            this._CoverTextureBig = song._CoverTextureBig;

            this.Medley = new SMedley();
            this.Medley.Source = song.Medley.Source;
            this.Medley.StartBeat = song.Medley.StartBeat;
            this.Medley.EndBeat = song.Medley.EndBeat;
            this.Medley.FadeInTime = song.Medley.FadeInTime;
            this.Medley.FadeOutTime = song.Medley.FadeOutTime;

            this.CalculateMedley = song.CalculateMedley;
            this.PreviewStart = song.PreviewStart;

            this.ShortEnd = song.ShortEnd;
            this.DuetPart1 = song.DuetPart1;
            this.DuetPart2 = song.DuetPart2;

            this.Encoding = song.Encoding;
            this.Folder = song.Folder;
            this.FolderName = song.FolderName;
            this.FileName = song.FileName;
            this.Relative = song.Relative;

            this.MP3FileName = song.MP3FileName;
            this.CoverFileName = song.CoverFileName;
            this.BackgroundFileName = song.BackgroundFileName;
            this.VideoFileName = song.VideoFileName;

            this.VideoAspect = song.VideoAspect;
            this._CoverSmallLoaded = song._CoverSmallLoaded;
            this._CoverBigLoaded = song._CoverBigLoaded;
            this._NotesLoaded = song._NotesLoaded;

            this.Artist = song.Artist;
            this.Title = song.Title;
            
            this.Start = song.Start;
            this.Finish = song.Finish;

            this.BPM = song.BPM;
            this.Gap = song.Gap;
            this.VideoGap = song.VideoGap;

            this.Comment = new List<string>();
            foreach (string value in song.Comment)
            {
                this.Comment.Add(value);
            }

            this.ID = song.ID;
            this.Visible = song.Visible;
            this.CatIndex = song.CatIndex;
            this.Selected = song.Selected;

            this.Edition = new List<string>();
            foreach (string value in song.Edition)
            {
                this.Edition.Add(value);
            }

            this.Genre = new List<string>();
            foreach (string value in song.Genre)
            {
                this.Genre.Add(value);
            }

            this.Year = song.Year;

            this.Language = new List<string>();
            foreach (string value in song.Language)
            {
                this.Language.Add(value);
            }

            this.Notes = new CNotes(song.Notes);
        }

        public string GetMP3()
        {
            return Path.Combine(Folder, MP3FileName);
        }

        public string GetVideo()
        {
            return Path.Combine(Folder, VideoFileName);
        }

        public bool ReadHeader(string FilePath)
        {
            if (!File.Exists(FilePath))
                return false;

            this.Folder = Path.GetDirectoryName(FilePath);

            foreach (string folder in CBase.Config.GetSongFolder())
            {
                if (this.Folder.Contains(folder))
                {
                    if (this.Folder.Length == folder.Length)
                        this.FolderName = "Songs";
                    else
                    {
                        this.FolderName = this.Folder.Substring(folder.Length + 1, this.Folder.Length - folder.Length - 1);

                        int pos = this.FolderName.IndexOf("\\");
                        if (pos >= 0)
                            this.FolderName = this.FolderName.Substring(0, pos);
                    }
                    break;
                }
            }

            this.FileName = Path.GetFileName(FilePath);

            EHeaderFlags HeaderFlags = new EHeaderFlags();
            StreamReader sr;
            try
            {

                sr = new StreamReader(FilePath, Encoding.Default, true);

                string line;

                int pos = -1;
                string Identifier = String.Empty;
                string Value = String.Empty;

                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Length == 0)
                        continue;
                    if (!line[0].ToString().Equals("#")) break;

                    pos = line.IndexOf(":");

                    if (pos > 0)
                    {
                        Identifier = line.Substring(1, pos - 1).Trim().ToUpper();
                        Value = line.Substring(pos + 1, line.Length - pos - 1).Trim();

                        if (Value.Length > 0)
                        {
                            switch (Identifier)
                            {
                                case "ENCODING":
                                    this.Encoding = CEncoding.GetEncoding(Value);
                                    sr = new StreamReader(FilePath, this.Encoding);
                                    Identifier = String.Empty;

                                    //Scip everything till encoding
                                    while (!sr.EndOfStream)
                                    {
                                        line = sr.ReadLine();
                                        if (line.Length == 0) 
                                            continue;
                                        if(!line[0].ToString().Equals("#")) break;

                                        pos = line.IndexOf(":");

                                        if (pos > 0)
                                        {
                                            Identifier = line.Substring(1, pos - 1).Trim().ToUpper();
                                            if (Identifier == "ENCODING")
                                                break;
                                        }
                                    }
                                    break;
                                case "TITLE":
                                    if (Value != String.Empty)
                                    {
                                        this.Title = Value;
                                        HeaderFlags |= EHeaderFlags.Title;
                                    }
                                    break;
                                case "ARTIST":
                                    if (Value != String.Empty)
                                    {
                                        this.Artist = Value;
                                        HeaderFlags |= EHeaderFlags.Artist;
                                    }
                                    break;
                                case "TITLE-ON-SORTING":
                                    if (Value != String.Empty)
                                    {
                                        this.TitleSorting = Value;
                                    }
                                    break;
                                case "ARTIST-ON-SORTING":
                                    if (Value != String.Empty)
                                    {
                                        this.ArtistSorting = Value;
                                    }
                                    break;
                                case "P1":
                                    if (Value != String.Empty)
                                    {
                                        this.DuetPart1 = Value;
                                    }
                                    break;
                                case "P2":
                                    if (Value != String.Empty)
                                    {
                                        this.DuetPart2 = Value;
                                    }
                                    break;
                                case "MP3":
                                    if (File.Exists(Path.Combine(this.Folder, Value)))
                                    {
                                        this.MP3FileName = Value;
                                        HeaderFlags |= EHeaderFlags.MP3;
                                    }
                                    else
                                    {
                                        CBase.Log.LogError("Can't find audio file: " + Path.Combine(this.Folder, Value));
                                        return false;
                                    }
                                    break;
                                case "BPM":
                                    if (CHelper.TryParse(Value, out this.BPM))
                                    {
                                        this.BPM *= 4;
                                        HeaderFlags |= EHeaderFlags.BPM;
                                    }
                                    break;
                                case "EDITION":
                                    if (Value.Length > 1)
                                        this.Edition.Add(Value);
                                    break;
                                case "GENRE":
                                    if (Value.Length > 1)
                                        this.Genre.Add(Value);
                                    break;
                                case "YEAR":
                                    int num = 0;
                                    if (Value.Length == 4 && int.TryParse(Value, out num) && num != 0)
                                    {
                                        this.Year = Value;
                                    }
                                    break;
                                case "LANGUAGE":
                                    if (Value.Length > 1)
                                        this.Language.Add(Value);
                                    break;
                                case "COMMENT":
                                    if (Value.Length > 1)
                                        this.Comment.Add(Value);
                                    break;
                                case "GAP":
                                    if (CHelper.TryParse(Value, out this.Gap))
                                        this.Gap /= 1000f;
                                    break;
                                case "COVER":
                                    if (File.Exists(Path.Combine(this.Folder, Value)))
                                        this.CoverFileName = Value;
                                    break;
                                case "BACKGROUND":
                                    if (File.Exists(Path.Combine(this.Folder, Value)))
                                        this.BackgroundFileName = Value;
                                    break;
                                case "VIDEO":
                                    if (File.Exists(Path.Combine(this.Folder, Value)))
                                        this.VideoFileName = Value;
                                    else
                                        CBase.Log.LogError("Can't find video file: " + Path.Combine(this.Folder, Value));
                                    break;
                                case "VIDEOGAP":
                                    CHelper.TryParse(Value, out this.VideoGap);
                                    break;
                                case "VIDEOASPECT":
                                    CHelper.TryParse<EAspect>(Value, out this.VideoAspect, true);
                                    break;
                                case "START":
                                    CHelper.TryParse(Value, out this.Start);
                                    break;
                                case "END":
                                    if (CHelper.TryParse(Value, out this.Finish))
                                        this.Finish /= 1000f;
                                    break;
                                case "PREVIEWSTART":
                                    if (CHelper.TryParse(Value, out this.PreviewStart))
                                        if (this.PreviewStart < 0f)
                                            this.PreviewStart = 0f;
                                        else
                                            HeaderFlags |= EHeaderFlags.PreviewStart;
                                    break;
                                case "MEDLEYSTARTBEAT":
                                    if (int.TryParse(Value, out this.Medley.StartBeat))
                                        HeaderFlags |= EHeaderFlags.MedleyStartBeat;
                                    break;
                                case "MEDLEYENDBEAT":
                                    if (int.TryParse(Value, out this.Medley.EndBeat))
                                        HeaderFlags |= EHeaderFlags.MedleyEndBeat;
                                    break;
                                case "CALCMEDLEY":
                                    if (Value.ToUpper() == "OFF")
                                        this.CalculateMedley = false;
                                    break;
                                case "RELATIVE":
                                    if (Value.ToUpper() == "YES" && Value.ToUpper() != "NO")
                                        this.Relative = EOffOn.TR_CONFIG_ON;
                                    break;
                            }
                        }
                    }
                } //end of while

                if (sr.EndOfStream)
                {
                    //No other data then header
                    CBase.Log.LogError("Lyrics/Notes missing: " + FilePath);
                    return false;
                }

                if ((HeaderFlags & EHeaderFlags.Title) == 0)
                {
                    CBase.Log.LogError("Title tag missing: " + FilePath);
                    return false;
                }

                if ((HeaderFlags & EHeaderFlags.Artist) == 0)
                {
                    CBase.Log.LogError("Artist tag missing: " + FilePath);
                    return false;
                }

                if ((HeaderFlags & EHeaderFlags.MP3) == 0)
                {
                    CBase.Log.LogError("MP3 tag missing: " + FilePath);
                    return false;
                }

                if ((HeaderFlags & EHeaderFlags.BPM) == 0)
                {
                    CBase.Log.LogError("BPM tag missing: " + FilePath);
                    return false;
                }

                #region check medley tags
                if ((HeaderFlags & EHeaderFlags.MedleyStartBeat) != 0 && (HeaderFlags & EHeaderFlags.MedleyEndBeat) != 0)
                {
                    if (this.Medley.StartBeat > this.Medley.EndBeat)
                    {
                        CBase.Log.LogError("MedleyStartBeat is bigger than MedleyEndBeat in file: " + FilePath);
                        HeaderFlags = HeaderFlags - EHeaderFlags.MedleyStartBeat - EHeaderFlags.MedleyEndBeat;
                    }
                }

                if ((HeaderFlags & EHeaderFlags.PreviewStart) == 0 || this.PreviewStart == 0f)
                {
                    //PreviewStart is not set or <=0
                    if ((HeaderFlags & EHeaderFlags.MedleyStartBeat) != 0)
                        //fallback to MedleyStart
                        this.PreviewStart = CBase.Game.GetTimeFromBeats(this.Medley.StartBeat, this.BPM);
                    else
                        //else set to 0, it will be set in FindRefrainStart
                        this.PreviewStart = 0f;
                }

                if ((HeaderFlags & EHeaderFlags.MedleyStartBeat) != 0 && (HeaderFlags & EHeaderFlags.MedleyEndBeat) != 0)
                {
                    this.Medley.Source = EMedleySource.Tag;
                    this.Medley.FadeInTime = CBase.Settings.GetDefaultMedleyFadeInTime();
                    this.Medley.FadeOutTime = CBase.Settings.GetDefaultMedleyFadeOutTime();
                }
                #endregion check medley tags

                this.Encoding = sr.CurrentEncoding;
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Error reading txt header in file \"" + FilePath + "\": " + e.Message);
                return false;
            }

            CheckFiles();

            //Before saving this tags to .txt: Check, if ArtistSorting and Artist are equal, then don't save this tag.
            if (this.ArtistSorting == String.Empty) 
            {
                this.ArtistSorting = this.Artist;
            }

            if (this.TitleSorting == String.Empty)
            {
                this.TitleSorting = this.Title;
            }

            return true;
        }

        public bool ReadNotes()
        {
            return ReadNotes(Path.Combine(this.Folder, this.FileName));
        }
        public bool ReadNotes(string FilePath, bool forceReload=false)
        {
            //Skip loading if already done and no reload is forced
            if (_NotesLoaded && !forceReload)
                return true;

            if (!File.Exists(FilePath))
            {
                CBase.Log.LogError("Error loading song. The file does not exist: " + FilePath);
                return false;
            }

            char TempC = char.MinValue;
            int Param1 = 0;
            int Param2 = 0;
            int Param3 = 0;
            int currentPos = 0;
            string ParamS = String.Empty;
            bool isNewSentence = false;

            string line = String.Empty;
            int Player = 0;
            int FileLineNo = 0;

            StreamReader sr;
            try
            {

                sr = new StreamReader(FilePath, this.Encoding, true);
                
                this.Notes.Reset();

                //Search for Note Beginning
                while(!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    FileLineNo++;

                    TempC = (char)sr.Peek();
                    if ((TempC == '#') || (TempC == '\r') || (TempC == '\n')) 
                        continue;
                    if ((TempC == ':') || (TempC == 'F') || (TempC == '*') || (TempC == 'P'))
                    {
                        TempC = (char)sr.Read();
                        break;
                    }
                }

                if (sr.EndOfStream)
                {
                    CBase.Log.LogError("Error loading song. Line No.: " + FileLineNo.ToString() + ". No lyrics/notes found: " + FilePath);
                    return false;
                }

                do
                {
              
                    char chr;
                    switch (TempC)
                    {
                        case 'P':
                            while ((chr = (char)sr.Read()) == ' ') { }

                            int.TryParse(chr.ToString(), out Param1);
                            if (Param1 == 1)
                                Player = 0;
                            else if (Param1 == 2)
                                Player = 1;
                            else if (Param1 == 3)
                                Player = 2;
                            else
                            {
                                CBase.Log.LogError("Error loading song. Line No.: " + FileLineNo.ToString() + ". Wrong or missing number after \"P\": " + FilePath);
                                return false;
                            }
                            sr.ReadLine();
                            break;
                        case ':':
                        case '*':
                        case 'F':
                            sr.Read();
                            Param1 = CHelper.TryReadInt(sr);

                            sr.Read();
                            Param2 = CHelper.TryReadInt(sr);

                            sr.Read();
                            Param3 = CHelper.TryReadInt(sr);

                            sr.Read();
                            ParamS = sr.ReadLine();

                            ENoteType NoteType;

                            if (TempC.CompareTo('*') == 0)
                                NoteType = ENoteType.Golden;
                            else if (TempC.CompareTo('F') == 0)
                                NoteType = ENoteType.Freestyle;
                            else
                                NoteType = ENoteType.Normal;

                            if (this.Relative == EOffOn.TR_CONFIG_ON)
                                Param1 += currentPos;

                            if (Player != 2)
                                // one singer
                                ParseNote(Player, NoteType, Param1, Param2, Param3, ParamS);
                            else
                            {
                                // both singer
                                ParseNote(0, NoteType, Param1, Param2, Param3, ParamS);
                                ParseNote(1, NoteType, Param1, Param2, Param3, ParamS);
                            }
                            isNewSentence = false;
                            break;
                        case '-':
                            if (isNewSentence)
                            {
                                CBase.Log.LogError("Error loading song. Line No.: " + FileLineNo.ToString() + ". Double sentence break: " + FilePath);
                                return false;
                            }
                            sr.Read();
                            Param1 = CHelper.TryReadInt(sr);

                            if (this.Relative == EOffOn.TR_CONFIG_ON)
                            {
                                Param1 += currentPos;
                                sr.Read();
                                Param2 = CHelper.TryReadInt(sr);
                                currentPos += Param2;
                            }

                            if (Player != 2)
                                // one singer
                                NewSentence(Player, Param1);
                            else
                            {
                                // both singer
                                NewSentence(0, Param1);
                                NewSentence(1, Param1);
                            }

                            isNewSentence = true;
                            sr.ReadLine();
                            break;
                        default:
                            CBase.Log.LogError("Error loading song. Line No.: " + FileLineNo.ToString() + ". Unexpected or missing character (" + TempC + "): " + FilePath);
                            return false;
                    }
                    int c = 0;
                    do
                    {
                        c = sr.Read();
                    } while (!sr.EndOfStream && (c == 19 || c == 16 || c == 13 || c == 10));
                    TempC = (char)c;
                    FileLineNo++;
                }while(!sr.EndOfStream && (TempC != 'E'));

                foreach(CLines lines in this.Notes.Lines)
                {
                    lines.UpdateTimings();
                }
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Error loading song. Line No.: " + FileLineNo.ToString() + ". An unhandled exception occured (" + e.Message + "): " + FilePath);
                return false;
            }
            FindRefrain();
            FindShortEnd();
            _NotesLoaded = true;
            if(IsDuet)
                CheckDuet();
            return true;
        }

        private bool ParseNote(int Player, ENoteType NoteType, int Start, int Length, int Tone, string Text)
        {
            CNote note = new CNote(Start, Length, Tone, Text, NoteType);
            CLines lines = this.Notes.GetLines(Player);
            return lines.AddNote(note);
        }

        private void NewSentence(int Player, int Start)
        {
            CLines lines = Notes.GetLines(Player);
            CLine line = new CLine();
            line.StartBeat = Start;
            lines.AddLine(line);   
        }

        public void LoadSmallCover()
        {
            if (_CoverSmallLoaded)
                return;
            if (this.CoverFileName != String.Empty)
            {
                if (!CBase.DataBase.GetCover(Path.Combine(this.Folder, this.CoverFileName), ref _CoverTextureSmall, CBase.Config.GetCoverSize()))
                    _CoverTextureSmall = CBase.Cover.GetNoCover();
            }
            else
                _CoverTextureSmall = CBase.Cover.GetNoCover();

            _CoverSmallLoaded = true;
        }

        private void CheckFiles()
        {
            if(this.CoverFileName == String.Empty){
                List<string> files = CHelper.ListFiles(this.Folder, "*.jpg", false);
                files.AddRange(CHelper.ListFiles(this.Folder, "*.png", false));
                foreach(String file in files)
                {
                    if (Regex.IsMatch(file, @".[CO].", RegexOptions.IgnoreCase) && (Regex.IsMatch(file, @"" + Regex.Escape(this.Title), RegexOptions.IgnoreCase) || Regex.IsMatch(file, @"" + Regex.Escape(this.Artist), RegexOptions.IgnoreCase)))
                    {
                        this.CoverFileName = file;
                    }
                }
            }

            if (this.BackgroundFileName == String.Empty)
            {
                List<string> files = CHelper.ListFiles(this.Folder, "*.jpg", false);
                files.AddRange(CHelper.ListFiles(this.Folder, "*.png", false));
                foreach (String file in files)
                {

                    if (Regex.IsMatch(file, @".[BG].", RegexOptions.IgnoreCase) && (Regex.IsMatch(file, @"" + Regex.Escape(this.Title), RegexOptions.IgnoreCase) || Regex.IsMatch(file, @"" + Regex.Escape(this.Artist), RegexOptions.IgnoreCase)))
                    {
                        this.BackgroundFileName = file;
                    }
                }
            }
        }

        private void CheckDuet()
        {
            if (DuetPart1 == String.Empty)
            {
                DuetPart1 = "Part 1";
                CBase.Log.LogError("Warning: Can't find #P1-tag for duets in \"" + this.Artist + " - " + this.Title + "\".");
            }
            if (DuetPart2 == String.Empty)
            {
                DuetPart2 = "Part 2";
                CBase.Log.LogError("Warning: Can't find #P2-tag for duets in \"" + this.Artist + " - " + this.Title + "\".");
            }
        }

        private struct Series
        {
            public int start;
            public int end;
            public int length;
        }

        private List<Series> GetSeries()
        {
            CLines lines = this.Notes.GetLines(0);

            if (lines.LineCount == 0)
                return null;

            // build sentences list
            List<string> sentences = new List<string>();
            foreach (CLine line in lines.Line)
            {
                if (line.Points != 0)
                    sentences.Add(line.Lyrics);
                else
                    sentences.Add(String.Empty);
            }

            // find equal sentences series
            List<Series> series = new List<Series>();
            for (int i = 0; i < lines.LineCount - 1; i++)
            {
                for (int j = i + 1; j < lines.LineCount; j++)
                {
                    if (sentences[i] == sentences[j] && sentences[i] != String.Empty)
                    {
                        Series tempSeries = new Series();
                        tempSeries.start = i;
                        tempSeries.end = i;

                        int max = 0;
                        if (j + j - i - 1 > lines.LineCount - 1)
                            max = lines.LineCount - 1 - j;
                        else
                            max = j - i - 1;

                        for (int k = 1; k <= max; k++)
                        {
                            if (sentences[i + k] == sentences[j + k] && sentences[i + k] != String.Empty)
                                tempSeries.end = i + k;
                            else
                                break;
                        }

                        tempSeries.length = tempSeries.end - tempSeries.start + 1;
                        series.Add(tempSeries);
                    }
                }
            }
            return series;
        }

        private void FindRefrain()
        {
            if (this.IsDuet)
            {
                this.Medley.Source = EMedleySource.None;
                return;
            }

            if (this.Medley.Source == EMedleySource.Tag)
                return;

            if (!this.CalculateMedley)
                return;

            List<Series> series = GetSeries();
            if (series == null)
                return;

            // search for longest series
            int longest = 0;
            for (int i = 0; i < series.Count; i++)
            {
                if (series[i].length > series[longest].length)
                    longest = i;
            }

            CLines lines = this.Notes.GetLines(0);

            // set medley vars
            if (series.Count > 0 && series[longest].length > CBase.Settings.GetMedleyMinSeriesLength())
            {
                this.Medley.StartBeat = lines.Line[series[longest].start].FirstNoteBeat;
                this.Medley.EndBeat = lines.Line[series[longest].end].LastNoteBeat;

                bool foundEnd = false;
                
                // set end if duration > MedleyMinDuration
                if (CBase.Game.GetTimeFromBeats(this.Medley.StartBeat, this.BPM) + CBase.Settings.GetMedleyMinDuration() >
                    CBase.Game.GetTimeFromBeats(this.Medley.EndBeat, this.BPM))
                    foundEnd = true;

                if (!foundEnd)
                {
                    for (int i = series[longest].start + 1; i < lines.LineCount - 1; i++)
                    {
                        if (CBase.Game.GetTimeFromBeats(this.Medley.StartBeat, this.BPM) + CBase.Settings.GetMedleyMinDuration() >
                            CBase.Game.GetTimeFromBeats(lines.Line[i].LastNoteBeat, this.BPM))
                        {
                            foundEnd = true;
                            this.Medley.EndBeat = lines.Line[i].LastNoteBeat;
                        }
                    }
                }

                if (foundEnd)
                {
                    this.Medley.Source = EMedleySource.Calculated;
                    this.Medley.FadeInTime = CBase.Settings.GetDefaultMedleyFadeInTime();
                    this.Medley.FadeOutTime = CBase.Settings.GetDefaultMedleyFadeOutTime();
                }
            }
            
            if (this.PreviewStart == 0f)
            {
                if (this.Medley.Source == EMedleySource.Calculated)
                    this.PreviewStart = CBase.Game.GetTimeFromBeats(this.Medley.StartBeat, this.BPM); 
            }
        }

        private void FindShortEnd(){
            List<Series> series = GetSeries();
            if (series == null)
                return;

            CLines lines = this.Notes.GetLines(0);

            //Calculate length of singing
            int stop = (lines.Line[lines.Line.Length - 1].LastNoteBeat - lines.Line[0].FirstNoteBeat) / 2 + lines.Line[0].StartBeat;

            //Check if stop is in series
            for (int i = 0; i < series.Count; i++)
            {
                if (lines.Line[series[i].start].FirstNoteBeat < stop && lines.Line[series[i].end].LastNoteBeat > stop)
                {
                    if (stop < (lines.Line[series[i].start].FirstNoteBeat + ((lines.Line[series[i].end].LastNoteBeat - lines.Line[series[i].start].FirstNoteBeat) / 2)))
                    {
                        this.ShortEnd = lines.Line[series[i].start-1].LastNote.EndBeat;
                        return;
                    }
                    else
                    {
                        this.ShortEnd = lines.Line[series[i].end].LastNote.EndBeat;
                        return;
                    }
                }
            }

            //Check if stop is in line
            for (int i = 0; i < lines.Line.Length; i++)
            {
                if (lines.Line[i].FirstNoteBeat < stop && lines.Line[i].LastNoteBeat > stop)
                {
                    this.ShortEnd = lines.Line[i].LastNoteBeat;
                    return;
                }
            }

            ShortEnd = stop;

        }

        public object Clone()
        {
            return base.MemberwiseClone();
        }
    }
}
