using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlurBerry.Models;
using BlurBerry.Services;
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
        private readonly HashSet<string> _addedFiles = new HashSet<string>();
        private readonly string[] _fileTypeFilter = [".jpg", ".jpeg", ".png", ".mp4", ".avi", ".mkv"];

        public static HomePageViewModel Instance { get; } = new HomePageViewModel();

        public ObservableCollection<MediaInfo> MediaInfos { get; } = new ObservableCollection<MediaInfo>();

        public RelayCommand OpenFileCommand { get; }
        public RelayCommand OpenFolderCommand { get; }

        public HomePageViewModel()
        {
            OpenFileCommand = new RelayCommand(OpenFileAsync);
            OpenFolderCommand = new RelayCommand(OpenFolderAsync);

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await MediaLibraryService.Instance.InitializeAsync();
            await LoadLibraryAsync();
        }

        private async void OpenFileAsync()
        {
            var openPicker = new FileOpenPicker();
            var hWnd = WindowNative.GetWindowHandle(App.MainWindow);

            InitializeWithWindow.Initialize(openPicker, hWnd);

            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.Desktop;

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

            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
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

        private async Task LoadLibraryAsync()
        {
            var libraryItems = await MediaLibraryService.Instance.LoadLibraryAsync();

            MediaInfos.Clear();
            _addedFiles.Clear();

            foreach (var item in libraryItems)
            {
                if (File.Exists(item.FilePath))
                {
                    var mediaInfo = await LoadMediaInfoWithThumbnail(item);
                    if (mediaInfo != null)
                    {
                        MediaInfos.Add(mediaInfo);
                        _addedFiles.Add(item.FilePath);
                    }
                }
            }

            await MediaLibraryService.Instance.CleanupOrphanedThumbnailsAsync(libraryItems);
        }

        private async Task<MediaInfo?> LoadMediaInfoWithThumbnail(MediaInfo item)
        {
            try
            {
                var thumbnail = await MediaLibraryService.Instance.LoadThumbnailAsync(item.ThumbnailPath);
                if (thumbnail != null)
                {
                    item.Thumbnail = thumbnail;
                }
                else
                {
                    await RegenerateThumbnailAsync(item);
                }

                return item;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load thumbnail for MediaInfo: {ex.Message}");
                return item;
            }
        }

        private async Task<BitmapImage?> GenerateThumbnailAsync(StorageFile file)
        {
            try
            {
                var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.PicturesView, 200);
                if (thumbnail != null)
                {
                    var bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(thumbnail);
                    return bitmapImage;
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to generate thumbnail for {file.Path}: {ex.Message}");
                return null;
            }
        }

        private async Task<string?> SaveThumbnailAsync(StorageFile file)
        {
            try
            {
                var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.PicturesView, 200);
                if (thumbnail != null)
                {
                    return await MediaLibraryService.Instance.SaveThumbnailAsync(file.Path, thumbnail);
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save thumbnail for {file.Path}: {ex.Message}");
                return null;
            }
        }

        private async Task<MediaInfo?> CreateMediaInfoAsync(StorageFile file)
        {
            try
            {
                var newMediaInfo = new MediaInfo
                {
                    FilePath = file.Path,
                };

                var basicProperties = await file.GetBasicPropertiesAsync();
                newMediaInfo.FileSize = (long)basicProperties.Size;

                newMediaInfo.ThumbnailPath = await SaveThumbnailAsync(file);
                newMediaInfo.Thumbnail = await GenerateThumbnailAsync(file);

                MediaType mediaType;
                TimeSpan duration = TimeSpan.Zero;

                if (file.ContentType.StartsWith("image"))
                {
                    mediaType = MediaType.Image;
                }
                else if (file.ContentType.StartsWith("video"))
                {
                    mediaType = MediaType.Video;
                    var videoProperties = await file.Properties.GetVideoPropertiesAsync();
                    duration = videoProperties.Duration;
                }
                else
                {
                    return null;
                }

                newMediaInfo.MediaType = mediaType;
                newMediaInfo.Duration = duration;
                newMediaInfo.LastModified = new FileInfo(file.Path).LastWriteTime;

                return newMediaInfo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to generate thumbnail for {file.Path}: {ex.Message}");
                return null;
            }
        }

        private async Task RegenerateThumbnailAsync(MediaInfo item)
        {
            try
            {
                if (!File.Exists(item.FilePath))
                {
                    return;
                }

                var file = await StorageFile.GetFileFromPathAsync(item.FilePath);
                var thumbnailPath = await SaveThumbnailAsync(file);
                var thumbnail = await GenerateThumbnailAsync(file);
                
                if (thumbnailPath != null)
                {
                    item.ThumbnailPath = thumbnailPath;
                    item.Thumbnail = thumbnail;

                    await MediaLibraryService.Instance.SaveLibraryAsync(MediaInfos.ToList());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to regenerate thumbnail for {item.FilePath}: {ex.Message}");
            }
        }

        private async Task AddMediaFileAsync(StorageFile file)
        {
            if (_addedFiles.Contains(file.Path))
            {
                return;
            }

            var fileInfo = new FileInfo(file.Path);
            var existingItem = MediaInfos.FirstOrDefault(x => x.FilePath == file.Path);
            
            if (existingItem != null && existingItem.LastModified == fileInfo.LastWriteTime)
            {
                _addedFiles.Add(file.Path);
                return;
            }

            var newMediaInfo = await CreateMediaInfoAsync(file);
            if (newMediaInfo == null)
            {
                return;
            }

            MediaInfos.Add(newMediaInfo);
            _addedFiles.Add(file.Path);

            await MediaLibraryService.Instance.SaveLibraryAsync(MediaInfos.ToList());
        }
    }
}
