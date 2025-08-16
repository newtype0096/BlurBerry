using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using BlurBerry.Models;
using System;
using System.ComponentModel;

namespace BlurBerry.Views.UserControls
{
    public sealed partial class MediaItemControl : UserControl
    {
        private MediaInfo? _currentMediaInfo;

        public MediaItemControl()
        {
            InitializeComponent();
            DataContextChanged += MediaItemControl_DataContextChanged;
        }

        private void MediaItemControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (_currentMediaInfo != null)
            {
                _currentMediaInfo.PropertyChanged -= MediaInfo_PropertyChanged;
            }

            _currentMediaInfo = args.NewValue as MediaInfo;

            if (_currentMediaInfo != null)
            {
                _currentMediaInfo.PropertyChanged += MediaInfo_PropertyChanged;
                UpdateVisualState();
            }
        }

        private void MediaInfo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MediaInfo.IsSelected))
            {
                UpdateVisualState();
            }
        }

        private void UpdateVisualState()
        {
            if (_currentMediaInfo?.IsSelected == true)
            {
                VisualStateManager.GoToState(this, "Selected", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "Normal", true);
            }
        }

        private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (DataContext is MediaInfo mediaInfo && mediaInfo.IsSelected)
            {
                VisualStateManager.GoToState(this, "Selected", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "PointerOver", true);
            }
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        }

        private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (DataContext is MediaInfo mediaInfo && mediaInfo.IsSelected)
            {
                VisualStateManager.GoToState(this, "Selected", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "Normal", true);
            }
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }

        private void SelectCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MediaInfo mediaInfo)
            {
                if (mediaInfo.IsSelected)
                {
                    VisualStateManager.GoToState(this, "Selected", true);
                }
                else
                {
                    VisualStateManager.GoToState(this, "Normal", true);
                }
            }
        }
    }
}