using MusicBeePlugin.Models;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBeePlugin.Services
{
    public class SpotifySyncHelper
    {
        private PrivateProfile Profile;
        private SpotifyWebAPI Spotify;
        private Action<string> Log;
        private Action<bool> OnLoginDone;

        public List<SimplePlaylist> Playlists { get; set; } = new List<SimplePlaylist>();

        public SpotifySyncHelper(Action<string> log, Action<bool> onLoginDone)
        {
            Log = log;
            OnLoginDone = onLoginDone;
        }

        public bool IsLoggedIn()
        {
            return Spotify != null && Spotify.AccessToken != null && Spotify.AccessToken != "";
        }

        public void Login()
        {
            AuthorizationCodeAuth auth = new AuthorizationCodeAuth(SpotifySecrets.CLIENT_ID, SpotifySecrets.CLIENT_SECRET, "http://localhost:8000", "http://localhost:8000",
                SpotifyAPI.Web.Enums.Scope.UserLibraryRead | SpotifyAPI.Web.Enums.Scope.PlaylistModifyPublic);

            auth.AuthReceived += AuthOnAuthReceived;

            try
            {
                auth.Start();
                auth.OpenBrowser();
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        private async void AuthOnAuthReceived(object sender, AuthorizationCode payload)
        {
            AuthorizationCodeAuth auth = (AuthorizationCodeAuth)sender;
            auth.Stop();

            Token token = await auth.ExchangeCode(payload.Code);
            Spotify = new SpotifyWebAPI
            {
                AccessToken = token.AccessToken,
                TokenType = token.TokenType
            };

            if (Spotify == null)
            {
                OnLoginDone(false);
            }
            else
            {
                Profile = await Spotify.GetPrivateProfileAsync();
                OnLoginDone(true);
            }

        }

        public async Task<List<SimplePlaylist>> RefreshPlaylists()
        {
            Playlists.Clear();
            Paging<SimplePlaylist> response = await Spotify.GetUserPlaylistsAsync(Profile.Id, limit:100);
            response.Items.ForEach(i => Playlists.Add(i));
            while (response.HasNextPage())
            {
                response = await Spotify.GetUserPlaylistsAsync(Profile.Id, limit: 100, offset:response.Offset+1);
                response.Items.ForEach(i => Playlists.Add(i));
            }

            return Playlists;
        }

        public async Task<List<IPlaylistSyncError>> SyncToSpotify(MusicBeeSyncHelper mb, List<MusicBeePlaylist> mbPlaylistsToSync,
            bool includeFoldersInPlaylistName = false, bool includeZAtStartOfDatePlaylistName = true)
        {
            List<IPlaylistSyncError> errors = new List<IPlaylistSyncError>();

            foreach (MusicBeePlaylist playlist in mbPlaylistsToSync)
            {
                // Use LINQ to check for a playlist with the same name
                // If there is one, clear it's contents, otherwise create one
                // Unless it's been deleted, in which case pretend it doesn't exist.
                // I'm not sure how to undelete a playlist, or even if you can
                string spotifyPlaylistName = null;
                if (includeFoldersInPlaylistName)
                {
                    spotifyPlaylistName = playlist.Name;
                }
                else
                {
                    spotifyPlaylistName = playlist.Name.Split('\\').Last();
                }

                if (includeZAtStartOfDatePlaylistName)
                {
                    // if it starts with a 2, it's a date playlist
                    if (spotifyPlaylistName.StartsWith("2"))
                    {
                        spotifyPlaylistName = $"Z {spotifyPlaylistName}";
                    }
                }

                // If Spotify playlist with same name already exists, clear it.
                // Otherwise create one
                SimplePlaylist thisPlaylist = Playlists.FirstOrDefault(p => p.Name == spotifyPlaylistName);
                string thisPlaylistId;
                if (thisPlaylist != null)
                {
                    ErrorResponse res = await Spotify.ReplacePlaylistTracksAsync(thisPlaylist.Id, new List<string>(){ });
                    if (res.HasError())
                    {
                        Log(res.Error.Message);
                        return errors;
                    }
                    thisPlaylistId = thisPlaylist.Id;
                }
                else
                {
                    FullPlaylist newPlaylist = await Spotify.CreatePlaylistAsync(Profile.Id, spotifyPlaylistName);
                    thisPlaylistId = newPlaylist.Id;
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

                List<FullTrack> songsToAdd = new List<FullTrack>();
                // And get the title and artist of each file, and add it to the GMusic playlist
                foreach (string file in playlistFiles)
                {
                    string title = mb.MbApiInterface.Library_GetFileTag(file, Plugin.MetaDataType.TrackTitle);
                    string artist = mb.MbApiInterface.Library_GetFileTag(file, Plugin.MetaDataType.Artist);
                    string album = mb.MbApiInterface.Library_GetFileTag(file, Plugin.MetaDataType.Album);

                    string artistEsc = EscapeChar(artist.ToLower());
                    string titleEsc = EscapeChar(title.ToLower());
                    string searchStr = $"artist:{artistEsc}%20track:{titleEsc}";
                    SearchItem search = await Spotify.SearchItemsAsync(searchStr, SpotifyAPI.Web.Enums.SearchType.Track);

                    if (search.HasError())
                    {
                        Log($"Could not find track on Spotify '{searchStr}' for '{title}' by '{artist}': {search.Error.Message}");
                        continue;
                    }

                    if (search.Tracks.HasError())
                    {
                        Log($"Could not find track on Spotify '{searchStr}' for '{title}' by '{artist}': {search.Tracks.Error.Message}");
                        continue;
                    }

                    if (search.Tracks.Total == 0)
                    {
                        Log($"Found 0 results on Spotify for: {searchStr} for '{title}' by '{artist}'");
                        continue;
                    }

                    // try to find track matching artist and title
                    FullTrack trackToAdd = null;
                    foreach (FullTrack track in search.Tracks.Items)
                    {
                        bool titleMatches = (track.Name.ToLower() != title.ToLower());
                        bool artistMatches = (track.Artists.Exists(a => a.Name.ToLower() == artist.ToLower()));
                        bool albumMatches = (track.Album.Name.ToLower() == album.ToLower());
                        if (titleMatches && artistMatches && albumMatches)
                        {
                            trackToAdd = track;
                            break;
                        }
                        else if ((titleMatches && artistMatches) || (titleMatches && albumMatches) || (artistMatches && albumMatches))
                        {
                            // if two of them match, guessing this track is correct is 
                            // probably better than just using the firstordefault, but keep looping hoping for a better track
                            trackToAdd = track;
                        }
                        else if (artistMatches && trackToAdd == null)
                        {
                            // if just the artist matches and we haven't found anything yet... this might be our best guess
                            trackToAdd = track;
                        }
                    }

                    if (trackToAdd == null)
                    {
                        trackToAdd = search.Tracks.Items.FirstOrDefault();
                        Log($"Didn't find a perfect match for {searchStr} for '{title}' by '{artist}', so using '{trackToAdd.Name}' by '{trackToAdd.Artists.FirstOrDefault().Name}' instead");
                    }

                    songsToAdd.Add(trackToAdd);
                }

                List<string> uris = songsToAdd.ConvertAll(x => x.Uri);
                while (uris.Count > 0)
                {
                    List<string> currUris = uris.Take(75).ToList();
                    if (currUris.Count == 0)
                    {
                        break;
                    }

                    uris.RemoveRange(0, currUris.Count);
                    ErrorResponse response = await Spotify.AddPlaylistTracksAsync(Profile.Id, thisPlaylistId, currUris);
                    if (response.HasError())
                    {
                        Log(response.Error.Message);
                        return errors;
                    }
                }
            }

            return errors;
        }

        public async Task<List<IPlaylistSyncError>> SyncToMusicBee(MusicBeeSyncHelper mb, List<SimplePlaylist> playlists)
        {
            List<IPlaylistSyncError> errors = new List<IPlaylistSyncError>();

            // Go through each playlist we want to sync in turn
            foreach (SimplePlaylist playlist in playlists)
            {
                // Create an empty list for this playlist's local songs
                List<MusicBeeSong> mbPlaylistSongs = new List<MusicBeeSong>();

                // Get all tracks for playlist
                List<PlaylistTrack> tracks = new List<PlaylistTrack>();
                Paging<PlaylistTrack> resp = await Spotify.GetPlaylistTracksAsync(playlist.Id);
                resp.Items.ForEach(t => tracks.Add(t));
                while (resp.HasNextPage())
                {
                    resp = await Spotify.GetPlaylistTracksAsync(playlist.Id, offset: resp.Offset + 1);
                    resp.Items.ForEach(t => tracks.Add(t));
                }

                foreach (PlaylistTrack track in tracks)
                {
                    string artistStr = track.Track.Artists.FirstOrDefault().Name.ToLower();
                    string titleStr = track.Track.Name.ToLower();
                    MusicBeeSong thisMbSong = mb.Songs.FirstOrDefault(s => s.Artist.ToLower() == artistStr && s.Title.ToLower() == titleStr);
                    if (thisMbSong != null)
                    {
                        mbPlaylistSongs.Add(thisMbSong);
                    }
                    else
                    {
                        errors.Add(new UnableToFindSpotifyTrackError()
                        {
                            AlbumName = track.Track.Album.Name,
                            ArtistName = track.Track.Artists.FirstOrDefault().Name,
                            PlaylistName = playlist.Name,
                            TrackName = track.Track.Name,
                            SearchedSpotify = false
                        });
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

        private string EscapeChar(string input)
        {
            int openIndex = input.IndexOf("(f");
            int closeIndex = input.IndexOf(")");
            if (openIndex >= 0 && closeIndex > 0 && openIndex < closeIndex)
            {
                input = input.Remove(openIndex, closeIndex-openIndex);
            }

            return input.Replace(",", " ")
                .Replace(".", " ")
                .Replace("!", " ")
                .Replace("?", " ")
                .Replace("'",  "")
                .Replace(")", " ")
                .Replace("(", " ")
                .Replace("#", " ")
                .Replace("\\"," ")
                .Replace("/", " ")
                .Replace("&", " ")
                .Replace(":", " ")
                .Replace(";", " ")
                .Replace(" ", "%20");
        }


        public class UnableToFindSpotifyTrackError : IPlaylistSyncError
        {
            public string PlaylistName { get; set; }
            public string TrackName { get; set; }
            public string ArtistName { get; set; }
            public string AlbumName { get; set; }
            public bool SearchedSpotify { get; set; }

            public string GetMessage()
            {
                string locationStr = SearchedSpotify ? "on Spotify" : "in your MusicBee library";
                return $"For playlist \"{PlaylistName}\", couldn't find \"{TrackName}\" from \"{AlbumName}\" by \"{ArtistName}\" {locationStr}";
            }
        }
    }
}
