using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace EvolutionTweaker.Helpers;

/// <summary>Лёгкий fade+slide при смене страницы во Frame.</summary>
public static class PageTransition
{
    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached(
            "Enabled", typeof(bool), typeof(PageTransition),
            new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject o) => (bool)o.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject o, bool v) => o.SetValue(EnabledProperty, v);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Frame frame) return;
        if ((bool)e.NewValue)
        {
            frame.Loaded += OnFrameLoaded;
            DependencyPropertyDescriptor
                .FromProperty(ContentControl.ContentProperty, typeof(Frame))
                .AddValueChanged(frame, OnContentChanged);
        }
        else
        {
            frame.Loaded -= OnFrameLoaded;
            DependencyPropertyDescriptor
                .FromProperty(ContentControl.ContentProperty, typeof(Frame))
                .RemoveValueChanged(frame, OnContentChanged);
        }
    }

    private static void OnFrameLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Frame f) Animate(f.Content as UIElement);
    }

    private static void OnContentChanged(object? sender, EventArgs e)
    {
        if (sender is Frame f) Animate(f.Content as UIElement);
    }

    private static void Animate(UIElement? page)
    {
        if (page is not FrameworkElement el) return;
        var tr = new TranslateTransform(0, 8);
        el.RenderTransform = tr;
        el.Opacity = 0;
        el.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
        tr.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(260))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
    }
}