using GooglePlayMusicAPI.Models.GooglePlayMusicModels;
using Microsoft.Win32;
using MusicBeePlugin;
using MusicBeePlugin.Models;
using MusicBeePlugin.Services;
using MusicBeePlugin.WPF;
using SpotifyAPI.Web;
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
        private GoogleSyncHelper Google;
        private SpotifySyncHelper Spotify;
        private YoutubeMusicSyncHelper Youtube;

        public ObservableCollection<CheckedListItem<MusicBeePlaylist>> MusicBeePlaylists { get; set; }
        public ObservableCollection<CheckedListItem<Playlist>> GooglePlaylists { get; set; }
        public ObservableCollection<CheckedListItem<SpotifyPlaylist>> SpotifyPlaylists { get; set; }
        public ObservableCollection<CheckedListItem<YoutubeMusicPlaylist>> YoutubePlaylists { get; set; }

        public MainWindow(Plugin.MusicBeeApiInterface apiInterface)
        {
            InitializeComponent();

            MusicBeePlaylists = new ObservableCollection<CheckedListItem<MusicBeePlaylist>>();
            GooglePlaylists = new ObservableCollection<CheckedListItem<Playlist>>();
            SpotifyPlaylists = new ObservableCollection<CheckedListItem<SpotifyPlaylist>>();
            YoutubePlaylists = new ObservableCollection<CheckedListItem<YoutubeMusicPlaylist>>();

            MusicBee = new MusicBeeSyncHelper(apiInterface);
            RefreshMusicBeePlaylists();

            Action<string> log = (s) => Dispatcher.Invoke(() => { Log(s); });
            Spotify = new SpotifySyncHelper(log);

            Google = new GoogleSyncHelper();

            Youtube = new YoutubeMusicSyncHelper(log);
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
            
            try
            {
                await Google.Login();
            }
            catch (Exception ex)
            {
                Log($"Exception while trying to log in: ${ex.Message}");
                return;
            }

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
                    errors = await Google.SyncToGoogle(MusicBee, mbPlaylistsToSync, IncludeFolders, IncludeZ);
                    await RefreshGooglePlaylists();
                }
                else
                {
                    List<Playlist> googlePlaylistsToSync = GetGooglePlaylistsToSync();
                    errors = await Google.SyncToMusicBee(MusicBee, googlePlaylistsToSync);
                    RefreshMusicBeePlaylists();
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
            googlePlaylists = googlePlaylists.OrderBy((p) => p.Name).ToList();
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

        private async void SpotifyLoginButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            Log("Opening browser to log in to Spotify...");
            SpotifyLoginButton.IsEnabled = false;

            bool success = false;
            try
            {
                success = await Spotify.LoginAsync();
            }
            catch (Exception ex)
            {
                Log($"Error when trying to login to Spotify: {ex.Message}");
            }

            if (success)
            {
                Log("Logged into Spotify successfully.");
                Log("Fetching Spotify Playlists...");
                SpotifySelectAllButton.IsEnabled = true;
                SpotifySyncButton.IsEnabled = true;
                await RefreshSpotifyPlaylists();
            }
            else
            {
                Log("Error when trying to login to Spotify.");
                SpotifyLoginButton.IsEnabled = true;
            }
        }

        private async void SpotifySyncButton_Click(object sender, RoutedEventArgs e)
        {
            List<IPlaylistSyncError> errors = new List<IPlaylistSyncError>();
            string direction = (SyncToService ? "to" : "from");
            Log($"Starting sync {direction} Spotify...");
            SpotifySelectAllButton.IsEnabled = false;
            SpotifySyncButton.IsEnabled = false;

            try
            {
                if (SyncToService)
                {
                    List<MusicBeePlaylist> mbPlaylistsToSync = GetMusicBeePlaylistsToSync();
                    errors = await Spotify.SyncToSpotify(MusicBee, mbPlaylistsToSync, IncludeFolders, IncludeZ);
                    await RefreshSpotifyPlaylists();
                }
                else
                {
                    List<SimplePlaylist> spotifyPlaylistsToSync = GetSpotifyPlaylistsToSync();
                    errors = await Spotify.SyncToMusicBee(MusicBee, spotifyPlaylistsToSync);
                    RefreshMusicBeePlaylists();
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
                    Log($"Successfully synced playlists {direction} Spotify");
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }


            SpotifySelectAllButton.IsEnabled = true;
            SpotifySyncButton.IsEnabled = true;
            Log($"Finished sync {direction} Spotify.");
        }
        private List<SimplePlaylist> GetSpotifyPlaylistsToSync()
        {
            List<SimplePlaylist> results = new List<SimplePlaylist>();
            foreach (var listItem in SpotifyPlaylists)
            {
                if (listItem.IsChecked)
                {
                    results.Add(listItem.Item.Playlist);
                }
            }
            return results;
        }

        private async Task RefreshSpotifyPlaylists()
        {
            List<SimplePlaylist> spotifyPlaylists = await Spotify.RefreshPlaylists();
            spotifyPlaylists = spotifyPlaylists.OrderBy(p => p.Name).ToList();
            SpotifyPlaylists.Clear();
            spotifyPlaylists.ForEach(x => SpotifyPlaylists.Add(new CheckedListItem<SpotifyPlaylist>(new SpotifyPlaylist(x))));
            SpotifyPlaylistListBox.ItemsSource = SpotifyPlaylists;
        }

        private void SpotifySelectAllButton_Checked(object sender, RoutedEventArgs e)
        {
            ChangeStateOfAllCheckBoxes(SpotifyPlaylists, true);
        }

        private void SpotifySelectAllButton_Unchecked(object sender, RoutedEventArgs e)
        {

            ChangeStateOfAllCheckBoxes(SpotifyPlaylists, false);
        }

        #endregion Spotify

        #region YoutubeMusic

        private async void YoutubeLoginButton_Click(object sender, RoutedEventArgs e)
        {
            Log("Opening file picker dialog to select cookie file for Youtube Music...");
            YoutubeLoginButton.IsEnabled = false;

            string file = OpenDialogToChooseFile();
            if (file == null)
            {
                Log("Please select a file containing cookies to log in to Youtube Music");
                YoutubeLoginButton.IsEnabled = true;
                return;
            }

            bool success = false;
            try
            {
                success = await Youtube.Login(file);
            }
            catch (Exception ex)
            {
                Log($"Error when trying to login to Youtube Music: {ex.Message}");
            }

            if (!success)
            {
                Log("Error when trying to login to Youtube Music.");
                YoutubeLoginButton.IsEnabled = true;
                return;
            }

            Log("Logged into Youtbe Music successfully.");
            Log("Fetching Youtube Music Playlists...");
            
            try
            {
                await RefreshYoutubePlaylists();
            }
            catch (Exception ex)
            {
                Log($"Error when fetching Youtube Music playlists: {ex.Message}");
                YoutubeLoginButton.IsEnabled = true;
                return;
            }

            YoutubeSelectAllButton.IsEnabled = true;
            YoutubeSyncButton.IsEnabled = true;
        }

        private string OpenDialogToChooseFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();

            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value == true)
            {
                return dialog.FileName;
            }
            else
            {
                return null;
            }
        }

        private async Task RefreshYoutubePlaylists()
        {
            var playlists = await Youtube.RefreshPlaylists();
            playlists = playlists.OrderBy(p => p.Title).ToList();
            YoutubePlaylists.Clear();
            playlists.ForEach(x => YoutubePlaylists.Add(new CheckedListItem<YoutubeMusicPlaylist>(new YoutubeMusicPlaylist(x))));
            YoutubePlaylistListBox.ItemsSource = YoutubePlaylists;
        }

        private async void YoutubeSyncButton_Click(object sender, RoutedEventArgs e)
        {
            List<IPlaylistSyncError> errors = new List<IPlaylistSyncError>();
            string direction = (SyncToService ? "to" : "from");
            Log($"Starting sync {direction} Youtube Music...");
            YoutubeSelectAllButton.IsEnabled = false;
            YoutubeSyncButton.IsEnabled = false;

            try
            {
                if (SyncToService)
                {
                    List<MusicBeePlaylist> mbPlaylistsToSync = GetMusicBeePlaylistsToSync();
                    errors = await Youtube.SyncToYoutubeMusic(MusicBee, mbPlaylistsToSync, IncludeFolders, IncludeZ);
                    await RefreshYoutubePlaylists();
                }
                else
                {
                    var youtubePlaylistsToSync = GetYoutubePlaylistsToSync();
                    errors = await Youtube.SyncToMusicBee(MusicBee, youtubePlaylistsToSync);
                    RefreshMusicBeePlaylists();
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
                    Log($"Successfully synced playlists {direction} Youtube Music");
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }


            YoutubeSelectAllButton.IsEnabled = true;
            YoutubeSyncButton.IsEnabled = true;
            Log($"Finished sync {direction} Youtube Music.");
        }

        private List<YoutubeMusicApi.Models.Playlist> GetYoutubePlaylistsToSync()
        {
            List<YoutubeMusicApi.Models.Playlist> results = new List<YoutubeMusicApi.Models.Playlist>();
            foreach (var listItem in YoutubePlaylists)
            {
                if (listItem.IsChecked)
                {
                    results.Add(listItem.Item.Playlist);
                }
            }
            return results;
        }

        private void YoutubeSelectAllButton_Checked(object sender, RoutedEventArgs e)
        {
            ChangeStateOfAllCheckBoxes(YoutubePlaylists, true);
        }

        private void YoutubeSelectAllButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ChangeStateOfAllCheckBoxes(YoutubePlaylists, false);
        }

        #endregion

        #region Helpers

        public void Log(string line)
        {
            OutputTextBox.Text += $"{line}\n";
            OutputTextBox.ScrollToEnd();
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
