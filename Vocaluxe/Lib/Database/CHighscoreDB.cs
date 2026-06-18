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
using System.Collections.Generic;
using System.IO;
using System.Text;
using Community.CsharpSqlite;
using Microsoft.Data.Sqlite;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Database
{
    public class CHighscoreDB : CDatabaseBase
    {
        private struct SData
        {
            public int Id;
            public long Ticks;
            public string Str1;
            public string Str2;
        }

        public CHighscoreDB(string filePath) : base(filePath) { }

        public override bool Init()
        {
            var oldDBFilePath = Path.Combine(CSettings.DataFolder, CSettings.FileNameOldHighscoreDB);
            if (File.Exists(oldDBFilePath))
            {
                if (File.Exists(_FilePath))
                {
                    if (!_CreateOrConvert(oldDBFilePath))
                    {
                        CLog.Fatal("Cannot init Highscore DB: Error opening old database: {DBFile}" + CLog.Params(oldDBFilePath));
                        return false;
                    }

                    if (!_CreateOrConvert(_FilePath))
                    {
                        CLog.Fatal("Cannot init Highscore DB: Error opening database: {DBFile}", CLog.Params(_FilePath));
                        return false;
                    }

                    if (!_ImportData(oldDBFilePath))
                    {
                        CLog.Fatal("Cannot init Highscore DB: Error importing data");
                        return false;
                    }
                }
                else
                {
                    File.Copy(oldDBFilePath, _FilePath);
                    if (!_CreateOrConvert(_FilePath))
                    {
                        CLog.Fatal("Cannot init Highscore DB: Error opening database: {DBFile}" + CLog.Params(_FilePath));
                        return false;
                    }
                }

                File.Delete(oldDBFilePath);
            }
            else if (!_CreateOrConvert(_FilePath))
            {
                CLog.Fatal("Cannot init Highscore DB: Error opening database: {DBFile}", CLog.Params(_FilePath));
                return false;
            }

            return true;
        }

        public override void Close()
        {
            //Do nothing
        }

        public bool GetDataBaseSongInfos(string artist, string title, out int numPlayed, out DateTime dateAdded, out int highscoreId)
        {
            string sArtist;
            string sTitle;
            int songId;
            using (var connection = new SqliteConnection())
            {
                connection.ConnectionString = "Data Source=" + _FilePath;

                try
                {
                    connection.Open();
                }
                catch (Exception) { }

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    songId = _GetDataBaseSongId(artist, title, 0, command);
                    highscoreId = songId;
                }
            }

            return _GetDataBaseSongInfos(songId, out sArtist, out sTitle, out numPlayed, out dateAdded, _FilePath);
        }

        public void IncreaseSongCounter(int dataBaseSongId)
        {
            using (var connection = new SqliteConnection())
            {
                connection.ConnectionString = "Data Source=" + _FilePath;

                try
                {
                    connection.Open();
                }
                catch (Exception) { }

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    _IncreaseSongCounter(dataBaseSongId, command);
                }
            }
        }

        public int AddScore(string playerName, int score, int lineNr, long date, int medley, int duet, int shortSong, int difficulty,
            string artist, string title, int numPlayed, string filePath)
        {
            using (var connection = new SqliteConnection())
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

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    var dataBaseSongId = _GetDataBaseSongId(artist, title, numPlayed, command);
                    var result = _AddScore(playerName, score, lineNr, date, medley, duet, shortSong, difficulty, dataBaseSongId, command);
                    return result;
                }
            }
        }

        public int AddScore(SPlayer player)
        {
            using (var connection = new SqliteConnection())
            {
                connection.ConnectionString = "Data Source=" + _FilePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return -1;
                }

                var medley = 0;
                var duet = 0;
                var shortSong = 0;
                switch (player.GameMode)
                {
                    case EGameMode.TR_GAMEMODE_MEDLEY:
                        medley = 1;
                        break;
                    case EGameMode.TR_GAMEMODE_DUET:
                        duet = 1;
                        break;
                    case EGameMode.TR_GAMEMODE_SHORTSONG:
                        shortSong = 1;
                        break;
                }

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    var dataBaseSongId = CSongs.GetSong(player.SongId).DataBaseSongId;
                    return _AddScore(CProfiles.GetPlayerName(player.ProfileId), (int)Math.Round(player.Points), player.VoiceNr, player.DateTicks, medley,
                        duet, shortSong, (int)CProfiles.GetDifficulty(player.ProfileId), dataBaseSongId, command);
                }
            }
        }

        private int _AddScore(string playerName, int score, int lineNr, long date, int medley, int duet, int shortSong, int difficulty,
            int dataBaseSongId, SqliteCommand command)
        {
            var lastInsertId = -1;

            if (dataBaseSongId >= 0)
            {
                command.CommandText = "SELECT id FROM Scores WHERE SongId = @SongId AND PlayerName = @PlayerName AND Score = @Score AND " +
                                      "LineNr = @LineNr AND Date = @Date AND Medley = @Medley AND Duet = @Duet AND ShortSong = @ShortSong AND Difficulty = @Difficulty";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@SongId", dataBaseSongId);
                command.Parameters.AddWithValue("@PlayerName", playerName ?? string.Empty);
                command.Parameters.AddWithValue("@Score", score);
                command.Parameters.AddWithValue("@LineNr", lineNr);
                command.Parameters.AddWithValue("@Date", date);
                command.Parameters.AddWithValue("@Medley", medley);
                command.Parameters.AddWithValue("@Duet", duet);
                command.Parameters.AddWithValue("@ShortSong", shortSong);
                command.Parameters.AddWithValue("@Difficulty", difficulty);

                SqliteDataReader reader = null;
                try
                {
                    reader = command.ExecuteReader();
                }
                catch (Exception) { }

                if (reader != null && reader.HasRows)
                {
                    if (reader.Read())
                    {
                        return reader.GetInt32(0);
                    }
                }

                if (reader != null)
                {
                    reader.Dispose();
                }

                command.CommandText = "INSERT INTO Scores (SongId, PlayerName, Score, LineNr, Date, Medley, Duet, ShortSong, Difficulty) " +
                                      "VALUES (@SongId, @PlayerName, @Score, @LineNr, @Date, @Medley, @Duet, @ShortSong, @Difficulty)";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@SongId", dataBaseSongId);
                command.Parameters.AddWithValue("@PlayerName", playerName ?? string.Empty);
                command.Parameters.AddWithValue("@Score", score);
                command.Parameters.AddWithValue("@LineNr", lineNr);
                command.Parameters.AddWithValue("@Date", date);
                command.Parameters.AddWithValue("@Medley", medley);
                command.Parameters.AddWithValue("@Duet", duet);
                command.Parameters.AddWithValue("@ShortSong", shortSong);
                command.Parameters.AddWithValue("@Difficulty", difficulty);
                command.ExecuteNonQuery();

                //Read last insert line
                command.CommandText = "SELECT id FROM Scores ORDER BY id DESC LIMIT 0, 1";

                reader = command.ExecuteReader();

                if (reader != null && reader.HasRows)
                {
                    while (reader.Read())
                    {
                        lastInsertId = reader.GetInt32(0);
                    }

                    reader.Dispose();
                }
            }

            return lastInsertId;
        }

        public List<SDBScoreEntry> LoadScore(int songId, EGameMode gameMode, EHighscoreStyle style)
        {
            var scores = new List<SDBScoreEntry>();
            using (var connection = new SqliteConnection())
            {
                connection.ConnectionString = "Data Source=" + _FilePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return scores;
                }

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    var medley = 0;
                    var duet = 0;
                    var shortSong = 0;
                    switch (gameMode)
                    {
                        case EGameMode.TR_GAMEMODE_MEDLEY:
                            medley = 1;
                            break;
                        case EGameMode.TR_GAMEMODE_DUET:
                            duet = 1;
                            break;
                        case EGameMode.TR_GAMEMODE_SHORTSONG:
                            shortSong = 1;
                            break;
                    }

                    var dataBaseSongId = _GetDataBaseSongId(songId, command);
                    if (dataBaseSongId < 0)
                    {
                        return scores;
                    }

                    switch (style)
                    {
                        case EHighscoreStyle.TR_CONFIG_HIGHSCORE_LIST_BEST:
                            command.CommandText = "SELECT os.PlayerName, os.Score, os.Date, os.Difficulty, os.LineNr, os.id " +
                                                  "FROM Scores os " +
                                                  "INNER JOIN ( " +
                                                  "SELECT sc.PlayerName, sc.Score, sc.Difficulty, sc.LineNr, MIN(sc.Date) AS Date " +
                                                  "FROM Scores sc " +
                                                  "INNER JOIN ( " +
                                                  "SELECT Playername, MAX(Score) AS Score, Difficulty, LineNr " +
                                                  "FROM Scores " +
                                                  "WHERE [SongId] = @SongId AND [Medley] = @Medley AND [Duet] = @Duet AND [ShortSong] = @ShortSong " +
                                                  "GROUP BY PlayerName, Difficulty, LineNr " +
                                                  ") AS mc " +
                                                  "ON sc.PlayerName = mc.PlayerName AND sc.Difficulty = mc.Difficulty AND sc.LineNr = mc.LineNr AND sc.Score = mc.Score " +
                                                  "WHERE [SongId] = @SongId AND [Medley] = @Medley AND [Duet] = @Duet AND [ShortSong] = @ShortSong " +
                                                  "GROUP BY sc.PlayerName, sc.Difficulty, sc.LineNr, sc.Score " +
                                                  ") AS iq " +
                                                  "ON os.PlayerName = iq.PlayerName AND os.Difficulty = iq.Difficulty AND os.LineNr = iq.LineNr AND os.Score = iq.Score AND os.Date = iq.Date " +
                                                  "WHERE [SongId] = @SongId AND [Medley] = @Medley AND [Duet] = @Duet AND [ShortSong] = @ShortSong " +
                                                  "GROUP BY os.PlayerName, os.Difficulty, os.LineNr, os.Score " +
                                                  "ORDER BY os.Score DESC, os.Date ASC";
                            break;
                        case EHighscoreStyle.TR_CONFIG_HIGHSCORE_LIST_ALL:
                            command.CommandText = "SELECT PlayerName, Score, Date, Difficulty, LineNr, id " +
                                                  "FROM Scores " +
                                                  "WHERE [SongId] = @SongId AND [Medley] = @Medley AND [Duet] = @Duet AND [ShortSong] = @ShortSong " +
                                                  "ORDER BY [Score] DESC, [Date] ASC";
                            break;
                    }

                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@SongId", dataBaseSongId);
                    command.Parameters.AddWithValue("@Medley", medley);
                    command.Parameters.AddWithValue("@Duet", duet);
                    command.Parameters.AddWithValue("@ShortSong", shortSong);

                    var reader = command.ExecuteReader();
                    if (reader != null && reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            var score = new SDBScoreEntry
                            {
                                Name = reader.GetString(0),
                                Score = reader.GetInt32(1),
                                Date = new DateTime(reader.GetInt64(2)).ToString("dd/MM/yyyy"),
                                Difficulty = (EGameDifficulty)reader.GetInt32(3),
                                VoiceNr = reader.GetInt32(4),
                                Id = reader.GetInt32(5)
                            };

                            scores.Add(score);
                        }

                        reader.Dispose();
                    }
                }
            }

            return scores;
        }

        private void _IncreaseSongCounter(int dataBaseSongId, SqliteCommand command)
        {
            command.CommandText = "UPDATE Songs SET NumPlayed = NumPlayed + 1 WHERE [id] = @id";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@id", dataBaseSongId);
            command.ExecuteNonQuery();
        }

        private int _GetDataBaseSongId(int songId, SqliteCommand command)
        {
            var song = CSongs.GetSong(songId);

            if (song == null)
            {
                return -1;
            }

            return _GetDataBaseSongId(song.Artist, song.Title, 0, command);
        }

        private int _GetDataBaseSongId(string artist, string title, int defNumPlayed, SqliteCommand command)
        {
            command.CommandText = "SELECT id FROM Songs WHERE [Title] = @title AND [Artist] = @artist";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@title", title ?? string.Empty);
            command.Parameters.AddWithValue("@artist", artist ?? string.Empty);

            var reader = command.ExecuteReader();

            if (reader != null && reader.HasRows)
            {
                reader.Read();
                var id = reader.GetInt32(0);
                reader.Dispose();
                return id;
            }

            if (reader != null)
            {
                reader.Close();
            }

            command.CommandText = "INSERT INTO Songs (Title, Artist, NumPlayed, DateAdded) " +
                                  "VALUES (@title, @artist, @numplayed, @dateadded)";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@title", title ?? string.Empty);
            command.Parameters.AddWithValue("@artist", artist ?? string.Empty);
            command.Parameters.AddWithValue("@numplayed", defNumPlayed);
            command.Parameters.AddWithValue("@dateadded", DateTime.Now.Ticks);
            command.ExecuteNonQuery();

            command.CommandText = "SELECT id FROM Songs WHERE [Title] = @title AND [Artist] = @artist";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@title", title ?? string.Empty);
            command.Parameters.AddWithValue("@artist", artist ?? string.Empty);

            reader = command.ExecuteReader();

            if (reader != null)
            {
                reader.Read();
                var id = reader.GetInt32(0);
                reader.Dispose();
                return id;
            }

            return -1;
        }

        private bool _GetDataBaseSongInfos(int songId, out string artist, out string title, out int numPlayed, out DateTime dateAdded, string filePath)
        {
            artist = string.Empty;
            title = string.Empty;
            numPlayed = 0;
            dateAdded = DateTime.Today;

            using (var connection = new SqliteConnection())
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

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "SELECT Artist, Title, NumPlayed, DateAdded FROM Songs WHERE [id] = @id";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@id", songId);

                    SqliteDataReader reader;
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

                        if (!reader.IsDBNull(0))
                        {
                            artist = reader.GetString(0);
                        }

                        if (!reader.IsDBNull(1))
                        {
                            title = reader.GetString(1);
                        }

                        if (!reader.IsDBNull(2))
                        {
                            numPlayed = reader.GetInt32(2);
                        }

                        if (!reader.IsDBNull(3))
                        {
                            dateAdded = new DateTime(reader.GetInt64(3));
                        }

                        reader.Dispose();
                        return true;
                    }

                    if (reader != null)
                    {
                        reader.Dispose();
                    }
                }
            }

            return false;
        }

        private void _CreateHighscoreDB(string filePath)
        {
            using (var connection = new SqliteConnection())
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

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "CREATE TABLE IF NOT EXISTS Version ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Value INTEGER NOT NULL);";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO Version (id, Value) VALUES(NULL, @Value)";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@Value", CSettings.DatabaseHighscoreVersion);
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE TABLE IF NOT EXISTS Songs ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                          "Artist TEXT NOT NULL, Title TEXT NOT NULL, NumPlayed INTEGER, DateAdded BIGINT);";
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE TABLE IF NOT EXISTS Scores ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                          "SongId INTEGER NOT NULL, PlayerName TEXT NOT NULL, Score INTEGER NOT NULL, LineNr INTEGER NOT NULL, Date BIGINT NOT NULL, " +
                                          "Medley INTEGER NOT NULL, Duet INTEGER NOT NULL, ShortSong INTEGER NOT NULL, Difficulty INTEGER NOT NULL);";
                    command.ExecuteNonQuery();
                }
            }
        }

        private void _CreateHighscoreDBV1(string filePath)
        {
            using (var connection = new SqliteConnection())
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

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "CREATE TABLE IF NOT EXISTS Version ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Value INTEGER NOT NULL);";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO Version (id, Value) VALUES(NULL, 1 )";
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE TABLE IF NOT EXISTS Songs ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                          "Artist TEXT NOT NULL, Title TEXT NOT NULL, NumPlayed INTEGER);";
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE TABLE IF NOT EXISTS Scores ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                          "SongId INTEGER NOT NULL, PlayerName TEXT NOT NULL, Score INTEGER NOT NULL, LineNr INTEGER NOT NULL, Date BIGINT NOT NULL, " +
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
        private bool _CreateOrConvert(string filePath)
        {
            var result = true;
            using (var connection = new SqliteConnection())
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

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "PRAGMA user_version";
                    var reader = command.ExecuteReader();
                    reader.Read();

                    var version = reader.GetInt32(0);

                    reader.Dispose();

                    //Check if old scores table exists
                    command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='US_Scores';";
                    reader = command.ExecuteReader();
                    reader.Read();
                    var scoresTableExists = reader.HasRows;

                    reader.Dispose();

                    command.CommandText = "SELECT Value FROM Version";
                    reader = null;

                    try
                    {
                        reader = command.ExecuteReader();
                    }
                    catch (Exception) { }

                    if (reader == null || reader.FieldCount == 0)
                    {
                        // create new database/tables
                        if (version == 1)
                        {
                            //Check for USDX 1.1 DB
                            _CreateHighscoreDBV1(filePath);
                            result &= _ConvertFrom110(filePath);
                            result &= _UpdateDatabase(1, connection);
                            result &= _UpdateDatabase(2, connection);
                        }
                        else if (version == 0 && scoresTableExists)
                        {
                            //Check for USDX 1.01 or CMD Mod DB
                            _CreateHighscoreDBV1(filePath);
                            result &= _ConvertFrom101(filePath);
                            result &= _UpdateDatabase(1, connection);
                            result &= _UpdateDatabase(2, connection);
                        }
                        else
                        {
                            _CreateHighscoreDB(filePath);
                        }
                    }
                    else
                    {
                        reader.Read();
                        var currentVersion = reader.GetInt32(0);
                        if (currentVersion < CSettings.DatabaseHighscoreVersion)
                        {
                            // update database
                            result &= _UpdateDatabase(currentVersion, connection);
                        }
                    }

                    if (reader != null)
                    {
                        reader.Dispose();
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///     Converts a USDX 1.1 database into the Vocaluxe format
        /// </summary>
        /// <param name="filePath">Database file path</param>
        /// <returns>True if succeeded</returns>
        private bool _ConvertFrom110(string filePath)
        {
            using (var connection = new SqliteConnection())
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

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    //The USDX database has no column for LineNr, Medley and Duet so just fill 0 in there
                    command.CommandText =
                        "INSERT INTO Scores (SongId, PlayerName, Score, LineNr, Date, Medley, Duet, Difficulty) SELECT SongId, Player, Score, '0', Date, '0', '0', Difficulty from US_Scores";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO Songs SELECT Id, Artist, Title, TimesPlayed from US_Songs";
                    command.ExecuteNonQuery();

                    var scores = new List<SData>();
                    var songs = new List<SData>();

                    command.CommandText = "SELECT id, PlayerName, Date FROM Scores";
                    var reader = command.ExecuteReader();

                    if (reader != null && reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            var data = new SData { Id = reader.GetInt32(0), Str1 = reader.GetString(1) };
                            Int64 ticks = 0;

                            try
                            {
                                ticks = reader.GetInt64(2);
                            }
                            catch { }

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
                            var data = new SData { Id = reader.GetInt32(0), Str1 = reader.GetString(1), Str2 = reader.GetString(2) };
                            songs.Add(data);
                        }
                    }

                    if (reader != null)
                    {
                        reader.Dispose();
                    }

                    var transaction = connection.BeginTransaction();
                    command.Transaction = transaction;
                    // update Title and Artist strings
                    foreach (var data in songs)
                    {
                        command.CommandText = "UPDATE Songs SET [Artist] = @artist, [Title] = @title WHERE [Id] = @id";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@title", data.Str2 ?? string.Empty);
                        command.Parameters.AddWithValue("@artist", data.Str1 ?? string.Empty);
                        command.Parameters.AddWithValue("@id", data.Id);
                        command.ExecuteNonQuery();
                    }

                    // update player names
                    foreach (var data in scores)
                    {
                        command.CommandText = "UPDATE Scores SET [PlayerName] = @player, [Date] = @date WHERE [id] = @id";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@player", data.Str1 ?? string.Empty);
                        command.Parameters.AddWithValue("@date", data.Ticks);
                        command.Parameters.AddWithValue("@id", data.Id);
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    //Delete old tables after conversion
                    command.CommandText = "DROP TABLE IF EXISTS us_scores;";
                    command.ExecuteNonQuery();

                    command.CommandText = "DROP TABLE IF EXISTS us_songs;";
                    command.ExecuteNonQuery();

                    command.CommandText = "DROP TABLE IF EXISTS us_statistics_info;";
                    command.ExecuteNonQuery();

                    command.CommandText = "DROP TABLE IF EXISTS us_users_info;";
                    command.ExecuteNonQuery();

                    command.CommandText = "DROP TABLE IF EXISTS us_webs;";
                    command.ExecuteNonQuery();

                    command.CommandText = "DROP TABLE IF EXISTS us_webs_stats;";
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
        private bool _ConvertFrom101(string filePath)
        {
            using (var connection = new SqliteConnection())
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

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "PRAGMA table_info(US_Scores);";
                    var dateExists = false;
                    using (var reader = command.ExecuteReader())
                    {
                        //Check for column Date
                        while (reader.Read())
                        {
                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                if (reader.GetName(i) == "name")
                                {
                                    if (reader.GetString(i) == "Date")
                                    {
                                        dateExists = true;
                                    }

                                    break;
                                }
                            }
                        }
                    }

                    //This is a USDX 1.01 DB
                    command.CommandText = !dateExists
                        ? "INSERT INTO Scores (SongId, PlayerName, Score, LineNr, Date, Medley, Duet, Difficulty) SELECT SongId, Player, Score, '0', '0', '0', '0', Difficulty from US_Scores"
                        : "INSERT INTO Scores (SongId, PlayerName, Score, LineNr, Date, Medley, Duet, Difficulty) SELECT SongId, Player, Score, '0', Date, '0', '0', Difficulty from US_Scores";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO Songs SELECT Id, Artist, Title, TimesPlayed from US_Songs";
                    command.ExecuteNonQuery();

                    // convert from CP1252 to UTF8
                    var scores = new List<SData>();
                    var songs = new List<SData>();

                    Sqlite3.sqlite3 oldDB;
                    var res = Sqlite3.sqlite3_open(filePath, out oldDB);

                    if (res != Sqlite3.SQLITE_OK)
                    {
                        CLog.Error("Error opening Database: " + filePath + " (" + Sqlite3.sqlite3_errmsg(oldDB) + ")");
                    }
                    else
                    {
                        var stmt = new Sqlite3.Vdbe();
                        res = Sqlite3.sqlite3_prepare_v2(oldDB, "SELECT id, Artist, Title FROM Songs", -1, ref stmt, 0);

                        if (res != Sqlite3.SQLITE_OK)
                        {
                            CLog.Error("Error query Database: " + filePath + " (" + Sqlite3.sqlite3_errmsg(oldDB) + ")");
                        }
                        else
                        {
                            //Sqlite3.sqlite3_step(Stmt);
                            var utf8 = Encoding.UTF8;
                            var cp1252 = Encoding.GetEncoding(1252);

                            while (Sqlite3.sqlite3_step(stmt) == Sqlite3.SQLITE_ROW)
                            {
                                var data = new SData { Id = Sqlite3.sqlite3_column_int(stmt, 0) };

                                var bytes = Sqlite3.sqlite3_column_rawbytes(stmt, 1);
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
                        {
                            res = Sqlite3.sqlite3_prepare_v2(oldDB, "SELECT id, PlayerName FROM Scores", -1, ref stmt, 0);
                        }
                        else
                        {
                            res = Sqlite3.sqlite3_prepare_v2(oldDB, "SELECT id, PlayerName, Date FROM Scores", -1, ref stmt, 0);
                        }

                        if (res != Sqlite3.SQLITE_OK)
                        {
                            CLog.Error("Error query Database: " + filePath + " (" + Sqlite3.sqlite3_errmsg(oldDB) + ")");
                        }
                        else
                        {
                            //Sqlite3.sqlite3_step(Stmt);
                            var utf8 = Encoding.UTF8;
                            var cp1252 = Encoding.GetEncoding(1252);

                            while (Sqlite3.sqlite3_step(stmt) == Sqlite3.SQLITE_ROW)
                            {
                                var data = new SData { Id = Sqlite3.sqlite3_column_int(stmt, 0) };

                                var bytes = Sqlite3.sqlite3_column_rawbytes(stmt, 1);
                                data.Str1 = bytes != null ? utf8.GetString(Encoding.Convert(cp1252, utf8, bytes)) : "Someone";

                                if (dateExists)
                                {
                                    data.Ticks = _UnixTimeToTicks(Sqlite3.sqlite3_column_int(stmt, 2));
                                }

                                scores.Add(data);
                            }

                            Sqlite3.sqlite3_finalize(stmt);
                        }
                    }

                    Sqlite3.sqlite3_close(oldDB);

                    var transaction = connection.BeginTransaction();
                    command.Transaction = transaction;

                    // update Title and Artist strings
                    foreach (var data in songs)
                    {
                        command.CommandText = "UPDATE Songs SET [Artist] = @artist, [Title] = @title WHERE [Id] = @id";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@title", data.Str2 ?? string.Empty);
                        command.Parameters.AddWithValue("@artist", data.Str1 ?? string.Empty);
                        command.Parameters.AddWithValue("@id", data.Id);
                        command.ExecuteNonQuery();
                    }

                    // update player names
                    foreach (var data in scores)
                    {
                        if (!dateExists)
                        {
                            command.CommandText = "UPDATE Scores SET [PlayerName] = @player WHERE [id] = @id";
                        }
                        else
                        {
                            command.CommandText = "UPDATE Scores SET [PlayerName] = @player, [Date] = @date WHERE [id] = @id";
                        }

                        command.Parameters.Clear();
                        if (dateExists)
                        {
                            command.Parameters.AddWithValue("@date", data.Ticks);
                        }

                        command.Parameters.AddWithValue("@player", data.Str1 ?? string.Empty);
                        command.Parameters.AddWithValue("@id", data.Id);
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

        private bool _UpdateDatabase(int currentVersion, SqliteConnection connection)
        {
            var updated = true;

            if (currentVersion < 2)
            {
                updated &= _ConvertV1toV2(connection);
            }
            else if (currentVersion < 3)
            {
                updated &= _ConvertV2toV3(connection);
            }

            return updated;
        }

        private bool _ConvertV1toV2(SqliteConnection connection)
        {
            using (var command = new SqliteCommand())
            {
                command.Connection = connection;
                command.CommandText = "ALTER TABLE Scores ADD ShortSong INTEGER";
                command.ExecuteNonQuery();
                command.CommandText = "UPDATE Scores SET [ShortSong] = @ShortSong";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@ShortSong", 0);
                command.ExecuteNonQuery();
                command.CommandText = "UPDATE Version SET [Value] = @version";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@version", 2);
                command.ExecuteNonQuery();
            }

            return true;
        }

        private bool _ConvertV2toV3(SqliteConnection connection)
        {
            var command = new SqliteCommand("ALTER TABLE Songs ADD DateAdded BIGINT", connection);

            command.ExecuteNonQuery();
            command.CommandText = "UPDATE Songs SET [DateAdded] = @DateAdded";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@DateAdded", DateTime.Now.Ticks);
            command.ExecuteNonQuery();
            command.CommandText = "UPDATE Version SET [Value] = @version";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@version", 3);
            command.ExecuteNonQuery();

            //Read NumPlayed from Scores and save to Songs
            command.CommandText = "SELECT SongId, Date FROM Scores ORDER BY Date ASC";

            SqliteDataReader reader;
            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception)
            {
                return false;
            }

            long lastDateAdded = -1;
            var lastId = -1;
            var dt = new DateTime(1, 1, 1, 0, 0, 5);
            var sec = dt.Ticks;
            var ids = new List<int>();
            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var dateAdded = reader.GetInt64(1);
                if (id != lastId || dateAdded > lastDateAdded + sec)
                {
                    ids.Add(id);
                    lastId = id;
                    lastDateAdded = dateAdded;
                }
            }

            reader.Dispose();

            foreach (var id in ids)
            {
                _IncreaseSongCounter(id, command);
            }

            command.Dispose();

            return true;
        }

        private bool _ImportData(string sourceDBPath)
        {
            #region open db
            using (var connSource = new SqliteConnection())
            {
                connSource.ConnectionString = "Data Source=" + sourceDBPath;

                try
                {
                    connSource.Open();
                }
                catch (Exception e)
                {
                    CLog.Error("Error on import high score data. Can't open source database \"" + sourceDBPath + "\" (" + e.Message + ")");
                    return false;
                }
                #endregion open db

                using (var cmdSource = new SqliteCommand())
                {
                    cmdSource.Connection = connSource;

                    #region import table scores
                    cmdSource.CommandText = "SELECT SongId, PlayerName, Score, LineNr, Date, Medley, Duet, ShortSong, Difficulty FROM Scores";
                    var source = cmdSource.ExecuteReader();
                    if (source == null)
                    {
                        return false;
                    }

                    if (source.FieldCount == 0)
                    {
                        source.Close();
                        return true;
                    }

                    while (source.Read())
                    {
                        var songid = source.GetInt32(0);
                        var player = source.GetString(1);
                        var score = source.GetInt32(2);
                        var linenr = source.GetInt32(3);
                        var date = source.GetInt64(4);
                        var medley = source.GetInt32(5);
                        var duet = source.GetInt32(6);
                        var shortsong = source.GetInt32(7);
                        var diff = source.GetInt32(8);

                        string artist, title;
                        DateTime dateadded;
                        int numplayed;
                        if (_GetDataBaseSongInfos(songid, out artist, out title, out numplayed, out dateadded, sourceDBPath))
                        {
                            AddScore(player, score, linenr, date, medley, duet, shortsong, diff, artist, title, numplayed, _FilePath);
                        }
                    }
                    #endregion import table scores

                    source.Close();
                }
            }

            return true;
        }
    }
}