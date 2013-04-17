using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using VocaluxeLib.Menu.SingNotes;

namespace VocaluxeLib.Menu.SongMenu
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
        private STexture _CoverTextureSmall = new STexture(-1);
        private STexture _CoverTextureBig = new STexture(-1);
        private bool _CoverBigLoaded;

        public CCategory(string name)
        {
            Name = name;
        }

        public STexture CoverTextureSmall
        {
            get { return _CoverTextureSmall; }

            set { _CoverTextureSmall = value; }
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
            set
            {
                if (value.Index != -1)
                {
                    _CoverTextureBig = value;
                    _CoverBigLoaded = true;
                }
            }
        }

        public CCategory(string name, STexture coverSmall, STexture coverBig)
        {
            Name = name;
            CoverTextureSmall = coverSmall;
            CoverTextureBig = coverBig;
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
        private bool _CoverSmallLoaded;
        private bool _CoverBigLoaded;
        private bool _NotesLoaded;
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
                    LoadSmallCover();
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
            set
            {
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
        private CSong() {}

        public static CSong LoadSong(string filePath)
        {
            CSong song = new CSong();
            return song.ReadHeader(filePath) ? song : null;
        }

        public CSong(CSong song)
        {
            _CoverTextureSmall = song._CoverTextureSmall;
            _CoverTextureBig = song._CoverTextureBig;

            Medley = new SMedley();
            Medley.Source = song.Medley.Source;
            Medley.StartBeat = song.Medley.StartBeat;
            Medley.EndBeat = song.Medley.EndBeat;
            Medley.FadeInTime = song.Medley.FadeInTime;
            Medley.FadeOutTime = song.Medley.FadeOutTime;

            CalculateMedley = song.CalculateMedley;
            PreviewStart = song.PreviewStart;

            ShortEnd = song.ShortEnd;
            DuetPart1 = song.DuetPart1;
            DuetPart2 = song.DuetPart2;

            Encoding = song.Encoding;
            Folder = song.Folder;
            FolderName = song.FolderName;
            FileName = song.FileName;
            Relative = song.Relative;

            MP3FileName = song.MP3FileName;
            CoverFileName = song.CoverFileName;
            BackgroundFileName = song.BackgroundFileName;
            VideoFileName = song.VideoFileName;

            VideoAspect = song.VideoAspect;
            _CoverSmallLoaded = song._CoverSmallLoaded;
            _CoverBigLoaded = song._CoverBigLoaded;
            _NotesLoaded = song._NotesLoaded;

            Artist = song.Artist;
            Title = song.Title;

            Start = song.Start;
            Finish = song.Finish;

            BPM = song.BPM;
            Gap = song.Gap;
            VideoGap = song.VideoGap;

            Comment = new List<string>();
            foreach (string value in song.Comment)
                Comment.Add(value);

            ID = song.ID;
            Visible = song.Visible;
            CatIndex = song.CatIndex;
            Selected = song.Selected;

            Edition = new List<string>();
            foreach (string value in song.Edition)
                Edition.Add(value);

            Genre = new List<string>();
            foreach (string value in song.Genre)
                Genre.Add(value);

            Year = song.Year;

            Language = new List<string>();
            foreach (string value in song.Language)
                Language.Add(value);

            Notes = new CNotes(song.Notes);
        }

        public string GetMP3()
        {
            return Path.Combine(Folder, MP3FileName);
        }

        public string GetVideo()
        {
            return Path.Combine(Folder, VideoFileName);
        }

        public bool ReadHeader(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            Folder = Path.GetDirectoryName(filePath);

            foreach (string folder in CBase.Config.GetSongFolder())
            {
                if (Folder.Contains(folder))
                {
                    if (Folder.Length == folder.Length)
                        FolderName = "Songs";
                    else
                    {
                        FolderName = Folder.Substring(folder.Length + 1, Folder.Length - folder.Length - 1);

                        int pos = FolderName.IndexOf("\\");
                        if (pos >= 0)
                            FolderName = FolderName.Substring(0, pos);
                    }
                    break;
                }
            }

            FileName = Path.GetFileName(filePath);

            EHeaderFlags headerFlags = new EHeaderFlags();
            StreamReader sr = null;
            try
            {
                sr = new StreamReader(filePath, Encoding.Default, true);

                string line;

                int pos = -1;
                string identifier = String.Empty;
                string value = String.Empty;

                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line.Length == 0)
                        continue;
                    if (!line[0].ToString().Equals("#"))
                        break;

                    pos = line.IndexOf(":");

                    if (pos > 0)
                    {
                        identifier = line.Substring(1, pos - 1).Trim().ToUpper();
                        value = line.Substring(pos + 1, line.Length - pos - 1).Trim();

                        if (value.Length > 0)
                        {
                            switch (identifier)
                            {
                                case "ENCODING":
                                    Encoding = CEncoding.GetEncoding(value);
                                    sr.Dispose();
                                    sr = null;
                                    sr = new StreamReader(filePath, Encoding);
                                    identifier = String.Empty;

                                    //Scip everything till encoding
                                    while (!sr.EndOfStream)
                                    {
                                        line = sr.ReadLine();
                                        if (line.Length == 0)
                                            continue;
                                        if (!line[0].ToString().Equals("#"))
                                            break;

                                        pos = line.IndexOf(":");

                                        if (pos > 0)
                                        {
                                            identifier = line.Substring(1, pos - 1).Trim().ToUpper();
                                            if (identifier == "ENCODING")
                                                break;
                                        }
                                    }
                                    break;
                                case "TITLE":
                                    if (value.Length > 0)
                                    {
                                        Title = value;
                                        headerFlags |= EHeaderFlags.Title;
                                    }
                                    break;
                                case "ARTIST":
                                    if (value.Length > 0)
                                    {
                                        Artist = value;
                                        headerFlags |= EHeaderFlags.Artist;
                                    }
                                    break;
                                case "TITLE-ON-SORTING":
                                    if (value.Length > 0)
                                        TitleSorting = value;
                                    break;
                                case "ARTIST-ON-SORTING":
                                    if (value.Length > 0)
                                        ArtistSorting = value;
                                    break;
                                case "P1":
                                    if (value.Length > 0)
                                        DuetPart1 = value;
                                    break;
                                case "P2":
                                    if (value.Length > 0)
                                        DuetPart2 = value;
                                    break;
                                case "MP3":
                                    if (File.Exists(Path.Combine(Folder, value)))
                                    {
                                        MP3FileName = value;
                                        headerFlags |= EHeaderFlags.MP3;
                                    }
                                    else
                                    {
                                        CBase.Log.LogError("Can't find audio file: " + Path.Combine(Folder, value));
                                        return false;
                                    }
                                    break;
                                case "BPM":
                                    if (CHelper.TryParse(value, out BPM))
                                    {
                                        BPM *= 4;
                                        headerFlags |= EHeaderFlags.BPM;
                                    }
                                    break;
                                case "EDITION":
                                    if (value.Length > 1)
                                        Edition.Add(value);
                                    break;
                                case "GENRE":
                                    if (value.Length > 1)
                                        Genre.Add(value);
                                    break;
                                case "YEAR":
                                    int num = 0;
                                    if (value.Length == 4 && int.TryParse(value, out num) && num != 0)
                                        Year = value;
                                    break;
                                case "LANGUAGE":
                                    if (value.Length > 1)
                                        Language.Add(value);
                                    break;
                                case "COMMENT":
                                    if (value.Length > 1)
                                        Comment.Add(value);
                                    break;
                                case "GAP":
                                    if (CHelper.TryParse(value, out Gap))
                                        Gap /= 1000f;
                                    break;
                                case "COVER":
                                    if (File.Exists(Path.Combine(Folder, value)))
                                        CoverFileName = value;
                                    break;
                                case "BACKGROUND":
                                    if (File.Exists(Path.Combine(Folder, value)))
                                        BackgroundFileName = value;
                                    break;
                                case "VIDEO":
                                    if (File.Exists(Path.Combine(Folder, value)))
                                        VideoFileName = value;
                                    else
                                        CBase.Log.LogError("Can't find video file: " + Path.Combine(Folder, value));
                                    break;
                                case "VIDEOGAP":
                                    CHelper.TryParse(value, out VideoGap);
                                    break;
                                case "VIDEOASPECT":
                                    CHelper.TryParse(value, out VideoAspect, true);
                                    break;
                                case "START":
                                    CHelper.TryParse(value, out Start);
                                    break;
                                case "END":
                                    if (CHelper.TryParse(value, out Finish))
                                        Finish /= 1000f;
                                    break;
                                case "PREVIEWSTART":
                                    if (CHelper.TryParse(value, out PreviewStart))
                                    {
                                        if (PreviewStart < 0f)
                                            PreviewStart = 0f;
                                        else
                                            headerFlags |= EHeaderFlags.PreviewStart;
                                    }
                                    break;
                                case "MEDLEYSTARTBEAT":
                                    if (int.TryParse(value, out Medley.StartBeat))
                                        headerFlags |= EHeaderFlags.MedleyStartBeat;
                                    break;
                                case "MEDLEYENDBEAT":
                                    if (int.TryParse(value, out Medley.EndBeat))
                                        headerFlags |= EHeaderFlags.MedleyEndBeat;
                                    break;
                                case "CALCMEDLEY":
                                    if (value.ToUpper() == "OFF")
                                        CalculateMedley = false;
                                    break;
                                case "RELATIVE":
                                    if (value.ToUpper() == "YES" && value.ToUpper() != "NO")
                                        Relative = EOffOn.TR_CONFIG_ON;
                                    break;
                            }
                        }
                    }
                } //end of while

                if (sr.EndOfStream)
                {
                    //No other data then header
                    CBase.Log.LogError("Lyrics/Notes missing: " + filePath);
                    return false;
                }

                if ((headerFlags & EHeaderFlags.Title) == 0)
                {
                    CBase.Log.LogError("Title tag missing: " + filePath);
                    return false;
                }

                if ((headerFlags & EHeaderFlags.Artist) == 0)
                {
                    CBase.Log.LogError("Artist tag missing: " + filePath);
                    return false;
                }

                if ((headerFlags & EHeaderFlags.MP3) == 0)
                {
                    CBase.Log.LogError("MP3 tag missing: " + filePath);
                    return false;
                }

                if ((headerFlags & EHeaderFlags.BPM) == 0)
                {
                    CBase.Log.LogError("BPM tag missing: " + filePath);
                    return false;
                }

                #region check medley tags
                if ((headerFlags & EHeaderFlags.MedleyStartBeat) != 0 && (headerFlags & EHeaderFlags.MedleyEndBeat) != 0)
                {
                    if (Medley.StartBeat > Medley.EndBeat)
                    {
                        CBase.Log.LogError("MedleyStartBeat is bigger than MedleyEndBeat in file: " + filePath);
                        headerFlags = headerFlags - EHeaderFlags.MedleyStartBeat - EHeaderFlags.MedleyEndBeat;
                    }
                }

                if ((headerFlags & EHeaderFlags.PreviewStart) == 0 || PreviewStart == 0f)
                {
                    //PreviewStart is not set or <=0
                    if ((headerFlags & EHeaderFlags.MedleyStartBeat) != 0)
                        //fallback to MedleyStart
                        PreviewStart = CBase.Game.GetTimeFromBeats(Medley.StartBeat, BPM);
                    else
                        //else set to 0, it will be set in FindRefrainStart
                        PreviewStart = 0f;
                }

                if ((headerFlags & EHeaderFlags.MedleyStartBeat) != 0 && (headerFlags & EHeaderFlags.MedleyEndBeat) != 0)
                {
                    Medley.Source = EMedleySource.Tag;
                    Medley.FadeInTime = CBase.Settings.GetDefaultMedleyFadeInTime();
                    Medley.FadeOutTime = CBase.Settings.GetDefaultMedleyFadeOutTime();
                }
                #endregion check medley tags

                Encoding = sr.CurrentEncoding;
            }
            catch (Exception e)
            {
                if (sr != null)
                    sr.Dispose();
                CBase.Log.LogError("Error reading txt header in file \"" + filePath + "\": " + e.Message);
                return false;
            }
            sr.Dispose();
            _CheckFiles();

            //Before saving this tags to .txt: Check, if ArtistSorting and Artist are equal, then don't save this tag.
            if (ArtistSorting.Length == 0)
                ArtistSorting = Artist;

            if (TitleSorting.Length == 0)
                TitleSorting = Title;

            return true;
        }

        public bool ReadNotes()
        {
            return _ReadNotes(Path.Combine(Folder, FileName));
        }

        private bool _ReadNotes(string filePath, bool forceReload = false)
        {
            //Skip loading if already done and no reload is forced
            if (_NotesLoaded && !forceReload)
                return true;

            if (!File.Exists(filePath))
            {
                CBase.Log.LogError("Error loading song. The file does not exist: " + filePath);
                return false;
            }

            char tempC = char.MinValue;
            int param1 = 0;
            int param2 = 0;
            int param3 = 0;
            int currentPos = 0;
            string paramS = String.Empty;
            bool isNewSentence = false;

            string line = String.Empty;
            int player = 0;
            int fileLineNo = 0;

            StreamReader sr = null;
            try
            {
                sr = new StreamReader(filePath, Encoding, true);

                Notes.Reset();

                //Search for Note Beginning
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    fileLineNo++;

                    tempC = (char)sr.Peek();
                    if ((tempC == '#') || (tempC == '\r') || (tempC == '\n'))
                        continue;
                    if ((tempC == ':') || (tempC == 'F') || (tempC == '*') || (tempC == 'P'))
                    {
                        tempC = (char)sr.Read();
                        break;
                    }
                }

                if (sr.EndOfStream)
                {
                    CBase.Log.LogError("Error loading song. Line No.: " + fileLineNo.ToString() + ". No lyrics/notes found: " + filePath);
                    return false;
                }

                do
                {
                    char chr;
                    switch (tempC)
                    {
                        case 'P':
                            while ((chr = (char)sr.Read()) == ' ') {}

                            int.TryParse(chr.ToString(), out param1);
                            if (param1 == 1)
                                player = 0;
                            else if (param1 == 2)
                                player = 1;
                            else if (param1 == 3)
                                player = 2;
                            else
                            {
                                CBase.Log.LogError("Error loading song. Line No.: " + fileLineNo.ToString() + ". Wrong or missing number after \"P\": " + filePath);
                                return false;
                            }
                            sr.ReadLine();
                            break;
                        case ':':
                        case '*':
                        case 'F':
                            sr.Read();
                            param1 = CHelper.TryReadInt(sr);

                            sr.Read();
                            param2 = CHelper.TryReadInt(sr);

                            sr.Read();
                            param3 = CHelper.TryReadInt(sr);

                            sr.Read();
                            paramS = sr.ReadLine();

                            ENoteType noteType;

                            if (tempC.CompareTo('*') == 0)
                                noteType = ENoteType.Golden;
                            else if (tempC.CompareTo('F') == 0)
                                noteType = ENoteType.Freestyle;
                            else
                                noteType = ENoteType.Normal;

                            if (Relative == EOffOn.TR_CONFIG_ON)
                                param1 += currentPos;

                            if (player != 2)
                                // one singer
                                _ParseNote(player, noteType, param1, param2, param3, paramS);
                            else
                            {
                                // both singer
                                _ParseNote(0, noteType, param1, param2, param3, paramS);
                                _ParseNote(1, noteType, param1, param2, param3, paramS);
                            }
                            isNewSentence = false;
                            break;
                        case '-':
                            if (isNewSentence)
                            {
                                CBase.Log.LogError("Error loading song. Line No.: " + fileLineNo.ToString() + ". Double sentence break: " + filePath);
                                return false;
                            }
                            sr.Read();
                            param1 = CHelper.TryReadInt(sr);

                            if (Relative == EOffOn.TR_CONFIG_ON)
                            {
                                param1 += currentPos;
                                sr.Read();
                                param2 = CHelper.TryReadInt(sr);
                                currentPos += param2;
                            }

                            if (player != 2)
                                // one singer
                                _NewSentence(player, param1);
                            else
                            {
                                // both singer
                                _NewSentence(0, param1);
                                _NewSentence(1, param1);
                            }

                            isNewSentence = true;
                            sr.ReadLine();
                            break;
                        default:
                            CBase.Log.LogError("Error loading song. Line No.: " + fileLineNo.ToString() + ". Unexpected or missing character (" + tempC + "): " + filePath);
                            return false;
                    }
                    int c = 0;
                    do
                    {
                        c = sr.Read();
                    } while (!sr.EndOfStream && (c == 19 || c == 16 || c == 13 || c == 10));
                    tempC = (char)c;
                    fileLineNo++;
                } while (!sr.EndOfStream && (tempC != 'E'));

                foreach (CLines lines in Notes.Lines)
                    lines.UpdateTimings();
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Error loading song. Line No.: " + fileLineNo.ToString() + ". An unhandled exception occured (" + e.Message + "): " + filePath);
                if (sr != null)
                    sr.Dispose();
                return false;
            }
            sr.Dispose();
            try
            {
                _FindRefrain();
                _FindShortEnd();
                _NotesLoaded = true;
                if (IsDuet)
                    _CheckDuet();
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Error loading song. An unhandled exception occured (" + e.Message + "): " + filePath);
                return false;
            }
            return true;
        }

        private bool _ParseNote(int player, ENoteType noteType, int start, int length, int tone, string text)
        {
            CNote note = new CNote(start, length, tone, text, noteType);
            CLines lines = Notes.GetLines(player);
            return lines.AddNote(note);
        }

        private void _NewSentence(int player, int start)
        {
            CLines lines = Notes.GetLines(player);
            CLine line = new CLine();
            line.StartBeat = start;
            lines.AddLine(line);
        }

        public void LoadSmallCover()
        {
            if (_CoverSmallLoaded)
                return;
            if (CoverFileName.Length > 0)
            {
                if (!CBase.DataBase.GetCover(Path.Combine(Folder, CoverFileName), ref _CoverTextureSmall, CBase.Config.GetCoverSize()))
                    _CoverTextureSmall = CBase.Cover.GetNoCover();
            }
            else
                _CoverTextureSmall = CBase.Cover.GetNoCover();

            _CoverSmallLoaded = true;
        }

        private void _CheckFiles()
        {
            if (CoverFileName.Length == 0)
            {
                List<string> files = CHelper.ListFiles(Folder, "*.jpg", false);
                files.AddRange(CHelper.ListFiles(Folder, "*.png", false));
                foreach (String file in files)
                {
                    if (Regex.IsMatch(file, @".[CO].", RegexOptions.IgnoreCase) &&
                        (Regex.IsMatch(file, @"" + Regex.Escape(Title), RegexOptions.IgnoreCase) || Regex.IsMatch(file, @"" + Regex.Escape(Artist), RegexOptions.IgnoreCase)))
                        CoverFileName = file;
                }
            }

            if (BackgroundFileName.Length == 0)
            {
                List<string> files = CHelper.ListFiles(Folder, "*.jpg", false);
                files.AddRange(CHelper.ListFiles(Folder, "*.png", false));
                foreach (String file in files)
                {
                    if (Regex.IsMatch(file, @".[BG].", RegexOptions.IgnoreCase) &&
                        (Regex.IsMatch(file, @"" + Regex.Escape(Title), RegexOptions.IgnoreCase) || Regex.IsMatch(file, @"" + Regex.Escape(Artist), RegexOptions.IgnoreCase)))
                        BackgroundFileName = file;
                }
            }
        }

        private void _CheckDuet()
        {
            if (DuetPart1.Length == 0)
            {
                DuetPart1 = "Part 1";
                CBase.Log.LogError("Warning: Can't find #P1-tag for duets in \"" + Artist + " - " + Title + "\".");
            }
            if (DuetPart2.Length == 0)
            {
                DuetPart2 = "Part 2";
                CBase.Log.LogError("Warning: Can't find #P2-tag for duets in \"" + Artist + " - " + Title + "\".");
            }
        }

        private struct SSeries
        {
            public int Start;
            public int End;
            public int Length;
        }

        private List<SSeries> _GetSeries()
        {
            CLines lines = Notes.GetLines(0);

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
            List<SSeries> series = new List<SSeries>();
            for (int i = 0; i < lines.LineCount - 1; i++)
            {
                for (int j = i + 1; j < lines.LineCount; j++)
                {
                    if (sentences[i] == sentences[j] && sentences[i].Length > 0)
                    {
                        SSeries tempSeries = new SSeries();
                        tempSeries.Start = i;
                        tempSeries.End = i;

                        int max = 0;
                        if (j + j - i - 1 > lines.LineCount - 1)
                            max = lines.LineCount - 1 - j;
                        else
                            max = j - i - 1;

                        for (int k = 1; k <= max; k++)
                        {
                            if (sentences[i + k] == sentences[j + k] && sentences[i + k].Length > 0)
                                tempSeries.End = i + k;
                            else
                                break;
                        }

                        tempSeries.Length = tempSeries.End - tempSeries.Start + 1;
                        series.Add(tempSeries);
                    }
                }
            }
            return series;
        }

        private void _FindRefrain()
        {
            if (IsDuet)
            {
                Medley.Source = EMedleySource.None;
                return;
            }

            if (Medley.Source == EMedleySource.Tag)
                return;

            if (!CalculateMedley)
                return;

            List<SSeries> series = _GetSeries();
            if (series == null)
                return;

            // search for longest series
            int longest = 0;
            for (int i = 0; i < series.Count; i++)
            {
                if (series[i].Length > series[longest].Length)
                    longest = i;
            }

            CLines lines = Notes.GetLines(0);

            // set medley vars
            if (series.Count > 0 && series[longest].Length > CBase.Settings.GetMedleyMinSeriesLength())
            {
                Medley.StartBeat = lines.Line[series[longest].Start].FirstNoteBeat;
                Medley.EndBeat = lines.Line[series[longest].End].LastNoteBeat;

                bool foundEnd = false;

                // set end if duration > MedleyMinDuration
                if (CBase.Game.GetTimeFromBeats(Medley.StartBeat, BPM) + CBase.Settings.GetMedleyMinDuration() >
                    CBase.Game.GetTimeFromBeats(Medley.EndBeat, BPM))
                    foundEnd = true;

                if (!foundEnd)
                {
                    for (int i = series[longest].Start + 1; i < lines.LineCount - 1; i++)
                    {
                        if (CBase.Game.GetTimeFromBeats(Medley.StartBeat, BPM) + CBase.Settings.GetMedleyMinDuration() >
                            CBase.Game.GetTimeFromBeats(lines.Line[i].LastNoteBeat, BPM))
                        {
                            foundEnd = true;
                            Medley.EndBeat = lines.Line[i].LastNoteBeat;
                        }
                    }
                }

                if (foundEnd)
                {
                    Medley.Source = EMedleySource.Calculated;
                    Medley.FadeInTime = CBase.Settings.GetDefaultMedleyFadeInTime();
                    Medley.FadeOutTime = CBase.Settings.GetDefaultMedleyFadeOutTime();
                }
            }

            if (PreviewStart == 0f)
            {
                if (Medley.Source == EMedleySource.Calculated)
                    PreviewStart = CBase.Game.GetTimeFromBeats(Medley.StartBeat, BPM);
            }
        }

        private void _FindShortEnd()
        {
            List<SSeries> series = _GetSeries();
            if (series == null)
                return;

            CLines lines = Notes.GetLines(0);

            //Calculate length of singing
            int stop = (lines.Line[lines.Line.Length - 1].LastNoteBeat - lines.Line[0].FirstNote.StartBeat) / 2 + lines.Line[0].FirstNote.StartBeat;

            //Check if stop is in series
            for (int i = 0; i < series.Count; i++)
            {
                if (lines.Line[series[i].Start].FirstNoteBeat < stop && lines.Line[series[i].End].LastNoteBeat > stop)
                {
                    if (stop < (lines.Line[series[i].Start].FirstNoteBeat + ((lines.Line[series[i].End].LastNoteBeat - lines.Line[series[i].Start].FirstNoteBeat) / 2)))
                    {
                        ShortEnd = lines.Line[series[i].Start - 1].LastNote.EndBeat;
                        return;
                    }
                    else
                    {
                        ShortEnd = lines.Line[series[i].End].LastNote.EndBeat;
                        return;
                    }
                }
            }

            //Check if stop is in line
            for (int i = 0; i < lines.Line.Length; i++)
            {
                if (lines.Line[i].FirstNoteBeat < stop && lines.Line[i].LastNoteBeat > stop)
                {
                    ShortEnd = lines.Line[i].LastNoteBeat;
                    return;
                }
            }

            ShortEnd = stop;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}