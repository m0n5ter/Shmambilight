using System.Linq;
using System.Windows;
using System.Windows.Media;
using Shmambilight.Leds;

namespace Shmambilight.UI
{
    public partial class DebugWindow
    {
        public DebugWindow()
        {
            InitializeComponent();
        }

        public void Update(App app)
        {
            if (Dispatcher?.Invoke(() => WindowState == WindowState.Minimized || !IsVisible) ?? true)
                return;
            
            var fade = app.LedDevice?.CurrentFade ?? 1;
            var screenFrame = app.ScreenGrabber?.ScreenFrame;

            var leds = app.LedStrips?.Leds?.Select(led => new Led(led.ScreenArea) {Color = Color.FromArgb(255, (byte) (led.Color.R * fade), (byte) (led.Color.G * fade), (byte) (led.Color.B * fade))})
                .ToArray();

            Dispatcher?.Invoke(() =>
            {
                if (!(LedView.Leds is Led[] curLeds) 
                    || leds != null && (leds.Length != curLeds.Length || leds.Select((led, i) => led.Color != curLeds[i].Color || led.ScreenArea != curLeds[i].ScreenArea).Any()))
                    LedView.Leds = leds;

                if (screenFrame != null)
                {
                    Grid.Width = LedView.Width = screenFrame.Width;
                    Grid.Height = LedView.Height = screenFrame.Height;
                    Letterbox.Margin = screenFrame.Margins;
                }

                ((StatsRow)Stats.Items[0]).Value = app.LedDevice?.PortName ?? "???";
                ((StatsRow)Stats.Items[1]).Value = leds?.Length.ToString() ?? "???";
                ((StatsRow) Stats.Items[2]).Value = $"{screenFrame?.Width.ToString() ?? "???"}x{screenFrame?.Height.ToString() ?? "???"}";
                ((StatsRow) Stats.Items[3]).Value = $"{screenFrame?.Margins.Left.ToString("0") ?? "???"};{screenFrame?.Margins.Top.ToString("0") ?? "???"}";
                ((StatsRow)Stats.Items[4]).Value = app.ScreenGrabber?.Fps.ToString("0.0") ?? "???";
                ((StatsRow)Stats.Items[5]).Value = app.LedDevice?.Fps.ToString("0.0") ?? "???";

                LedView.InvalidateVisual();
            });

        }
    }
}