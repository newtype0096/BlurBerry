using BlurBerry.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;

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

        private EditPageViewModel() { }

        public void SetSelectedMedia(MediaInfo mediaInfo)
        {
            SelectedMediaInfo = mediaInfo;
        }
    }
}