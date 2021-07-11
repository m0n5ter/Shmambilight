using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Media;
using NLog;
using Shmambilight.Config;
using Shmambilight.Screen;

namespace Shmambilight.Leds
{
    public class LedDevice
    {
        private readonly SerialPort _port;
        private readonly Thread _thread;
        private Color[] _colors = new Color[0];
        private double _fadeDuration;
        private double _fadeEndValue;
        private DateTime _fadeStartTime = DateTime.MinValue;
        private double _fadeStartValue;
        private bool _isClosing;

        public event Action Updated;

        private TickCounter Ticks { get; } = new TickCounter();

        public LedDevice(string portName, LedDevice original)
        {
            if (portName != null)
            {
                _port = new SerialPort(portName, 115200)
                {
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Parity = Parity.None,
                    Encoding = Encoding.ASCII,
                    ReadTimeout = 3000,
                    WriteTimeout = 3000
                };

                _port.Open();
            }

            try
            {
                Write(new byte[256 * 3]);
                _port?.Write("LEDS");
                WriteByte(0);
                ReadResponse();

                Ticks.Add();
            }
            catch
            {
                _port?.Close();
                throw;
            }

            if (original != null)
            {
                _colors = original._colors.ToArray();
                _fadeStartTime = original._fadeStartTime;
                _fadeDuration = original._fadeDuration;
                _fadeEndValue = original._fadeEndValue;
                _fadeStartValue = original._fadeStartValue;
            }

            _thread = new Thread(LedDeviceThreadProc) {IsBackground = true};
            _thread.Start();
        }

        public double CurrentFade
        {
            get
            {
                var now = DateTime.Now;

                if (now <= _fadeStartTime)
                    return _fadeStartValue;

                if (now >= _fadeStartTime.AddMilliseconds(_fadeDuration))
                    return _fadeEndValue;

                return _fadeStartValue + (_fadeEndValue - _fadeStartValue) * (now - _fadeStartTime).TotalMilliseconds / _fadeDuration;
            }
        }

        public Exception Error { get; private set; }

        public string PortName => _port?.PortName ?? "Emulated";

        public DateTime? LastUpdated { get; private set; }

        public void Close()
        {
            _isClosing = true;
            _thread.Join();
        }

        public void Fade(uint duration, double value)
        {
            var now = DateTime.Now;

            if (Math.Abs(_fadeEndValue - value) < 0.000001 && now.AddMilliseconds(duration) >= _fadeStartTime.AddMilliseconds(_fadeDuration))
                return;

            _fadeStartValue = CurrentFade;
            _fadeEndValue = value;
            _fadeStartTime = now;
            _fadeDuration = duration;
        }

        private void LedDeviceThreadProc()
        {
            var lastSentBytes = new byte[0];

            while (!_isClosing)
            {
                Thread.Sleep(5);
                Color[] colors;

                lock (this)
                    colors = _colors.ToArray();

                try
                {
                    var fade = CurrentFade;
                    var bytes = colors.SelectMany(color => new[] {(byte) Math.Round(color.R * fade), (byte) Math.Round(color.G * fade), (byte) Math.Round(color.B * fade)}).ToArray();

                    if (!lastSentBytes.SequenceEqual(bytes))
                    {
                        _port?.Write("LEDS");
                        WriteByte((byte) colors.Length);
                        Write(bytes);
                        ReadResponse();
                        lastSentBytes = bytes;

                        Ticks.Add();
                        LastUpdated = DateTime.Now;
                        Updated?.Invoke();
                    }
                }
                catch (Exception exception)
                {
                    Error = exception;
                    _port?.Dispose();
                    return;
                }
            }

            _port?.Dispose();
        }

        private void WriteByte(byte b)
        {
            Write(new[] {b});
        }

        private void Write(byte[] bytes)
        {
            try
            {
                _port?.Write(bytes, 0, bytes.Length);
            }
            catch (Exception exception)
            {
                throw new LedDeviceException("Write failed", exception);
            }
        }

        private void ReadResponse()
        {
            if (_port != null)
            {
                if (!SpinWait.SpinUntil(() => _port.BytesToRead == 2, 1000))
                    throw new LedDeviceException("Waiting for OK response timed out");

                var response = _port.ReadExisting();

                if (response != "OK")
                    throw new LedDeviceException($"Wrong response received: {response}");
            }
        }

        public void WriteLeds(IList<Led> leds)
        {
            WriteLeds(leds.Select(_ => _.Color).ToArray());
        }

        public void WriteLeds(IList<Color> colors)
        {
            if (colors == null)
                throw new ArgumentNullException(nameof(colors));

            if (colors.Count > 256)
                throw new NotSupportedException("Maximum of 256 LEDS are supported");

            lock (this)
            {
                _colors = colors.ToArray();
            }
        }

        public static LedDevice Detect(LedDevice original, Action<LogLevel, string> onLog)
        {
            if (Settings.Current.LedDevice.Port != null)
                return new LedDevice(Settings.Current.LedDevice.Port, original);

            var result = SerialPort.GetPortNames().Select(portName =>
            {
                try
                {
                    return new LedDevice(portName, original);
                }
                catch (UnauthorizedAccessException)
                {
                    onLog?.Invoke(LogLevel.Info, $"COM{portName} seems to be used by another process");
                    return null;
                }
                catch (LedDeviceException)
                {
                    onLog?.Invoke(LogLevel.Info, $"COM{portName} is available but device doesn't seem to be connected");
                    return null;
                }
                catch (Exception)
                {
                    return null;
                }
            }).FirstOrDefault(_ => _ != null);

            if (result != null)
                onLog?.Invoke(LogLevel.Info,  $"LED device detected at {result.PortName}");
            else
                onLog?.Invoke(LogLevel.Warn, "LED device was not detected");

            return result;
        }
    }
}