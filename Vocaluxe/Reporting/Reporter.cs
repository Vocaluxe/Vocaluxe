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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Vocaluxe.Base;
using Vocaluxe.Lib;
using VocaluxeLib.Log;

namespace Vocaluxe.Reporting
{
    public class CReporter
    {
        // Init string with defaults for the case that loading localizations caused the problem
        private string _TitleText = "We are sorry";
        private string _MessageCrashText =
            "that Vocaluxe run into an error and crashed.\r\nTo help us fix the problem please send us a report.\r\nOpen the log to check it and remove possibly sensitive information before submitting.";
        private string _MessageNoCrashText =
            "that you experienced an error.\r\nTo help us fix the problem please send us a report.\r\nOpen the log to check it and remove possibly sensitive information before submitting.";
        private string _NoUploadText = "Don\'t upload anything (you can still copy the error message above)";
        private string _GistAndIssueText = "Upload and open an issue (publicly visible + requires a github account)";
        private string _GistOnlyText = "Upload and get a link to your report (publicly visible)";
        private string _OpenLogText = "Open the log to check or edit it";
        private string _NoUploadButtonText = "Don't upload";
        private string _GistAndIssueButtonText = "Upload + issue";
        private string _GistOnlyButtonText = "Upload";
        private string _OpenLogButtonText = "Open log";
        private string _SubmitStep2ExitText = "Exit";
        private string _SubmitStep2ContinueText = "Continue";
        private string _SubmitedMessageText = "The link to your report:";
        private string _LogUploadErrorText = "Error uploading the log.";
        private string _LastErrorTitleText = "Last error";
        private string _LastErrorNa = "Not available";
        private string _IssueTemplate = "Describe your issue here.\n\n### Steps to reproduce\nTell us how to reproduce this issue.\n\n### Vocaluxe version and logfile\n{0}\n{1}";

        private readonly bool _Crash;
        private readonly string _VocaluxeVersionTag;
        private readonly bool _ShowContinue;
        private string _LastError;
        private string _LogFile;

        private static readonly HttpClient _Client = new HttpClient();
        private static readonly Regex _GetGistUrlRegex = new Regex("\"html_url\": *\"([^\"]+)\"");

        public static ShowReporterDelegate ShowReporterFunc
        {
            get { return _ShowReporter; }
        }

        /// <summary>
        /// Shows a new instance of the log file reporter.
        /// </summary>
        /// <param name="crash">True if we are submitting a crash, false otherwise (e.g. error).</param>
        /// <param name="showContinue">True if the reporter show show a continue message, false if it should show an exit message.</param>
        /// <param name="vocaluxeVersionTag">The full version tag of this instance (like it is diplayed in the main menu).</param>
        /// <param name="log">The log to submit.</param>
        /// <param name="lastError">The last error message.</param>
        private static void _ShowReporter(bool crash, bool showContinue, string vocaluxeVersionTag, string log, string lastError)
        {
            new CReporter(crash, showContinue, vocaluxeVersionTag, log, lastError)._Show();
        }

        /// <summary>
        /// Creates a new instance of the log file reporter.
        /// </summary>
        /// <param name="crash">True if we are submitting a crash, false otherwise (e.g. error).</param>
        /// <param name="showContinue">True if the reporter show show a continue message, false if it should show an exit message.</param>
        /// <param name="vocaluxeVersionTag">The full version tag of this instance (like it is diplayed in the main menu).</param>
        /// <param name="log">The log to submit.</param>
        /// <param name="lastError">The last error message.</param>
        public CReporter(bool crash, bool showContinue, string vocaluxeVersionTag, string log, string lastError)
        {
            _Crash = crash;
            _ShowContinue = showContinue;
            _VocaluxeVersionTag = vocaluxeVersionTag;
            _Init(log, lastError);
        }

