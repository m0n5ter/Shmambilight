using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Shmambilight.Config;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using ResultCode = SharpDX.DXGI.ResultCode;

namespace Shmambilight.Screen
{
    public class ScreenGrabber
    {
        private readonly int _adapterIndex;
        private readonly int _outputIndex;
        private readonly Thread _thread;
        private bool _isRunning;
        private readonly List<DateTime> _ticks = new List<DateTime>();

        public bool IsPaused { get; set; }

        private ScreenGrabber(int adapterIndex, int outputIndex)
        {
            _adapterIndex = adapterIndex;
            _outputIndex = outputIndex;
            _isRunning = true;

            lock (_ticks)
                _ticks.Add(DateTime.Now);

            _thread = new Thread(ScreenGrabberThreadProc) { IsBackground = true };
            _thread.Start();
        }

        public ScreenGrabber() : this(Settings.Current.ScreenGrabber.AdapterIndex, Settings.Current.ScreenGrabber.OutputIndex)
        {
        }

        public ScreenFrame ScreenFrame { get; private set; }

        public DateTime LastUpdateTime
        {
            get
            {
                lock (_ticks)
                    return _ticks.Last();
            }
        }

        public double Fps { get; private set; }

        public void Close()
        {
            _isRunning = false;
            _thread.Join();
        }

        private void ScreenGrabberThreadProc()
        {
            while (_isRunning)
            {
                var factory = new Factory1();
                var adapter = factory.GetAdapter1(_adapterIndex);
                var device = new Device(adapter);
                var output = adapter.GetOutput(_outputIndex);
                var output1 = output.QueryInterface<Output1>();

                var width = output.Description.DesktopBounds.Right;
                var height = output.Description.DesktopBounds.Bottom;
                var stride = width * 4;

                var screenTexture = new Texture2D(device, new Texture2DDescription
                {
                    CpuAccessFlags = CpuAccessFlags.Read,
                    BindFlags = BindFlags.None,
                    Format = Format.B8G8R8A8_UNorm,
                    Width = output.Description.DesktopBounds.Right,
                    Height = output.Description.DesktopBounds.Bottom,
                    OptionFlags = ResourceOptionFlags.None,
                    MipLevels = 1,
                    ArraySize = 1,
                    SampleDescription = {Count = 1, Quality = 0},
                    Usage = ResourceUsage.Staging
                });

                var duplicatedOutput = output1.DuplicateOutput(device);
                ScreenFrame = new ScreenFrame(new byte[stride * height], width);

                while (_isRunning)
                {
                    Thread.Sleep(10);

                    if (!IsPaused)
                    {
                        try
                        {
                            duplicatedOutput.AcquireNextFrame(1000, out _, out var screenResource);

                            using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
                            {
                                device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);
                            }

                            var mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, MapFlags.None);

                            try
                            {
                                Marshal.Copy(mapSource.DataPointer, ScreenFrame.Pixels, 0, ScreenFrame.Pixels.Length);
                                ScreenFrame.UpdateMargins();

                                lock (_ticks)
                                {
                                    _ticks.Add(DateTime.Now);

                                    while (_ticks.Count > 11)
                                        _ticks.RemoveAt(0);

                                    Fps = 1 / _ticks.Skip(1).Select((dt, i) => (dt - _ticks[i]).TotalSeconds).Average();
                                }
                            }
                            finally
                            {
                                screenResource.Dispose();
                                duplicatedOutput.ReleaseFrame();
                            }
                        }
                        catch (SharpDXException exception)
                        {
                            if (exception.ResultCode.Code == ResultCode.AccessLost.Result.Code)
                            {
                                duplicatedOutput.Dispose();
                                screenTexture.Dispose();
                                output1.Dispose();
                                output.Dispose();
                                device.Dispose();
                                adapter.Dispose();
                                factory.Dispose();

                                break;
                            }

                            if (exception.ResultCode.Code != ResultCode.WaitTimeout.Result.Code)
                                App.LogError("ScreenGrabber error occured", exception);
                        }
                    }
                }
            }
        }
    }
}