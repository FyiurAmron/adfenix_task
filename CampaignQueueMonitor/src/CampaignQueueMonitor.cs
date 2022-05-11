namespace CampaignQueueMonitor {

using System.Net;
using System.Text.RegularExpressions;

public static class CampaignQueueMonitor {
    private const string VisualiserSeriesUri = "https://localhost/api/v1/series";
    private const string VisualiserApiKey = "randomstring";
    private const string CaseManagementQueueCountUrl = "https://localhost:9000";
    private const string CaseManagementAuthToken = "Basic token";

    public static void Main( string[] args ) {
        int epochTimestamp = (int) ( DateTime.UtcNow.Subtract( new DateTime( 1970, 1, 1 ) ) ).TotalSeconds;
        short[] serverIds = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

        foreach ( short serverId in serverIds ) {
            SendData( $"Campaign.{serverId}", FetchNewCountFromServer( serverId ), epochTimestamp );
        }

        SendData( "Zendesk.Metric", FetchZendeskQueueCount(), epochTimestamp );
    }

    private static void SendData( string metric, string value, int epochTimestamp ) {
        HttpWebRequest? postMetricRequest =
            (HttpWebRequest) WebRequest.Create( VisualiserSeriesUri + "?api_key=" + VisualiserApiKey );
        postMetricRequest.ContentType = "application/json";
        postMetricRequest.Method = "POST";

        using ( StreamWriter? streamWriter = new StreamWriter( postMetricRequest.GetRequestStream() ) ) {
            string? json = "{'series':[{'metric':'" + metric + "','points':[[" + epochTimestamp + "," + value
                + "]],'type':'count'}]}";
            json = json.Replace( "'", "\"" );
            streamWriter.Write( json );
        }

        // note that this throws away the response as we don't need it
        using StreamReader? streamReader =
            new StreamReader( postMetricRequest.GetResponse().GetResponseStream()
                              ?? throw new InvalidOperationException() );
        streamReader?.ReadToEnd();
    }

    private static string FetchNewCountFromServer( short serverId ) {
        using WebClient? client = new WebClient();
        string? url = $"http://{serverId}.localhost.com/count";
        try {
            string? htmlCode = client.DownloadString( url );
            const string newCount = "new count: (.*)";
            Match? match = new Regex( newCount, RegexOptions.IgnoreCase ).Match( htmlCode );
            string? campaignCount = match.Groups[1].Value;
            Console.WriteLine( $"Server: {serverId}   Campaign Queue Size: {campaignCount}" );
            return campaignCount;
        } catch ( Exception ) {
            return "failed";
        }
    }

    private static string FetchZendeskQueueCount() {
        using WebClient? client = new WebClient();
        client.Headers.Add( "Authorization", CaseManagementAuthToken );
        string? json = client.DownloadString( CaseManagementQueueCountUrl );
        var queueCount = JObject.Parse( json )["count"].ToString();
        Console.WriteLine( $"Zendesk Engineering Ticket count: {queueCount}" );
        return queueCount;
    }
}

}
