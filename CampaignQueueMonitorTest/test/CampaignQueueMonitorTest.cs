using System.Text.Json;
using CampaignQueueMonitor.Utils;
using RichardSzalay.MockHttp;
using Serilog;
using QueueMonitor = CampaignQueueMonitor.QueueMonitor.QueueMonitor;

namespace CampaignQueueMonitorTest.test;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    // TODO write more tests when we get more information about the actual remote service behaviour

    [Test]
    public void ExecutesProperly()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        var testConfig = new QueueMonitor.Config(
            "https://localhost/api/v1/series",
            "randomstring",
            "https://localhost:9000",
            "Basic",
            "token",
            "http://{0}.localhost.com/count",
            1,
            11
        );

        string zendeskQueueJson = JsonSerializer.Serialize(new QueueMonitor.ZendeskQueueDto(5));
        string ignoredContent = "{\"ignored\":true}";

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(testConfig.CaseManagementQueueCountUrl)
            .Respond("application/json", zendeskQueueJson);
        mockHttp.When(testConfig.VisualiserSeriesUriWithApiKey)
            .Respond("application/json", ignoredContent);
        mockHttp.When("http://*.localhost.com/count")
            .Respond("text/html", "<body>new count: 123</body>");
        mockHttp.Fallback
            .Respond(req => throw new InvalidOperationException($"unknown URI {req.RequestUri}"));
        
        HttpClient mockHttpClient = mockHttp.ToHttpClient();

        var campaignQueueMonitor = new QueueMonitor(testConfig, mockHttpClient);
        var postTasks = campaignQueueMonitor.Execute();
        HttpResponseMessage[] result = Task.WhenAll(postTasks).Result;

        mockHttp.VerifyNoOutstandingExpectation();
        var expectedResponseCount = testConfig.CampaignServerIdRange.LengthInclusive() + 1;
        Assert.That(result.Length, Is.EqualTo(expectedResponseCount));
        Assert.That(result.Select(r => r.Content.ReadAsStringAsync().Result), Is.All.EqualTo(ignoredContent));
    }
}
