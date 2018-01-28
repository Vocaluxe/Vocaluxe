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
            this.TitleLabel = new System.Windows.Forms.Label();
            this.MessageText = new System.Windows.Forms.TextBox();
            this.LogBox = new System.Windows.Forms.TextBox();
            this.GistOnlySelect = new System.Windows.Forms.RadioButton();
            this.GistAndIssueSelect = new System.Windows.Forms.RadioButton();
            this.NoUploadSelect = new System.Windows.Forms.RadioButton();
            this.SubmitButton = new System.Windows.Forms.Button();
            this.Url = new System.Windows.Forms.TextBox();
            this.SubmitedTitleLabel = new System.Windows.Forms.Label();
            this.LastErrorTitleLabel = new System.Windows.Forms.Label();
            this.LastErrorBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // TitleLabel
            // 
            this.TitleLabel.AutoSize = true;
            this.TitleLabel.Enabled = false;
            this.TitleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TitleLabel.Location = new System.Drawing.Point(9, 8);
            this.TitleLabel.Name = "TitleLabel";
            this.TitleLabel.Size = new System.Drawing.Size(110, 20);
            this.TitleLabel.TabIndex = 0;
            this.TitleLabel.Text = "We are sorry";
            // 
            // MessageText
            // 
            this.MessageText.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.MessageText.Enabled = false;
            this.MessageText.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MessageText.Location = new System.Drawing.Point(13, 31);
            this.MessageText.Multiline = true;
            this.MessageText.Name = "MessageText";
            this.MessageText.ReadOnly = true;
            this.MessageText.Size = new System.Drawing.Size(625, 63);
            this.MessageText.TabIndex = 0;
            this.MessageText.TabStop = false;
            this.MessageText.Text = "that Vocaluxe run into an error and crashed.\r\nTo help us fix the problem please s" +
    "end us a report.\r\nEdit the following log before submitting to remove possibly se" +
    "nsitive information.";
            // 
            // LogBox
            // 
            this.LogBox.AcceptsReturn = true;
            this.LogBox.AcceptsTab = true;
            this.LogBox.Location = new System.Drawing.Point(13, 182);
            this.LogBox.Multiline = true;
            this.LogBox.Name = "LogBox";
            this.LogBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.LogBox.Size = new System.Drawing.Size(625, 189);
            this.LogBox.TabIndex = 1;
            this.LogBox.WordWrap = false;
            // 
            // GistOnlySelect
            // 
            this.GistOnlySelect.AutoSize = true;
            this.GistOnlySelect.Checked = true;
            this.GistOnlySelect.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GistOnlySelect.Location = new System.Drawing.Point(12, 377);
            this.GistOnlySelect.Name = "GistOnlySelect";
            this.GistOnlySelect.Size = new System.Drawing.Size(385, 24);
            this.GistOnlySelect.TabIndex = 2;
            this.GistOnlySelect.TabStop = true;
            this.GistOnlySelect.Text = "Upload and get a link to your report (publicly visible)";
            this.GistOnlySelect.UseVisualStyleBackColor = true;
            // 
            // GistAndIssueSelect
            // 
            this.GistAndIssueSelect.AutoSize = true;
            this.GistAndIssueSelect.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GistAndIssueSelect.Location = new System.Drawing.Point(12, 407);
            this.GistAndIssueSelect.Name = "GistAndIssueSelect";
            this.GistAndIssueSelect.Size = new System.Drawing.Size(519, 24);
            this.GistAndIssueSelect.TabIndex = 3;
            this.GistAndIssueSelect.TabStop = true;
            this.GistAndIssueSelect.Text = "Upload and open an issue (publicly visible + requires a github account)";
            this.GistAndIssueSelect.UseVisualStyleBackColor = true;
            // 
            // NoUploadSelect
            // 
            this.NoUploadSelect.AutoSize = true;
            this.NoUploadSelect.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.NoUploadSelect.Location = new System.Drawing.Point(12, 437);
            this.NoUploadSelect.Name = "NoUploadSelect";
            this.NoUploadSelect.Size = new System.Drawing.Size(493, 24);
            this.NoUploadSelect.TabIndex = 4;
            this.NoUploadSelect.TabStop = true;
            this.NoUploadSelect.Text = "Don\'t upload anything (you can still copy the error message above)";
            this.NoUploadSelect.UseVisualStyleBackColor = true;
            this.NoUploadSelect.CheckedChanged += new System.EventHandler(this.NoUpload_CheckedChanged);
            // 
            // SubmitButton
            // 
            this.SubmitButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SubmitButton.Location = new System.Drawing.Point(537, 388);
            this.SubmitButton.Name = "SubmitButton";
            this.SubmitButton.Size = new System.Drawing.Size(101, 73);
            this.SubmitButton.TabIndex = 5;
            this.SubmitButton.Text = "Submit";
            this.SubmitButton.UseVisualStyleBackColor = true;
            this.SubmitButton.Click += new System.EventHandler(this.Submit_Click);
            // 
            // Url
            // 
            this.Url.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Url.Location = new System.Drawing.Point(12, 409);
            this.Url.Name = "Url";
            this.Url.ReadOnly = true;
            this.Url.Size = new System.Drawing.Size(519, 22);
            this.Url.TabIndex = 6;
            this.Url.Text = "https://gist.github.com/lukeIam/cc81f7ef3545ef2baf0ca9a28ffe4bd8";
            this.Url.Visible = false;
            // 
            // SubmitedTitleLabel
            // 
            this.SubmitedTitleLabel.AutoSize = true;
            this.SubmitedTitleLabel.Enabled = false;
            this.SubmitedTitleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SubmitedTitleLabel.Location = new System.Drawing.Point(9, 381);
            this.SubmitedTitleLabel.Name = "SubmitedTitleLabel";
            this.SubmitedTitleLabel.Size = new System.Drawing.Size(189, 20);
            this.SubmitedTitleLabel.TabIndex = 8;
            this.SubmitedTitleLabel.Text = "The link to your report:";
            this.SubmitedTitleLabel.Visible = false;
            // 
            // LastErrorTitleLabel
            // 
            this.LastErrorTitleLabel.AutoSize = true;
            this.LastErrorTitleLabel.Enabled = false;
            this.LastErrorTitleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LastErrorTitleLabel.Location = new System.Drawing.Point(9, 97);
            this.LastErrorTitleLabel.Name = "LastErrorTitleLabel";
            this.LastErrorTitleLabel.Size = new System.Drawing.Size(87, 20);
            this.LastErrorTitleLabel.TabIndex = 9;
            this.LastErrorTitleLabel.Text = "Last error";
            // 
            // LastErrorBox
            // 
            this.LastErrorBox.AcceptsReturn = true;
            this.LastErrorBox.AcceptsTab = true;
            this.LastErrorBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LastErrorBox.Location = new System.Drawing.Point(12, 120);
            this.LastErrorBox.Multiline = true;
            this.LastErrorBox.Name = "LastErrorBox";
            this.LastErrorBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.LastErrorBox.Size = new System.Drawing.Size(626, 38);
            this.LastErrorBox.TabIndex = 10;
            // 
            // CReporter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(650, 467);
            this.Controls.Add(this.LastErrorBox);
            this.Controls.Add(this.LastErrorTitleLabel);
            this.Controls.Add(this.SubmitedTitleLabel);
            this.Controls.Add(this.Url);
            this.Controls.Add(this.SubmitButton);
            this.Controls.Add(this.NoUploadSelect);
            this.Controls.Add(this.GistAndIssueSelect);
            this.Controls.Add(this.GistOnlySelect);
            this.Controls.Add(this.LogBox);
            this.Controls.Add(this.MessageText);
            this.Controls.Add(this.TitleLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CReporter";
            this.Text = "Vocaluxe Issue Reporter";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label TitleLabel;
        private System.Windows.Forms.TextBox MessageText;
        private System.Windows.Forms.TextBox LogBox;
        private System.Windows.Forms.RadioButton GistOnlySelect;
        private System.Windows.Forms.RadioButton GistAndIssueSelect;
        private System.Windows.Forms.RadioButton NoUploadSelect;
        private System.Windows.Forms.Button SubmitButton;
        private System.Windows.Forms.TextBox Url;
        private System.Windows.Forms.Label SubmitedTitleLabel;
        private System.Windows.Forms.Label LastErrorTitleLabel;
        private System.Windows.Forms.TextBox LastErrorBox;
    }
}