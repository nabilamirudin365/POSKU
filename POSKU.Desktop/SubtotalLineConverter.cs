using System;
using System.Globalization;
using System.Windows.Data;

namespace POSKU.Desktop
{
    public class SubtotalLineConverter : IValueConverter
    {
        // value = CartLineVM
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CartLineVM line)
            {
                var v = (line.Qty * line.Price) - line.Discount;
                return v < 0 ? 0m : v;
            }
            return 0m; // hindari warning nullable
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
