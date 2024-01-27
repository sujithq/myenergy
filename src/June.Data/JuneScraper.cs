using June.Data.Commands;
using Microsoft.Extensions.Options;
using myenergy.Common;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace June.Data
{
    internal class JuneScraper(IOptions<JuneSettings> settings) : IJuneScraper
    {
        private readonly JuneSettings settings = settings.Value;

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
        public async Task<JsonDocument?> GetData(Dictionary<string, string> config, string? date_id)
        {

            var from = DateOnly.ParseExact(date_id!, "yyyyMMdd").ToString("yyyy-MM-dd");
            var to = DateOnly.ParseExact(date_id!, "yyyyMMdd").AddDays(1).ToString("yyyy-MM-dd");
            var valueType = "ENERGY";
            var token = config["token"];

            // June API endpoints
            var juneBaseAddress = "https://api.june.energy/";
            var juneSummaryEndpoint = $"eliq/contract/{settings.contract}/summary?from={from}&to={to}&valueType={valueType}";

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
            return await Task.FromResult<JsonDocument?>(default);
        }

        public async Task<QuarterData?> GetQuarterData(Dictionary<string, string> config, string? date_id)
        {
            var from = DateOnly.ParseExact(date_id!, "yyyyMMdd").ToString("yyyy-MM-dd");
            var to = DateOnly.ParseExact(date_id!, "yyyyMMdd").AddDays(1).ToString("yyyy-MM-dd");
            var valueType = "ENERGY";
            var token = config["token"];

            // June API endpoints
            var juneBaseAddress = "https://api.june.energy/";
            var june15mEndpointE = $"eliq/contract/{settings.contract}/electricity/series/15min?from={from}&to={to}&valueType={valueType}";
            var june15mEndpointG = $"eliq/contract/{settings.contract}/gas/series/15min?from={from}&to={to}&valueType={valueType}";

            // Create a new HttpClient and set the base address
            var client = new HttpClient { BaseAddress = new Uri(juneBaseAddress) };

            // Create the request body as JSON
            string json = JsonSerializer.Serialize(settings);

            // Create the request body as JSON
            var requestDataE = new HttpRequestMessage(HttpMethod.Get, june15mEndpointE)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var requestDataG = new HttpRequestMessage(HttpMethod.Get, june15mEndpointG)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            // Add the access token to the request header
            requestDataE.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            requestDataG.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Send the request to the server and wait for the response
            var dataResponseE = await client.SendAsync(requestDataE);
            var dataResponseG = await client.SendAsync(requestDataG);
            EnergyData? dataE = default;
            EnergyData? dataG = default;

            // If the response contains content we want to read it!
            if (dataResponseE.IsSuccessStatusCode)
            {
                // Read the response content as a string
                var dataResponseStringContent = await dataResponseE.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                dataE = JsonSerializer.Deserialize<EnergyData>(dataResponseStringContent, options);
            }
            else
            {
                Console.WriteLine($"Electricity: {dataResponseE.StatusCode}: {await dataResponseE.Content.ReadAsStringAsync()}");
            }

            // If the response contains content we want to read it!
            if (dataResponseG.IsSuccessStatusCode)
            {
                // Read the response content as a string
                var dataResponseStringContent = await dataResponseG.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                dataG = JsonSerializer.Deserialize<EnergyData>(dataResponseStringContent, options);
            }
            else
            {
                Console.WriteLine($"Electricity: {dataResponseE.StatusCode}: {await dataResponseE.Content.ReadAsStringAsync()}");
            }

            return dataE == default && dataG == default ? await Task.FromResult<QuarterData?>(default) : ConvertToQuarterData(dataE, dataG);

        }

        private static QuarterData? ConvertToQuarterData(EnergyData? dataE, EnergyData? dataG)
        {
            if (dataE == null && dataG == null)
                return default;

            List<Coordinates> consumption = [];
            List<Coordinates> injection = [];
            List<Coordinates> gas = [];

            if (dataE != null)
            {
                consumption = FilterDataByPrefix(dataE, "electricity-consumption-total");

                injection = FilterDataByPrefix(dataE, "electricity-injection-total");

            }

            if (dataG != null)
            {
                gas = FilterDataByPrefix(dataG, "gas-consumption-total");
            }

            return new QuarterData(consumption, injection, gas, []);
        }
        private static List<Coordinates> FilterDataByPrefix(EnergyData? data, string prefix)
        {
            if (data == null)
                return new List<Coordinates>();

            return data.Series.Where(s => s.Name.StartsWith(prefix))
                             .SelectMany(s => s.Points)
                             .Select(p => new Coordinates(p.Y ?? 0))
                             .ToList();
        }

        public async Task<QuarterData2?> GetQuarterData2(Dictionary<string, string> config, string? date_id)
        {
            var from = DateOnly.ParseExact(date_id!, "yyyyMMdd").ToString("yyyy-MM-dd");
            var to = DateOnly.ParseExact(date_id!, "yyyyMMdd").AddDays(1).ToString("yyyy-MM-dd");
            var valueType = "ENERGY";
            var token = config["token"];

            // June API endpoints
            var juneBaseAddress = "https://api.june.energy/";
            var june15mEndpointE = $"eliq/contract/{settings.contract}/electricity/series/15min?from={from}&to={to}&valueType={valueType}";
            var june15mEndpointG = $"eliq/contract/{settings.contract}/gas/series/15min?from={from}&to={to}&valueType={valueType}";

            // Create a new HttpClient and set the base address
            var client = new HttpClient { BaseAddress = new Uri(juneBaseAddress) };

            // Create the request body as JSON
            string json = JsonSerializer.Serialize(settings);

            // Create the request body as JSON
            var requestDataE = new HttpRequestMessage(HttpMethod.Get, june15mEndpointE)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var requestDataG = new HttpRequestMessage(HttpMethod.Get, june15mEndpointG)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            // Add the access token to the request header
            requestDataE.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            requestDataG.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Send the request to the server and wait for the response
            var dataResponseE = await client.SendAsync(requestDataE);
            var dataResponseG = await client.SendAsync(requestDataG);
            EnergyData? dataE = default;
            EnergyData? dataG = default;

            // If the response contains content we want to read it!
            if (dataResponseE.IsSuccessStatusCode)
            {
                // Read the response content as a string
                var dataResponseStringContent = await dataResponseE.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                dataE = JsonSerializer.Deserialize<EnergyData>(dataResponseStringContent, options);
            }
            else
            {
                Console.WriteLine($"Electricity: {dataResponseE.StatusCode}: {await dataResponseE.Content.ReadAsStringAsync()}");
            }

            // If the response contains content we want to read it!
            if (dataResponseG.IsSuccessStatusCode)
            {
                // Read the response content as a string
                var dataResponseStringContent = await dataResponseG.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                dataG = JsonSerializer.Deserialize<EnergyData>(dataResponseStringContent, options);
            }
            else
            {
                Console.WriteLine($"Electricity: {dataResponseE.StatusCode}: {await dataResponseE.Content.ReadAsStringAsync()}");
            }

            return dataE == default && dataG == default ? await Task.FromResult<QuarterData2?>(default) : ConvertToQuarterData2(dataE, dataG);

        }

        private static QuarterData2? ConvertToQuarterData2(EnergyData? dataE, EnergyData? dataG)
        {
            if (dataE == null && dataG == null)
                return default;

            List<double> consumption = [];
            List<double> injection = [];
            List<double> gas = [];

            if (dataE != null)
            {
                consumption = FilterDataByPrefix2(dataE, "electricity-consumption-total");

                injection = FilterDataByPrefix2(dataE, "electricity-injection-total");

            }

            if (dataG != null)
            {
                gas = FilterDataByPrefix2(dataG, "gas-consumption-total");
            }

            return new QuarterData2(consumption, injection, gas, []);
        }
        private static List<double> FilterDataByPrefix2(EnergyData? data, string prefix)
        {
            if (data == null)
                return [];

            return data.Series.Where(s => s.Name.StartsWith(prefix))
                             .SelectMany(s => s.Points)
                             .Select(p => p.Y ?? 0)
                             .ToList();
        }
    }

}
