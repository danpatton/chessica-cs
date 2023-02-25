using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Chessica.Gui.View;

public class BindingResourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var resourceKey = value as string;
        if (!string.IsNullOrEmpty(resourceKey))
        {
            var resource = Application.Current.FindResource(resourceKey);             
            if (resource != null)
            {
                return resource;
            }
        }
        return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}