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
using System.Text;
using System.Text.RegularExpressions;
using Serilog;
using VocaluxeLib.Log.Enricher;
using VocaluxeLib.Log.Rolling;
using VocaluxeLib.Log.Serilog;

namespace VocaluxeLib.Log
{
    public static partial class CLog
    {
        private static readonly StringBuilder _MainLogStringBuilder = new StringBuilder();
        private static readonly StringWriter _MainLogStringWriter = new StringWriter(_MainLogStringBuilder);

        private static ILogger _MainLog = new CSilentLogger();
        private static ILogger _SongLog = new CSilentLogger();
        private static string _LogFolder;
        private static string _CrashMarkerFilePath;
        private static ShowReporterDelegate _ShowReporterFunc;
        private static string _CurrentVersion;

        private const string _MainLogTemplate = "[{TimeStampFromStart}] [{Level}] {Message}{NewLine}{Properties}{NewLine}{Exception}";
        private const string _SongLogTemplate = "{Message}{NewLine}Additional info:{Properties}{NewLine}{Exception}";

        /// <summary>
        /// Initialize the logging framework.
        /// </summary>
        /// <param name="logFolder">The folder where to write the log files.</param>
        /// <param name="fileNameMainLog">The name of the main log.</param>
        /// <param name="fileNameSongInfoLog">The name of the log for song problems.</param>
        /// <param name="fileNameCrashMarker">The name of the file which is used as crash marker.</param>
        /// <param name="currentVersion">The current version tag as it is displayed in the main menu.</param>
        /// <param name="showReporterFunc">Delegate to the function which should be called if the reporter have to been shown.</param>
        /// <param name="logLevel">The log level for log messages.</param>
        public static void Init(string logFolder, string fileNameMainLog, string fileNameSongInfoLog, string fileNameCrashMarker, string currentVersion, ShowReporterDelegate showReporterFunc, ELogLevel logLevel)
        {
            _LogFolder = logFolder;
            _ShowReporterFunc = showReporterFunc;
            _CurrentVersion = currentVersion;

            _CrashMarkerFilePath = Path.Combine(_LogFolder, fileNameCrashMarker);

            // Creates the log directory if it does not exist
            if (!Directory.Exists(_LogFolder))
                Directory.CreateDirectory(_LogFolder);

            var mainLogFilePath = Path.Combine(_LogFolder, fileNameMainLog);
            var songLogFilePath = Path.Combine(_LogFolder, fileNameSongInfoLog);

            // Check if crash marker file
            if (File.Exists(_CrashMarkerFilePath))
            {
                // There was a crash in the last run -> check version tag of the crashed application instance
                string versionTag;
                using (StreamReader reader = new StreamReader(_CrashMarkerFilePath, Encoding.UTF8))
                {
                    versionTag = (reader.ReadLine() ?? "").Trim();
                }

                // Delete the old marker
                File.Delete(_CrashMarkerFilePath);

#if !DEBUG
                if (_CurrentVersion == versionTag && File.Exists(mainLogFilePath))
                {
                    string logContent = File.ReadAllText(mainLogFilePath, Encoding.UTF8);
                    _ShowReporterFunc(crash:true,
                        showContinue:true,
                        vocaluxeVersionTag:versionTag,
                        log:logContent,
                        lastError:"Vocaluxe crashed while the last execution.");
                }
#endif
            }
            
            // Write new marker
            File.WriteAllText(_CrashMarkerFilePath, _CurrentVersion, Encoding.UTF8);

            CLogFileRoller.RollLogs(mainLogFilePath, 2);
            CLogFileRoller.RollLogs(songLogFilePath, 2);

            _MainLog = new LoggerConfiguration()
                .MinimumLevel.Is(logLevel.ToSerilogLogLevel())
                .Enrich.WithThreadId()
                .Enrich.FromLogContext()
                .Enrich.WithTimeStampFromStart()
                .WriteTo.TextWriter(_MainLogStringWriter,
                    outputTemplate: _MainLogTemplate)
                // Json can be activated by adding "new CompactJsonFormatter()" as first argument
                .WriteTo.File(mainLogFilePath,
                    flushToDiskInterval: TimeSpan.FromSeconds(30),
                    outputTemplate: _MainLogTemplate)
#if DEBUG
                .WriteTo.Console(outputTemplate: _MainLogTemplate)
#endif
                .CreateLogger();

            _SongLog = new LoggerConfiguration()
                .MinimumLevel.Is(logLevel.ToSerilogLogLevel())
                .WriteTo.File(songLogFilePath, 
                    flushToDiskInterval: TimeSpan.FromSeconds(60),
                    outputTemplate: _SongLogTemplate)
#if DEBUG
                .WriteTo.Console(outputTemplate: "[SongInfo] " + _SongLogTemplate)
#endif
                .CreateLogger();

            // Adding first line to log with information about this run
            Information("Starting to log", 
                Params( new { Version = _CurrentVersion},
                    new { StartDate = DateTime.Now},
                    new { Id = Guid.NewGuid() } ) );
        }

        /// <summary>
        /// Close all log files and the close the logging framework.
        /// </summary>
        public static void Close()
        {
            if (!(_MainLog is CSilentLogger))
            {
                ILogger loggerToDispose = _MainLog;

                _MainLog = new CSilentLogger();
                (loggerToDispose as IDisposable)?.Dispose();
            }

            if (!(_SongLog is CSilentLogger))
            {
                ILogger loggerToDispose = _SongLog;

                _SongLog = new CSilentLogger();
                (loggerToDispose as IDisposable)?.Dispose();
            }

            // Delete the crash marker
            File.Delete(_CrashMarkerFilePath);

        }

        /// <summary>
        /// Show the report assistant.
        /// </summary>
        /// <param name="messageTemplate">Last error message template.</param>
        /// <param name="propertyValues">Data to fill the last error message template.</param>
        /// <param name="crash">True if the crash version of the message should be shown.</param>
        /// <param name="showContinue">True if the assistant should show a continue butoon, exit button otherwise.</param>
        public static void ShowLogAssistant(string messageTemplate, object[] propertyValues, bool crash = false, bool showContinue = true)
        {
            // Flush the _MainLogStringWriter to get the latest entries to _MainLogStringBuilder
            _MainLogStringWriter.Flush();
            // Show the Reporter
            _ShowReporterFunc(crash: crash, showContinue: showContinue, vocaluxeVersionTag: _CurrentVersion, log: _MainLogStringBuilder.ToString(), lastError: _FormatMessageTemplate(messageTemplate, propertyValues));

            // Delete the crash marker (we do not want to show this error again on the next restart)
            if (!showContinue)
            {
                File.Delete(_CrashMarkerFilePath);
            }
        }

        /// <summary>
        /// Simple version of inserting values into a message template (have not the same result as the original)
        /// </summary>
        /// <param name="template">The template to fill in the data.</param>
        /// <param name="propertyValues">The values to fill in.</param>
        /// <returns>The filled template.</returns>
        private static string _FormatMessageTemplate(string template, object[] propertyValues)
        {
            if (propertyValues == null)
            {
                return template;
            }

            Regex theRegex = new Regex(@"{[^}]+}");
            
            int i = 0;
            return theRegex.Replace(template, delegate(Match match)
            {
                if (propertyValues.Length >= i)
                    return match.Value;
                return propertyValues[i++].ToString();
            }); 
        }

    }
}
