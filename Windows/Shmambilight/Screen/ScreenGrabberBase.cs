using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NLog;
using SharpDX;
using Shmambilight.Config;

namespace Shmambilight.Screen
{
    public abstract class ScreenGrabberBase: IDisposable
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        protected readonly int _width;
        protected readonly int _height;
        protected readonly int _left;
        protected readonly int _top;
        protected readonly int _stride;
        private DateTime _lastFrameUpdateTime;

        public ScreenFrame Frame { get; }

        public TickCounter GrabCounter { get; } = new TickCounter();

        public static IList<MonitorInfo> Screens => Native.GetScreens();
        public DateTime LastUpdateTime => _lastFrameUpdateTime;
        public MonitorInfo Screen { get; }

        protected ScreenGrabberBase(MonitorInfo screen)
        {
            _logger.Error($"Staring screen grabber: {GetType().Name}, {screen.DeviceName}, {screen.MonitorArea}");

            Screen = screen;
            
            _width = (int)screen.MonitorArea.Width;
            _height = (int)screen.MonitorArea.Height;
            _left = (int)screen.MonitorArea.Left;
            _top = (int)screen.MonitorArea.Top;
            _stride = _width * 4;

            Frame = new ScreenFrame(_width, _height, new byte[_stride * _height]);
        }

        public ScreenFrame Grab()
        {
            if ((DateTime.Now - _lastFrameUpdateTime).TotalSeconds < 1 / Settings.Current.ScreenGrabber.FrameRateLimit)
                return null;

            _lastFrameUpdateTime = Frame.LastUpdate;
            
            GrabPrivate();
            GrabCounter.Add();
            return Frame;
        }

        public abstract void GrabPrivate();

        protected void CopyFromMapSource(DataBox mapSource)
        {
            if (mapSource.RowPitch == _stride)
                Marshal.Copy(mapSource.DataPointer, Frame.Pixels, 0, Frame.Pixels.Length);
            else
            {
                var u = 0;
                var v = 0;

                for (var y = 0; y < _height; y++, u += mapSource.RowPitch, v += _stride) Marshal.Copy(mapSource.DataPointer + u, Frame.Pixels, v, _stride);
            }

            Frame.Update();
        }

        public virtual void Dispose()
        {
            _logger.Error($"Stopping screen recording: {GrabCounter.Count} frames grabbed");
        }
    }
}