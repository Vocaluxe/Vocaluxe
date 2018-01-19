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
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace VocaluxeLib.Log.Enricher
{
    public static class CWithTimeStampFromStartExtension
    {
        // ReSharper disable once InconsistentNaming
        public static LoggerConfiguration WithTimeStampFromStart(this LoggerEnrichmentConfiguration enrich)
        {
            return enrich.With(new CTimeStampFromStartEnricher());
        }

        private class CTimeStampFromStartEnricher : ILogEventEnricher
        {
            private readonly DateTimeOffset _Start = DateTimeOffset.Now;

            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory lepf)
            {
                logEvent.AddPropertyIfAbsent(
                    lepf.CreateProperty("TimeStampFromStart", logEvent.Timestamp - _Start));
            }
        }
    }
}
