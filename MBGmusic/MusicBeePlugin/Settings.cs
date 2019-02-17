using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace MusicBeePlugin
{
    [Serializable]
    public class Settings
    {
        public Settings(string filename)
        {

            MBPlaylistsToSync = new List<String>();
            GMusicPlaylistsToSync = new List<String>();

            // defaults
            SyncLocalToRemote = true;
            IncludeFoldersInPlaylistName = false;
            IncludeZAtStartOfDatePlaylistName = true;

            PlaylistDirectory = "GMusic";

            SettingsFile = filename;

        }

        public Settings()
        {
            MBPlaylistsToSync = new List<String>();
            GMusicPlaylistsToSync = new List<String>();
        }

        public String SettingsFile { get; set; }

        public Boolean SyncLocalToRemote { get; set; }
        public List<String> MBPlaylistsToSync { get; private set; }
        public List<String> GMusicPlaylistsToSync { get; private set; }
        public String PlaylistDirectory { get; set; }
        public Boolean IncludeFoldersInPlaylistName { get; set; }
        public Boolean IncludeZAtStartOfDatePlaylistName { get; set; }

        public bool Save()
        {
            Settings.SaveSettings(this);
            return true;
        }

        public void Delete()
        {
            File.Delete(SettingsFile);
        /*
            try
            {
                // delete the config file
                
                return true;
            }
            catch
            {
                return false;
            }*/
        }

        public static Settings ReadSettings(string filename)
        {
            if (File.Exists(filename))
            {
                XmlSerializer controlsDefaultsSerializer = controlsDefaultsSerializer = new XmlSerializer(typeof(Settings));

                FileStream stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None);
                StreamReader file = new StreamReader(stream, Encoding.UTF8);
                Settings settings = null;
                try
                {
                    settings = (Settings)controlsDefaultsSerializer.Deserialize(file);
                }
                catch (Exception e)
                {
                    Logger.Instance.Log("ERROR: Couldn't read saved settings");
                    Logger.Instance.DebugLog("ERROR: Couldn't read saved settings");
                    Logger.Instance.DebugLog(e.Message);
                    return new Settings(filename);
                }
                finally
                {
                    file.Close();
                }
                settings.SettingsFile = filename;
                return settings;
            }
            else
            {
                return new Settings(filename);
            }
        }

        public static void SaveSettings(Settings settings)
        {
            try
            {
                using (FileStream cfgFile = File.Open(settings.SettingsFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    StreamWriter file = new StreamWriter(cfgFile, Encoding.UTF8);
                    XmlSerializer controlsDefaultsSerializer = new XmlSerializer(typeof(Settings));
                    controlsDefaultsSerializer.Serialize(file, settings);
                    file.Close();
                }
            }
            catch
            {
                Logger.Instance.Log("ERROR: Couldn't save settings");
                Logger.Instance.DebugLog("ERROR: Couldn't save settings");
            }

        }

    }
}
