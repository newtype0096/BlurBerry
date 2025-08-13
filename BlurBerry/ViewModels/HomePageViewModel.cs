using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace BlurBerry.ViewModels
{
    public class HomePageViewModel
    {
        public static HomePageViewModel Instance { get; } = new HomePageViewModel();

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
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");
            openPicker.FileTypeFilter.Add(".mp4");
            openPicker.FileTypeFilter.Add(".avi");

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {

            }
        }

        private async void OpenFolderAsync()
        {
            var  folderPicker = new FolderPicker();
            var hWnd = WindowNative.GetWindowHandle(App.MainWindow);

            InitializeWithWindow.Initialize(folderPicker, hWnd);

            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {

            }
        }
    }
}
