     namespace CampaignQueueMonitor
{
    internal static class Program
    {
        private const string VisualiserSeriesUri = "https://localhost/api/v1/series";
        private const string VisualiserApiKey = "randomstring";
        private const string CaseManagementQueueCountUrl = "https://localhost:9000";
        private const string CaseManagementAuthToken = "Basic token";

        public static void Main(string[] args)
        {
            var epochTimestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            short[] serverIds = {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11};

            foreach (var serverId in serverIds)
                SendData($"Campaign.{serverId}", FetchNewCountFromServer(serverId), epochTimestamp);

            SendData("Zendesk.Metric", FetchZendeskQueueCount(), epochTimestamp);
        }

        private static void SendData (string metric, string value, int epochTimestamp )
        {
            var postMetricRequest = (HttpWebRequest)WebRequest.Create(VisualiserSeriesUri + "?api_key=" + VisualiserApiKey);
            postMetricRequest.ContentType = "application/json";
            postMetricRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(postMetricRequest.GetRequestStream()))
            {
                var json = "{'series':[{'metric':'" + metric + "','points':[[" + epochTimestamp + "," + value + "]],'type':'count'}]}";
                json = json.Replace("'", "\"");
                streamWriter.Write(json);
            }

            // note that this throws away the response as we don't need it
            using var streamReader = new StreamReader(postMetricRequest.GetResponse().GetResponseStream() ?? throw new InvalidOperationException());
            streamReader?.ReadToEnd();
        }

        private static string FetchNewCountFromServer(short serverId)
        {
            using var client = new WebClient();
            var url = $"http://{serverId}.localhost.com/count";
            try
            {
                var htmlCode = client.DownloadString(url);
                const string newCount = "new count: (.*)";
                var match = new Regex(newCount, RegexOptions.IgnoreCase).Match(htmlCode);
                var campaignCount = match.Groups[1].Value;
                Console.WriteLine($"Server: {serverId}   Campaign Queue Size: {campaignCount}");
                return campaignCount;
            }
            catch (Exception)
            {
                return "failed";
            }
        }

        private static string FetchZendeskQueueCount()
        {
            using var client = new WebClient();
            client.Headers.Add("Authorization", CaseManagementAuthToken);
            var json = client.DownloadString(CaseManagementQueueCountUrl);
            var queueCount = JObject.Parse(json)["count"].ToString();
            Console.WriteLine($"Zendesk Engineering Ticket count: {queueCount}");
            return queueCount;
        }
    }
}