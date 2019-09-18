using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Shmambilight.Config;
using Shmambilight.Leds;
using Shmambilight.Screen;
using Shmambilight.UI;
using Shmambilight.Utils;

namespace Shmambilight
{
    public partial class App
    {
        private static LogRecord _lastLog;
        public static Mutex _mutex;
        private readonly int _currentProcessId = Process.GetCurrentProcess().Id;
        private DebugWindow _debugWindow;
        private bool _isExiting;
        private Thread _mainThread;

        public ScreenGrabber ScreenGrabber { get; private set; }

        public LedDevice LedDevice { get; private set; }

        public LedStripArray LedStrips { get; private set; }

        public static ObservableCollection<LogRecord> Log { get; } = new ObservableCollection<LogRecord>();

        private static void AddLog(LogRecord logRecord)
        {
            Current.Dispatcher?.Invoke(() =>
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

        public static void LogInfo(string message)
        {
            AddLog(new LogRecord(LogLevel.Info, message));
        }

        public static void LogError(string message, Exception exception = null)
        {
            AddLog(new LogRecord(LogLevel.Error, message, exception));
        }

        public static void LogWarning(string message, Exception exception = null)
        {
            AddLog(new LogRecord(LogLevel.Warning, message, exception));
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            const string mutexName = "Shmambilight_Mutex";

            if (Mutex.TryOpenExisting(mutexName, out _))
            {
                Shutdown(-4);
                return;
            }

            _mutex = new Mutex(true, mutexName);

            _debugWindow = new DebugWindow();

            var notifyIcon = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                Visible = true
            };

            notifyIcon.DoubleClick += (sender, args) =>
            {
                if (_debugWindow.IsVisible)
                    _debugWindow.Hide();
                else
                    _debugWindow.Show();
            };


            _debugWindow.Closing += (sender, args) =>
            {
                if (!_isExiting)
                {
                    _isExiting = true;
                    args.Cancel = true;
                    _debugWindow.Hide();

                    Task.Run(() =>
                    {
                        _mainThread.Join();
                        _debugWindow.Dispatcher?.Invoke(() => _debugWindow.Close());
                    });
                }
            };

            _mainThread = new Thread(MainThreadProc);
            _mainThread.Start();
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
                    Thread.Sleep(10);

                    ScreenGrabber = ScreenGrabber ?? new ScreenGrabber();
                    LedStrips = LedStrips ?? new LedStripArray();

                    if (LedDevice == null && ledDeviceDetectingTask == null && (now - lastDeviceCheck).TotalSeconds > 5)
                    {
                        ledDeviceDetectingTask = new Task(() =>
                        {
                            LedDevice = LedDevice.Detect();
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
                        ScreenGrabber.IsPaused = !enableForProcess;
                        lastProcessCheck = DateTime.Now;
                    }

                    if (enableForProcess)
                    {
                        if (ScreenGrabber.LastUpdateTime > lastFrameTime)
                        {
                            lastFrameTime = ScreenGrabber.LastUpdateTime;
                            var screenFrame = ScreenGrabber.ScreenFrame;

                            if (screenFrame != null)
                            {
                                LedStrips.Calculate(screenFrame);

                                if ((now - LedStrips.LastChanged).TotalSeconds > 3)
                                {
                                    LedDevice?.Fade(2000, 0);
                                }
                                else
                                {
                                    LedDevice?.Fade(100, 1);
                                    LedDevice?.WriteLeds(LedStrips.Leds);
                                }
                            }
                        }
                        else if ((now - ScreenGrabber.LastUpdateTime).TotalSeconds > 3)
                        {
                            LedDevice?.Fade(2000, 0);
                        }
                    }
                    else
                    {
                        LedDevice?.Fade(2000, 0);
                    }

                    _debugWindow?.Update(this);
                }
                catch (Exception exception)
                {
                    LogError("Error occured", exception);
                }
            }

            ScreenGrabber?.Close();

            if (LedDevice != null)
            {
                LedDevice?.Fade(1000, 0);
                SpinWait.SpinUntil(() => LedDevice?.Error != null || LedDevice?.CurrentFade <= 0, 4000);
                LedDevice?.Close();
            }
        }
    }
}