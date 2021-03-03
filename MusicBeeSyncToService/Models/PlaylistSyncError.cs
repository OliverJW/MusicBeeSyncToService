using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBeePlugin.Models
{
    public interface IPlaylistSyncError
    {
        string GetMessage();
    }

    public class UnableToFindTrackError : IPlaylistSyncError
    {
        public string PlaylistName { get; set; }
        public string TrackName { get; set; }
        public string ArtistName { get; set; }
        public string AlbumName { get; set; }
        public bool SearchedService { get; set; }
        public string Service { get; set; }

        public string GetMessage()
        {
            string locationStr = SearchedService ? $"on {Service}" : "in your MusicBee library";
            return $"For playlist \"{PlaylistName}\", couldn't find \"{TrackName}\" from \"{AlbumName}\" by \"{ArtistName}\" {locationStr}";
        }
    }

    public class UnableToFindPerfectMatchTrackError : IPlaylistSyncError
    {
        public string PlaylistName { get; set; }
        public string TrackName { get; set; }
        public string ArtistName { get; set; }
        public string AlbumName { get; set; }
        public bool SearchedService { get; set; }
        public string Service { get; set; }
        public string GetMessage()
        {
            string locationStr = SearchedService ? $"on {Service}" : "in your MusicBee library";
            return $"For playlist \"{PlaylistName}\", couldn't find exact match for \"{TrackName}\" from \"{AlbumName}\" by \"{ArtistName}\" {locationStr}";
        }
    }


    public class UnableToFindGPMTrackError : UnableToFindTrackError
    { 
        public UnableToFindGPMTrackError()
        {
            Service = "Google Play Music";
        }
    }

    public class UnableToFindSpotifyTrackError : UnableToFindTrackError
    {
        public UnableToFindSpotifyTrackError()
        {
            Service = "Spotify";
        }
    }

    public class UnableToFindYTMTrackError : UnableToFindTrackError
    {
        public UnableToFindYTMTrackError()
        {
            Service = "Youtube Music";
        }
    }




}
