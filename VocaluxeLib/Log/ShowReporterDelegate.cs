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

namespace VocaluxeLib.Log
{
    /// <summary>
    /// Shows a new instance of the log file reporter.
    /// </summary>
    /// <param name="crash">True if we are submitting a crash, false otherwise (e.g. error).</param>
    /// <param name="showContinue">True if the reporter show show a continue message, false if it should show an exit message.</param>
    /// <param name="vocaluxeVersionTag">The full version tag of this instance (like it is diplayed in the main menu).</param>
    /// <param name="log">The log to submit.</param>
    /// <param name="lastError">The last error message.</param>
    public delegate void ShowReporterDelegate(bool crash, bool showContinue, string vocaluxeVersionTag, string log, string lastError);
}
