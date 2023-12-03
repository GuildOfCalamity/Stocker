using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace UpdateViewer.Support
{
    public class BooleanConverter<T> : IValueConverter
    {
        public BooleanConverter(T trueValue, T falseValue)
        {
            True = trueValue;
            False = falseValue;
        }

        public T True { get; set; }
        public T False { get; set; }

        #region [IValueConverter Members]

        public virtual object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is bool && ((bool)value) ? True : False;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is T && EqualityComparer<T>.Default.Equals((T)value, True);
        }

        #endregion
    }
    public class NullableBooleanConverter<T> : IValueConverter
    {
        public NullableBooleanConverter(T trueValue, T falseValue, T nullValue)
        {
            True = trueValue;
            False = falseValue;
            Null = nullValue;
        }

        public T True { get; set; }
        public T False { get; set; }
        public T Null { get; set; }

        #region [IValueConverter Members]

        public virtual object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool? val = value as bool?;
            return (val.HasValue) ? ((val.Value) ? True : False) : Null;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is T && EqualityComparer<T>.Default.Equals((T)value, True);
        }

        #endregion
    }

    // Implement Needed Converters
    public sealed class BooleanToVisibilityConverter : BooleanConverter<Visibility>
    {
        public BooleanToVisibilityConverter() : base(Visibility.Visible, Visibility.Collapsed) { }
    }
    public sealed class NullBooleanToVisibilityConverter : NullableBooleanConverter<Visibility>
    {
        public NullBooleanToVisibilityConverter() : base(Visibility.Visible, Visibility.Hidden, Visibility.Collapsed) { }
    }

    public sealed class BooleanToStringConverter : BooleanConverter<string>
    {
        public BooleanToStringConverter() : base("True", "False") { }
    }
    public sealed class BooleanToBrushConverter : BooleanConverter<Brush>
    {
        public BooleanToBrushConverter() : base(Brushes.Green, Brushes.Red) { }
    }

    public sealed class BooleanToOppositeConverter : BooleanConverter<bool>
    {
        public BooleanToOppositeConverter() : base(false, true) { }
    }
}
