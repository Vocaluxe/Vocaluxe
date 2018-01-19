
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
