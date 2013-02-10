using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;

#if WIN
using System.Data.SQLite;
#else
using Mono.Data.Sqlite;
using SQLiteConnection = Mono.Data.Sqlite.SqliteConnection;
using SQLiteTransaction = Mono.Data.Sqlite.SqliteTransaction;
using SQLiteCommand = Mono.Data.Sqlite.SqliteCommand;
using SQLiteDataReader = Mono.Data.Sqlite.SqliteDataReader;
#endif

using Community.CsharpSqlite;

using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;
using Vocaluxe.Menu.SongMenu;

namespace Vocaluxe.Base
{
    static class CDataBase
    {
        struct SData
        {
            public int id;
            public long ticks;
            public string str1;
            public string str2;
        }

        private static string _HighscoreFilePath;
        private static string _CoverFilePath;
        private static string _CreditsRessourcesFilePath;

        private static SQLiteConnection _ConnectionCover = null;
        private static SQLiteTransaction _TransactionCover = null;

        public static void Init()
        {
            _HighscoreFilePath = Path.Combine(System.Environment.CurrentDirectory, CSettings.sFileHighscoreDB);
            _CoverFilePath = Path.Combine(System.Environment.CurrentDirectory, CSettings.sFileCoverDB);
            _CreditsRessourcesFilePath = Path.Combine(System.Environment.CurrentDirectory, CSettings.sFileCreditsRessourcesDB);

            InitHighscoreDB();
            InitCoverDB();
            InitCreditsRessourcesDB();
            GC.Collect();
        }

        #region Highscores
        public static int AddScore(string PlayerName, int Score, int LineNr, long Date, int Medley, int Duet, int ShortSong, int Diff,
            string Artist, string Title, int NumPlayed, string FilePath)
        {
            SPlayer player = new SPlayer();
            player.Name = PlayerName;
            player.Points = Score;
            player.LineNr = LineNr;
            player.DateTicks = Date;
            player.Medley = (Medley == 1);
            player.Duet = (Duet == 1);
            player.ShortSong = (ShortSong == 1);
            player.Difficulty = (EGameDifficulty)Diff;

            SQLiteConnection connection = new SQLiteConnection();
            SQLiteCommand command;

            connection.ConnectionString = "Data Source=" + FilePath;
            

            try
            {
                connection.Open();
            }
            catch (Exception)
            {
                return -1;
            }

            command = new SQLiteCommand(connection);

            int DataBaseSongID = GetDataBaseSongID(Artist, Title, NumPlayed, command);
            int result = AddScore(player, command, DataBaseSongID);

            command.Dispose();
            connection.Close();
            connection.Dispose();

            return result;
        }

        public static int AddScore(SPlayer player)
        {
            SQLiteConnection connection = new SQLiteConnection();
            connection.ConnectionString = "Data Source=" + _HighscoreFilePath;
            SQLiteCommand command;

            try
            {
                connection.Open();
            }
            catch (Exception)
            {
                return -1;
            }

            command = new SQLiteCommand(connection);

            int DataBaseSongID = GetDataBaseSongID(player, command);
            int result = AddScore(player, command, DataBaseSongID);

            connection.Close();
            connection.Dispose();

            return result;
        }

        private static int AddScore(SPlayer player, SQLiteCommand command, int DataBaseSongID)
        {
            int lastInsertID = -1;

            if (DataBaseSongID >= 0)
            {

                int Medley = 0;
                if (player.Medley)
                    Medley = 1;

                int Duet = 0;
                if (player.Duet)
                    Duet = 1;

                int ShortSong = 0;
                if (player.ShortSong)
                    ShortSong = 1;

                command.CommandText = "SELECT id FROM Scores WHERE SongID = @SongID AND PlayerName = @PlayerName AND Score = @Score AND " +
                    "LineNr = @LineNr AND Date = @Date AND Medley = @Medley AND Duet = @Duet AND ShortSong = @ShortSong AND Difficulty = @Difficulty";
                command.Parameters.Add("@SongID", System.Data.DbType.Int32, 0).Value = DataBaseSongID;
                command.Parameters.Add("@PlayerName", System.Data.DbType.String, 0).Value = player.Name;
                command.Parameters.Add("@Score", System.Data.DbType.Int32, 0).Value = (int)Math.Round(player.Points);
                command.Parameters.Add("@LineNr", System.Data.DbType.Int32, 0).Value = (int)player.LineNr;
                command.Parameters.Add("@Date", System.Data.DbType.Int64, 0).Value = player.DateTicks;
                command.Parameters.Add("@Medley", System.Data.DbType.Int32, 0).Value = Medley;
                command.Parameters.Add("@Duet", System.Data.DbType.Int32, 0).Value = Duet;
                command.Parameters.Add("@ShortSong", System.Data.DbType.Int32, 0).Value = ShortSong;
                command.Parameters.Add("@Difficulty", System.Data.DbType.Int32, 0).Value = (int)player.Difficulty;

                SQLiteDataReader reader = null;
                try
                {
                    reader = command.ExecuteReader();
                }
                catch (Exception)
                {
                    ;
                }

                if (reader != null && reader.HasRows)
                {
                    if (reader.Read())
                        return reader.GetInt32(0);
                }

                if (reader != null)
                    reader.Close();


                command.CommandText = "INSERT INTO Scores (SongID, PlayerName, Score, LineNr, Date, Medley, Duet, ShortSong, Difficulty) " +
                    "VALUES (@SongID, @PlayerName, @Score, @LineNr, @Date, @Medley, @Duet, @ShortSong, @Difficulty)";
                command.Parameters.Add("@SongID", System.Data.DbType.Int32, 0).Value = DataBaseSongID;
                command.Parameters.Add("@PlayerName", System.Data.DbType.String, 0).Value = player.Name;
                command.Parameters.Add("@Score", System.Data.DbType.Int32, 0).Value = (int)Math.Round(player.Points);
                command.Parameters.Add("@LineNr", System.Data.DbType.Int32, 0).Value = (int)player.LineNr;
                command.Parameters.Add("@Date", System.Data.DbType.Int64, 0).Value = player.DateTicks;
                command.Parameters.Add("@Medley", System.Data.DbType.Int32, 0).Value = Medley;
                command.Parameters.Add("@Duet", System.Data.DbType.Int32, 0).Value = Duet;
                command.Parameters.Add("@ShortSong", System.Data.DbType.Int32, 0).Value = ShortSong;
                command.Parameters.Add("@Difficulty", System.Data.DbType.Int32, 0).Value = (int)player.Difficulty;
                command.ExecuteNonQuery();

                //Read last insert line
                command.CommandText = "SELECT id FROM Scores ORDER BY id DESC LIMIT 0, 1";

                reader = null;
                try
                {
                    reader = command.ExecuteReader();
                }
                catch (Exception)
                {
                    throw;
                }

                if (reader != null && reader.HasRows)
                {
                    while (reader.Read())
                    {
                        lastInsertID = reader.GetInt32(0);
                    }

                    reader.Close();
                    reader.Dispose();
                }
            }

            return lastInsertID;
        }

