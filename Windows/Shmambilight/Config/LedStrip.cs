using System;

namespace Shmambilight.Config
{
    [Serializable]
    public class LedStrip
    {
        public LetStripLocation Location { get; set; }

        public double PercentStart { get; set; }

        public double PercentEnd { get; set; }

        public double SpotWidthMultiplier { get; set; } = 1;

        public double SpotHeightMultiplier { get; set; } = 1;

        public int LedCount { get; set; }

        public bool IsHorizontal => Location == LetStripLocation.Top || Location == LetStripLocation.Bottom;
    }
}