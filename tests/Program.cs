using System.Text.Json;
using CodexUsage;
static List<UsageWindow> Parse(string json) => Usage.Parse(JsonDocument.Parse(json).RootElement);
static void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("PASS " + name); }
var values = Parse("""{"rateLimitsByLimitId":{"codex":{"primary":{"usedPercent":25,"windowDurationMins":300,"resetsAt":1800000000},"secondary":{"usedPercent":105,"windowDurationMins":10080,"resetsAt":null}},"extra":{"primary":{"usedPercent":-5}}},"rateLimits":{"primary":{"usedPercent":99}}}""");
Check(values.Count == 3, "Prefer all named buckets over legacy");
Check(values[0].Remaining == 75 && values[0].Label == "5 hours", "Calculate remaining and duration");
Check(values[1].Remaining == 0 && values[2].Remaining == 100, "Clamp remaining bounds");
Check(values[1].Label == "Weekly" && values[1].Reset == null, "Weekly and null reset");
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
foreach (var offset in new[] { 0.0, 0.1, 0.8, 1.0 })
foreach (var edge in new[] { "Kiri", "Kanan", "Atas", "Bawah" })
{
    var vertical = edge is "Kiri" or "Kanan";
    var small = DockGeometry.Place(edge, 0, 0, 1920, 1040, vertical ? 32 : 112, vertical ? 112 : 32, offset);
    var large = DockGeometry.Place(edge, 0, 0, 1920, 1040, 400, 600, offset);
    Check(small.Left >= large.Left && small.Top >= large.Top && small.Left + (vertical ? 32 : 112) <= large.Left + 400 && small.Top + (vertical ? 112 : 32) <= large.Top + 600, $"{edge} offset {offset} keeps hover inside panel");
}
var release = """{"draft":false,"prerelease":false,"tag_name":"v1.3.0","assets":[{"name":"CodexUsageWidget-1.3.0-x64.msi","browser_download_url":"https://github.com/muhamadsyafiee/codex-usage/releases/download/v1.3.0/app.msi"},{"name":"SHA256-1.3.0.txt","browser_download_url":"https://github.com/muhamadsyafiee/codex-usage/releases/download/v1.3.0/hash.txt"}]}""";
ReleaseUpdate? ReadRelease(string json, string current) => UpdateService.Parse(JsonDocument.Parse(json).RootElement, Version.Parse(current));
Check(ReadRelease(release, "1.2.0")?.Version == new Version(1,3,0), "Detect newer release");
Check(ReadRelease(release, "1.3.0") == null && ReadRelease(release, "2.0.0") == null, "Never install same or older release");
Check(ReadRelease(release.Replace("\"prerelease\":false", "\"prerelease\":true"), "1.2.0") == null, "Ignore prerelease");
Check(ReadRelease(release.Replace("SHA256-1.3.0.txt", "missing.txt"), "1.2.0") == null, "Require checksum asset");
var rejected = false;
try { ReadRelease(release.Replace("https://github.com/", "https://example.com/"), "1.2.0"); } catch (System.IO.IOException) { rejected = true; }
Check(rejected, "Reject assets outside configured repository");
var reordered = Parse("""{"rateLimitsByLimitId":{"z":{"limitName":"Other","primary":{"usedPercent":1}},"gpt_reserve":{"limitName":"GPT reserve","primary":{"usedPercent":2}},"codex":{"primary":{"usedPercent":3,"windowDurationMins":300},"secondary":{"usedPercent":4,"windowDurationMins":10080}}}}""");
Check(reordered.Select(w => w.BucketId).SequenceEqual(new[] { "codex", "codex", "gpt_reserve", "z" }), "Codex five-hour and weekly precede GPT reserve regardless of response order");
Check(reordered[0].Label == "5 hours" && reordered[1].Label == "Weekly", "English duration labels");
Check(FeedbackService.FormResponseUrl.Contains("docs.google.com/forms/d/e/") && FeedbackService.FeedbackEntry == "entry.412582170", "Feedback form endpoint is configured");
Check(FeedbackService.BuildDeviceInfo().Contains("Architecture:") && FeedbackService.BuildDeviceInfo().Contains("App version:"), "Feedback device info is minimal and labeled");
var checksum = new string('A', 64);
Check(UpdateService.ParseChecksum($"{checksum}  CodexUsageWidget-1.3.4-x64.msi") == checksum, "Accept standard checksum manifest");
