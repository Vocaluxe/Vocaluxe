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
using System.IO;
using Vocaluxe.Base;
#if WIN
using System.Data.SQLite;

#else
using Mono.Data.Sqlite;
#endif

namespace Vocaluxe.Lib.Database
{
    public class CDatabaseBase
    {
        protected readonly string _FilePath;
        protected SQLiteConnection _Connection;

        protected int _Version; //Version of the database (value of row "Value" in table "Version") or -1 if none

        public CDatabaseBase(string filePath)
        {
            _FilePath = filePath;
        }

        public virtual bool Init()
        {
            _Connection = new SQLiteConnection("Data Source=" + _FilePath);
            try
            {
                _Connection.Open();
            }
            catch (Exception)
            {
                _Connection.Dispose();
                _Connection = null;
                return false;
            }

            using (var command = new SQLiteCommand(_Connection))
            {
                command.CommandText = "SELECT Value FROM Version";

                SQLiteDataReader reader = null;

                try
                {
                    reader = command.ExecuteReader();
                }
                catch (Exception) {}

                if (reader == null || !reader.Read() || reader.FieldCount == 0)
                    _Version = -1;
                else
                    _Version = reader.GetInt32(0);

                if (reader != null)
                    reader.Dispose();
            }
            return true;
        }

        public virtual void Close()
        {
            if (_Connection != null)
            {
                _Connection.Dispose();
                _Connection = null;
            }
        }

        protected static long _UnixTimeToTicks(int unixTime)
        {
            var t70 = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            t70 = t70.AddSeconds(unixTime);
            return t70.Ticks;
        }

        protected static byte[] _GetBytes(SQLiteDataReader reader, int field = 0)
        {
            const int chunkSize = 2 * 1024;
            var buffer = new byte[chunkSize];
            long fieldOffset = 0;
            using (var stream = new MemoryStream())
            {
                int bytesRead;
                while ((bytesRead = (int)reader.GetBytes(field, fieldOffset, buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, bytesRead);
                    fieldOffset += bytesRead;
                }
                return stream.ToArray();
            }
        }
    }
}