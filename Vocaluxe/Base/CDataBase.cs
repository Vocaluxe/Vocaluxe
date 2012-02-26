using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;

using System.Data.SQLite;

using Vocaluxe.Lib.Draw;
using Vocaluxe.Lib.Song;
using System.Data;

namespace Vocaluxe.Base
{
    static class CDataBase
    {
        private static string _HighscoreFilePath;
        private static string _CoverFilePath;
        private static string _CreditsRessourcesFilePath;

        public static void Init()
        {
            _HighscoreFilePath = Path.Combine(System.Environment.CurrentDirectory, CSettings.sFileHighscoreDB);
            _CoverFilePath = Path.Combine(System.Environment.CurrentDirectory, CSettings.sFileCoverDB);
            _CreditsRessourcesFilePath = Path.Combine(System.Environment.CurrentDirectory, CSettings.sFileCreditsRessourcesDB);

            InitHighscoreDB();
            InitCoverDB();
            InitCreditsRessourcesDB();
        }

        #region Highscores
        public static int AddScore(SPlayer player)
        {
            int lastInsertID = -1;

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
            if (DataBaseSongID >= 0)
            {

                int Medley = 0;
                if (player.Medley)
                    Medley = 1;

                int Duet = 0;
                if (player.Duet)
                    Duet = 1;
                
                command.CommandText = "INSERT INTO Scores (SongID, PlayerName, Score, LineNr, Date, Medley, Duet, Difficulty) " +
                    "VALUES (@SongID, @PlayerName, @Score, @LineNr, @Date, @Medley, @Duet, @Difficulty)";
                command.Parameters.Add("@SongID", System.Data.DbType.Int32, 0).Value = DataBaseSongID;
                command.Parameters.Add("@PlayerName", System.Data.DbType.String, 0).Value = player.Name;
                command.Parameters.Add("@Score", System.Data.DbType.Int32, 0).Value = (int)Math.Round(player.Points);
                command.Parameters.Add("@LineNr", System.Data.DbType.Int32, 0).Value = (int)player.LineNr;
                command.Parameters.Add("@Date", System.Data.DbType.Int64, 0).Value = player.DateTicks;
                command.Parameters.Add("@Medley", System.Data.DbType.Int32, 0).Value = Medley;
                command.Parameters.Add("@Duet", System.Data.DbType.Int32, 0).Value = Duet;
                command.Parameters.Add("@Difficulty", System.Data.DbType.Int32, 0).Value = (int)player.Difficulty;
                command.ExecuteNonQuery();

                //Read last insert line
                command.CommandText = "SELECT id FROM Scores ORDER BY Date DESC LIMIT 0, 1";

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
                        lastInsertID = reader.GetInt32(0);
                    }

                    reader.Close();
                    reader.Dispose();
                }
            }

            command.Dispose();
            connection.Close();
            connection.Dispose();

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

            int DataBaseSongID = GetDataBaseSongID(player, command);
            if (DataBaseSongID >= 0)
            {
                command.CommandText = "SELECT PlayerName, Score, Date, Difficulty, LineNr, id FROM Scores " +
                    "WHERE [SongID] = @SongID AND [Medley] = @Medley AND [Duet] = @Duet " +
                    "ORDER BY [Score] DESC";
                command.Parameters.Add("@SongID", System.Data.DbType.Int32, 0).Value = DataBaseSongID;
                command.Parameters.Add("@Medley", System.Data.DbType.Int32, 0).Value = Medley;
                command.Parameters.Add("@Duet", System.Data.DbType.Int32, 0).Value = Duet;
                
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

            command.CommandText = "SELECT id FROM Songs WHERE [Title] = @title AND [Artist] = @artist";
            command.Parameters.Add("@title", System.Data.DbType.String, 0).Value = song.Title;
            command.Parameters.Add("@artist", System.Data.DbType.String, 0).Value = song.Artist;

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
                    "VALUES (@title, @artist, 0)";
                command.Parameters.Add("@title", System.Data.DbType.String, 0).Value = song.Title;
                command.Parameters.Add("@artist", System.Data.DbType.String, 0).Value = song.Artist;
                command.ExecuteNonQuery();

                command.CommandText = "SELECT id FROM Songs WHERE [Title] = @title AND [Artist] = @artist";
                command.Parameters.Add("@title", System.Data.DbType.String, 0).Value = song.Title;
                command.Parameters.Add("@artist", System.Data.DbType.String, 0).Value = song.Artist;

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

        private static bool InitHighscoreDB()
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
                CreateHighscoreDB();
            }
            else if (reader.FieldCount == 0)
            {
                // create new database/tables
                CreateHighscoreDB();
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

            //Check for USDX 1.1 DB
            if (version == 1)
                ConvertFrom110();
            //Check for USDX 1.01 or CMD Mod DB
            else if (version == 0 && scoresTableExists)
                ConvertFrom101();

            command.Dispose();

            connection.Close();
            connection.Dispose();

            return true;
        }

