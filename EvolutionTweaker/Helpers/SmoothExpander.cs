using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace EvolutionTweaker.Helpers;

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
            new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(200))));

    public static Duration GetDuration(DependencyObject o) => (Duration)o.GetValue(DurationProperty);
    public static void SetDuration(DependencyObject o, Duration v) => o.SetValue(DurationProperty, v);

    // Конечная точка текущей анимации (для резкого довода при прерывании)
    private static readonly DependencyProperty PendingTargetProperty =
        DependencyProperty.RegisterAttached("PendingTarget", typeof(double), typeof(SmoothExpander),
            new PropertyMetadata(double.NaN));

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement el) return;
        el.ClipToBounds = true;
        if ((bool)e.NewValue) Expand(el);
        else Collapse(el);
    }

    private static void Expand(FrameworkElement el)
    {
        // Резко довести текущую анимацию до конца (snap)
        double pending = (double)el.GetValue(PendingTargetProperty);
        el.BeginAnimation(FrameworkElement.HeightProperty, null);
        if (!double.IsNaN(pending)) el.Height = pending;

        // ВАЖНО: стартуем с локального Height (после snap), а не с ActualHeight
        double from = double.IsNaN(el.Height) ? 0 : el.Height;
        double target = MeasureContentHeight(el);
        if (double.IsNaN(target) || target <= 0) target = from;
        el.SetValue(PendingTargetProperty, target);

        var anim = new DoubleAnimation(from, target, GetDuration(el))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        anim.Completed += (s, e) =>
        {
            el.BeginAnimation(FrameworkElement.HeightProperty, null);
            el.Height = double.NaN;   // Auto — текст переносится при ресайзе
            el.SetValue(PendingTargetProperty, double.NaN);
        };
        el.BeginAnimation(FrameworkElement.HeightProperty, anim);
    }

    private static void Collapse(FrameworkElement el)
    {
        // Резко довести текущую анимацию раскрытия до полного раскрытия, потом закрывать
        double pending = (double)el.GetValue(PendingTargetProperty);
        el.BeginAnimation(FrameworkElement.HeightProperty, null);
        if (!double.IsNaN(pending)) el.Height = pending;

        double from = double.IsNaN(el.Height) ? el.ActualHeight : el.Height;
        el.SetValue(PendingTargetProperty, 0.0);

        var anim = new DoubleAnimation(from, 0, GetDuration(el))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        anim.Completed += (s, e) =>
        {
            el.BeginAnimation(FrameworkElement.HeightProperty, null);
            el.Height = 0;
            el.SetValue(PendingTargetProperty, double.NaN);
        };
        el.BeginAnimation(FrameworkElement.HeightProperty, anim);
    }

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
        double width = el.ActualWidth > 0 ? el.ActualWidth : double.PositiveInfinity;
        el.Measure(new Size(width, double.PositiveInfinity));
        return el.DesiredSize.Height;
    }
}