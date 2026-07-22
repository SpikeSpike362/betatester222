using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EvolutionTweaker.Helpers;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NonEmptyStringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string s && !string.IsNullOrWhiteSpace(s) ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Принимает два значения (элемент списка и выбранную категорию) и возвращает
/// Visible, если они совпадают, иначе Collapsed. Нужен для подсветки активного пункта меню.
/// </summary>
public class SelectedCategoryVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values != null && values.Length >= 2 && values[0] != null && Equals(values[0], values[1]))
            return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
public class SelectedCategoryBrushConverter : IMultiValueConverter
{
    private static readonly System.Windows.Media.Brush Active =
        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
    static SelectedCategoryBrushConverter() => Active.Freeze();
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        bool selected = values != null && values.Length >= 2 && values[0] != null && Equals(values[0], values[1]);
        if (selected) return Active;
        return Application.Current.TryFindResource("TextSecondaryBrush") ?? System.Windows.Media.Brushes.White;
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}