namespace MusicBeePlugin
{
    partial class Configure
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Configure));
            this.loginButton = new System.Windows.Forms.Button();
            this.syncNowButton = new System.Windows.Forms.Button();
            this.localPlaylistBox = new System.Windows.Forms.CheckedListBox();
            this.googleMusicPlaylistBox = new System.Windows.Forms.CheckedListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.allLocalPlayCheckbox = new System.Windows.Forms.CheckBox();
            this.allRemotePlayCheckbox = new System.Windows.Forms.CheckBox();
            this.toGMusicRadiobutton = new System.Windows.Forms.RadioButton();
            this.fromGMusicRadioButton = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.includeFoldersInNameCheckBox = new System.Windows.Forms.CheckBox();
            this.outputTextBox = new System.Windows.Forms.RichTextBox();
            this.includeZInDatePlaylistsCheckbox = new System.Windows.Forms.CheckBox();
            this.zAtDatePlaylistToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.gpmTab = new System.Windows.Forms.TabControl();
            this.GoogleTab = new System.Windows.Forms.TabPage();
            this.SpotifyTab = new System.Windows.Forms.TabPage();
            this.groupBox1.SuspendLayout();
            this.gpmTab.SuspendLayout();
            this.GoogleTab.SuspendLayout();
            this.SuspendLayout();
            // 
            // loginButton
            // 
            this.loginButton.Location = new System.Drawing.Point(9, 42);
            this.loginButton.Margin = new System.Windows.Forms.Padding(6);
            this.loginButton.Name = "loginButton";
            this.loginButton.Size = new System.Drawing.Size(284, 44);
            this.loginButton.TabIndex = 0;
            this.loginButton.Text = "Login to Google";
            this.loginButton.UseVisualStyleBackColor = true;
            this.loginButton.Click += new System.EventHandler(this.loginButton_Click);
            // 
            // syncNowButton
            // 
            this.syncNowButton.Enabled = false;
            this.syncNowButton.Location = new System.Drawing.Point(831, 42);
            this.syncNowButton.Margin = new System.Windows.Forms.Padding(6);
            this.syncNowButton.Name = "syncNowButton";
            this.syncNowButton.Size = new System.Drawing.Size(284, 44);
            this.syncNowButton.TabIndex = 8;
            this.syncNowButton.Text = "Synchronise Selected";
            this.syncNowButton.UseVisualStyleBackColor = true;
            this.syncNowButton.Click += new System.EventHandler(this.syncNowButton_Click);
            // 
            // localPlaylistBox
            // 
            this.localPlaylistBox.CheckOnClick = true;
            this.localPlaylistBox.FormattingEnabled = true;
            this.localPlaylistBox.Location = new System.Drawing.Point(12, 88);
            this.localPlaylistBox.Margin = new System.Windows.Forms.Padding(6);
            this.localPlaylistBox.Name = "localPlaylistBox";
            this.localPlaylistBox.Size = new System.Drawing.Size(526, 676);
            this.localPlaylistBox.TabIndex = 11;
            this.localPlaylistBox.SelectedIndexChanged += new System.EventHandler(this.localPlaylistBox_SelectedIndexChanged);
            // 
            // googleMusicPlaylistBox
            // 
            this.googleMusicPlaylistBox.CheckOnClick = true;
            this.googleMusicPlaylistBox.FormattingEnabled = true;
            this.googleMusicPlaylistBox.Location = new System.Drawing.Point(564, 88);
            this.googleMusicPlaylistBox.Margin = new System.Windows.Forms.Padding(6);
            this.googleMusicPlaylistBox.Name = "googleMusicPlaylistBox";
            this.googleMusicPlaylistBox.Size = new System.Drawing.Size(526, 676);
            this.googleMusicPlaylistBox.TabIndex = 12;
            this.googleMusicPlaylistBox.SelectedIndexChanged += new System.EventHandler(this.googleMusicPlaylistBox_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 46);
            this.label1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(148, 25);
            this.label1.TabIndex = 13;
            this.label1.Text = "Local playlists";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(558, 46);
            this.label2.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(228, 25);
            this.label2.TabIndex = 14;
            this.label2.Text = "Google Music playlists";
            // 
            // allLocalPlayCheckbox
            // 
            this.allLocalPlayCheckbox.AutoSize = true;
            this.allLocalPlayCheckbox.Location = new System.Drawing.Point(398, 44);
            this.allLocalPlayCheckbox.Margin = new System.Windows.Forms.Padding(6);
            this.allLocalPlayCheckbox.Name = "allLocalPlayCheckbox";
            this.allLocalPlayCheckbox.Size = new System.Drawing.Size(134, 29);
            this.allLocalPlayCheckbox.TabIndex = 15;
            this.allLocalPlayCheckbox.Text = "Select All";
            this.allLocalPlayCheckbox.UseVisualStyleBackColor = true;
            this.allLocalPlayCheckbox.CheckedChanged += new System.EventHandler(this.allLocalPlayCheckbox_CheckedChanged);
            // 
            // allRemotePlayCheckbox
            // 
            this.allRemotePlayCheckbox.AutoSize = true;
            this.allRemotePlayCheckbox.Location = new System.Drawing.Point(954, 44);
            this.allRemotePlayCheckbox.Margin = new System.Windows.Forms.Padding(6);
            this.allRemotePlayCheckbox.Name = "allRemotePlayCheckbox";
            this.allRemotePlayCheckbox.Size = new System.Drawing.Size(134, 29);
            this.allRemotePlayCheckbox.TabIndex = 16;
            this.allRemotePlayCheckbox.Text = "Select All";
            this.allRemotePlayCheckbox.UseVisualStyleBackColor = true;
            this.allRemotePlayCheckbox.CheckedChanged += new System.EventHandler(this.allRemotePlayCheckbox_CheckedChanged);
            // 
            // toGMusicRadiobutton
            // 
            this.toGMusicRadiobutton.AutoSize = true;
            this.toGMusicRadiobutton.Checked = true;
            this.toGMusicRadiobutton.Location = new System.Drawing.Point(9, 110);
            this.toGMusicRadiobutton.Margin = new System.Windows.Forms.Padding(6);
            this.toGMusicRadiobutton.Name = "toGMusicRadiobutton";
            this.toGMusicRadiobutton.Size = new System.Drawing.Size(231, 29);
            this.toGMusicRadiobutton.TabIndex = 18;
            this.toGMusicRadiobutton.TabStop = true;
            this.toGMusicRadiobutton.Text = "-> To Google Music";
            this.toGMusicRadiobutton.UseVisualStyleBackColor = true;
            this.toGMusicRadiobutton.CheckedChanged += new System.EventHandler(this.toGMusicRadiobutton_CheckedChanged);
            // 
            // fromGMusicRadioButton
            // 
            this.fromGMusicRadioButton.AutoSize = true;
            this.fromGMusicRadioButton.Location = new System.Drawing.Point(9, 151);
            this.fromGMusicRadioButton.Margin = new System.Windows.Forms.Padding(6);
            this.fromGMusicRadioButton.Name = "fromGMusicRadioButton";
            this.fromGMusicRadioButton.Size = new System.Drawing.Size(255, 29);
            this.fromGMusicRadioButton.TabIndex = 19;
            this.fromGMusicRadioButton.Text = "<- From Google Music";
            this.fromGMusicRadioButton.UseVisualStyleBackColor = true;
            this.fromGMusicRadioButton.CheckedChanged += new System.EventHandler(this.fromGMusicRadioButton_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.allLocalPlayCheckbox);
            this.groupBox1.Controls.Add(this.localPlaylistBox);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.allRemotePlayCheckbox);
            this.groupBox1.Controls.Add(this.googleMusicPlaylistBox);
            this.groupBox1.Location = new System.Drawing.Point(9, 192);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(6);
            this.groupBox1.Size = new System.Drawing.Size(1106, 788);
            this.groupBox1.TabIndex = 21;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Set Up Sync";
            // 
            // includeFoldersInNameCheckBox
            // 
            this.includeFoldersInNameCheckBox.AutoSize = true;
            this.includeFoldersInNameCheckBox.Location = new System.Drawing.Point(282, 110);
            this.includeFoldersInNameCheckBox.Name = "includeFoldersInNameCheckBox";
            this.includeFoldersInNameCheckBox.Size = new System.Drawing.Size(351, 29);
            this.includeFoldersInNameCheckBox.TabIndex = 22;
            this.includeFoldersInNameCheckBox.Text = "Include Folders in Playlist Name";
            this.includeFoldersInNameCheckBox.UseVisualStyleBackColor = true;
            this.includeFoldersInNameCheckBox.CheckedChanged += new System.EventHandler(this.includeFoldersInNameCheckBox_CheckedChanged);
            // 
            // outputTextBox
            // 
            this.outputTextBox.Location = new System.Drawing.Point(12, 1058);
            this.outputTextBox.Name = "outputTextBox";
            this.outputTextBox.Size = new System.Drawing.Size(1146, 468);
            this.outputTextBox.TabIndex = 23;
            this.outputTextBox.Text = "";
            // 
            // includeZInDatePlaylistsCheckbox
            // 
            this.includeZInDatePlaylistsCheckbox.AutoSize = true;
            this.includeZInDatePlaylistsCheckbox.Checked = true;
            this.includeZInDatePlaylistsCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.includeZInDatePlaylistsCheckbox.Location = new System.Drawing.Point(282, 152);
            this.includeZInDatePlaylistsCheckbox.Name = "includeZInDatePlaylistsCheckbox";
            this.includeZInDatePlaylistsCheckbox.Size = new System.Drawing.Size(365, 29);
            this.includeZInDatePlaylistsCheckbox.TabIndex = 24;
            this.includeZInDatePlaylistsCheckbox.Text = "Include Z at start of Date Playlists";
            this.zAtDatePlaylistToolTip.SetToolTip(this.includeZInDatePlaylistsCheckbox, resources.GetString("includeZInDatePlaylistsCheckbox.ToolTip"));
            this.includeZInDatePlaylistsCheckbox.UseVisualStyleBackColor = true;
            this.includeZInDatePlaylistsCheckbox.CheckedChanged += new System.EventHandler(this.includeZInDatePlaylistsCheckbox_CheckedChanged);
            // 
            // gpmTab
            // 
            this.gpmTab.Controls.Add(this.GoogleTab);
            this.gpmTab.Controls.Add(this.SpotifyTab);
            this.gpmTab.Location = new System.Drawing.Point(12, 12);
            this.gpmTab.Name = "gpmTab";
            this.gpmTab.SelectedIndex = 0;
            this.gpmTab.Size = new System.Drawing.Size(1146, 1040);
            this.gpmTab.TabIndex = 25;
            // 
            // GoogleTab
            // 
            this.GoogleTab.Controls.Add(this.loginButton);
            this.GoogleTab.Controls.Add(this.includeZInDatePlaylistsCheckbox);
            this.GoogleTab.Controls.Add(this.groupBox1);
            this.GoogleTab.Controls.Add(this.syncNowButton);
            this.GoogleTab.Controls.Add(this.includeFoldersInNameCheckBox);
            this.GoogleTab.Controls.Add(this.toGMusicRadiobutton);
            this.GoogleTab.Controls.Add(this.fromGMusicRadioButton);
            this.GoogleTab.Location = new System.Drawing.Point(8, 39);
            this.GoogleTab.Name = "GoogleTab";
            this.GoogleTab.Padding = new System.Windows.Forms.Padding(3);
            this.GoogleTab.Size = new System.Drawing.Size(1130, 993);
            this.GoogleTab.TabIndex = 0;
            this.GoogleTab.Text = "Google";
            this.GoogleTab.UseVisualStyleBackColor = true;
            // 
            // SpotifyTab
            // 
            this.SpotifyTab.Location = new System.Drawing.Point(8, 39);
            this.SpotifyTab.Name = "SpotifyTab";
            this.SpotifyTab.Padding = new System.Windows.Forms.Padding(3);
            this.SpotifyTab.Size = new System.Drawing.Size(1130, 993);
            this.SpotifyTab.TabIndex = 1;
            this.SpotifyTab.Text = "Spotify";
            this.SpotifyTab.UseVisualStyleBackColor = true;
            // 
            // Configure
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1170, 1538);
            this.Controls.Add(this.gpmTab);
            this.Controls.Add(this.outputTextBox);
            this.Margin = new System.Windows.Forms.Padding(6);
            this.Name = "Configure";
            this.Text = "Google Music Sync";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Configure_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.gpmTab.ResumeLayout(false);
            this.GoogleTab.ResumeLayout(false);
            this.GoogleTab.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button loginButton;
        private System.Windows.Forms.Button syncNowButton;
        private System.Windows.Forms.CheckedListBox localPlaylistBox;
        private System.Windows.Forms.CheckedListBox googleMusicPlaylistBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox allLocalPlayCheckbox;
        private System.Windows.Forms.CheckBox allRemotePlayCheckbox;
        private System.Windows.Forms.RadioButton toGMusicRadiobutton;
        private System.Windows.Forms.RadioButton fromGMusicRadioButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox includeFoldersInNameCheckBox;
        private System.Windows.Forms.RichTextBox outputTextBox;
        private System.Windows.Forms.CheckBox includeZInDatePlaylistsCheckbox;
        private System.Windows.Forms.ToolTip zAtDatePlaylistToolTip;
        private System.Windows.Forms.TabControl gpmTab;
        private System.Windows.Forms.TabPage GoogleTab;
        private System.Windows.Forms.TabPage SpotifyTab;
    }
}