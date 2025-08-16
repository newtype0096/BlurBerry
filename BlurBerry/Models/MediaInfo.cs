using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlurBerry.Models
{
    public enum MediaType
    {
        Image,
        Video
    }

    public class MediaInfo : ObservableObject
    {
        private MediaType _mediaType;
        public MediaType MediaType
        {
            get => _mediaType;
            set => SetProperty(ref _mediaType, value);
        }

        private string? _filePath;
        public string? FilePath
        {
            get => _filePath;
            set
            {
                FileName = Path.GetFileName(value);
                SetProperty(ref _filePath, value);
            }
        }

        private string? _fileName;
        public string? FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        private long _fileSize;
        public long FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }

        private BitmapImage? _thumbnail;
        public BitmapImage? Thumbnail
        {
            get => _thumbnail;
            set => SetProperty(ref _thumbnail, value);
        }

        private TimeSpan _duration;
        public TimeSpan Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }
    }
}
