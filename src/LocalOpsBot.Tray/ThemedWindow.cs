using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace LocalOpsBot.Tray;

/// <summary>
/// Base window that wears the Homebase custom title-bar chrome (see the <c>Window.Chrome</c> style
/// in HomebaseTheme.xaml). Title-bar dragging and Aero-Snap stay native via
/// <see cref="System.Windows.Shell.WindowChrome"/>; this wires the themed minimize/close caption
/// buttons drawn in the template, and asks DWM to round the corners (which also draws the standard
/// Windows drop shadow) on Windows 11.
/// </summary>
public class ThemedWindow : Window
{
    // DWM window attributes (Windows 11 build 22000+). Rounding a borderless WindowChrome window
    // also gives it the standard drop shadow; on Windows 10 the attribute is a harmless no-op.
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int round = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));
        }
        catch { /* pre-Win11 or unsupported: corners stay square, no harm */ }
    }

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

    private void Wire(string partName, Action action)
    {
        if (GetTemplateChild(partName) is Button button)
            button.Click += (_, _) => action();
    }
}
