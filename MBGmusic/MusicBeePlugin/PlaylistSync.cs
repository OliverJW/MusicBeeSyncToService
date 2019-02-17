using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MusicBeePlugin.Models;
using System.Threading;
using GooglePlayMusicAPI;
using System.Threading.Tasks;
using GooglePlayMusicAPI.Models.GooglePlayMusicModels;
using GooglePlayMusicAPI.Models.RequestModels;
using System.IO;

namespace MusicBeePlugin
{
    class PlaylistSync
    {
        private Logger log;

        private Settings _settings;

        private Plugin.MusicBeeApiInterface _mbApiInterface;

        public PlaylistSync(Settings settings, Plugin.MusicBeeApiInterface mbApiInterface)
        {
            _settings = settings;

            _mbApiInterface = mbApiInterface;

            log = Logger.Instance;

            GpmPlaylists = new List<Playlist>();
            GpmSongsFetched = new List<Track>();

            MbSongs = GetMbSongs();
            MbPlaylists = GetMbPlaylists();
        }


        private bool SyncRunning = false;

        private GooglePlayMusicClient api = new GooglePlayMusicClient("MusicBeeGMusicSync");
        public List<Playlist> GpmPlaylists { get; set; }
        public List<Track> GpmSongsFetched { get; set; }

        List<MusicBeePlaylist> MbPlaylists { get; set; }
        List<MusicBeeSong> MbSongs { get; set; }


        public async Task<List<IPlaylistSyncError>> SyncPlaylists(List<MusicBeePlaylist> mbPlaylists, List<Playlist> gmusicPlaylists)
        {
            List<IPlaylistSyncError> errors = new List<IPlaylistSyncError>();

            if (!IsLoggedInToGpm() || SyncRunning)
                return errors;

            if (_settings.SyncLocalToRemote)
            {
                errors = await SyncPlaylistsToGMusic(mbPlaylists);
            }
            else
            {
                errors = await SyncPlaylistsToMusicBee(gmusicPlaylists);
            }

            return errors;
        }


        #region Google Play Music methods

        public async Task<bool> LoginToGpm()
        {
            bool result = await api.LoginAsync();
            return result;
        }

        public bool IsLoggedInToGpm()
        {
            return api.LoggedIn();
        }

        public async Task<List<Playlist>> FetchGPMPlaylists(bool fetchSongs=false)
        {
            GpmPlaylists = await api.GetPlaylistsWithEntriesAsync();
            if (GpmPlaylists.Count > 0)
            {
                GpmPlaylists = GpmPlaylists.OrderBy(p => p.Name).ToList();
            }

            if (fetchSongs)
            {
                // get songs that are in GMusic playlists but not in GMusic library
                foreach (Playlist playlist in GpmPlaylists)
                {
                    foreach (PlaylistEntry entry in playlist.Songs)
                    {
                        if (GpmSongsFetched.FirstOrDefault(t => t.Id == entry.TrackID || t.NID == entry.TrackID) == null)
                        {
                            Track track = await api.GetTrackAsync(entry.TrackID);
                            GpmSongsFetched.Add(track);
                        }
                    }
                }
            }

            return GpmPlaylists;
        }

        public async Task<List<Track>> RefreshGpmLibrary()
        {
            GpmSongsFetched = await api.GetLibraryAsync();
            return GpmSongsFetched;
        }

        public async Task<Track> TryGetTrackAsync(String artist, String title, String album)
        {
            // Didn't find it in cached library, so query for it
            SearchResult result = await api.SearchAsync($"{artist} {title} {album}", types: new SearchEntryType[] { SearchEntryType.Track });
            if (result.Tracks != null && result.Tracks.Count > 0)
            {
                // most likely the track is the first one
                return result.Tracks.First();
            }

            return null;
        }

