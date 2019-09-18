using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using Shmambilight.Leds;

namespace Shmambilight.UI
{
    public class LedViewControl : UserControl
    {
        public Led[] Leds { get; set; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (Leds == null)
                return;

            foreach (var led in Leds.Where(_ => !_.ScreenArea.IsEmpty))
            {
                var radius = Math.Min(led.ScreenArea.Width, led.ScreenArea.Height) * 0.1;
                drawingContext.DrawRoundedRectangle(new SolidColorBrush(led.Color), new Pen(Brushes.Black, 1), led.ScreenArea, radius, radius);
            }
        }
    }
}