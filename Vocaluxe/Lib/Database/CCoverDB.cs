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
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Database
{
    public class CCoverDB : CDatabaseBase
    {
        private const long _JpegQuality = 90;
        private static readonly ImageCodecInfo _JpegCodec = ImageCodecInfo.GetImageEncoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

        private SqliteTransaction _TransactionCover;

        public CCoverDB(string filePath) : base(filePath) { }

        public override bool Init()
        {
            lock (_Mutex)
            {
                if (!base.Init())
                {
                    return false;
                }

                if (_Version < 0)
                {
                    return _CreateCoverDB();
                }

                if (_Version != CSettings.DatabaseCoverVersion)
                {
                    CLog.Information("Cover DB has version {OldVersion}, expected {NewVersion}. Recreating it.",
                        CLog.Params(_Version, CSettings.DatabaseCoverVersion));
                    return _RecreateCoverDB();
                }
            }

            return true;
        }

        public override void Close()
        {
            //Do commit and close atomicly otherwhise we may loose changes
            lock (_Mutex)
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
                CLog.Error("Can't find File: " + coverPath);
                return false;
            }

            var fileInfo = new FileInfo(coverPath);
            var fileSize = fileInfo.Length;
            var lastWrite = fileInfo.LastWriteTimeUtc.Ticks;

            var cachedData = _ReadCachedCover(coverPath, fileSize, lastWrite, maxSize);
            if (cachedData != null)
            {
                try
                {
                    using (var stream = new MemoryStream(cachedData))
                    using (var source = new System.Drawing.Bitmap(stream))
                        tex = CDraw.EnqueueTexture(_FitToMaxSize(source, maxSize));
                }
                catch (Exception e)
                {
                    CLog.Error("Can't decode cover from database: " + e);
                    return false;
                }

                return true;
            }

            // At this point we do not have a matching entry in the CoverDB
            // (either no Data found and deleted or nothing at all)
            byte[] data;
            int width;
            int height;

            try
            {
                using (var source = CHelper.LoadBitmap(coverPath))
                {
                    if (source == null)
                        return false;

                    var bitmap = _FitToMaxSize(source, maxSize);
                    try
                    {
                        width = bitmap.Width;
                        height = bitmap.Height;
                        data = source.Width <= maxSize && source.Height <= maxSize
                                   ? File.ReadAllBytes(coverPath)
                                   : _EncodeCover(bitmap, source.PixelFormat);
                    }
                    catch
                    {
                        bitmap.Dispose();
                        throw;
                    }

                    tex = CDraw.EnqueueTexture(bitmap);
                }
            }
            catch (Exception e)
            {
                CLog.Error("Error loading cover: " + coverPath + " - " + e);
                return false;
            }

            _StoreCover(coverPath, fileSize, lastWrite, maxSize, width, height, data);
            return true;
        }

        private static System.Drawing.Bitmap _FitToMaxSize(System.Drawing.Bitmap source, int maxSize)
        {
            var sourceSize = source.GetSize();
            if (sourceSize.Width <= maxSize && sourceSize.Height <= maxSize)
                return new System.Drawing.Bitmap(source);

            var targetSize = CHelper.FitInBounds(
                new SRectF(0, 0, maxSize, maxSize, 0),
                (float)sourceSize.Width / sourceSize.Height,
                EAspect.LetterBox
            ).SizeI;

            return source.Resize(targetSize) ?? throw new InvalidOperationException("Resizing the cover failed");
        }

        private static byte[] _EncodeCover(System.Drawing.Bitmap bitmap, PixelFormat sourceFormat)
        {
            using (var stream = new MemoryStream())
            {
                if (System.Drawing.Image.IsAlphaPixelFormat(sourceFormat))
                {
                    bitmap.Save(stream, ImageFormat.Png);
                }
                else
                {
                    using (var parameters = new EncoderParameters(1))
                    {
                        parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, _JpegQuality);
                        bitmap.Save(stream, _JpegCodec, parameters);
                    }
                }

                return stream.ToArray();
            }
        }

        private byte[] _ReadCachedCover(string coverPath, long fileSize, long lastWrite, int maxSize)
        {
            lock (_Mutex)
            {
                //Double check here because we may have just closed our connection
                if (_Connection == null)
                    return null;

                using (var command = new SqliteCommand())
                {
                    command.Connection = _Connection;
                    // If we have an open transaction on this connection, all commands must use it.
                    if (_TransactionCover != null)
                        command.Transaction = _TransactionCover;

                    command.CommandText = "SELECT id, FileSize, LastWrite, MaxSize FROM Cover WHERE [Path] = @path";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@path", coverPath);

                    int id;
                    bool isUpToDate;
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                            return null;

                        id = reader.GetInt32(0);
                        isUpToDate = reader.GetInt64(1) == fileSize && reader.GetInt64(2) == lastWrite && reader.GetInt32(3) == maxSize;
                    }

                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@id", id);

                    if (isUpToDate)
                    {
                        command.CommandText = "SELECT Data FROM CoverData WHERE CoverId = @id";
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                                return _GetBytes(reader);
                        }
                    }

                    command.CommandText = "DELETE FROM CoverData WHERE CoverId = @id; DELETE FROM Cover WHERE id = @id;";
                    command.ExecuteNonQuery();

                    return null;
                }
            }
        }

        private void _StoreCover(string coverPath, long fileSize, long lastWrite, int maxSize, int width, int height, byte[] data)
        {
            lock (_Mutex)
            {
                // Double check here because we may have just closed our connection
                if (_Connection == null)
                    return;

                if (_TransactionCover == null)
                    _TransactionCover = _Connection.BeginTransaction();

                using (var command = new SqliteCommand())
                {
                    command.Connection = _Connection;
                    command.Transaction = _TransactionCover;
                    command.CommandText = "INSERT OR IGNORE INTO Cover (Path, width, height, FileSize, LastWrite, MaxSize) " +
                                          "VALUES (@path, @w, @h, @size, @lastWrite, @maxSize)";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@w", width);
                    command.Parameters.AddWithValue("@h", height);
                    command.Parameters.AddWithValue("@path", coverPath);
                    command.Parameters.AddWithValue("@size", fileSize);
                    command.Parameters.AddWithValue("@lastWrite", lastWrite);
                    command.Parameters.AddWithValue("@maxSize", maxSize);
                    if (command.ExecuteNonQuery() == 0)
                        return;

                    command.CommandText = "SELECT last_insert_rowid()";
                    command.Parameters.Clear();
                    var id = Convert.ToInt32(command.ExecuteScalar());

                    command.CommandText = "INSERT INTO CoverData (CoverId, Data) VALUES (@id, @data)";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@data", data);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void CommitCovers()
        {
            lock (_Mutex)
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
            {
                return;
            }

            _TransactionCover.Commit();
            _TransactionCover.Dispose();
            _TransactionCover = null;
        }

        private bool _CreateCoverDB()
        {
            try
            {
                using (var command = new SqliteCommand())
                {
                    command.Connection = _Connection;
                    command.CommandText = "CREATE TABLE IF NOT EXISTS Version (Value INTEGER NOT NULL);";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO Version (Value) VALUES(@Value)";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@Value", CSettings.DatabaseCoverVersion);
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE TABLE IF NOT EXISTS Cover ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                          "Path TEXT NOT NULL, width INTEGER NOT NULL, height INTEGER NOT NULL, " +
                                          "FileSize INTEGER NOT NULL, LastWrite INTEGER NOT NULL, MaxSize INTEGER NOT NULL);";
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE TABLE IF NOT EXISTS CoverData ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                          "CoverId INTEGER NOT NULL, Data BLOB NOT NULL);";
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS IX_Cover_Path ON Cover (Path);";
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS IX_CoverData_CoverId ON CoverData (CoverId);";
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                CLog.Error("Error creating Cover DB " + e);
                return false;
            }

            return true;
        }

        private bool _RecreateCoverDB()
        {
            try
            {
                using (var command = new SqliteCommand())
                {
                    command.Connection = _Connection;
                    command.CommandText = "DROP TABLE IF EXISTS CoverData; DROP TABLE IF EXISTS Cover; DROP TABLE IF EXISTS Version;";
                    command.ExecuteNonQuery();
                    command.CommandText = "VACUUM;";
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                CLog.Error("Error dropping old Cover DB " + e);
                return false;
            }

            return _CreateCoverDB();
        }
    }
}
