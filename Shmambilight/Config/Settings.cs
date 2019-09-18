using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace Shmambilight.Config
{
    [Serializable]
    public class Settings
    {
        private const string ConfigFileName = "Config.xml";

        public static Settings Current { get; }

        public int Version { get; set; } = 1;

        static Settings()
        {
            try
            {
                Current = (Settings)new XmlSerializer(typeof(Settings)).Deserialize(new XmlTextReader(File.OpenRead(ConfigFileName)));
            }
            catch (Exception exception)
            {
                App.LogError($"Failed to read {ConfigFileName}, using default", exception);
                Current = new Settings();
            }

            if (!File.Exists(ConfigFileName))
                new XmlSerializer(typeof(Settings)).Serialize(new XmlTextWriter(ConfigFileName, Encoding.UTF8) {Formatting = Formatting.Indented}, Current);
        }

        protected Settings() {}

        public LedDeviceConfig LedDevice { get; set; } = new LedDeviceConfig();

        public ScreenGrabberConfig ScreenGrabber { get; set; } = new ScreenGrabberConfig();

        public LedStrip[] LedStrips { get; set; } =
        {
            new LedStrip {LedCount = 21, Location = LetStripLocation.Left, PercentStart = 20.5 / 21, PercentEnd = 0.5 / 21},
            new LedStrip {LedCount = 36, Location = LetStripLocation.Top, PercentStart = 0.5 / 36, PercentEnd = 35.5 / 36},
            new LedStrip {LedCount = 21, Location = LetStripLocation.Right, PercentStart = 0.5 / 21, PercentEnd = 20.5 / 21}
        };

        public List<string> ProcessNames { get; set; } = new List<string>{ ".*PotPlayer.*", ".*mpc-hc.*" };

        public double MarginChangeDelay { get; set; } = 3;

        public bool EnableForProcess(Process process)
        {
            return ProcessNames.Any(s => Regex.IsMatch(process.ProcessName, s));
        }
    }
}