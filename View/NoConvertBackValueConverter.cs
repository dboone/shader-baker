using System;
using System.Globalization;
using System.Windows.Data;

namespace ShaderBaker.View
{

public abstract class NoConvertBackValueConverter : IValueConverter
{
    public abstract object Convert(
        object value, Type targetType, object parameter, CultureInfo culture);

    public object ConvertBack(
        object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new InvalidOperationException("ConvertBack() not supported!");
    }
}

}
