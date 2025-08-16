using BlurBerry.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace BlurBerry.Services
{
    public class MediaLibraryService
    {
        private const string LibraryFileName = "media_library.json";
        private const string ThumbnailCacheFolder = "ThumbnailCache";

        private StorageFolder? _localFolder;
        private StorageFolder? _thumbnailCacheFolder;

        public static MediaLibraryService Instance { get; } = new MediaLibraryService();

        private MediaLibraryService() { }

        public async Task InitializeAsync()
        {
            _localFolder = ApplicationData.Current.LocalFolder;
            
            try
            {
                _thumbnailCacheFolder = await _localFolder.CreateFolderAsync(ThumbnailCacheFolder, CreationCollisionOption.OpenIfExists);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create thumbnail cache folder: {ex.Message}");
            }
        }

        public async Task<List<MediaInfo>> LoadLibraryAsync()
        {
            try
            {
                if (_localFolder == null)
                {
                    await InitializeAsync();
                }

                var libraryFile = await _localFolder!.TryGetItemAsync(LibraryFileName) as StorageFile;
                if (libraryFile == null)
                {
                    return new List<MediaInfo>();
                }

                var json = await FileIO.ReadTextAsync(libraryFile);
                var items = JsonSerializer.Deserialize<List<MediaInfo>>(json);
                
                return items ?? new List<MediaInfo>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load media library: {ex.Message}");
                return new List<MediaInfo>();
            }
        }

        public async Task SaveLibraryAsync(List<MediaInfo> items)
        {
            try
            {
                if (_localFolder == null)
                {
                    await InitializeAsync();
                }

                var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
                var libraryFile = await _localFolder!.CreateFileAsync(LibraryFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(libraryFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save media library: {ex.Message}");
            }
        }

        public async Task<string?> SaveThumbnailAsync(string filePath, IRandomAccessStream thumbnailStream)
        {
            try
            {
                if (_thumbnailCacheFolder == null)
                {
                    await InitializeAsync();
                }

                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var fileExtension = Path.GetExtension(filePath);
                var lastModified = File.GetLastWriteTime(filePath).ToString("yyyyMMddHHmmss");
                var thumbnailFileName = $"{fileName}_{lastModified}_{fileExtension.Replace(".", "")}.jpg";

                var thumbnailFile = await _thumbnailCacheFolder!.CreateFileAsync(thumbnailFileName, CreationCollisionOption.ReplaceExisting);
                
                using var fileStream = await thumbnailFile.OpenAsync(FileAccessMode.ReadWrite);
                await RandomAccessStream.CopyAndCloseAsync(thumbnailStream.GetInputStreamAt(0), fileStream.GetOutputStreamAt(0));

                return thumbnailFile.Path;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save thumbnail: {ex.Message}");
                return null;
            }
        }

        public async Task<BitmapImage?> LoadThumbnailAsync(string? thumbnailPath)
        {
            try
            {
                if (string.IsNullOrEmpty(thumbnailPath) || !File.Exists(thumbnailPath))
                {
                    return null;
                }

                var thumbnailFile = await StorageFile.GetFileFromPathAsync(thumbnailPath);
                using var stream = await thumbnailFile.OpenAsync(FileAccessMode.Read);
                
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream);
                return bitmapImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load thumbnail: {ex.Message}");
                return null;
            }
        }

        public async Task CleanupOrphanedThumbnailsAsync(List<MediaInfo> currentItems)
        {
            try
            {
                if (_thumbnailCacheFolder == null)
                {
                    return;
                }

                var thumbnailFiles = await _thumbnailCacheFolder.GetFilesAsync();
                var validThumbnailPaths = new HashSet<string>();

                foreach (var item in currentItems)
                {
                    if (!string.IsNullOrEmpty(item.ThumbnailPath) && File.Exists(item.ThumbnailPath))
                    {
                        validThumbnailPaths.Add(Path.GetFileName(item.ThumbnailPath));
                    }
                }

                foreach (var file in thumbnailFiles)
                {
                    if (!validThumbnailPaths.Contains(file.Name))
                    {
                        try
                        {
                            await file.DeleteAsync();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to delete orphaned thumbnail {file.Name}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to cleanup orphaned thumbnails: {ex.Message}");
            }
        }
    }

}