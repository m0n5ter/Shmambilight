using System;
using System.Linq;
using Shmambilight.Screen;

namespace Shmambilight.Config
{
    [Serializable]
    public class ScreenGrabberConfig
    {
        public string DeviceName { get; set; } = ScreenGrabberBase.Screens.FirstOrDefault()?.DeviceName;
        public double FrameRateLimit { get; set; } = 30;
    }
}