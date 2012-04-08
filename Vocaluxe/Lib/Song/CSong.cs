using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;

namespace Vocaluxe.Lib.Song
{
    [Flags]
    enum EHeaderFlags
    {
        Title,
        Artist,
        MP3,
        BPM,
        PreviewStart,
        MedleyStartBeat,
        MedleyEndBeat
    }

    class CSong
    {
        private bool _CoverLoaded = false;
        private bool _NotesLoaded = false;
        private STexture _CoverTextureSmall = new STexture(-1);
        private STexture _CoverTextureBig = new STexture(-1);

        public Encoding Encoding = Encoding.Default;
        public string Folder = String.Empty;
        public string FolderName = String.Empty;
        public string FileName = String.Empty;

        public string MP3FileName = String.Empty;
        public string CoverFileName = String.Empty;
        public string BackgroundFileName = String.Empty;
        public string VideoFileName = String.Empty;

        public EAspect VideoAspect = EAspect.Crop;

        public bool CoverSmallLoaded
        {
            get { return _CoverLoaded; }
        }

        public bool CoverBigLoaded = false;
        public bool BackgroundLoaded = false;

        public STexture CoverTextureSmall
        {
            get
            {
                if (!_CoverLoaded)
                {
                    if (!_NotesLoaded)
                        this.ReadNotes();

                    if (this.CoverFileName != String.Empty)
                    {
                        if (!CDataBase.GetCover(Path.Combine(this.Folder, this.CoverFileName), ref _CoverTextureSmall, CConfig.CoverSize))
                            _CoverTextureSmall = CCover.NoCover;
                    }
                    else
                        _CoverTextureSmall = CCover.NoCover;

                    _CoverLoaded = true;
                }
                return _CoverTextureSmall;
            }

            set
            {
                _CoverTextureSmall = value;
                _CoverLoaded = true;
            }
        }

        public STexture CoverTextureBig
        {
            get { return _CoverTextureBig; }
            set { _CoverTextureBig = value; }
        }

        public CBackground Background = new CBackground();

        public string Title = String.Empty;
        public string Artist = String.Empty;

        public float Start = 0f;
        public float Finish = 0f;
        
        public float BPM = 1f;
        public float Gap = 0f;
        public float VideoGap = 0f;

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
        public string Year = "0000";
        public List<string> Language = new List<string>();

        // Notes
        public CNotes Notes = new CNotes();

        public string GetMP3()
        {
            return Path.Combine(Folder, MP3FileName);
        }

        public bool ReadTXTSong(string FilePath)
        {
            if (!File.Exists(FilePath))
                return false;

            if (!ReadTXTHeader(FilePath))
                return false;

            return true;
        }

        public bool ReadTXTHeader(string FilePath)
        {
            if (!File.Exists(FilePath))
                return false;

            this.Folder = Path.GetDirectoryName(FilePath);

            foreach (string folder in CConfig.SongFolder)
            {
                if (this.Folder.Contains(folder))
                {
                    if (this.Folder.Length == folder.Length)
                        this.FolderName = "Songs";
                    else
                    {
                        this.FolderName = this.Folder.Substring(folder.Length + 1, this.Folder.Length - folder.Length - 1);

                        string str = this.FolderName;
                        try
                        {
                            str = this.FolderName.Substring(0, this.FolderName.IndexOf("\\"));
                        }
                        catch (Exception)
                        {

                        }
                        this.FolderName = str;
                    }
                }
            }

            this.FileName = Path.GetFileName(FilePath);

            EHeaderFlags HeaderFlags = new EHeaderFlags();
            StreamReader sr;
            try
            {

                sr = new StreamReader(FilePath, Encoding.Default, true);

                string line = sr.ReadLine();
                if (line.Length == 0)
                    return false;

                int pos = -1;
                string Identifier = String.Empty;
                string Value = String.Empty;

                while ((line.Length == 0) || (line[0].ToString().Equals("#")))
                {
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
                                    line = sr.ReadLine();

                                    while ((line.Length == 0) || (line[0].ToString().Equals("#")) && (Identifier != "ENCODING"))
                                    {
                                        pos = line.IndexOf(":");

                                        if (pos > 0)
                                        {
                                            Identifier = line.Substring(1, pos - 1).Trim().ToUpper();
                                            Value = line.Substring(pos + 1, line.Length - pos - 1).Trim();
                                        }

                                        if (!sr.EndOfStream)
                                            if (Identifier == "ENCODING")
                                                break;
                                            else
                                                line = sr.ReadLine();
                                        else
                                        {
                                            return false;
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
                                case "MP3":
                                    if (File.Exists(Path.Combine(this.Folder, Value)))
                                    {
                                        this.MP3FileName = Value;
                                        HeaderFlags |= EHeaderFlags.MP3;
                                    }
                                    else
                                    {
                                        CLog.LogError("Can't find audio file: " + Path.Combine(this.Folder, Value));
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
                                    if (Value.Length == 4 && int.TryParse(Value, out num))
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
                                        CLog.LogError("Can't find video file: " + Path.Combine(this.Folder, Value));
                                        
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
                                default:
                                    ;
                                    break;
                            }
                        }
                    }

                    if (!sr.EndOfStream)
                        line = sr.ReadLine();
                    else
                    {
                        return false;
                    }
                } //end of while

                if (HeaderFlags != (EHeaderFlags.Title | EHeaderFlags.Artist | EHeaderFlags.MP3 | EHeaderFlags.BPM))
                    return false;

                this.Encoding = sr.CurrentEncoding;
            }
            catch (Exception)
            {
                return false;
            }

            CheckFiles();

            return true;
        }

