using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using BlurBerry.Views.Pages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Vpn;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BlurBerry
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SetWindowProperties();

            navigationView.SelectedItem = home;
        }

        private void SetWindowProperties()
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(titleBar);
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
        }

        public void Navigate(Type pageType, object? targetPageArguments = null, NavigationTransitionInfo? navigationTransitionInfo = null)
        {
            rootFrame.Navigate(pageType, targetPageArguments, navigationTransitionInfo);
        }

        private void titleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            navigationView.IsPaneOpen = !navigationView.IsPaneOpen;
        }

        private void navigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                if (rootFrame.CurrentSourcePageType != typeof(SettingsPage))
                {
                    Navigate(typeof(SettingsPage));
                }
            }
            else
            {
                var selectedItem = args.SelectedItemContainer;
                if (selectedItem == home)
                {
                    if (rootFrame.CurrentSourcePageType != typeof(HomePage))
                    {
                        Navigate(typeof(HomePage));
                    }
                }
                else if (selectedItem == image)
                {
                    if (rootFrame.CurrentSourcePageType != typeof(ImagePage))
                    {
                        Navigate(typeof(ImagePage));
                    }
                }
                else if (selectedItem == video)
                {
                    if (rootFrame.CurrentSourcePageType != typeof(VideoPage))
                    {
                        Navigate(typeof(VideoPage));
                    }
                }
            }
        }
    }
}
