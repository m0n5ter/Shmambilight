using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Shmambilight.Leds;
using Shmambilight.Screen;
using Shmambilight.ViewModels;

namespace Shmambilight.UI
{
    public class LedViewControl : UserControl
    {
        private readonly Pen _borderPen;
        private MainViewModel _viewModel;

        public LedViewControl()
        {
            _borderPen = new Pen(Brushes.Black, 1);
            _borderPen.Freeze();

            DataContextChanged += (sender, args) =>
            {
                if (_viewModel != null) _viewModel.LedsUpdated -= OnLedsUpdated;
                _viewModel = args.NewValue as MainViewModel;
                if (_viewModel != null) _viewModel.LedsUpdated += OnLedsUpdated;
            };
        }

        private void OnLedsUpdated()
        {
            Dispatcher?.BeginInvoke(DispatcherPriority.Normal, (Action) (() =>
            {
                var window = Window.GetWindow(this);

                if (window?.WindowState == WindowState.Minimized || window?.IsVisible != true)
                    return;

                InvalidateVisual();
            }));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var app = DataContext as MainViewModel;

            if (app?.LedStrips?.Leds is Led[] allLeds && allLeds.Where(_ => !_.GrabArea.IsEmpty).ToArray() is Led[] leds && leds.Any() && (app.ScreenGrabber.Frame is ScreenFrame frame))
            {
                var fade = app.LedDevice?.CurrentFade ?? 1;

                drawingContext.DrawRectangle(Brushes.Bisque, _borderPen, new Rect(0, 0, ActualWidth, ActualHeight));

                drawingContext.DrawRectangle(Brushes.Pink, _borderPen, new Rect(frame.Margins.Left, frame.Margins.Top,
                    app.ScreenGrabber.Screen.ScreenSize.Width - frame.Margins.Left - frame.Margins.Right, app.ScreenGrabber.Screen.ScreenSize.Height - frame.Margins.Top - frame.Margins.Bottom));

                //foreach (var led in leds)
                //{
                //    drawingContext.DrawRoundedRectangle(new SolidColorBrush(Color.Multiply(led.Color, (float) fade * 0.3f)), _borderPen, led.GrabArea, 5, 5);
                //}

                foreach (var led in leds)
                {
                    if (led.Location != led.ConnectorPoint)
                    {
                        drawingContext.DrawEllipse(Brushes.Black, null, led.ConnectorPoint, 5, 5);
                        drawingContext.DrawLine(_borderPen, led.ConnectorPoint, led.Location);
                    }

                    drawingContext.DrawRoundedRectangle(Brushes.Transparent, _borderPen, led.GrabArea, 5, 5);
                }

                foreach (var led in leds)
                {
                    var c = Color.Multiply(led.Color, (float) fade);
                    
                    drawingContext.PushClip(new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(new Rect(-300, -300, ActualWidth + 600, ActualHeight + 600)),
                        new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight))));

                    drawingContext.DrawEllipse(new SolidColorBrush(c), null, led.Location, 250, 250);

                    drawingContext.Pop();
                }

            }
        }
    }
}