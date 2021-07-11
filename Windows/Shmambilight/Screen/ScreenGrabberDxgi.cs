using System;
using System.Linq;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using ResultCode = SharpDX.DXGI.ResultCode;

namespace Shmambilight.Screen
{
    public class ScreenGrabberDxgi : ScreenGrabberBase
    {
        private Factory1 _factory;
        private Adapter1 _adapter;
        private Device _device;
        private Output _output;
        private Output1 _output1;
        private OutputDuplication _duplicatedOutput;
        private Texture2D _screenTexture;

        public ScreenGrabberDxgi(MonitorInfo screen) : base(screen)
        {
            Initialize();
        }

        private void Initialize()
        {
            _factory = new Factory1();

            var adapterOutput = _factory.Adapters1
                .Select(adapter => new {adapter, adapter.Outputs})
                .SelectMany(_ => _.Outputs.Select(output => new {_.adapter, output}))
                .FirstOrDefault(_ => _.output.Description.MonitorHandle == Screen.HMon) ?? throw new InvalidOperationException();

            _device = new Device(adapterOutput.adapter);
            _output = adapterOutput.output;
            _adapter = adapterOutput.adapter;
            _output1 = _output.QueryInterface<Output1>();

            _screenTexture = new Texture2D(_device, new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = _width,
                Height = _height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            });

            _duplicatedOutput = _output1.DuplicateOutput(_device);
        }

        public override void Dispose()
        {
            base.Dispose();
            Cleanup();
        }

        private void Cleanup()
        {
            _duplicatedOutput?.Dispose();
            _screenTexture?.Dispose();
            _output1?.Dispose();
            _output?.Dispose();
            _device?.Dispose();
            _adapter?.Dispose();
            _factory?.Dispose();
        }

        public override void GrabPrivate()
        {
            try
            {
                _duplicatedOutput.AcquireNextFrame(10000, out _, out var screenResource);

                using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
                {
                    _device.ImmediateContext.CopyResource(screenTexture2D, _screenTexture);
                }

                var mapSource = _device.ImmediateContext.MapSubresource(_screenTexture, 0, MapMode.Read, MapFlags.None);

                try
                {
                    CopyFromMapSource(mapSource);
                }
                finally
                {
                    _device.ImmediateContext.UnmapSubresource(_screenTexture, 0);
                    screenResource.Dispose();
                    _duplicatedOutput.ReleaseFrame();
                }
            }
            catch (SharpDXException exception)
            {
                if (exception.ResultCode.Code == ResultCode.AccessLost.Result.Code)
                {
                    Cleanup();
                    Initialize();
                    return;
                }

                if (exception.ResultCode.Code == ResultCode.WaitTimeout.Result.Code)
                {
                    return;
                }

                throw;
            }
        }
    }
}