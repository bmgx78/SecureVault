using System.Globalization;

namespace PasswordManager.Converters
{
    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is bool b && !b;
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is bool b && !b;
    }

    public class StringNotEmptyConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            !string.IsNullOrEmpty(value as string);
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class IntGreaterThanZeroConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is int i && i > 0;
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is not string param) return value?.ToString() ?? string.Empty;
            var parts = param.Split('|');
            return value is true ? (parts.Length > 0 ? parts[0] : "") : (parts.Length > 1 ? parts[1] : "");
        }
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class PercentToDoubleConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is int i ? i / 100.0 : 0.0;
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is double d ? (int)(d * 100) : 0;
    }

    public class StrengthToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            int score = value is int i ? i : 0;
            return score switch
            {
                >= 80 => Color.FromArgb("#A6E3A1"),
                >= 60 => Color.FromArgb("#89B4FA"),
                >= 40 => Color.FromArgb("#F9E2AF"),
                >= 20 => Color.FromArgb("#FAB387"),
                _ => Color.FromArgb("#F38BA8")
            };
        }
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class TitleToInitialConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            string.IsNullOrEmpty(value as string) ? "?" : ((string)value)[0].ToString().ToUpperInvariant();
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
