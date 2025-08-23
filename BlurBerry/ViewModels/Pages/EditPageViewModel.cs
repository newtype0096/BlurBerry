using BlurBerry.Models;
using BlurBerry.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace BlurBerry.ViewModels.Pages
{
    public class EditPageViewModel : ObservableObject
    {
        public static EditPageViewModel Instance { get; } = new EditPageViewModel();

        private MediaInfo? _selectedMediaInfo;
        public MediaInfo? SelectedMediaInfo
        {
            get => _selectedMediaInfo;
            set
            {
                if (SetProperty(ref _selectedMediaInfo, value))
                {
                    OnPropertyChanged(nameof(FileName));
                    OnPropertyChanged(nameof(FileSize));
                    OnPropertyChanged(nameof(FormattedFileSize));
                    OnPropertyChanged(nameof(Duration));
                    OnPropertyChanged(nameof(FormattedDuration));
                    OnPropertyChanged(nameof(FileExtension));
                    OnPropertyChanged(nameof(IsVideo));
                }
            }
        }

        public string? FileName => SelectedMediaInfo?.FileName;

        public long FileSize => SelectedMediaInfo?.FileSize ?? 0;

        public string FormattedFileSize
        {
            get
            {
                if (SelectedMediaInfo?.FileSize == null)
                {
                    return "알 수 없음";
                }

                var size = SelectedMediaInfo.FileSize;
                string[] units = { "B", "KB", "MB", "GB" };
                int unitIndex = 0;
                double formattedSize = size;

                while (formattedSize >= 1024 && unitIndex < units.Length - 1)
                {
                    formattedSize /= 1024;
                    unitIndex++;
                }

                return $"{formattedSize:F1} {units[unitIndex]}";
            }
        }

        public TimeSpan Duration => SelectedMediaInfo?.Duration ?? TimeSpan.Zero;

        public string FormattedDuration
        {
            get
            {
                if (SelectedMediaInfo?.Duration == null || SelectedMediaInfo.Duration == TimeSpan.Zero)
                    return "-";

                var duration = SelectedMediaInfo.Duration;
                if (duration.TotalHours >= 1)
                {
                    return $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
                }
                else
                {
                    return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
                }
            }
        }

        public string FileExtension
        {
            get
            {
                if (string.IsNullOrEmpty(SelectedMediaInfo?.FilePath))
                {
                    return "알 수 없음";
                }

                var extension = Path.GetExtension(SelectedMediaInfo.FilePath);
                return string.IsNullOrEmpty(extension) ? "알 수 없음" : extension.ToUpperInvariant().Replace(".", string.Empty);
            }
        }

        public bool IsVideo => SelectedMediaInfo?.MediaType == MediaType.Video;

        private BitmapImage? _currentFrame;
        public BitmapImage? CurrentFrame
        {
            get => _currentFrame;
            set => SetProperty(ref _currentFrame, value);
        }

        private bool _isFrameLoading;
        public bool IsFrameLoading
        {
            get => _isFrameLoading;
            set => SetProperty(ref _isFrameLoading, value);
        }

        private EditPageViewModel() { }

        public async void SetSelectedMedia(MediaInfo mediaInfo)
        {
            SelectedMediaInfo = mediaInfo;
            
            // 미디어 파일이 있고 비디오인 경우 첫 번째 프레임 로드
            if (mediaInfo?.FilePath != null)
            {
                await LoadFirstFrameAsync();
            }
        }

        /// <summary>
        /// 선택된 미디어의 첫 번째 프레임을 로드합니다.
        /// </summary>
        public async Task LoadFirstFrameAsync()
        {
            if (string.IsNullOrEmpty(SelectedMediaInfo?.FilePath))
            {
                CurrentFrame = null;
                return;
            }

            IsFrameLoading = true;

            try
            {
                if (IsVideo)
                {
                    // 비디오 파일의 첫 번째 프레임 추출
                    CurrentFrame = await OpenCVFrameService.Instance.ExtractFirstFrameAsync(SelectedMediaInfo.FilePath);
                }
                else
                {
                    // 이미지 파일을 직접 로드
                    try
                    {
                        var file = await StorageFile.GetFileFromPathAsync(SelectedMediaInfo.FilePath);
                        using var stream = await file.OpenAsync(FileAccessMode.Read);
                        
                        var bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(stream);
                        CurrentFrame = bitmapImage;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"이미지 로드 실패: {ex.Message}");
                        CurrentFrame = null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"프레임 로드 실패: {ex.Message}");
                CurrentFrame = null;
            }
            finally
            {
                IsFrameLoading = false;
            }
        }

        /// <summary>
        /// 지정된 시간 위치의 프레임을 로드합니다.
        /// </summary>
        /// <param name="timePositionMs">시간 위치(밀리초)</param>
        public async Task LoadFrameAtTimeAsync(double timePositionMs)
        {
            if (!IsVideo || string.IsNullOrEmpty(SelectedMediaInfo?.FilePath))
            {
                return;
            }

            IsFrameLoading = true;

            try
            {
                CurrentFrame = await OpenCVFrameService.Instance.ExtractFrameAtTimeAsync(SelectedMediaInfo.FilePath, timePositionMs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"시간 기반 프레임 로드 실패: {ex.Message}");
            }
            finally
            {
                IsFrameLoading = false;
            }
        }
    }
}