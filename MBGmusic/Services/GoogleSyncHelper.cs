using GooglePlayMusicAPI;
using GooglePlayMusicAPI.Models.GooglePlayMusicModels;
using GooglePlayMusicAPI.Models.RequestModels;
using MusicBeePlugin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBeePlugin.Services
{
    public class GoogleSyncHelper
    {
        private GooglePlayMusicClient api = new GooglePlayMusicClient("MusicBeeGMusicSync");
        public List<Playlist> GooglePlaylists { get; set; } = new List<Playlist>();
        public List<Track> GoogleSongsFetched { get; set; } = new List<Track>();
        public async Task<bool> Login()
        {
            return await api.LoginAsync();
        }

        public bool IsLoggedIn()
        {
            return api.LoggedIn();
        }

        public async Task<List<Playlist>> FetchPlaylists(bool fetchSongs = false)
        {
            GooglePlaylists = await api.GetPlaylistsWithEntriesAsync();
            if (GooglePlaylists.Count > 0)
            {
                GooglePlaylists = GooglePlaylists.OrderBy(p => p.Name).ToList();
            }

            if (fetchSongs)
            {
                // get songs that are in GMusic playlists but not in GMusic library
                foreach (Playlist playlist in GooglePlaylists)
                {
                    foreach (PlaylistEntry entry in playlist.Songs)
                    {
                        if (GoogleSongsFetched.FirstOrDefault(t => t.Id == entry.TrackID || t.NID == entry.TrackID) == null)
                        {
                            Track track = await api.GetTrackAsync(entry.TrackID);
                            GoogleSongsFetched.Add(track);
                        }
                    }
                }
            }

            return GooglePlaylists;
        }

        public async Task<List<Track>> RefreshLibrary()
        {
            GoogleSongsFetched = await api.GetLibraryAsync();
            return GoogleSongsFetched;
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

        public async Task<List<IPlaylistSyncError>> SyncToGoogle(MusicBeeSyncHelper mb, List<MusicBeePlaylist> mbPlaylistsToSync, 
            bool includeFoldersInPlaylistName=false, bool includeZAtStartOfDatePlaylistName=true)
        {
            List<IPlaylistSyncError> errors = new List<IPlaylistSyncError>();

            foreach (MusicBeePlaylist playlist in mbPlaylistsToSync)
            {
                // Use LINQ to check for a playlist with the same name
                // If there is one, clear it's contents, otherwise create one
                // Unless it's been deleted, in which case pretend it doesn't exist.
                // I'm not sure how to undelete a playlist, or even if you can
                string gpmPlaylistName = null;
                if (includeFoldersInPlaylistName)
                {
                    gpmPlaylistName = playlist.Name;
                }
                else
                {
                    gpmPlaylistName = playlist.Name.Split('\\').Last();
                }

                if (includeZAtStartOfDatePlaylistName)
                {
                    // if it starts with a 2, it's a date playlist
                    if (gpmPlaylistName.StartsWith("2"))
                    {
                        gpmPlaylistName = $"Z {gpmPlaylistName}";
                    }
                }

                Playlist thisPlaylist = GooglePlaylists.FirstOrDefault(p => p.Name == gpmPlaylistName && p.Deleted == false);
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
                if (mb.MbApiInterface.Playlist_QueryFiles(playlist.mbName))
                {
                    bool success = mb.MbApiInterface.Playlist_QueryFilesEx(playlist.mbName, ref playlistFiles);
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
                    string title = mb.MbApiInterface.Library_GetFileTag(file, Plugin.MetaDataType.TrackTitle);
                    string artist = mb.MbApiInterface.Library_GetFileTag(file, Plugin.MetaDataType.Artist);
                    string album = mb.MbApiInterface.Library_GetFileTag(file, Plugin.MetaDataType.Album);

                    // First check for matching title, artist, album, if we find nothing, then check for matching title/artist
                    Track gSong = GoogleSongsFetched.FirstOrDefault(item => (item.Artist.ToLower() == artist.ToLower() && item.Title.ToLower() == title.ToLower() && item.Album.ToLower() == album.ToLower()));
                    if (gSong == null)
                    {
                        gSong = GoogleSongsFetched.FirstOrDefault(item => (item.Artist.ToLower() == artist.ToLower() && item.Title.ToLower() == title.ToLower()));
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
                                GoogleSongsFetched.Add(result);
                                songsToAdd.Add(result);
                            }
                            else
                            {
                                // didn't find it even via querying 
                                errors.Add(new UnableToFindGoogleTrackError()
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

            return errors;
        }

        public async Task<List<IPlaylistSyncError>> SyncToMusicBee(MusicBeeSyncHelper mb, List<Playlist> playlists)
        {
            List<IPlaylistSyncError> errors = new List<IPlaylistSyncError>();

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
                    Track thisSong = GoogleSongsFetched.FirstOrDefault(s => s.Id == entry.TrackID || s.NID == entry.TrackID);

                    // if we couldn't find it, attempt to fetch it by trackID and cache it
                    if (IsTrackInvalid(thisSong))
                    {
                        thisSong = await api.GetTrackAsync(entry.TrackID);
                        if (IsTrackInvalid(thisSong))
                        {
                            errors.Add(new UnableToFindGooglePlaylistEntryError()
                            {
                                GpmPlaylistName = playlist.Name,
                                GpmPlaylistPosition = entry.AbsolutePosition,
                                GpmTrackId = entry.TrackID,
                            });
                        }
                        else
                        {
                            GoogleSongsFetched.Add(thisSong);
                        }
                    }

                    if (!IsTrackInvalid(thisSong))
                    {
                        MusicBeeSong thisMbSong = mb.Songs.FirstOrDefault(s => s.Artist == thisSong.Artist && s.Title == thisSong.Title);
                        if (thisMbSong != null)
                        {
                            mbPlaylistSongs.Add(thisMbSong);
                        }
                        else
                        {
                            errors.Add(new UnableToFindGoogleTrackError()
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
                MusicBeePlaylist localPlaylist = mb.Playlists.FirstOrDefault(p => p.Name == playlist.Name);
                if (localPlaylist != null)
                {
                    mb.MbApiInterface.Playlist_DeletePlaylist(localPlaylist.mbName);
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
                    mb.MbApiInterface.Playlist_CreatePlaylist("", playlistName, mbPlaylistSongFiles);
                }

                mb.MbApiInterface.Playlist_CreatePlaylist(playlistRelativeDir, playlistName, mbPlaylistSongFiles);
            }

            // Get the local playlists again
            mb.RefreshMusicBeePlaylists();

            return errors;
        }

        private static bool IsTrackInvalid(Track track)
        {
            return track == null || track.Artist == null || track.Artist == "" || track.Title == null || track.Title == "";
        }

        public class UnableToFindGooglePlaylistEntryError : IPlaylistSyncError
        {
            public string GpmTrackId { get; set; }
            public string GpmPlaylistPosition { get; set; }
            public string GpmPlaylistName { get; set; }

            public string GetMessage()
            {
                return $"For entry \"{GpmPlaylistPosition}\" of Google Play playlist \"{GpmPlaylistName}\", couldn't find track in Google Play with track id of \"{GpmTrackId}\"";
            }
        }

        public class UnableToFindGoogleTrackError : IPlaylistSyncError
        {
            public string PlaylistName { get; set; }
            public string TrackName { get; set; }
            public string ArtistName { get; set; }
            public string AlbumName { get; set; }
            public bool IsGpmTrack { get; set; }

            public string GetMessage()
            {
                string locationStr = IsGpmTrack ? "on Google Play" : "in your MusicBee library";
                return $"For playlist \"{PlaylistName}\", couldn't find \"{TrackName}\" from \"{AlbumName}\" by \"{ArtistName}\" {locationStr}";
            }
        }


    }
}
