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

using Microsoft.Data.Sqlite;
using System;
using System.Drawing;
using Vocaluxe.Base;
using VocaluxeLib.Draw;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Database
{
    public class CCoverDB : CDatabaseBase
    {
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

                if (_Version < CSettings.DatabaseCoverVersion)
                {
                    throw new NotImplementedException("Upgrading of cover DB not implemented");
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

        public bool EnqueueCoverToTransaction(string coverId, Size size, byte[] data)
        {
            lock (_Mutex)
            {
                //Double check here because we may have just closed our connection
                if (_Connection == null)
                {
                    return false;
                }

                _TransactionCover ??= _Connection.BeginTransaction();

                using var command = new SqliteCommand();
                command.Connection = _Connection;
                command.Transaction = _TransactionCover;
                command.CommandText = "INSERT INTO Cover (Path, width, height) VALUES (@path, @w, @h)";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@w", size.Width);
                command.Parameters.AddWithValue("@h", size.Height);
                command.Parameters.AddWithValue("@path", coverId);
                command.ExecuteNonQuery();

                command.CommandText = "SELECT id FROM Cover WHERE [Path] = @path";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@path", coverId);
                var reader = command.ExecuteReader();

                if (!reader.Read())
                {
                    return false;
                }

                var id = reader.GetInt32(0);
                reader.Dispose();
                command.CommandText = "INSERT INTO CoverData (CoverId, Data) VALUES (@id, @data)";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@data", data);
                command.ExecuteNonQuery();
                return true;
            }
        }

        public CTextureRef GetCover(string coverId)
        {
            if (_Connection == null)
            {
                CLog.Error("DB cover connection is not set");
                return null;
            }

            lock (_Mutex)
            {
                //Double check here because we may have just closed our connection
                if (_Connection == null)
                {
                    CLog.Error("DB cover connection is not set");
                    return null;
                }

                using var command = new SqliteCommand();
                command.Connection = _Connection;
                // If we have an open transaction on this connection, all commands must use it.
                if (_TransactionCover != null)
                {
                    command.Transaction = _TransactionCover;
                }

                command.CommandText = "SELECT id, width, height FROM Cover WHERE [Path] = @path";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@path", coverId);

                var reader = command.ExecuteReader();
                try
                {
                    if (reader.Read())
                    {
                        var id = reader.GetInt32(0);
                        var w = reader.GetInt32(1);
                        var h = reader.GetInt32(2);
                        reader.Dispose();

                        command.CommandText = "SELECT Data FROM CoverData WHERE CoverId = @id";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@id", id);
                        reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            var coverData = _GetBytes(reader);
                            return CDraw.EnqueueTexture(w, h, coverData);
                        }

                        command.CommandText = "DELETE FROM Cover WHERE id = @id";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@id", id);
                        command.ExecuteNonQuery();
                    }
                }
                finally
                {
                    reader.Dispose();
                }
            }

            return null;
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
                                          "Path TEXT NOT NULL, width INTEGER NOT NULL, height INTEGER NOT NULL);";
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE TABLE IF NOT EXISTS CoverData ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                          "CoverId INTEGER NOT NULL, Data BLOB NOT NULL);";
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
    }
}