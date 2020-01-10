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
using System.Text.RegularExpressions;

namespace VocaluxeLib.Log.Rolling
{
    static class CLogFileRoller
    {
        public static void RollLogs(string mainLogFile, int numLogsToKeep)
        {
            _DeleteOldLogs(mainLogFile, numLogsToKeep);

            string currentSourceFile = _GenerateRollLogFileName(mainLogFile, numLogsToKeep);


            for (var i = numLogsToKeep; i > 1; i--)
            {
                string currentTargetFile = currentSourceFile;
                currentSourceFile = _GenerateRollLogFileName(mainLogFile, i - 1);

                if (File.Exists(currentSourceFile))
                {
                    try
                    {
                        File.Move(currentSourceFile, currentTargetFile);
                    }
                    catch (Exception e)
                    {
                        // Cant log anything here as the log isn't initialized yet
#if DEBUG
                        Console.WriteLine($"Error moving old log file: {e.Message}");
#endif
                    }
                }
            }

            if (File.Exists(mainLogFile))
            {
                try
                {
                    if (numLogsToKeep > 0)
                        File.Move(mainLogFile, currentSourceFile);
                    else
                        File.Delete(mainLogFile);
                }
                catch (Exception e)
                {
                    // Cant log anything here as the log isn't initialized yet
#if DEBUG
                    Console.WriteLine($"Error moving old main log file: {e.Message}");
#endif
                }
            }
        }

        private static void _DeleteOldLogs(string mainLogFile, int numLogsToKeep)
        {
            Regex r = new Regex($"{Regex.Escape(Path.GetDirectoryName(mainLogFile)??"")}\\{Path.DirectorySeparatorChar}{Regex.Escape(Path.GetFileNameWithoutExtension(mainLogFile)??"")}_([0-9]+){Path.GetExtension(mainLogFile)}", RegexOptions.IgnoreCase);
            IEnumerable<string> logFiles = Directory.EnumerateFiles($"{Path.GetDirectoryName(mainLogFile)}", $"{Path.GetFileNameWithoutExtension(mainLogFile)}*{Path.GetExtension(mainLogFile)}");
            foreach (string file in logFiles)
            {
                Match m = r.Match(file);
                if (m.Success)
                {
                    int logNum = -1;
                    Int32.TryParse(m.Groups[1].Value, out logNum);

                    if (logNum < 0 || logNum > numLogsToKeep - 1)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception e)
                        {
                            // Cant log anything here as the log isn't initialized yet
#if DEBUG
                            Console.WriteLine($"Error deleting old log file: {e.Message}");
#endif
                        }

                    }
                }
            }
        }

        private static string _GenerateRollLogFileName(string mainLogFile, int number)
        {
            return $"{ Path.GetDirectoryName(mainLogFile) }{ Path.DirectorySeparatorChar }{ Path.GetFileNameWithoutExtension(mainLogFile) }_{ number }{ Path.GetExtension(mainLogFile) }";
        }
    }
}
