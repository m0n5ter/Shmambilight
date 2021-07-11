using System.Windows;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace Shmambilight.Screen
{
    public class ScreenGrabberWindowsCapture: ScreenGrabberBase
    {
        private Direct3D11CaptureFramePool _framePool;
        private GraphicsCaptureSession _session;
        private SharpDX.Direct3D11.Device _d3dDevice;
        private SwapChain1 _swapChain;
        private Texture2D _screenTexture;

        public ScreenGrabberWindowsCapture(MonitorInfo screen) : base(screen)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var device = WindowsCaptureHelper.CreateDirect3DDeviceFromSharpDXDevice(new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport));
                _d3dDevice = WindowsCaptureHelper.CreateSharpDXDevice(device);
                var item = WindowsCaptureHelper.CreateItemForMonitor(Screen.HMon);

                var factory = new Factory2();

                var description = new SwapChainDescription1
                {
                    Width = item.Size.Width,
                    Height = item.Size.Height,
                    Format = Format.B8G8R8A8_UNorm,
                    Stereo = false,
                    SampleDescription = new SampleDescription
                    {
                        Count = 1,
                        Quality = 0
                    },
                    Usage = Usage.RenderTargetOutput,
                    BufferCount = 2,
                    Scaling = Scaling.Stretch,
                    SwapEffect = SwapEffect.FlipSequential,
                    AlphaMode = AlphaMode.Premultiplied,
                    Flags = SwapChainFlags.None
                };

                _swapChain = new SwapChain1(factory, _d3dDevice, ref description);
                _framePool = Direct3D11CaptureFramePool.Create(device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, item.Size);
                _session = _framePool.CreateCaptureSession(item);
                _session.IsCursorCaptureEnabled = false;

                _swapChain.ResizeBuffers(2, item.Size.Width, item.Size.Height, Format.B8G8R8A8_UNorm, SwapChainFlags.None);

                _screenTexture = new Texture2D(_d3dDevice, new Texture2DDescription
                {
                    CpuAccessFlags = CpuAccessFlags.Read,
                    BindFlags = BindFlags.None,
                    Format = Format.B8G8R8A8_UNorm,
                    Width = item.Size.Width,
                    Height = item.Size.Height,
                    OptionFlags = ResourceOptionFlags.None,
                    MipLevels = 1,
                    ArraySize = 1,
                    SampleDescription = {Count = 1, Quality = 0},
                    Usage = ResourceUsage.Staging
                });

                _framePool.FrameArrived += OnFrameArrived;

                _session.StartCapture();

            });
        }

        private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            using (var frame = sender.TryGetNextFrame())
            using (var bitmap = WindowsCaptureHelper.CreateSharpDXTexture2D(frame.Surface))
            {
                _d3dDevice.ImmediateContext.CopyResource(bitmap, _screenTexture);
                var mapSource = _d3dDevice.ImmediateContext.MapSubresource(_screenTexture, 0, MapMode.Read, MapFlags.None);
                CopyFromMapSource(mapSource);
                _d3dDevice.ImmediateContext.UnmapSubresource(_screenTexture, 0);
            }
        }

        public override void Dispose()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                base.Dispose();
                Cleanup();
            });
        }

        private void Cleanup()
        {
            _session?.Dispose();
            _framePool?.Dispose();
            _swapChain?.Dispose();
            _d3dDevice?.Dispose();
        }

        public override void GrabPrivate()
        {
        }
    }
}