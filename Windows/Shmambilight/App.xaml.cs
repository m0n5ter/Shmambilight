using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using NLog;
using Shmambilight.UI;
using Shmambilight.ViewModels;
using MessageBox = System.Windows.MessageBox;

namespace Shmambilight
{
    public partial class App
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private static Mutex _mutex;

        private DebugWindow _debugWindow;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += (sender, ex) =>
            {
                _logger.Error("DispatcherUnhandledException");
                ShowError(ex.Exception);

                ex.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, ex) =>
            {
                _logger.Error("UnhandledException");

                ShowError(ex.ExceptionObject as Exception ??
                          new ApplicationException($"{nameof(AppDomain.UnhandledException)}; {nameof(sender)}: {sender?.GetType()}; {nameof(ex.ExceptionObject)}: {ex.ExceptionObject?.GetType()}"));
            };

            TaskScheduler.UnobservedTaskException += (sender, ex) =>
            {
                _logger.Error("UnobservedTaskException");
                ShowError(ex.Exception);

                ex.SetObserved();
            };

            try
            {
                const string mutexName = "Shmambilight_Mutex";

                if (Mutex.TryOpenExisting(mutexName, out _))
                {
                    Shutdown(-2);
                    return;
                }

                _mutex = new Mutex(true, mutexName);

                var mainViewModel = new MainViewModel();
                mainViewModel.Start();

                _debugWindow = new DebugWindow {DataContext = mainViewModel};
                
                var notifyIcon = new NotifyIcon
                {
                    Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                    Visible = true
                };

                _debugWindow.IsVisibleChanged += (sender, args) => mainViewModel.IsDebug = _debugWindow.IsVisible;

                notifyIcon.DoubleClick += (sender, args) =>
                {
                    if (_debugWindow.IsVisible)
                        _debugWindow.Hide();
                    else
                        _debugWindow.Show();
                };

                _debugWindow.Closing += (sender, args) =>
                {
                    if (!mainViewModel.IsExiting)
                    {
                        mainViewModel.IsExiting = true;
                        args.Cancel = true;
                        _debugWindow.Hide();

                        Task.Run(() =>
                        {
                            mainViewModel.Stop();
                            _debugWindow.Dispatcher?.Invoke(() => _debugWindow.Close());
                        });
                    }
                };
            }
            catch (Exception exception)
            {
                ShowError(new ApplicationException("Startup failed", exception));
                Shutdown(-1);
            }
        }

        private void ShowError(Exception exception)
        {
            _logger.Error(exception);
            MessageBox.Show(exception.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}