using System;
using System.Windows;
using Shmambilight.Config;

namespace Shmambilight.Screen
{
    public class ScreenFrame
    {
        public byte[] Pixels { get; }

        public int Stride { get; }

        public int Width { get; }

        public int Height { get; }

        public Thickness Margins { get; set; }
        
        private Thickness _newMargins;
        private DateTime _newMarginsTime = DateTime.Now;

        public ScreenFrame(byte[] pixels, int width)
        {
            Pixels = pixels;
            Width = width;
            Stride = width * 4;
            Margins = new Thickness(0);

            if (pixels.Length % Stride != 0)
                throw new ArgumentException("Pixels are not aligned properly");

            Height = pixels.Length / Stride;
        }

        public void UpdateMargins()
        {
            var w2 = Width / 2;
            var left = w2;
            var top = 0;

            for (var y = 0; y < Height / 2; y++)
            {
                var f = 0;
                var u = y * Stride;

                for (var x = 0; x < left; x++)
                {
                    if (Pixels[u++] != 0 || Pixels[u++] != 0 || Pixels[u++] != 0 || Pixels[u++] != 0xFF)
                        break;
                    f++;
                }

                if (f < left)
                    left = f;

                if (f == w2 && left == w2)
                    top++;
            }

            var newMargins = new Thickness(left, top, left, top);

            if (newMargins != _newMargins)
            {
                _newMargins = newMargins;
                _newMarginsTime = DateTime.Now;
            }
            else
            {
                if ((DateTime.Now - _newMarginsTime).TotalSeconds > Settings.Current.MarginChangeDelay)
                    Margins = newMargins;
            }
        }
    }
}