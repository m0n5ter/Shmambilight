using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Shmambilight.Screen
{
    public class TickCounter : INotifyPropertyChanged
    {
        private readonly List<DateTime> _ticks = new List<DateTime>();
        private DateTime _lastFpsReport = DateTime.MinValue;

        public event PropertyChangedEventHandler PropertyChanged;
        
        public double Fps { get; private set; }

        public int Count { get; private set; }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public DateTime Last { get; private set; } = DateTime.MinValue;

        public void Add()
        {
            var now = DateTime.Now;

            lock (_ticks)
            {
                _ticks.Add(now);
                Count++;
                Last = now;
                OnPropertyChanged(nameof(Last));

                while (_ticks.Count > 50)
                {
                    _ticks.RemoveAt(0);
                }

                Fps = _ticks.Count > 1 ? 1 / _ticks.Skip(1).Select((dt, i) => (dt - _ticks[i]).TotalSeconds).Average() : 0;

                if ((now - _lastFpsReport).TotalSeconds > 1)
                {
                    OnPropertyChanged(nameof(Fps));
                    OnPropertyChanged(nameof(Count));
                    _lastFpsReport = now;
                }
            }
        }

        public void Clear()
        {
            lock (_ticks)
            {
                _ticks.Clear();
                Count = 0;
            }
        }
    }
}