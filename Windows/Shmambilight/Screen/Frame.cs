using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Shmambilight.Config;

namespace Shmambilight.Screen
{
    public class ScreenFrame: INotifyPropertyChanged
    {
        private BitmapSource _bitmap;
        private Thickness _newMargins;
        private DateTime _newMarginsTime = DateTime.Now;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Width { get; private set; }

        public int Height{ get; private set; }

        public byte[] Pixels{ get; private set; }

        public DateTime LastUpdate { get; private set; }

        public Thickness Margins { get; set; }

        public BitmapSource Bitmap
        {
            get
            {
                if (_bitmap == null && Pixels != null)
                {
                    _bitmap = BitmapSource.Create(Width, Height, 96, 96, PixelFormats.Bgra32, null, Pixels, Width * 4);
                    _bitmap.Freeze();
                }

                return _bitmap;
            }
        }

        public ScreenFrame(int width, int height, byte[] pixels) => Update(width, height, pixels);

        public void Update()
        {
            LastUpdate = DateTime.Now;
            _bitmap = null;

            OnPropertyChanged(nameof(Pixels));
            OnPropertyChanged(nameof(LastUpdate));
            OnPropertyChanged(nameof(Bitmap));
        }

        public void Update(int width, int height, byte[] pixels)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
            LastUpdate = DateTime.Now;
            _bitmap = null;

            OnPropertyChanged(nameof(Width));
            OnPropertyChanged(nameof(Height));
            OnPropertyChanged(nameof(Pixels));
            OnPropertyChanged(nameof(LastUpdate));
            OnPropertyChanged(nameof(Bitmap));
        }

        public void DetectMargins()
        {
            var w2 = Width / 2;
            var left = w2;
            var top = 0;
            var pixels = Pixels;

            for (var y = 0; y < Height / 2; y++)
            {
                var f = 0;
                var u = y * Width * 4;

                for (var x = 0; x < left; x++)
                {
                    if (pixels[u++] != 0 || pixels[u++] != 0 || pixels[u++] != 0 || pixels[u++] != 0xFF)
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

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}