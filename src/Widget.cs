using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Button = System.Windows.Controls.Button;
using Brushes = System.Windows.Media.Brushes;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
namespace CodexUsage;
public sealed class Preferences
{
    public string Layout { get; set; } = "Kad";
    public bool Light { get; set; }
    public bool Topmost { get; set; } = true;
    public double Left { get; set; } = 80;
    public double Top { get; set; } = 80;
    public string Dock { get; set; } = "Bebas";
    public string? Monitor { get; set; }
    public double DockOffset { get; set; } = 0.5;
    public double WidgetOpacity { get; set; } = 0.7;
}
public sealed class Widget : Window
{
    private readonly CodexClient client = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMinutes(1) };
    private readonly System.Windows.Forms.NotifyIcon tray;
    private readonly System.Drawing.Icon trayIcon;
    private readonly StackPanel body = new();
    private readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexUsage", "settings.json");
    private Preferences settings = new();
    private List<UsageWindow> windows = [];
    private string account = "Not signed in", status = "Sign in to view your remaining usage.";
    private bool loggedIn, busy;
    private string? loginId;
    private bool expanded, menuOpen;
    private readonly DispatcherTimer collapseTimer = new() { Interval = TimeSpan.FromMilliseconds(450) };
    private bool Docked => settings.Dock != "Bebas";
    private Rect dockArea;
    private bool dragging, checkingUpdate, installing;
    private string? renderedState;
    private TextBlock? statusText, accountText;
    private readonly List<(TextBlock Value, Grid Track, TextBlock? Reset)> usageRows = [];
    private System.Windows.Point dragStart;
    private double dragOffset;
    private readonly DispatcherTimer updateTimer = new() { Interval = TimeSpan.FromHours(6) };
    private readonly UpdateService updates = new();
    private ReleaseUpdate? availableUpdate;
    public Widget()
    {
        try { if (File.Exists(settingsPath)) settings = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(settingsPath)) ?? new(); } catch { }
        Title = "Codex Usage"; WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
        var iconUri = new Uri("pack://application:,,,/CodexUsage;component/Assets/app.ico");
        Icon = System.Windows.Media.Imaging.BitmapFrame.Create(iconUri);
        using (var iconStream = System.Windows.Application.GetResourceStream(iconUri)!.Stream)
        using (var sourceIcon = new System.Drawing.Icon(iconStream)) trayIcon = (System.Drawing.Icon)sourceIcon.Clone();
        AllowsTransparency = true;
        SizeToContent = SizeToContent.Height; FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
        Topmost = settings.Topmost; Left = Math.Clamp(settings.Left, SystemParameters.VirtualScreenLeft, Math.Max(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenWidth - 420));
        Top = Math.Clamp(settings.Top, SystemParameters.VirtualScreenTop, Math.Max(SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenHeight - 300));
        Content = new Border { BorderThickness = new Thickness(1), BorderBrush = Brushes.Gray, Padding = new Thickness(22), Child = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled } };
        if (!new[] { "Bebas", "Kiri", "Kanan", "Atas", "Bawah" }.Contains(settings.Dock)) settings.Dock = "Bebas";
        MouseEnter += (_, _) => { collapseTimer.Stop(); if (Docked && !expanded && !dragging) { expanded = true; Render(); } };
        MouseMove += MoveDock;
        MouseLeftButtonUp += (_, _) => EndDockDrag();
        LostMouseCapture += (_, _) => { if (dragging) EndDockDrag(); };
        MouseLeave += (_, _) => { if (Docked) collapseTimer.Start(); };
        collapseTimer.Tick += (_, _) => { collapseTimer.Stop(); if (Docked && !IsMouseOver && !menuOpen && !dragging) { expanded = false; Render(); } };
        Deactivated += (_, _) => { if (Docked) collapseTimer.Start(); };
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && Docked) { expanded = false; Render(); e.Handled = true; return; }
            if (Docked && e.Key is Key.Left or Key.Right or Key.Up or Key.Down)
            {
                var along = settings.Dock is "Kiri" or "Kanan" ? e.Key is Key.Up or Key.Down : e.Key is Key.Left or Key.Right;
                if (along) { settings.DockOffset = Math.Clamp(settings.DockOffset + ((e.Key is Key.Right or Key.Down) ? 0.02 : -0.02), 0, 1); PositionDock(); Save(); e.Handled = true; }
            }
        };
        MouseRightButtonUp += (_, _) => ShowMenu();
        SizeChanged += (_, _) => PositionDock();
        tray = new System.Windows.Forms.NotifyIcon { Icon = trayIcon, Text = "Codex Usage", Visible = true };
        tray.DoubleClick += (_, _) => Dispatcher.Invoke(Reveal);
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Open widget", null, (_, _) => Dispatcher.Invoke(Reveal));
        menu.Items.Add("Check for updates", null, (_, _) => Dispatcher.Invoke(async () => await CheckUpdate(true)));
        tray.BalloonTipClicked += (_, _) => Dispatcher.Invoke(() => { Show(); expanded = true; Render(); Activate(); });
        menu.Items.Add("Exit", null, (_, _) => Dispatcher.Invoke(Close)); tray.ContextMenuStrip = menu;
        timer.Tick += async (_, _) => await Refresh(true);
        client.Notification += (method, data) => Dispatcher.BeginInvoke(async () =>
        {
            if (method == "account/login/completed")
            {
                loginId = null;
                if (data.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True) await Refresh();
                else { status = "Sign-in failed. Try again."; Render(); }
            }
        });
        updateTimer.Tick += async (_, _) => await CheckUpdate(false);
        Loaded += async (_, _) => { ShowInTaskbar = !Docked; UpdateDockArea(); PositionDock(); await Refresh(); timer.Start(); updateTimer.Start(); await CheckUpdate(false); };
        Closed += (_, _) => { timer.Stop(); updateTimer.Stop(); collapseTimer.Stop(); Save(); tray.Dispose(); trayIcon.Dispose(); client.Dispose(); System.Windows.Application.Current.Shutdown(); };
        Render();
    }
    private void Save()
    {
        if (!Docked) { settings.Left = Left; settings.Top = Top; }
        try { Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!); File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings)); } catch { }
    }
    private TextBlock Text(string value, double size = 12) => new() { Text = value, FontSize = size, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8), Foreground = Foreground };
    private Button Action(string label, Func<Task> action)
    {
        var button = new Button { Content = label, Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 3, 6, 3), Background = Background, Foreground = Foreground, BorderBrush = Foreground, BorderThickness = new Thickness(1), IsEnabled = true };
        button.Click += async (_, _) => { try { await action(); } catch { status = "Operation failed. Try again or sign in again."; Render(); } };
        return button;
    }
    private void Render()
    {
        var collapsed = Docked && !expanded;
        var state = collapsed ? $"tab|{settings.Light}|{settings.Dock}|{availableUpdate?.Version}" : $"{settings.Light}|{settings.Layout}|{settings.Dock}|{expanded}|{loggedIn}|{loginId}|{availableUpdate?.Version}|{installing}|{settings.WidgetOpacity}";
        if (!collapsed) state += string.Join("|", windows.Select(w => w.BucketId + w.Bucket + w.Label));
        if (renderedState == state) { UpdateReadings(); return; }
        renderedState = state;
        ShowInTaskbar = !Docked && IsVisible;
        Background = settings.Light ? Brushes.White : Brushes.Black; Foreground = settings.Light ? Brushes.Black : Brushes.White;
        var preferredOpacity = double.IsFinite(settings.WidgetOpacity) ? Math.Clamp(settings.WidgetOpacity, 0.2, 1) : 0.7;
        Opacity = Docked && expanded ? 1 : preferredOpacity;
        var frame = (Border)Content;
        frame.Padding = new Thickness(22);
        ((ScrollViewer)frame.Child).VerticalScrollBarVisibility = Docked && !expanded ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
        MaxHeight = Docked && dockArea.Height > 0 ? dockArea.Height : double.PositiveInfinity;
        SizeToContent = collapsed ? SizeToContent.Manual : SizeToContent.Height;
        Height = collapsed ? (settings.Dock is "Kiri" or "Kanan" ? 112 : 32) : double.NaN;
        Topmost = Docked || settings.Topmost;
        Width = collapsed ? (settings.Dock is "Kiri" or "Kanan" ? 32 : 112) : settings.Layout == "Kompak" ? 320 : 400;
        body.Children.Clear();
        usageRows.Clear(); statusText = null; accountText = null;
        if (Docked && !expanded)
        {
            var vertical = settings.Dock is "Kiri" or "Kanan";
            SizeToContent = SizeToContent.Manual;
            Width = vertical ? 32 : 112; Height = vertical ? 112 : 32;
            frame.Padding = new Thickness(0);
            body.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            body.VerticalAlignment = VerticalAlignment.Center;
            var handle = new Button { Content = availableUpdate == null ? "CODEX" : "UPDATE", FontSize = 11, Padding = new Thickness(0), Background = Background, Foreground = Foreground, BorderBrush = Foreground, BorderThickness = new Thickness(0), Focusable = true, ToolTip = "Hover or press Enter to view usage" };
            handle.Click += (_, _) => { expanded = true; Render(); };
            handle.MouseLeftButtonDown += BeginDockDrag;
            handle.Margin = new Thickness(0);
            if (vertical) handle.LayoutTransform = new RotateTransform(-90);
            body.Children.Add(handle);
            PositionDock();
            return;
        }
        body.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
        body.VerticalAlignment = VerticalAlignment.Top;
        var header = Text("CODEX / USAGE", 13); header.FontWeight = FontWeights.SemiBold; header.Cursor = System.Windows.Input.Cursors.SizeAll;
        header.ToolTip = "Drag to move · right-click for settings"; header.MouseLeftButtonDown += (_, e) => { if (Docked) BeginDockDrag(header, e); else if (e.ButtonState == MouseButtonState.Pressed) { DragMove(); Save(); } }; body.Children.Add(header);
        if (settings.Layout != "Kompak") { accountText = Text(account); body.Children.Add(accountText); }
        if (!loggedIn)
        {
            body.Children.Add(Text("Your usage.\nAt a glance.", 28));
            body.Children.Add(Text("Connect your ChatGPT account. Sign in securely in your browser."));
        }
        foreach (var item in windows)
        {
            var card = new StackPanel { Margin = new Thickness(0, 10, 0, 10) };
            card.Children.Add(Text($"{item.Bucket.ToUpperInvariant()}  /  {item.Label}"));
            var valueText = Text("", settings.Layout == "Kompak" ? 23 : 38);
            card.Children.Add(valueText);
            var track = new Grid { Height = 5, Background = settings.Light ? Brushes.LightGray : Brushes.DimGray };
            track.ColumnDefinitions.Add(new ColumnDefinition());
            track.ColumnDefinitions.Add(new ColumnDefinition());
            track.Children.Add(new Border { Background = Foreground });
            card.Children.Add(track);
            var resetText = settings.Layout == "Terperinci" ? Text("") : null;
            if (resetText != null) card.Children.Add(resetText);
            usageRows.Add((valueText, track, resetText));
            body.Children.Add(card);
        }
        statusText = Text(status, 11); body.Children.Add(statusText);
        if (availableUpdate != null) body.Children.Add(Action(installing ? "Downloading update…" : $"Update v{availableUpdate.Version}", InstallUpdate));
        var controls = new WrapPanel();
        if (!loggedIn) controls.Children.Add(Action(loginId == null ? "Sign in" : "Cancel sign-in", Login));
        controls.Children.Add(Action("Refresh", () => Refresh()));
        var feedbackButton = Action("Feedback", OpenFeedback);
        System.Windows.Automation.AutomationProperties.SetName(feedbackButton, "Send feedback");
        controls.Children.Add(feedbackButton);
        var settingsButton = Action("⚙", () => { ShowMenu(); return Task.CompletedTask; });
        settingsButton.ToolTip = "Settings";
        System.Windows.Automation.AutomationProperties.SetName(settingsButton, "Settings");
        settingsButton.FontSize = 15;
        settingsButton.Padding = new Thickness(8, 3, 8, 3);
        controls.Children.Add(settingsButton);
        controls.Children.Add(Action("Hide", () => { ShowInTaskbar = false; Hide(); return Task.CompletedTask; })); body.Children.Add(controls);
        var footer = Text("Made with ♥ by Syafiee Anis @ 2026", 11);
        footer.TextAlignment = TextAlignment.Center;
        footer.Margin = new Thickness(0, 14, 0, 0);
        System.Windows.Automation.AutomationProperties.SetName(footer, "Made with love by Syafiee Anis at 2026");
        body.Children.Add(footer);
        UpdateReadings();
        PositionDock();
    }
    private Task OpenFeedback()
    {
        var dialog = new FeedbackWindow(settings.Light) { Owner = this };
        dialog.ShowDialog();
        return Task.CompletedTask;
    }
    private void UpdateDockArea()
    {
        var screen = System.Windows.Forms.Screen.AllScreens.FirstOrDefault(s => s.DeviceName == settings.Monitor)
            ?? System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
        var scale = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var area = screen.WorkingArea;
        var origin = scale.Transform(new System.Windows.Point(area.Left, area.Top));
        var size = scale.Transform(new System.Windows.Vector(area.Width, area.Height));
        dockArea = new Rect(origin.X, origin.Y, size.X, size.Y);
    }
    private void UpdateReadings()
    {
        if (statusText != null) statusText.Text = status;
        if (accountText != null) accountText.Text = account;
        for (var i = 0; i < Math.Min(usageRows.Count, windows.Count); i++)
        {
            var row = usageRows[i]; var item = windows[i];
            row.Value.Text = item.Remaining is { } n ? $"{n:0.#}% remaining" : "Unavailable";
            var percent = item.Remaining ?? 0;
            row.Track.ColumnDefinitions[0].Width = new GridLength(percent, GridUnitType.Star);
            row.Track.ColumnDefinitions[1].Width = new GridLength(100 - percent, GridUnitType.Star);
            if (row.Reset != null) row.Reset.Text = item.Reset is { } reset ? $"Reset {reset.ToLocalTime().ToString("ddd, dd MMM · HH:mm", System.Globalization.CultureInfo.GetCultureInfo("en-US"))}" : "Reset time unavailable";
        }
    }
    private void PositionDock()
    {
        if (!Docked || !IsLoaded) return;
        UpdateDockArea();
        MaxHeight = dockArea.Height;
        var height = SizeToContent == SizeToContent.Manual ? Height : ActualHeight;
        var location = DockGeometry.Place(settings.Dock, dockArea.X, dockArea.Y, dockArea.Width, dockArea.Height, Width, height, settings.DockOffset);
        Left = location.Left; Top = location.Top;
    }
    private void SetDock(string edge)
    {
        if (!Docked) { settings.Left = Left; settings.Top = Top; }
        if (edge != "Bebas") settings.Monitor = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle).DeviceName;
        settings.Dock = edge; expanded = false;
        if (!Docked) { Left = settings.Left; Top = settings.Top; }
        Save(); Render();
    }
    private void BeginDockDrag(object sender, MouseButtonEventArgs e)
    {
        if (!Docked) return;
        dragging = true;
        dragStart = PointToScreen(e.GetPosition(this));
        dragOffset = settings.DockOffset;
        collapseTimer.Stop(); CaptureMouse(); e.Handled = true;
    }
    private void MoveDock(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!dragging) return;
        var point = PointToScreen(e.GetPosition(this));
        var scale = VisualTreeHelper.GetDpi(this);
        var vertical = settings.Dock is "Kiri" or "Kanan";
        var travel = vertical ? dockArea.Height - ActualHeight : dockArea.Width - ActualWidth;
        var delta = vertical ? (point.Y - dragStart.Y) / scale.DpiScaleY : (point.X - dragStart.X) / scale.DpiScaleX;
        settings.DockOffset = Math.Clamp(dragOffset + delta / Math.Max(1, travel), 0, 1);
        PositionDock();
    }
    private void EndDockDrag()
    {
        if (!dragging) return;
        dragging = false; ReleaseMouseCapture(); Save(); collapseTimer.Start();
    }
    public void Reveal()
    {
        ShowInTaskbar = !Docked; Show(); expanded = true; Render(); Activate();
    }
    private async Task CheckUpdate(bool manual)
    {
        if (checkingUpdate || installing) return;
        checkingUpdate = true;
        if (manual) { status = "Checking for updates…"; Render(); }
        try
        {
            var found = await updates.Check();
            var notify = found != null && found.Version != availableUpdate?.Version;
            availableUpdate = found;
            if (notify) tray.ShowBalloonTip(8000, "Update Codex Usage", $"Version {found!.Version} is available. Click to open the widget.", System.Windows.Forms.ToolTipIcon.Info);
            if (manual) { status = found == null ? "You are up to date." : $"Version {found.Version} is available."; Reveal(); }
            else Render();
        }
        catch { if (manual) { status = "Update check failed. Check your connection and try again."; Reveal(); } }
        finally { checkingUpdate = false; }
    }
    private async Task InstallUpdate()
    {
        if (installing || availableUpdate == null) return;
        installing = true; Render();
        try
        {
            var path = await updates.Download(availableUpdate);
            var start = new ProcessStartInfo("msiexec.exe") { UseShellExecute = true };
            start.Arguments = $"/i \"{path}\" /norestart";
            _ = Process.Start(start) ?? throw new IOException("Could not start the installer.");
            Close();
        }
        catch { status = "Update failed. Try again from the update menu."; }
        finally { installing = false; if (IsLoaded) Render(); }
    }
    private static string EnglishLabel(string value) => value switch { "Bebas" => "Floating", "Kiri" => "Left", "Kanan" => "Right", "Atas" => "Top", "Bawah" => "Bottom", "Kompak" => "Compact", "Kad" => "Card", "Terperinci" => "Detailed", _ => value };
    private void ShowMenu()
    {
        var menu = new ContextMenu();
        menuOpen = true;
        menu.Closed += (_, _) => { menuOpen = false; collapseTimer.Start(); };
        var docking = new MenuItem { Header = "Dock position" };
        foreach (var edge in new[] { "Bebas", "Kiri", "Kanan", "Atas", "Bawah" })
        {
            var option = new MenuItem { Header = EnglishLabel(edge), IsCheckable = true, IsChecked = settings.Dock == edge };
            option.Click += (_, _) => SetDock(edge);
            docking.Items.Add(option);
        }
        menu.Items.Add(docking);
        var opacity = new MenuItem { Header = $"Opacity: {settings.WidgetOpacity:P0}" };
        var opacityValue = new TextBlock { Text = $"{settings.WidgetOpacity:P0}", Margin = new Thickness(12, 6, 12, 0) };
        var slider = new Slider { Minimum = 20, Maximum = 100, Value = settings.WidgetOpacity * 100, TickFrequency = 1, IsSnapToTickEnabled = true, Width = 180, Margin = new Thickness(12) };
        slider.ValueChanged += (_, _) => { settings.WidgetOpacity = slider.Value / 100; Opacity = Docked && expanded ? 1 : settings.WidgetOpacity; opacityValue.Text = $"{slider.Value:0}%"; opacity.Header = $"Opacity: {slider.Value:0}%"; Save(); };
        var opacityPanel = new StackPanel(); opacityPanel.Children.Add(opacityValue); opacityPanel.Children.Add(slider);
        opacity.Items.Add(new MenuItem { Header = opacityPanel, StaysOpenOnClick = true }); menu.Items.Add(opacity);
        var update = new MenuItem { Header = availableUpdate == null ? "Check for updates" : $"Update v{availableUpdate.Version}", IsEnabled = !installing };
        update.Click += async (_, _) => { if (availableUpdate == null) await CheckUpdate(true); else await InstallUpdate(); }; menu.Items.Add(update);
        foreach (var layout in new[] { "Kompak", "Kad", "Terperinci" })
        {
            var item = new MenuItem { Header = EnglishLabel(layout), IsCheckable = true, IsChecked = settings.Layout == layout };
            item.Click += (_, _) => { settings.Layout = layout; Save(); Render(); }; menu.Items.Add(item);
        }
        var theme = new MenuItem { Header = "Light theme", IsCheckable = true, IsChecked = settings.Light };
        theme.Click += (_, _) => { settings.Light = !settings.Light; Save(); Render(); }; menu.Items.Add(theme);
        var pin = new MenuItem { Header = "Always on top", IsCheckable = true, IsChecked = Topmost, IsEnabled = !Docked };
        pin.Click += (_, _) => { settings.Topmost = Topmost = !Topmost; Save(); }; menu.Items.Add(pin);
        var logout = new MenuItem { Header = "Sign out", IsEnabled = loggedIn };
        logout.Click += async (_, _) =>
        {
            if (busy) return;
            busy = true;
            try { await client.Call("account/logout"); loggedIn = false; windows.Clear(); account = "Not signed in"; status = "You have signed out."; }
            catch { status = "Sign-out failed. Try again."; }
            finally { busy = false; Render(); }
        }; menu.Items.Add(logout);
        var exit = new MenuItem { Header = "Exit" }; exit.Click += (_, _) => Close(); menu.Items.Add(exit);
        menu.IsOpen = true;
    }
    private async Task Login()
    {
        if (busy) return;
        busy = true; Render();
        try
        {
            await client.Start();
            if (loginId != null) { await client.Call("account/login/cancel", new { loginId }); loginId = null; status = "Sign-in cancelled."; }
            else
            {
                var response = await client.Call("account/login/start", new { type = "chatgpt" });
                loginId = response.GetProperty("loginId").GetString();
                var url = new Uri(response.GetProperty("authUrl").GetString()!);
                if (url.Scheme != "https" || !(url.Host == "auth.openai.com" || url.Host == "chatgpt.com")) throw new InvalidOperationException();
                Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
                status = "Complete sign-in in your browser. Usage will update automatically.";
            }
        }
        catch { status = "Could not start sign-in. Close other sign-in windows and try again."; loginId = null; }
        finally { busy = false; Render(); }
    }
    private async Task Refresh(bool background = false)
    {
        if (busy) return;
        busy = true; if (!background) { status = "Refreshing usage…"; Render(); }
        try
        {
            await client.Start();
            var result = await client.Call("account/read", new { refreshToken = false });
            var user = result.GetProperty("account");
            loggedIn = user.ValueKind == JsonValueKind.Object && user.GetProperty("type").GetString() == "chatgpt";
            if (!loggedIn) { windows.Clear(); account = "Not signed in"; status = loginId == null ? "Sign in to view your remaining usage." : "Waiting for browser sign-in…"; return; }
            account = (user.TryGetProperty("email", out var email) ? email.GetString() : "ChatGPT") ?? "ChatGPT";
            windows = Usage.Parse(await client.Call("account/rateLimits/read"));
            status = windows.Count == 0 ? "No usage limits are available for this account." : $"Updated {DateTime.Now:HH:mm} · every 60 seconds";
        }
        catch { windows.Clear(); status = "Could not read usage. Check your connection, refresh or sign in again."; }
        finally { busy = false; Render(); }
    }
}
