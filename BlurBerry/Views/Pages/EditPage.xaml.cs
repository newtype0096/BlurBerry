using BlurBerry.Models;
using BlurBerry.ViewModels.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.IO;
using Windows.Storage;

namespace BlurBerry.Views.Pages
{
    public sealed partial class EditPage : Page
    {
        public EditPage()
        {
            InitializeComponent();
            DataContext = EditPageViewModel.Instance;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is MediaInfo mediaInfo)
            {
                EditPageViewModel.Instance.SetSelectedMedia(mediaInfo);
            }
        }
    }
}