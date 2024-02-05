using June.Data;
using June.Data.Commands.Settings;
using myenergy.Common.Extensions;
using myenergy.Common;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using June.Data.Commands;
using NodaTime;

namespace Sungrow.Data.Commands
{
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
        public override int Execute(CommandContext context, SungrowRunSettings settings)
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
                                value.Add(new BarChartData(item.D, 0, 0, 0, false, false, new MeteoStatData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0), false, new AnomalyData(0, 0, 0, false), new QuarterData([], [], [], []), false));
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

                            var newQ = new QuarterData(d.Q.C, d.Q.I, d.Q.G, P2);

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

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "Data/data.json"), JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));

            if (failed)
            {
                Environment.ExitCode = 1;
            }
            return Environment.ExitCode;
        }
    }

}
