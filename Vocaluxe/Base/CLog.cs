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
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Vocaluxe.Base
{
    static class CLog
    {
        private static bool _Initialized;

        private static readonly StringBuilder _MainLogStringBuilder = new StringBuilder();
        private static readonly StringWriter _MainLogStringWriter = new StringWriter(_MainLogStringBuilder);

        private static Logger _MainLog;
        private static Logger _SongInfoLog;
        private static string _LogFolder;

        private const string _MainLogTemplate = "[{TimeStampFromStart}] [{Level}] {Message}{NewLine}{Properties}{NewLine}{Exception}";
        private const string _SongInfoLogTemplate = "{Message}{NewLine}Additional info:{Properties}{NewLine}{Exception}";

        public static void Init(string logFolder)
        {
            _LogFolder = logFolder;

            _RollLogs(Path.Combine(_LogFolder, CSettings.FileNameMainLog), 2);
            _RollLogs(Path.Combine(_LogFolder, CSettings.FileNameSongInfoLog), 2);

            _MainLog = new LoggerConfiguration()
                .Enrich.WithThreadId()
                .Enrich.FromLogContext()
                .Enrich.WithTimeStampFromStart()
                .WriteTo.TextWriter(_MainLogStringWriter, outputTemplate: _MainLogTemplate)
                // Json can be activated by adding "new CompactJsonFormatter()" as first argument
                .WriteTo.File(Path.Combine(_LogFolder, CSettings.FileNameMainLog), flushToDiskInterval: TimeSpan.FromSeconds(30), outputTemplate: _MainLogTemplate)
#if DEBUG
                .WriteTo.Console(outputTemplate: _MainLogTemplate)
#endif
                .CreateLogger();

            Log.Logger = _MainLog;

            _SongInfoLog = new LoggerConfiguration()
                .WriteTo.File(Path.Combine(_LogFolder, CSettings.FileNameSongInfoLog), flushToDiskInterval: TimeSpan.FromSeconds(60), outputTemplate: _SongInfoLogTemplate)
#if DEBUG
                .WriteTo.Console(outputTemplate: "[SongInfo] " + _SongInfoLogTemplate)
#endif
                .CreateLogger();
           
            _Initialized = true;
        }
        

        public static void Close()
        {
            if (_Initialized)
            {
                
                _MainLog.Dispose();
                _MainLog = null;
                _SongInfoLog.Dispose();
                _SongInfoLog = null;
                _Initialized = false;
            }
        }

        public static string MainLogEntries
        {
            get {
                _MainLogStringWriter.Flush();
                return _MainLogStringBuilder.ToString();
            }
        }

        public static void LogError(Exception exception, string errorText, bool show = false, bool exit = false, [CallerMemberName] string callerMethodeName = "",
            [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1, params object[] propertyValues)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName = callerMethodeName, callerFilePath = callerFilePath, callerLineNumer = callerLineNumer }))
            {
                if (exit)
                {
                    _MainLog.Fatal(exception, errorText, propertyValues);
                }
                else
                {
                    _MainLog.Error(exception, errorText, propertyValues);
                }
            }

            if (show)
            {
                //Todo: Logsend assistent
                MessageBox.Show(errorText, CSettings.ProgramName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


            if (exit)
            {
                // Flush the logging piplines before exiting
                _MainLog.Dispose();
                _SongInfoLog.Dispose();
                // Close the Programm
                Environment.Exit(Environment.ExitCode);
            }
        }

        public static void LogError(string errorText, bool show = false, bool exit = false, [CallerMemberName] string callerMethodeName = "",
            [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1, params object[] propertyValues)
        {
            // ReSharper disable ExplicitCallerInfoArgument
            LogError(null, errorText, show, exit, callerMethodeName, callerFilePath, callerLineNumer, propertyValues);
        }

        public static void LogWarning(string text, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1, params object[] propertyValues)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName = callerMethodeName, callerFilePath = callerFilePath, callerLineNumer = callerLineNumer }))
            {
                _MainLog.Warning(text, propertyValues);
            }
        }

        public static void LogInformation(string text, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1, params object[] propertyValues)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName = callerMethodeName, callerFilePath = callerFilePath, callerLineNumer = callerLineNumer }))
            {
                _MainLog.Information(text, propertyValues);
            }
        }

        public static void LogDebug(string text, [CallerMemberName] string callerMethodeName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumer = -1, params object[] propertyValues)
        {
            using (LogContext.PushProperty("CallingContext", new { callerMethodeName = callerMethodeName, callerFilePath = callerFilePath, callerLineNumer = callerLineNumer }))
            {
                _MainLog.Debug(text, propertyValues);
            }
        }

        public static void LogSongError(Exception exception, string text, params object[] propertyValues)
        {
            _SongInfoLog.Error(exception, text, propertyValues);
        }
        public static void LogSongError(string text, params object[] propertyValues)
        {
            _SongInfoLog.Error(text, propertyValues);
        }

        public static void LogSongWaring(string text, params object[] propertyValues)
        {
            _SongInfoLog.Warning(text, propertyValues);
        }

        #region Log rolling

        private static void _RollLogs(string mainLogFile, int numLogsToKeep)
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
                try { 
                    if(numLogsToKeep > 0) 
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

        #endregion

        #region CTimeStampFromStartEnricher

        // ReSharper disable once InconsistentNaming
        private static LoggerConfiguration WithTimeStampFromStart(this LoggerEnrichmentConfiguration enrich)
        {
            return enrich.With(new CTimeStampFromStartEnricher());
        }

        private class CTimeStampFromStartEnricher : ILogEventEnricher
        {
            private readonly DateTimeOffset _Start  = DateTimeOffset.Now;

            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory lepf)
            {
                logEvent.AddPropertyIfAbsent(
                    lepf.CreateProperty("TimeStampFromStart", logEvent.Timestamp - _Start));
            }
        }

        #endregion
    }
}