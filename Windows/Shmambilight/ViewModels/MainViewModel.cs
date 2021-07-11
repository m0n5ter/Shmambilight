using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using NLog;
using Shmambilight.Config;
using Shmambilight.Leds;
using Shmambilight.Mvvm;
using Shmambilight.Screen;
using Shmambilight.Utils;

namespace Shmambilight.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ScreenGrabberBase _screenGrabber;
        private LedDevice _ledDevice;
        private LedStripArray _ledStrips;
        private Thread _mainThread;
        private readonly Dispatcher _dispatcher;

        private static LogRecord _lastLog;
        private readonly int _currentProcessId = Process.GetCurrentProcess().Id;
        private bool _isExiting;

        public event Action LedsUpdated;

        public bool IsDebug { get; set; }

        public bool IsExiting
        {
            get => _isExiting;
            set => Set(ref _isExiting, value);
        }

        public ScreenGrabberBase ScreenGrabber
        {
            get => _screenGrabber;
            private set => Set(ref _screenGrabber, value);
        }

        public LedDevice LedDevice
        {
            get => _ledDevice;
            private set => Set(ref _ledDevice, value);
        }

        public LedStripArray LedStrips
        {
            get => _ledStrips;
            private set => Set(ref _ledStrips, value);
        }

        public static ObservableCollection<LogRecord> Log { get; } = new ObservableCollection<LogRecord>();

        public MainViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public void Start()
        {
            lock (this)
            {
                if (_mainThread != null) Stop();
                _mainThread = new Thread(MainThreadProc);
                _mainThread.Start();
            }
        }

        public void Stop()
        {
            lock (this)
            {
                _isExiting = true;
                _mainThread.Join();
                _mainThread = null;
            }
        }

        private void AddLog(LogLevel level, string message) => AddLog(new LogRecord(level, message));

        private void AddLog(LogRecord logRecord)
        {
            _dispatcher?.Invoke(() =>
            {
                if (_lastLog?.Message != logRecord.Message)
                {
                    Log.Add(logRecord);

                    while (Log.Count > 10000)
                        Log.RemoveAt(0);

                    _lastLog = logRecord;
                }
            });
        }

        public void LogInfo(string message)
        {
            AddLog(new LogRecord(LogLevel.Info, message));
        }

        public void LogError(string message, Exception exception = null)
        {
            AddLog(new LogRecord(LogLevel.Error, message, exception));
        }

        public void LogWarning(string message, Exception exception = null)
        {
            AddLog(new LogRecord(LogLevel.Warn, message, exception));
        }

        private void MainThreadProc()
        {
            Task ledDeviceDetectingTask = null;
            var lastProcessCheck = DateTime.MinValue;
            var lastDeviceCheck = DateTime.MinValue;
            var enableForProcess = false;
            var lastFrameTime = DateTime.MinValue;

            while (!_isExiting)
            {
                try
                {
                    var now = DateTime.Now;
                    Thread.Sleep(1);

                    ScreenGrabber = ScreenGrabber ?? new ScreenGrabberDxgi(ScreenGrabberBase.Screens.FirstOrDefault(_ => _.DeviceName == Settings.Current.ScreenGrabber.DeviceName) ??
                                                                                     ScreenGrabberBase.Screens.FirstOrDefault());
                    
                    LedStrips = LedStrips ?? new LedStripArray();

                    if ((LedDevice == null || LedDevice.PortName == "Emulated") && ledDeviceDetectingTask == null && (now - lastDeviceCheck).TotalSeconds > 5)
                    {
                        ledDeviceDetectingTask = new Task(() =>
                        {
                            var oldDevice = LedDevice;
                            
                            if (oldDevice != null)
                            {
                                oldDevice.Updated -= OnLedsUpdated;
                                oldDevice.Close();
                            }

                            LedDevice = LedDevice.Detect(oldDevice, AddLog) ?? new LedDevice(null, oldDevice);

                            if (LedDevice != null) LedDevice.Updated += OnLedsUpdated;

                            ledDeviceDetectingTask = null;
                            lastDeviceCheck = DateTime.Now;
                        });

                        ledDeviceDetectingTask.Start();
                    }

                    if (LedDevice?.Error != null)
                    {
                        LogError("Led device error", LedDevice?.Error);
                        LedDevice = null;
                    }

                    if ((now - lastProcessCheck).TotalSeconds > 1)
                    {
                        var process = ProcessUtils.GetForegroundProcess();
                        enableForProcess = process.Id == _currentProcessId || Settings.Current.EnableForProcess(process);
                        lastProcessCheck = DateTime.Now;
                    }

                    if (enableForProcess || IsDebug)
                    {
                        var frame = ScreenGrabber.Grab();
                        
                        if (ScreenGrabber.LastUpdateTime > lastFrameTime)
                        {
                            lastFrameTime = ScreenGrabber.LastUpdateTime;

                            if (frame != null)
                            {
                                frame.DetectMargins();
                                LedStrips.Calculate(frame);

                                if ((now - LedStrips.LastUpdated).TotalSeconds > 3)
                                {
                                    LedDevice?.Fade(Settings.Current.FadeOutDuration, 0);
                                }
                                else
                                {
                                    LedDevice?.Fade(Settings.Current.FadeInDuration, 1);
                                    LedDevice?.WriteLeds(LedStrips.Leds);
                                }
                            }
                        }
                        else if ((now - ScreenGrabber.LastUpdateTime).TotalSeconds > 3)
                        {
                            LedDevice?.Fade(Settings.Current.FadeOutDuration, 0);
                        }
                    }
                    else
                    {
                        LedDevice?.Fade(Settings.Current.FadeOutDuration, 0);
                    }
                }
                catch (Exception exception)
                {
                    LogError("Error occurred", exception);
                }
            }

            ScreenGrabber?.Dispose();

            if (LedDevice != null)
            {
                LedDevice?.Fade(Settings.Current.FadeOutDuration, 0);
                SpinWait.SpinUntil(() => LedDevice?.Error != null || LedDevice?.CurrentFade <= 0, (int) (Settings.Current.FadeOutDuration * 2));
                LedDevice?.Close();
            }
        }

        private void OnLedsUpdated() => LedsUpdated?.Invoke();
    }
}
