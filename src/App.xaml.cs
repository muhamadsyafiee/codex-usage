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
        new Widget().Show();
    }
    protected override void OnExit(ExitEventArgs e) { mutex?.Dispose(); base.OnExit(e); }
}
