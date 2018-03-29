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
using Serilog.Events;

namespace VocaluxeLib.Log
{
    public enum ELogLevel
    {
        Verbose,
        Debug,
        Information,
        Warning,
        Error,
        Fatal
    }

    public static class CLogLevelExtension
    {
        public static LogEventLevel ToSerilogLogLevel(this ELogLevel level)
        {
            switch (level)
            {
                case ELogLevel.Verbose:
                    return LogEventLevel.Verbose;
                case ELogLevel.Debug:
                    return LogEventLevel.Debug;
                case ELogLevel.Information:
                    return LogEventLevel.Information;
                case ELogLevel.Warning:
                    return LogEventLevel.Warning;
                case ELogLevel.Error:
                    return LogEventLevel.Error;
                case ELogLevel.Fatal:
                    return LogEventLevel.Fatal;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }
    }
}
