using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace ConsoleDeck.Helpers;

public static class SmoothScroll
{
    // Proxy property: callback scrolls the viewer when animation updates it
    private static readonly DependencyProperty ScrollOffsetProperty =
        DependencyProperty.RegisterAttached("ScrollOffset", typeof(double), typeof(SmoothScroll),
            new PropertyMetadata(0.0, (d, e) =>
            {
                if (d is ScrollViewer sv) sv.ScrollToVerticalOffset((double)e.NewValue);
            }));

    // Tracks the intended final position (accumulates fast consecutive scrolls)
    private static readonly DependencyProperty TargetOffsetProperty =
        DependencyProperty.RegisterAttached("TargetOffset", typeof(double), typeof(SmoothScroll),
            new PropertyMetadata(0.0));

    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached("Enabled", typeof(bool), typeof(SmoothScroll),
            new PropertyMetadata(false, (d, e) =>
            {
                if (d is not ScrollViewer sv) return;
                if ((bool)e.NewValue) sv.PreviewMouseWheel += OnWheel;
                else sv.PreviewMouseWheel -= OnWheel;
            }));

    public static bool GetEnabled(DependencyObject d) => (bool)d.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject d, bool v) => d.SetValue(EnabledProperty, v);

    private static void OnWheel(object sender, MouseWheelEventArgs e)
    {
        var sv = (ScrollViewer)sender;
        var prev = (double)sv.GetValue(TargetOffsetProperty);

        // Respect Windows "lines per notch" setting; normalize for high-resolution mice
        var step = SystemParameters.WheelScrollLines * 50.0 * -(e.Delta / 120.0);
        var next = Math.Clamp(prev + step, 0, sv.ScrollableHeight);

        sv.SetValue(TargetOffsetProperty, next);
        sv.SetValue(ScrollOffsetProperty, next); // base value for FillBehavior.Stop

        // DecelerationRatio=1: starts at full speed, brakes smoothly to zero — matches Windows feel
        sv.BeginAnimation(ScrollOffsetProperty,
            new DoubleAnimation(sv.VerticalOffset, next, TimeSpan.FromMilliseconds(200))
            {
                DecelerationRatio = 1.0,
                FillBehavior = FillBehavior.Stop,
            });

        e.Handled = true;
    }
}
