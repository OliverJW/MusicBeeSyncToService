using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBeePlugin.Models
{
    public class SpotifyPlaylist
    {
        public SimplePlaylist Playlist { get; set; }

        public SpotifyPlaylist(SimplePlaylist pl)
        {
            Playlist = pl;
        }

        public override string ToString()
        {
            return Playlist.Name;
        }
    }
}
