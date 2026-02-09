using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VideoEditor.Helpers;

public class BoolToPremiumTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool requiresPremium)
        {
            return requiresPremium ? "需要" : "免费";
        }
        return "免费";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
