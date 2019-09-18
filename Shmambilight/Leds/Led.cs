using System.Windows;
using System.Windows.Media;

namespace Shmambilight.Leds
{
    public class Led
    {
        public Rect ScreenArea { get; }

        public Color Color { get; set; }

        public Led(Rect screenArea)
        {
            ScreenArea = screenArea;
        }
    }
}