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
        public static extern bool OpenVideo(string filepath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GrabNextFrame();

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
            // 1. 엔진 초기화 (4K)
            if (!Initialize(3840, 2160))
            {
                MessageBox.Show("DirectX 초기화 실패!");
                return;
            }

            // 2. 비디오 파일 열기
            string videoPath = @"C:\Users\M\Desktop\hevc_4k25P_main_2.mp4";
            if (!OpenVideo(videoPath))
            {
                MessageBox.Show($"영상 열기 실패: {videoPath}");
                return;
            }

            // 3. 브릿지 연결
            _backBufferHandle = GetSharedHandle();
            _d3dImage = new D3DImage();
            _d3dImage.Lock();
            _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _backBufferHandle);
            _d3dImage.Unlock();

            VideoScreen.Source = _d3dImage;

            // 4. 렌더링 시작
            CompositionTarget.Rendering += OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (_d3dImage != null && _d3dImage.IsFrontBufferAvailable && _backBufferHandle != IntPtr.Zero)
            {
                // C++에게 "다음 프레임 가져와!" 명령
                bool hasFrame = GrabNextFrame();

                if (hasFrame)
                {
                    _d3dImage.Lock();
                    _d3dImage.AddDirtyRect(new Int32Rect(0, 0, _d3dImage.PixelWidth, _d3dImage.PixelHeight));
                    _d3dImage.Unlock();
                }
            }
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
            Cleanup();
        }
    }
}