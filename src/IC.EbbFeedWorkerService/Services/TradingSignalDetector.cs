using IC.EbbFeedWorkerService.Models;

namespace IC.EbbFeedWorkerService.Services;

/// <summary>
/// Detects trading signals from a list of pipeline notices.
/// </summary>
public class TradingSignalDetector
{
    // TODO: we can make these easier to change by moving them to a config file
    private static readonly string[] _keywords = { "force majeure", "outage", "curtailment" };
    private static readonly string[] _louisianaKeywords = { "louisiana", "henry hub", "sabine", "cameron", "columbia gulf", "transco", "texas eastern", "gulf south", "enable", "gulfstream" };
    private static readonly double _noticeAgeLimitDays = 3;

    private static readonly System.Text.RegularExpressions.Regex _curtailmentVolumeRegex =
        new System.Text.RegularExpressions.Regex(
            @"(\d{1,3}(?:,\d{3})*(?:\.\d+)?)[ ]*(mmbtu|dth|mmcf/d)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled,
            TimeSpan.FromSeconds(5));

    /// <summary>
    /// Analyzes the provided notices and returns those that are considered trading signals.
    /// </summary>
    /// <param name="notices">The list of notices to analyze.</param>
    /// <returns>A list of notices with trading signals.</returns>
    public static List<Notice> GetTradableNotices(IList<Notice> notices)
    {
        var tradableNotices = new List<Notice>();

        var utcNow = DateTimeOffset.UtcNow;

        foreach (var notice in notices)
        {
            // Check if notice is within the last 3 days
            if ((utcNow - notice.PostedDateTime.ToUniversalTime()).TotalDays > _noticeAgeLimitDays) continue;

            // Check for keywords in title or summary
            var title = notice.Title?.ToLowerInvariant() ?? string.Empty;
            var summary = notice.Summary?.ToLowerInvariant() ?? string.Empty;
            bool hasKeyword = _keywords.Any(k => title.Contains(k) || summary.Contains(k));

            // Check for Louisiana or Henry Hub–linked pipelines
            var tsp = notice.TspName?.ToLowerInvariant() ?? string.Empty;
            var segment = notice.SegmentLocation?.ToLowerInvariant() ?? string.Empty;
            bool mentionsLouisiana = _louisianaKeywords.Any(k =>
                tsp.Contains(k) || title.Contains(k) || summary.Contains(k) || segment.Contains(k));

            if (hasKeyword || mentionsLouisiana)
            {
                // Extract curtailment volume if available (e.g., mmbtu/Dth, or MMcf/d)
                var volumeMatches = _curtailmentVolumeRegex.Matches(notice.Summary ?? "");

                if (volumeMatches.Count > 0)
                {
                    var volumes = volumeMatches
                        .Select(m => m.Value.Trim())
                        .Where(v => !string.IsNullOrEmpty(v));
                    notice.CurtailmentVolumes = string.Join("; ", volumes);
                }

                tradableNotices.Add(notice);
            }
        }

        return tradableNotices;
    }
}