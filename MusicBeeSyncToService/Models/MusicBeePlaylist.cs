using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicBeePlugin.Models
{
    public class MusicBeePlaylist
    {
        public List<MusicBeeSong> Songs = new List<MusicBeeSong>();

        public string mbName;
        public string Name;
        public override string ToString()
        {
            return Name;
        }
    }
}
