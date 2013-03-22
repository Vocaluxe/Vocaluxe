using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Vocaluxe.Base
{
    class Log : IDisposable
    {
        private readonly string _LogFileName;
        private readonly string _LogName;
        private StreamWriter _LogFile;

        public Log(string FileName, string LogName)
        {
            _LogName = LogName;
            _LogFileName = FileName;
        }

        public void Close()
        {
            if (_LogFile != null)
            {
                try
                {
                    _LogFile.Flush();
                    _LogFile.Close();
                }
                catch (Exception) {}
            }
        }

        public void Add(string Text)
        {
            if (_LogFile == null)
                Open();

            try
            {
                _LogFile.WriteLine(Text);
                _LogFile.Flush();
            }
            catch (Exception) {}
        }

        private void Open()
        {
            _LogFile = new StreamWriter(Path.Combine(Environment.CurrentDirectory, _LogFileName), false, Encoding.UTF8);

            _LogFile.WriteLine(_LogName + " " + CSettings.GetFullVersionText() + " " + DateTime.Now.ToString());
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
        private const int MAXBenchmarks = 10;

        private static Log _ErrorLog;
        private static Log _PerformanceLog;
        private static Log _BenchmarkLog;

        private static int _NumErrors;
        private static Stopwatch[] _BenchmarkTimer;
        private static readonly double nanosecPerTick = (1000.0 * 1000.0 * 1000.0) / Stopwatch.Frequency;

        public static void Init()
        {
            _ErrorLog = new Log(CSettings.sFileErrorLog, "ErrorLog");
            _PerformanceLog = new Log(CSettings.sFilePerformanceLog, "PerformanceLog");
            _BenchmarkLog = new Log(CSettings.sFileBenchmarkLog, "BenchmarkLog");

            _NumErrors = 0;
            _BenchmarkTimer = new Stopwatch[MAXBenchmarks];
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
        public static void LogError(string ErrorText)
        {
            _NumErrors++;

            _ErrorLog.Add(_NumErrors.ToString() + ") " + ErrorText);
            _ErrorLog.Add(String.Empty);
        }
        #endregion LogError

        #region LogPerformance
        public static void LogPerformance(string Text)
        {
            _PerformanceLog.Add(Text);
            _PerformanceLog.Add("-------------------------------");
        }
        #endregion LogPerformance

        #region LogBenchmark
        public static void StartBenchmark(int BenchmarkNr, string Text)
        {
            if (BenchmarkNr >= 0 && BenchmarkNr < MAXBenchmarks)
            {
                _BenchmarkTimer[BenchmarkNr].Stop();
                _BenchmarkTimer[BenchmarkNr].Reset();

                string space = String.Empty;
                for (int i = 0; i < BenchmarkNr; i++)
                    space += "  ";
                _BenchmarkLog.Add(space + "Start " + Text);

                _BenchmarkTimer[BenchmarkNr].Start();
            }
        }

        public static void StopBenchmark(int BenchmarkNr, string Text)
        {
            if (BenchmarkNr >= 0 && BenchmarkNr < MAXBenchmarks)
            {
                _BenchmarkTimer[BenchmarkNr].Stop();

                string space = String.Empty;
                for (int i = 0; i < BenchmarkNr; i++)
                    space += "  ";

                float ms;
                if (Stopwatch.IsHighResolution && nanosecPerTick != 0.0)
                    ms = (float)((nanosecPerTick * _BenchmarkTimer[BenchmarkNr].ElapsedTicks) / (1000.0 * 1000.0));
                else
                    ms = _BenchmarkTimer[BenchmarkNr].ElapsedMilliseconds;

                _BenchmarkLog.Add(space + "Stop " + Text + ", Elapsed Time: " + ms.ToString("0.000") + "ms");
                _BenchmarkLog.Add(String.Empty);
            }
        }
        #endregion LogBenchmark
    }
}