        public static void LoadScore(ref List<SScores> Score, SPlayer player)
        {
            SQLiteConnection connection = new SQLiteConnection();
            connection.ConnectionString = "Data Source=" + _HighscoreFilePath;
            SQLiteCommand command;

            Score = new List<SScores>();

            try
            {
                connection.Open();
            }
            catch (Exception)
            {
                return;
            }

            command = new SQLiteCommand(connection);

            int Medley = 0;
            if (player.Medley)
                Medley = 1;

            int Duet = 0;
            if (player.Duet)
                Duet = 1;

            int ShortSong = 0;
            if (player.ShortSong)
                ShortSong = 1;

            int DataBaseSongID = GetDataBaseSongID(player, command);
            if (DataBaseSongID >= 0)
            {
                command.CommandText = "SELECT PlayerName, Score, Date, Difficulty, LineNr, id FROM Scores " +
                    "WHERE [SongID] = @SongID AND [Medley] = @Medley AND [Duet] = @Duet AND [ShortSong] = @ShortSong " +
                    "ORDER BY [Score] DESC";
                command.Parameters.Add("@SongID", System.Data.DbType.Int32, 0).Value = DataBaseSongID;
                command.Parameters.Add("@Medley", System.Data.DbType.Int32, 0).Value = Medley;
                command.Parameters.Add("@Duet", System.Data.DbType.Int32, 0).Value = Duet;
                command.Parameters.Add("@ShortSong", System.Data.DbType.Int32, 0).Value = ShortSong;

                SQLiteDataReader reader = null;
                try
                {
                    reader = command.ExecuteReader();
                }
                catch (Exception)
                {
                    throw;
                }

                if (reader != null && reader.HasRows)
                {
                    while (reader.Read())
                    {
                        SScores score = new SScores();
                        score.Name = reader.GetString(0);
                        score.Score = reader.GetInt32(1);
                        score.Date = new DateTime(reader.GetInt64(2)).ToString("dd/MM/yyyy");
                        score.Difficulty = (EGameDifficulty)reader.GetInt32(3);
                        score.LineNr = reader.GetInt32(4);
                        score.ID = reader.GetInt32(5);

                        Score.Add(score);
                    }

                    reader.Close();
                    reader.Dispose();
                }

            }


            command.Dispose();
            connection.Close();
            connection.Dispose();
        }

        private static int GetDataBaseSongID(SPlayer player, SQLiteCommand command)
        {
            CSong song = CSongs.GetSong(player.SongID);

            if (song == null)
                return -1;

            return GetDataBaseSongID(song.Artist, song.Title, 0, command);
        }

        private static int GetDataBaseSongID(string Artist, string Title, string FilePath, int DefNumPlayed)
        {
            SQLiteConnection connection = new SQLiteConnection();
            connection.ConnectionString = "Data Source=" + FilePath;
            SQLiteCommand command;

            try
            {
                connection.Open();
            }
            catch (Exception)
            {
                return -1;
            }

            command = new SQLiteCommand(connection);
            int id = GetDataBaseSongID(Artist, Title, DefNumPlayed, command);
            command.Dispose();
            connection.Close();
            connection.Dispose();

            return id;
        }

        private static int GetDataBaseSongID(string Artist, string Title, int DefNumPlayed, SQLiteCommand command)
        {
            command.CommandText = "SELECT id FROM Songs WHERE [Title] = @title AND [Artist] = @artist";
            command.Parameters.Add("@title", System.Data.DbType.String, 0).Value = Title;
            command.Parameters.Add("@artist", System.Data.DbType.String, 0).Value = Artist;

            SQLiteDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception)
            {
                throw;
            }

            if (reader != null && reader.HasRows)
            {
                reader.Read();
                int id = reader.GetInt32(0);
                reader.Close();
                reader.Dispose();
                return id;
            }
            else
            {
                if (reader != null)
                    reader.Close();

                command.CommandText = "INSERT INTO Songs (Title, Artist, NumPlayed) " +
                    "VALUES (@title, @artist, @numplayed)";
                command.Parameters.Add("@title", System.Data.DbType.String, 0).Value = Title;
                command.Parameters.Add("@artist", System.Data.DbType.String, 0).Value = Artist;
                command.Parameters.Add("@numplayed", System.Data.DbType.Int32, 0).Value = DefNumPlayed;
                command.ExecuteNonQuery();

                command.CommandText = "SELECT id FROM Songs WHERE [Title] = @title AND [Artist] = @artist";
                command.Parameters.Add("@title", System.Data.DbType.String, 0).Value = Title;
                command.Parameters.Add("@artist", System.Data.DbType.String, 0).Value = Artist;

                reader = null;
                try
                {
                    reader = command.ExecuteReader();
                }
                catch (Exception)
                {
                    throw;
                }

                if (reader != null)
                {
                    reader.Read();
                    int id = reader.GetInt32(0);
                    reader.Close();
                    reader.Dispose();
                    return id;
                }
            }

