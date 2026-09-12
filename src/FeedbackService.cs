using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CodexUsage;

public static class FeedbackService
{
    public const string FormResponseUrl = "https://docs.google.com/forms/d/e/1FAIpQLSeGYbO_GyfWLUlWgHFLtsZYHsN8dmxURbqzBNq8V8eg2Kd44g/formResponse";
    public const string FeedbackEntry = "entry.412582170";

    private static readonly HttpClient Http = CreateClient();

    public static string BuildDeviceInfo()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "unknown";
        var os = (RuntimeInformation.OSDescription ?? Environment.OSVersion.VersionString).Replace('\r', ' ').Replace('\n', ' ').Trim();
        return string.Join('\n',
            $"App version: {version}",
            $"OS: {os}",
            $"Architecture: {RuntimeInformation.OSArchitecture}",
            $"Runtime: {RuntimeInformation.FrameworkDescription}");
    }

    public static async Task SubmitAsync(string feedback, bool includeDeviceInfo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(feedback)) throw new ArgumentException("Feedback cannot be empty.", nameof(feedback));
        var response = feedback.Trim();
        if (includeDeviceInfo) response += $"\n\n---\nAutomatic device info:\n{BuildDeviceInfo()}";

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            [FeedbackEntry] = response,
            ["fvv"] = "1",
            ["pageHistory"] = "0"
        });
        using var result = await Http.PostAsync(FormResponseUrl, content, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccessStatusCode) throw new HttpRequestException($"Google Forms returned {(int)result.StatusCode}.");
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "unknown";
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"CodexUsageWidget/{version}");
        return client;
    }
}
