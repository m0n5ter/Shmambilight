using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Shmambilight.UI
{
    [ValueConversion(typeof(object), typeof(object))]
    public class IsEqualConverter : MarkupExtension, IValueConverter
    {
        public object TrueValue { get; set; } = true;

        public object FalseValue { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null && parameter == null
                ? TrueValue
                : value?.Equals(parameter) ?? false ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}