        public async Task<List<IPlaylistSyncError>> SyncPlaylistsToGMusic(List<MusicBeePlaylist> mbPlaylistsToSync)
        {
            SyncRunning = true;

            List<IPlaylistSyncError> errors = new List<IPlaylistSyncError>();

            foreach (MusicBeePlaylist playlist in mbPlaylistsToSync)
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

                Playlist thisPlaylist = GpmPlaylists.FirstOrDefault(p => p.Name == gpmPlaylistName && p.Deleted == false);
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
                    Track gSong = GpmSongsFetched.FirstOrDefault(item => (item.Artist == artist && item.Title == title && item.Album == album));
                    if (gSong == null)
                    {
                        gSong = GpmSongsFetched.FirstOrDefault(item => (item.Artist == artist && item.Title == title));
                        if (gSong != null)
                        {
                            songsToAdd.Add(gSong);
                        }
                        else
                        {
                            // Didn't find it in cached library, so query for it
                            Track result = await TryGetTrackAsync(artist, title, album);
                            if (result != null)
                            {
                                GpmSongsFetched.Add(result);
                                songsToAdd.Add(result);
                            }
                            else
                            {
                                // didn't find it even via querying 
                                errors.Add(new UnableToFindTrackError()
                                {
                                    PlaylistName = gpmPlaylistName,
                                    AlbumName = album,
                                    ArtistName = artist,
                                    TrackName = title,
                                    IsGpmTrack = true
                                });
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

            SyncRunning = false;

            return errors;
        }

        #endregion

        #region MusicBee methods

        public List<MusicBeePlaylist> GetMbPlaylists()
        {
            List<MusicBeePlaylist> MbPlaylists = new List<MusicBeePlaylist>();
            _mbApiInterface.Playlist_QueryPlaylists();
            string playlist = _mbApiInterface.Playlist_QueryGetNextPlaylist();
            while (playlist != null)
            {
                string playlistName = _mbApiInterface.Playlist_GetName(playlist);
                MusicBeePlaylist MbPlaylist = new MusicBeePlaylist();
                MbPlaylist.mbName = playlist;
                MbPlaylist.Name = playlistName;

                MbPlaylists.Add(MbPlaylist);

                // Query the next mbPlaylist to start again
                playlist = _mbApiInterface.Playlist_QueryGetNextPlaylist();
            }

            MbPlaylists = MbPlaylists.OrderBy(p => p.Name).ToList();
            return MbPlaylists;
        }

        public List<MusicBeeSong> GetMbSongs()
        {
            string[] files = null;
            List<MusicBeeSong> allMbSongs = new List<MusicBeeSong>();

            if (_mbApiInterface.Library_QueryFiles("domain=library"))
            {
                // Old (deprecated)
                //public char[] filesSeparators = { '\0' };
                //files = _mbApiInterface.Library_QueryGetAllFiles().Split(filesSeparators, StringSplitOptions.RemoveEmptyEntries);
                _mbApiInterface.Library_QueryFilesEx("domain=library", ref files);
            }
            else
            {
                files = new string[0];
            }

            foreach (string path in files)
            {
                MusicBeeSong thisSong = new MusicBeeSong();
                thisSong.Filename = path;
                thisSong.Artist = _mbApiInterface.Library_GetFileTag(path, Plugin.MetaDataType.Artist);
                thisSong.Title = _mbApiInterface.Library_GetFileTag(path, Plugin.MetaDataType.TrackTitle);
                allMbSongs.Add(thisSong);
            }
            return allMbSongs;
        }

        public async Task<List<IPlaylistSyncError>> SyncPlaylistsToMusicBee(List<Playlist> playlists)
        {
            List<IPlaylistSyncError> errors = new List<IPlaylistSyncError>();

            // Get the absolute path to the root of playlist dir
            // We do this by creating a blank playlist and seeing where it was created
            string tempPlaylistName = "mbsynctempplaylist";
            _mbApiInterface.Playlist_CreatePlaylist("", tempPlaylistName, new string[] { });

            // Refresh Mb playlists after making temp playlist
            MbPlaylists = GetMbPlaylists();

            // Find the root dir from the temp playlist
            // clean up temp playlist
            MusicBeePlaylist tempPlaylist = MbPlaylists.FirstOrDefault(x => x.Name == tempPlaylistName);
            string[] tempPlaylistPathSplit = tempPlaylist.mbName.Split('\\');
            string musicBeePlaylistRootDir = String.Join("\\", tempPlaylistPathSplit.Take(tempPlaylistPathSplit.Length - 1).ToArray());
            _mbApiInterface.Playlist_DeletePlaylist(tempPlaylist.mbName);

            // Go through each playlist we want to sync in turn
            foreach (Playlist playlist in playlists)
            {
                // Create an empty list for this playlist's local songs
                List<MusicBeeSong> mbPlaylistSongs = new List<MusicBeeSong>();

                // For each entry in the playlist we're syncing, get the song from the GMusic library we've downloaded,
                // Get the song Title and Artist and then look it up in the list of local songs.
                // If we find it, add it to the list of local songs
                foreach (PlaylistEntry entry in playlist.Songs)
                {
                    Track thisSong = GpmSongsFetched.FirstOrDefault(s => s.Id == entry.TrackID || s.NID == entry.TrackID);

                    // if we couldn't find it, attempt to fetch it by trackID
                    if (thisSong == null)
                    {
                        // Didn't find it in cached library, so query for it
                        thisSong = await api.GetTrackAsync(entry.TrackID);
                        if (thisSong == null)
                        {
                            errors.Add(new UnableToFindGpmPlaylistEntryError()
                            {
                                GpmPlaylistName = playlist.Name,
                                GpmPlaylistPosition = entry.AbsolutePosition,
                                GpmTrackId = entry.TrackID,
                            });
                        }
                        else
                        {
                            GpmSongsFetched.Add(thisSong);
                        }
                    }

                    if (thisSong != null)
                    {
                        MusicBeeSong thisMbSong = MbSongs.FirstOrDefault(s => s.Artist == thisSong.Artist && s.Title == thisSong.Title);
                        if (thisMbSong != null)
                        {
                            mbPlaylistSongs.Add(thisMbSong);
                        }
                        else
                        {
                            errors.Add(new UnableToFindTrackError()
                            {
                                AlbumName = thisSong.Album,
                                ArtistName = thisSong.Artist,
                                PlaylistName = playlist.Name,
                                TrackName = thisSong.Title,
                                IsGpmTrack = false
                            });
                        }
                    }
                }

                //mbAPI expects a string array of song filenames to create a playlist
                string[] mbPlaylistSongFiles = new string[mbPlaylistSongs.Count];
                int i = 0;
                foreach (MusicBeeSong song in mbPlaylistSongs)
                {
                    mbPlaylistSongFiles[i] = song.Filename;
                    i++;
                }

                // Now we need to delete any existing playlist with matching name
                MusicBeePlaylist localPlaylist = MbPlaylists.FirstOrDefault(p => p.Name == playlist.Name);
                if (localPlaylist != null)
                {
                    _mbApiInterface.Playlist_DeletePlaylist(localPlaylist.mbName);
                }

                // Create the playlist locally
                string playlistRelativeDir = "";
                string playlistName = playlist.Name;

                // if it's a date playlist, remove first Z
                if (playlistName.StartsWith("Z "))
                {
                    playlistName = playlistName.Skip(2).ToString();
                }

                string[] itemsInPath = playlist.Name.Split('\\');
                if (itemsInPath.Length > 1)
                {
                    // Creates a playlist at top level directory
                    _mbApiInterface.Playlist_CreatePlaylist("", playlistName, mbPlaylistSongFiles);
                }

                _mbApiInterface.Playlist_CreatePlaylist(playlistRelativeDir, playlistName, mbPlaylistSongFiles);
            }

            // Get the local playlists again
            MbPlaylists = GetMbPlaylists();

            return errors;
        }

        #endregion
    }
}
