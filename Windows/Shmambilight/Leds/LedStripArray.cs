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

        public DateTime LastUpdated { get; private set; }

        public Led[] Leds { get; private set; }

        public int LedCount => _ledStrips.Sum(_ => _.LedCount);

        public void Calculate(ScreenFrame frame)
        {
            var pixels = frame.Pixels;
            var stride = frame.Width * 4;
            var leds = new List<Led>();

            if (frame.Width != _w || frame.Height != _h || frame.Margins != _margins)
            {
                _w = frame.Width;
                _h = frame.Height;
                _margins = frame.Margins;

                var letterbox = new Rect(_margins.Left, _margins.Top, _w - _margins.Left - _margins.Right, _h - _margins.Top - _margins.Bottom);

                foreach (var s in _ledStrips)
                {
                    if (s.LedCount < 2)
                        continue;

                    var letterboxDistance = (s.IsHorizontal ? letterbox.Width : letterbox.Height) * (s.PercentEnd - s.PercentStart) / (s.LedCount - 1);
                    var screenDistance = (s.IsHorizontal ? _w : _h) * (s.PercentEnd - s.PercentStart) / (s.LedCount - 1);
                    var spotWidth = Math.Abs(letterboxDistance * s.SpotWidthMultiplier);
                    var spotHeight = Math.Abs(letterboxDistance * s.SpotHeightMultiplier);

                    Point letterboxPoint;
                    Point screenPoint;
                    Point connectorPoint;

                    switch (s.Location)
                    {
                        case LetStripLocation.Left:
                            connectorPoint = new Point(letterbox.Left, letterbox.Top + letterbox.Height * s.PercentStart);
                            letterboxPoint = new Point(connectorPoint.X + spotWidth / 2, connectorPoint.Y);
                            screenPoint = new Point(0, _h * s.PercentStart);
                            break;

                        case LetStripLocation.Top:
                            connectorPoint = new Point(letterbox.Left + letterbox.Width * s.PercentStart, letterbox.Top);
                            letterboxPoint = new Point(connectorPoint.X, connectorPoint.Y + spotHeight / 2);
                            screenPoint = new Point(_w * s.PercentStart, 0);
                            break;

                        case LetStripLocation.Right:
                            connectorPoint = new Point(letterbox.Right, letterbox.Top + letterbox.Height * s.PercentStart);
                            letterboxPoint = new Point(connectorPoint.X - spotWidth / 2, connectorPoint.Y);
                            screenPoint = new Point(_w, _h * s.PercentStart);
                            break;

                        case LetStripLocation.Bottom:
                            connectorPoint = new Point(letterbox.Left + letterbox.Width * s.PercentStart, letterbox.Bottom);
                            letterboxPoint = new Point(connectorPoint.X, connectorPoint.Y - spotHeight / 2);
                            screenPoint = new Point(_w * s.PercentStart, _h);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    var letterboxOffset = s.IsHorizontal ? new Point(letterboxDistance, 0) : new Point(0, letterboxDistance);
                    var screenOffset = s.IsHorizontal ? new Point(screenDistance, 0) : new Point(0, screenDistance);

                    for (var i = 0; i < s.LedCount; i++)
                    {
                        leds.Add(new Led
                        {
                            GrabArea = Rect.Intersect(new Rect(letterboxPoint.X - spotWidth / 2, letterboxPoint.Y - spotHeight / 2, spotWidth, spotHeight), letterbox),
                            Location = screenPoint,
                            ConnectorPoint = connectorPoint
                        });

                        letterboxPoint.Offset(letterboxOffset.X, letterboxOffset.Y);
                        connectorPoint.Offset(letterboxOffset.X, letterboxOffset.Y);
                        screenPoint.Offset(screenOffset.X, screenOffset.Y);
                    }
                }
            }
            else
                leds = Leds.Select(_ => new Led
                {
                    GrabArea = _.GrabArea, 
                    ConnectorPoint = _.ConnectorPoint, 
                    Location = _.Location
                }).ToList();

            Parallel.ForEach(leds, (led, state, i) =>
            {
                var rect = led.GrabArea.IsEmpty
                    ? Int32Rect.Empty
                    : new Int32Rect((int) Math.Floor(led.GrabArea.X), (int) Math.Floor(led.GrabArea.Y), (int) Math.Ceiling(led.GrabArea.Width),
                        (int) Math.Ceiling(led.GrabArea.Height));

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

            if (Leds != null && (leds.Count != Leds.Length || Enumerable.Range(0, leds.Count).Any(i => leds[i].Color != Leds[i].Color || leds[i].GrabArea != Leds[i].GrabArea)))
                LastUpdated = DateTime.Now;

            Leds = leds.ToArray();
        }
    }
}