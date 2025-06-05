using IC.EbbFeedWorkerService.Models;
using IC.EbbFeedWorkerService.Services;
using FluentAssertions;

namespace IC.EbbFeedWorkerService.Tests.Services
{
    public class TradingSignalDetectorTests
    {
        [Fact]
        public void DetectTradingSignals_ReturnsEmpty_WhenNoNotices()
        {
            var notices = new List<Notice>();

            var result = TradingSignalDetector.GetTradableNotices(notices);

            result.Should().BeEmpty();
        }

        [Fact]
        public void DetectTradingSignals_FiltersOutOldNotices()
        {
            var oldNotice = new Notice
            {
                Title = "Force Majeure Event",
                PostedDateTime = DateTime.UtcNow.AddDays(-5)
            };
            var recentNotice = new Notice
            {
                Title = "Outage at Henry Hub",
                PostedDateTime = DateTime.UtcNow.AddDays(-1)
            };

            var result = TradingSignalDetector.GetTradableNotices(new[] { oldNotice, recentNotice });

            result.Should().HaveCount(1);
            result.Should().Contain(recentNotice);
        }

        [Fact]
        public void DetectTradingSignals_DetectsKeywordInTitleOrSummary()
        {
            var notice = new Notice
            {
                Title = "Unexpected Curtailment",
                Summary = "Routine maintenance",
                PostedDateTime = DateTime.UtcNow
            };

            var result = TradingSignalDetector.GetTradableNotices(new[] { notice });

            result.Should().HaveCount(1);
            result.Should().Contain(notice);
        }

        [Fact]
        public void DetectTradingSignals_DetectsLouisianaKeywords()
        {
            var notice = new Notice
            {
                Title = "Routine Notice",
                Summary = "No issues",
                TspName = "Columbia Gulf Transmission",
                PostedDateTime = DateTime.UtcNow
            };

            var result = TradingSignalDetector.GetTradableNotices(new[] { notice });

            result.Should().HaveCount(1);
            result.Should().Contain(notice);
        }

        [Fact]
        public void DetectTradingSignals_ExtractsCurtailmentVolumes()
        {
            var notice = new Notice
            {
                Title = "Curtailment Notice",
                Summary = "Curtailment of 100,000 MMBtu and 50 MMcf/d expected.",
                PostedDateTime = DateTime.UtcNow
            };

            var result = TradingSignalDetector.GetTradableNotices(new[] { notice });

            result.Should().HaveCount(1);
            var detected = result.First();
            detected.CurtailmentVolumes.Should().NotBeNull();
            var volumes = detected.CurtailmentVolumes.ToString();
            volumes.Should().Contain("100,000 MMBtu");
            volumes.Should().Contain("50 MMcf/d");
        }
    }
}
