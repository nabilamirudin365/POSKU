using System;
using System.Globalization;
using System.Windows.Data;

namespace POSKU.Desktop
{
    public class PurchaseLineSubtotalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PurchaseLineVM line) return line.Qty * line.Cost;
            return 0m;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
