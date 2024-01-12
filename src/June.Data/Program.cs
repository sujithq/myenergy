using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using myenergy.Common;
using myenergy.Common.Extensions;
using System.Globalization;
using System.Text.Json;

namespace June.Data
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var failed = false;
            var devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");

            var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) ||
                                devEnvironmentVariable.ToLower() == "development";

            //Determines the working environment as IHostingEnvironment is unavailable in a console app

            var builder = new ConfigurationBuilder();
            // tell the builder to look for the appsettings.json file
            builder
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // only add secrets in development
            if (isDevelopment)
            {
                builder
                    .AddUserSecrets<Program>();
            }

            var Configuration = builder.Build();

            IServiceCollection services = new ServiceCollection();

            //Map the implementations of your classes here ready for DI
            services
                .Configure<JuneSettings>(Configuration.GetSection(nameof(JuneSettings)))
                .Configure<SungrowSettings>(Configuration.GetSection(nameof(SungrowSettings)))
                .AddOptions()
                .BuildServiceProvider();

            var serviceProvider = services.BuildServiceProvider();

            // Get JuneSettings from DI
            var juneSettings = serviceProvider.GetService<IOptions<JuneSettings>>();
            var sungrowSettings = serviceProvider.GetService<IOptions<SungrowSettings>>();

            var data = JsonSerializer.Deserialize<Dictionary<int, List<BarChartData>>>(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Data/data.json")));

            var currentDateInBelgium = MyExtensions.BelgiumTime();
            var currentDateInBelgiumString = currentDateInBelgium.ToString("yyyyMMdd", null);

            // For JuneProcessed = false
            var listForJuneProcessed = data!
                .SelectMany(kvp => kvp.Value.Where(data => !data.J || data.P < data.I / 1000.0)
                                            .Select(data => (kvp.Key, data.D, data.D.DayOfYearLocalDate(kvp.Key)))
                .Where(date => date.Item3 <= currentDateInBelgium.Date)
                .Select(date => (date.Key, date.D, date.Item3.ToString("yyyyMMdd", null), date.Item3)))
                .ToList();

            if (listForJuneProcessed.Count == 0)
                listForJuneProcessed.Add((currentDateInBelgium.Year, currentDateInBelgium.DayOfYear, currentDateInBelgiumString, currentDateInBelgium.Date));

            // For SungrowProcessed = false
            var listForSungrowProcessed = data!
                .SelectMany(kvp => kvp.Value.Where(data => !data.S || data.P < data.I / 1000.0)
                                            .Select(data => (kvp.Key, data.D, data.D.DayOfYearLocalDate(kvp.Key)))
                .Where(date => date.Item3 <= currentDateInBelgium.Date)
                .Select(date => (date.Key, date.D, date.Item3.ToString("yyyyMMdd", null), date.Item3)))
                .ToList();

            var listForSungrowProcessed2 = string.Join(Environment.NewLine, data!
                .SelectMany(kvp => kvp.Value.Select(data => (kvp.Key, data.D.DayOfYearLocalDate(kvp.Key).ToString("dd/MM/yyyy", null), data.P)))
                .Select(date => $"{date.Key} {date.Item2} {date.P}")
                .ToList());

            var juneScraper = new JuneScraper(juneSettings!.Value);
            var juneLogin = await juneScraper.LoginAsync();
            if (juneLogin != default)
            {
                var refresh_token = juneLogin!.RootElement.GetProperty("refresh_token").GetString();

                foreach (var item in listForJuneProcessed)
                {
                    var juneData = await juneScraper.GetData(new Dictionary<string, string>() { { "refresh_token", refresh_token! } }, item.Item3);

                    var consumption = juneData.RootElement.GetProperty("electricity").GetProperty("single").GetProperty("consumption").GetDouble();
                    var injection = juneData.RootElement.GetProperty("electricity").GetProperty("single").GetProperty("injection").GetDouble() * 1000;

                    if (!data!.TryGetValue(item.Key, out List<BarChartData>? value))
                    {
                        value = [];
                        data.Add(item.Key, value);
                    }
                    if (value.Count < item.D)
                    {
                        value.Add(new BarChartData(item.D, 0, 0, 0, false, false));
                    }

                    var d = value[item.D - 1];
                    value[item.D - 1] = new BarChartData(d.D, d.P, consumption, injection, item.Item4 == currentDateInBelgium.Date ? false : true, d.S);
                }
            }
            else
            {
                await Console.Error.WriteLineAsync($"Warning: Could not login into June");
            }

            var sungrowScraper = new SungrowScraper(sungrowSettings!.Value);
            var sungrowLogin = await sungrowScraper.LoginAsync();

            if (sungrowLogin != default)
            {
                var token = sungrowLogin.RootElement.GetProperty("result_data").GetProperty("token").GetString();
                var user_id = sungrowLogin.RootElement.GetProperty("result_data").GetProperty("user_id").GetString();

                foreach (var item in listForSungrowProcessed)
                {
                    var sungrowData = await sungrowScraper.GetData(new Dictionary<string, string>() { { "token", token! }, { "user_id", user_id! } }, item.Item3);

                    if (sungrowData != default)
                    {
                        var result_data = sungrowData.RootElement.GetProperty("result_data");
                        if (result_data.ValueKind == JsonValueKind.Null)
                        {
                            await Console.Error.WriteLineAsync($"Error: ({sungrowData.RootElement.GetProperty("result_code").GetString()}) {sungrowData.RootElement.GetProperty("result_msg").GetString()}");
                            failed = true;
                        }
                        else
                        {
                            _ = double.TryParse(result_data.GetProperty("day_data").GetProperty("p83077_map_virgin").GetProperty("value").GetString()!, out var production);

                            production /= 1000;

                            if (!data!.TryGetValue(item.Key, out List<BarChartData>? value))
                            {
                                value = [];
                                data.Add(item.Key, value);
                            }
                            if (value.Count < item.D)
                            {
                                value.Add(new BarChartData(item.D, 0, 0, 0, false, false));
                            }


                            var d = value[item.D - 1];
                            value[item.D - 1] = new BarChartData(d.D, production, d.U, d.I, d.J, item.Item4 == currentDateInBelgium.Date ? false : true);
                        }
                    }
                }
            }
            else
            {
                await Console.Error.WriteLineAsync($"Warning: Could not login into Sungrow");
            }


            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "Data/data.json"), JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));

            if (failed)
            {
                Environment.ExitCode = 1;
            }
        }
    }
}
