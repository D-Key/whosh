using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WindowChrome.Demo.Styles.VS2012.Converters
{
    /// <summary>
    /// Convert a value to a Thickness(value, 0, 0, 0)
    /// </summary>
    public class ThicknessLeft : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double t = System.Convert.ToDouble(value);
            if (parameter != null) t += System.Convert.ToDouble(parameter);
            return new Thickness(t, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double t = ((Thickness)value).Left;
            if (parameter != null) t -= System.Convert.ToDouble(parameter);
            return t;
        }
    }
}
