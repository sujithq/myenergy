using System.Text.Json;

namespace June.Data
{
    internal class MeteoStatScraper : IScraper
    {
        private readonly MeteoStatSettings settings;
        public MeteoStatScraper(MeteoStatSettings settings)
        {
            this.settings = settings;
        }
        public async Task<JsonDocument?> LoginAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<JsonDocument> GetData(Dictionary<string, string> config, string _)
        {
            // MeteoStat API endpoints
            var meteoStatBaseAddress = "https://meteostat.p.rapidapi.com/";
            var start = config["start"];
            var end = config["end"];
            var meteoStatTokenEndpoint = $"stations/daily?station=10637&start={start}&end={end}";

            // Create a new HttpClient and set the base address
            var client = new HttpClient { BaseAddress = new Uri(meteoStatBaseAddress) };

            var requestToken = new HttpRequestMessage(HttpMethod.Get, meteoStatTokenEndpoint);
            requestToken.Headers.Add("X-RapidAPI-Key", settings.Key);
            requestToken.Headers.Add("X-RapidAPI-Host", settings.Host);

            // Send the request to the server and wait for the response
            var response = await client.SendAsync(requestToken);

            // If the response contains content we want to read it!
            if (response.IsSuccessStatusCode)
            {
                // Read the response content as a string
                var json = await response.Content.ReadAsStringAsync();

                return JsonDocument.Parse(json);
            }
            return default;
        }
    }

}
