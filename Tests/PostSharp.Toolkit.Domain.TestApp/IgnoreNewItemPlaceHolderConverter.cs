using System;
using System.Windows;
using System.Windows.Data;

namespace PostSharp.Toolkit.Domain.TestApp
{
    public class IgnoreNewItemPlaceHolderConverter : IValueConverter
    {
        private const string newItemPlaceholderName = "{NewItemPlaceholder}";

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null && value.ToString() == newItemPlaceholderName)
                return null;
            return value;
        }
    }

}
