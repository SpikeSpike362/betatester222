using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace EvolutionTweaker.Helpers;

/// <summary>
/// Плавное появление элемента при загрузке: сдвиг по вертикали + прозрачность,
/// с задержкой по порядковому номеру среди соседей (эффект волны при смене списка).
/// </summary>
public static class StaggeredReveal
{
    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached(
            "Enabled", typeof(bool), typeof(StaggeredReveal),
            new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject o) => (bool)o.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject o, bool v) => o.SetValue(EnabledProperty, v);

    public static readonly DependencyProperty StepProperty =
        DependencyProperty.RegisterAttached(
            "Step", typeof(TimeSpan), typeof(StaggeredReveal),
            new PropertyMetadata(TimeSpan.FromMilliseconds(35)));

    public static TimeSpan GetStep(DependencyObject o) => (TimeSpan)o.GetValue(StepProperty);
    public static void SetStep(DependencyObject o, TimeSpan v) => o.SetValue(StepProperty, v);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement el) return;
        if ((bool)e.NewValue)
        {
            el.Opacity = 0;
            el.Loaded += OnLoaded;
        }
        else
        {
            el.Loaded -= OnLoaded;
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement el) return;
        el.Loaded -= OnLoaded;

        int index = ResolveIndex(el);
        var delay = TimeSpan.FromMilliseconds(Math.Max(0, index) * GetStep(el).TotalMilliseconds);

        if (el.RenderTransform is not TranslateTransform)
            el.RenderTransform = new TranslateTransform(0, 8);

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260)) { BeginTime = delay, EasingFunction = ease };
        var slide = new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(260)) { BeginTime = delay, EasingFunction = ease };
        fade.Completed += (s, args) => el.Opacity = 1;

        el.BeginAnimation(UIElement.OpacityProperty, fade);
        if (el.RenderTransform is TranslateTransform tt)
            tt.BeginAnimation(TranslateTransform.YProperty, slide);
    }

    private static int ResolveIndex(FrameworkElement el)
    {
        try
        {
            DependencyObject? cur = el;
            DependencyObject? prev = null;
            for (int i = 0; i < 4 && cur != null; i++)
            {
                prev = cur;
                cur = VisualTreeHelper.GetParent(cur);
                if (cur is Panel panel && prev != null)
                    return panel.Children.IndexOf((UIElement)prev);
            }
        }
        catch { }
        return -1;
    }
}