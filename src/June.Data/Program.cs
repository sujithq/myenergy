using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace June.Data
{
    internal class Program
    {


        static async Task Main(string[] args)
        {

            var devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");

            var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) ||
                                devEnvironmentVariable.ToLower() == "development";
            //Determines the working environment as IHostingEnvironment is unavailable in a console app

            var builder = new ConfigurationBuilder();
            // tell the builder to look for the appsettings.json file
            builder
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            //only add secrets in development
            if (isDevelopment)
            {
                builder.AddUserSecrets<Program>();
            }

            var Configuration = builder.Build();

            IServiceCollection services = new ServiceCollection();

            //Map the implementations of your classes here ready for DI
            services
                .Configure<JuneSettings>(Configuration.GetSection(nameof(JuneSettings)))
                .AddOptions()
                //.AddLogging()
                .BuildServiceProvider();

            var serviceProvider = services.BuildServiceProvider();

            // Get JuneSettings from DI
            var juneSettings = serviceProvider.GetService<IOptions<JuneSettings>>();

            // Some parameters
            var from = "2023-12-29";
            var to = "2023-12-30";
            var valueType = "ENERGY";

            // June API endpoints
            var juneBaseAddress = "https://api.june.energy/";
            var juneTokenEndpoint = "rest/oauth/token";
            var juneSummaryEndpoint = $"eliq/contract/18713/summary?from={from}&to={to}&valueType={valueType}";

            // Create a new HttpClient and set the base address
            var client = new HttpClient { BaseAddress = new Uri(juneBaseAddress) };

            // Create the request body as JSON
            string json = JsonSerializer.Serialize(juneSettings!.Value);

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

                // If you need to deserialize the JSON into an object
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);

                // Create the request body as JSON
                var requestData = new HttpRequestMessage(HttpMethod.Get, juneSummaryEndpoint)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                // Add the access token to the request header
                requestData.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse!.refresh_token);

                // Send the request to the server and wait for the response
                var dataResponse = await client.SendAsync(requestData);

                // If the response contains content we want to read it!
                if (dataResponse.IsSuccessStatusCode)
                {
                    // Read the response content as a string
                    var dataResponseStringContent = await dataResponse.Content.ReadAsStringAsync();

                    // If you need to deserialize the JSON into an object
                    var summaryResponse = JsonSerializer.Deserialize<SummaryResponse>(dataResponseStringContent);

                    Console.WriteLine($"from {from} to {to} Consumption is {summaryResponse!.electricity.single.consumption} kWh and Injection is {summaryResponse.electricity.single.injection} kWh");
                }
                else
                {
                    await Console.Out.WriteLineAsync("Error: " + dataResponse.StatusCode);
                }

            }
            else
            {
                await Console.Out.WriteLineAsync("Error: " + response.StatusCode);
            }



        }
    }

    internal class TokenResponse
    {
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string access_token { get; set; }
        public string refresh_token { get; set; }
    }

    internal class SummaryResponse
    {
        public bool hasElectricity { get; set; }
        public bool hasElectricityInjection { get; set; }
        public bool hasGas { get; set; }
        public ElectricityData electricity { get; set; }
        public GasData gas { get; set; }
    }

    internal class ElectricityData
    {
        public ConsumptionInjectionData single { get; set; }
        public ConsumptionInjectionData day { get; set; }
        public ConsumptionInjectionData night { get; set; }
    }

    internal class ConsumptionInjectionData
    {
        public double consumption { get; set; }
        public double injection { get; set; }
    }

    internal class GasData
    {
        public double consumption { get; set; }
    }

    public class JuneSettings
    {
        public string username { get; set; }
        public string password { get; set; }
        public string grant_type { get; set; }
        public string client_id { get; set; }
        public string client_secret { get; set; }
    }
}
