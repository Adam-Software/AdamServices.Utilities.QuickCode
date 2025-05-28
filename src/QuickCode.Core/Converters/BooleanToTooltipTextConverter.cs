using System;
using System.Globalization;
using System.Windows.Data;

namespace QuickCode.Core.Converters
{
    public class BooleanToTooltipTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            const string startDebugTooltipText = "Start execute code with pdb debug";
            const string stepTooltipText = "Execute the current line, stop at the first possible occasion";

            bool successParse = bool.TryParse(value.ToString(), out bool inputValue);

            if (!successParse)
                return startDebugTooltipText;

            if (!inputValue)
                return startDebugTooltipText;

            return stepTooltipText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
