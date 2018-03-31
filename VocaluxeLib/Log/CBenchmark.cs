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
using VocaluxeLib.Utils;

namespace VocaluxeLib.Log
{
    /// <summary>
    /// Class for Benchmarks.
    /// Can be used as instance or through the static wrappers.
    /// </summary>
    public class CBenchmark : IDisposable
    {
        // Pool of stop watches shared between all instances (we can get more than 5 but it only keeps 5)
        private static readonly CObjectPool<Stopwatch> _WatchesPool = new CObjectPool<Stopwatch>(() => new Stopwatch(), 5);
        private static readonly double _NanosecPerTick = (1000.0 * 1000.0 * 1000.0) / Stopwatch.Frequency;

        // The stop watch used by this instance
        private Stopwatch _Watch = null;
        // The operation name the will be written to the log on events for this instance
        private readonly string _OperationName;

        /// <summary>
        /// Creates a new benchmark instance.
        /// </summary>
        /// <param name="operationName">The name of the operation (will be used for log messages).</param>
        public CBenchmark(string operationName)
        {
            _OperationName = operationName;
        }

        /// <summary>
        /// Starts this benchmark.
        /// </summary>
        public void Start()
        {
            if (_Watch != null)
                throw new InvalidOperationException("Timer is already running.");
            _Watch = _WatchesPool.GetObject();
            _Watch.Restart();
            CLog.Information("Started {StartedOperation}", CLog.Params(_OperationName));
        }

        /// <summary>
        /// Stops this benchmark.
        /// </summary>
        /// <param name="success">If true a success message will be added to the log otherwise a message for failure.</param>
        public void Stop(bool success = true)
        {
            if (_Watch == null)
                return;
            _Watch.Stop();
            double duration = _GetElapsedTime(_Watch);

            _WatchesPool.PutObject(_Watch);
            _Watch = null;
            if (success)
                CLog.Information("Finished {StartedOperation} successfully in {Duration:#,##0.00}ms", CLog.Params(_OperationName, duration));
            else
                CLog.Information("Failed {StartedOperation} in {Duration:#,##0.00}ms", CLog.Params(_OperationName, duration));
        }

        /// <summary>
        /// Helper method to extract the elapsed time from a stopwatch.
        /// </summary>
        /// <param name="watch">The stopwatch.</param>
        /// <returns>The elapsed time of the given stop watch.</returns>
        private double _GetElapsedTime(Stopwatch watch)
        {
            if (Stopwatch.IsHighResolution && _NanosecPerTick > 0)
                return (float)((_NanosecPerTick * watch.ElapsedTicks) / (1000.0 * 1000.0));
            else
                return watch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Ends a possibly running benchmark (sucessfully).
        /// </summary>
        public void Dispose()
        {
            if (_Watch != null)
                Stop();
        }

        #region Static functions

        /// <summary>
        /// Logs the time from now to disposal of the returned object.
        /// </summary>
        /// <param name="operationName">The name of the operation (will be used for log messages).</param>
        /// <returns>Object which needs to be disposed to stop the benchmark.</returns>
        /// <example>
        /// <code> 
        /// using(CBenchmark.Time("TestOperation")){
        ///     //do someting
        /// }
        /// </code>
        /// </example>
        public static IDisposable Time(string operationName)
        {
            CBenchmark benchmark = new CBenchmark(operationName);
            benchmark.Start();
            return benchmark;
        }

        /// <summary>
        /// Logs the time from now to the call of End() on the returned object (== success) or it's disposal (== failure).
        /// </summary>
        /// <param name="operationName">The name of the operation (will be used for log messages).</param>
        /// <returns>Object to stop the benchmark.</returns>
        /// <example>
        /// <code> 
        /// using(var op = CBenchmark.Begin("TestOperation")){
        ///     //do someting that could fail
        ///     op.End();
        /// }
        /// </code>
        /// </example>
        public static COperation Begin(string operationName)
        {
            CBenchmark benchmark = new CBenchmark(operationName);
            benchmark.Start();
            return new COperation(benchmark);
        }

        #region Helper class for operation

        /// <summary>
        /// Helper object to end a benchmark (End() for success and Dispose() for failure).
        /// </summary>
        public class COperation : IDisposable
        {
            private readonly CBenchmark _Benchmark;

            /// <summary>
            /// Creates a new instance of the benchmark helper object.
            /// </summary>
            /// <param name="benchmark">The associated benchmark object.</param>
            internal COperation(CBenchmark benchmark)
            {
                _Benchmark = benchmark;

            }

            /// <summary>
            /// End the associated benchmark sucessfully.
            /// </summary>
            public void End()
            {
                _Benchmark.Stop(true);
            }

            /// <summary>
            /// End the associated benchmark not sucessfully.
            /// </summary>
            public void Dispose()
            {
                _Benchmark.Stop(false);
            }
        }

        #endregion

        #endregion
    }
}
