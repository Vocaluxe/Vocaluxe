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
using VocaluxeLib;
using VocaluxeLib.Songs;
using VocaluxeLib.Draw;

namespace Vocaluxe.Base
{
    static class CDataBase
    {
        private struct SData
        {
            public int Id;
            public long Ticks;
            public string Str1;
            public string Str2;
        }

        private static string _HighscoreFilePath;
        private static string _CoverFilePath;
        private static string _CreditsRessourcesFilePath;

        private static SQLiteConnection _ConnectionCover;
        private static SQLiteTransaction _TransactionCover;

        public static void Init()
        {
            _HighscoreFilePath = Path.Combine(Environment.CurrentDirectory, CSettings.FileHighscoreDB);
            _CoverFilePath = Path.Combine(Environment.CurrentDirectory, CSettings.FileCoverDB);
            _CreditsRessourcesFilePath = Path.Combine(Environment.CurrentDirectory, CSettings.FileCreditsRessourcesDB);

            _InitHighscoreDB();
            _InitCoverDB();
            _InitCreditsRessourcesDB();
            GC.Collect();
        }

        #region Highscores

        public static bool GetDataBaseSongInfos(string artist, string title, out int numPlayed, out string dateAdded, out int highscoreID)
        {
            string sArtist = string.Empty;
            string sTitle = string.Empty;
            int songID = -1;
            using (SQLiteConnection connection = new SQLiteConnection())
            {
                connection.ConnectionString = "Data Source=" + _HighscoreFilePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                }

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    songID = _GetDataBaseSongID(artist, title, 0, command);
                    highscoreID = songID;
                }
            }
            return _GetDataBaseSongInfos(songID, out sArtist, out sTitle, out numPlayed, out dateAdded, _HighscoreFilePath);
        }

        public static void IncreaseSongCounter(SPlayer player)
        {
            using (SQLiteConnection connection = new SQLiteConnection())
            {
                connection.ConnectionString = "Data Source=" + _HighscoreFilePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                }

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    int dataBaseSongID = _GetDataBaseSongID(player, command);
                    _IncreaseSongCounter(dataBaseSongID, command);
                }
            }
        }

        public static void IncreaseSongCounter(int dataBaseSongID)
        {
            using (SQLiteConnection connection = new SQLiteConnection())
            {
                connection.ConnectionString = "Data Source=" + _HighscoreFilePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                }

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    _IncreaseSongCounter(dataBaseSongID, command);
                }
            }
        }

        public static int AddScore(string playerName, int score, int lineNr, long date, int medley, int duet, int shortSong, int difficulty,
                                   string artist, string title, int numPlayed, string filePath)
        {
            using (SQLiteConnection connection = new SQLiteConnection())
            {
                connection.ConnectionString = "Data Source=" + filePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return -1;
                }

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    int dataBaseSongID = _GetDataBaseSongID(artist, title, numPlayed, command);
                    int result = _AddScore(playerName, score, lineNr, date, medley, duet, shortSong, difficulty, dataBaseSongID, command);
                    return result;
                }
            }
        }

        public static int AddScore(SPlayer player)
        {
            using (SQLiteConnection connection = new SQLiteConnection())
            {
                connection.ConnectionString = "Data Source=" + _HighscoreFilePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return -1;
                }

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    int dataBaseSongID = CSongs.GetSong(player.SongID).DataBaseSongID;
                    return _AddScore(CProfiles.GetPlayerName(player.ProfileID), (int)Math.Round(player.Points), player.LineNr, player.DateTicks, player.Medley ? 1 : 0,
                                     player.Duet ? 1 : 0, player.ShortSong ? 1 : 0, (int)CProfiles.GetDifficulty(player.ProfileID), dataBaseSongID, command);
                }
            }
        }

        private static int _AddScore(string playerName, int score, int lineNr, long date, int medley, int duet, int shortSong, int difficulty,
                                     int dataBaseSongID, SQLiteCommand command)
        {
            int lastInsertID = -1;

            if (dataBaseSongID >= 0)
            {
                command.CommandText = "SELECT id FROM Scores WHERE SongID = @SongID AND PlayerName = @PlayerName AND Score = @Score AND " +
                                      "LineNr = @LineNr AND Date = @Date AND Medley = @Medley AND Duet = @Duet AND ShortSong = @ShortSong AND Difficulty = @Difficulty";
                command.Parameters.Add("@SongID", DbType.Int32, 0).Value = dataBaseSongID;
                command.Parameters.Add("@PlayerName", DbType.String, 0).Value = playerName;
                command.Parameters.Add("@Score", DbType.Int32, 0).Value = score;
                command.Parameters.Add("@LineNr", DbType.Int32, 0).Value = lineNr;
                command.Parameters.Add("@Date", DbType.Int64, 0).Value = date;
                command.Parameters.Add("@Medley", DbType.Int32, 0).Value = medley;
                command.Parameters.Add("@Duet", DbType.Int32, 0).Value = duet;
                command.Parameters.Add("@ShortSong", DbType.Int32, 0).Value = shortSong;
                command.Parameters.Add("@Difficulty", DbType.Int32, 0).Value = difficulty;

                SQLiteDataReader reader = null;
                try
                {
                    reader = command.ExecuteReader();
                }
                catch (Exception) {}

                if (reader != null && reader.HasRows)
                {
                    if (reader.Read())
                        return reader.GetInt32(0);
                }

                if (reader != null)
                    reader.Dispose();

                command.CommandText = "INSERT INTO Scores (SongID, PlayerName, Score, LineNr, Date, Medley, Duet, ShortSong, Difficulty) " +
                                      "VALUES (@SongID, @PlayerName, @Score, @LineNr, @Date, @Medley, @Duet, @ShortSong, @Difficulty)";
                command.Parameters.Add("@SongID", DbType.Int32, 0).Value = dataBaseSongID;
                command.Parameters.Add("@PlayerName", DbType.String, 0).Value = playerName;
                command.Parameters.Add("@Score", DbType.Int32, 0).Value = score;
                command.Parameters.Add("@LineNr", DbType.Int32, 0).Value = lineNr;
                command.Parameters.Add("@Date", DbType.Int64, 0).Value = date;
                command.Parameters.Add("@Medley", DbType.Int32, 0).Value = medley;
                command.Parameters.Add("@Duet", DbType.Int32, 0).Value = duet;
                command.Parameters.Add("@ShortSong", DbType.Int32, 0).Value = shortSong;
                command.Parameters.Add("@Difficulty", DbType.Int32, 0).Value = difficulty;
                command.ExecuteNonQuery();

                //Read last insert line
                command.CommandText = "SELECT id FROM Scores ORDER BY id DESC LIMIT 0, 1";

                reader = command.ExecuteReader();

                if (reader != null && reader.HasRows)
                {
                    while (reader.Read())
                        lastInsertID = reader.GetInt32(0);
                    reader.Dispose();
                }
            }

            return lastInsertID;
        }

        public static void LoadScore(out List<SScores> scores, SPlayer player)
        {
            using (SQLiteConnection connection = new SQLiteConnection())
            {
                connection.ConnectionString = "Data Source=" + _HighscoreFilePath;

                scores = new List<SScores>();

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return;
                }

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    int medley = 0;
                    if (player.Medley)
                        medley = 1;

                    int duet = 0;
                    if (player.Duet)
                        duet = 1;

                    int shortSong = 0;
                    if (player.ShortSong)
                        shortSong = 1;

                    int dataBaseSongID = _GetDataBaseSongID(player, command);
                    if (dataBaseSongID >= 0)
                    {
                        command.CommandText = "SELECT PlayerName, Score, Date, Difficulty, LineNr, id FROM Scores " +
                                              "WHERE [SongID] = @SongID AND [Medley] = @Medley AND [Duet] = @Duet AND [ShortSong] = @ShortSong " +
                                              "ORDER BY [Score] DESC";
                        command.Parameters.Add("@SongID", DbType.Int32, 0).Value = dataBaseSongID;
                        command.Parameters.Add("@Medley", DbType.Int32, 0).Value = medley;
                        command.Parameters.Add("@Duet", DbType.Int32, 0).Value = duet;
                        command.Parameters.Add("@ShortSong", DbType.Int32, 0).Value = shortSong;

                        SQLiteDataReader reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                SScores score = new SScores
                                    {
                                        Name = reader.GetString(0),
                                        Score = reader.GetInt32(1),
                                        Date = new DateTime(reader.GetInt64(2)).ToString("dd/MM/yyyy"),
                                        Difficulty = (EGameDifficulty)reader.GetInt32(3),
                                        LineNr = reader.GetInt32(4),
                                        ID = reader.GetInt32(5)
                                    };

                                scores.Add(score);
                            }
                            reader.Dispose();
                        }
                    }
                }
            }
        }

        private static void _IncreaseSongCounter(int dataBaseSongID, SQLiteCommand command)
        {
            command.CommandText = "UPDATE Songs SET NumPlayed = NumPlayed + 1 WHERE [id] = @id";
            command.Parameters.Add("@id", DbType.Int32, 0).Value = dataBaseSongID;
            command.ExecuteNonQuery();
        }

        private static int _GetDataBaseSongID(SPlayer player, SQLiteCommand command)
        {
            CSong song = CSongs.GetSong(player.SongID);

            if (song == null)
                return -1;

            return _GetDataBaseSongID(song.Artist, song.Title, 0, command);
        }

        private static int _GetDataBaseSongID(string artist, string title, int defNumPlayed, SQLiteCommand command)
        {
            command.CommandText = "SELECT id FROM Songs WHERE [Title] = @title AND [Artist] = @artist";
            command.Parameters.Add("@title", DbType.String, 0).Value = title;
            command.Parameters.Add("@artist", DbType.String, 0).Value = artist;

            SQLiteDataReader reader = command.ExecuteReader();

            if (reader != null && reader.HasRows)
            {
                reader.Read();
                int id = reader.GetInt32(0);
                reader.Dispose();
                return id;
            }

            if (reader != null)
                reader.Close();

            command.CommandText = "INSERT INTO Songs (Title, Artist, NumPlayed, DateAdded) " +
                                  "VALUES (@title, @artist, @numplayed, @dateadded)";
            command.Parameters.Add("@title", DbType.String, 0).Value = title;
            command.Parameters.Add("@artist", DbType.String, 0).Value = artist;
            command.Parameters.Add("@numplayed", DbType.Int32, 0).Value = defNumPlayed;
            command.Parameters.Add("@dateadded", System.Data.DbType.Int64, 0).Value = DateTime.Now.Ticks;
            command.ExecuteNonQuery();

            command.CommandText = "SELECT id FROM Songs WHERE [Title] = @title AND [Artist] = @artist";
            command.Parameters.Add("@title", DbType.String, 0).Value = title;
            command.Parameters.Add("@artist", DbType.String, 0).Value = artist;

            reader = command.ExecuteReader();

            if (reader != null)
            {
                reader.Read();
                int id = reader.GetInt32(0);
                reader.Dispose();
                return id;
            }

            return -1;
        }

        private static bool _GetDataBaseSongInfos(int songID, out string artist, out string title, out int numPlayed, out string dateAdded, string filePath)
        {
            artist = String.Empty;
            title = String.Empty;
            numPlayed = 0;
            dateAdded = String.Empty;

            using (SQLiteConnection connection = new SQLiteConnection())
            {
                connection.ConnectionString = "Data Source=" + filePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return false;
                }

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT Artist, Title, NumPlayed, DateAdded FROM Songs WHERE [id] = @id";
                    command.Parameters.Add("@id", DbType.String, 0).Value = songID;

                    SQLiteDataReader reader;
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
                        artist = reader.GetString(0);
                        title = reader.GetString(1);
                        numPlayed = reader.GetInt32(2);
                        dateAdded = new DateTime(reader.GetInt64(3)).ToString("dd/MM/yyyy");
                        reader.Dispose();
                        return true;
                    }
                    if (reader != null)
                        reader.Dispose();
                }
            }

            return false;
        }

        private static void _InitHighscoreDB()
        {
            string oldDBFilePath = Path.Combine(Environment.CurrentDirectory, CSettings.FileOldHighscoreDB);
            if (File.Exists(oldDBFilePath))
            {
                if (File.Exists(_HighscoreFilePath))
                {
                    _CreateOrConvert(oldDBFilePath);
                    _CreateOrConvert(_HighscoreFilePath);
                    _ImportData(oldDBFilePath);

                    File.Delete(oldDBFilePath);
                }
                else
                {
                    File.Copy(oldDBFilePath, _HighscoreFilePath);
                    _CreateOrConvert(_HighscoreFilePath);
                    File.Delete(oldDBFilePath);
                }
            }
            else
                _CreateOrConvert(_HighscoreFilePath);
        }

        private static void _CreateHighscoreDB(string filePath)
        {
            using (SQLiteConnection connection = new SQLiteConnection())
            {
                connection.ConnectionString = "Data Source=" + filePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return;
                }

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = "CREATE TABLE IF NOT EXISTS Version ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Value INTEGER NOT NULL);";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO Version (id, Value) VALUES(NULL, @Value)";
                    command.Parameters.Add("@Value", DbType.Int32).Value = CSettings.DatabaseHighscoreVersion;
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE TABLE IF NOT EXISTS Songs ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                          "Artist TEXT NOT NULL, Title TEXT NOT NULL, NumPlayed INTEGER, DateAdded BIGING);";
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE TABLE IF NOT EXISTS Scores ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                          "SongID INTEGER NOT NULL, PlayerName TEXT NOT NULL, Score INTEGER NOT NULL, LineNr INTEGER NOT NULL, Date BIGINT NOT NULL, " +
                                          "Medley INTEGER NOT NULL, Duet INTEGER NOT NULL, ShortSong INTEGER NOT NULL, Difficulty INTEGER NOT NULL);";
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void _CreateHighscoreDBV1(string filePath)
        {
            using (SQLiteConnection connection = new SQLiteConnection())
            {
                connection.ConnectionString = "Data Source=" + filePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return;
                }

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
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
                }
            }
        }

        /// <summary>
        ///     Creates a new Vocaluxe Database if no file exists. Converts an existing old Ultrastar Deluxe highscore database into vocaluxe format.
        /// </summary>
        /// <param name="filePath">Database file path</param>
        /// <returns></returns>
        private static bool _CreateOrConvert(string filePath)
        {
            using (SQLiteConnection connection = new SQLiteConnection())
            {
                connection.ConnectionString = "Data Source=" + filePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return false;
                }

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = "PRAGMA user_version";
                    SQLiteDataReader reader = command.ExecuteReader();
                    reader.Read();

                    int version = reader.GetInt32(0);

                    reader.Dispose();

                    //Check if old scores table exists
                    command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='US_Scores';";
                    reader = command.ExecuteReader();
                    reader.Read();
                    bool scoresTableExists = reader.HasRows;

                    reader.Dispose();

                    command.CommandText = "SELECT Value FROM Version";
                    reader = null;

                    try
                    {
                        reader = command.ExecuteReader();
                    }
                    catch (Exception) {}

                    if (reader == null)
                    {
                        // create new database/tables
                        if (version == 1)
                        {
                            //Check for USDX 1.1 DB
                            _CreateHighscoreDBV1(filePath);
                            _ConvertFrom110(filePath);
                            _UpdateDatabase(1, connection);
                            _UpdateDatabase(2, connection);
                        }
                        else if (version == 0 && scoresTableExists)
                        {
                            //Check for USDX 1.01 or CMD Mod DB
                            _CreateHighscoreDBV1(filePath);
                            _ConvertFrom101(filePath);
                            _UpdateDatabase(1, connection);
                            _UpdateDatabase(2, connection);
                        }
                        else
                            _CreateHighscoreDB(filePath);
                    }
                    else if (reader.FieldCount == 0)
                    {
                        // create new database/tables
                        if (version == 1)
                        {
                            //Check for USDX 1.1 DB
                            _CreateHighscoreDBV1(filePath);
                            _ConvertFrom110(filePath);
                            _UpdateDatabase(1, connection);
                            _UpdateDatabase(2, connection);
                        }
                        else if (version == 0 && scoresTableExists)
                        {
                            //Check for USDX 1.01 or CMD Mod DB
                            _CreateHighscoreDBV1(filePath);
                            _ConvertFrom101(filePath);
                            _UpdateDatabase(1, connection);
                            _UpdateDatabase(2, connection);
                        }
                        else
                            _CreateHighscoreDB(filePath);
                    }
                    else
                    {
                        reader.Read();
                        int currentVersion = reader.GetInt32(0);
                        if (currentVersion < CSettings.DatabaseHighscoreVersion)
                        {
                            // update database
                            _UpdateDatabase(currentVersion, connection);
                        }
                    }

                    if (reader != null)
                        reader.Dispose();
                }
            }

            return true;
        }

        /// <summary>
        ///     Converts a USDX 1.1 database into the Vocaluxe format
        /// </summary>
        /// <param name="filePath">Database file path</param>
        /// <returns>True if succeeded</returns>
        private static bool _ConvertFrom110(string filePath)
        {
            using (SQLiteConnection connection = new SQLiteConnection())
            {
                connection.ConnectionString = "Data Source=" + filePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return false;
                }

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    //The USDX database has no column for LineNr, Medley and Duet so just fill 0 in there
                    command.CommandText =
                        "INSERT INTO Scores (SongID, PlayerName, Score, LineNr, Date, Medley, Duet, Difficulty) SELECT SongID, Player, Score, '0', Date, '0', '0', Difficulty from US_Scores";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO Songs SELECT ID, Artist, Title, TimesPlayed from US_Songs";
                    command.ExecuteNonQuery();

                    List<SData> scores = new List<SData>();
                    List<SData> songs = new List<SData>();

                    command.CommandText = "SELECT id, PlayerName, Date FROM Scores";
                    SQLiteDataReader reader = command.ExecuteReader();

                    if (reader != null && reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            SData data = new SData {Id = reader.GetInt32(0), Str1 = reader.GetString(1)};
                            Int64 ticks = 0;

                            try
                            {
                                ticks = reader.GetInt64(2);
                            }
                            catch {}

                            data.Ticks = _UnixTimeToTicks((int)ticks);

                            scores.Add(data);
                        }
                        reader.Close();
                    }

                    command.CommandText = "SELECT id, Artist, Title FROM Songs";

                    reader = command.ExecuteReader();

                    if (reader != null && reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            SData data = new SData {Id = reader.GetInt32(0), Str1 = reader.GetString(1), Str2 = reader.GetString(2)};
                            songs.Add(data);
                        }
                    }

                    if (reader != null)
                        reader.Dispose();

                    SQLiteTransaction transaction = connection.BeginTransaction();
                    // update Title and Artist strings
                    foreach (SData data in songs)
                    {
                        command.CommandText = "UPDATE Songs SET [Artist] = @artist, [Title] = @title WHERE [ID] = @id";
                        command.Parameters.Add("@title", DbType.String, 0).Value = data.Str2;
                        command.Parameters.Add("@artist", DbType.String, 0).Value = data.Str1;
                        command.Parameters.Add("@id", DbType.Int32, 0).Value = data.Id;
                        command.ExecuteNonQuery();
                    }

                    // update player names
                    foreach (SData data in scores)
                    {
                        command.CommandText = "UPDATE Scores SET [PlayerName] = @player, [Date] = @date WHERE [id] = @id";
                        command.Parameters.Add("@player", DbType.String, 0).Value = data.Str1;
                        command.Parameters.Add("@date", DbType.Int64, 0).Value = data.Ticks;
                        command.Parameters.Add("@id", DbType.Int32, 0).Value = data.Id;
                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();

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
                }
            }

            return true;
        }

        /// <summary>
        ///     Converts a USDX 1.01 or CMD 1.01 database to Vocaluxe format
        /// </summary>
        /// <param name="filePath">Database file path</param>
        /// <returns>True if succeeded</returns>
        private static bool _ConvertFrom101(string filePath)
        {
            using (SQLiteConnection connection = new SQLiteConnection())
            {
                connection.ConnectionString = "Data Source=" + filePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return false;
                }

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = "PRAGMA table_info(US_Scores);";
                    bool dateExists = false;
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
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
                    }

                    //This is a USDX 1.01 DB
                    command.CommandText = !dateExists
                                              ? "INSERT INTO Scores (SongID, PlayerName, Score, LineNr, Date, Medley, Duet, Difficulty) SELECT SongID, Player, Score, '0', '0', '0', '0', Difficulty from US_Scores"
                                              : "INSERT INTO Scores (SongID, PlayerName, Score, LineNr, Date, Medley, Duet, Difficulty) SELECT SongID, Player, Score, '0', Date, '0', '0', Difficulty from US_Scores";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO Songs SELECT ID, Artist, Title, TimesPlayed from US_Songs";
                    command.ExecuteNonQuery();

                    // convert from CP1252 to UTF8
                    List<SData> scores = new List<SData>();
                    List<SData> songs = new List<SData>();

                    Sqlite3.sqlite3 oldDB;
                    int res = Sqlite3.sqlite3_open(filePath, out oldDB);

                    if (res != Sqlite3.SQLITE_OK)
                        CLog.LogError("Error opening Database: " + filePath + " (" + Sqlite3.sqlite3_errmsg(oldDB) + ")");
                    else
                    {
                        Sqlite3.Vdbe stmt = new Sqlite3.Vdbe();
                        res = Sqlite3.sqlite3_prepare_v2(oldDB, "SELECT id, Artist, Title FROM Songs", -1, ref stmt, 0);

                        if (res != Sqlite3.SQLITE_OK)
                            CLog.LogError("Error query Database: " + filePath + " (" + Sqlite3.sqlite3_errmsg(oldDB) + ")");
                        else
                        {
                            //Sqlite3.sqlite3_step(Stmt);
                            Encoding utf8 = Encoding.UTF8;
                            Encoding cp1252 = Encoding.GetEncoding(1252);

                            while (Sqlite3.sqlite3_step(stmt) == Sqlite3.SQLITE_ROW)
                            {
                                SData data = new SData {Id = Sqlite3.sqlite3_column_int(stmt, 0)};

                                byte[] bytes = Sqlite3.sqlite3_column_rawbytes(stmt, 1);
                                data.Str1 = bytes != null ? utf8.GetString(Encoding.Convert(cp1252, utf8, bytes)) : "Someone";

                                bytes = Sqlite3.sqlite3_column_rawbytes(stmt, 2);
                                data.Str2 = bytes != null ? utf8.GetString(Encoding.Convert(cp1252, utf8, bytes)) : "Someone";

                                songs.Add(data);
                            }
                            Sqlite3.sqlite3_finalize(stmt);
                        }

                        stmt = new Sqlite3.Vdbe();

                        // ReSharper disable ConvertIfStatementToConditionalTernaryExpression
                        if (!dateExists)
                            // ReSharper restore ConvertIfStatementToConditionalTernaryExpression
                            res = Sqlite3.sqlite3_prepare_v2(oldDB, "SELECT id, PlayerName FROM Scores", -1, ref stmt, 0);
                        else
                            res = Sqlite3.sqlite3_prepare_v2(oldDB, "SELECT id, PlayerName, Date FROM Scores", -1, ref stmt, 0);

                        if (res != Sqlite3.SQLITE_OK)
                            CLog.LogError("Error query Database: " + filePath + " (" + Sqlite3.sqlite3_errmsg(oldDB) + ")");
                        else
                        {
                            //Sqlite3.sqlite3_step(Stmt);
                            Encoding utf8 = Encoding.UTF8;
                            Encoding cp1252 = Encoding.GetEncoding(1252);

                            while (Sqlite3.sqlite3_step(stmt) == Sqlite3.SQLITE_ROW)
                            {
                                SData data = new SData {Id = Sqlite3.sqlite3_column_int(stmt, 0)};

                                byte[] bytes = Sqlite3.sqlite3_column_rawbytes(stmt, 1);
                                data.Str1 = bytes != null ? utf8.GetString(Encoding.Convert(cp1252, utf8, bytes)) : "Someone";

                                if (dateExists)
                                    data.Ticks = _UnixTimeToTicks(Sqlite3.sqlite3_column_int(stmt, 2));

                                scores.Add(data);
                            }
                            Sqlite3.sqlite3_finalize(stmt);
                        }
                    }
                    Sqlite3.sqlite3_close(oldDB);

                    SQLiteTransaction transaction = connection.BeginTransaction();

                    // update Title and Artist strings
                    foreach (SData data in songs)
                    {
                        command.CommandText = "UPDATE Songs SET [Artist] = @artist, [Title] = @title WHERE [ID] = @id";
                        command.Parameters.Add("@title", DbType.String, 0).Value = data.Str2;
                        command.Parameters.Add("@artist", DbType.String, 0).Value = data.Str1;
                        command.Parameters.Add("@id", DbType.Int32, 0).Value = data.Id;
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
                            command.Parameters.Add("@date", DbType.Int64, 0).Value = data.Ticks;
                        }
                        command.Parameters.Add("@player", DbType.String, 0).Value = data.Str1;
                        command.Parameters.Add("@id", DbType.Int32, 0).Value = data.Id;
                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();

                    //Delete old tables after conversion
                    command.CommandText = "DROP TABLE US_Scores;";
                    command.ExecuteNonQuery();

                    command.CommandText = "DROP TABLE US_Songs;";
                    command.ExecuteNonQuery();
                }
            }

            return true;
        }

        private static bool _UpdateDatabase(int currentVersion, SQLiteConnection connection)
        {
            bool updated = true;

            if (currentVersion < 2)
            {
                updated &= _ConvertV1toV2(connection);
            }
            if (currentVersion < 3)
            {
                updated &= _ConvertV2toV3(connection);
            }

            return updated;
        }

        private static bool _ConvertV1toV2(SQLiteConnection connection)
        {
            using (SQLiteCommand command = new SQLiteCommand(connection))
            {
                command.CommandText = "ALTER TABLE Scores ADD ShortSong INTEGER";
                command.ExecuteNonQuery();
                command.CommandText = "UPDATE Scores SET [ShortSong] = @ShortSong";
                command.Parameters.Add("@ShortSong", DbType.Int32, 0).Value = 0;
                command.ExecuteNonQuery();
                command.CommandText = "UPDATE Version SET [Value] = @version";
                command.Parameters.Add("@version", DbType.Int32, 0).Value = 2;
                command.ExecuteNonQuery();
            }

            return true;
        }

        private static bool _ConvertV2toV3(SQLiteConnection connection)
        {
            SQLiteCommand command;

            command = new SQLiteCommand(connection);

            command.CommandText = "ALTER TABLE Songs ADD DateAdded BIGINT";
            command.ExecuteNonQuery();
            command.CommandText = "UPDATE Songs SET [DateAdded] = @DateAdded";
            command.Parameters.Add("@DateAdded",System.Data.DbType.Int64, 0).Value = DateTime.Now.Ticks;
            command.ExecuteNonQuery();
            command.CommandText = "UPDATE Version SET [Value] = @version";
            command.Parameters.Add("@version", System.Data.DbType.Int32, 0).Value = 3;
            command.ExecuteNonQuery();

            //Read NumPlayed from Scores and save to Songs
            command.CommandText = "SELECT SongID, Date FROM Scores ORDER BY Date ASC";

            SQLiteDataReader reader;
            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception e)
            {
                return false;
            }

            long lastDateAdded = -1;
            int lastID = -1;
            DateTime dt = new DateTime(1, 1, 1, 0, 0, 5);
            long sec = dt.Ticks;
            List<int> ids = new List<int>();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                long dateAdded = reader.GetInt64(1);
                if (id != lastID || dateAdded > lastDateAdded + sec)
                {
                    ids.Add(id);
                    lastID = id;
                    lastDateAdded = dateAdded;
                }
            }
            if (reader != null)
                reader.Dispose();

            foreach (int id in ids)
                _IncreaseSongCounter(id, command);
            command.Dispose();

            return true;
        }

        private static bool _ImportData(string sourceDBPath)
        {
            #region open db
            using (SQLiteConnection connSource = new SQLiteConnection())
            {
                connSource.ConnectionString = "Data Source=" + sourceDBPath;

                try
                {
                    connSource.Open();
                }
                catch (Exception e)
                {
                    CLog.LogError("Error on import high score data. Can't open source database \"" + sourceDBPath + "\" (" + e.Message + ")");
                    return false;
                }
                #endregion open db

                using (SQLiteCommand cmdSource = new SQLiteCommand(connSource))
                {
                    #region import table scores
                    cmdSource.CommandText = "SELECT SongID, PlayerName, Score, LineNr, Date, Medley, Duet, ShortSong, Difficulty FROM Scores";
                    SQLiteDataReader source = cmdSource.ExecuteReader();
                    if (source == null)
                        return false;

                    if (source.FieldCount == 0)
                    {
                        source.Close();
                        return true;
                    }

                    while (source.Read())
                    {
                        int songid = source.GetInt32(0);
                        string player = source.GetString(1);
                        int score = source.GetInt32(2);
                        int linenr = source.GetInt32(3);
                        long date = source.GetInt64(4);
                        int medley = source.GetInt32(5);
                        int duet = source.GetInt32(6);
                        int shortsong = source.GetInt32(7);
                        int diff = source.GetInt32(8);

                        string artist, title, dateadded;
                        int numplayed;
                        if (_GetDataBaseSongInfos(songid, out artist, out title, out numplayed, out dateadded, sourceDBPath))
                            AddScore(player, score, linenr, date, medley, duet, shortsong, diff, artist, title, numplayed, _HighscoreFilePath);
                    }
                    #endregion import table scores

                    source.Close();
                }
            }

            return true;
        }
        #endregion Highscores

        #region Cover
        public static bool GetCover(string coverPath, ref CTexture tex, int maxSize)
        {
            if (!File.Exists(coverPath))
            {
                CLog.LogError("Can't find File: " + coverPath);
                return false;
            }

            if (_ConnectionCover == null)
            {
                _ConnectionCover = new SQLiteConnection {ConnectionString = "Data Source=" + _CoverFilePath};
                _ConnectionCover.Open();
            }

            using (SQLiteCommand command = new SQLiteCommand(_ConnectionCover))
            {
                command.CommandText = "SELECT id, width, height FROM Cover WHERE [Path] = @path";
                command.Parameters.Add("@path", DbType.String, 0).Value = coverPath;

                SQLiteDataReader reader = command.ExecuteReader();

                if (reader != null && reader.HasRows)
                {
                    reader.Read();
                    int id = reader.GetInt32(0);
                    int w = reader.GetInt32(1);
                    int h = reader.GetInt32(2);
                    reader.Close();

                    command.CommandText = "SELECT Data FROM CoverData WHERE CoverID = @id";
                    command.Parameters.Add("@id", DbType.Int32).Value = id;
                    reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        reader.Read();
                        byte[] data = _GetBytes(reader);
                        reader.Dispose();
                        tex = CDraw.EnqueueTexture(w, h, data);
                        return true;
                    }
                }
                else
                {
                    if (reader != null)
                        reader.Close();

                    if (_TransactionCover == null)
                        _TransactionCover = _ConnectionCover.BeginTransaction();

                    Bitmap origin;
                    try
                    {
                        origin = new Bitmap(coverPath);
                    }
                    catch (Exception)
                    {
                        CLog.LogError("Error loading Texture: " + coverPath);
                        return false;
                    }

                    int w = maxSize;
                    int h = maxSize;
                    byte[] data;

                    try
                    {
                        if (origin.Width >= origin.Height && origin.Width > w)
                            h = (int)Math.Round((float)w / origin.Width * origin.Height);
                        else if (origin.Height > origin.Width && origin.Height > h)
                            w = (int)Math.Round((float)h / origin.Height * origin.Width);

                        using (Bitmap bmp = new Bitmap(w, h))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                                g.DrawImage(origin, new Rectangle(0, 0, w, h));

                            data = new byte[w * h * 4];
                            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                            Marshal.Copy(bmpData.Scan0, data, 0, w * h * 4);
                            bmp.UnlockBits(bmpData);
                        }
                    }
                    finally
                    {
                        origin.Dispose();
                    }

                    tex = CDraw.EnqueueTexture(w, h, data);

                    command.CommandText = "INSERT INTO Cover (Path, width, height) VALUES (@path, @w, @h)";
                    command.Parameters.Add("@w", DbType.Int32).Value = w;
                    command.Parameters.Add("@h", DbType.Int32).Value = h;
                    command.Parameters.Add("@path", DbType.String, 0).Value = coverPath;
                    command.ExecuteNonQuery();

                    command.CommandText = "SELECT id FROM Cover WHERE [Path] = @path";
                    command.Parameters.Add("@path", DbType.String, 0).Value = coverPath;
                    reader = command.ExecuteReader();

                    if (reader != null)
                    {
                        reader.Read();
                        int id = reader.GetInt32(0);
                        reader.Dispose();
                        command.CommandText = "INSERT INTO CoverData (CoverID, Data) VALUES (@id, @data)";
                        command.Parameters.Add("@id", DbType.Int32).Value = id;
                        command.Parameters.Add("@data", DbType.Binary, 20).Value = data;
                        command.ExecuteReader();
                        return true;
                    }
                }
            }

            return false;
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
                _ConnectionCover.Dispose();
                _ConnectionCover = null;
            }
        }

        private static bool _InitCoverDB()
        {
            using (SQLiteConnection connection = new SQLiteConnection())
            {
                connection.ConnectionString = "Data Source=" + _CoverFilePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return false;
                }

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT Value FROM Version";

                    SQLiteDataReader reader = null;

                    try
                    {
                        reader = command.ExecuteReader();
                    }
                    catch (Exception) {}

                    if (reader == null)
                    {
                        // create new database/tables
                        _CreateCoverDB();
                    }
                    else if (reader.FieldCount == 0)
                    {
                        // create new database/tables
                        _CreateCoverDB();
                    }
                    else
                    {
                        reader.Read();

                        if (reader.GetInt32(0) < CSettings.DatabaseHighscoreVersion)
                        {
                            // update database
                        }
                    }
                    if (reader != null)
                        reader.Dispose();
                }
            }
            return true;
        }

        private static void _CreateCoverDB()
        {
            using (SQLiteConnection connection = new SQLiteConnection())
            {
                connection.ConnectionString = "Data Source=" + _CoverFilePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return;
                }

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = "CREATE TABLE IF NOT EXISTS Version ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Value INTEGER NOT NULL);";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO Version (id, Value) VALUES(NULL, @Value)";
                    command.Parameters.Add("@Value", DbType.Int32).Value = CSettings.DatabaseCoverVersion;
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE TABLE IF NOT EXISTS Cover ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                          "Path TEXT NOT NULL, width INTEGER NOT NULL, height INTEGER NOT NULL);";
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE TABLE IF NOT EXISTS CoverData ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                          "CoverID INTEGER NOT NULL, Data BLOB NOT NULL);";
                    command.ExecuteNonQuery();
                }
            }
        }
        #endregion Cover

        #region CreditsRessources
        public static bool GetCreditsRessource(string fileName, ref CTexture tex)
        {
            bool result = false;

            using (SQLiteConnection connection = new SQLiteConnection())
            {
                connection.ConnectionString = "Data Source=" + _CreditsRessourcesFilePath;
                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return false;
                }
                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT id, width, height FROM Images WHERE [Path] = @path";
                    command.Parameters.Add("@path", DbType.String, 0).Value = fileName;

                    SQLiteDataReader reader = command.ExecuteReader();

                    if (reader != null && reader.HasRows)
                    {
                        reader.Read();
                        int id = reader.GetInt32(0);
                        int w = reader.GetInt32(1);
                        int h = reader.GetInt32(2);
                        reader.Close();

                        command.CommandText = "SELECT Data FROM ImageData WHERE ImageID = @id";
                        command.Parameters.Add("@id", DbType.Int32).Value = id;
                        reader = command.ExecuteReader();

                        if (reader.HasRows)
                        {
                            result = true;
                            reader.Read();
                            byte[] data = _GetBytes(reader);
                            tex = CDraw.AddTexture(w, h, data);
                            /*using (Bitmap bmp = new Bitmap(w, h))
                            {
                                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                                Marshal.Copy(data, 0, bmpData.Scan0, w * h * 4);
                                bmp.UnlockBits(bmpData);
                                bmp.Save(fileName);
                            }*/
                        }
                    }

                    if (reader != null)
                        reader.Dispose();
                }
            }

            return result;
        }

        //If you want to add an image to db, call this method!
        /*
        private static bool _AddImageToCreditsDB(String imagePath)
        {
            bool result = false;

            if (File.Exists(imagePath))
            {
                using (SQLiteConnection connection = new SQLiteConnection())
                {
                    connection.ConnectionString = "Data Source=" + _CreditsRessourcesFilePath;
                    try
                    {
                        connection.Open();
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    using (SQLiteCommand command = new SQLiteCommand(connection))
                    {
                        Bitmap origin;
                        try
                        {
                            origin = new Bitmap(imagePath);
                        }
                        catch (Exception)
                        {
                            CLog.LogError("Error loading Texture: " + imagePath);
                            return false;
                        }
                        int w = origin.Width;
                        int h = origin.Height;
                        byte[] data;

                        try
                        {
                            data = new byte[w * h * 4];

                            BitmapData bmpData = origin.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                            Marshal.Copy(bmpData.Scan0, data, 0, w * h * 4);
                            origin.UnlockBits(bmpData);

                            command.CommandText = "INSERT INTO Images (Path, width, height) VALUES (@path, @w, @h)";
                            command.Parameters.Add("@path", DbType.String, 0).Value = Path.GetFileName(imagePath);
                            command.Parameters.Add("@w", DbType.Int32, 0).Value = w;
                            command.Parameters.Add("@h", DbType.Int32, 0).Value = h;
                            command.ExecuteNonQuery();

                            command.CommandText = "SELECT id FROM Images WHERE [Path] = @path";
                            command.Parameters.Add("@path", DbType.String, 0).Value = Path.GetFileName(imagePath);
                            SQLiteDataReader reader = command.ExecuteReader();

                            if (reader != null)
                            {
                                reader.Read();
                                int id = reader.GetInt32(0);
                                reader.Close();
                                command.CommandText = "INSERT INTO ImageData (ImageID, Data) VALUES (@id, @data)";
                                command.Parameters.Add("@id", DbType.Int32, 20).Value = id;
                                command.Parameters.Add("@data", DbType.Binary, 20).Value = data;
                                command.ExecuteReader();
                                result = true;
                            }
                        }
                        finally
                        {
                            origin.Dispose();
                        }
                    }
                }
            }

            return result;
        }
*/

        private static bool _InitCreditsRessourcesDB()
        {
            using (SQLiteConnection connection = new SQLiteConnection())
            {
                connection.ConnectionString = "Data Source=" + _CreditsRessourcesFilePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return false;
                }

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT Value FROM Version";

                    SQLiteDataReader reader = null;

                    try
                    {
                        reader = command.ExecuteReader();
                    }
                    catch (Exception) {}

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

                        if (reader.GetInt32(0) < CSettings.DatabaseHighscoreVersion)
                        {
                            // update database
                        }
                    }

                    if (reader != null)
                        reader.Dispose();
                }
            }
            return true;
        }

        /*
        private static void _CreateCreditsRessourcesDB()
        {
            using (SQLiteConnection connection = new SQLiteConnection())
            {
                connection.ConnectionString = "Data Source=" + _CreditsRessourcesFilePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return;
                }

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = "CREATE TABLE IF NOT EXISTS Version ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Value INTEGER NOT NULL);";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO Version (id, Value) VALUES(NULL, @Value)";
                    command.Parameters.Add("@Value", DbType.Int32).Value = CSettings.DatabaseCreditsRessourcesVersion;
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE TABLE IF NOT EXISTS Images ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                          "Path TEXT NOT NULL, width INTEGER NOT NULL, height INTEGER NOT NULL);";
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE TABLE IF NOT EXISTS ImageData ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                          "ImageID INTEGER NOT NULL, Data BLOB NOT NULL);";
                    command.ExecuteNonQuery();
                }
            }
        }
*/
        #endregion CreditsRessources

        private static byte[] _GetBytes(SQLiteDataReader reader)
        {
            const int chunkSize = 2 * 1024;
            byte[] buffer = new byte[chunkSize];
            long fieldOffset = 0;
            using (MemoryStream stream = new MemoryStream())
            {
                long bytesRead;
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

        private static long _UnixTimeToTicks(int unixTime)
        {
            DateTime t70 = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            t70 = t70.AddSeconds(unixTime);
            return t70.Ticks;
        }
    }
}