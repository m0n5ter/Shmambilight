using System;

namespace Shmambilight.Config
{
    [Serializable]
    public class LedStrip
    {
        public LetStripLocation Location { get; set; }

        public double PercentStart { get; set; }

        public double PercentEnd { get; set; }

        public int LedCount { get; set; }
    }
}