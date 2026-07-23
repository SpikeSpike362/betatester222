using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace EvolutionTweaker.Helpers;

/// <summary>Плавное появление элементов списка «лесенкой» (fade + сдвиг снизу).</summary>
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
            "Step", typeof(double), typeof(StaggeredReveal),
            new PropertyMetadata(45.0)); // мс между элементами

    public static double GetStep(DependencyObject o) => (double)o.GetValue(StepProperty);
    public static void SetStep(DependencyObject o, double v) => o.SetValue(StepProperty, v);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement el) return;
        if ((bool)e.NewValue)
        {
            el.Opacity = 0;                 // гасим сразу, чтобы не мелькало до Loaded
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

        int index = Math.Min(GetIndex(el), 14);          // cap, чтобы длинный список не «полз» вечно
        double step = GetStep(el);
        var delay = TimeSpan.FromMilliseconds(index * step);

        var tr = new TranslateTransform(0, 12);
        el.RenderTransform = tr;
        el.Opacity = 0;

        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(320))
        {
            BeginTime = delay,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var slide = new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(360))
        {
            BeginTime = delay,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        el.BeginAnimation(UIElement.OpacityProperty, fade);
        tr.BeginAnimation(TranslateTransform.YProperty, slide);
    }

    private static int GetIndex(DependencyObject el)
    {
        DependencyObject child = el;
        DependencyObject? parent = VisualTreeHelper.GetParent(el);
        while (parent != null && parent is not Panel)
        {
            child = parent;
            parent = VisualTreeHelper.GetParent(parent);
        }
        if (parent is Panel panel && child is UIElement u)
        {
            int i = panel.Children.IndexOf(u);
            if (i >= 0) return i;
        }
        return 0;
    }
}