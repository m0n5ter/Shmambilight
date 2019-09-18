using System;

namespace Shmambilight.Config
{
    [Serializable]
    public class ScreenGrabberConfig
    {
        public int AdapterIndex { get; set; } = 0;

        public int OutputIndex { get; set; } = 0;
    }
}