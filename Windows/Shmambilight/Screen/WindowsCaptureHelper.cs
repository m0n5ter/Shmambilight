using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using SharpDX.Direct3D11;

namespace Shmambilight.Screen
{
    public static class WindowsCaptureHelper
    {
        private static readonly Guid _graphicsCaptureItemGuid = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760");
        private static readonly Guid _id3D11Device = new Guid("db6f6ddb-ac77-4e88-8253-819df9bbf140");
        private static readonly Guid _id3D11Texture2D = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

        [ComImport]
        [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        private interface IGraphicsCaptureItemInterop
        {
            IntPtr CreateForWindow([In] IntPtr window, [In] ref Guid iid);
            IntPtr CreateForMonitor([In] IntPtr monitor, [In] ref Guid iid);
        }

        [ComImport]
        [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        interface IDirect3DDxgiInterfaceAccess
        {
            IntPtr GetInterface([In] ref Guid iid);
        };

        [DllImport("d3d11.dll",
            EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice",
            SetLastError = true,
            CharSet = CharSet.Unicode,
            ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall)]
        private static extern uint CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

        public static Device CreateSharpDXDevice(IDirect3DDevice device)
        {
            var access = (IDirect3DDxgiInterfaceAccess)device;
            var d3dPointer = access.GetInterface(_id3D11Device);
            var d3dDevice = new Device(d3dPointer);
            return d3dDevice;
        }

        public static GraphicsCaptureItem CreateItemForMonitor(IntPtr hMon)
        {
            var factory = WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem));
            var interop = (IGraphicsCaptureItemInterop)factory;
            var itemPointer = interop.CreateForMonitor(hMon, _graphicsCaptureItemGuid);
            var item = Marshal.GetObjectForIUnknown(itemPointer) as GraphicsCaptureItem;
            Marshal.Release(itemPointer);

            return item;
        }

        public static IDirect3DDevice CreateDirect3DDeviceFromSharpDXDevice(Device d3dDevice)
        {
            IDirect3DDevice device = null;

            using (var dxgiDevice = d3dDevice.QueryInterface<SharpDX.DXGI.Device3>())
            {
                var hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out IntPtr pUnknown);

                if (hr == 0)
                {
                    device = Marshal.GetObjectForIUnknown(pUnknown) as IDirect3DDevice;
                    Marshal.Release(pUnknown);
                }
            }

            return device;
        }

        public static Texture2D CreateSharpDXTexture2D(IDirect3DSurface surface)
        {
            var access = (IDirect3DDxgiInterfaceAccess)surface;
            var d3dPointer = access.GetInterface(_id3D11Texture2D);
            var d3dSurface = new Texture2D(d3dPointer);
            return d3dSurface;
        }
    }
}