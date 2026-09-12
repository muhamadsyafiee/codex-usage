using System.Text.Json;
using CodexUsage;
static List<UsageWindow> Parse(string json) => Usage.Parse(JsonDocument.Parse(json).RootElement);
static void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("PASS " + name); }
var values = Parse("""{"rateLimitsByLimitId":{"codex":{"primary":{"usedPercent":25,"windowDurationMins":300,"resetsAt":1800000000},"secondary":{"usedPercent":105,"windowDurationMins":10080,"resetsAt":null}},"extra":{"primary":{"usedPercent":-5}}},"rateLimits":{"primary":{"usedPercent":99}}}""");
Check(values.Count == 3, "Prefer all named buckets over legacy");
Check(values[0].Remaining == 75 && values[0].Label == "5 jam", "Calculate remaining and duration");
Check(values[1].Remaining == 0 && values[2].Remaining == 100, "Clamp remaining bounds");
Check(values[1].Label == "Mingguan" && values[1].Reset == null, "Weekly and null reset");
Check(values[0].Reset?.ToUnixTimeSeconds() == 1800000000, "Reset uses seconds");
Check(Parse("""{"rateLimits":{"primary":{"usedPercent":null,"windowDurationMins":null,"resetsAt":null},"secondary":null}}""")[0].Remaining == null, "Unknown is not zero");
Check(Parse("""{"rateLimits":null}""").Count == 0, "Absent limits");
Check(Parse("""{"rateLimitsByLimitId":{},"rateLimits":{"primary":{"usedPercent":60}}}""")[0].Remaining == 40, "Empty map legacy fallback");
foreach (var edge in new[] { "Kiri", "Kanan", "Atas", "Bawah" })
{
    var vertical = edge is "Kiri" or "Kanan";
    var smallWidth = vertical ? 32 : 112;
    var smallHeight = vertical ? 112 : 32;
    var small = DockGeometry.Place(edge, -1920, 40, 1920, 1040, smallWidth, smallHeight);
    var large = DockGeometry.Place(edge, -1920, 40, 1920, 1040, 400, 600);
    Check(small.Left >= large.Left && small.Top >= large.Top && small.Left + smallWidth <= large.Left + 400 && small.Top + smallHeight <= large.Top + 600, edge + " hover handle remains inside expanded panel");
    Check(large.Left >= -1920 && large.Top >= 40 && large.Left + 400 <= 0 && large.Top + 600 <= 1080, edge + " stays in monitor work area");
}
Check(DockGeometry.Place("Bawah", 0, 0, 1920, 1040, 400, 600).Top == 440, "Bottom dock respects taskbar work area");
