using System.Text;
using System.Text.Json;
using IC.EbbFeedWorkerService.Models;
using Polly;

namespace IC.EbbFeedWorkerService.Services;

public class SlackMessageSender : ISlackMessageSender
{
    private readonly ILogger<EbbFeedWorker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    // A Polly retry policy that handles HttpRequestException or any non-successful HTTP response 
    private readonly Polly.Retry.AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public SlackMessageSender(ILogger<EbbFeedWorker> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;

        _retryPolicy = Policy
        .Handle<HttpRequestException>() // handle network-related exceptions 
        .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode) // or non-success responses 
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // exponential backoff 
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                string errorMsg = outcome.Exception != null
                    ? outcome.Exception.Message
                    : $"HTTP {(int)outcome.Result.StatusCode} {outcome.Result.ReasonPhrase}";

                _logger.LogWarning("Attempt {RetryAttempt}: Failed to send Slack message. Waiting ({Delay}) before next retry. Error: {Error}",
                    retryAttempt, timespan, errorMsg);
            });
    }

    /// <inheritdoc />
    public async Task<bool> SendMessageAsync(string webhookUrl, Notice notice)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _logger.LogWarning("Slack webhook URL is not configured. Skipping message sending.");
            return false;
        }
        if (notice == null)
        {
            _logger.LogWarning("Notice is null. Skipping message sending.");
            return false;
        }

        var payload = new
        {
            text = $"*{notice.NoticeType}* - {notice.Title}\n" +
                   $"*TSP:* {notice.TspName}\n" +
                   $"*Summary:* {notice.Summary}\n" +
                   $"*Posted:* {notice.PostedDateTime:yyyy-MM-dd HH:mm}\n" +
                   $"*Effective:* {notice.EffectiveDateTime:yyyy-MM-dd HH:mm}\n" +
                   $"*Ends:* {notice.EndDateTime:yyyy-MM-dd HH:mm}\n" +
                   $"*Segment/Location:* {notice.SegmentLocation}\n" +
                   $"< {notice.NoticeUrl} | View Notice >"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        var client = _httpClientFactory.CreateClient();
        try
        {
            var response = await _retryPolicy.ExecuteAsync(() => client.PostAsync(webhookUrl, content));
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Slack message sent successfully.");
            }
            else
            {
                // Log details if the final HTTP response is not successful. 
                _logger.LogError("Failed to send Slack message. Response: {StatusCode} - {ReasonPhrase}",
                    response.StatusCode, response.ReasonPhrase);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while attempting to send Slack message after retries.");
            return false;
        }

        return true;
    }
}
