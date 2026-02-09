using System;
using System.Globalization;
using System.Windows.Data;

namespace VideoEditor.Helpers;

public class DurationFormatterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal seconds)
        {
            return VideoEditor.Models.TimeFormatter.FormatDuration(seconds);
        }

        if (value is double doubleSeconds)
        {
            return VideoEditor.Models.TimeFormatter.FormatDuration((decimal)doubleSeconds);
        }

        if (value is null)
        {
            return "";
        }

        return value.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class DurationFormatterShortConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal seconds)
        {
            return VideoEditor.Models.TimeFormatter.FormatDurationShort(seconds);
        }

        if (value is double doubleSeconds)
        {
            return VideoEditor.Models.TimeFormatter.FormatDurationShort((decimal)doubleSeconds);
        }

        if (value is null)
        {
            return "";
        }

        return value.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
