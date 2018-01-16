using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace VocaluxeLib.Log.Rolling
{
    static class LogFileRoller
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
                    catch (Exception)
                    {
                        // Cant log anything here as the log isn't initialized yet
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
                catch (Exception)
                {
                    // Cant log anything here as the log isn't initialized yet
                }
            }
        }

        private static void _DeleteOldLogs(string mainLogFile, int numLogsToKeep)
        {
            Regex r = new Regex($"{Path.GetDirectoryName(mainLogFile)}\\\\{Path.GetFileNameWithoutExtension(mainLogFile)}_([0-9]+){Path.GetExtension(mainLogFile)}", RegexOptions.IgnoreCase);
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
                        catch (Exception)
                        {
                            // Cant log anything here as the log isn't initialized yet
                        }

                    }
                }
            }
        }

        private static string _GenerateRollLogFileName(string mainLogFile, int number)
        {
            return $"{Path.GetDirectoryName(mainLogFile)}\\{Path.GetFileNameWithoutExtension(mainLogFile)}_{number}{Path.GetExtension(mainLogFile)}";
        }
    }
}
