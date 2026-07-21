using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace EvolutionTweaker.Helpers;

/// <summary>
/// Плавное раскрытие/сворачивание по высоте с обрезкой краем.
/// При быстром клике анимация НЕ прерывается — новое действие откладывается
/// и выполняется сразу после завершения текущей анимации (без дёрганья).
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
            new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(220))));

    public static Duration GetDuration(DependencyObject o) => (Duration)o.GetValue(DurationProperty);
    public static void SetDuration(DependencyObject o, Duration v) => o.SetValue(DurationProperty, v);

    // Идёт ли сейчас анимация
    private static readonly DependencyProperty IsAnimatingProperty =
        DependencyProperty.RegisterAttached("IsAnimating", typeof(bool), typeof(SmoothExpander),
            new PropertyMetadata(false));
    // Отложенное действие (null = нет, true = раскрыть, false = свернуть)
    private static readonly DependencyProperty PendingExpandProperty =
        DependencyProperty.RegisterAttached("PendingExpand", typeof(bool?), typeof(SmoothExpander),
            new PropertyMetadata(null));

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement el) return;
        el.ClipToBounds = true;
        bool expand = (bool)e.NewValue;

        // Если анимация ещё идёт — не прерываем, а откладываем действие до её завершения
        if ((bool)el.GetValue(IsAnimatingProperty))
        {
            el.SetValue(PendingExpandProperty, expand);
            return;
        }

        el.SetValue(IsAnimatingProperty, true);
        if (expand) Expand(el); else Collapse(el);
    }

    private static void Expand(FrameworkElement el)
    {
        double from = el.ActualHeight;
        double target = MeasureContentHeight(el);
        if (double.IsNaN(target) || target <= 0) target = from;

        var anim = new DoubleAnimation(from, target, GetDuration(el))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        anim.Completed += (s, e) =>
        {
            el.BeginAnimation(FrameworkElement.HeightProperty, null);
            el.Height = double.NaN;   // Auto — текст переносится при ресайзе окна
            FinishAnimation(el);
        };
        el.BeginAnimation(FrameworkElement.HeightProperty, anim);
    }

    private static void Collapse(FrameworkElement el)
    {
        double from = el.ActualHeight;
        var anim = new DoubleAnimation(from, 0, GetDuration(el))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        anim.Completed += (s, e) =>
        {
            el.BeginAnimation(FrameworkElement.HeightProperty, null);
            el.Height = 0;
            FinishAnimation(el);
        };
        el.BeginAnimation(FrameworkElement.HeightProperty, anim);
    }

    /// <summary>Завершает анимацию и выполняет отложенное действие (если было).</summary>
    private static void FinishAnimation(FrameworkElement el)
    {
        el.SetValue(IsAnimatingProperty, false);
        bool? pending = (bool?)el.GetValue(PendingExpandProperty);
        if (pending.HasValue)
        {
            el.SetValue(PendingExpandProperty, null);
            el.SetValue(IsAnimatingProperty, true);
            if (pending.Value) Expand(el); else Collapse(el);
        }
    }

    /// <summary>Измеряет желаемую высоту внутреннего контента, не меняя Height элемента.</summary>
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