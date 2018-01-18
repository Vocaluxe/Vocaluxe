using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using VocaluxeLib.Log;

namespace Vocaluxe.Reporting
{
    public partial class CReporter : Form
    {
        private string _TitleText = "We are sorry";
        private string _MessageCrashText = "that Vocaluxe run into an error and crashed.\r\nTo help us fix the problem please send us a report.\r\nEdit the following log before submitting to remove possibly sensitive information.";
        private string _MessageNoCrashText = "that you experienced an error.\r\nTo help us fix the problem please send us a report.\r\nEdit the following log before submitting to remove possibly sensitive information.";
        private string _NoUploadText = "Don\'t upload anything (you can still copy the error message above)";
        private string _GistAndIssueText = "Upload and open an issue (publicly visible + requires a github account)";
        private string _GistOnlyText = "Upload and get a link to your report (publicly visible)";
        private string _SubmitStep0Text = "Submit";
        private string _SubmitStep1Text = "Uploading";
        private string _SubmitStep2CrashText = "Exit";
        private string _SubmitStep2NoCrashText = "Continue";
        private string _SubmitedMessageText = "The link to your report:";
        private string _LogUploadErrorText = "Error uploading the log.";


        private string _IssueTemplate = "Describe your issue here.\n\n### Steps to reproduce\nTell us how to reproduce this issue.\n\n### Vocaluxe version and logfile\n{0}\n{1}";

        private int _Step = 0;
        private readonly bool _Crash;
        private readonly string _VocaluxeVersionTag;
        private readonly bool _ShowContinue;

        private static readonly HttpClient _Client = new HttpClient();
        private static readonly Regex _GetGistUrlRegex = new Regex("\"raw_url\": *\"([^\"]+)\"");
        

        public static ShowReporterDelegate ShowReporterFunc
        {
            get { return _ShowReporter; }
        }

        private static void _ShowReporter(bool crash, bool showContinue, string vocaluxeVersionTag, string log)
        {
            using (var reporter = new CReporter(crash, showContinue, vocaluxeVersionTag, log))
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
        public CReporter(bool crash, bool showContinue, string vocaluxeVersionTag, string log)
        {
            InitializeComponent();
            _Crash = crash;
            _ShowContinue = showContinue;
            _VocaluxeVersionTag = vocaluxeVersionTag;
            _Init(log);
        }

        /// <summary>
        /// Initilaize the gui.
        /// </summary>
        /// <param name="log">The Log to submit.</param>
        private void _Init(string log)
        {
            _Client.DefaultRequestHeaders.Add("accept", "application/json");
            _Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/6.0)");

            this.Title.Text = _TitleText;
            this.Message.Text = _Crash ? _MessageCrashText : _MessageNoCrashText;
            this.NoUpload.Text = _NoUploadText;
            this.GistAndIssue.Text = _GistAndIssueText;
            this.GistOnly.Text = _GistOnlyText;
            this.Submit.Text = _SubmitStep0Text;
            this.SubmitedMessage.Text = _SubmitedMessageText;
            this.Log.Text = log.Replace("\n","\r\n");
            this.Log.SelectionStart = 0;
        }

        private async void Submit_Click(object sender, EventArgs e)
        {
            switch (_Step)
            {
                case 0:
                    if (this.NoUpload.Checked)
                    {
                        this.Close();
                    }
                    else
                    {
                        _Step = 1;
                        this.Submit.Text = _SubmitStep1Text;
                        this.Submit.Enabled = false;
                        this.NoUpload.Visible = false;
                        this.GistAndIssue.Visible = false;
                        this.GistOnly.Visible = false;
                        this.Log.Enabled = false;

                        string url;
                        if (this.GistOnly.Checked)
                        {
                            await Task.Run(() => _StartGistUpload(this.Log.Text));
                        }
                        else if (this.GistAndIssue.Checked)
                        {
                            await Task.Run(() => _StartIssueUpload(this.Log.Text));
                        }
                        else
                        {
                            _Step = 0;
                            this.Submit.Text = _SubmitStep0Text;
                            this.Submit.Enabled = true;
                            this.NoUpload.Visible = true;
                            this.GistAndIssue.Visible = true;
                            this.GistOnly.Visible = true;
                            this.Log.Enabled = true;
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
            this.Submit.Text = this.NoUpload.Checked ? (_ShowContinue ? _SubmitStep2CrashText : _SubmitStep2NoCrashText) : _SubmitStep0Text;
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
                this.Submit.Text = _ShowContinue ? _SubmitStep2CrashText : _SubmitStep2NoCrashText;
                this.Submit.Enabled = true;
                this.SubmitedMessage.Visible = true;
                this.Url.Text = string.IsNullOrWhiteSpace(url)?_LogUploadErrorText:url;
                this.Url.Visible = true;
                this.Url.SelectAll();
            }
        }

        /// <summary>
        /// Upload a log to gist and updates the gui.
        /// </summary>
        /// <param name="log">The log to upload.</param>
        private async void _StartGistUpload(string log)
        {
            // Upload log to github gist and show the link
            _UploadFinished(await _UploadLogToGist(log));
        }

        /// <summary>
        /// Upload a log to gist, opens an issue template on github and updates the gui.
        /// </summary>
        /// <param name="log">The log to upload.</param>
        private async void _StartIssueUpload(string log)
        {
            // Upload log to github gist
            string gistUrl = await _UploadLogToGist(log);

            // Build issue body
            string template = string.Format(_IssueTemplate, _VocaluxeVersionTag, gistUrl);

            // Build url for github issue template
            string issueUrl = $"https://github.com/Vocaluxe/Vocaluxe/issues/new?title=Give%20me%20a%20meaningful%20title&body={ Uri.EscapeDataString(template) }";

            // Start the browser with the issue template url
            System.Diagnostics.Process.Start(issueUrl);

            // Show the link
            _UploadFinished(issueUrl);
        }

        /// <summary>
        /// Uploads a log to a new anonymous gist.
        /// </summary>
        /// <param name="log">The log that should be uploaded.</param>
        /// <returns>The link to the uploaded file.</returns>
        private async Task<string> _UploadLogToGist(string log)
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
