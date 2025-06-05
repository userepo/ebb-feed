using IC.EbbFeedWorkerService.Models;

namespace IC.EbbFeedWorkerService.Services;

public interface INoticeExtractor
{
    IList<Notice> ExtractNotices(string rawContent);
}
