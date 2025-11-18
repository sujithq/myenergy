using June.Data;
using June.Data.Commands;
using June.Data.Commands.Settings;
using Microsoft.Extensions.Options;
using myenergy.Common;
using myenergy.Common.Extensions;
using NodaTime;
using Spectre.Console.Cli;
using System.Text.Json;

namespace Sungrow.Data.Commands
{
    public class SungrowSettings
    {
        public required string username { get; set; }
        public required string gatewayUrl { get; set; }

        private string? _password;
        public string password
        {
            get => string.IsNullOrEmpty(_password) ? Environment.GetEnvironmentVariable("SUNGROW_PASSWORD")! : _password;
            set => _password = value;
        }

        private string? _APP_RSA_PUBLIC_KEY;
        public string APP_RSA_PUBLIC_KEY
        {
            get => string.IsNullOrEmpty(_APP_RSA_PUBLIC_KEY) ? Environment.GetEnvironmentVariable("SUNGROW_APP_RSA_PUBLIC_KEY")! : _APP_RSA_PUBLIC_KEY;
            set => _APP_RSA_PUBLIC_KEY = value;
        }

        private string? _ACCESS_KEY;
        public string ACCESS_KEY
        {
            get => string.IsNullOrEmpty(_ACCESS_KEY) ? Environment.GetEnvironmentVariable("SUNGROW_ACCESS_KEY")! : _ACCESS_KEY;
            set => _ACCESS_KEY = value;

        }

        private string? _APP_KEY;
        public string APP_KEY
        {
            get => string.IsNullOrEmpty(_APP_KEY) ? Environment.GetEnvironmentVariable("SUNGROW_APP_KEY")! : _APP_KEY;
            set => _APP_KEY = value;

        }
        private string? _PS_ID;
        public string PS_ID
        {
            get => string.IsNullOrEmpty(_PS_ID) ? Environment.GetEnvironmentVariable("SUNGROW_PS_ID")! : _PS_ID;
            set => _PS_ID = value;

        }
    }

    public class SungrowRunSettings : BaseCommandSettings
    {

    }

    public class SungrowRunCommand : BaseRunCommand<SungrowRunSettings, SungrowSettings>
    {
        public SungrowRunCommand(IOptions<SungrowSettings> settings, IScraper scraper) : base(settings)
        {
            Scraper = scraper;
        }

        public IScraper Scraper { get; }
        public override int Execute(CommandContext context, SungrowRunSettings settings, CancellationToken ct)
        {
            var failed = false;

            var dataPath = Path.Combine(AppContext.BaseDirectory, "Data/data.json");

            Alert($"Reading from {dataPath}", "Info", ConsoleColor.Green);

            var data = JsonSerializer.Deserialize<Dictionary<int, List<BarChartData>>>(File.ReadAllTextAsync(dataPath).GetAwaiter().GetResult());

            var currentDateInBelgium = MyExtensions.BelgiumTime();
            var currentDateInBelgiumString = currentDateInBelgium.ToString("yyyyMMdd", null);

            // For SungrowProcessed = false
            var listForSungrowProcessed = data!
                .SelectMany(kvp => kvp.Value.Where(data => !data.S || data.P < data.I / 1000.0)
                                            .Select(data => (kvp.Key, data.D, data.D.DayOfYearLocalDate(kvp.Key)))
                .Where(date => date.Item3 <= currentDateInBelgium.Date)
                .Select(date => (date.Key, date.D, date.Item3.ToString("yyyyMMdd", null), date.Item3)))
                .ToList();

            if (listForSungrowProcessed.FindIndex(f => f.Key == currentDateInBelgium.Year && f.D == currentDateInBelgium.DayOfYear) == -1)
                listForSungrowProcessed.Add((currentDateInBelgium.Year, currentDateInBelgium.DayOfYear, currentDateInBelgiumString, currentDateInBelgium.Date));

            var sungrowLogin = Scraper.LoginAsync().GetAwaiter().GetResult();

            if (sungrowLogin != default)
            {
                var token = sungrowLogin.RootElement.GetProperty("result_data").GetProperty("token").GetString();
                var user_id = sungrowLogin.RootElement.GetProperty("result_data").GetProperty("user_id").GetString();

                foreach (var item in listForSungrowProcessed)
                {
                    var sungrowData = Scraper.GetData(new Dictionary<string, string>() { { "token", token! }, { "user_id", user_id! } }, item.Item3).GetAwaiter().GetResult();

                    if (sungrowData != default)
                    {
                        Alert($"Process Sungrow Data ({item.D} => {item.Item3})", "Info", ConsoleColor.Green);


                        var result_data = sungrowData.RootElement.GetProperty("result_data");
                        if (result_data.ValueKind == JsonValueKind.Null)
                        {
                            Alert($"({sungrowData.RootElement.GetProperty("result_code").GetString()}) {sungrowData.RootElement.GetProperty("result_msg").GetString()}", "Warning");
                            failed = true;
                        }
                        else
                        {
                            var dd = result_data.GetProperty("day_data");
                            _ = double.TryParse(dd.GetProperty("p83077_map_virgin").GetProperty("value").GetString()!, out var production);

                            production /= 1000;

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

                            var d15l = dd.GetProperty("point_data_15_list");

                            List<double> P2 = [];

                            foreach (JsonElement it in d15l.EnumerateArray())
                            {
                                double val = int.Parse(it.GetProperty("p83076").GetString()!) / 1000.0;
                                P2.Add(val);
                            }

                            var newQ = new QuarterData(d.Q.C, d.Q.I, d.Q.G, P2, d.Q.WRT, d.Q.WOT, d.Q.WP);

                            value[idx] = new BarChartData(d.D, production, d.U, d.I, d.J, item.Item4 >= currentDateInBelgium.Date.Minus(Period.FromDays(5)) ? false : true, d.MS, d.M, d.AS, newQ, d.C, d.SRS);
                        }
                    }
                    else
                    {
                        Alert("No Sungrow To Process", "Warning");
                    }
                }
            }
            else
            {
                Alert("Could not login into Sungrow", "Warning");
            }

            DetectAnomaly(data!);

            Alert("Saving data.json", "Info");

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "Data/data.json"), JsonSerializer.Serialize(data, JsonSerializerOptions));

            Alert("Calculate consolidation", "Info");
            var totalP = data!.SelectMany(sm => sm.Value.Select(s => s.P)).Sum();
            var totalU = data!.SelectMany(sm => sm.Value.Select(s => s.U)).Sum();
            var totalI = data!.SelectMany(sm => sm.Value.Select(s => s.I)).Sum() / 1000.0;
            var consolidated = new Consolidated(totalP, totalU, totalI);

            Alert("Saving consolidated.json", "Info");

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "Data/consolidated.json"), JsonSerializer.Serialize(consolidated, JsonSerializerOptions));

            if (failed)
            {
                Environment.ExitCode = 1;
            }
            return Environment.ExitCode;
        }
    }

}
