using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CampaignQueueMonitor.Utils;
using Serilog;
using static CampaignQueueMonitor.Utils.ExceptionUtils;

namespace CampaignQueueMonitor.QueueMonitor;

public partial class QueueMonitor
{
    private Config config { get; }
    private HttpClient httpClient { get; }

    public QueueMonitor(Config config, HttpClient httpClient)
        => (this.config, this.httpClient)
            = (config, httpClient);

    public IEnumerable<Task<HttpResponseMessage>> Execute()
        => config.CampaignServerIdRange.MapInclusive(
            serverId => SendData(
                $"Campaign.{serverId}",
                SuppressExceptions(() => FetchCampaignQueueSize(serverId), "failed")
            )
        ).Append(
            SendData("Zendesk.Metric", $"{FetchZendeskQueueCount()}")
        );

    protected Task<HttpResponseMessage> SendData(string metric, string value)
    {
        SeriesItemDto seriesItemDto = new(
            metric,
            new() { new() { $"{DateTimeOffset.Now.ToUnixTimeSeconds()}", value } },
            "count"
        );
        VisualiserSeriesDto visualiserSeriesDto = new(
            new() { seriesItemDto }
        );
        StringContent stringContent = new(
            JsonSerializer.Serialize(visualiserSeriesDto), Encoding.UTF8, "application/json"
        );

        return httpClient.PostAsync(config.VisualiserSeriesUriWithApiKey, stringContent);
    }

    protected string FetchCampaignQueueSize(int serverId)
    {
        string url = string.Format(config.CountServerUriFormat, serverId);
        using HttpResponseMessage response = httpClient.GetAsync(url).Result;
        using HttpContent content = response.Content;
        response.EnsureSuccessStatusCode();
        var htmlCode = content.ReadAsStringAsync().Result;

        // FIXME someone's scraping HTML with regex XD
        Match match = new Regex("new count: (\\d*)", RegexOptions.IgnoreCase).Match(htmlCode);
        if (match.Groups.Count < 2)
        {
            throw new InvalidOperationException("Count server response HTML scraping failed (no matches)");
        }

        string campaignQueueSize = match.Groups[1].Value;
        Log.Information($"Server: {serverId} :: campaign queue size: {campaignQueueSize}");

        return campaignQueueSize;
    }

    protected int FetchZendeskQueueCount()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, config.CaseManagementQueueCountUrl);
        request.Headers.Authorization
            = config.CaseManagementAuthenticationHeaderValue; // name mismatch due to M$ lib code
        
        using HttpResponseMessage httpResponseMessage = httpClient.SendAsync(request).Result;
        httpResponseMessage.EnsureSuccessStatusCode();
        var dto = httpResponseMessage.Content.ReadFromJsonAsync<ZendeskQueueDto>().Result
            ?? throw new InvalidOperationException("ZendeskQueue response JSON malformed");

        Log.Information($"Zendesk Engineering ticket count: {dto.count}");

        return dto.count;
    }
}
