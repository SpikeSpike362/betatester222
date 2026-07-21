using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace EvolutionTweaker.Helpers;

/// <summary>
/// Плавное раскрытие/сворачивание по высоте с обрезкой краем (аналог CSS grid 0fr→1fr + overflow:hidden).
/// Контент внутри НЕ должен быть Collapsed — он всегда в разметке со своей естественной высотой.
/// Usage:  <Border helpers:SmoothExpander.IsExpanded="{Binding IsExpanded}" Height="0" ClipToBounds="True" .../>
/// </summary>
public static class SmoothExpander
{
    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.RegisterAttached(
            "IsExpanded", typeof(bool), typeof(SmoothExpander),
            new PropertyMetadata(false, OnIsExpandedChanged));

    public static bool GetIsExpanded(DependencyObject o) => (bool)o.GetValue(IsExpandedProperty);
    public static void SetIsExpanded(DependencyObject o, bool v) => o.SetValue(IsExpandedProperty, v);

    public static readonly DependencyProperty DurationProperty =
        DependencyProperty.RegisterAttached(
            "Duration", typeof(Duration), typeof(SmoothExpander),
            new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(280))));

    public static Duration GetDuration(DependencyObject o) => (Duration)o.GetValue(DurationProperty);
    public static void SetDuration(DependencyObject o, Duration v) => o.SetValue(DurationProperty, v);

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement el) return;
        el.ClipToBounds = true;

        if ((bool)e.NewValue) Expand(el);
        else Collapse(el);
    }

    private static void Expand(FrameworkElement el)
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        double from = el.ActualHeight;                       // текущая визуальная высота (без прыжков при быстром клике)
        double width = el.ActualWidth > 0 ? el.ActualWidth : double.PositiveInfinity;

        // измеряем истинную желаемую высоту (Height на время = NaN, иначе DesiredSize зажмётся текущим Height)
        el.Height = double.NaN;
        el.Measure(new Size(width, double.PositiveInfinity));
        double target = el.DesiredSize.Height;
        if (double.IsNaN(target) || target <= 0) target = from;

        el.Height = from;                                    // возвращаем, чтобы не мелькнул полный контент до старта анимации

        var anim = new DoubleAnimation(from, target, GetDuration(el)) { EasingFunction = ease };
        anim.Completed += (s, e) =>
        {
            el.BeginAnimation(FrameworkElement.HeightProperty, null);
            el.Height = double.NaN;                          // после раскрытия — Auto, чтобы текст корректно переносился при ресайзе окна
        };
        el.BeginAnimation(FrameworkElement.HeightProperty, anim);
    }

    private static void Collapse(FrameworkElement el)
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        double from = el.ActualHeight;

        var anim = new DoubleAnimation(from, 0, GetDuration(el)) { EasingFunction = ease };
        anim.Completed += (s, e) =>
        {
            el.BeginAnimation(FrameworkElement.HeightProperty, null);
            el.Height = 0;
        };
        el.BeginAnimation(FrameworkElement.HeightProperty, anim);
    }
}