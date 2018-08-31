using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MusicBeePlugin.Models;
using System.Threading.Tasks;
using GooglePlayMusicAPI;
using GooglePlayMusicAPI.Models.GooglePlayMusicModels;
using GooglePlayMusicAPI.Models.RequestModels;

namespace MusicBeePlugin
{
    class GMusicSyncData
    {
        private Settings _settings;

        private Logger log;

        private Plugin.MusicBeeApiInterface _mbApiInterface;

        public GMusicSyncData(Settings settings, Plugin.MusicBeeApiInterface mbApiInterface)
        {
            _allPlaylists = new List<Playlist>();
            _allSongs = new List<Track>();

            _settings = settings;

            _syncRunning = false;
            
            log = Logger.Instance;

            _mbApiInterface = mbApiInterface;
        }

        private List<Playlist> _allPlaylists;
        public List<Playlist> AllPlaylists { get { return _allPlaylists; } }

        private List<Track> _allSongs;
        public List<Track> AllSongs { get { return _allSongs; } }

        

        private Boolean _syncRunning;
        public Boolean SyncRunning { get { return _syncRunning; } }

        private Boolean _dataFetched;
        public Boolean DataFetched { get { return _dataFetched; } }

        public Boolean LoggedIn { get { return api.LoggedIn(); } }

        #region Logging in

        public async Task<bool> LoginToGMusic(string email, string password)
        {
            bool result = await api.LoginAsync(email, password);
            return result;
        }

        #endregion

        #region Fetch GMusic Information

        // The global-ish stuff we need to sync with Google Music
        private GooglePlayMusicClient api = new GooglePlayMusicClient();

        public async Task<bool> FetchLibraryAndPlaylists()
        {
            _allSongs = await api.GetLibraryAsync();
            _allPlaylists = await api.GetPlaylistsWithEntriesAsync();
            _dataFetched = true;
            return _dataFetched;
        }

        public async Task<List<Track>> FetchLibrary()
        {
            _allSongs = await api.GetLibraryAsync();
            return _allSongs;
        }

        public async Task<List<Playlist>> FetchPlaylists()
        {
            _allPlaylists = await api.GetPlaylistsWithEntriesAsync();
            if (_allPlaylists.Count > 0)
            {
                _allPlaylists = _allPlaylists.OrderBy(p => p.Name).ToList();
            }

            // get songs that are in GMusic playlists but not in GMusic library
            foreach (Playlist playlist in _allPlaylists)
            {
                foreach (PlaylistEntry entry in playlist.Songs)
                {
                    if (_allSongs.FirstOrDefault(t => t.Id == entry.TrackID || t.NID == entry.TrackID) == null)
                    {
                        Track track = await api.GetTrackAsync(entry.TrackID);
                        _allSongs.Add(track);
                    }
                }
            }

            _dataFetched = true;
            return _allPlaylists;
        }

        #endregion

        #region Sync to GMusic

        // Synchronise the playlists defined in the settings file to Google Music
        public async Task<bool> SyncPlaylistsToGMusic(List<MbPlaylist> mbPlaylistsToSync)
        {
            _syncRunning = true;
            AutoResetEvent waitForEvent = new AutoResetEvent(false);

            if (_dataFetched)
            {
                // Get the MusicBee playlists
                foreach (MbPlaylist playlist in mbPlaylistsToSync)
                {
                    // Use LINQ to check for a playlist with the same name
                    // If there is one, clear it's contents, otherwise create one
                    // Unless it's been deleted, in which case pretend it doesn't exist.
                    // I'm not sure how to undelete a playlist, or even if you can
                    string gpmPlaylistName = null;
                    if (_settings.IncludeFoldersInPlaylistName)
                    {
                        gpmPlaylistName = playlist.Name;
                    }
                    else
                    {
                        gpmPlaylistName = playlist.Name.Split('\\').Last();
                    }

                    if (_settings.IncludeZAtStartOfDatePlaylistName)
                    {
                        // if it starts with a 2, it's a date playlist
                        if (gpmPlaylistName.StartsWith("2"))
                        {
                            gpmPlaylistName = $"Z {gpmPlaylistName}";
                        }
                    }

                    Playlist thisPlaylist = _allPlaylists.FirstOrDefault(p => p.Name == gpmPlaylistName && p.Deleted == false);
                    String thisPlaylistID = "";
                    if (thisPlaylist != null)
                    {
                        List<PlaylistEntry> allPlsSongs = thisPlaylist.Songs;

                        if (allPlsSongs.Count > 0)
                        {
                            MutatePlaylistResponse response = await api.RemoveFromPlaylistAsync(allPlsSongs);
                        }
                        thisPlaylistID = thisPlaylist.Id;
                    }
                    else
                    {
                        MutatePlaylistResponse response = await api.CreatePlaylistAsync(gpmPlaylistName);
                        thisPlaylistID = response.MutateResponses.First().ID;
                    }

                    // Create a list of files based on the MB Playlist
                    string[] playlistFiles = null;
                    if (_mbApiInterface.Playlist_QueryFiles(playlist.mbName))
                    {
                        bool success = _mbApiInterface.Playlist_QueryFilesEx(playlist.mbName, ref playlistFiles);
                        if (!success)
                            throw new Exception("Couldn't get playlist files");
                    }
                    else
                    {
                        playlistFiles = new string[0];
                    }

                    List<Track> songsToAdd = new List<Track>();
                    // And get the title and artist of each file, and add it to the GMusic playlist
                    foreach (string file in playlistFiles)
                    {
                        string title = _mbApiInterface.Library_GetFileTag(file, Plugin.MetaDataType.TrackTitle);
                        string artist = _mbApiInterface.Library_GetFileTag(file, Plugin.MetaDataType.Artist);
                        string album = _mbApiInterface.Library_GetFileTag(file, Plugin.MetaDataType.Album);

                        // First check for matching title, artist, album, if we find nothing, then check for matching title/artist
                        Track gSong = _allSongs.FirstOrDefault(item => (item.Artist == artist && item.Title == title && item.Album == album));
                        if (gSong == null)
                        {
                            gSong = _allSongs.FirstOrDefault(item => (item.Artist == artist && item.Title == title));
                            if (gSong != null)
                            {
                                songsToAdd.Add(gSong);
                            }
                            else
                            {
                                // Didn't find it in cached library, so query for it
                                SearchResult result = await api.SearchAsync($"{artist} {title} {album}", types: new SearchEntryType[] { SearchEntryType.Track });
                                if (result.Tracks != null && result.Tracks.Count > 0)
                                {
                                    // most likely the track is the first one
                                    gSong = result.Tracks.First();
                                    songsToAdd.Add(gSong);
                                }
                                else
                                {
                                    // didn't find it even via querying 
                                }
                            }

                        }
                        else
                        {
                            songsToAdd.Add(gSong);
                        }
                    }

                    await api.AddToPlaylistAsync(thisPlaylistID, songsToAdd);
                }

                _syncRunning = false;

            }
            else
            {
                throw new Exception("Not fetched data yet");
            }

            return true;
        }

        #endregion
    }
}
