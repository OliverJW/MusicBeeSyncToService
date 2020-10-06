using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeMusicApi.Models;

namespace MusicBeePlugin.Models
{
    public class YoutubeMusicPlaylist
    {
        public Playlist Playlist { get; set; }

        public YoutubeMusicPlaylist(Playlist pl)
        {
            Playlist = pl;
        }

        public override string ToString()
        {
            return Playlist.Title;
        }

    }
}
