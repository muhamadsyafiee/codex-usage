using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
namespace CodexUsage;
public sealed class CodexClient : IDisposable
{
    private Process? process;
    private int sequence;
    private readonly SemaphoreSlim writer = new(1);
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement>> pending = new();
    public event Action<string, JsonElement>? Notification;
    public static string Home => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexUsage", "session");
    public async Task Start()
    {
        if (process is { HasExited: false }) return;
        Directory.CreateDirectory(Home);
        var executable = Path.Combine(AppContext.BaseDirectory, "codex.exe");
        var info = new ProcessStartInfo(executable, "app-server") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        info.Environment["CODEX_HOME"] = Home;
        process = Process.Start(info) ?? throw new IOException("Codex gagal dimulakan.");
        _ = process.StandardError.ReadToEndAsync();
        _ = Read(process);
        await Call("initialize", new { clientInfo = new { name = "codex_usage_widget", version = "1.0.0" } });
        await Send(new { method = "initialized", @params = new { } });
    }
    private async Task Read(Process source)
    {
        try
        {
            while (await source.StandardOutput.ReadLineAsync() is { } line)
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                if (root.TryGetProperty("id", out var id) && id.TryGetInt32(out var number) && pending.TryRemove(number, out var completion))
                {
                    if (root.TryGetProperty("error", out var error)) completion.TrySetException(new IOException(error.GetProperty("message").GetString()));
                    else completion.TrySetResult(root.GetProperty("result").Clone());
                }
                else if (root.TryGetProperty("method", out var method)) Notification?.Invoke(method.GetString()!, root.TryGetProperty("params", out var p) ? p.Clone() : default);
            }
        }
        catch (Exception) { /* Pending callers receive a safe connection error below. */ }
        finally
        {
            foreach (var pair in pending) if (pending.TryRemove(pair.Key, out var item)) item.TrySetException(new IOException("Sambungan Codex terputus. Cuba refresh."));
        }
    }
    private async Task Send(object value)
    {
        await writer.WaitAsync();
        try { await process!.StandardInput.WriteLineAsync(JsonSerializer.Serialize(value)); await process.StandardInput.FlushAsync(); }
        finally { writer.Release(); }
    }
    public async Task<JsonElement> Call(string method, object? parameters = null)
    {
        var id = Interlocked.Increment(ref sequence);
        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        pending[id] = completion;
        try { await Send(new { id, method, @params = parameters ?? new { } }); return await completion.Task.WaitAsync(TimeSpan.FromSeconds(30)); }
        finally { pending.TryRemove(id, out _); }
    }
    public void Dispose()
    {
        if (process is { HasExited: false }) process.Kill(true);
        process?.Dispose();
    }
}
