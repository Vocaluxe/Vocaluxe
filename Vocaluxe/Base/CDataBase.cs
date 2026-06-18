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
using System.Drawing;
using System.IO;
using Vocaluxe.Lib.Database;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Log;

namespace Vocaluxe.Base
{
    static class CDataBase
    {
        private static CHighscoreDB _HighscoreDB;
        private static CCoverDB _CoverDB;

        public static bool Init()
        {
            _HighscoreDB = new CHighscoreDB(CConfig.FileHighscoreDB);
            _CoverDB = new CCoverDB(Path.Combine(CSettings.DataFolder, CSettings.FileNameCoverDB));

            if (!_HighscoreDB.Init())
            {
                CLog.Fatal("Error initializing Highscore-DB");
                return false;
            }

            if (!_CoverDB.Init())
            {
                CLog.Fatal("Error initializing Cover-DB");
                return false;
            }

            return true;
        }

        public static void Close()
        {
            if (_HighscoreDB != null)
            {
                _HighscoreDB.Close();
                _HighscoreDB = null;
            }

            if (_CoverDB != null)
            {
                _CoverDB.Close();
                _CoverDB = null;
            }
        }

        public static bool GetDataBaseSongInfos(string artist, string title, out int numPlayed, out DateTime dateAdded, out int highscoreId)
        {
            if (_HighscoreDB == null)
            {
                numPlayed = 0;
                dateAdded = new DateTime();
                highscoreId = 0;
                return false;
            }

            return _HighscoreDB.GetDataBaseSongInfos(artist, title, out numPlayed, out dateAdded, out highscoreId);
        }

        public static List<SDBScoreEntry> LoadScore(int songId, EGameMode gameMode, EHighscoreStyle style)
        {
            return _HighscoreDB?.LoadScore(songId, gameMode, style);
        }

        public static int AddScore(SPlayer player)
        {
            return _HighscoreDB?.AddScore(player) ?? -1;
        }

        public static void IncreaseSongCounter(int dataBaseSongId)
        {
            _HighscoreDB?.IncreaseSongCounter(dataBaseSongId);
        }

        public static CTextureRef GetCover(string coverId)
        {
            return _CoverDB?.GetCover(coverId);
        }

        public static bool EnqueueCoverToTransaction(string coverId, Size size, byte[] data)
        {
            return _CoverDB != null && _CoverDB.EnqueueCoverToTransaction(coverId, size, data);
        }

        public static void CommitCovers()
        {
            _CoverDB?.CommitCovers();
        }
    }
}