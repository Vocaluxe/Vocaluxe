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
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Vocaluxe.Base;
using VocaluxeLib.Draw;
using VocaluxeLib.Log;
#if WIN
using System.Data.SQLite;

#else
using Mono.Data.Sqlite;
using SQLiteCommand = Mono.Data.Sqlite.SqliteCommand;
using SQLiteConnection = Mono.Data.Sqlite.SqliteConnection;
using SQLiteDataReader = Mono.Data.Sqlite.SqliteDataReader;
using SQLiteTransaction = Mono.Data.Sqlite.SqliteTransaction;
#endif

namespace Vocaluxe.Lib.Database
{
    public class CResourceDB : CDatabaseBase
    {
#if DEBUG
        private readonly string[] _FilesV1 = new string[]
            {
                "Logo_voc.png", "PerfectNoteStar.png", "redDot.png", "blueDot.png", "brunzel.png", "Darkice.png", "flokuep.png", "flamefire.png", "bohning.png", "mesand.png",
                "babene03.png", "lukeIam.png"
            };
#endif
        public CResourceDB(string filePath) : base(filePath) {}

        public override bool Init()
        {
            if (!base.Init())
                return false;
            if (_Version < 0)
            {
                CLog.Error("Can't find Ressource-DB!");
                return false;
            }

            if (_Version < CSettings.DatabaseCreditsRessourcesVersion)
            {
#if DEBUG
                return _CreateDB() && _UpdateV1();
#else
                CLog.Error("Upgrading Ressource-DB not possible");
                return false;
#endif
            }

            return true;
        }

        public bool GetCreditsRessource(string fileName, ref CTextureRef tex)
        {
            if (_Connection == null)
                return false;
            bool result = false;

            using (var command = new SQLiteCommand(_Connection))
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
                    }
                }

                if (reader != null)
                    reader.Dispose();
            }

            return result;
        }

#if DEBUG
        //Update from V0 (empty) to V1
        private bool _UpdateV1()
        {
            if (_Connection == null || _Version >= 1)
                return false;
            using (SQLiteTransaction transaction = _Connection.BeginTransaction())
            {
                try
                {
                    foreach (string file in _FilesV1)
                    {
                        string filePath = Path.Combine(CSettings.ProgramFolder, file);
                        if (!_AddImageToCreditsDB(filePath, transaction))
                        {
                            transaction.Rollback();
                            return false;
                        }
                    }
                    using (SQLiteCommand command = new SQLiteCommand(_Connection))
                    {
                        command.Transaction = transaction;
                        command.CommandText = "Update Version SET Value=@Value)";
                        command.Parameters.Add("@Value", DbType.Int32).Value = 1;
                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    return false;
                }
            }
            return true;
        }

        //If you want to add an image to db, call this method!
        private bool _AddImageToCreditsDB(string imagePath, SQLiteTransaction transaction)
        {
            if (_Connection == null || !File.Exists(imagePath))
                return false;

            Bitmap origin;
            try
            {
                origin = new Bitmap(imagePath);
            }
            catch (Exception)
            {
                CLog.Error("Error loading image: " + imagePath);
                return false;
            }
            try
            {
                int w = origin.Width;
                int h = origin.Height;
                byte[] data = new byte[w * h * 4];

                BitmapData bmpData = origin.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(bmpData.Scan0, data, 0, w * h * 4);
                origin.UnlockBits(bmpData);

                using (SQLiteCommand command = new SQLiteCommand(_Connection))
                {
                    command.Transaction = transaction;
                    command.CommandText = "INSERT INTO Images (Path, width, height) VALUES (@path, @w, @h)";
                    command.Parameters.Add("@path", DbType.String).Value = Path.GetFileName(imagePath);
                    command.Parameters.Add("@w", DbType.Int32).Value = w;
                    command.Parameters.Add("@h", DbType.Int32).Value = h;
                    command.ExecuteNonQuery();
                }

                int id = -1;
                using (SQLiteCommand command = new SQLiteCommand(_Connection))
                {
                    command.Transaction = transaction;
                    command.CommandText = "SELECT id FROM Images WHERE [Path] = @path";
                    command.Parameters.Add("@path", DbType.String, 0).Value = Path.GetFileName(imagePath);
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader != null)
                        {
                            reader.Read();
                            id = reader.GetInt32(0);
                        }
                    }
                }
                if (id < 0)
                    return false;
                using (SQLiteCommand command = new SQLiteCommand(_Connection))
                {
                    command.Transaction = transaction;
                    command.CommandText = "INSERT INTO ImageData (ImageID, Data) VALUES (@id, @data)";
                    command.Parameters.Add("@id", DbType.Int32).Value = id;
                    command.Parameters.Add("@data", DbType.Binary).Value = data;
                    command.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                origin.Dispose();
            }
        }

        private bool _CreateDB()
        {
            if (_Connection == null)
                return false;

            try
            {
                using (SQLiteCommand command = new SQLiteCommand(_Connection))
                {
                    command.CommandText = "CREATE TABLE IF NOT EXISTS Version (Value INTEGER NOT NULL);";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO Version (Value) VALUES(0)";
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE TABLE IF NOT EXISTS Images ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                          "Path TEXT NOT NULL, width INTEGER NOT NULL, height INTEGER NOT NULL);";
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE TABLE IF NOT EXISTS ImageData ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                          "ImageID INTEGER NOT NULL, Data BLOB NOT NULL);";
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                CLog.Error("Error creating Ressource DB " + e);
                return false;
            }
            return true;
        }
#endif
    }
}