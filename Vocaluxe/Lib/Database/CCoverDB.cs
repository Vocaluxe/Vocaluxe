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
#if WIN
using System.Data.SQLite;

#else
using Mono.Data.Sqlite;
#endif

namespace Vocaluxe.Lib.Database
{
    public class CCoverDB : CDatabaseBase
    {
        //You have to lock all actions using cover connection or transaction, otherwhise order is not guaranted
        private readonly object _CoverMutex = new object();
        private SQLiteTransaction _TransactionCover;

        public CCoverDB(string filePath) : base(filePath) {}

        public override bool Init()
        {
            if (!base.Init())
                return false;

            if (_Version < 0)
                return _CreateCoverDB();
            if (_Version < CSettings.DatabaseCoverVersion)
                throw new NotImplementedException("Upgrading of cover DB not implemented");
            return true;
        }

        public override void Close()
        {
            //Do commit and close atomicly otherwhise we may loose changes
            lock (_CoverMutex)
            {
                _CommitCovers();

                base.Close();
            }
        }

        public bool GetCover(string coverPath, ref CTextureRef tex, int maxSize)
        {
            if (_Connection == null)
                return false;
            if (!File.Exists(coverPath))
            {
                CLog.LogError("Can't find File: " + coverPath);
                return false;
            }

            lock (_CoverMutex)
            {
                //Double check here because we may have just closed our connection
                if (_Connection == null)
                    return false;
                using (var command = new SQLiteCommand(_Connection))
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
                            _TransactionCover = _Connection.BeginTransaction();

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

                            using (var bmp = new Bitmap(w, h))
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
            }
            return false;
        }

        public void CommitCovers()
        {
            lock (_CoverMutex)
            {
                _CommitCovers();
            }
        }

        /// <summary>
        ///     You have to hold the CoverMutex when calling this!
        /// </summary>
        private void _CommitCovers()
        {
            if (_TransactionCover == null)
                return;
            _TransactionCover.Commit();
            _TransactionCover.Dispose();
            _TransactionCover = null;
        }

        private bool _CreateCoverDB()
        {
            try
            {
                using (var command = new SQLiteCommand(_Connection))
                {
                    command.CommandText = "CREATE TABLE IF NOT EXISTS Version (Value INTEGER NOT NULL);";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO Version (Value) VALUES(@Value)";
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
            catch (Exception e)
            {
                CLog.LogError("Error creating Cover DB " + e);
                return false;
            }
            return true;
        }
    }
}