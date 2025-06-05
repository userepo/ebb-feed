using IC.EbbFeedWorkerService;
using IC.EbbFeedWorkerService.Services;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<EbbFeedWorker>();

        builder.Services.AddHttpClient();

        builder.Services.AddTransient<ISlackMessageSender, SlackMessageSender>();

        var host = builder.Build();
        host.Run();
    }
}