        public bool ReadNotes()
        {
            return ReadNotes(Path.Combine(this.Folder, this.FileName));
        }
        public bool ReadNotes(string FilePath)
        {
            if (!File.Exists(FilePath))
            {
                CLog.LogError("Error loading song. The file does not exist: " + FilePath);
                return false;
            }

            char TempC = char.MinValue;
            int Param1 = 0;
            int Param2 = 0;
            int Param3 = 0;
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
                do
                {
                    line = sr.ReadLine();
                    FileLineNo++;

                    if (sr.EndOfStream || (line.Length == 0))
                    {
                        CLog.LogError("Error loading song. Line No.: " + FileLineNo.ToString() + ". The file is empty or it starts with an empty line: " + FilePath);
                        return false;
                    }
                    
                    TempC = (char)sr.Read();
                } while ((TempC.CompareTo(':') != 0) && (TempC.CompareTo('F') != 0) && (TempC.CompareTo('*') != 0) && (TempC.CompareTo('P') != 0));

                FileLineNo++;
                while (!sr.EndOfStream && (TempC.CompareTo('E') != 0))
                {
                    if (TempC.CompareTo('P') == 0)
                    {
                        char chr = (char)sr.Read();
                        int.TryParse(chr.ToString(), out Param1);

                        if (Param1 == 1)
                            Player = 0;
                        else if (Param1 == 2)
                            Player = 1;
                        else if (Param1 == 3)
                            Player = 2;
                        else
                        {
                            CLog.LogError("Error loading song. Line No.: " + FileLineNo.ToString() + ". Wrong or missing number after \"P\": " + FilePath);
                            return false;
                        }
                    }

                    if ((TempC.CompareTo(':') == 0) || (TempC.CompareTo('*') == 0) || (TempC.CompareTo('F') == 0))
                    {
                        char chr = (char)sr.Read();
                        Param1 = CHelper.TryReadInt(sr);

                        chr = (char)sr.Read();
                        Param2 = CHelper.TryReadInt(sr);

                        chr = (char)sr.Read();
                        Param3 = CHelper.TryReadInt(sr);

                        chr = (char)sr.Read();
                        ParamS = sr.ReadLine();

                        ENoteType NoteType = ENoteType.Normal;

                        if (TempC.CompareTo('*') == 0)
                            NoteType = ENoteType.Golden;

                        if (TempC.CompareTo('F') == 0)
                            NoteType = ENoteType.Freestyle;
	                    
                        
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
                    }

                    if (TempC.CompareTo('-') == 0)
                    {
                        if (isNewSentence)
                        {
                            CLog.LogError("Error loading song. Line No.: " + FileLineNo.ToString() + ". Double sentence break: " + FilePath);
                            return false;
                        }

                        char chr = (char)sr.Read();
                        Param1 = CHelper.TryReadInt(sr);

                        NewSentence(Player, Param1);
                        
                        isNewSentence = true;
                    }

                    int c = 0;
                    do
                    {
                        c = sr.Read();
                        TempC = (char)c;
                    } while ((TempC.CompareTo('E') != 0) && !sr.EndOfStream && (c == 19 || c == 16 || c == 13));
                }
                foreach(CLines lines in this.Notes.Lines)
                {
                    lines.UpdateTimings();
                }
            }
            catch (Exception e)
            {
                CLog.LogError("Error loading song. Line No.: " + FileLineNo.ToString() + ". An unhandled exception occured (" + e.Message + "): " + FilePath);
                return false;
            }
            _NotesLoaded = true;
            return true;
        }

        private void ParseNote(int Player, ENoteType NoteType, int Start, int Length, int Tone, string Text)
        {
            CNote note = new CNote(Start, Length, Tone, Text, NoteType);
            CLines lines = this.Notes.GetLines(Player);
            
            if (lines.LineCount == 0)
            {
                CLine line = new CLine();
                line.AddNote(note);
                lines.AddLine(line, false);
            }
            else
            {
                lines.AddNote(note, lines.LineCount - 1, false);
            }        
        }

        private void NewSentence(int Player, int Start)
        {
            CLines lines = Notes.GetLines(Player);
            CLine line = new CLine();
            line.StartBeat = Start;
            if (lines.LineCount == 0)
            {
                lines.AddLine(line);
            }
            else
            {
                lines.AddLine(line);
            }        
        }

        private void CheckFiles()
        {
            CHelper Helper = new CHelper();

            if(this.CoverFileName == String.Empty){
                List<string> files = Helper.ListFiles(this.Folder, "*.jpg", false);
                files.AddRange(Helper.ListFiles(this.Folder, "*.png", false));
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
                List<string> files = Helper.ListFiles(this.Folder, "*.jpg", false);
                files.AddRange(Helper.ListFiles(this.Folder, "*.png", false));
                foreach (String file in files)
                {

                    if (Regex.IsMatch(file, @".[BG].", RegexOptions.IgnoreCase) && (Regex.IsMatch(file, @"" + Regex.Escape(this.Title), RegexOptions.IgnoreCase) || Regex.IsMatch(file, @"" + Regex.Escape(this.Artist), RegexOptions.IgnoreCase)))
                    {
                        this.BackgroundFileName = file;
                    }
                }
            }
        }
    }
}
