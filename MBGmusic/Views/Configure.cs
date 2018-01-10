using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Configuration;
using MusicBeePlugin.Models;
using System.Threading.Tasks;
using GooglePlayMusicAPI;
using System.Security;
using GooglePlayMusicAPI.Models.GooglePlayMusicModels;

namespace MusicBeePlugin
{
    partial class Configure : Form, IDisposable
    {

        private PlaylistSync _playlistSync;

        private Settings _settings;

        private Logger log;

        private void WriteLine(string text)
        {
            outputTextBox.Text += $"{text}\n";
        }

        public Configure(PlaylistSync playlistSync, Settings settings, Plugin.MusicBeeApiInterface mbApiInterface)
        {
            InitializeComponent();

            foreach (Control control in this.Controls)
            {
                control.ForeColor = Color.FromArgb(mbApiInterface.Setting_GetSkinElementColour(
                    MusicBeePlugin.Plugin.SkinElement.SkinInputPanel,
                    MusicBeePlugin.Plugin.ElementState.ElementStateDefault,
                    MusicBeePlugin.Plugin.ElementComponent.ComponentForeground));
                control.BackColor = Color.FromArgb(mbApiInterface.Setting_GetSkinElementColour(
                    MusicBeePlugin.Plugin.SkinElement.SkinInputControl,
                    MusicBeePlugin.Plugin.ElementState.ElementStateDefault,
                    MusicBeePlugin.Plugin.ElementComponent.ComponentBackground));

                if (control.Controls.Count > 0)
                {
                    foreach (Control child in control.Controls)
                    {
                        child.ForeColor = Color.FromArgb(mbApiInterface.Setting_GetSkinElementColour(
                            MusicBeePlugin.Plugin.SkinElement.SkinInputPanel,
                            MusicBeePlugin.Plugin.ElementState.ElementStateDefault,
                            MusicBeePlugin.Plugin.ElementComponent.ComponentForeground));
                        child.BackColor = Color.FromArgb(mbApiInterface.Setting_GetSkinElementColour(
                            MusicBeePlugin.Plugin.SkinElement.SkinInputControl,
                            MusicBeePlugin.Plugin.ElementState.ElementStateDefault,
                            MusicBeePlugin.Plugin.ElementComponent.ComponentBackground));
                    }
                }
            }

            this.ForeColor = Color.FromArgb(mbApiInterface.Setting_GetSkinElementColour(
                MusicBeePlugin.Plugin.SkinElement.SkinInputPanel,
                MusicBeePlugin.Plugin.ElementState.ElementStateDefault,
                MusicBeePlugin.Plugin.ElementComponent.ComponentForeground));
            this.BackColor = Color.FromArgb(mbApiInterface.Setting_GetSkinElementColour(
                MusicBeePlugin.Plugin.SkinElement.SkinInputControl,
                MusicBeePlugin.Plugin.ElementState.ElementStateDefault,
                MusicBeePlugin.Plugin.ElementComponent.ComponentBackground));



            log = Logger.Instance;
            log.OnLogUpdated = new EventHandler(log_OnLogUpdated);

            _settings = settings;
            _playlistSync = playlistSync;

            // Register event handlers
            _playlistSync.MBSync.OnSyncComplete = new EventHandler(MBSync_OnSyncComplete);

            if (_playlistSync.GMusic.SyncRunning)
            {
                WriteLine("Background sync running");
            }

            if (_settings.Email != null)
            {
                // decode it from base64
                byte[] email = Convert.FromBase64String(_settings.Email);
                emailTextBox.Text = Encoding.UTF8.GetString(email);
            }

            if (_settings.Password != null)
            {
                byte[] passwd = Convert.FromBase64String(_settings.Password);
                passwordTextBox.Text = Encoding.UTF8.GetString(passwd);
            }

            rememberCheckbox.Checked = _settings.SaveCredentials;

           // autoSyncCheckbox.Checked = _settings.SyncOnStartup;

            populateLocalPlaylists();

            // tagToolsPlugin.mbForm.AddOwnedForm(this);
        }

        void MBSync_OnSyncComplete(object sender, EventArgs e)
        {
            this.Invoke(new MethodInvoker(delegate
            {
                WriteLine("Synchronisation done!");
                populateLocalPlaylists();
            }));
        }

        void GMusic_OnSyncComplete(object sender, EventArgs e)
        {
            this.Invoke(new MethodInvoker(delegate
            {
                WriteLine("Synchronisation done!");
                // Just refresh the playlists (don't bother fetching all the songs again)
                _playlistSync.GMusic.FetchPlaylists();
            }));
        }

        void log_OnLogUpdated(object sender, EventArgs e)
        {
            this.Invoke(new MethodInvoker(delegate
            {
                WriteLine(log.LastLog);
            }));
        }

