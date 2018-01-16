using System;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace VocaluxeLib.Log.Enricher
{
    public static class WithTimeStampFromStartExtension
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
