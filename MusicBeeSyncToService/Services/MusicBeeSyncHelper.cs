using MusicBeePlugin.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicBeePlugin.Services
{
    public class MusicBeeSyncHelper
    {

        public Plugin.MusicBeeApiInterface MbApiInterface;
        public List<MusicBeePlaylist> Playlists { get; private set; } = new List<MusicBeePlaylist>();
        public List<MusicBeeSong> Songs { get; private set; } = new List<MusicBeeSong>();

        public MusicBeeSyncHelper(Plugin.MusicBeeApiInterface apiInterface)
        {
            MbApiInterface = apiInterface;
            RefreshMusicBeePlaylists();
            RefreshMusicBeeSongs();
        }

        public void RefreshMusicBeePlaylists()
        {
            Playlists.Clear();
            Playlists = GetMusicBeePlaylists();
        }

        public void RefreshMusicBeeSongs()
        {
            Songs.Clear();
            Songs = GetMusicBeeSongs();
        }

        private List<MusicBeePlaylist> GetMusicBeePlaylists()
        {
            List<MusicBeePlaylist> MbPlaylists = new List<MusicBeePlaylist>();
            MbApiInterface.Playlist_QueryPlaylists();
            string playlist = MbApiInterface.Playlist_QueryGetNextPlaylist();
            while (playlist != null)
            {
                string playlistName = MbApiInterface.Playlist_GetName(playlist);
                MusicBeePlaylist MbPlaylist = new MusicBeePlaylist();
                MbPlaylist.mbName = playlist;
                MbPlaylist.Name = playlistName;

                MbPlaylists.Add(MbPlaylist);

                // Query the next mbPlaylist to start again
                playlist = MbApiInterface.Playlist_QueryGetNextPlaylist();
            }

            MbPlaylists = MbPlaylists.OrderBy(p => p.Name).ToList();
            return MbPlaylists;
        }

        private List<MusicBeeSong> GetMusicBeeSongs()
        {
            string[] files = null;
            List<MusicBeeSong> allMbSongs = new List<MusicBeeSong>();

            if (MbApiInterface.Library_QueryFiles("domain=library"))
            {
                // Old (deprecated)
                //public char[] filesSeparators = { '\0' };
                //files = _mbApiInterface.Library_QueryGetAllFiles().Split(filesSeparators, StringSplitOptions.RemoveEmptyEntries);
                MbApiInterface.Library_QueryFilesEx("domain=library", ref files);
            }
            else
            {
                files = new string[0];
            }

            foreach (string path in files)
            {
                MusicBeeSong thisSong = new MusicBeeSong();
                thisSong.Filename = path;
                thisSong.Artist = MbApiInterface.Library_GetFileTag(path, Plugin.MetaDataType.Artist);
                thisSong.Title = MbApiInterface.Library_GetFileTag(path, Plugin.MetaDataType.TrackTitle);
                thisSong.Album = MbApiInterface.Library_GetFileTag(path, Plugin.MetaDataType.Album);
                allMbSongs.Add(thisSong);
            }
            return allMbSongs;
        }

    }
}
