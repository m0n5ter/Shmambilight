using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using NLog;

namespace Shmambilight.Config
{
    [Serializable]
    public class Settings
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private const string ConfigFileName = "Config.xml";

        public static Settings Current { get; }

        public int Version { get; set; } = 1;

        static Settings()
        {
            try
            {
                using (var fileStream = File.OpenRead(ConfigFileName))
                using (var xnlReader = new XmlTextReader(fileStream))
                    Current = (Settings) new XmlSerializer(typeof(Settings)).Deserialize(xnlReader);
            }
            catch (Exception exception)
            {
                _logger.Warn(exception, $"Failed to read {ConfigFileName}, using default");
                Current = new Settings();
            }

            try
            {
                new XmlSerializer(typeof(Settings)).Serialize(new XmlTextWriter(ConfigFileName, Encoding.UTF8) {Formatting = Formatting.Indented}, Current);
            }
            catch 
            {
                //ignore
            }
        }

        protected Settings() {}

        public LedDeviceConfig LedDevice { get; set; } = new LedDeviceConfig();

        public ScreenGrabberConfig ScreenGrabber { get; set; } = new ScreenGrabberConfig();

        public LedStrip[] LedStrips { get; set; } =
        {
            new LedStrip {LedCount = 21, Location = LetStripLocation.Left, PercentStart = 20.5 / 21, PercentEnd = 0.5 / 21, SpotWidthMultiplier = 2},
            new LedStrip {LedCount = 36, Location = LetStripLocation.Top, PercentStart = 0.5 / 36, PercentEnd = 35.5 / 36, SpotHeightMultiplier = 2},
            new LedStrip {LedCount = 21, Location = LetStripLocation.Right, PercentStart = 0.5 / 21, PercentEnd = 20.5 / 21, SpotWidthMultiplier = 2}
        };

        public List<string> ProcessNames { get; set; } = new List<string>{ ".*PotPlayer.*", ".*mpc-hc.*" };

        public double MarginChangeDelay { get; set; } = 3;
        
        public uint FadeOutDuration { get; set; } = 2000;
        
        public uint FadeInDuration { get; set; } = 100;

        public bool EnableForProcess(Process process)
        {
            return ProcessNames.Any(s => Regex.IsMatch(process.ProcessName, s));
        }
    }
}