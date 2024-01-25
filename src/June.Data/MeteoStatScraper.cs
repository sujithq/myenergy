using Microsoft.Extensions.Options;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace June.Data
{
    internal class MeteoStatScraper(IOptions<MeteoStatSettings> settings) : IScraper
    {
        private readonly MeteoStatSettings settings = settings.Value;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<JsonDocument?> LoginAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            throw new NotImplementedException();
        }

        public async Task<JsonDocument?> GetData(Dictionary<string, string> config, string? _)
        {
            // MeteoStat API endpoints
            var meteoStatBaseAddress = "https://meteostat.p.rapidapi.com/";
            var start = config["start"];
            var end = config["end"];
            var meteoStatTokenEndpoint = $"stations/daily?station={settings.Station}&start={start}&end={end}&lat={settings.Lat}&lon={settings.Station}{settings.Lon}";

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

                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "Data/meteostat.json"), json);

                return JsonDocument.Parse(json);
            }
            else
            {
                Console.WriteLine($"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            }
            return default;
        }
    }

}
