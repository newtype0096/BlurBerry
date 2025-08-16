using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using BlurBerry.Models;

namespace BlurBerry.Views.UserControls
{
    public sealed partial class MediaItemControl : UserControl
    {
        public MediaItemControl()
        {
            InitializeComponent();
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