            if (reader != null)
            {
                reader.Close();
                reader.Dispose();
            }

            return -1;
        }

        private static bool GetDataBaseSongInfos(int SongID, out string Artist, out string Title, out int NumPlayed, string FilePath)
        {
            Artist = String.Empty;
            Title = String.Empty;
            NumPlayed = 0;

            SQLiteConnection connection = new SQLiteConnection();
            connection.ConnectionString = "Data Source=" + FilePath;
            SQLiteCommand command;

            try
            {
                connection.Open();
            }
            catch (Exception)
            {
                return false;
            }

            command = new SQLiteCommand(connection);
            command.CommandText = "SELECT Artist, Title, NumPlayed FROM Songs WHERE [id] = @id";
            command.Parameters.Add("@id", System.Data.DbType.String, 0).Value = SongID;

            SQLiteDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception)
            {
                return false;
            }

            if (reader != null && reader.HasRows)
            {
                reader.Read();
                Artist = reader.GetString(0);
                Title = reader.GetString(1);
                NumPlayed = reader.GetInt32(2);
                reader.Close();
                reader.Dispose();

                command.Dispose();
                connection.Close();
                connection.Dispose();
                return true;
            }
            else
            {
                if (reader != null)
                    reader.Close();
            }

            command.Dispose();
            connection.Close();
            connection.Dispose();

