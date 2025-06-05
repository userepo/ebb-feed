using IC.EbbFeedWorkerService.Models;

namespace IC.EbbFeedWorkerService.Services;
public interface ISlackMessageSender
{
    /// <summary>
    /// Sends a notice to the specified webhook URL asynchronously.
    /// </summary>
    /// <remarks>Ensure that the <paramref name="webhookUrl"/> is a valid and reachable URL. The method does
    /// not retry failed requests.</remarks>
    /// <param name="webhookUrl">The URL of the webhook to which the notice will be sent. This cannot be null or empty.</param>
    /// <param name="notice">The notice object containing the message and associated data to be sent. This cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the message was
    /// sent successfully; otherwise, <see langword="false"/>.</returns>
    Task<bool> SendMessageAsync(string webhookUrl, Notice notice);
}