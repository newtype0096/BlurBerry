using Microsoft.UI.Xaml.Media.Imaging;
using OpenCvSharp;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;

namespace BlurBerry.Services
{
    public class OpenCVFrameService
    {
        public static OpenCVFrameService Instance { get; } = new OpenCVFrameService();

        private OpenCVFrameService() { }

        /// <summary>
        /// 미디어 파일에서 첫 번째 프레임을 추출하여 BitmapImage로 반환합니다.
        /// </summary>
        /// <param name="filePath">미디어 파일 경로</param>
        /// <returns>첫 번째 프레임의 BitmapImage, 실패시 null</returns>
        public async Task<BitmapImage?> ExtractFirstFrameAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return null;
            }

            try
            {
                using var capture = new VideoCapture(filePath);
                
                if (!capture.IsOpened())
                {
                    System.Diagnostics.Debug.WriteLine($"OpenCV: 파일을 열 수 없습니다: {filePath}");
                    return null;
                }

                using var frame = new Mat();
                
                // 첫 번째 프레임 읽기
                if (!capture.Read(frame) || frame.Empty())
                {
                    System.Diagnostics.Debug.WriteLine($"OpenCV: 첫 번째 프레임을 읽을 수 없습니다: {filePath}");
                    return null;
                }

                // Mat을 BitmapImage로 변환
                return await ConvertMatToBitmapImageAsync(frame);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenCV 프레임 추출 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 지정된 시간 위치에서 프레임을 추출하여 BitmapImage로 반환합니다.
        /// </summary>
        /// <param name="filePath">미디어 파일 경로</param>
        /// <param name="timePositionMs">프레임을 추출할 시간 위치(밀리초)</param>
        /// <returns>지정된 위치의 프레임 BitmapImage, 실패시 null</returns>
        public async Task<BitmapImage?> ExtractFrameAtTimeAsync(string filePath, double timePositionMs)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return null;
            }

            try
            {
                using var capture = new VideoCapture(filePath);
                
                if (!capture.IsOpened())
                {
                    System.Diagnostics.Debug.WriteLine($"OpenCV: 파일을 열 수 없습니다: {filePath}");
                    return null;
                }

                // 지정된 시간 위치로 이동
                capture.Set(VideoCaptureProperties.PosFrames, (int)(timePositionMs * capture.Get(VideoCaptureProperties.Fps) / 1000));

                using var frame = new Mat();
                
                if (!capture.Read(frame) || frame.Empty())
                {
                    System.Diagnostics.Debug.WriteLine($"OpenCV: 지정된 위치의 프레임을 읽을 수 없습니다: {filePath}");
                    return null;
                }

                return await ConvertMatToBitmapImageAsync(frame);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenCV 프레임 추출 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Mat 객체를 WinUI3용 BitmapImage로 변환합니다.
        /// </summary>
        /// <param name="mat">변환할 Mat 객체</param>
        /// <returns>변환된 BitmapImage</returns>
        private async Task<BitmapImage?> ConvertMatToBitmapImageAsync(Mat mat)
        {
            try
            {
                // Mat을 바이트 배열로 인코딩
                if (!Cv2.ImEncode(".png", mat, out var imageBytes))
                {
                    System.Diagnostics.Debug.WriteLine("OpenCV: 이미지 인코딩 실패");
                    return null;
                }

                // 메모리 스트림 생성
                using var memoryStream = new MemoryStream(imageBytes);
                
                // InMemoryRandomAccessStream 생성
                var randomAccessStream = new InMemoryRandomAccessStream();
                var outputStream = randomAccessStream.GetOutputStreamAt(0);
                
                // 바이트 데이터를 스트림에 쓰기
                var writer = new Windows.Storage.Streams.DataWriter(outputStream);
                writer.WriteBytes(imageBytes);
                await writer.StoreAsync();
                await outputStream.FlushAsync();
                
                // BitmapImage 생성 및 스트림 설정
                var bitmapImage = new BitmapImage();
                randomAccessStream.Seek(0);
                await bitmapImage.SetSourceAsync(randomAccessStream);
                
                return bitmapImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mat to BitmapImage 변환 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 비디오 파일의 기본 정보를 가져옵니다.
        /// </summary>
        /// <param name="filePath">비디오 파일 경로</param>
        /// <returns>비디오 정보 (프레임 수, FPS, 해상도 등)</returns>
        public VideoInfo? GetVideoInfo(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return null;
            }

            try
            {
                using var capture = new VideoCapture(filePath);
                
                if (!capture.IsOpened())
                {
                    return null;
                }

                var frameCount = (int)capture.Get(VideoCaptureProperties.FrameCount);
                var fps = capture.Get(VideoCaptureProperties.Fps);
                var width = (int)capture.Get(VideoCaptureProperties.FrameWidth);
                var height = (int)capture.Get(VideoCaptureProperties.FrameHeight);
                var duration = frameCount / fps;

                return new VideoInfo
                {
                    FrameCount = frameCount,
                    Fps = fps,
                    Width = width,
                    Height = height,
                    Duration = TimeSpan.FromSeconds(duration)
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"비디오 정보 가져오기 실패: {ex.Message}");
                return null;
            }
        }
    }

    public class VideoInfo
    {
        public int FrameCount { get; set; }
        public double Fps { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public TimeSpan Duration { get; set; }
    }
}