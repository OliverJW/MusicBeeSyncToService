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

            includeFoldersInNameCheckBox.Checked = _settings.IncludeFoldersInPlaylistName;
            includeZInDatePlaylistsCheckbox.Checked = _settings.IncludeZAtStartOfDatePlaylistName;

            populateLocalPlaylists();
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
            if (_playlistSync.IsLoggedInToGpm())
            {
                WriteLine("Already logged in.");
                return;
            }

            try
            {
                bool loggedIn = await _playlistSync.LoginToGpm();
                if (loggedIn)
                {
                    WriteLine("Successfully logged in.");

                    // Need to fetch library because currently no way to get user uploaded track other than this endpoint
                    WriteLine("Fetching library.");
                    List<Track> tracks = await _playlistSync.RefreshGpmLibrary();

                    WriteLine("Fetching playlists.");
                    List<Playlist> allPlaylists = await _playlistSync.FetchGPMPlaylists();

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
                    WriteLine("Login failed. Please try again.");
                }
            }
            catch (Exception ex)
            {
                WriteLine($"Exception encountered: {ex.Message}");
                WriteLine(ex.StackTrace);
            }
        }

        // Get local Mb playlists and place them in the checkedlistbox
        // Check the settings to see if they're currently syncable
        private void populateLocalPlaylists()
        {
            List<MusicBeePlaylist> mbPlaylists = _playlistSync.GetMbPlaylists();
            localPlaylistBox.Items.Clear();
            foreach (MusicBeePlaylist mbPlaylist in mbPlaylists)
            {
                if (_settings.MBPlaylistsToSync.Contains(mbPlaylist.mbName))
                     localPlaylistBox.Items.Add(mbPlaylist, true);
                else
                    localPlaylistBox.Items.Add(mbPlaylist, false);
            }

        }

        // Depending on user settings, either start a local sync to remote, or start a remote sync to local
        private async void syncNowButton_Click(object sender, EventArgs e)
        {
            // Make sure we're logged in and have playlists, otherwise stop
            if (!_playlistSync.IsLoggedInToGpm())
            {
                WriteLine("Login to be able to sync.");
                return;
            }

            this.syncNowButton.Enabled = false;

            WriteLine("Now synchronising. Please wait.");

            List<MusicBeePlaylist> mbPlaylists = getMbPlaylistsToSync();
            List<Playlist> gmusicPlaylists = getGMusicPlaylistToSync();
            List<IPlaylistSyncError> result = await _playlistSync.SyncPlaylists(mbPlaylists, gmusicPlaylists);

            if (result == null || result.Count == 0)
            {
                WriteLine("Synchronize success.");
            }
            else
            {
                WriteLine("Errors during sync:");
                result.ForEach((err) => WriteLine(err.GetMessage()));
            }

            this.syncNowButton.Enabled = true;
        }

        private List<MusicBeePlaylist> getMbPlaylistsToSync()
        {
            List<MusicBeePlaylist> result = new List<MusicBeePlaylist>();
            foreach (MusicBeePlaylist playlist in localPlaylistBox.CheckedItems)
            {
                result.Add(playlist);
            }

            return result;
        }

        private List<Playlist> getGMusicPlaylistToSync()
        {
            List<Playlist> result = new List<Playlist>();
            foreach (Playlist playlist in googleMusicPlaylistBox.CheckedItems)
            {
                result.Add(playlist);
            }
            return result;
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
            foreach (MusicBeePlaylist playlist in localPlaylistBox.CheckedItems)
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

        private void includeZInDatePlaylistsCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            _settings.IncludeZAtStartOfDatePlaylistName = includeZInDatePlaylistsCheckbox.Checked;
        }
    }
}
