﻿using GooglePlayMusicAPI.Models.GooglePlayMusicModels;
using MusicBeePlugin;
using MusicBeePlugin.Models;
using MusicBeePlugin.Services;
using MusicBeePlugin.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MBSyncToServiceUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool IncludeFolders { get { return IncludeFoldersCheckBox.IsChecked.HasValue && IncludeFoldersCheckBox.IsChecked.Value; } }
        private bool IncludeZ { get { return IncludeZCheckBox.IsChecked.HasValue && IncludeZCheckBox.IsChecked.Value; } }
        private bool SyncToService { get { return SyncToServiceRadioButton.IsChecked.HasValue && SyncToServiceRadioButton.IsChecked.Value; } }
        private MusicBeeSyncHelper MusicBee;
        private GoogleSyncHelper Google = new GoogleSyncHelper();
        private SpotifySyncHelper Spotify = new SpotifySyncHelper();

        public ObservableCollection<CheckedListItem<MusicBeePlaylist>> MusicBeePlaylists { get; set; }
        public ObservableCollection<CheckedListItem<Playlist>> GooglePlaylists { get; set; }

        public MainWindow(Plugin.MusicBeeApiInterface apiInterface)
        {
            InitializeComponent();

            MusicBeePlaylists = new ObservableCollection<CheckedListItem<MusicBeePlaylist>>();
            GooglePlaylists = new ObservableCollection<CheckedListItem<Playlist>>();

            MusicBee = new MusicBeeSyncHelper(apiInterface);
            RefreshMusicBeePlaylists();
        }


        #region MusicBee

        private List<MusicBeePlaylist> GetMusicBeePlaylistsToSync()
        {
            List<MusicBeePlaylist> results = new List<MusicBeePlaylist>();
            foreach (var listItem in MusicBeePlaylists)
            {
                if (listItem.IsChecked)
                {
                    results.Add(listItem.Item);
                }
            }

            return results;
        }

        private void RefreshMusicBeePlaylists()
        {
            MusicBeePlaylists.Clear();
            MusicBee.RefreshMusicBeePlaylists();
            MusicBee.Playlists.ForEach(x => MusicBeePlaylists.Add(new CheckedListItem<MusicBeePlaylist>(x)));
            MusicBeeListBox.ItemsSource = MusicBeePlaylists;
        }

        #endregion MusicBee

        #region Google
        private async void GoogleLoginButton_Click(object sender, RoutedEventArgs e)
        {
            GoogleLoginButton.IsEnabled = false;
            Log("Logging into Google...");
            await Google.Login();
            if (!Google.IsLoggedIn())
            {
                Log("Error while trying to log in to Google Play Music.");
                GoogleLoginButton.IsEnabled = true;
            }
            else
            {
                Log("Logged in successfully.");
                Log("Fetching Playlists and library (this can take some time for large libraries)");
                await Google.RefreshLibrary();
                await RefreshGooglePlaylists();

                GoogleSyncButton.IsEnabled = true;
                GoogleSelectAllButton.IsEnabled = true;
                GoogleLoginButton.IsEnabled = false;
                GoogleLoginButton.Content = "Logged in";
                Log("Ready to sync to/from Google");
            }
        }

        private async void GoogleSyncButton_Click(object sender, RoutedEventArgs e)
        {
            List<IPlaylistSyncError> errors = new List<IPlaylistSyncError>();
            GoogleSyncButton.IsEnabled = false;
            GoogleSelectAllButton.IsEnabled = false;
            string direction = (SyncToService ? "to" : "from");
            Log($"Starting sync {direction} Google Play Music...");
            try
            {
                if (SyncToService)
                {
                    List<MusicBeePlaylist> mbPlaylistsToSync = GetMusicBeePlaylistsToSync();
                    errors = await Google.SyncPlaylistsToGMusic(MusicBee, mbPlaylistsToSync, IncludeFolders, IncludeZ);
                }
                else
                {
                    List<Playlist> googlePlaylistsToSync = GetGooglePlaylistsToSync();
                    errors = await Google.SyncPlaylistsToMusicBee(MusicBee, googlePlaylistsToSync);
                }

                if (errors.Count > 0)
                {
                    foreach (IPlaylistSyncError error in errors)
                    {
                        Log(error.GetMessage());
                    }
                    Log("See errors above");
                }
                else
                {
                    Log($"Successfully synced playlists {direction} Google Play Music");
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
            GoogleSyncButton.IsEnabled = true;
            GoogleSelectAllButton.IsEnabled = true;
        }

        private List<Playlist> GetGooglePlaylistsToSync()
        {
            List<Playlist> results = new List<Playlist>();
            foreach (var listItem in GooglePlaylists)
            {
                if (listItem.IsChecked)
                {
                    results.Add(listItem.Item);
                }
            }
            return results;
        }

        private async Task RefreshGooglePlaylists(bool fetchSongs = false)
        {
            List<Playlist> googlePlaylists = await Google.FetchPlaylists(fetchSongs);
            GooglePlaylists.Clear();
            googlePlaylists.ForEach(x => GooglePlaylists.Add(new CheckedListItem<Playlist>(x)));
            GooglePlaylistListBox.ItemsSource = GooglePlaylists;
        }

        private void GoogleSelectAllButton_Checked(object sender, RoutedEventArgs e)
        {
            ChangeStateOfAllCheckBoxes(GooglePlaylists, true);
        }

        private void GoogleSelectAllButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ChangeStateOfAllCheckBoxes(GooglePlaylists, false);
        }

        #endregion Google

        #region Spotify

        private void SpotifyLoginButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SpotifySyncButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void SpotifySelectAllButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void SpotifySelectAllButton_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        #endregion Spotify

        #region Helpers

        public void Log(string line)
        {
            OutputTextBox.Text += $"{line}\n";
        }
        private void ChangeStateOfAllCheckBoxes<T>(ObservableCollection<CheckedListItem<T>> list, bool isChecked)
        {
            foreach (var item in list)
            {
                item.IsChecked = isChecked;
            }
        }
        #endregion
    }
}
