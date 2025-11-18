using June.Data.Commands.Settings;
using Microsoft.Extensions.Options;
using myenergy.Common;
using myenergy.Common.Extensions;
using NodaTime;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace June.Data.Commands
{

    public class JuneSettings
    {
        public required string username { get; set; }
        public required string grant_type { get; set; }

        private string? _password;
        public string password
        {
            get => string.IsNullOrEmpty(_password) ? Environment.GetEnvironmentVariable("JUNE_PASSWORD")! : _password;
            set => _password = value;

        }
        private string? _client_id;
        public string client_id
        {
            get => string.IsNullOrEmpty(_client_id) ? Environment.GetEnvironmentVariable("JUNE_CLIENT_ID")! : _client_id;
            set => _client_id = value;

        }
        private string? _client_secret;
        public string client_secret
        {
            get => string.IsNullOrEmpty(_client_secret) ? Environment.GetEnvironmentVariable("JUNE_CLIENT_SECRET")! : _client_secret;
            set => _client_secret = value;

        }
        private string? _contract;
        public string contract
        {
            get => string.IsNullOrEmpty(_contract) ? Environment.GetEnvironmentVariable("JUNE_CONTRACT")! : _contract;
            set => _contract = value;

        }
    }

    public class JuneRunSettings : BaseCommandSettings
    {

    }

    public class JuneRunCommand : BaseRunCommand<JuneRunSettings, JuneSettings>
    {
        public JuneRunCommand(IOptions<JuneSettings> settings, IJuneScraper scraper) : base(settings)
        {
            Scraper = scraper;
        }

        public IJuneScraper Scraper { get; }

        public override int Execute(CommandContext context, JuneRunSettings settings, CancellationToken ct)
        {
            var dataPath = Path.Combine(AppContext.BaseDirectory, "Data/data.json");

            Alert($"Reading from {dataPath}", "Info", ConsoleColor.Green);

            var data = JsonSerializer.Deserialize<Dictionary<int, List<BarChartData>>>(File.ReadAllTextAsync(dataPath).GetAwaiter().GetResult());

            var currentDateInBelgium = MyExtensions.BelgiumTime();
            var currentDateInBelgiumString = currentDateInBelgium.ToString("yyyyMMdd", null);

            // For JuneProcessed = false
            var listForJuneProcessed = data!
                .SelectMany(kvp => kvp.Value.Where(data => !data.J || data.P * 1000 < data.I || data.Q.C.Count == 0 || data.Q.I.Count == 0)
                                            .Select(data => (kvp.Key, data.D, data.D.DayOfYearLocalDate(kvp.Key)))
                .Where(date => date.Item3 <= currentDateInBelgium.Date)
                .Select(date => (date.Key, date.D, date.Item3.ToString("yyyyMMdd", null), date.Item3)))
                .ToList();

            if (listForJuneProcessed.FindIndex(f => f.Key == currentDateInBelgium.Year && f.D == currentDateInBelgium.DayOfYear) == -1)
                listForJuneProcessed.Add((currentDateInBelgium.Year, currentDateInBelgium.DayOfYear, currentDateInBelgiumString, currentDateInBelgium.Date));

            var juneLogin = Scraper.LoginAsync().GetAwaiter().GetResult();
            if (juneLogin != default)
            {
                var token_name = "token";
                var token = juneLogin!.RootElement.GetProperty(token_name).GetString();

                foreach (var item in listForJuneProcessed)
                {
                    var juneData = Scraper.GetData(new Dictionary<string, string>() { { "token", token! } }, item.Item3).GetAwaiter().GetResult();
                    var jQ = Scraper.GetQuarterData2(new Dictionary<string, string>() { { "token", token! } }, item.Item3).GetAwaiter().GetResult();

                    if (juneData != default)
                    {
                        Alert($"Process June Data ({item.D} => {item.Item3})", "Info", ConsoleColor.Green);

                        var consumption = juneData.RootElement.GetProperty("electricity").GetProperty("single").GetProperty("consumption").GetDouble();
                        var injection = juneData.RootElement.GetProperty("electricity").GetProperty("single").GetProperty("injection").GetDouble() * 1000;

                        if (!data!.TryGetValue(item.Key, out List<BarChartData>? value))
                        {
                            value = [];
                            data.Add(item.Key, value);
                        }
                        if (value.FindIndex(f => f.D == item.D) == -1)
                        {
                            value.Add(new BarChartData(item.D, 0, 0, 0, false, false, new MeteoStatData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0), false, new AnomalyData(0, 0, 0, false), new QuarterData([], [], [], [], [], [], []), false));
                        }

                        var idx = value.FindIndex(f => f.D == item.D);
                        var d = value[idx];
                        var qd = new QuarterData(jQ!.C, jQ.I, jQ.G, d.Q.P, d.Q.WRT, d.Q.WOT, d.Q.WP);

                        value[idx] = new BarChartData(d.D, d.P, consumption, injection, item.Item4 >= currentDateInBelgium.Date.Minus(Period.FromDays(5)) ? false : true, d.S, d.MS, d.M, d.AS, qd, d.C, d.SRS);
                    }
                    else
                    {
                        Alert("No June Data To Process", "Warning");
                    }



                }
            }
            else
            {
                Alert("Could not login into June", "Warning");
            }

            DetectAnomaly(data!);

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "Data/data.json"), JsonSerializer.Serialize(data, JsonSerializerOptions));

            var totalP = data!.SelectMany(sm => sm.Value.Select(s => s.P)).Sum();
            var totalU = data!.SelectMany(sm => sm.Value.Select(s => s.U)).Sum();
            var totalI = data!.SelectMany(sm => sm.Value.Select(s => s.I)).Sum() / 1000.0;
            var consolidated = new Consolidated(totalP, totalU, totalI);

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "Data/consolidated.json"), JsonSerializer.Serialize(consolidated, JsonSerializerOptions));


            return Environment.ExitCode;
        }
    }

    public class EnergyData
    {
        [JsonPropertyName("contractId")]
        public string ContractId { get; set; }

        [JsonPropertyName("energyType")]
        public string EnergyType { get; set; }

        [JsonPropertyName("from")]
        public string From { get; set; }

        [JsonPropertyName("resolution")]
        public string Resolution { get; set; }

        [JsonPropertyName("series")]
        public List<SeriesData> Series { get; set; }

        [JsonPropertyName("to")]
        public string To { get; set; }

        [JsonPropertyName("usageType")]
        public string? UsageType { get; set; }
    }

    public class SeriesData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("points")]
        public List<PointData> Points { get; set; }
    }

    public class PointData
    {
        //[JsonPropertyName("x")]
        //public string X { get; set; }

        [JsonPropertyName("y")]
        public double? Y { get; set; }
    }


}
