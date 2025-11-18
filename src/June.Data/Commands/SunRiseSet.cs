using June.Data.Commands.Settings;
using Microsoft.Extensions.Options;
using myenergy.Common;
using myenergy.Common.Extensions;
using Newtonsoft.Json.Linq;
using Spectre.Console.Cli;
using System.Globalization;
using System.Text.Json;

namespace June.Data.Commands
{

    public class SunRiseSetSettings
    {
    }

    public class SunRiseSetRunSettings : BaseCommandSettings
    {

    }

    public class SunRiseSetRunCommand : BaseRunCommand<SunRiseSetRunSettings, SunRiseSetSettings>
    {
        public SunRiseSetRunCommand(IOptions<SunRiseSetSettings> settings) : base(settings)
        {
        }

        public override int Execute(CommandContext context, SunRiseSetRunSettings settings, CancellationToken ct)
        {
            var dataPath = Path.Combine(AppContext.BaseDirectory, "Data/data.json");
            Alert($"Reading from {dataPath}", "Info", ConsoleColor.Green);

            var data = JsonSerializer.Deserialize<Dictionary<int, List<BarChartData>>>(File.ReadAllTextAsync(dataPath).GetAwaiter().GetResult());

            HttpClient client = new()
            {
                BaseAddress = new Uri("https://api.sunrise-sunset.org")
            };


            var lst = data!.SelectMany(sm => sm.Value.Select(s => (sm.Key, s))).ToList();


            foreach (var (year, day) in lst)
            {
                if (day.SRS == null)
                {
                    var localDate = day.D.DayOfYearLocalDate(year).ToDateOnly();
                    var query = localDate.ToString("yyyy-MM-dd", null);

                    Alert($"Get data for {query}", "info");
                    var response = client.GetAsync($"json?lat=50.9746&lng=4.5917&date={query}").GetAwaiter().GetResult();
                    var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var json = JObject.Parse(content);


                    TimeOnly.TryParse(json["results"]!["sunrise"]!.Value<string>(), CultureInfo.InvariantCulture, out var sunrise);
                    TimeOnly.TryParse(json["results"]!["sunset"]!.Value<string>(), CultureInfo.InvariantCulture, out var sunset);

                    var idx = data![year].FindIndex(f => f.D == day.D);
                    var d = data[year][idx];

                    data[year][idx] = new BarChartData(d.D, d.P, d.U, d.I, d.J, d.S, d.MS, d.M, d.AS, d.Q, d.C, new SunRiseSet(localDate.ToDateTime(sunrise), localDate.ToDateTime(sunset)));
                }

            }

            DetectAnomaly(data!);

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "Data/data.json"), JsonSerializer.Serialize(data, JsonSerializerOptions));

            return Environment.ExitCode;
        }


    }    
}