            return false;
        }

        private static void InitHighscoreDB()
        {
            string OldDBFilePath = Path.Combine(Environment.CurrentDirectory, CSettings.sFileOldHighscoreDB);
            if (File.Exists(OldDBFilePath))
            {
                if (File.Exists(_HighscoreFilePath))
                {
                    CreateOrConvert(OldDBFilePath);
                    CreateOrConvert(_HighscoreFilePath);
                    ImportData(OldDBFilePath, _HighscoreFilePath);

                    File.Delete(OldDBFilePath);
                }
                else
                {
                    File.Copy(OldDBFilePath, _HighscoreFilePath);
                    CreateOrConvert(_HighscoreFilePath);
                    File.Delete(OldDBFilePath);
                }
            }
            else
                CreateOrConvert(_HighscoreFilePath);
        }

        private static void CreateHighscoreDB(string FilePath)
        {
            SQLiteConnection connection = new SQLiteConnection();
            connection.ConnectionString = "Data Source=" + FilePath;
            SQLiteCommand command;

            try
            {
                connection.Open();
            }
            catch (Exception)
            {
                return;
            }

            command = new SQLiteCommand(connection);

            command.CommandText = "CREATE TABLE IF NOT EXISTS Version ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Value INTEGER NOT NULL);";
            command.ExecuteNonQuery();

            command.CommandText = "INSERT INTO Version (id, Value) VALUES(NULL, " + CSettings.iDatabaseHighscoreVersion.ToString() + ")";
            command.ExecuteNonQuery();

            command.CommandText = "CREATE TABLE IF NOT EXISTS Songs ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                "Artist TEXT NOT NULL, Title TEXT NOT NULL, NumPlayed INTEGER);";
            command.ExecuteNonQuery();

            command.CommandText = "CREATE TABLE IF NOT EXISTS Scores ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                "SongID INTEGER NOT NULL, PlayerName TEXT NOT NULL, Score INTEGER NOT NULL, LineNr INTEGER NOT NULL, Date BIGINT NOT NULL, " +
                "Medley INTEGER NOT NULL, Duet INTEGER NOT NULL, ShortSong INTEGER NOT NULL, Difficulty INTEGER NOT NULL);";
            command.ExecuteNonQuery();

            command.Dispose();
            connection.Close();
            connection.Dispose();
        }

        private static void CreateHighscoreDBV1(string FilePath)
        {
            SQLiteConnection connection = new SQLiteConnection();
            connection.ConnectionString = "Data Source=" + FilePath;
            SQLiteCommand command;

            try
            {
                connection.Open();
            }
            catch (Exception)
            {
                return;
            }

            command = new SQLiteCommand(connection);

            command.CommandText = "CREATE TABLE IF NOT EXISTS Version ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Value INTEGER NOT NULL);";
            command.ExecuteNonQuery();

            command.CommandText = "INSERT INTO Version (id, Value) VALUES(NULL, 1 )";
            command.ExecuteNonQuery();

            command.CommandText = "CREATE TABLE IF NOT EXISTS Songs ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                "Artist TEXT NOT NULL, Title TEXT NOT NULL, NumPlayed INTEGER);";
            command.ExecuteNonQuery();

            command.CommandText = "CREATE TABLE IF NOT EXISTS Scores ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                "SongID INTEGER NOT NULL, PlayerName TEXT NOT NULL, Score INTEGER NOT NULL, LineNr INTEGER NOT NULL, Date BIGINT NOT NULL, " +
                "Medley INTEGER NOT NULL, Duet INTEGER NOT NULL, Difficulty INTEGER NOT NULL);";
            command.ExecuteNonQuery();

            command.Dispose();
            connection.Close();
            connection.Dispose();
        }

        /// <summary>
        /// Creates a new Vocaluxe Database if no file exists. Converts an existing old Ultrastar Deluxe highscore database into vocaluxe format.
        /// </summary>
        /// <param name="FilePath">Database file path</param>
        /// <returns></returns>
        private static bool CreateOrConvert(string FilePath)
        {
            SQLiteConnection connection = new SQLiteConnection();
            connection.ConnectionString = "Data Source=" + FilePath;
            SQLiteCommand command;

            try
            {
                connection.Open();
            }
            catch (Exception)
            {
                return false;
            }

            SQLiteDataReader reader = null;
            command = new SQLiteCommand(connection);

            command.CommandText = "PRAGMA user_version";
            reader = command.ExecuteReader();
            reader.Read();

            int version = reader.GetInt32(0);

            reader.Close();
            reader.Dispose();

            //Check if old scores table exists
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='US_Scores';";
            reader = command.ExecuteReader();
            reader.Read();
            bool scoresTableExists = reader.HasRows;

            reader.Close();
            reader.Dispose();

            command.CommandText = "SELECT Value FROM Version";
            reader = null;

            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception)
            {
                ;
            }

            if (reader == null)
            {
                // create new database/tables
                if (version == 1) //Check for USDX 1.1 DB
                {
                    CreateHighscoreDBV1(FilePath);
                    ConvertFrom110(FilePath);
                    UpdateDatabase(1, connection);
                }
                else if (version == 0 && scoresTableExists) //Check for USDX 1.01 or CMD Mod DB
                {
                    CreateHighscoreDBV1(FilePath);
                    ConvertFrom101(FilePath);
                    UpdateDatabase(1, connection);
                }
                else
                    CreateHighscoreDB(FilePath);
            }
            else if (reader.FieldCount == 0)
            {
                // create new database/tables
                if (version == 1) //Check for USDX 1.1 DB
                {
                    CreateHighscoreDBV1(FilePath);
                    ConvertFrom110(FilePath);
                    UpdateDatabase(1, connection);
                }
                else if (version == 0 && scoresTableExists) //Check for USDX 1.01 or CMD Mod DB
                {
                    CreateHighscoreDBV1(FilePath);
                    ConvertFrom101(FilePath);
                    UpdateDatabase(1, connection);
                }
                else
                    CreateHighscoreDB(FilePath);
            }
            else
            {
                reader.Read();
                int CurrentVersion = reader.GetInt32(0);
                if (CurrentVersion < CSettings.iDatabaseHighscoreVersion)
                {
                    // update database
                    UpdateDatabase(CurrentVersion, connection);
                }
            }

            if (reader != null)
            {
                reader.Close();
                reader.Dispose();
            }

            command.Dispose();

            connection.Close();
            connection.Dispose();

            return true;
        }

        /// <summary>
        /// Converts a USDX 1.1 database into the Vocaluxe format
        /// </summary>
        /// <param name="FilePath">Database file path</param>
        /// <returns>True if succeeded</returns>
        private static bool ConvertFrom110(string FilePath)
        {
            SQLiteConnection connection = new SQLiteConnection();
            connection.ConnectionString = "Data Source=" + FilePath;
            SQLiteCommand command;

            try
            {
                connection.Open();
            }
            catch (Exception)
            {
                return false;
            }

            command = new SQLiteCommand(connection);

            //The USDX database has no column for LineNr, Medley and Duet so just fill 0 in there
            command.CommandText = "INSERT INTO Scores (SongID, PlayerName, Score, LineNr, Date, Medley, Duet, Difficulty) SELECT SongID, Player, Score, '0', Date, '0', '0', Difficulty from US_Scores";
            command.ExecuteNonQuery();

            command.CommandText = "INSERT INTO Songs SELECT ID, Artist, Title, TimesPlayed from US_Songs";
            command.ExecuteNonQuery();

            List<SData> scores = new List<SData>();
            List<SData> songs = new List<SData>();

            SQLiteDataReader reader = null;
            command.CommandText = "SELECT id, PlayerName, Date FROM Scores";
            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception)
            {
                throw;
            }

            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    SData data = new SData();
                    data.id = reader.GetInt32(0);
                    data.str1 = reader.GetString(1);
                    Int64 ticks = 0;

                    try
                    {
                        ticks = reader.GetInt64(2);
                    }
                    catch { }

                    data.ticks = UnixTimeToTicks((int)ticks);

                    scores.Add(data);
                }
                reader.Close();
            }

            command.CommandText = "SELECT id, Artist, Title FROM Songs";
            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception)
            {
                throw;
            }

            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    SData data = new SData();
                    data.id = reader.GetInt32(0);
                    data.str1 = reader.GetString(1);
                    data.str2 = reader.GetString(2);
                    songs.Add(data);
                }
                reader.Close();
            }

            reader.Dispose();

            SQLiteTransaction _Transaction = connection.BeginTransaction();
            // update Title and Artist strings
            foreach (SData data in songs)
            {
                command.CommandText = "UPDATE Songs SET [Artist] = @artist, [Title] = @title WHERE [ID] = @id";
                command.Parameters.Add("@title", System.Data.DbType.String, 0).Value = data.str2;
                command.Parameters.Add("@artist", System.Data.DbType.String, 0).Value = data.str1;
                command.Parameters.Add("@id", System.Data.DbType.Int32, 0).Value = data.id;
                command.ExecuteNonQuery();
            }

            // update player names
            foreach (SData data in scores)
            {
                command.CommandText = "UPDATE Scores SET [PlayerName] = @player, [Date] = @date WHERE [id] = @id";
                command.Parameters.Add("@player", System.Data.DbType.String, 0).Value = data.str1;
                command.Parameters.Add("@date", System.Data.DbType.Int64, 0).Value = data.ticks;
                command.Parameters.Add("@id", System.Data.DbType.Int32, 0).Value = data.id;
                command.ExecuteNonQuery();
            }
            _Transaction.Commit();

            //Delete old tables after conversion
            command.CommandText = "DROP TABLE US_Scores;";
            command.ExecuteNonQuery();

            command.CommandText = "DROP TABLE US_Songs;";
            command.ExecuteNonQuery();

            command.CommandText = "DROP TABLE us_statistics_info;";
            command.ExecuteNonQuery();

            //This versioning is not used in Vocaluxe so reset it to 0
            command.CommandText = "PRAGMA user_version = 0";
            command.ExecuteNonQuery();

            command.Dispose();
            connection.Close();
            connection.Dispose();

            return true;
        }

        /// <summary>
        /// Converts a USDX 1.01 or CMD 1.01 database to Vocaluxe format
        /// </summary>
        /// <param name="FilePath">Database file path</param>
        /// <returns>True if succeeded</returns>
        private static bool ConvertFrom101(string FilePath)
        {
            SQLiteConnection connection = new SQLiteConnection();
            connection.ConnectionString = "Data Source=" + FilePath;
            SQLiteCommand command;
            SQLiteDataReader reader = null;

            try
            {
                connection.Open();
            }
            catch (Exception)
            {
                return false;
            }

            command = new SQLiteCommand(connection);

            command.CommandText = "PRAGMA table_info(US_Scores);";
            reader = command.ExecuteReader();


            bool dateExists = false;

            //Check for column Date
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (reader.GetName(i) == "name")
                    {
                        if (reader.GetString(i) == "Date")
                            dateExists = true;
                        break;
                    }
                }
            }


            reader.Close();

            //This is a USDX 1.01 DB
            if (!dateExists)
                command.CommandText = "INSERT INTO Scores (SongID, PlayerName, Score, LineNr, Date, Medley, Duet, Difficulty) SELECT SongID, Player, Score, '0', '0', '0', '0', Difficulty from US_Scores";
            else // This is a CMD 1.01 DB
                command.CommandText = "INSERT INTO Scores (SongID, PlayerName, Score, LineNr, Date, Medley, Duet, Difficulty) SELECT SongID, Player, Score, '0', Date, '0', '0', Difficulty from US_Scores";
            command.ExecuteNonQuery();

            command.CommandText = "INSERT INTO Songs SELECT ID, Artist, Title, TimesPlayed from US_Songs";
            command.ExecuteNonQuery();

            // convert from CP1252 to UTF8
            List<SData> scores = new List<SData>();
            List<SData> songs = new List<SData>();

            Sqlite3.sqlite3 OldDB;
            int res = Sqlite3.sqlite3_open(FilePath, out OldDB);

            if (res != Sqlite3.SQLITE_OK)
            {
                CLog.LogError("Error opening Database: " + FilePath + " (" + Sqlite3.sqlite3_errmsg(OldDB) + ")");
            }
            else
            {
                Sqlite3.Vdbe Stmt = new Sqlite3.Vdbe();
                res = Sqlite3.sqlite3_prepare_v2(OldDB, "SELECT id, Artist, Title FROM Songs", -1, ref Stmt, 0);

                if (res != Sqlite3.SQLITE_OK)
                {
                    CLog.LogError("Error query Database: " + FilePath + " (" + Sqlite3.sqlite3_errmsg(OldDB) + ")");
                }
                else
                {
                    //Sqlite3.sqlite3_step(Stmt);

                    Encoding UTF8 = Encoding.UTF8;
                    Encoding CP1252 = Encoding.GetEncoding(1252);

                    while (Sqlite3.sqlite3_step(Stmt) == Sqlite3.SQLITE_ROW)
                    {
                        SData data = new SData();

                        data.id = Sqlite3.sqlite3_column_int(Stmt, 0);

                        byte[] bytes = Sqlite3.sqlite3_column_rawbytes(Stmt, 1);
                        if (bytes != null)
                            data.str1 = UTF8.GetString(Encoding.Convert(CP1252, UTF8, bytes));
                        else
                            data.str1 = "Someone";

                        bytes = Sqlite3.sqlite3_column_rawbytes(Stmt, 2);
                        if (bytes != null)
                            data.str2 = UTF8.GetString(Encoding.Convert(CP1252, UTF8, bytes));
                        else
                            data.str2 = "Someone";

                        songs.Add(data);
                    }
                    Sqlite3.sqlite3_finalize(Stmt);
                }

                Stmt = new Sqlite3.Vdbe();

                if (!dateExists)
                    res = Sqlite3.sqlite3_prepare_v2(OldDB, "SELECT id, PlayerName FROM Scores", -1, ref Stmt, 0);
                else
                    res = Sqlite3.sqlite3_prepare_v2(OldDB, "SELECT id, PlayerName, Date FROM Scores", -1, ref Stmt, 0);

                if (res != Sqlite3.SQLITE_OK)
                {
                    CLog.LogError("Error query Database: " + FilePath + " (" + Sqlite3.sqlite3_errmsg(OldDB) + ")");
                }
                else
                {
                    //Sqlite3.sqlite3_step(Stmt);

                    Encoding UTF8 = Encoding.UTF8;
                    Encoding CP1252 = Encoding.GetEncoding(1252);

                    while (Sqlite3.sqlite3_step(Stmt) == Sqlite3.SQLITE_ROW)
                    {
                        SData data = new SData();

                        data.id = Sqlite3.sqlite3_column_int(Stmt, 0);

                        byte[] bytes = Sqlite3.sqlite3_column_rawbytes(Stmt, 1);
                        if (bytes != null)
                            data.str1 = UTF8.GetString(Encoding.Convert(CP1252, UTF8, bytes));
                        else
                            data.str1 = "Someone";

                        if (dateExists)
                            data.ticks = UnixTimeToTicks(Sqlite3.sqlite3_column_int(Stmt, 2));

                        scores.Add(data);
                    }
                    Sqlite3.sqlite3_finalize(Stmt);
                }
            }
            Sqlite3.sqlite3_close(OldDB);

            SQLiteTransaction _Transaction = connection.BeginTransaction();      
             
            // update Title and Artist strings
            foreach (SData data in songs)
            {
                command.CommandText = "UPDATE Songs SET [Artist] = @artist, [Title] = @title WHERE [ID] = @id";
                command.Parameters.Add("@title", System.Data.DbType.String, 0).Value = data.str2;
                command.Parameters.Add("@artist", System.Data.DbType.String, 0).Value = data.str1;
                command.Parameters.Add("@id", System.Data.DbType.Int32, 0).Value = data.id;
                command.ExecuteNonQuery();
            }           

            // update player names
            foreach (SData data in scores)
            {
                if (!dateExists)
                    command.CommandText = "UPDATE Scores SET [PlayerName] = @player WHERE [id] = @id";
                else
                {
                    command.CommandText = "UPDATE Scores SET [PlayerName] = @player, [Date] = @date WHERE [id] = @id";
                    command.Parameters.Add("@date", System.Data.DbType.Int64, 0).Value = data.ticks;
                }
                command.Parameters.Add("@player", System.Data.DbType.String, 0).Value = data.str1;
                command.Parameters.Add("@id", System.Data.DbType.Int32, 0).Value = data.id;
                command.ExecuteNonQuery();
            }
            _Transaction.Commit();

            //Delete old tables after conversion
            command.CommandText = "DROP TABLE US_Scores;";
            command.ExecuteNonQuery();

            command.CommandText = "DROP TABLE US_Songs;";
            command.ExecuteNonQuery();

            reader.Dispose();
            command.Dispose();
            connection.Close();
            connection.Dispose();

            return true;
        }

        private static bool UpdateDatabase(int CurrentVersion, SQLiteConnection connection)
        {
            bool updated = true;
            if (CurrentVersion < 2)
            {
                updated &= ConvertV1toV2(connection);
            }
            return updated;
        }

        private static bool ConvertV1toV2(SQLiteConnection connection)
        {
            SQLiteCommand command;

            command = new SQLiteCommand(connection);

            command.CommandText = "ALTER TABLE Scores ADD ShortSong INTEGER";
            command.ExecuteNonQuery();
            command.CommandText = "UPDATE Scores SET [ShortSong] = @ShortSong";
            command.Parameters.Add("@ShortSong", System.Data.DbType.Int32, 0).Value = 0;
            command.ExecuteNonQuery();
            command.CommandText = "UPDATE Version SET [Value] = @version";
            command.Parameters.Add("@version", System.Data.DbType.Int32, 0).Value = 2;
            command.ExecuteNonQuery();
            command.Dispose();

            return true;
        }

        private static bool ImportData(string SourceDBPath, string DestinationDBPath)
        {
            #region open db
            SQLiteConnection connSource = new SQLiteConnection();
            connSource.ConnectionString = "Data Source=" + SourceDBPath;

            try
            {
                connSource.Open();
            }
            catch (Exception e)
            {
                CLog.LogError("Error on import high score data. Can't open source database \"" + SourceDBPath + "\" (" + e.Message + ")");
                return false;
            }
            #endregion open db

            SQLiteCommand cmdSource = new SQLiteCommand(connSource);
            SQLiteDataReader rSource;

            #region import table scores
            cmdSource.CommandText = "SELECT SongID, PlayerName, Score, LineNr, Date, Medley, Duet, ShortSong, Difficulty FROM Scores";
            rSource = cmdSource.ExecuteReader();
            if (rSource == null)
            {
                cmdSource.Dispose();
                connSource.Close();
                return false;
            }

            if (rSource.FieldCount == 0)
            {
                rSource.Close();
                cmdSource.Dispose();
                connSource.Close();
                return true;
            }

            while (rSource.Read())
            {
                int songid = rSource.GetInt32(0);
                string player = rSource.GetString(1);
                int score = rSource.GetInt32(2);
                int linenr = rSource.GetInt32(3);
                long date = rSource.GetInt64(4);
                int medley = rSource.GetInt32(5);
                int duet = rSource.GetInt32(6);
                int shortsong = rSource.GetInt32(7);
                int diff = rSource.GetInt32(8);

                string artist, title;
                int numplayed;
                if (GetDataBaseSongInfos(songid, out artist, out title, out numplayed, SourceDBPath))
                {
                    AddScore(player, score, linenr, date, medley, duet, shortsong, diff, artist, title, numplayed, _HighscoreFilePath);
                }
            }
            #endregion import table scores

            rSource.Close();
            cmdSource.Dispose();
            connSource.Close();

            return true;
        }
        #endregion Highscores

        #region Cover
        public static bool GetCover(string CoverPath, ref STexture tex, int MaxSize)
        {
            bool result = false;

            if (!File.Exists(CoverPath))
            {
                CLog.LogError("Can't find File: " + CoverPath);
                return false;
            }

            if (_ConnectionCover == null)
            {
                _ConnectionCover = new SQLiteConnection();
                _ConnectionCover.ConnectionString = "Data Source=" + _CoverFilePath;
                _ConnectionCover.Open();
            }

            SQLiteCommand command;
            command = new SQLiteCommand(_ConnectionCover);

            command.CommandText = "SELECT id, width, height FROM Cover WHERE [Path] = @path";
            command.Parameters.Add("@path", System.Data.DbType.String, 0).Value = CoverPath;

            SQLiteDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception)
            {
                throw;
            }

            if (reader != null && reader.HasRows)
            {
                reader.Read();
                int id = reader.GetInt32(0);
                int w = reader.GetInt32(1);
                int h = reader.GetInt32(2);
                reader.Close();

                command.CommandText = "SELECT Data FROM CoverData WHERE CoverID = " + id.ToString();
                try
                {
                    reader = command.ExecuteReader();
                }
                catch (Exception)
                {
                    throw;
                }

                if (reader.HasRows)
                {
                    result = true;
                    reader.Read();
                    byte[] data = GetBytes(reader);
                    tex = CDraw.QuequeTexture(w, h, ref data);
                }
            }
            else
            {
                if (reader != null)
                    reader.Close();

                if (_TransactionCover == null)
                {
                    _TransactionCover = _ConnectionCover.BeginTransaction();
                }

                Bitmap origin;
                try
                {
                    origin = new Bitmap(CoverPath);
                }
                catch (Exception)
                {
                    CLog.LogError("Error loading Texture: " + CoverPath);
                    tex = new STexture(-1);

                    if (reader != null)
                    {
                        reader.Close();
                        reader.Dispose();
                    }
                    command.Dispose();

                    return false;
                }

                int w = MaxSize;
                int h = MaxSize;

                if (origin.Width >= origin.Height && origin.Width > w)
                    h = (int)Math.Round((float)w / origin.Width * origin.Height);
                else if (origin.Height > origin.Width && origin.Height > h)
                    w = (int)Math.Round((float)h / origin.Height * origin.Width);

                Bitmap bmp = new Bitmap(w, h);
                Graphics g = Graphics.FromImage(bmp);
                g.DrawImage(origin, new Rectangle(0, 0, w, h));
                g.Dispose();
                if (origin != null)
                    origin.Dispose();

                byte[] data = new byte[w * h * 4];

                BitmapData bmp_data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Marshal.Copy(bmp_data.Scan0, data, 0, w * h * 4);
                bmp.UnlockBits(bmp_data);
                bmp.Dispose();

                tex = CDraw.QuequeTexture(w, h, ref data);
                
                command.CommandText = "INSERT INTO Cover (Path, width, height) " +
                    "VALUES (@path, " + w.ToString() + ", " + h.ToString() + ")";
                command.Parameters.Add("@path", System.Data.DbType.String, 0).Value = CoverPath;
                command.ExecuteNonQuery();

                command.CommandText = "SELECT id FROM Cover WHERE [Path] = @path";
                command.Parameters.Add("@path", System.Data.DbType.String, 0).Value = CoverPath;
                reader = null;
                try
                {
                    reader = command.ExecuteReader();
                }
                catch (Exception)
                {
                    throw;
                }

                if (reader != null)
                {
                    reader.Read();
                    int id = reader.GetInt32(0);
                    reader.Close();
                    command.CommandText = "INSERT INTO CoverData (CoverID, Data) " +
                    "VALUES ('" + id.ToString() + "', @data)";
                    command.Parameters.Add("@data", System.Data.DbType.Binary, 20).Value = data;
                    command.ExecuteReader();
                    result = true;
                }
            }

            if (reader != null)
            {
                reader.Close();
                reader.Dispose();
            }
            command.Dispose();

            return result;
        }

        public static void CommitCovers()
        {
            if (_TransactionCover != null)
            {
                _TransactionCover.Commit();
                _TransactionCover = null;
                GC.Collect();
            }
        }

        public static void CloseConnections()
        {
            CommitCovers();

            if (_ConnectionCover != null)
            {
                _ConnectionCover.Close();
                _ConnectionCover = null;
            }
        }

        private static bool InitCoverDB()
        {
            SQLiteConnection connection = new SQLiteConnection();
            connection.ConnectionString = "Data Source=" + _CoverFilePath;
            SQLiteCommand command;

            try
            {
                connection.Open();
            }
            catch (Exception)
            {
                return false;
            }

            command = new SQLiteCommand(connection);
            command.CommandText = "SELECT Value FROM Version";

            SQLiteDataReader reader = null;

            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception)
            {
                ;
            }

            if (reader == null)
            {
                // create new database/tables
                CreateCoverDB();
            }
            else if (reader.FieldCount == 0)
            {
                // create new database/tables
                CreateCoverDB();
            }
            else
            {
                reader.Read();

                if (reader.GetInt32(0) < CSettings.iDatabaseHighscoreVersion)
                {
                    // update database
                }
            }

            if (reader != null)
            {
                reader.Close();
                reader.Dispose();
            }

            command.Dispose();

            connection.Close();
            connection.Dispose();

            return true;
        }

        private static void CreateCoverDB()
        {
            SQLiteConnection connection = new SQLiteConnection();
            connection.ConnectionString = "Data Source=" + _CoverFilePath;
            SQLiteCommand command;

            try
            {
                connection.Open();
            }
            catch (Exception)
            {
                return;
            }

            command = new SQLiteCommand(connection);

            command.CommandText = "CREATE TABLE IF NOT EXISTS Version ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Value INTEGER NOT NULL);";
            command.ExecuteNonQuery();

            command.CommandText = "INSERT INTO Version (id, Value) VALUES(NULL, " + CSettings.iDatabaseCoverVersion.ToString() + ")";
            command.ExecuteNonQuery();

            command.CommandText = "CREATE TABLE IF NOT EXISTS Cover ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                "Path TEXT NOT NULL, width INTEGER NOT NULL, height INTEGER NOT NULL);";
            command.ExecuteNonQuery();

            command.CommandText = "CREATE TABLE IF NOT EXISTS CoverData ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                "CoverID INTEGER NOT NULL, Data BLOB NOT NULL);";
            command.ExecuteNonQuery();

            command.Dispose();
            connection.Close();
            connection.Dispose();
        }
        #endregion Cover

        #region CreditsRessources
        public static bool GetCreditsRessource(string FileName, ref STexture tex)
        {
            bool result = false;

            SQLiteConnection connection = new SQLiteConnection();
            connection.ConnectionString = "Data Source=" + _CreditsRessourcesFilePath;
            SQLiteCommand command;
            try
            {
                connection.Open();
            }
            catch (Exception)
            {
                return false;
            }
            command = new SQLiteCommand(connection);

            command.CommandText = "SELECT id, width, height FROM Images WHERE [Path] = @path";
            command.Parameters.Add("@path", System.Data.DbType.String, 0).Value = FileName;

            SQLiteDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception)
            {
                throw;
            }

            if (reader != null && reader.HasRows)
            {
                reader.Read();
                int id = reader.GetInt32(0);
                int w = reader.GetInt32(1);
                int h = reader.GetInt32(2);
                reader.Close();

                command.CommandText = "SELECT Data FROM ImageData WHERE ImageID = " + id.ToString();
                try
                {
                    reader = command.ExecuteReader();
                }
                catch (Exception)
                {
                    throw;
                }

                if (reader.HasRows)
                {
                    result = true;
                    reader.Read();
                    byte[] data = GetBytes(reader);
                    tex = CDraw.AddTexture(w, h, ref data);
                }
            }

            if (reader != null)
            {
                reader.Close();
                reader.Dispose();
            }
            command.Dispose();
            connection.Close();
            connection.Dispose();

            return result;
        }

        //If you want to add an image to db, call this method!
        private static bool AddImageToCreditsDB(String ImagePath)
        {
            bool result = false;
            STexture tex;

            if (File.Exists(ImagePath))
            {

                SQLiteConnection connection = new SQLiteConnection();
                connection.ConnectionString = "Data Source=" + _CreditsRessourcesFilePath;
                SQLiteCommand command;
                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return false;
                }
                command = new SQLiteCommand(connection);

                SQLiteDataReader reader = null;

                if (reader != null)
                    reader.Close();

                Bitmap origin;
                try
                {
                    origin = new Bitmap(ImagePath);
                }
                catch (Exception)
                {
                    CLog.LogError("Error loading Texture: " + ImagePath);
                    tex = new STexture(-1);

                    if (reader != null)
                    {
                        reader.Close();
                        reader.Dispose();
                    }
                    command.Dispose();
                    connection.Close();
                    connection.Dispose();

                    return false;
                }

                int w = origin.Width;
                int h = origin.Height;

                Bitmap bmp = new Bitmap(w, h);
                Graphics g = Graphics.FromImage(bmp);
                g.DrawImage(origin, new Rectangle(0, 0, w, h));
                g.Dispose();
                if (origin != null)
                    origin.Dispose();
                tex = CDraw.AddTexture(bmp);
                byte[] data = new byte[w * h * 4];

                BitmapData bmp_data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Marshal.Copy(bmp_data.Scan0, data, 0, w * h * 4);
                bmp.UnlockBits(bmp_data);
                bmp.Dispose();

                command.CommandText = "INSERT INTO Images (Path, width, height) " +
                    "VALUES (@path, " + w.ToString() + ", " + h.ToString() + ")";
                command.Parameters.Add("@path", System.Data.DbType.String, 0).Value = Path.GetFileName(ImagePath);
                command.ExecuteNonQuery();

                command.CommandText = "SELECT id FROM Images WHERE [Path] = @path";
                command.Parameters.Add("@path", System.Data.DbType.String, 0).Value = Path.GetFileName(ImagePath);
                reader = null;
                try
                {
                    reader = command.ExecuteReader();
                }
                catch (Exception)
                {
                    throw;
                }

                if (reader != null)
                {
                    reader.Read();
                    int id = reader.GetInt32(0);
                    reader.Close();
                    command.CommandText = "INSERT INTO ImageData (ImageID, Data) " +
                    "VALUES ('" + id.ToString() + "', @data)";
                    command.Parameters.Add("@data", System.Data.DbType.Binary, 20).Value = data;
                    command.ExecuteReader();
                    result = true;
                }
            }

            return result;
        }

        private static bool InitCreditsRessourcesDB()
        {
            SQLiteConnection connection = new SQLiteConnection();
            connection.ConnectionString = "Data Source=" + _CreditsRessourcesFilePath;
            SQLiteCommand command;

            try
            {
                connection.Open();
            }
            catch (Exception)
            {
                return false;
            }

            command = new SQLiteCommand(connection);
            command.CommandText = "SELECT Value FROM Version";

            SQLiteDataReader reader = null;

            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception)
            {
                ;
            }

            if (reader == null)
            {
                // Log error
                CLog.LogError("Can't find Credits-DB!");
            }
            else if (reader.FieldCount == 0)
            {
                // Log error
                CLog.LogError("Can't find Credits-DB! Field-Count = 0");
            }
            else
            {
                reader.Read();

                if (reader.GetInt32(0) < CSettings.iDatabaseHighscoreVersion)
                {
                    // update database
                }
            }

            if (reader != null)
            {
                reader.Close();
                reader.Dispose();
            }

            command.Dispose();

            connection.Close();
            connection.Dispose();

            return true;
        }

        private static void CreateCreditsRessourcesDB()
        {
            SQLiteConnection connection = new SQLiteConnection();
            connection.ConnectionString = "Data Source=" + _CreditsRessourcesFilePath;
            SQLiteCommand command;

            try
            {
                connection.Open();
            }
            catch (Exception)
            {
                return;
            }

            command = new SQLiteCommand(connection);

            command.CommandText = "CREATE TABLE IF NOT EXISTS Version ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Value INTEGER NOT NULL);";
            command.ExecuteNonQuery();

            command.CommandText = "INSERT INTO Version (id, Value) VALUES(NULL, " + CSettings.iDatabaseCreditsRessourcesVersion.ToString() + ")";
            command.ExecuteNonQuery();

            command.CommandText = "CREATE TABLE IF NOT EXISTS Images ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                "Path TEXT NOT NULL, width INTEGER NOT NULL, height INTEGER NOT NULL);";
            command.ExecuteNonQuery();

            command.CommandText = "CREATE TABLE IF NOT EXISTS ImageData ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                "ImageID INTEGER NOT NULL, Data BLOB NOT NULL);";
            command.ExecuteNonQuery();

            command.Dispose();
            connection.Close();
            connection.Dispose();
        }
        #endregion CreditsRessources

        private static byte[] GetBytes(SQLiteDataReader reader)
        {
            const int CHUNK_SIZE = 2 * 1024;
            byte[] buffer = new byte[CHUNK_SIZE];
            long bytesRead;
            long fieldOffset = 0;
            using (MemoryStream stream = new MemoryStream())
            {
                while ((bytesRead = reader.GetBytes(0, fieldOffset, buffer, 0, buffer.Length)) > 0)
                {
                    byte[] actualRead = new byte[bytesRead];
                    Buffer.BlockCopy(buffer, 0, actualRead, 0, (int)bytesRead);
                    stream.Write(actualRead, 0, actualRead.Length);
                    fieldOffset += bytesRead;
                }
                return stream.ToArray();
            }
        }

        private static long UnixTimeToTicks(int UnixTime)
        {
            DateTime t70 = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            t70 = t70.AddSeconds(UnixTime);
            return t70.Ticks;
        }
    }
}