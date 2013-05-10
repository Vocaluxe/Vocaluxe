namespace ClientTest
{
    partial class ClientTest
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lbConnectionStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.lbConnectionStatusText = new System.Windows.Forms.ToolStripStatusLabel();
            this.tbDataSending = new System.Windows.Forms.TextBox();
            this.lbDateSending = new System.Windows.Forms.Label();
            this.lbDataReceiving = new System.Windows.Forms.Label();
            this.tbDataReceiving = new System.Windows.Forms.TextBox();
            this.lbPort = new System.Windows.Forms.Label();
            this.tbPort = new System.Windows.Forms.TextBox();
            this.tbServerIP = new System.Windows.Forms.TextBox();
            this.btConnect = new System.Windows.Forms.Button();
            this.lbServerIP = new System.Windows.Forms.Label();
            this.tbPassword = new System.Windows.Forms.TextBox();
            this.lbPassword = new System.Windows.Forms.Label();
            this.btLogin = new System.Windows.Forms.Button();
            this.btLeft = new System.Windows.Forms.Button();
            this.btRight = new System.Windows.Forms.Button();
            this.btUp = new System.Windows.Forms.Button();
            this.btDown = new System.Windows.Forms.Button();
            this.btSendAvatar = new System.Windows.Forms.Button();
            this.btSendProfile = new System.Windows.Forms.Button();
            this.btDiscover = new System.Windows.Forms.Button();
            this.tbDiscoveredServer = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lbConnectionStatus,
            this.lbConnectionStatusText});
            this.statusStrip1.Location = new System.Drawing.Point(0, 528);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1008, 22);
            this.statusStrip1.TabIndex = 4;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // lbConnectionStatus
            // 
            this.lbConnectionStatus.Name = "lbConnectionStatus";
            this.lbConnectionStatus.Size = new System.Drawing.Size(107, 17);
            this.lbConnectionStatus.Text = "Connection Status:";
            // 
            // lbConnectionStatusText
            // 
            this.lbConnectionStatusText.Name = "lbConnectionStatusText";
            this.lbConnectionStatusText.Size = new System.Drawing.Size(79, 17);
            this.lbConnectionStatusText.Text = "Disconnected";
            // 
            // tbDataSending
            // 
            this.tbDataSending.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDataSending.Location = new System.Drawing.Point(12, 25);
            this.tbDataSending.Multiline = true;
            this.tbDataSending.Name = "tbDataSending";
            this.tbDataSending.Size = new System.Drawing.Size(810, 120);
            this.tbDataSending.TabIndex = 7;
            // 
            // lbDateSending
            // 
            this.lbDateSending.AutoSize = true;
            this.lbDateSending.Location = new System.Drawing.Point(12, 9);
            this.lbDateSending.Name = "lbDateSending";
            this.lbDateSending.Size = new System.Drawing.Size(75, 13);
            this.lbDateSending.TabIndex = 6;
            this.lbDateSending.Text = "Data Sending:";
            // 
            // lbDataReceiving
            // 
            this.lbDataReceiving.AutoSize = true;
            this.lbDataReceiving.Location = new System.Drawing.Point(12, 197);
            this.lbDataReceiving.Name = "lbDataReceiving";
            this.lbDataReceiving.Size = new System.Drawing.Size(84, 13);
            this.lbDataReceiving.TabIndex = 8;
            this.lbDataReceiving.Text = "Data Receiving:";
            // 
            // tbDataReceiving
            // 
            this.tbDataReceiving.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDataReceiving.Location = new System.Drawing.Point(12, 213);
            this.tbDataReceiving.Multiline = true;
            this.tbDataReceiving.Name = "tbDataReceiving";
            this.tbDataReceiving.Size = new System.Drawing.Size(810, 120);
            this.tbDataReceiving.TabIndex = 9;
            // 
            // lbPort
            // 
            this.lbPort.AutoSize = true;
            this.lbPort.Location = new System.Drawing.Point(828, 41);
            this.lbPort.Name = "lbPort";
            this.lbPort.Size = new System.Drawing.Size(29, 13);
            this.lbPort.TabIndex = 1;
            this.lbPort.Text = "Port:";
            // 
            // tbPort
            // 
            this.tbPort.Location = new System.Drawing.Point(910, 38);
            this.tbPort.Name = "tbPort";
            this.tbPort.Size = new System.Drawing.Size(86, 20);
            this.tbPort.TabIndex = 3;
            this.tbPort.Text = "3000";
            // 
            // tbServerIP
            // 
            this.tbServerIP.Location = new System.Drawing.Point(910, 12);
            this.tbServerIP.Name = "tbServerIP";
            this.tbServerIP.Size = new System.Drawing.Size(86, 20);
            this.tbServerIP.TabIndex = 2;
            this.tbServerIP.Text = "127.0.0.1";
            // 
            // btConnect
            // 
            this.btConnect.Location = new System.Drawing.Point(831, 64);
            this.btConnect.Name = "btConnect";
            this.btConnect.Size = new System.Drawing.Size(165, 23);
            this.btConnect.TabIndex = 5;
            this.btConnect.Text = "Connect";
            this.btConnect.UseVisualStyleBackColor = true;
            this.btConnect.Click += new System.EventHandler(this.btConnect_Click);
            // 
            // lbServerIP
            // 
            this.lbServerIP.AutoSize = true;
            this.lbServerIP.Location = new System.Drawing.Point(828, 15);
            this.lbServerIP.Name = "lbServerIP";
            this.lbServerIP.Size = new System.Drawing.Size(54, 13);
            this.lbServerIP.TabIndex = 0;
            this.lbServerIP.Text = "Server IP:";
            // 
            // tbPassword
            // 
            this.tbPassword.Location = new System.Drawing.Point(910, 128);
            this.tbPassword.Name = "tbPassword";
            this.tbPassword.Size = new System.Drawing.Size(86, 20);
            this.tbPassword.TabIndex = 6;
            this.tbPassword.UseSystemPasswordChar = true;
            // 
            // lbPassword
            // 
            this.lbPassword.AutoSize = true;
            this.lbPassword.Location = new System.Drawing.Point(828, 131);
            this.lbPassword.Name = "lbPassword";
            this.lbPassword.Size = new System.Drawing.Size(56, 13);
            this.lbPassword.TabIndex = 7;
            this.lbPassword.Text = "Password:";
            // 
            // btLogin
            // 
            this.btLogin.Enabled = false;
            this.btLogin.Location = new System.Drawing.Point(831, 154);
            this.btLogin.Name = "btLogin";
            this.btLogin.Size = new System.Drawing.Size(165, 23);
            this.btLogin.TabIndex = 8;
            this.btLogin.Text = "Login";
            this.btLogin.UseVisualStyleBackColor = true;
            this.btLogin.Click += new System.EventHandler(this.btLogin_Click);
            // 
            // btLeft
            // 
            this.btLeft.Enabled = false;
            this.btLeft.Location = new System.Drawing.Point(831, 238);
            this.btLeft.Name = "btLeft";
            this.btLeft.Size = new System.Drawing.Size(69, 23);
            this.btLeft.TabIndex = 10;
            this.btLeft.Text = "Left";
            this.btLeft.UseVisualStyleBackColor = true;
            this.btLeft.Click += new System.EventHandler(this.btLeft_Click);
            // 
            // btRight
            // 
            this.btRight.Enabled = false;
            this.btRight.Location = new System.Drawing.Point(927, 238);
            this.btRight.Name = "btRight";
            this.btRight.Size = new System.Drawing.Size(69, 23);
            this.btRight.TabIndex = 11;
            this.btRight.Text = "Right";
            this.btRight.UseVisualStyleBackColor = true;
            this.btRight.Click += new System.EventHandler(this.brRight_Click);
            // 
            // btUp
            // 
            this.btUp.Enabled = false;
            this.btUp.Location = new System.Drawing.Point(877, 209);
            this.btUp.Name = "btUp";
            this.btUp.Size = new System.Drawing.Size(69, 23);
            this.btUp.TabIndex = 12;
            this.btUp.Text = "Up";
            this.btUp.UseVisualStyleBackColor = true;
            this.btUp.Click += new System.EventHandler(this.btUp_Click);
            // 
            // btDown
            // 
            this.btDown.Enabled = false;
            this.btDown.Location = new System.Drawing.Point(877, 267);
            this.btDown.Name = "btDown";
            this.btDown.Size = new System.Drawing.Size(69, 23);
            this.btDown.TabIndex = 13;
            this.btDown.Text = "Down";
            this.btDown.UseVisualStyleBackColor = true;
            this.btDown.Click += new System.EventHandler(this.btDown_Click);
            // 
            // btSendAvatar
            // 
            this.btSendAvatar.Enabled = false;
            this.btSendAvatar.Location = new System.Drawing.Point(831, 310);
            this.btSendAvatar.Name = "btSendAvatar";
            this.btSendAvatar.Size = new System.Drawing.Size(165, 23);
            this.btSendAvatar.TabIndex = 14;
            this.btSendAvatar.Text = "Send Avatar";
            this.btSendAvatar.UseVisualStyleBackColor = true;
            this.btSendAvatar.Click += new System.EventHandler(this.btSendAvatar_Click);
            // 
            // btSendProfile
            // 
            this.btSendProfile.Enabled = false;
            this.btSendProfile.Location = new System.Drawing.Point(831, 339);
            this.btSendProfile.Name = "btSendProfile";
            this.btSendProfile.Size = new System.Drawing.Size(165, 23);
            this.btSendProfile.TabIndex = 15;
            this.btSendProfile.Text = "Send Profile";
            this.btSendProfile.UseVisualStyleBackColor = true;
            this.btSendProfile.Click += new System.EventHandler(this.btSendProfile_Click);
            // 
            // btDiscover
            // 
            this.btDiscover.Location = new System.Drawing.Point(831, 502);
            this.btDiscover.Name = "btDiscover";
            this.btDiscover.Size = new System.Drawing.Size(165, 23);
            this.btDiscover.TabIndex = 16;
            this.btDiscover.Text = "Discover";
            this.btDiscover.UseVisualStyleBackColor = true;
            this.btDiscover.Click += new System.EventHandler(this.btDiscover_Click);
            // 
            // tbDiscoveredServer
            // 
            this.tbDiscoveredServer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDiscoveredServer.Location = new System.Drawing.Point(15, 405);
            this.tbDiscoveredServer.Multiline = true;
            this.tbDiscoveredServer.Name = "tbDiscoveredServer";
            this.tbDiscoveredServer.Size = new System.Drawing.Size(810, 120);
            this.tbDiscoveredServer.TabIndex = 17;
            this.tbDiscoveredServer.WordWrap = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 389);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 13);
            this.label1.TabIndex = 18;
            this.label1.Text = "Server:";
            // 
            // ClientTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 550);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbDiscoveredServer);
            this.Controls.Add(this.btDiscover);
            this.Controls.Add(this.btSendProfile);
            this.Controls.Add(this.btSendAvatar);
            this.Controls.Add(this.btDown);
            this.Controls.Add(this.btUp);
            this.Controls.Add(this.btRight);
            this.Controls.Add(this.btLeft);
            this.Controls.Add(this.btLogin);
            this.Controls.Add(this.tbDataReceiving);
            this.Controls.Add(this.lbPassword);
            this.Controls.Add(this.lbDataReceiving);
            this.Controls.Add(this.tbPassword);
            this.Controls.Add(this.lbDateSending);
            this.Controls.Add(this.lbServerIP);
            this.Controls.Add(this.tbDataSending);
            this.Controls.Add(this.btConnect);
            this.Controls.Add(this.tbServerIP);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.tbPort);
            this.Controls.Add(this.lbPort);
            this.Name = "ClientTest";
            this.Text = "ClientTest";
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lbConnectionStatus;
        private System.Windows.Forms.ToolStripStatusLabel lbConnectionStatusText;
        private System.Windows.Forms.TextBox tbDataSending;
        private System.Windows.Forms.Label lbDateSending;
        private System.Windows.Forms.Label lbDataReceiving;
        private System.Windows.Forms.TextBox tbDataReceiving;
        private System.Windows.Forms.Label lbPort;
        private System.Windows.Forms.TextBox tbPort;
        private System.Windows.Forms.TextBox tbServerIP;
        private System.Windows.Forms.Button btConnect;
        private System.Windows.Forms.Label lbServerIP;
        private System.Windows.Forms.TextBox tbPassword;
        private System.Windows.Forms.Label lbPassword;
        private System.Windows.Forms.Button btLogin;
        private System.Windows.Forms.Button btLeft;
        private System.Windows.Forms.Button btRight;
        private System.Windows.Forms.Button btUp;
        private System.Windows.Forms.Button btDown;
        private System.Windows.Forms.Button btSendAvatar;
        private System.Windows.Forms.Button btSendProfile;
        private System.Windows.Forms.Button btDiscover;
        private System.Windows.Forms.TextBox tbDiscoveredServer;
        private System.Windows.Forms.Label label1;
    }
}