        private async void loginButton_Click(object sender, EventArgs e)
        {
            // Save the pwd and email to disc
            // NOTE although this encodes them as base64 (so they're not immediately obvious to anyone reading them) this is
            // NOT SECURITY. There is no encryption. Don't check that box unless you trust your machine!
            if (rememberCheckbox.Checked)
            {
                _settings.SaveCredentials = true;
                Byte[] email = Encoding.UTF8.GetBytes(emailTextBox.Text);
                _settings.Email = Convert.ToBase64String(email);
                Byte[] pwd = Encoding.UTF8.GetBytes(passwordTextBox.Text);
                _settings.Password = Convert.ToBase64String(pwd);
                _settings.Save();
            }
            else
            {
                _settings.SaveCredentials = false;
                _settings.Email = "";
                _settings.Password = "";
                _settings.Save();
            }

            bool loggedIn = await _playlistSync.GMusic.LoginToGMusic(emailTextBox.Text, passwordTextBox.Text);

            if (loggedIn)
            {
                WriteLine("Successfully logged in.");

                WriteLine("Fetching library.");
                List<Track> tracks = await _playlistSync.GMusic.FetchLibrary();

                WriteLine("Fetching playlists.");
                List<Playlist> allPlaylists = await _playlistSync.GMusic.FetchPlaylists();

                googleMusicPlaylistBox.Items.Clear();
                foreach (Playlist playlist in allPlaylists)
                {
                    if (!playlist.Deleted)
                    {
                        if (_settings.GMusicPlaylistsToSync.Contains(playlist.Id))
                            googleMusicPlaylistBox.Items.Add(playlist, true);
                        else
                            googleMusicPlaylistBox.Items.Add(playlist, false);
                    }
                }

                this.syncNowButton.Enabled = true;
            }
            else
            {
                WriteLine("LOGIN FAILED. PLEASE TRY AGAIN");
            }
        }

        // Get local Mb playlists and place them in the checkedlistbox
        // Check the settings to see if they're currently syncable
        private void populateLocalPlaylists()
        {
            List<MbPlaylist> mbPlaylists = _playlistSync.MBSync.GetMbPlaylists();
            localPlaylistBox.Items.Clear();
            foreach (MbPlaylist mbPlaylist in mbPlaylists)
            {
                if (_settings.MBPlaylistsToSync.Contains(mbPlaylist.mbName))
                     localPlaylistBox.Items.Add(mbPlaylist, true);
                else
                    localPlaylistBox.Items.Add(mbPlaylist, false);
            }

        }

        /*
        private void autoSyncCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            _settings.SyncOnStartup = autoSyncCheckbox.Checked;
            _settings.Save();
        }*/

        // Depending on user settings, either start a local sync to remote, or start a remote sync to local
        private async void syncNowButton_Click(object sender, EventArgs e)
        {
            // Make sure we're logged in and have playlists, otherwise stop
            if (!_playlistSync.GMusic.LoggedIn)
            {
                WriteLine("Error: NOT LOGGED IN");
                return;
            }

            if (!_playlistSync.GMusic.DataFetched)
            {
                WriteLine("Please fetch data before attempting to sync.");
                return;
            }

            WriteLine("Now synchronising. Please wait.");


            bool result = await _playlistSync.SyncPlaylists();

            WriteLine(result ? "Synchronize success." : "Error during synchronizing");

            //                List<GMusicPlaylist> selected = new List<GMusicPlaylist>();
            //foreach (GMusicPlaylist selectedPlaylist in googleMusicPlaylistBox.CheckedItems)
             //   selected.Add(selectedPlaylist);

        }

        private void toGMusicRadiobutton_CheckedChanged(object sender, EventArgs e)
        {
            if (toGMusicRadiobutton.Checked)
                fromGMusicRadioButton.Checked = false;

            _settings.SyncLocalToRemote = toGMusicRadiobutton.Checked;
            _settings.Save();
        }

        private void fromGMusicRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (fromGMusicRadioButton.Checked)
                toGMusicRadiobutton.Checked = false;
        }
        

        private void allLocalPlayCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (allLocalPlayCheckbox.Checked)
            {
                for (int i = 0; i < localPlaylistBox.Items.Count; i++)
                {
                    localPlaylistBox.SetItemChecked(i, true);
                }
            }
            else
            {
                for (int i = 0; i < localPlaylistBox.Items.Count; i++)
                {
                    localPlaylistBox.SetItemChecked(i, false);
                }
            }

            saveLocalPlaylistSettings();
        }


        private void allRemotePlayCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            
            if (allRemotePlayCheckbox.Checked)
                for (int i = 0; i < googleMusicPlaylistBox.Items.Count; i++)
                    googleMusicPlaylistBox.SetItemChecked(i, true);
            else
                for (int i = 0; i < googleMusicPlaylistBox.Items.Count; i++)
                    googleMusicPlaylistBox.SetItemChecked(i, false);

            saveRemotePlaylistSettings();
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //SelectedIndexChanged NOT CheckedItemsChanged because the latter is called before the CheckedItems collection has actually updated,
        // making our eventual list miss the most recently selected item...
        private void localPlaylistBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            saveLocalPlaylistSettings();
        }

        private void googleMusicPlaylistBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            saveRemotePlaylistSettings();
        }



        private void saveLocalPlaylistSettings()
        {
            _settings.MBPlaylistsToSync.Clear();
            foreach (MbPlaylist playlist in localPlaylistBox.CheckedItems)
            {
                _settings.MBPlaylistsToSync.Add(playlist.mbName);
            }
            _settings.Save();
        }

        private void saveRemotePlaylistSettings()
        {
            _settings.GMusicPlaylistsToSync.Clear();
            foreach (Playlist playlist in googleMusicPlaylistBox.CheckedItems)
            {
                _settings.GMusicPlaylistsToSync.Add(playlist.Id);
            }
            _settings.Save();
        }

        private void unsubscribeEvents()
        {
            log.OnLogUpdated = null;

        }

        public new void Dispose()
        {
            this.unsubscribeEvents();
            base.Dispose();
        }

        private void Configure_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.unsubscribeEvents();
        }

        private void includeFoldersInNameCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            _settings.IncludeFoldersInPlaylistName = includeFoldersInNameCheckBox.Checked;
        }
    }
}
