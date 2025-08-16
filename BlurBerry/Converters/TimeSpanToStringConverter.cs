using Microsoft.UI.Xaml.Data;
using System;

namespace BlurBerry.Converters
{
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is TimeSpan timeSpan)
            {
                if (timeSpan.TotalHours >= 1)
                {
                    return timeSpan.ToString(@"h\:mm\:ss");
                }
                else
                {
                    return timeSpan.ToString(@"m\:ss");
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}