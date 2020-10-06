using MusicBeePlugin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeMusicApi;
using YoutubeMusicApi.Models;
using YoutubeMusicApi.Models.Search;

namespace MusicBeePlugin.Services
{

    public class YoutubeMusicSyncHelper
    {
        private Action<string> Log;

        private YoutubeMusicClient Ytm;

        private List<Playlist> Playlists = new List<Playlist>();

        public YoutubeMusicSyncHelper(Action<string> log)
        {
            Log = log;
        }

        public async Task<bool> Login(string filePath)
        {
            Ytm = new YoutubeMusicClient();
            try
            {
                bool res = await Ytm.LoginWithAuthJsonFile(filePath);
                if (!res)
                {
                    Log($"Could not login to YoutubeMusic from file '{filePath}'. Make sure the file contains the necessary cookies");
                }

                return res;
            }
            catch (Exception ex)
            {
                Log($"Could not login to YoutubeMusic from file '{filePath}', with exception '{ex.Message}'");
                return false;
            }
        }

        public async Task<List<Playlist>> RefreshPlaylists()
        {
            Playlists = new List<Playlist>();

            var response = await Ytm.GetLibraryPlaylists();
            Playlists.AddRange(response.Playlists);
            while (response.Continuation != null)
            {
                response = await Ytm.GetLibraryPlaylists(response.Continuation);
                Playlists.AddRange(response.Playlists);
            }

            return Playlists;
        }

        public async Task<List<IPlaylistSyncError>> SyncToYoutubeMusic(MusicBeeSyncHelper mb, List<MusicBeePlaylist> mbPlaylistsToSync,
            bool includeFoldersInPlaylistName = false, bool includeZAtStartOfDatePlaylistName = true)
        {
            List<IPlaylistSyncError> errors = new List<IPlaylistSyncError>();

            foreach (var playlist in mbPlaylistsToSync)
            {
                string newPlaylistName = null;
                if (includeFoldersInPlaylistName)
                {
                    newPlaylistName = playlist.Name;
                }
                else
                {
                    newPlaylistName = playlist.Name.Split('\\').Last();
                }

                if (includeZAtStartOfDatePlaylistName)
                {
                    // if it starts with a 2, it's a date playlist
                    if (newPlaylistName.StartsWith("2"))
                    {
                        newPlaylistName = $"Z {newPlaylistName}";
                    }
                }


                List<string> videoIds = new List<string>();


                foreach (var song in playlist.Songs)
                {
                    string searchStr = $"{song.Title} {song.Artist} {song.Album}";
                    var response = await Ytm.Search(searchStr);
                    var video = FindMatchInSearchResult(searchStr, song, response);

                    if (video == null || !IsPerfectMatch(song, video))
                    {
                        // if we couldn't find it, try searching uploads
                        response = await Ytm.SearchUploads(searchStr);
                        var otherVideo = FindMatchInSearchResult(searchStr, song, response);

                        video = FindBestMatchBetweenTwo(song, video, otherVideo);
                    }

                    if (video == null)
                    {
                        errors.Add(new UnableToFindYTMTrackError()
                        {
                            AlbumName = song.Album,
                            ArtistName = song.Artist,
                            PlaylistName = playlist.Name,
                            SearchedService = true,
                            TrackName = song.Title,
                        });
                    }
                    else
                    {
                        videoIds.Add(video.VideoId);
                    }
                }

                // If YTM playlist with same name already exists, clear it.
                // Otherwise create one
                var thisPlaylist = Playlists.FirstOrDefault(p => p.Title == newPlaylistName);
                if (thisPlaylist != null)
                {
                    var playlistWithSongs = await Ytm.GetPlaylist(thisPlaylist.PlaylistId, authRequired: true);
                    if (playlistWithSongs.Tracks.Count != 0)
                    {
                        var res = await Ytm.RemovePlaylistItems(playlistWithSongs.PlaylistId, playlistWithSongs.Tracks);
                        if (!res)
                        {
                            Log("Error while trying to clear playlist before syncing new tracks");
                            return errors;
                        }
                    }

                    await Ytm.AddPlaylistItems(playlistWithSongs.PlaylistId, videoIds);
                }
                else
                {
                    var res = await Ytm.CreatePlaylist(newPlaylistName, "", videoIds: videoIds);
                }
            }

            return errors;
        }

        public async Task<List<IPlaylistSyncError>> SyncToMusicBee(MusicBeeSyncHelper mb, List<Playlist> playlists)
        {
            List<IPlaylistSyncError> errors = new List<IPlaylistSyncError>();

            return errors;
        }

        private SongResult FindBestMatchBetweenTwo(MusicBeeSong song, SongResult a, SongResult b)
        {
            int aMatches = NumberOfMatches(song, a);
            int bMatches = NumberOfMatches(song, b);

            if (aMatches == 0 && bMatches == 0)
            {
                return null;
            }

            // Prefer the YTM tracks over uploads if there is a tie
            return aMatches >= bMatches ? a : b;
        }

        private bool IsPerfectMatch(MusicBeeSong song, SongResult songResult)
        {
            return NumberOfMatches(song, songResult) == 3;
        }

        private int NumberOfMatches(MusicBeeSong song, SongResult songResult)
        {
            if (songResult == null)
            {
                return 0;
            }

            bool titleMatches = song.Title.ToLower() == songResult.Title.ToLower();
            bool artistMatches = songResult.Artists.Exists(x => x.Name.ToLower() == song.Artist.ToLower());
            bool albumMatches = song.Album.ToLower() == songResult.Album.Name.ToLower();
            return (titleMatches ? 1 : 0) + (artistMatches ? 1 : 0) + (albumMatches ? 1 : 0);
        }

        private SongResult FindMatchInSearchResult(string searchStr, MusicBeeSong song, SearchResult response)
        {
            SongResult bestMatch = null;
            foreach (var songResult in response.Songs)
            {
                bool titleMatches = song.Title.ToLower() == songResult.Title.ToLower();
                bool artistMatches = songResult.Artists.Exists(x => x.Name.ToLower() == song.Artist.ToLower());
                bool albumMatches = song.Album.ToLower() == songResult.Album.Name.ToLower();
                if (titleMatches && artistMatches && albumMatches)
                {
                    // if we matched all three, likely this is the right track
                    bestMatch = songResult;
                    return bestMatch;
                }
                else if ((titleMatches && artistMatches) || (titleMatches && albumMatches))
                {
                    // if two of them match, guessing this track is correct is 
                    // probably better than just using the firstordefault, but keep looping hoping for a better track
                    bestMatch = songResult;
                    return bestMatch;
                }
                else if (artistMatches && bestMatch == null)
                {
                    // if just the artist matches and we haven't found anything yet... this might be our best guess
                    bestMatch = songResult;
                }
            }

            //if (videoIdToAdd == null)
            //{
            //    var first = response.Songs.FirstOrDefault();

            //    if (first != null)
            //    {
            //        videoIdToAdd = first.VideoId;
            //        Log($"Didn't find a perfect match for {searchStr} for '{song.Title}' by '{song.Artist}', so using '{first.Title}' by '{first.Artists.FirstOrDefault().Name}' instead");
            //    }
            //}

            return bestMatch;
        }
    }
}
