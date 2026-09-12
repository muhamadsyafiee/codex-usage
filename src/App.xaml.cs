using System.Windows;
namespace CodexUsage;
public partial class App : System.Windows.Application
{
    private Mutex? mutex;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        mutex = new Mutex(true, "Local\\CodexUsageWidget", out var first);
        if (!first) { Shutdown(); return; }
        var widget = new Widget();
        widget.Show();
        if (e.Args.Contains("--after-update")) widget.Reveal();
    }
    protected override void OnExit(ExitEventArgs e) { mutex?.Dispose(); base.OnExit(e); }
}
