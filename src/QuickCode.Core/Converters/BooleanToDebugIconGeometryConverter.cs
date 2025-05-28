using System;
using System.Globalization;
using System.Windows.Data;

namespace QuickCode.Core.Converters
{
    public class BooleanToDebugIconGeometryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            const string debugStartGeometry = "M8,5.14V19.14L19,12.14L8,5.14Z";
            const string nextStepGeometry = "M31.25 175H50V25H31.25V175zM92.95 170.125L78.125 162.5V37.5L92.95 29.875L180.45 92.375V107.625L92.95 170.125zM158.875 100L96.875 55.7125V144.2875L158.875 100z";

            bool successParse = bool.TryParse(value.ToString(), out bool inputValue);

            if(!successParse)
                return debugStartGeometry;

            if(!inputValue)
                return debugStartGeometry;

            return nextStepGeometry;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
