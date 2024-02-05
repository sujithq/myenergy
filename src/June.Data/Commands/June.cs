using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using myenergy.Common.Extensions;
using myenergy.Common;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Spectre.Console;
using June.Data.Commands.Settings;
using System.Text.Json.Serialization;
using NodaTime;

namespace June.Data.Commands
{
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

        public override int Execute(CommandContext context, JuneRunSettings settings)
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
                var token_name = "access_token";
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
                            value.Add(new BarChartData(item.D, 0, 0, 0, false, false, new MeteoStatData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0), false, new AnomalyData(0, 0, 0, false), new QuarterData([], [], [], []), false));
                        }

                        var idx = value.FindIndex(f => f.D == item.D);
                        var d = value[idx];
                        var qd = new QuarterData(jQ!.C, jQ.I, jQ.G, d.Q.P);

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

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "Data/data.json"), JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));

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
