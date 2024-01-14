using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace June.Data
{


    internal class JuneScraper : IScraper
    {
        private readonly JuneSettings settings;
        public JuneScraper(JuneSettings settings)
        {
            this.settings = settings;
        }
        public async Task<JsonDocument?> LoginAsync()
        {

            // June API endpoints
            var juneBaseAddress = "https://api.june.energy/";
            var juneTokenEndpoint = "rest/oauth/token";

            // Create a new HttpClient and set the base address
            var client = new HttpClient { BaseAddress = new Uri(juneBaseAddress) };

            // Create the request body as JSON
            string json = JsonSerializer.Serialize(settings);

            var requestToken = new HttpRequestMessage(HttpMethod.Post, juneTokenEndpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Send the request to the server and wait for the response
            var response = await client.SendAsync(requestToken);

            // If the response contains content we want to read it!
            if (response.IsSuccessStatusCode)
            {
                // Read the response content as a string
                json = await response.Content.ReadAsStringAsync();

                return JsonDocument.Parse(json);
            }
            else
            {
                Console.WriteLine($"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            }
            return default;
        }
        public async Task<JsonDocument> GetData(Dictionary<string, string> config, string date_id)
        {

            var from = DateOnly.ParseExact(date_id, "yyyyMMdd").ToString("yyyy-MM-dd");
            var to = DateOnly.ParseExact(date_id, "yyyyMMdd").AddDays(1).ToString("yyyy-MM-dd");
            var valueType = "ENERGY";
            var token = config["token"];

            // June API endpoints
            var juneBaseAddress = "https://api.june.energy/";
            var juneSummaryEndpoint = $"eliq/contract/18713/summary?from={from}&to={to}&valueType={valueType}";

            // Create a new HttpClient and set the base address
            var client = new HttpClient { BaseAddress = new Uri(juneBaseAddress) };

            // Create the request body as JSON
            string json = JsonSerializer.Serialize(settings);

            // Create the request body as JSON
            var requestData = new HttpRequestMessage(HttpMethod.Get, juneSummaryEndpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Add the access token to the request header
            requestData.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Send the request to the server and wait for the response
            var dataResponse = await client.SendAsync(requestData);

            // If the response contains content we want to read it!
            if (dataResponse.IsSuccessStatusCode)
            {
                // Read the response content as a string
                var dataResponseStringContent = await dataResponse.Content.ReadAsStringAsync();

                return JsonDocument.Parse(dataResponseStringContent);

            }
            else
            {
                Console.WriteLine($"{dataResponse.StatusCode}: {await dataResponse.Content.ReadAsStringAsync()}");
            }
            return await Task.FromResult<JsonDocument>(null);
        }
    }

}
