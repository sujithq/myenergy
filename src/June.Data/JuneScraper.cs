using June.Data.Commands;
using Microsoft.Extensions.Options;
using myenergy.Common;
using Spectre.Console;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace June.Data
{
    internal class JuneScraper(IOptions<JuneSettings> settings) : IJuneScraper
    {
        private readonly JuneSettings settings = settings.Value;

        public async Task<JsonDocument?> LoginAsync()
        {

            var juneBaseAddress = "https://api.june.energy/";
            var juneTokenEndpoint = "rest/oauth/token";

            using var client = new HttpClient { BaseAddress = new Uri(juneBaseAddress) };
            string json = JsonSerializer.Serialize(settings);

            var requestToken = new HttpRequestMessage(HttpMethod.Post, juneTokenEndpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(requestToken);
            if (!response.IsSuccessStatusCode)
            {
                Alert($"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}", "info");
                return default;
            }

            var content = await response.Content.ReadAsStringAsync();
            var loginDoc = JsonDocument.Parse(content);

            // If an access token is returned directly (rare), use it
            if (loginDoc.RootElement.TryGetProperty("access_token", out var accessTokenElem) &&
                accessTokenElem.ValueKind == JsonValueKind.String)
            {
                return loginDoc;
            }

            // Otherwise, get the code and call GetToken
            var code = loginDoc.RootElement.GetProperty("code").GetString();
            if (string.IsNullOrEmpty(code))
            {
                Alert("No login code returned.");
                return default;
            }

            // Now get the actual Bearer token using the code
            var token = await GetToken(code);
            if (token != null)
            {
                // Wrap it in a JSON object for downstream compatibility
                var obj = JsonSerializer.SerializeToDocument(new { token });
                return obj;
            }

            Alert("Could not retrieve Bearer token.");
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
                Alert($"{dataResponse.StatusCode}: {await dataResponse.Content.ReadAsStringAsync()}");
            }
            return await Task.FromResult<JsonDocument?>(default);
        }

        public async Task<QuarterData?> GetQuarterData2(Dictionary<string, string> config, string? date_id)
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
                Alert($"Electricity: {dataResponseE.StatusCode}: {await dataResponseE.Content.ReadAsStringAsync()}");
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
                Alert($"Gas: {dataResponseE.StatusCode}: {await dataResponseE.Content.ReadAsStringAsync()}");
            }

            return dataE == default && dataG == default ? await Task.FromResult<QuarterData?>(default) : ConvertToQuarterData2(dataE, dataG);

        }

        private static QuarterData? ConvertToQuarterData2(EnergyData? dataE, EnergyData? dataG)
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

            return new QuarterData(consumption, injection, gas, [], [], [], []);
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

        private async Task<string?> GetToken(string code)
        {
            using var client = new HttpClient();
            string loginPageHtml = await client.GetStringAsync("https://app.june.energy/login");

            // Extract buildId robustly from __NEXT_DATA__ script tag
            string? buildId = ExtractBuildIdFromNextData(loginPageHtml);
            if (buildId == null)
            {
                Alert("Could not extract buildId from login page.");
                return null;
            }

            string callbackUrl = $"https://app.june.energy/_next/data/{buildId}/login/callback.json?code={code}";
            var callbackResponse = await client.GetAsync(callbackUrl);
            if (!callbackResponse.IsSuccessStatusCode)
            {
                Alert($"Failed to fetch callback: {callbackResponse.StatusCode} - {await callbackResponse.Content.ReadAsStringAsync()}");
                return null;
            }
            string callbackJson = await callbackResponse.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(callbackJson);
            if (doc.RootElement.TryGetProperty("pageProps", out var pageProps) && pageProps.TryGetProperty("token", out var tokenElem))
            {
                string token = tokenElem.GetString();
                // Use this token for your API calls!
                return token;
            }

            // Fallback: recursively search for token
            string? nestedToken = FindTokenRecursively(doc.RootElement);
            if (nestedToken != null)
                return nestedToken;

            Alert("Token not found in callback JSON.");
            return null;
        }

        // Helper function to recursively search for 'token' property
        private static string? FindTokenRecursively(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals("token") && property.Value.ValueKind == JsonValueKind.String)
                        return property.Value.GetString();
                    else
                    {
                        var found = FindTokenRecursively(property.Value);
                        if (found != null)
                            return found;
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var found = FindTokenRecursively(item);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        private static string? ExtractBuildIdFromNextData(string html)
        {
            const string marker = "<script id=\"__NEXT_DATA__\" type=\"application/json\">";
            int idx = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx == -1)
                return null;

            int startIdx = idx + marker.Length;
            int endIdx = html.IndexOf("</script>", startIdx, StringComparison.OrdinalIgnoreCase);
            if (endIdx == -1)
                return null;

            string json = html[startIdx..endIdx];
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("buildId", out var buildIdElem))
                return buildIdElem.GetString();

            return null;
        }

        protected static void Alert(string message, string type = "info", ConsoleColor cc = ConsoleColor.Red)
        {
            AnsiConsole.MarkupLine($"[bold {cc}]{type}[/]: {message}");
        }
    }

}
