using System.Reflection;
using System.Windows;
using System.Windows.Input;
using CodexUsage;
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var window = new Widget();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var type = typeof(Widget);
        try
        {
            foreach (var edge in new[] { "Kiri", "Kanan", "Atas", "Bawah" })
            {
                type.GetField("settings", flags)!.SetValue(window, new Preferences { Dock = edge });
                type.GetField("expanded", flags)!.SetValue(window, false);
                type.GetMethod("Render", flags)!.Invoke(window, null);
                var vertical = edge is "Kiri" or "Kanan";
                if (window.Width != (vertical ? 32 : 112) || window.Height != (vertical ? 112 : 32)) throw new Exception(edge + " collapsed bounds");
                window.RaiseEvent(new System.Windows.Input.MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseEnterEvent });
                if (window.Width != 400 || !(bool)type.GetField("expanded", flags)!.GetValue(window)!) throw new Exception(edge + " hover expansion");
                Console.WriteLine("PASS " + edge + " WPF handle and hover expansion");
            }
            type.GetField("settings", flags)!.SetValue(window, new Preferences());
            type.GetMethod("Render", flags)!.Invoke(window, null);
            if (window.Width != 400 || window.SizeToContent != SizeToContent.Height) throw new Exception("Floating restore");
            Console.WriteLine("PASS WPF floating layout restored");
            if (Math.Abs(window.Opacity - 0.7) > 0.001 || !window.AllowsTransparency) throw new Exception("Default opacity");
            Console.WriteLine("PASS WPF default 70% opacity");
            type.GetField("settings", flags)!.SetValue(window, new Preferences { WidgetOpacity = 0.42 });
            type.GetMethod("Render", flags)!.Invoke(window, null);
            if (Math.Abs(window.Opacity - 0.42) > 0.001) throw new Exception("Custom opacity");
            Console.WriteLine("PASS WPF custom opacity");
        }
        finally
        {
            ((System.Windows.Forms.NotifyIcon)type.GetField("tray", flags)!.GetValue(window)!).Dispose();
            ((CodexClient)type.GetField("client", flags)!.GetValue(window)!).Dispose();
        }
    }
}
