using System;
using System.IO;
using System.Text;
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

        private static ILogger _MainLog = new SilentLogger();
        private static ILogger _SongLog = new SilentLogger();
        private static string _LogFolder;

        private const string _MainLogTemplate = "[{TimeStampFromStart}] [{Level}] {Message}{NewLine}{Properties}{NewLine}{Exception}";
        private const string _SongLogTemplate = "{Message}{NewLine}Additional info:{Properties}{NewLine}{Exception}";

        public static void Init(string logFolder, string fileNameMainLog, string fileNameSongInfoLog)
        {
            _LogFolder = logFolder;
            if (!Directory.Exists(_LogFolder))
                Directory.CreateDirectory(_LogFolder);

            LogFileRoller.RollLogs(Path.Combine(_LogFolder, fileNameMainLog), 2);
            LogFileRoller.RollLogs(Path.Combine(_LogFolder, fileNameSongInfoLog), 2);

            _MainLog = new LoggerConfiguration()
                .Enrich.WithThreadId()
                .Enrich.FromLogContext()
                .Enrich.WithTimeStampFromStart()
                .WriteTo.TextWriter(_MainLogStringWriter, outputTemplate: _MainLogTemplate)
                // Json can be activated by adding "new CompactJsonFormatter()" as first argument
                .WriteTo.File(Path.Combine(_LogFolder, fileNameMainLog), flushToDiskInterval: TimeSpan.FromSeconds(30), outputTemplate: _MainLogTemplate)
#if DEBUG
                .WriteTo.Console(outputTemplate: _MainLogTemplate)
#endif
                .CreateLogger();

            _SongLog = new LoggerConfiguration()
                .WriteTo.File(Path.Combine(_LogFolder, fileNameSongInfoLog), flushToDiskInterval: TimeSpan.FromSeconds(60), outputTemplate: _SongLogTemplate)
#if DEBUG
                .WriteTo.Console(outputTemplate: "[SongInfo] " + _SongLogTemplate)
#endif
                .CreateLogger();
        }

        public static void Close()
        {
            if (!(_MainLog is SilentLogger))
            {
                ILogger loggerToDispose = _MainLog;

                _MainLog = new SilentLogger();
                (_MainLog as IDisposable)?.Dispose();
            }

            if (!(_SongLog is SilentLogger))
            {
                ILogger loggerToDispose = _MainLog;

                _SongLog = new SilentLogger();
                (_SongLog as IDisposable)?.Dispose();
            }

        }

        private static void _ShowLogAssistent(string messageTemplate, object[] propertyValues, string callerMethodeName = "", string callerFilePath = "", int callerLineNumer = -1)
        {
            //Todo: Logsend assistent
            //MessageBox.Show(messageTemplate, "Vocaluxe", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

    }
}
