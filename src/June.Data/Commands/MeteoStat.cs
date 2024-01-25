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

namespace MeteoStat.Data.Commands
{
    public class MeteoStatRunSettings : BaseCommandSettings
    {
        
    }

    
    public class MeteoStatRunCommand : BaseRunCommand<MeteoStatRunSettings, MeteoStatSettings>
    {
        public MeteoStatRunCommand(IOptions<MeteoStatSettings> settings, IScraper scraper) : base(settings)
        {
            Scraper = scraper;
        }

        public IScraper Scraper { get; }
        public override int Execute(CommandContext context, MeteoStatRunSettings settings)
        {
            var dataPath = Path.Combine(AppContext.BaseDirectory, "Data/data.json");

            Alert($"Reading from {dataPath}", "Info", ConsoleColor.Green);

            var data = JsonSerializer.Deserialize<Dictionary<int, List<BarChartData>>>(File.ReadAllTextAsync(dataPath).GetAwaiter().GetResult());

            var currentDateInBelgium = MyExtensions.BelgiumTime();
            var currentDateInBelgiumString = currentDateInBelgium.ToString("yyyyMMdd", null);

            // For MeteoStat
            var listForMeteoStatProcessed = data!
                .SelectMany(kvp => kvp.Value //.Where(data => !data.M)
                                            .Select(data => (kvp.Key, data.D, data.D.DayOfYearLocalDate(kvp.Key)))
                .Where(date => date.Item3 <= currentDateInBelgium.Date)
                .Select(date => (date.Key, date.D, date.Item3)))
                .ToList();

            if (listForMeteoStatProcessed.FindIndex(f => f.Key == currentDateInBelgium.Year && f.D == currentDateInBelgium.DayOfYear) == -1)
                listForMeteoStatProcessed.Add((currentDateInBelgium.Year, currentDateInBelgium.DayOfYear, currentDateInBelgium.Date));

            var meteoStatStart = listForMeteoStatProcessed.Min(item => item.Item3.ToString("yyyy-MM-dd", null));
            var meteoStatEnd = listForMeteoStatProcessed.Max(item => item.Item3.ToString("yyyy-MM-dd", null));

            var mdata = Scraper.GetData(new Dictionary<string, string>() { { "start", meteoStatStart! }, { "end", meteoStatEnd! } }, default).GetAwaiter().GetResult();

            if (mdata != default)
            {
                Alert("Process Meteo Stat Data", "Info", ConsoleColor.Green);
                var el = mdata.RootElement.GetProperty("data").EnumerateArray();

                foreach (var item in listForMeteoStatProcessed)
                {
                    var dateStr = item.Item3.ToString("yyyy-MM-dd", null);

                    var obj = el.FirstOrDefault(item => item.GetProperty("date").GetString() == dateStr);

                    if (obj.ValueKind != JsonValueKind.Null)
                    {

                        var msObj = obj.GetProperty("tavg");
                        double tavg = 0.0;

                        if (msObj.ValueKind != JsonValueKind.Null)
                        {
                            msObj.TryGetDouble(out tavg);
                        }

                        msObj = obj.GetProperty("tmin");
                        double tmin = 0.0;

                        if (msObj.ValueKind != JsonValueKind.Null)
                        {
                            msObj.TryGetDouble(out tmin);
                        }

                        msObj = obj.GetProperty("tmax");
                        double tmax = 0.0;

                        if (msObj.ValueKind != JsonValueKind.Null)
                        {
                            msObj.TryGetDouble(out tmax);
                        }

                        msObj = obj.GetProperty("tsun");
                        double tsun = 0.0;

                        if (msObj.ValueKind != JsonValueKind.Null)
                        {
                            msObj.TryGetDouble(out tsun);
                        }

                        msObj = obj.GetProperty("prcp");
                        double prcp = 0.0;

                        if (msObj.ValueKind != JsonValueKind.Null)
                        {
                            msObj.TryGetDouble(out prcp);
                        }

                        msObj = obj.GetProperty("snow");
                        double snow = 0.0;

                        if (msObj.ValueKind != JsonValueKind.Null)
                        {
                            msObj.TryGetDouble(out snow);
                        }

                        msObj = obj.GetProperty("wdir");
                        double wdir = 0.0;

                        if (msObj.ValueKind != JsonValueKind.Null)
                        {
                            msObj.TryGetDouble(out wdir);
                        }

                        msObj = obj.GetProperty("wspd");
                        double wspd = 0.0;

                        if (msObj.ValueKind != JsonValueKind.Null)
                        {
                            msObj.TryGetDouble(out wspd);
                        }

                        msObj = obj.GetProperty("wpgt");
                        double wpgt = 0.0;

                        if (msObj.ValueKind != JsonValueKind.Null)
                        {
                            msObj.TryGetDouble(out wpgt);
                        }

                        msObj = obj.GetProperty("pres");
                        double pres = 0.0;

                        if (msObj.ValueKind != JsonValueKind.Null)
                        {
                            msObj.TryGetDouble(out pres);
                        }

                        MeteoStatData msd = new(tavg, tmin, tmax, prcp, snow, wdir, wspd, wpgt, pres, tsun);

                        if (!data!.TryGetValue(item.Key, out List<BarChartData>? value))
                        {
                            value = [];
                            data.Add(item.Key, value);
                        }
                        if (value.FindIndex(f => f.D == item.D) == -1)
                        {
                            value.Add(new BarChartData(item.D, 0, 0, 0, false, false, msd, false, new AnomalyData(0, 0, 0, false), new QuarterData([], [], [])));
                        }

                        var idx = value.FindIndex(f => f.D == item.D);
                        var d = value[idx];
                        value[idx] = new BarChartData(d.D, d.P, d.U, d.I, d.J, d.S, msd, item.Item3 == currentDateInBelgium.Date ? false : true, d.AS, d.Q);

                    }
                }
            }
            else
            {
                Alert("No Meteo Stat Data To Process", "Warning");
            }

            DetectAnomaly(data!);

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "Data/data.json"), JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));

            return Environment.ExitCode;
        }
    }

}
