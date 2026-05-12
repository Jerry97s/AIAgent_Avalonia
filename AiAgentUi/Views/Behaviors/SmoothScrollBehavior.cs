using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AiAgentUi.Views.Behaviors;

public static class SmoothScrollBehavior
{
    public static readonly AttachedProperty<bool> SmoothScrollProperty =
        AvaloniaProperty.RegisterAttached<ListBox, bool>("SmoothScroll", typeof(SmoothScrollBehavior));

    public static readonly AttachedProperty<double> SmoothWheelStepProperty =
        AvaloniaProperty.RegisterAttached<ListBox, double>("SmoothWheelStep", typeof(SmoothScrollBehavior), 48);

    public static void SetSmoothScroll(AvaloniaObject element, bool value) => element.SetValue(SmoothScrollProperty, value);
    public static bool GetSmoothScroll(AvaloniaObject element) => element.GetValue(SmoothScrollProperty);

    public static void SetSmoothWheelStep(AvaloniaObject element, double value) => element.SetValue(SmoothWheelStepProperty, value);
    public static double GetSmoothWheelStep(AvaloniaObject element) => element.GetValue(SmoothWheelStepProperty);

    static SmoothScrollBehavior()
    {
        SmoothScrollProperty.Changed.AddClassHandler<ListBox>(OnSmoothScrollChanged);
    }

    private static void OnSmoothScrollChanged(ListBox listBox, AvaloniaPropertyChangedEventArgs e)
    {
        listBox.RemoveHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged);
        if (e.NewValue is true)
            listBox.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
    }

    private static void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (sender is not ListBox listBox || !GetSmoothScroll(listBox))
            return;

        var viewer = listBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (viewer is null)
            return;

        var step = GetSmoothWheelStep(listBox);
        var direction = e.Delta.Y > 0 ? -1 : 1;
        var next = viewer.Offset.Y + direction * step;
        next = Math.Clamp(next, 0, Math.Max(0, viewer.Extent.Height - viewer.Viewport.Height));
        viewer.Offset = viewer.Offset.WithY(next);
        e.Handled = true;
    }
}
