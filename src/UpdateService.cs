using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
namespace CodexUsage;
public record ReleaseUpdate(Version Version, string InstallerUrl, string ChecksumUrl);
public sealed class UpdateService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(10) };
    public static readonly Version Current = typeof(UpdateService).Assembly.GetName().Version ?? new Version(1, 2, 0);
    public static ReleaseUpdate? Parse(JsonElement release, Version current)
    {
        if (release.GetProperty("draft").GetBoolean() || release.GetProperty("prerelease").GetBoolean()) return null;
        var tag = release.GetProperty("tag_name").GetString()!;
        if (!Version.TryParse(tag.TrimStart('v'), out var version) || version <= current) return null;
        var assets = release.GetProperty("assets").EnumerateArray().ToArray();
        string? Find(string name) => assets.FirstOrDefault(a => a.GetProperty("name").GetString() == name) is var a && a.ValueKind == JsonValueKind.Object ? a.GetProperty("browser_download_url").GetString() : null;
        var installer = Find($"CodexUsageWidget-{version}-x64.msi");
        var checksum = Find($"SHA256-{version}.txt");
        if (installer == null || checksum == null) return null;
        var prefix = $"https://github.com/muhamadsyafiee/codex-usage/releases/download/{tag}/";
        if (!installer.StartsWith(prefix, StringComparison.Ordinal) || !checksum.StartsWith(prefix, StringComparison.Ordinal)) throw new IOException("Unexpected release source.");
        return new(version, installer, checksum);
    }
    public async Task<ReleaseUpdate?> Check()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/muhamadsyafiee/codex-usage/releases/latest");
        request.Headers.UserAgent.ParseAdd($"CodexUsageWidget/{Current}");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var response = await Http.SendAsync(request, timeout.Token);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
        return Parse(json.RootElement, Current);
    }
    public async Task<string> Download(ReleaseUpdate update)
    {
        var hash = ParseChecksum(await Http.GetStringAsync(update.ChecksumUrl));
        if (hash.Length != 64 || !hash.All(Uri.IsHexDigit)) throw new IOException("Invalid checksum.");
        var directory = Path.Combine(Path.GetTempPath(), "CodexUsageUpdates", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory, $"CodexUsageWidget-{update.Version}-x64.msi");
        try
        {
            using var response = await Http.GetAsync(update.InstallerUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            await using (var output = File.Create(file)) await response.Content.CopyToAsync(output);
            await using var input = File.OpenRead(file);
            var actual = Convert.ToHexString(await SHA256.HashDataAsync(input));
            if (!actual.Equals(hash, StringComparison.OrdinalIgnoreCase)) throw new IOException("Checksum mismatch.");
            return file;
        }
        catch { File.Delete(file); throw; }
    }
    internal static string ParseChecksum(string content) => content
        .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
        .FirstOrDefault()?.Trim('\uFEFF') ?? string.Empty;
}
