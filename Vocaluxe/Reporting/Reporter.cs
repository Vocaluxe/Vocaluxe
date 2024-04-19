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
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Vocaluxe.Base;
using VocaluxeLib.Log;

namespace Vocaluxe.Reporting
{
    public partial class CReporter : Form
    {
        // Init string with defaults for the case that loading localizations caused the problem
        private string _TitleText = "We are sorry";
        private string _MessageCrashText = "that Vocaluxe run into an error and crashed.\r\nTo help us fix the problem please send us a report.\r\nEdit the following log before submitting to remove possibly sensitive information.";
        private string _MessageNoCrashText = "that you experienced an error.\r\nTo help us fix the problem please send us a report.\r\nEdit the following log before submitting to remove possibly sensitive information.";
        private string _NoUploadText = "Don\'t upload anything (you can still copy the error message above)";
        private string _GistAndIssueText = "Upload and open an issue (publicly visible + requires a github account)";
        private string _GistOnlyText = "Upload and get a link to your report (publicly visible)";
        private string _SubmitStep0Text = "Submit";
        private string _SubmitStep1Text = "Uploading";
        private string _SubmitStep2ExitText = "Exit";
        private string _SubmitStep2ContinueText = "Continue";
        private string _SubmitedMessageText = "The link to your report:";
        private string _LogUploadErrorText = "Error uploading the log.";
        private string _LastErrorTitleText = "Last error";
        private string _LastErrorNa = "Not available";
        private string _IssueTemplate = "Describe your issue here.\n\n### Steps to reproduce\nTell us how to reproduce this issue.\n\n### Vocaluxe version and logfile\n{0}\n{1}";



        private int _Step = 0;
        private readonly bool _Crash;
        private readonly string _VocaluxeVersionTag;
        private readonly bool _ShowContinue;

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
            using (var reporter = new CReporter(crash, showContinue, vocaluxeVersionTag, log, lastError))
            {
                reporter.ShowDialog();
            }
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
            InitializeComponent();
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
                _SubmitStep0Text = CLanguage.Translate("TR_REPORTER_SUBMIT_STEP0_TEXT");
                _SubmitStep1Text = CLanguage.Translate("TR_REPORTER_SUBMIT_STEP1_TEXT");
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

            this.TitleLabel.Text = _TitleText;
            this.MessageText.Text = _Crash ? _MessageCrashText : _MessageNoCrashText;
            this.NoUploadSelect.Text = _NoUploadText;
            this.GistAndIssueSelect.Text = _GistAndIssueText;
            this.GistOnlySelect.Text = _GistOnlyText;
            this.SubmitButton.Text = _SubmitStep0Text;
            this.SubmitedTitleLabel.Text = _SubmitedMessageText;
            this.LastErrorTitleLabel.Text = _LastErrorTitleText;
            this.LastErrorBox.Text = string.IsNullOrWhiteSpace(lastError) ? _LastErrorNa : lastError;
            this.LogBox.Text = log.Replace("\n","\r\n");
            this.LogBox.SelectionStart = 0;
        }

        private async void Submit_Click(object sender, EventArgs e)
        {
            switch (_Step)
            {
                case 0:
                    if (this.NoUploadSelect.Checked)
                    {
                        this.Close();
                    }
                    else
                    {
                        _Step = 1;
                        this.SubmitButton.Text = _SubmitStep1Text;
                        this.SubmitButton.Enabled = false;
                        this.NoUploadSelect.Visible = false;
                        this.GistAndIssueSelect.Visible = false;
                        this.GistOnlySelect.Visible = false;
                        this.LogBox.Enabled = false;
                        this.LastErrorBox.Enabled = false;

                        if (this.GistOnlySelect.Checked)
                        {
                            await Task.Run(() => _StartGistUpload(this.LogBox.Text, this.LastErrorBox.Text));
                        }
                        else if (this.GistAndIssueSelect.Checked)
                        {
                            await Task.Run(() => _StartIssueUpload(this.LogBox.Text, this.LastErrorBox.Text));
                        }
                        else
                        {
                            _Step = 0;
                            this.SubmitButton.Text = _SubmitStep0Text;
                            this.SubmitButton.Enabled = true;
                            this.NoUploadSelect.Visible = true;
                            this.GistAndIssueSelect.Visible = true;
                            this.GistOnlySelect.Visible = true;
                            this.LogBox.Enabled = true;
                            this.LastErrorBox.Enabled = false;
                        }
                            
                    }

                    break;
                case 2:
                    this.Close();
                    break;
            }
        }

