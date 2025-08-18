using CommunityToolkit.Mvvm.ComponentModel;

namespace BlurBerry.ViewModels.Windows
{
    public class MainWindowViewModel : ObservableObject
    {
        public static MainWindowViewModel Instance { get; } = new MainWindowViewModel();

        private bool _isBackButtonVisible = false;
       
        public bool IsBackButtonVisible
        {
            get => _isBackButtonVisible;
            set => SetProperty(ref _isBackButtonVisible, value);
        }

        private bool _isPaneVisible = true;
        public bool IsPaneVisible
        {
            get => _isPaneVisible;
            set => SetProperty(ref _isPaneVisible, value);
        }
    }
}
