using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBeePlugin.Models
{

    public class SyncToSpotifySettings
    {
        public bool IncludeFoldersInPlaylistName { get; set; }

        public bool IncludeZAtStartOfDatePlaylistName { get; set; }

        public bool ValidateCloseMatches { get; set; }
    }
}
