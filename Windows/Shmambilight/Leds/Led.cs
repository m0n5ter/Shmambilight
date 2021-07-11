using System.Windows;
using System.Windows.Media;

namespace Shmambilight.Leds
{
    public class Led
    {
        public Rect GrabArea { get; set; }

        public Point Location { get; set; }

        public Point ConnectorPoint { get; set; }

        public Color Color { get; set; }
    }
}