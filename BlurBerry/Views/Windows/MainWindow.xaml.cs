using System;
using BlurBerry.Models;
using BlurBerry.ViewModels;
using BlurBerry.ViewModels.Windows;
using BlurBerry.Views.Pages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BlurBerry.Views.Windows
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

            rootGrid.DataContext = MainWindowViewModel.Instance;

            navigationView.SelectedItem = home;
            rootFrame.Navigated += RootFrame_Navigated;
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

        public void NavigateToEditPage(MediaInfo mediaInfo)
        {
            MainWindowViewModel.Instance.IsBackButtonVisible = true;
            MainWindowViewModel.Instance.IsPaneVisible = false;
            rootFrame.Navigate(typeof(EditPage), mediaInfo, new DrillInNavigationTransitionInfo());
        }

        private void RootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.SourcePageType == typeof(EditPage))
            {
                MainWindowViewModel.Instance.IsBackButtonVisible = true;
                MainWindowViewModel.Instance.IsPaneVisible = false;
                navigationView.SelectedItem = null;
            }
            else
            {
                MainWindowViewModel.Instance.IsBackButtonVisible = false;
                MainWindowViewModel.Instance.IsPaneVisible = true;
            }
        }

        private void titleBar_BackRequested(TitleBar sender, object args)
        {
            if (rootFrame.CanGoBack)
            {
                rootFrame.GoBack();
            }
        }

        private void navigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = args.SelectedItemContainer;
            if (selectedItem == home)
            {
                if (rootFrame.CurrentSourcePageType != typeof(HomePage))
                {
                    Navigate(typeof(HomePage));
                }
            }
            else if (selectedItem == settings)
            {
                if (rootFrame.CurrentSourcePageType != typeof(SettingsPage))
                {
                    Navigate(typeof(SettingsPage));
                }
            }
        }
    }
}
