using System.Text.Json;
namespace CodexUsage;
public record UsageWindow(string Bucket, string Label, double? Remaining, DateTimeOffset? Reset, string BucketId = "");
public static class Usage
{
    public static List<UsageWindow> Parse(JsonElement root)
    {
        var result = new List<UsageWindow>();
        if (root.TryGetProperty("rateLimitsByLimitId", out var map) && map.ValueKind == JsonValueKind.Object && map.EnumerateObject().Any())
            foreach (var bucket in map.EnumerateObject()) Add(bucket.Name, bucket.Value, result);
        else if (root.TryGetProperty("rateLimits", out var legacy) && legacy.ValueKind == JsonValueKind.Object) Add("codex", legacy, result);
        return result.OrderBy(w => w.BucketId.Equals("codex", StringComparison.OrdinalIgnoreCase) ? 0 : (w.BucketId + w.Bucket).Contains("reserve", StringComparison.OrdinalIgnoreCase) ? 1 : 2)
            .ThenBy(w => w.BucketId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(w => w.Label == "5 hours" ? 0 : w.Label == "Weekly" ? 1 : 2).ToList();
    }
    private static void Add(string name, JsonElement bucket, List<UsageWindow> result)
    {
        var id = name;
        if (bucket.TryGetProperty("limitName", out var title) && title.ValueKind == JsonValueKind.String) name = title.GetString()!;
        foreach (var key in new[] { "primary", "secondary" })
        {
            if (!bucket.TryGetProperty(key, out var window) || window.ValueKind != JsonValueKind.Object) continue;
            double? remaining = window.TryGetProperty("usedPercent", out var used) && used.ValueKind == JsonValueKind.Number && used.TryGetDouble(out var value) ? Math.Clamp(100 - value, 0, 100) : null;
            var label = key == "primary" ? "Primary" : "Secondary";
            if (window.TryGetProperty("windowDurationMins", out var duration) && duration.ValueKind == JsonValueKind.Number && duration.TryGetInt32(out var minutes)) label = minutes == 10080 ? "Weekly" : minutes >= 60 ? $"{minutes / 60.0:0.#} hours" : $"{minutes} minutes";
            DateTimeOffset? reset = window.TryGetProperty("resetsAt", out var stamp) && stamp.ValueKind == JsonValueKind.Number && stamp.TryGetInt64(out var seconds) ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null;
            result.Add(new(name, label, remaining, reset, id));
        }
    }
}