        private void NoUpload_CheckedChanged(object sender, EventArgs e)
        {
            this.SubmitButton.Text = this.NoUploadSelect.Checked ? (_ShowContinue ? _SubmitStep2ContinueText : _SubmitStep2ExitText) : _SubmitStep0Text;
        }

        /// <summary>
        /// Updates the gui after the upload finished.
        /// </summary>
        /// <param name="url">Url to show, if null or empty an error message is shown.</param>
        private void _UploadFinished(string url)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() => { _UploadFinished(url); }));
            }
            else
            {
                _Step = 2;
                this.SubmitButton.Text = _ShowContinue ? _SubmitStep2ContinueText : _SubmitStep2ExitText;
                this.SubmitButton.Enabled = true;
                this.SubmitedTitleLabel.Visible = true;
                this.Url.Text = string.IsNullOrWhiteSpace(url)?_LogUploadErrorText:url;
                this.Url.Visible = true;
                this.Url.SelectAll();
                this.Url.Focus();
            }
        }

        /// <summary>
        /// Upload a log to gist and updates the gui.
        /// </summary>
        /// <param name="log">The log to upload.</param>
        /// <param name="lastError">The error message displayed to the user (if available).</param>
        private async void _StartGistUpload(string log, string lastError= "not available")
        {
            // Upload log to github gist and show the link
            _UploadFinished(await _UploadLogToGist(log, lastError));
        }

        /// <summary>
        /// Upload a log to gist, opens an issue template on github and updates the gui.
        /// </summary>
        /// <param name="log">The log to upload.</param>
        /// <param name="lastError">The error message displayed to the user (if available).</param>
        private async void _StartIssueUpload(string log, string lastError = "not available")
        {
            // Upload log to github gist
            string gistUrl = await _UploadLogToGist(log, lastError);

            // Build issue body
            string template = string.Format(_IssueTemplate, _VocaluxeVersionTag, gistUrl);

            // Build url for github issue template
            string issueUrl = $"https://github.com/Vocaluxe/Vocaluxe/issues/new?title=Give%20me%20a%20meaningful%20title&body={ Uri.EscapeDataString(template) }";

            // Show the link
            _UploadFinished(issueUrl);

            // Start the browser with the issue template url
            System.Diagnostics.Process.Start(issueUrl);
        }

        /// <summary>
        /// Uploads a log to a new anonymous gist.
        /// </summary>
        /// <param name="log">The log that should be uploaded.</param>
        /// <param name="lastError">The error message displayed to the user (if available).</param>
        /// <returns>The link to the uploaded file.</returns>
        private async Task<string> _UploadLogToGist(string log, string lastError= "not available")
        {
            string json = JsonConvert.SerializeObject(new CGistCreateData()
            {
                Description = $"An { (_Crash ? "crash" : "error") } log submission for { _VocaluxeVersionTag }",
                Public = false,
                Files = new Dictionary<string, CGistFileData>()
                {
                    {
                        "Vocaluxe.log",
                        new CGistFileData()
                        {
                            Content = log
                        }
                    },
                    {
                        "LastError.txt",
                        new CGistFileData()
                        {
                            Content = lastError
                        }
                    }
                }
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            string responseString = "";
            try
            {
                var response = await _Client.PostAsync("https://api.github.com/gists", content);

                responseString = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                CLog.Error(e, "Couldn't upload log", show:true, propertyValues:CLog.Params(json));
            }

            return _GetGistUrlRegex.Match(responseString)?.Groups[1]?.Value??"";
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
