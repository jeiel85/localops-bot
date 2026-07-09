using System.Windows;
using System.Windows.Controls;

namespace LocalOpsBot.Tray;

/// <summary>
/// Base window that wears the Homebase custom title-bar chrome (see the <c>Window.Chrome</c> style
/// in HomebaseTheme.xaml). Title-bar dragging and Aero-Snap stay native via
/// <see cref="System.Windows.Shell.WindowChrome"/>; this wires the themed minimize/close caption
/// buttons drawn in the template to the corresponding window actions.
/// </summary>
public class ThemedWindow : Window
{
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        Wire("PART_Minimize", () => WindowState = WindowState.Minimized);
        Wire("PART_Close", Close);

        // Dialogs (NoResize) get a close button only. Done here rather than with a template
        // trigger — a TargetName trigger inside this Window ControlTemplate trips a WPF
        // template-parser NullReferenceException.
        if (ResizeMode == ResizeMode.NoResize && GetTemplateChild("PART_Minimize") is UIElement min)
            min.Visibility = Visibility.Collapsed;
    }

    private void Wire(string partName, System.Action action)
    {
        if (GetTemplateChild(partName) is Button button)
            button.Click += (_, _) => action();
    }
}
