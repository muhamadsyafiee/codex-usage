using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RadioButton = System.Windows.Controls.RadioButton;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace CodexUsage;

public partial class SettingsWindow : Window
{
    private readonly Preferences settings;
    private readonly Action changed;
    private readonly Action<string> setDock;
    private readonly Func<string> updateLabel;
    private readonly Func<Task<string>> update;
    private readonly Func<Task> signOut;
    private readonly Action exit;
    private readonly Action<bool> setStartup;
    private bool ready;

    public SettingsWindow(Preferences settings, string account, bool loggedIn, Action changed,
        Action<string> setDock, Func<string> updateLabel, Func<Task<string>> update,
        Func<Task> signOut, Action exit, Action<bool>? setStartup = null)
    {
        this.settings = settings;
        this.changed = changed;
        this.setDock = setDock;
        this.updateLabel = updateLabel;
        this.update = update;
        this.signOut = signOut;
        this.exit = exit;
        this.setStartup = setStartup ?? StartupService.SetEnabled;
        InitializeComponent();
        using (var stream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/CodexUsage;component/Assets/app.ico"))!.Stream)
        {
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            BrandIcon.Source = decoder.Frames.OrderByDescending(frame => frame.PixelWidth).First();
        }
        SetTheme();
        foreach (var option in DockOptions.Children.OfType<RadioButton>()) option.IsChecked = (string)option.Tag == settings.Dock;
        foreach (var option in LayoutOptions.Children.OfType<RadioButton>()) option.IsChecked = (string)option.Tag == settings.Layout;
        LightTheme.IsChecked = settings.Light;
        DarkTheme.IsChecked = !settings.Light;
        OpacitySlider.Value = double.IsFinite(settings.WidgetOpacity) ? Math.Clamp(settings.WidgetOpacity * 100, 20, 100) : 70;
        OpacityValue.Text = $"{OpacitySlider.Value:0}%";
        StartupToggle.IsChecked = settings.RunAtWindowsLogin;
        UpdateDockControls();
        UpdateButton.Content = updateLabel();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Version {version?.ToString(3)}";
        AccountText.Text = account;
        SignOutButton.IsEnabled = loggedIn;
        ready = true;
    }

    private void SetTheme()
    {
        Resources["Surface"] = settings.Light ? Brushes.White : Brushes.Black;
        Resources["Ink"] = settings.Light ? Brushes.Black : Brushes.White;
        Resources["Muted"] = new SolidColorBrush(settings.Light ? Color.FromRgb(80, 80, 80) : Color.FromRgb(189, 189, 189));
        Resources["Line"] = new SolidColorBrush(settings.Light ? Color.FromRgb(136, 136, 136) : Color.FromRgb(119, 119, 119));
    }

    private void UpdateDockControls()
    {
        var docked = settings.Dock != "Bebas";
        TopmostToggle.IsEnabled = !docked;
        TopmostToggle.IsChecked = docked || settings.Topmost;
        TopmostHint.Text = docked ? "Docked widgets always stay on top." : "Keep the floating widget above other windows.";
    }

    private void DockChanged(object sender, RoutedEventArgs e)
    {
        if (!ready) return;
        setDock((string)((RadioButton)sender).Tag);
        UpdateDockControls();
    }

    private void LayoutChanged(object sender, RoutedEventArgs e)
    {
        if (!ready) return;
        settings.Layout = (string)((RadioButton)sender).Tag;
        changed();
    }

    private void ThemeChanged(object sender, RoutedEventArgs e)
    {
        if (!ready) return;
        settings.Light = LightTheme.IsChecked == true;
        SetTheme();
        changed();
    }

    private void OpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!ready) return;
        settings.WidgetOpacity = OpacitySlider.Value / 100;
        OpacityValue.Text = $"{OpacitySlider.Value:0}%";
        changed();
    }

    private void TopmostChanged(object sender, RoutedEventArgs e)
    {
        settings.Topmost = TopmostToggle.IsChecked == true;
        changed();
    }

    private void StartupChanged(object sender, RoutedEventArgs e)
    {
        try
        {
            setStartup(StartupToggle.IsChecked == true);
            settings.RunAtWindowsLogin = StartupToggle.IsChecked == true;
            changed();
            SaveStatus.Text = "Changes saved automatically";
        }
        catch
        {
            StartupToggle.IsChecked = settings.RunAtWindowsLogin;
            SaveStatus.Text = "Could not change Windows startup. Try again.";
        }
    }

    private async void UpdateClicked(object sender, RoutedEventArgs e)
    {
        UpdateButton.IsEnabled = false;
        UpdateStatus.Text = updateLabel().StartsWith("Install ", StringComparison.Ordinal) ? "Downloading update…" : "Checking for updates…";
        try { UpdateStatus.Text = await update(); }
        catch { UpdateStatus.Text = "Update check failed. Check your connection and try again."; }
        finally { UpdateButton.Content = updateLabel(); UpdateButton.IsEnabled = true; }
    }

    private async void SignOutClicked(object sender, RoutedEventArgs e)
    {
        SignOutButton.IsEnabled = false;
        AccountStatus.Text = "Signing out…";
        try
        {
            await signOut();
            AccountText.Text = "Not signed in";
            AccountStatus.Text = "Signed out. Use Sign in on the widget to reconnect.";
        }
        catch
        {
            SignOutButton.IsEnabled = true;
            AccountStatus.Text = "Could not sign out. Try again shortly.";
        }
    }

    private void DoneClicked(object sender, RoutedEventArgs e) => Close();
    private void ExitClicked(object sender, RoutedEventArgs e) { Close(); exit(); }
}
