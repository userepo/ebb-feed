using HtmlAgilityPack;
using IC.EbbFeedWorkerService.Models;

namespace IC.EbbFeedWorkerService.Services;


/// <summary>
/// Extracts a collection of notices from the provided raw HTML content from the ANR pipeline.
/// </summary>
/// <remarks>This method parses the HTML content of a table with a specific structure to extract notice
/// details. If the table or expected rows are not found, an empty list is returned.</remarks>
public class AnrNoticeExtractor : INoticeExtractor
{
    private const string BaseNoticeUrl = "https://ebb.anrpl.com/Notices/NoticeView.asp?sPipelineCode=ANR&sSubCategory=Critical&sNoticeId=";

    public IList<Notice> ExtractNotices(string rawContent)
    {
        var notices = new List<Notice>();
        var doc = new HtmlDocument();
        doc.LoadHtml(rawContent);

        var rows = doc.DocumentNode.SelectNodes("//table[@width='650' and @border='1' and @cellpadding='5' and @cellspacing='0' and @bordercolor='#000000' and @bordercolordark='#000000' and @bordercolorlight='#000000']");

        if (rows == null) return notices;

        foreach (var row in rows)
        {
            string? noticeType = row.SelectSingleNode(".//tr[td/small/strong[contains(text(), 'Notice Type Desc:')]]/td[2]/small")?.InnerText.Trim();

            var noticeTextTd = row.SelectSingleNode(".//tr[td/small/strong[text()='Notice Text:']]/td[2]//table//tr[2]/td");
            string noticeText = noticeTextTd?.InnerHtml ?? string.Empty;

            string title;
            string summary;
            if (noticeText.Contains("<br>", StringComparison.OrdinalIgnoreCase))
            {
                var parts = noticeText.Split(["<br>", "<br/>", "<br />"], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                title = parts[0].Trim();
                summary = parts.Length > 1
                    ? string.Join(Environment.NewLine, parts.Skip(1)).Trim()
                    : string.Empty;
            }
            else
            {
                // If no <br> tags, use the entire text as title and summary
                title = noticeText.Trim();
                summary = noticeText.Trim();
            }

            var postedDateTimeStr = row.SelectSingleNode(".//tr[td/small/strong[contains(text(), 'Posting Date/Time:')]]/td[2]/small")?.InnerText.Trim();
            if (!DateTime.TryParse(postedDateTimeStr, out var postedDateTime))
            {
                postedDateTime = DateTime.MinValue; // Default value if parsing fails
            }

            var effectiveDateTimeStr = row.SelectSingleNode(".//tr[td/small/strong[contains(text(), 'Notice Effective Date/Time:')]]/td[2]/small")?.InnerText.Trim();
            if (!DateTime.TryParse(effectiveDateTimeStr, out var effectiveDateTime))
            {
                effectiveDateTime = DateTime.MinValue; // Default value if parsing fails
            }

            var endDateTimeStr = row.SelectSingleNode(".//tr[td/small/strong[contains(text(), 'Notice End Date/Time:')]]/td[2]/small")?.InnerText.Trim();
            if (!DateTime.TryParse(endDateTimeStr, out var endDateTime))
            {
                endDateTime = DateTime.MinValue; // Default value if parsing fails
            }

            string? noticeId = row.SelectSingleNode(".//tr[td/small/strong[contains(text(), 'Notice ID:')]]/td[2]/small")?.InnerText.Trim();

            var noticeUrl = $"{BaseNoticeUrl}{noticeId}";

            var notice = new Notice
            {
                TspName = "ANR",
                NoticeType = noticeType ?? string.Empty,
                Title = title,
                Summary = summary,
                PostedDateTime = postedDateTime,
                EffectiveDateTime = effectiveDateTime,
                EndDateTime = endDateTime,
                SegmentLocation = string.Empty,
                NoticeUrl = noticeUrl
            };
            notices.Add(notice);
        }
        return notices;
    }
}
