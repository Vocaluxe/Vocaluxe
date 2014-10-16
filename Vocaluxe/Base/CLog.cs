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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Vocaluxe.Base
{
    class CLogFile : IDisposable
    {
        private readonly string _LogFileName;
        private readonly string _LogName;
        private StreamWriter _LogFile;
        private readonly Object _FileMutex = new Object();

        public CLogFile(string fileName, string logName)
        {
            _LogName = logName;
            _LogFileName = fileName;
        }

        private void _Open()
        {
            lock (_FileMutex)
            {
                if (_LogFile != null)
                    return;
                _LogFile = new StreamWriter(Path.Combine(CSettings.DataFolder, _LogFileName), false, Encoding.UTF8);

                _LogFile.WriteLine(_LogName + " " + CSettings.GetFullVersionText() + " " + DateTime.Now);
                _LogFile.WriteLine("----------------------------------------");
                _LogFile.WriteLine();
            }
        }

        public void Close()
        {
            lock (_FileMutex)
            {
                if (_LogFile == null)
                    return;
                try
                {
                    _LogFile.Flush();
                    _LogFile.Close();
                }
                catch (Exception) {}
            }
        }

        public virtual void Add(string text)
        {
            if (_LogFile == null)
                _Open();

            // ReSharper disable PossibleNullReferenceException
            _LogFile.WriteLine(text);
            // ReSharper restore PossibleNullReferenceException
            _LogFile.Flush();
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }
    }

    class CErrorLogFile : CLogFile
    {
        private int _NumErrors;
        private readonly Object _WriteMutex = new Object();

        public CErrorLogFile(string fileName, string logName) : base(fileName, logName) {}

        public override void Add(string errorText)
        {
            lock (_WriteMutex)
            {
                _NumErrors++;
                base.Add(_NumErrors + ") " + errorText);
            }
        }
    }

    static class CLog
    {
        private const int _MaxBenchmarks = 10;
        private static int _BenchmarksRunning;
        private static bool _Initialized;

        private static CLogFile _ErrorLog;
        private static CLogFile _PerformanceLog;
        private static CLogFile _BenchmarkLog;
        private static CLogFile _DebugLog;
        private static CLogFile _SongInfoLog;

        private static Stopwatch[] _BenchmarkTimer;
        private static readonly double _NanosecPerTick = (1000.0 * 1000.0 * 1000.0) / Stopwatch.Frequency;

        public static void Init()
        {
            _ErrorLog = new CErrorLogFile(CSettings.FileNameErrorLog, "Error-Log");
            _PerformanceLog = new CLogFile(CSettings.FileNamePerformanceLog, "Performance-Log");
            _BenchmarkLog = new CLogFile(CSettings.FileNameBenchmarkLog, "Benchmark-Log");
            _DebugLog = new CLogFile(CSettings.FileNameDebugLog, "Debug-Log");
            _SongInfoLog = new CLogFile(CSettings.FileNameSongInfoLog, "Song-Information-Log");

            _BenchmarkTimer = new Stopwatch[_MaxBenchmarks];
            for (int i = 0; i < _BenchmarkTimer.Length; i++)
                _BenchmarkTimer[i] = new Stopwatch();
            _BenchmarksRunning = 0;
            _Initialized = true;
        }

        public static void Close()
        {
            if (_Initialized)
            {
                _ErrorLog.Dispose();
                _ErrorLog = null;
                _PerformanceLog.Dispose();
                _PerformanceLog = null;
                _BenchmarkLog.Dispose();
                _BenchmarkLog = null;
                _DebugLog.Dispose();
                _DebugLog = null;
                _SongInfoLog.Dispose();
                _SongInfoLog = null;
                _Initialized = false;
            }
        }

        #region LogError
        public static void LogError(string errorText, bool show = false, bool exit = false, Exception e = null)
        {
            if (show)
                MessageBox.Show(errorText, CSettings.ProgramName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (e != null)
                errorText += ": " + e;
            _ErrorLog.Add(errorText);
            if (exit)
                Environment.Exit(Environment.ExitCode);
        }
        #endregion LogError

        public static void LogDebug(string text)
        {
            _DebugLog.Add(String.Format("{0:HH:mm:ss.ffff}", DateTime.Now) + ":" + text);
#if DEBUG
            Console.WriteLine(text);
#endif
        }

        public static void LogSongInfo(string text)
        {
            _SongInfoLog.Add(text);
        }

        public static void LogPerformance(string text)
        {
            _PerformanceLog.Add(text);
            _PerformanceLog.Add("-------------------------------");
        }

        #region LogBenchmark
        public static void StartBenchmark(string text)
        {
            if (_BenchmarksRunning < _MaxBenchmarks)
            {
                string space = String.Empty;
                for (int i = 0; i < _BenchmarksRunning; i++)
                    space += "  ";
                _BenchmarkLog.Add(space + "Start " + text);

                _BenchmarkTimer[_BenchmarksRunning].Restart();
            }
            else
                LogError("Tried to start to many benchmarks at once"); //Log and ignore
            _BenchmarksRunning++; //Inc even if not started to correct right Stop() call
        }

        public static void StopBenchmark(string text)
        {
            if (_BenchmarksRunning > 0 && _BenchmarksRunning <= _MaxBenchmarks)
            {
                _BenchmarksRunning--;
                _BenchmarkTimer[_BenchmarksRunning].Stop();

                string space = String.Empty;
                for (int i = 0; i < _BenchmarksRunning; i++)
                    space += "  ";

                float ms;
                if (Stopwatch.IsHighResolution && _NanosecPerTick > 0)
                    ms = (float)((_NanosecPerTick * _BenchmarkTimer[_BenchmarksRunning].ElapsedTicks) / (1000.0 * 1000.0));
                else
                    ms = _BenchmarkTimer[_BenchmarksRunning].ElapsedMilliseconds;

                _BenchmarkLog.Add(space + "Stop " + text + ", Elapsed Time: " + ms.ToString("0.000") + "ms");
                _BenchmarkLog.Add(String.Empty);
            }
            else
                _BenchmarksRunning--;
        }
        #endregion LogBenchmark
    }
}