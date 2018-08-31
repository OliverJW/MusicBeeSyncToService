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
        public async Task<bool> SyncPlaylists(List<MbPlaylist> mbPlaylists, List<Playlist> gmusicPlaylists)
        {
            bool result = true;

            if (!_gMusic.LoggedIn || _gMusic.SyncRunning)
                return false;

            if (_settings.SyncLocalToRemote)
            {
                result = await _gMusic.SyncPlaylistsToGMusic(mbPlaylists);
            }
            else
            {
                if (_gMusic.DataFetched)
                {
                    result = await _mbSync.SyncPlaylistsToMusicBee(gmusicPlaylists, _gMusic.AllSongs);
                }
            }

            return result;

        }

    }
}
