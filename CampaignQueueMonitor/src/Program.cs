using Microsoft.Extensions.Configuration;
using Serilog;
using QueueMonitor = CampaignQueueMonitor.QueueMonitor.QueueMonitor;

Log.Logger = new LoggerConfiguration()
             .WriteTo.Console()
             .CreateLogger();

var configurationRoot = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json").Build();

var campaignQueueMonitorConfig = configurationRoot
    .GetSection("CampaignQueueMonitorConfig")
    .Get<QueueMonitor.Config>();

if (campaignQueueMonitorConfig == null)
{
    throw new InvalidOperationException("appsettings.json malformed");
}

var campaignQueueMonitor = new QueueMonitor( campaignQueueMonitorConfig, new HttpClient() );
HttpResponseMessage[] result = Task.WhenAll(campaignQueueMonitor.Execute()).Result;

// TODO log the responses or whatever
