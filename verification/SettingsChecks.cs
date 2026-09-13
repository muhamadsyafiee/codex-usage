using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CodexUsage;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using RadioButton = System.Windows.Controls.RadioButton;
using ButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using Control = System.Windows.Controls.Control;

internal static class SettingsChecks
{
    public static void Run()
    {
        var preferences = new Preferences();
        var changes = 0;
        var startupFails = false;
        var startupEnabled = false;
        var updateFails = false;
        TaskCompletionSource<string>? pendingUpdate = null;
        var signOutFails = true;
        var exited = false;
        var dialog = new SettingsWindow(preferences, "Not signed in", true, () => changes++,
            edge => { preferences.Dock = edge; changes++; }, () => "Check for updates",
            () => pendingUpdate?.Task ?? (updateFails ? Task.FromException<string>(new IOException()) : Task.FromResult("You are up to date.")),
            () => signOutFails ? Task.FromException(new IOException()) : Task.CompletedTask,
            () => exited = true,
            enabled => { if (startupFails) throw new IOException(); startupEnabled = enabled; });
        T Find<T>(string name) where T : FrameworkElement => (T)dialog.FindName(name);
        void Click(string name) => dialog.Dispatcher.Invoke(() => Find<ButtonBase>(name).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)));
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

        Check(changes == 0 && preferences.WidgetOpacity == 0.7, "Opening settings mutated preferences");
        foreach (var option in Find<WrapPanel>("DockOptions").Children.OfType<RadioButton>())
        {
            option.IsChecked = true;
            Check(preferences.Dock == (string)option.Tag, "Dock selection not applied");
            Check(Find<CheckBox>("TopmostToggle").IsEnabled == (preferences.Dock == "Bebas"), "Dock topmost control state");
        }
        var floating = Find<WrapPanel>("DockOptions").Children.OfType<RadioButton>().First();
        floating.IsChecked = true;
        Find<CheckBox>("TopmostToggle").IsChecked = false;
        Click("TopmostToggle");
        Check(!preferences.Topmost, "Floating always-on-top not applied");
        foreach (var option in Find<WrapPanel>("LayoutOptions").Children.OfType<RadioButton>())
        {
            option.IsChecked = true;
            Check(preferences.Layout == (string)option.Tag, "Layout selection not applied");
        }
        foreach (var percent in new[] { 20d, 42d, 100d })
        {
            Find<Slider>("OpacitySlider").Value = percent;
            Check(preferences.WidgetOpacity == percent / 100 && Find<TextBlock>("OpacityValue").Text == $"{percent:0}%", "Opacity percentage not applied");
        }
        Find<CheckBox>("StartupToggle").IsChecked = true;
        Click("StartupToggle");
        Check(startupEnabled && preferences.RunAtWindowsLogin, "Startup setting not applied");
        startupFails = true;
        Find<CheckBox>("StartupToggle").IsChecked = false;
        Click("StartupToggle");
        Check(Find<CheckBox>("StartupToggle").IsChecked == true && preferences.RunAtWindowsLogin && Find<TextBlock>("SaveStatus").Text.Contains("Could not"), "Startup error did not revert checkbox");
        Click("UpdateButton");
        Check(Find<TextBlock>("UpdateStatus").Text == "You are up to date." && Find<Button>("UpdateButton").IsEnabled, "Update success state");
        updateFails = true;
        Click("UpdateButton");
        Check(Find<TextBlock>("UpdateStatus").Text.Contains("failed") && Find<Button>("UpdateButton").IsEnabled, "Update error state");
        pendingUpdate = new TaskCompletionSource<string>();
        Click("UpdateButton");
        Check(!Find<Button>("UpdateButton").IsEnabled && Find<TextBlock>("UpdateStatus").Text == "Checking for updates…", "Update loading state missing");
        pendingUpdate.SetResult("You are up to date.");
        dialog.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
        Check(Find<Button>("UpdateButton").IsEnabled && Find<TextBlock>("UpdateStatus").Text == "You are up to date.", "Update completion did not restore button");
        Click("SignOutButton");
        Check(Find<Button>("SignOutButton").IsEnabled && Find<TextBlock>("AccountStatus").Text.Contains("Could not"), "Sign-out retry state");
        signOutFails = false;
        Click("SignOutButton");
        Check(!Find<Button>("SignOutButton").IsEnabled && Find<TextBlock>("AccountText").Text == "Not signed in", "Sign-out success state");
        Console.WriteLine("PASS settings dock/layout/opacity/topmost actions; startup rollback; update loading/success/retry; sign-out success/retry (UI events with fake external services)");

        Find<TextBlock>("SaveStatus").Text = "Changes saved automatically";
        Find<TextBlock>("AccountStatus").Text = "";
        Find<TextBlock>("UpdateStatus").Text = "";
        Find<Slider>("OpacitySlider").Value = 70;
        foreach (var light in new[] { false, true })
        {
            (light ? Find<RadioButton>("LightTheme") : Find<RadioButton>("DarkTheme")).IsChecked = true;
            Check(preferences.Light == light, "Theme not applied");
            foreach (var size in new[] { (Width: 510d, Height: 930d), (Width: 390d, Height: 430d) })
            {
                var root = (FrameworkElement)dialog.Content;
                root.Measure(new System.Windows.Size(size.Width, size.Height));
                root.Arrange(new Rect(0, 0, size.Width, size.Height));
                root.UpdateLayout();
                var footer = (Border)((Grid)root).Children[2];
                var footerTop = footer.TranslatePoint(new System.Windows.Point(), root).Y;
                Check(footerTop >= 0 && footerTop + footer.ActualHeight <= size.Height + 0.1, "Settings footer clipped");
                Check(Find<Slider>("OpacitySlider").Template.FindName("PART_Track", Find<Slider>("OpacitySlider")) is Track, "Slider track missing");
                var image = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
                image.Render(root);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                Directory.CreateDirectory("dist/settings-preview");
                using var output = File.Create($"dist/settings-preview/{(light ? "white" : "black")}-{size.Height:0}.png");
                encoder.Save(output);
                if (size.Height == 430)
                {
                    var scroll = (ScrollViewer)((Grid)root).Children[1];
                    scroll.ScrollToEnd();
                    root.UpdateLayout();
                    var accountY = Find<TextBlock>("AccountText").TranslatePoint(new System.Windows.Point(), scroll).Y;
                    Check(accountY >= 0 && accountY + Find<TextBlock>("AccountText").ActualHeight <= scroll.ActualHeight, "Account unreachable by scrolling");
                    scroll.ScrollToHome();
                    root.UpdateLayout();
                }
            }
        }
        foreach (var control in Descendants((DependencyObject)dialog.Content).OfType<Control>().Where(c => c is Button or CheckBox or RadioButton or Slider))
            Check(control.Focusable && control.FocusVisualStyle != null, "Settings control missing keyboard focus style");
        Check(Descendants((DependencyObject)dialog.Content).OfType<Button>().Single(b => b.Content?.ToString() == "Done").IsCancel, "Settings Escape dismissal missing");
        var closed = false;
        dialog.Closed += (_, _) => closed = true;
        Descendants((DependencyObject)dialog.Content).OfType<Button>().Single(b => b.Content?.ToString() == "Done").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Check(closed && !exited, "Done did not close settings independently");
        Console.WriteLine("PASS settings black/white render at 510x930 and 390x430; fixed footer; keyboard focus styles; Done closes; Escape wired via IsCancel");

        var exitDialog = new SettingsWindow(new Preferences(), "Not signed in", false, () => { }, _ => { },
            () => "Check for updates", () => Task.FromResult(""), () => Task.CompletedTask, () => exited = true, _ => { });
        ((FrameworkElement)exitDialog.Content).Measure(new System.Windows.Size(510, 930));
        ((FrameworkElement)exitDialog.Content).Arrange(new Rect(0, 0, 510, 930));
        Check(!((Button)exitDialog.FindName("SignOutButton")).IsEnabled, "Signed-out account action enabled");
        Descendants((DependencyObject)exitDialog.Content).OfType<Button>().Single(b => b.Content?.ToString() == "Exit app").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Check(exited, "Exit callback missing");
        Console.WriteLine("PASS settings signed-out empty state and Exit app action");
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
