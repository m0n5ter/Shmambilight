using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Shmambilight.Config;
using Shmambilight.Screen;

namespace Shmambilight.Leds
{
    public class LedStripArray
    {
        private readonly LedStrip[] _ledStrips;

        private int _h;
        private int _w;
        private Thickness _margins;

        public LedStripArray()
        {
            _ledStrips = Settings.Current.LedStrips;
        }

        public DateTime LastChanged { get; private set; }

        public Led[] Leds { get; private set; }

        public void Calculate(ScreenFrame frame)
        {
            var pixels = frame.Pixels;
            var stride = frame.Stride;
            var leds = new List<Led>();

            if (frame.Width != _w || frame.Height != _h || frame.Margins != _margins)
            {
                _w = frame.Width;
                _h = frame.Height;
                _margins = frame.Margins;

                foreach (var s in _ledStrips)
                {
                    var step = s.LedCount < 2 ? 0.1 : (s.PercentEnd - s.PercentStart) / (s.LedCount - 1);
                    var screenStep2 = Math.Max(1, Math.Ceiling(Math.Abs((s.Location == LetStripLocation.Left || s.Location == LetStripLocation.Right ? _h : _w) * step)));
                    var dw = Math.Max(1, Math.Min(screenStep2 * 2, (_w - _margins.Left - _margins.Right) * 0.1));
                    var dh = Math.Max(1, Math.Min(screenStep2 * 2, (_h - _margins.Top - _margins.Bottom) * 0.1));
                    var letterbox = new Rect(_margins.Left, _margins.Top, _w - _margins.Left - _margins.Right, _h - _margins.Top - _margins.Bottom);

                    for (var i = 0; i < s.LedCount; i++)
                        leds.Add(new Led(Rect.Intersect(letterbox, s.Location == LetStripLocation.Left
                            ? new Rect(_margins.Left, (s.PercentStart + i * step) * _h - screenStep2, dw, screenStep2 * 2)
                            : s.Location == LetStripLocation.Right
                                ? new Rect(_w - _margins.Right - dw, (s.PercentStart + i * step) * _h - screenStep2, dw, screenStep2 * 2)
                                : s.Location == LetStripLocation.Top
                                    ? new Rect((s.PercentStart + i * step) * _w - screenStep2, _margins.Top, screenStep2 * 2, dh)
                                    : new Rect((s.PercentStart + i * step) * _w - screenStep2, _w - _margins.Bottom - dh, screenStep2 * 2, dh))));
                }
            }
            else
                leds = Leds.Select(led => new Led(led.ScreenArea)).ToList();

            Parallel.ForEach(leds, (led, state, i) =>
            {
                var rect = led.ScreenArea.IsEmpty
                    ? Int32Rect.Empty
                    : new Int32Rect((int) Math.Floor(led.ScreenArea.X), (int) Math.Floor(led.ScreenArea.Y), (int) Math.Ceiling(led.ScreenArea.Width),
                        (int) Math.Ceiling(led.ScreenArea.Height));

                if (rect.IsEmpty)
                    led.Color = Colors.Black;
                else
                {
                    var sr = 0d;
                    var sg = 0d;
                    var sb = 0d;

                    var u = rect.Y * stride + rect.X * 4;

                    for (var y = 0; y < rect.Height; y++)
                    {
                        for (var x = 0; x < rect.Width; x++)
                        {
                            var b = pixels[u++];
                            var g = pixels[u++];
                            var r = pixels[u++];
                            u++;

                            sr += r;
                            sg += g;
                            sb += b;
                        }

                        u += stride - rect.Width * 4;
                    }

                    var pc = rect.Width * rect.Height;
                    led.Color = Color.FromArgb(255, (byte) (sr / pc), (byte) (sg / pc), (byte) (sb / pc));
                }
            });

            if (Leds != null && (leds.Count != Leds.Length || Enumerable.Range(0, leds.Count).Any(i => leds[i].Color != Leds[i].Color || leds[i].ScreenArea != Leds[i].ScreenArea)))
                LastChanged = DateTime.Now;

            Leds = leds.ToArray();
        }
    }
}