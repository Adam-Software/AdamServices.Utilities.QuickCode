using System;
using System.Globalization;
using System.Windows.Data;

namespace QuickCode.Core.Converters
{
    public class BooleanToDebugColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            const string debugEnableColor = "Red";
            const string debugDisableColor = "Black";

            bool successParse = bool.TryParse(value.ToString(), out bool inputValue);

            if (!successParse)
                return debugDisableColor;

            if (!inputValue)
                return debugDisableColor;

            return debugEnableColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
