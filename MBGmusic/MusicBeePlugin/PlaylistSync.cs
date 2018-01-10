using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MusicBeePlugin.Models;
using System.Threading;
using GooglePlayMusicAPI;
using System.Threading.Tasks;
using GooglePlayMusicAPI.Models.GooglePlayMusicModels;

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

            _gMusic = new GMusicSyncData(settings, mbApiInterface);
            _mbSync = new MbSyncData(settings, mbApiInterface);
        }
               
        private GMusicSyncData _gMusic;
        public GMusicSyncData GMusic { get { return _gMusic; } }

        private MbSyncData _mbSync;
        public MbSyncData MBSync { get { return _mbSync; } }

        /// <summary>
        /// This is blocking, so run it on a thread
        /// </summary>
        public async Task<bool> SyncPlaylists()
        {
            bool result = true;

            if (!_gMusic.LoggedIn || _gMusic.SyncRunning)
                return false;

            if (_settings.SyncLocalToRemote)
            {
                List<MbPlaylist> playlists = new List<MbPlaylist>();
                List<MbPlaylist> allPlaylists = _mbSync.GetMbPlaylists();
                // Only sync the playlists that the settings say we should
                // Surely there's a nicer LINQ query for this?
                // There is :)
                playlists = allPlaylists.Where(p => _settings.MBPlaylistsToSync.Contains(p.mbName)).ToList();
                result = await _gMusic.SyncPlaylistsToGMusic(playlists);   
            }
            else
            {
                if (_gMusic.DataFetched)
                {
                    List<Playlist> playlists = new List<Playlist>();
                    foreach (string id in _settings.GMusicPlaylistsToSync)
                    {
                        Playlist pls = _gMusic.AllPlaylists.FirstOrDefault(p => p.Id == id);
                        if (pls != null)
                            playlists.Add(pls);
                    }
                    result = await _mbSync.SyncPlaylistsToMusicBee(playlists, _gMusic.AllSongs);
                }
            }

            return result;

        }

    }
}
