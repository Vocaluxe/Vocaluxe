namespace Vocaluxe.Reporting
{
    partial class CReporter
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CReporter));
            this.Title = new System.Windows.Forms.Label();
            this.Message = new System.Windows.Forms.TextBox();
            this.Log = new System.Windows.Forms.TextBox();
            this.GistOnly = new System.Windows.Forms.RadioButton();
            this.GistAndIssue = new System.Windows.Forms.RadioButton();
            this.NoUpload = new System.Windows.Forms.RadioButton();
            this.Submit = new System.Windows.Forms.Button();
            this.Url = new System.Windows.Forms.TextBox();
            this.SubmitedMessage = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // Title
            // 
            this.Title.AutoSize = true;
            this.Title.Enabled = false;
            this.Title.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Title.Location = new System.Drawing.Point(9, 8);
            this.Title.Name = "Title";
            this.Title.Size = new System.Drawing.Size(110, 20);
            this.Title.TabIndex = 0;
            this.Title.Text = "We are sorry";
            // 
            // Message
            // 
            this.Message.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.Message.Enabled = false;
            this.Message.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Message.Location = new System.Drawing.Point(13, 31);
            this.Message.Multiline = true;
            this.Message.Name = "Message";
            this.Message.ReadOnly = true;
            this.Message.Size = new System.Drawing.Size(625, 63);
            this.Message.TabIndex = 0;
            this.Message.TabStop = false;
            this.Message.Text = "that Vocaluxe run into an error and crashed.\r\nTo help us fix the problem please s" +
    "end us a report.\r\nEdit the following log before submitting to remove possibly se" +
    "nsitive information.";
            // 
            // Log
            // 
            this.Log.AcceptsReturn = true;
            this.Log.AcceptsTab = true;
            this.Log.Location = new System.Drawing.Point(13, 100);
            this.Log.Multiline = true;
            this.Log.Name = "Log";
            this.Log.Size = new System.Drawing.Size(625, 189);
            this.Log.TabIndex = 1;
            // 
            // GistOnly
            // 
            this.GistOnly.AutoSize = true;
            this.GistOnly.Checked = true;
            this.GistOnly.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GistOnly.Location = new System.Drawing.Point(12, 295);
            this.GistOnly.Name = "GistOnly";
            this.GistOnly.Size = new System.Drawing.Size(385, 24);
            this.GistOnly.TabIndex = 2;
            this.GistOnly.TabStop = true;
            this.GistOnly.Text = "Upload and get a link to your report (publicly visible)";
            this.GistOnly.UseVisualStyleBackColor = true;
            // 
            // GistAndIssue
            // 
            this.GistAndIssue.AutoSize = true;
            this.GistAndIssue.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GistAndIssue.Location = new System.Drawing.Point(12, 325);
            this.GistAndIssue.Name = "GistAndIssue";
            this.GistAndIssue.Size = new System.Drawing.Size(519, 24);
            this.GistAndIssue.TabIndex = 3;
            this.GistAndIssue.TabStop = true;
            this.GistAndIssue.Text = "Upload and open an issue (publicly visible + requires a github account)";
            this.GistAndIssue.UseVisualStyleBackColor = true;
            // 
            // NoUpload
            // 
            this.NoUpload.AutoSize = true;
            this.NoUpload.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.NoUpload.Location = new System.Drawing.Point(12, 355);
            this.NoUpload.Name = "NoUpload";
            this.NoUpload.Size = new System.Drawing.Size(493, 24);
            this.NoUpload.TabIndex = 4;
            this.NoUpload.TabStop = true;
            this.NoUpload.Text = "Don\'t upload anything (you can still copy the error message above)";
            this.NoUpload.UseVisualStyleBackColor = true;
            this.NoUpload.CheckedChanged += new System.EventHandler(this.NoUpload_CheckedChanged);
            // 
            // Submit
            // 
            this.Submit.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Submit.Location = new System.Drawing.Point(537, 306);
            this.Submit.Name = "Submit";
            this.Submit.Size = new System.Drawing.Size(101, 73);
            this.Submit.TabIndex = 5;
            this.Submit.Text = "Submit";
            this.Submit.UseVisualStyleBackColor = true;
            this.Submit.Click += new System.EventHandler(this.Submit_Click);
            // 
            // Url
            // 
            this.Url.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Url.Location = new System.Drawing.Point(12, 327);
            this.Url.Name = "Url";
            this.Url.ReadOnly = true;
            this.Url.Size = new System.Drawing.Size(519, 22);
            this.Url.TabIndex = 6;
            this.Url.Text = "https://gist.github.com/lukeIam/cc81f7ef3545ef2baf0ca9a28ffe4bd8";
            this.Url.Visible = false;
            // 
            // SubmitedMessage
            // 
            this.SubmitedMessage.AutoSize = true;
            this.SubmitedMessage.Enabled = false;
            this.SubmitedMessage.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SubmitedMessage.Location = new System.Drawing.Point(9, 299);
            this.SubmitedMessage.Name = "SubmitedMessage";
            this.SubmitedMessage.Size = new System.Drawing.Size(189, 20);
            this.SubmitedMessage.TabIndex = 8;
            this.SubmitedMessage.Text = "The link to your report:";
            this.SubmitedMessage.Visible = false;
            // 
            // CReporter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(650, 387);
            this.Controls.Add(this.SubmitedMessage);
            this.Controls.Add(this.Url);
            this.Controls.Add(this.Submit);
            this.Controls.Add(this.NoUpload);
            this.Controls.Add(this.GistAndIssue);
            this.Controls.Add(this.GistOnly);
            this.Controls.Add(this.Log);
            this.Controls.Add(this.Message);
            this.Controls.Add(this.Title);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "CReporter";
            this.Text = "Vocaluxe Issue Reporter";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Title;
        private System.Windows.Forms.TextBox Message;
        private System.Windows.Forms.TextBox Log;
        private System.Windows.Forms.RadioButton GistOnly;
        private System.Windows.Forms.RadioButton GistAndIssue;
        private System.Windows.Forms.RadioButton NoUpload;
        private System.Windows.Forms.Button Submit;
        private System.Windows.Forms.TextBox Url;
        private System.Windows.Forms.Label SubmitedMessage;
    }
}