using System;

namespace Shmambilight.Leds
{
    public class LedDeviceException : Exception
    {
        public LedDeviceException(string message) : base(message)
        {
        }

        public LedDeviceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}