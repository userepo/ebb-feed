using IC.EbbFeedWorkerService.Models;
using IC.EbbFeedWorkerService.Services;
using Polly;

namespace IC.EbbFeedWorkerService;

public class EbbFeedWorker : BackgroundService
{
    private readonly ILogger<EbbFeedWorker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ISlackMessageSender _slackMessageSender;
    private readonly string _slackWebhookUrl; 
    private readonly int _intervalMinutes;
    private readonly AsyncPolicy<HttpResponseMessage> _retryPolicy;

    public EbbFeedWorker(ILogger<EbbFeedWorker> logger, 
        IHttpClientFactory httpClientFactory, 
        IConfiguration config,
        ISlackMessageSender slackMessageSender)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _config = config;

        _slackMessageSender = slackMessageSender ?? throw new ArgumentNullException(nameof(slackMessageSender), "Slack message sender cannot be null.");
        _slackWebhookUrl = config.GetValue<string>("EbbFeedWorkerSettings:SlackWebhookUrl") ?? throw new ArgumentNullException(nameof(config), "Slack webhook URL cannot be null or empty.");

        _intervalMinutes = config.GetValue<int>("EbbFeedWorkerSettings:IntervalMinutes", 15);

        _retryPolicy = Polly.Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    _logger.LogWarning("Retry {RetryAttempt} for {EbbFeedUrl} due to {Reason}. Waiting {Delay} before next retry.",
                        retryAttempt, 
                        config.GetValue<string>("EbbFeedWorkerSettings:EbbFeedUrl"), 
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString(), 
                        timespan);
                });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                Console.WriteLine("Checking ANR pipeline...");

                try
                {
                    await CheckAnrPipelineAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking ANR pipeline.");
                }

                await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Handle cancellation gracefully
            _logger.LogInformation("EbbFeedWorker has been cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred in EbbFeedWorker.");

            Environment.Exit(1);
        }
    }

    private async Task CheckAnrPipelineAsync()
    {
        try
        {
            var ebbFeedUrl = _config.GetValue<string>("EbbFeedWorkerSettings:EbbFeedUrl")
                ?? throw new ArgumentNullException(nameof(_config), "EbbFeedUrl setting cannot be null or empty.");

            _logger.LogInformation("Fetching ANR pipeline feed from: {EbbFeedUrl}", ebbFeedUrl);

            var client = _httpClientFactory.CreateClient();

            var response = await _retryPolicy.ExecuteAsync(() => client.GetAsync(ebbFeedUrl));
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine("ANR pipeline feed fetched successfully.");

                var extractor = new AnrNoticeExtractor();
                var notices = extractor.ExtractNotices(content);

                Console.WriteLine($"Found {notices.Count} notice(s).");
                _logger.LogInformation("Found {NoticeCount} notices at {Time}", notices.Count, DateTime.UtcNow);

                if (notices.Count == 0) return;              

                var detector = new TradingSignalDetector();

                List<Notice> tradableNotices = TradingSignalDetector.GetTradableNotices(notices);
                Console.WriteLine($"{tradableNotices.Count} trading signal(s) detected.");

                // TODO: remove - added for debugging
                /*
                foreach (var notice in tradableNotices)
                {
                    Console.WriteLine($"NoticeType: {notice.NoticeType}");
                    Console.WriteLine($"Title: {notice.Title}");
                    Console.WriteLine($"Summary: {notice.Summary}");
                    Console.WriteLine($"Posted DateTime: {notice.PostedDateTime}");
                    Console.WriteLine($"Effective DateTime: {notice.EffectiveDateTime}");
                    Console.WriteLine($"End DateTime: {notice.EndDateTime}");
                    Console.WriteLine($"FullNoticeUrl: {notice.NoticeUrl}");

                    Console.WriteLine("--------------");
                }
                */

                //Send Slack notifications for notices with trading signals
                var tasks = tradableNotices.Select(async notice =>
                {
                    try
                    {
                        bool sent = await _slackMessageSender.SendMessageAsync(_slackWebhookUrl, notice);
                        if (sent)
                        {
                            _logger.LogInformation("Slack message sent for notice: {Title}", notice.Title);
                            Console.WriteLine("Slack message sent.");
                        }
                        else
                        {
                            _logger.LogWarning("Failed to send Slack message for notice: {Title}", notice.Title);
                            Console.WriteLine("Slack message failed to be sent.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending Slack message for notice: {Title}", notice.Title);
                    }
                });

                await Task.WhenAll(tasks);
            }
            else
            {
                Console.WriteLine($"Failed to fetch ANR pipeline feed. Status code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking ANR pipeline feed.");
        }
    }

}