        private static void CreateHighscoreDB()
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
                "Medley INTEGER NOT NULL, Duet INTEGER NOT NULL, Difficulty INTEGER NOT NULL);";
            command.ExecuteNonQuery();

            command.Dispose();
            connection.Close();
            connection.Dispose();

            /*
            // Auslesen des zuletzt eingefügten Datensatzes.
            command.CommandText = "SELECT id, name FROM beispiel ORDER BY id DESC LIMIT 0, 1";

            while (reader.Read())
            {
                Console.WriteLine("Dies ist der {0}. eingefügte Datensatz mit dem Wert: \"{1}\"", reader[0].ToString(), reader[1].ToString());
            }
            */
        }

        /// <summary>
        /// Converts a USDX 1.1 database into the Vocaluxe format
        /// </summary>
        /// <returns>True if succeeded</returns>
        private static bool ConvertFrom110()
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
                return false;
            }

            command = new SQLiteCommand(connection);

            //The USDX database has no column for LineNr, Medley and Duet so just fill 0 in there
            command.CommandText = "INSERT INTO Scores (SongID, PlayerName, Score, LineNr, Date, Medley, Duet, Difficulty) SELECT SongID, Player, Score, '0', Date, '0', '0', Difficulty from US_Scores";
            command.ExecuteNonQuery();

            command.CommandText = "INSERT INTO Songs SELECT ID, Artist, Title, TimesPlayed from US_Songs";
            command.ExecuteNonQuery();

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
        /// <returns>True if succeeded</returns>
        private static bool ConvertFrom101()
        {
            SQLiteConnection connection = new SQLiteConnection();
            connection.ConnectionString = "Data Source=" + _HighscoreFilePath;
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
            reader.Read();

            bool dateExists = false;

            //Check for column Date
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i) == "Date")
                {
                    dateExists = true;
                    break;
                }
            }

            reader.Close();

            //This is a USDX 1.01 DB
            if(!dateExists)
                command.CommandText = "INSERT INTO Scores (SongID, PlayerName, Score, LineNr, Date, Medley, Duet, Difficulty) SELECT SongID, Player, Score, '0', '0', '0', '0', Difficulty from US_Scores";
            else // This is a CMD 1.01 DB
                command.CommandText = "INSERT INTO Scores (SongID, PlayerName, Score, LineNr, Date, Medley, Duet, Difficulty) SELECT SongID, Player, Score, '0', Date, '0', '0', Difficulty from US_Scores";
            command.ExecuteNonQuery();

            command.CommandText = "INSERT INTO Songs SELECT ID, Artist, Title, TimesPlayed from US_Songs";
            command.ExecuteNonQuery();

            CLog.LogError("The database has been converted but all non ASCII characters got lost in this process");

            /* Unfortunately all SQLite Wrappers do convert data to UTF8 when interacting with the database, so converting cant be done in .Net,
             * we might have to write a library in C++ which can convert data or just write a simple program to which converts the tables.
             */
            
            //Now loop through all tables and convert from CP1252 to UTF8
            //command.CommandText = "SELECT ID, Artist, Title from US_Songs";

            //Create a DataTable containing the results
            //DataTable results = new DataTable();
            //SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
            //adapter.Fill(results);

            //foreach (DataRow row in results.Rows)
            //{
            //    //FIXME: Convert Latin1(CP1252) to UTF8
            //    int id = Convert.ToInt32(row.ItemArray[0].ToString());
            //    string artist = row.ItemArray[1].ToString();
            //    //artist = Encoding.Unicode.GetString(Encoding.Convert(Encoding.GetEncoding(1252), Encoding.Unicode, Encoding.GetEncoding(1252).GetBytes(row.ItemArray[1].ToString())));
            //    string title = row.ItemArray[2].ToString();

            //    command.CommandText = "UPDATE Songs SET Artist ='" + artist + "', Title = '" + title + "'WHERE ID = " + id;
            //    command.ExecuteNonQuery();
            //}
            
            //Delete old tables after conversion
            command.CommandText = "DROP TABLE US_Scores;";
            command.ExecuteNonQuery();

            command.CommandText = "DROP TABLE US_Songs;";
            command.ExecuteNonQuery();

            //results.Dispose();
            //adapter.Dispose();
            
            reader.Dispose();
            command.Dispose();
            connection.Close();
            connection.Dispose();

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
                    tex = CDraw.AddTexture(w, h, ref data);
                }
            }
            else
            {
                if (reader != null)
                    reader.Close();

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
                    connection.Close();
                    connection.Dispose();

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
                tex = CDraw.AddTexture(bmp);
                byte[] data = new byte[w * h * 4];

                BitmapData bmp_data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Marshal.Copy(bmp_data.Scan0, data, 0, w*h*4);
                bmp.UnlockBits(bmp_data);
                bmp.Dispose();

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
            connection.Close();
            connection.Dispose();

            return result;
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
                "ImageID INTEGER NOT NULL, Data BLOB NOT NULL);";
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
    }
}
