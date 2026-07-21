using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace EvolutionTweaker.Helpers;

/// <summary>
/// Плавное раскрытие/сворачивание по высоте с обрезкой краем.
/// Usage: <Border helpers:SmoothExpander.IsExpanded="{Binding IsExpanded}" Height="0" ClipToBounds="True" .../>
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
        double from = el.ActualHeight;                 // текущая визуальная высота — стартуем плавно даже посреди анимации
        double target = MeasureContentHeight(el);      // измеряем БЕЗ изменения Height → нет мелькания
        if (double.IsNaN(target) || target <= 0) target = from;

        var anim = new DoubleAnimation(from, target, GetDuration(el)) { EasingFunction = ease };
        anim.Completed += (s, e) =>
        {
            el.BeginAnimation(FrameworkElement.HeightProperty, null);
            el.Height = double.NaN;                    // Auto — чтобы текст переносился при ресайзе окна
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

    /// <summary>
    /// Измеряет желаемую высоту ВНУТРЕННЕГО контента, не меняя Height самого элемента.
    /// Это убирает мелькание при быстром клике (раньше ставили Height=NaN для измерения).
    /// </summary>
    private static double MeasureContentHeight(FrameworkElement el)
    {
        if (el is Border border && border.Child is FrameworkElement child)
        {
            double padV = border.Padding.Top + border.Padding.Bottom
                        + border.BorderThickness.Top + border.BorderThickness.Bottom;
            double availW = border.ActualWidth > 0
                ? border.ActualWidth - border.Padding.Left - border.Padding.Right
                  - border.BorderThickness.Left - border.BorderThickness.Right
                : double.PositiveInfinity;
            child.Measure(new Size(availW, double.PositiveInfinity));
            return child.DesiredSize.Height + padV;
        }
        // фолбэк (элемент не Border)
        double width = el.ActualWidth > 0 ? el.ActualWidth : double.PositiveInfinity;
        el.Measure(new Size(width, double.PositiveInfinity));
        return el.DesiredSize.Height;
    }
}