        /// <summary>
        /// Initilaize the gui.
        /// </summary>
        /// <param name="log">The Log to submit.</param>
        /// <param name="lastError">The last error message.</param>
        private void _Init(string log, string lastError)
        {
            // Try to load localized strings
            try
            {
                _TitleText = CLanguage.Translate("TR_REPORTER_TITLE_TEXT");
                _MessageCrashText = CLanguage.Translate("TR_REPORTER_MESSAGE_CRASH_TEXT").Replace("\\n", "\n").Replace("\\r", "\r");
                _MessageNoCrashText = CLanguage.Translate("TR_REPORTER_MESSAGE_NO_CRASH_TEXT").Replace("\\n", "\n").Replace("\\r", "\r");
                _NoUploadText = CLanguage.Translate("TR_REPORTER_NO_UPLOAD_TEXT");
                _GistAndIssueText = CLanguage.Translate("TR_REPORTER_GIST_AND_ISSUE_TEXT");
                _GistOnlyText = CLanguage.Translate("TR_REPORTER_GIST_ONLY_TEXT");
                _OpenLogText = CLanguage.Translate("TR_REPORTER_OPEN_LOG_TEXT");
                _NoUploadButtonText = CLanguage.Translate("TR_REPORTER_NO_UPLOAD_BUTTON");
                _GistAndIssueButtonText = CLanguage.Translate("TR_REPORTER_GIST_AND_ISSUE_BUTTON");
                _GistOnlyButtonText = CLanguage.Translate("TR_REPORTER_GIST_ONLY_BUTTON");
                _OpenLogButtonText = CLanguage.Translate("TR_REPORTER_OPEN_LOG_BUTTON");
                _SubmitStep2ExitText = CLanguage.Translate("TR_REPORTER_SUBMIT_STEP2_EXIT_TEXT");
                _SubmitStep2ContinueText = CLanguage.Translate("TR_REPORTER_SUBMIT_STEP2_CONTINUE_TEXT");
                _SubmitedMessageText = CLanguage.Translate("TR_REPORTER_SUBMITED_MESSAGE_TEXT");
                _LogUploadErrorText = CLanguage.Translate("TR_REPORTER_LOGUPLOAD_ERROR_TEXT");
                _LastErrorTitleText = CLanguage.Translate("TR_REPORTER_LAST_ERROR_TITLE_TEXT");
                _LastErrorNa = CLanguage.Translate("TR_REPORTER_LAST_ERROR_NA");
                _IssueTemplate = CLanguage.Translate("TR_REPORTER_ISSUE_TEMPLATE").Replace("\\n", "\n").Replace("\\r", "\r");
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine($"Unable to load translation for report assistant: {e.Message}");
#endif
            }

            _Client.DefaultRequestHeaders.Add("accept", "application/json");
            _Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/6.0)");

            _LastError = string.IsNullOrWhiteSpace(lastError) ? _LastErrorNa : lastError;
            _LogFile = Path.Combine(CSettings.DataFolder, CSettings.FolderNameLogs, "Report.log");
            try
            {
                File.WriteAllText(_LogFile, log.Replace("\n", Environment.NewLine));
            }
            catch (Exception e)
            {
                _LogFile = null;
                CLog.Error(e, "Couldn't write the report log");
            }
        }

        private void _Show()
        {
            const int gistOnly = 0;
            const int gistAndIssue = 1;
            const int openLog = 2;
            const int noUpload = 3;
            var options = new List<int> { gistOnly, gistAndIssue };
            if (_LogFile != null)
            {
                options.Add(openLog);
            }

            options.Add(noUpload);
            var buttonTexts = new[] { _GistOnlyButtonText, _GistAndIssueButtonText, _OpenLogButtonText, _NoUploadButtonText };
            var descriptions = new[] { _GistOnlyText, _GistAndIssueText, _OpenLogText, _NoUploadText };

            var message = (_Crash ? _MessageCrashText : _MessageNoCrashText).Replace("\r\n", "\n") + "\n\n" + _LastErrorTitleText + ":\n" + _LastError + "\n";
            foreach (var option in options)
            {
                message += "\n" + buttonTexts[option] + ": " + descriptions[option];
            }

            while (true)
            {
                var selected = CSdl.ShowMessage(_TitleText, message, true, options.Select(option => buttonTexts[option]).ToArray());
                if (selected < 0 || options[selected] == noUpload)
                {
                    return;
                }

                if (options[selected] == openLog)
                {
                    CSdl.OpenUrl(new Uri(_LogFile).AbsoluteUri);
                    continue;
                }

                var log = _ReadLog();
                var url = options[selected] == gistAndIssue ? _StartIssueUpload(log, _LastError) : _UploadLogToGist(log, _LastError).GetAwaiter().GetResult();
                _UploadFinished(url);
                return;
            }
        }

