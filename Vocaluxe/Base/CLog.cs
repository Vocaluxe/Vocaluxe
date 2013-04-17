using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Vocaluxe.Base
{
    class CLogFile : IDisposable
    {
        private readonly string _LogFileName;
        private readonly string _LogName;
        private StreamWriter _LogFile;

        public CLogFile(string fileName, string logName)
        {
            _LogName = logName;
            _LogFileName = fileName;
        }

        public void Close()
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

        public void Add(string text)
        {
            if (_LogFile == null)
                _Open();

            try
            {
                _LogFile.WriteLine(text);
                _LogFile.Flush();
            }
            catch (Exception) {}
        }

        private void _Open()
        {
            _LogFile = new StreamWriter(Path.Combine(Environment.CurrentDirectory, _LogFileName), false, Encoding.UTF8);

            _LogFile.WriteLine(_LogName + " " + CSettings.GetFullVersionText() + " " + DateTime.Now);
            _LogFile.WriteLine("----------------------------------------");
            _LogFile.WriteLine();
        }

        public void Dispose()
        {
            if (_LogFile != null)
            {
                _LogFile.Dispose();
                _LogFile = null;
            }
            GC.SuppressFinalize(this);
        }
    }

    static class CLog
    {
        private const int _MaxBenchmarks = 10;

        private static CLogFile _ErrorLog;
        private static CLogFile _PerformanceLog;
        private static CLogFile _BenchmarkLog;

        private static int _NumErrors;
        private static Stopwatch[] _BenchmarkTimer;
        private static readonly double _NanosecPerTick = (1000.0 * 1000.0 * 1000.0) / Stopwatch.Frequency;

        public static void Init()
        {
            _ErrorLog = new CLogFile(CSettings.FileErrorLog, "ErrorLog");
            _PerformanceLog = new CLogFile(CSettings.FilePerformanceLog, "PerformanceLog");
            _BenchmarkLog = new CLogFile(CSettings.FileBenchmarkLog, "BenchmarkLog");

            _NumErrors = 0;
            _BenchmarkTimer = new Stopwatch[_MaxBenchmarks];
            for (int i = 0; i < _BenchmarkTimer.Length; i++)
                _BenchmarkTimer[i] = new Stopwatch();
        }

        public static void CloseAll()
        {
            _ErrorLog.Close();
            _PerformanceLog.Close();
            _BenchmarkLog.Close();
        }

        #region LogError
        public static void LogError(string errorText)
        {
            _NumErrors++;

            _ErrorLog.Add(_NumErrors + ") " + errorText);
            _ErrorLog.Add(String.Empty);
        }
        #endregion LogError

        #region LogPerformance
        public static void LogPerformance(string text)
        {
            _PerformanceLog.Add(text);
            _PerformanceLog.Add("-------------------------------");
        }
        #endregion LogPerformance

        #region LogBenchmark
        public static void StartBenchmark(int benchmarkNr, string text)
        {
            if (benchmarkNr >= 0 && benchmarkNr < _MaxBenchmarks)
            {
                _BenchmarkTimer[benchmarkNr].Stop();
                _BenchmarkTimer[benchmarkNr].Reset();

                string space = String.Empty;
                for (int i = 0; i < benchmarkNr; i++)
                    space += "  ";
                _BenchmarkLog.Add(space + "Start " + text);

                _BenchmarkTimer[benchmarkNr].Start();
            }
        }

        public static void StopBenchmark(int benchmarkNr, string text)
        {
            if (benchmarkNr >= 0 && benchmarkNr < _MaxBenchmarks)
            {
                _BenchmarkTimer[benchmarkNr].Stop();

                string space = String.Empty;
                for (int i = 0; i < benchmarkNr; i++)
                    space += "  ";

                float ms;
                if (Stopwatch.IsHighResolution && _NanosecPerTick != 0.0)
                    ms = (float)((_NanosecPerTick * _BenchmarkTimer[benchmarkNr].ElapsedTicks) / (1000.0 * 1000.0));
                else
                    ms = _BenchmarkTimer[benchmarkNr].ElapsedMilliseconds;

                _BenchmarkLog.Add(space + "Stop " + text + ", Elapsed Time: " + ms.ToString("0.000") + "ms");
                _BenchmarkLog.Add(String.Empty);
            }
        }
        #endregion LogBenchmark
    }
}