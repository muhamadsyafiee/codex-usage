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
    private readonly StackPanel body = new();
    private readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexUsage", "settings.json");
    private Preferences settings = new();
    private List<UsageWindow> windows = [];
    private string account = "Belum log masuk", status = "Log masuk untuk melihat baki usage.";
    private bool loggedIn, busy;
    private string? loginId;
    private bool expanded, menuOpen;
    private readonly DispatcherTimer collapseTimer = new() { Interval = TimeSpan.FromMilliseconds(450) };
    private bool Docked => settings.Dock != "Bebas";
    private Rect dockArea;
    private bool dragging, checkingUpdate, installing;
    private System.Windows.Point dragStart;
    private double dragOffset;
    private readonly DispatcherTimer updateTimer = new() { Interval = TimeSpan.FromHours(6) };
    private readonly UpdateService updates = new();
    private ReleaseUpdate? availableUpdate;
    public Widget()
    {
        try { if (File.Exists(settingsPath)) settings = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(settingsPath)) ?? new(); } catch { }
        Title = "Codex Usage"; WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
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
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape && Docked) { expanded = false; Render(); e.Handled = true; } };
        MouseRightButtonUp += (_, _) => ShowMenu();
        SizeChanged += (_, _) => PositionDock();
        tray = new System.Windows.Forms.NotifyIcon { Icon = System.Drawing.SystemIcons.Application, Text = "Codex Usage", Visible = true };
        tray.DoubleClick += (_, _) => Dispatcher.Invoke(() => { Show(); Activate(); });
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Buka widget", null, (_, _) => Dispatcher.Invoke(() => { Show(); Activate(); }));
        menu.Items.Add("Semak update", null, (_, _) => Dispatcher.Invoke(async () => await CheckUpdate(true)));
        tray.BalloonTipClicked += (_, _) => Dispatcher.Invoke(() => { Show(); expanded = true; Render(); Activate(); });
        menu.Items.Add("Keluar", null, (_, _) => Dispatcher.Invoke(Close)); tray.ContextMenuStrip = menu;
        timer.Tick += async (_, _) => await Refresh();
        client.Notification += (method, data) => Dispatcher.BeginInvoke(async () =>
        {
            if (method == "account/login/completed")
            {
                loginId = null;
                if (data.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True) await Refresh();
                else { status = "Login tidak berjaya. Cuba semula."; Render(); }
            }
        });
        updateTimer.Tick += async (_, _) => await CheckUpdate(false);
        Loaded += async (_, _) => { UpdateDockArea(); PositionDock(); await Refresh(); timer.Start(); updateTimer.Start(); await CheckUpdate(false); };
        Closed += (_, _) => { timer.Stop(); updateTimer.Stop(); collapseTimer.Stop(); Save(); tray.Dispose(); client.Dispose(); System.Windows.Application.Current.Shutdown(); };
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
        var button = new Button { Content = label, Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 3, 6, 3), Background = Background, Foreground = Foreground, BorderBrush = Foreground, BorderThickness = new Thickness(1), IsEnabled = !busy };
        button.Click += async (_, _) => { try { await action(); } catch { status = "Operasi gagal. Cuba semula atau log masuk semula."; Render(); } };
        return button;
    }
    private void Render()
    {
        Background = settings.Light ? Brushes.White : Brushes.Black; Foreground = settings.Light ? Brushes.Black : Brushes.White;
        Opacity = double.IsFinite(settings.WidgetOpacity) ? Math.Clamp(settings.WidgetOpacity, 0.2, 1) : 0.7;
        var frame = (Border)Content;
        frame.Padding = new Thickness(22);
        ((ScrollViewer)frame.Child).VerticalScrollBarVisibility = Docked && !expanded ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
        MaxHeight = Docked && dockArea.Height > 0 ? dockArea.Height : double.PositiveInfinity;
        SizeToContent = SizeToContent.Height;
        Height = double.NaN;
        Topmost = Docked || settings.Topmost;
        Width = settings.Layout == "Kompak" ? 320 : 400;
        body.Children.Clear();
        if (Docked && !expanded)
        {
            var vertical = settings.Dock is "Kiri" or "Kanan";
            SizeToContent = SizeToContent.Manual;
            Width = vertical ? 32 : 112; Height = vertical ? 112 : 32;
            frame.Padding = new Thickness(0);
            body.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            body.VerticalAlignment = VerticalAlignment.Center;
            var handle = Text(availableUpdate == null ? "◈ CODEX" : "↑ UPDATE", 11);
            handle.MouseLeftButtonDown += BeginDockDrag;
            handle.Margin = new Thickness(0);
            if (vertical) handle.LayoutTransform = new RotateTransform(-90);
            body.Children.Add(handle);
            PositionDock();
            return;
        }
        body.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
        body.VerticalAlignment = VerticalAlignment.Top;
        var header = Text("◈   CODEX / USAGE", 13); header.FontWeight = FontWeights.SemiBold; header.Cursor = System.Windows.Input.Cursors.SizeAll;
        header.ToolTip = "Seret untuk pindahkan widget · klik kanan untuk tetapan"; header.MouseLeftButtonDown += (_, e) => { if (Docked) BeginDockDrag(header, e); else if (e.ButtonState == MouseButtonState.Pressed) { DragMove(); Save(); } }; body.Children.Add(header);
        if (settings.Layout != "Kompak") body.Children.Add(Text(account));
        if (!loggedIn)
        {
            body.Children.Add(Text("Usage anda.\nSekilas pandang.", 28));
            body.Children.Add(Text("Sambungkan akaun ChatGPT melalui login rasmi. Kata laluan dimasukkan dalam browser."));
        }
        foreach (var item in windows)
        {
            var card = new StackPanel { Margin = new Thickness(0, 10, 0, 10) };
            card.Children.Add(Text($"{item.Bucket.ToUpperInvariant()}  /  {item.Label}"));
            card.Children.Add(Text(item.Remaining is { } n ? $"{n:0.#}% baki" : "Tidak tersedia", settings.Layout == "Kompak" ? 23 : 38));
            var track = new Grid { Height = 5, Background = settings.Light ? Brushes.LightGray : Brushes.DimGray };
            if (item.Remaining is { } percent)
            {
                track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(percent, GridUnitType.Star) });
                track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100 - percent, GridUnitType.Star) });
                track.Children.Add(new Border { Background = Foreground });
            }
            card.Children.Add(track);
            if (settings.Layout == "Terperinci") card.Children.Add(Text(item.Reset is { } reset ? $"Reset {reset.ToLocalTime():ddd, dd MMM · HH:mm}" : "Masa reset tidak tersedia"));
            body.Children.Add(card);
        }
        body.Children.Add(Text(status, 11));
        if (availableUpdate != null) body.Children.Add(Action(installing ? "Memuat turun update…" : $"Update v{availableUpdate.Version}", InstallUpdate));
        var controls = new WrapPanel();
        if (!loggedIn) controls.Children.Add(Action(loginId == null ? "Log masuk" : "Batal login", Login));
        controls.Children.Add(Action("Refresh", Refresh));
        controls.Children.Add(Action("···", () => { ShowMenu(); return Task.CompletedTask; }));
        controls.Children.Add(Action("Sorok", () => { Hide(); return Task.CompletedTask; })); body.Children.Add(controls);
        PositionDock();
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
        Show(); expanded = true; Render(); Activate();
    }
    private async Task CheckUpdate(bool manual)
    {
        if (checkingUpdate || installing) return;
        checkingUpdate = true;
        try
        {
            var found = await updates.Check();
            var notify = found != null && found.Version != availableUpdate?.Version;
            availableUpdate = found;
            if (notify) tray.ShowBalloonTip(8000, "Update Codex Usage", $"Versi {found!.Version} tersedia. Klik untuk buka widget.", System.Windows.Forms.ToolTipIcon.Info);
            if (manual) { status = found == null ? "Anda menggunakan versi terkini." : $"Versi {found.Version} tersedia."; Reveal(); }
            else Render();
        }
        catch { if (manual) { status = "Semakan update gagal. Semak internet dan cuba semula."; Reveal(); } }
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
            _ = Process.Start(start) ?? throw new IOException("Installer gagal dimulakan.");
            Close();
        }
        catch { status = "Update gagal. Cuba semula melalui menu update."; }
        finally { installing = false; if (IsLoaded) Render(); }
    }
    private void ShowMenu()
    {
        var menu = new ContextMenu();
        menuOpen = true;
        menu.Closed += (_, _) => { menuOpen = false; collapseTimer.Start(); };
        var docking = new MenuItem { Header = "Dock skrin" };
        foreach (var edge in new[] { "Bebas", "Kiri", "Kanan", "Atas", "Bawah" })
        {
            var option = new MenuItem { Header = edge, IsCheckable = true, IsChecked = settings.Dock == edge };
            option.Click += (_, _) => SetDock(edge);
            docking.Items.Add(option);
        }
        menu.Items.Add(docking);
        var opacity = new MenuItem { Header = "Opacity" };
        var slider = new Slider { Minimum = 20, Maximum = 100, Value = Opacity * 100, TickFrequency = 1, IsSnapToTickEnabled = true, Width = 180, Margin = new Thickness(12), ToolTip = $"{Opacity:P0}" };
        slider.ValueChanged += (_, _) => { settings.WidgetOpacity = slider.Value / 100; Opacity = settings.WidgetOpacity; slider.ToolTip = $"{slider.Value:0}%"; Save(); };
        opacity.Items.Add(new MenuItem { Header = slider, StaysOpenOnClick = true }); menu.Items.Add(opacity);
        var update = new MenuItem { Header = availableUpdate == null ? "Semak update" : $"Update v{availableUpdate.Version}", IsEnabled = !installing };
        update.Click += async (_, _) => { if (availableUpdate == null) await CheckUpdate(true); else await InstallUpdate(); }; menu.Items.Add(update);
        foreach (var layout in new[] { "Kompak", "Kad", "Terperinci" })
        {
            var item = new MenuItem { Header = layout, IsCheckable = true, IsChecked = settings.Layout == layout };
            item.Click += (_, _) => { settings.Layout = layout; Save(); Render(); }; menu.Items.Add(item);
        }
        var theme = new MenuItem { Header = "Tema putih", IsCheckable = true, IsChecked = settings.Light };
        theme.Click += (_, _) => { settings.Light = !settings.Light; Save(); Render(); }; menu.Items.Add(theme);
        var pin = new MenuItem { Header = "Sentiasa di atas", IsCheckable = true, IsChecked = Topmost, IsEnabled = !Docked };
        pin.Click += (_, _) => { settings.Topmost = Topmost = !Topmost; Save(); }; menu.Items.Add(pin);
        var logout = new MenuItem { Header = "Log keluar", IsEnabled = loggedIn };
        logout.Click += async (_, _) =>
        {
            if (busy) return;
            busy = true;
            try { await client.Call("account/logout"); loggedIn = false; windows.Clear(); account = "Belum log masuk"; status = "Anda telah log keluar."; }
            catch { status = "Log keluar gagal. Cuba semula."; }
            finally { busy = false; Render(); }
        }; menu.Items.Add(logout);
        var exit = new MenuItem { Header = "Keluar" }; exit.Click += (_, _) => Close(); menu.Items.Add(exit);
        menu.IsOpen = true;
    }
    private async Task Login()
    {
        if (busy) return;
        busy = true; Render();
        try
        {
            await client.Start();
            if (loginId != null) { await client.Call("account/login/cancel", new { loginId }); loginId = null; status = "Login dibatalkan."; }
            else
            {
                var response = await client.Call("account/login/start", new { type = "chatgpt" });
                loginId = response.GetProperty("loginId").GetString();
                var url = new Uri(response.GetProperty("authUrl").GetString()!);
                if (url.Scheme != "https" || !(url.Host == "auth.openai.com" || url.Host == "chatgpt.com")) throw new InvalidOperationException();
                Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
                status = "Lengkapkan login dalam browser. Widget akan dikemas kini.";
            }
        }
        catch { status = "Login gagal dimulakan. Tutup sesi login lain dan cuba semula."; loginId = null; }
        finally { busy = false; Render(); }
    }
    private async Task Refresh()
    {
        if (busy) return;
        busy = true; Render();
        try
        {
            await client.Start();
            var result = await client.Call("account/read", new { refreshToken = false });
            var user = result.GetProperty("account");
            loggedIn = user.ValueKind == JsonValueKind.Object && user.GetProperty("type").GetString() == "chatgpt";
            if (!loggedIn) { windows.Clear(); account = "Belum log masuk"; status = loginId == null ? "Log masuk untuk melihat baki usage." : "Menunggu login dalam browser…"; return; }
            account = (user.TryGetProperty("email", out var email) ? email.GetString() : "ChatGPT") ?? "ChatGPT";
            windows = Usage.Parse(await client.Call("account/rateLimits/read"));
            status = windows.Count == 0 ? "Tiada data had tersedia untuk akaun ini." : $"Dikemas kini {DateTime.Now:HH:mm} · setiap 60 saat";
        }
        catch { windows.Clear(); status = "Usage tidak dapat dibaca. Semak internet, refresh atau log masuk semula."; }
        finally { busy = false; Render(); }
    }
}