        private string _ReadLog()
        {
            try
            {
                return File.ReadAllText(_LogFile);
            }
            catch (Exception e)
            {
                CLog.Error(e, "Couldn't read the report log");
                return "";
            }
        }

        /// <summary>
        /// Updates the gui after the upload finished.
        /// </summary>
        /// <param name="url">Url to show, if null or empty an error message is shown.</param>
        private void _UploadFinished(string url)
        {
            var text = string.IsNullOrWhiteSpace(url) ? _LogUploadErrorText : _SubmitedMessageText + "\n" + url;
            if (!string.IsNullOrWhiteSpace(url))
            {
                CSdl.SetClipboardText(url);
            }

            CSdl.ShowMessage(_TitleText, text, false, _ShowContinue ? _SubmitStep2ContinueText : _SubmitStep2ExitText);
        }

        /// <summary>
        /// Upload a log to gist, opens an issue template on github and updates the gui.
        /// </summary>
        /// <param name="log">The log to upload.</param>
        /// <param name="lastError">The error message displayed to the user (if available).</param>
        private string _StartIssueUpload(string log, string lastError = "not available")
        {
            // Upload log to github gist
            var gistUrl = _UploadLogToGist(log, lastError).GetAwaiter().GetResult();

            // Build issue body
            var template = string.Format(_IssueTemplate, _VocaluxeVersionTag, gistUrl);

            // Build url for github issue template
            var issueUrl = $"https://github.com/Vocaluxe/Vocaluxe/issues/new?title=Give%20me%20a%20meaningful%20title&body={Uri.EscapeDataString(template)}";

            // Start the browser with the issue template url
            CSdl.OpenUrl(issueUrl);

            return issueUrl;
        }

        /// <summary>
        /// Uploads a log to a new anonymous gist.
        /// </summary>
        /// <param name="log">The log that should be uploaded.</param>
        /// <param name="lastError">The error message displayed to the user (if available).</param>
        /// <returns>The link to the uploaded file.</returns>
        private async Task<string> _UploadLogToGist(string log, string lastError = "not available")
        {
            var json = JsonConvert.SerializeObject(new CGistCreateData
            {
                Description = $"An {(_Crash ? "crash" : "error")} log submission for {_VocaluxeVersionTag}",
                Public = false,
                Files = new Dictionary<string, CGistFileData>
                {
                    {
                        "Vocaluxe.log",
                        new CGistFileData
                        {
                            Content = log
                        }
                    },
                    {
                        "LastError.txt",
                        new CGistFileData
                        {
                            Content = lastError
                        }
                    }
                }
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var responseString = "";
            try
            {
                var response = await _Client.PostAsync("https://api.github.com/gists", content);

                responseString = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                CLog.Error(e, "Couldn't upload log", show: true, propertyValues: CLog.Params(json));
            }

            return _GetGistUrlRegex.Match(responseString)?.Groups[1]?.Value ?? "";
        }

        /// <summary>
        /// Helper class for gist creation.
        /// </summary>
        private class CGistCreateData
        {
            [JsonProperty("description")]
            public string Description { get; set; }
            [JsonProperty("public")]
            public bool Public { get; set; }
            [JsonProperty("files")]
            public Dictionary<string, CGistFileData> Files { get; set; }
        }

        /// <summary>
        /// Helper class for gist creation.
        /// </summary>
        private class CGistFileData
        {
            [JsonProperty("content")]
            public string Content { get; set; }
        }
    }
}