using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MusicBeePlugin.Models;
using System.IO;
using GooglePlayMusicAPI;
using GooglePlayMusicAPI.Models.GooglePlayMusicModels;
using System.Threading.Tasks;

namespace MusicBeePlugin
{
    class MbSyncData
    {
        private Settings _settings;

        private Logger log;

        private Plugin.MusicBeeApiInterface _mbApiInterface;

        public EventHandler OnSyncComplete;

        public MbSyncData(Settings settings, Plugin.MusicBeeApiInterface mbApiInterface)
        {
            _settings = settings;

            log = Logger.Instance;

            _mbApiInterface = mbApiInterface;
        }

        //Taken from TagTools AdvanceSearchandReplace.cs #720
        // In MB you run a query and then fetch the results
        // First, we query the library for all the playlists.
        // For each playlist, we fetch all its files.
        public List<MbPlaylist> GetMbPlaylists()
        {
            List<MbPlaylist> MbPlaylists = new List<MbPlaylist>();
            _mbApiInterface.Playlist_QueryPlaylists();
            string playlist = _mbApiInterface.Playlist_QueryGetNextPlaylist();
            while (playlist != null)
            {
                string playlistName = _mbApiInterface.Playlist_GetName(playlist);
                MbPlaylist MbPlaylist = new MbPlaylist();
                MbPlaylist.mbName = playlist;
                MbPlaylist.Name = playlistName;

                MbPlaylists.Add(MbPlaylist);

                // Query the next mbPlaylist to start again
                playlist = _mbApiInterface.Playlist_QueryGetNextPlaylist();
            }

            MbPlaylists = MbPlaylists.OrderBy(p => p.Name).ToList();
            return MbPlaylists;
        }

        public List<MbSong> GetMbSongs()
        {
            string[] files = null;
            List<MbSong> allMbSongs = new List<MbSong>();

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
                MbSong thisSong = new MbSong();
                thisSong.Filename = path;
                thisSong.Artist = _mbApiInterface.Library_GetFileTag(path, Plugin.MetaDataType.Artist);
                thisSong.Title = _mbApiInterface.Library_GetFileTag(path, Plugin.MetaDataType.TrackTitle);
                allMbSongs.Add(thisSong);
            }
            return allMbSongs;
        }


        // Go through the selected playlists from GMusic,
        // delete the correspondingly named MusicBee playlist
        // Create a new playlist with the GMusic playlist contents
        public async Task<bool> SyncPlaylistsToMusicBee(List<Playlist> playlists, List<Track> allGMusicSongs)
        {
            // Get the absolute path to the root of playlist dir
            // We do this by creating a blank playlist and seeing where it was created
            string tempPlaylistName = "mbsynctempplaylist";
            _mbApiInterface.Playlist_CreatePlaylist("", tempPlaylistName, new string[] { });

            List<MbPlaylist> localPlaylists = GetMbPlaylists();
            List<MbSong> allMbSongs = GetMbSongs();

            // Find the root dir from the temp playlist
            // clean up temp playlist
            MbPlaylist tempPlaylist = localPlaylists.FirstOrDefault(x => x.Name == tempPlaylistName);
            string[] tempPlaylistPathSplit = tempPlaylist.mbName.Split('\\');
            string musicBeePlaylistRootDir = String.Join("\\", tempPlaylistPathSplit.Take(tempPlaylistPathSplit.Length - 1).ToArray());
            _mbApiInterface.Playlist_DeletePlaylist(tempPlaylist.mbName);

            // Go through each playlist we want to sync in turn
            foreach (Playlist playlist in playlists)
            {
                // Create an empty list for this playlist's local songs
                List<MbSong> mbPlaylistSongs = new List<MbSong>();

                // For each entry in the playlist we're syncing, get the song from the GMusic library we've downloaded,
                // Get the song Title and Artist and then look it up in the list of local songs.
                // If we find it, add it to the list of local songs
                foreach (PlaylistEntry entry in playlist.Songs)
                {
                    Track thisSong = allGMusicSongs.FirstOrDefault(s => s.Id == entry.TrackID || s.NID == entry.TrackID);
                    if (thisSong != null)
                    {
                        MbSong thisMbSong = allMbSongs.FirstOrDefault(s => s.Artist == thisSong.Artist && s.Title == thisSong.Title);
                        if (thisMbSong != null)
                        {
                            mbPlaylistSongs.Add(thisMbSong);
                        }
                    }
                }

                //mbAPI expects a string array of song filenames to create a playlist
                string[] mbPlaylistSongFiles = new string[mbPlaylistSongs.Count];
                int i = 0;
                foreach (MbSong song in mbPlaylistSongs)
                {
                    mbPlaylistSongFiles[i] = song.Filename;
                    i++;
                }
                // Now we need to either clear (by deleting and recreating the file) or create the playlist 
                MbPlaylist localPlaylist = localPlaylists.FirstOrDefault(p => p.Name == playlist.Name);
                if (localPlaylist != null)
                {
                    string playlistPath = localPlaylist.mbName;
                    // delete the local playlist
                    File.Delete(playlistPath);
                    // And create a new empty file in its place
                    File.Create(playlistPath).Dispose();

                    // Set all our new files into the playlist
                    _mbApiInterface.Playlist_SetFiles(localPlaylist.mbName, mbPlaylistSongFiles);
                }
                else
                {
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
            }

            // Get the local playlists again

            return true;
        }


    }
}
