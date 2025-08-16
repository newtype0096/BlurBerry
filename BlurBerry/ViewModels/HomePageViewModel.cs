using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BlurBerry.Models;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using WinRT.Interop;

namespace BlurBerry.ViewModels
{
    public class HomePageViewModel
    {
        private readonly HashSet<string> _addedFiles = [];
        private readonly string[] _fileTypeFilter = [".jpg", ".jpeg", ".png", ".mp4", ".avi", ".mkv"];

        public static HomePageViewModel Instance { get; } = new HomePageViewModel();

        public ObservableCollection<MediaInfo> MediaInfos { get; } = [];

        public RelayCommand OpenFileCommand { get; }
        public RelayCommand OpenFolderCommand { get; }

        public HomePageViewModel()
        {
            OpenFileCommand = new RelayCommand(OpenFileAsync);
            OpenFolderCommand = new RelayCommand(OpenFolderAsync);
        }

        private async void OpenFileAsync()
        {
            var openPicker = new FileOpenPicker();
            var hWnd = WindowNative.GetWindowHandle(App.MainWindow);

            InitializeWithWindow.Initialize(openPicker, hWnd);

            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            foreach (var ext in _fileTypeFilter)
            {
                openPicker.FileTypeFilter.Add(ext);
            }

            var files = await openPicker.PickMultipleFilesAsync();
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    await AddMediaFileAsync(file);
                }
            }
        }

        private async void OpenFolderAsync()
        {
            var folderPicker = new FolderPicker();
            var hWnd = WindowNative.GetWindowHandle(App.MainWindow);

            InitializeWithWindow.Initialize(folderPicker, hWnd);

            folderPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            folderPicker.FileTypeFilter.Add("*");

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                var queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, _fileTypeFilter);
                var queryResult = folder.CreateFileQueryWithOptions(queryOptions);
                var files = await queryResult.GetFilesAsync();

                foreach (var file in files)
                {
                    await AddMediaFileAsync(file);
                }
            }
        }

        private async Task AddMediaFileAsync(StorageFile file)
        {
            if (_addedFiles.Contains(file.Path))
            {
                return;
            }

            var mediaInfo = new MediaInfo
            {
                FilePath = file.Path,
            };

            var basicProperties = await file.GetBasicPropertiesAsync();
            mediaInfo.FileSize = (long)basicProperties.Size;

            var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.PicturesView, 200);
            if (thumbnail != null)
            {
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(thumbnail);
                mediaInfo.Thumbnail = bitmapImage;
            }

            if (file.ContentType.StartsWith("image"))
            {
                mediaInfo.MediaType = MediaType.Image;
            }
            else if (file.ContentType.StartsWith("video"))
            {
                mediaInfo.MediaType = MediaType.Video;
                var videoProperties = await file.Properties.GetVideoPropertiesAsync();
                mediaInfo.Duration = videoProperties.Duration;
            }
            else
            {
                return;
            }

            App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                MediaInfos.Add(mediaInfo);
                _addedFiles.Add(file.Path);
            });
        }
    }
}
