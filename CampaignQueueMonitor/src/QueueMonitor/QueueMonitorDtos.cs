using System.Net.Http.Headers;

// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable InconsistentNaming

namespace CampaignQueueMonitor.QueueMonitor;

public partial class QueueMonitor
{
    public record Config(
        string VisualiserSeriesUri,
        string VisualiserApiKey,
        string CaseManagementQueueCountUrl,
        string CaseManagementAuthScheme,
        string CaseManagementAuthToken,
        string CountServerUriFormat,
        int CampaignServerIdFirst,
        int CampaignServerIdLast
    )
    {
        public Config() : this("", "", "", "", "", "", 0, 0)
        {
            // needed for (de)serialization
        }

        public string VisualiserSeriesUriWithApiKey => VisualiserSeriesUri + "?api_key=" + VisualiserApiKey;

        public AuthenticationHeaderValue CaseManagementAuthenticationHeaderValue
            => new(CaseManagementAuthScheme, CaseManagementAuthToken);

        public Range CampaignServerIdRange => CampaignServerIdFirst..CampaignServerIdLast;
    }

    public record SeriesItemDto(
        string metric,
        List<List<string>> points,
        string type
    );

    public record VisualiserSeriesDto(
        List<SeriesItemDto> series
    );

    public record ZendeskQueueDto(int count);
}
