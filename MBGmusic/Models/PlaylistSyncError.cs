using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBeePlugin.Models
{
    interface IPlaylistSyncError
    {
        string GetMessage();
    }


    class UnableToFindGpmPlaylistEntryError : IPlaylistSyncError
    {
        public string GpmTrackId { get; set; }
        public string GpmPlaylistPosition { get; set; }
        public string GpmPlaylistName { get; set; }

        public string GetMessage()
        {
            return $"For entry \"{GpmPlaylistPosition}\" of Google Play playlist \"{GpmPlaylistName}\", couldn't find track in Google Play with track id of \"{GpmTrackId}\"";
        }
    }

    class UnableToFindTrackError : IPlaylistSyncError
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
