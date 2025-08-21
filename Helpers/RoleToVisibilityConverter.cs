using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace DrJaw.Helpers
{
    public sealed class RoleToVisibilityConverter : IValueConverter
    {
        // value: строка роли пользователя, например "USER"/"CLOUD"/"ADMIN"
        // parameter: список разрешённых ролей через запятую, например "USER,ADMIN"
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // По умолчанию скрываем, если что-то не так
            if (value is null) return Visibility.Collapsed;

            var role = value.ToString()?.Trim();
            if (string.IsNullOrEmpty(role)) return Visibility.Collapsed;

            var param = parameter as string;
            if (string.IsNullOrWhiteSpace(param)) return Visibility.Collapsed;

            var allowed = param
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s.ToUpperInvariant())
                .ToHashSet();

            return allowed.Contains(role.ToUpperInvariant())
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
