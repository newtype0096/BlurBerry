using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using BlurBerry.Models;

namespace BlurBerry.Converters
{
    public class MediaTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is MediaType mediaType)
            {
                return mediaType == MediaType.Video ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}