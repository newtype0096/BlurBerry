using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BlurBerry
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string DllName = "BlurBerry.Engine.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Initialize(int width, int height);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Cleanup();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetSharedHandle();

        private D3DImage? _d3dImage;
        private IntPtr _backBufferHandle;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            bool success = Initialize(3840, 2160);
            if (!success)
            {
                MessageBox.Show("DirectX 초기화 실패! (GPU 문제거나 DLL 경로 문제)");
                return;
            }

            _backBufferHandle = GetSharedHandle();
            if (_backBufferHandle == IntPtr.Zero)
            {
                MessageBox.Show("핸들 가져오기 실패!");
                return;
            }

            _d3dImage = new D3DImage();

            _d3dImage.Lock();
            _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _backBufferHandle);
            _d3dImage.Unlock();

            VideoScreen.Source = _d3dImage;

            CompositionTarget.Rendering += OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (_d3dImage != null && _d3dImage.IsFrontBufferAvailable && _backBufferHandle != IntPtr.Zero)
            {
                _d3dImage.Lock();
                _d3dImage.AddDirtyRect(new Int32Rect(0, 0, _d3dImage.PixelWidth, _d3dImage.PixelHeight));
                _d3dImage.Unlock();
            }
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
            Cleanup();
        }
    }
}