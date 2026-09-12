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
            var panel = (System.Windows.Controls.StackPanel)type.GetField("body", flags)!.GetValue(window)!;
            var footer = panel.Children.OfType<System.Windows.Controls.TextBlock>().Last();
            if (footer.Text != "Made with ♥ by Syafiee Anis @ 2026" || footer.TextAlignment != TextAlignment.Center) throw new Exception("Footer text/alignment");
            var settingsControl = panel.Children.OfType<System.Windows.Controls.WrapPanel>().Single().Children.OfType<System.Windows.Controls.Button>().FirstOrDefault(b => b.Content?.ToString() == "⚙");
            if (settingsControl == null || System.Windows.Automation.AutomationProperties.GetName(settingsControl) != "Settings") throw new Exception("Missing accessible settings gear");
            var feedbackControl = panel.Children.OfType<System.Windows.Controls.WrapPanel>().Single().Children.OfType<System.Windows.Controls.Button>().FirstOrDefault(b => b.Content?.ToString() == "Feedback");
            if (feedbackControl == null || System.Windows.Automation.AutomationProperties.GetName(feedbackControl) != "Send feedback") throw new Exception("Missing accessible feedback button");
            Console.WriteLine("PASS settings gear and feedback controls have accessible names");
            var feedbackWindow = new FeedbackWindow(false);
            var feedbackGrid = (System.Windows.Controls.Grid)((System.Windows.Controls.Border)feedbackWindow.Content).Child;
            var submit = feedbackGrid.Children.OfType<System.Windows.Controls.StackPanel>().Single().Children.OfType<System.Windows.Controls.Button>().Single(b => b.Content?.ToString() == "Submit feedback");
            if (System.Windows.Controls.Grid.GetRow(submit.Parent as System.Windows.Controls.StackPanel) != 7) throw new Exception("Feedback submit button is not in bottom row");
            if (feedbackGrid.Children.OfType<System.Windows.Controls.CheckBox>().Single().IsChecked != true) throw new Exception("Feedback device info default");
            Console.WriteLine("PASS feedback dialog keeps submit button visible and device info selected");
            if (window.Icon == null || ((System.Windows.Forms.NotifyIcon)type.GetField("tray", flags)!.GetValue(window)!).Icon == null) throw new Exception("Missing application/tray icon");
            Console.WriteLine("PASS centered heart footer and bundled window/tray icons");
            using (var iconPixels = ((System.Windows.Forms.NotifyIcon)type.GetField("tray", flags)!.GetValue(window)!).Icon!.ToBitmap())
            {
                var black = 0; var white = 0;
                for (var y = 0; y < iconPixels.Height; y++)
                for (var x = 0; x < iconPixels.Width; x++)
                {
                    var pixel = iconPixels.GetPixel(x, y);
                    if (pixel.A > 200 && Math.Max(pixel.R, Math.Max(pixel.G, pixel.B)) < 100) black++;
                    if (pixel.A > 200 && Math.Min(pixel.R, Math.Min(pixel.G, pixel.B)) > 170) white++;
                }
                var minimum = iconPixels.Width * iconPixels.Height / 10;
                if (black < minimum || white < minimum) throw new Exception("Icon needs opaque dark and light regions for Windows themes");
            }
            Console.WriteLine("PASS loaded tray icon retains opaque dark and light contrast regions");
            var rows = new List<UsageWindow> { new("Codex", "5 hours", 75, null, "codex") };
            type.GetField("windows", flags)!.SetValue(window, rows);
            type.GetField("loggedIn", flags)!.SetValue(window, true);
            type.GetMethod("Render", flags)!.Invoke(window, null);
            var children = panel.Children.Cast<UIElement>().ToArray();
            var width = window.Width;
            var sizeMode = window.SizeToContent;
            rows[0] = rows[0] with { Remaining = 42 };
            type.GetMethod("Render", flags)!.Invoke(window, null);
            if (!children.SequenceEqual(panel.Children.Cast<UIElement>()) || window.Width != width || window.SizeToContent != sizeMode) throw new Exception("Refresh replaced controls or changed geometry");
            var quotaPanel = (System.Windows.Controls.StackPanel)children[2];
            if (!((System.Windows.Controls.TextBlock)quotaPanel.Children[1]).Text.StartsWith("42%")) throw new Exception("Usage value did not refresh");
            Console.WriteLine("PASS refresh updates values without replacing controls or window geometry");
            type.GetField("settings", flags)!.SetValue(window, new Preferences { Dock = "Kiri" });
            type.GetField("expanded", flags)!.SetValue(window, false);
            type.GetMethod("Render", flags)!.Invoke(window, null);
            var tab = panel.Children[0];
            type.GetField("loggedIn", flags)!.SetValue(window, false);
            rows.Clear();
            type.GetMethod("Render", flags)!.Invoke(window, null);
            if (!ReferenceEquals(tab, panel.Children[0]) || window.Width != 32 || window.Height != 112 || window.ShowInTaskbar) throw new Exception("Background auth change disturbed collapsed dock");
            Console.WriteLine("PASS dock remains collapsed and absent from taskbar through background changes");
        }
        finally
        {
            ((System.Windows.Forms.NotifyIcon)type.GetField("tray", flags)!.GetValue(window)!).Dispose();
            ((System.Drawing.Icon)type.GetField("trayIcon", flags)!.GetValue(window)!).Dispose();
            ((CodexClient)type.GetField("client", flags)!.GetValue(window)!).Dispose();
        }
    }
}
