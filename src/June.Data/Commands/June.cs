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
using Microsoft.ML;
using Spectre.Console;
using June.Data.Commands.Settings;
using System.Text.Json.Serialization;

namespace June.Data.Commands
{

    public class TypeRegistrar : ITypeRegistrar, ITypeResolver
    {
        private readonly IServiceProvider _provider;

        public TypeRegistrar(IServiceProvider provider)
        {
            _provider = provider;
        }

        public ITypeResolver Build()
        {
            return this;
        }

        public void Register(Type service, Type implementation)
        {
            // Implement registration logic if necessary
        }

        public void RegisterInstance(Type service, object implementation)
        {
            // Implement registration logic if necessary
        }

        public void RegisterLazy(Type service, Func<object> factory)
        {
            // Implement registration logic if necessary
        }

        public object? Resolve(Type? type)
        {
            return _provider.GetService(type!);
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

        public override int Execute(CommandContext context, JuneRunSettings settings)
        {
            var dataPath = Path.Combine(AppContext.BaseDirectory, "Data/data.json");

            Alert($"Reading from {dataPath}", "Info", ConsoleColor.Green);

            var data = JsonSerializer.Deserialize<Dictionary<int, List<BarChartData>>>(File.ReadAllTextAsync(dataPath).GetAwaiter().GetResult());

            var currentDateInBelgium = MyExtensions.BelgiumTime();
            var currentDateInBelgiumString = currentDateInBelgium.ToString("yyyyMMdd", null);

            // For JuneProcessed = false
            var listForJuneProcessed = data!
                .SelectMany(kvp => kvp.Value.Where(data => !data.J || data.P * 1000 < data.I)
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
                    var q = Scraper.GetQuarterData(new Dictionary<string, string>() { { "token", token! } }, item.Item3).GetAwaiter().GetResult();

                    if (juneData != default)
                    {
                        Alert($"Process June Data ({item.Item3})", "Info", ConsoleColor.Green);

                        var consumption = juneData.RootElement.GetProperty("electricity").GetProperty("single").GetProperty("consumption").GetDouble();
                        var injection = juneData.RootElement.GetProperty("electricity").GetProperty("single").GetProperty("injection").GetDouble() * 1000;

                        if (!data!.TryGetValue(item.Key, out List<BarChartData>? value))
                        {
                            value = [];
                            data.Add(item.Key, value);
                        }
                        if (value.FindIndex(f => f.D == item.D) == -1)
                        {
                            value.Add(new BarChartData(item.D, 0, 0, 0, false, false, new MeteoStatData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0), false, new AnomalyData(0, 0, 0, false), q!));
                        }

                        var idx = value.FindIndex(f => f.D == item.D);
                        var d = value[idx];
                        value[idx] = new BarChartData(d.D, d.P, consumption, injection, item.Item4 == currentDateInBelgium.Date ? false : true, d.S, d.MS, d.M, new AnomalyData(0, 0, 0, false), q!);
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

    public abstract class BaseRunCommand<T, U> : Command<T> where T : CommandSettings where U : class
    {
        public BaseRunCommand(IOptions<U> settings)
        {
            this.Settings = settings.Value;
        }

        public U Settings { get; }

        protected static void Alert(string message, string type, ConsoleColor cc = ConsoleColor.Red)
        {
            AnsiConsole.MarkupLine($"[bold {cc}]{type}[/]: {message}");
        }
        protected static void DetectAnomaly(Dictionary<int, List<BarChartData>> data)
        {
            var lst = data
            .SelectMany(kv => kv.Value.Where(w => w.J && w.S && w.M).Select(bcd =>
                new AnomalyRecord(
                    kv.Key,
                    bcd.D,
                    (float)bcd.P,
                    (float)bcd.U,
                    (float)bcd.I,
                    (float)bcd.MS.tavg,
                    (float)bcd.MS.tmin,
                    (float)bcd.MS.tmax,
                    (float)bcd.MS.prcp,
                    (float)bcd.MS.snow,
                    (float)bcd.MS.wdir,
                    (float)bcd.MS.wspd,
                    (float)bcd.MS.wpgt,
                    (float)bcd.MS.pres,
                    (float)bcd.MS.tsun
                )))
            .ToList();

            // Create a new ML context
            var mlContext = new MLContext();

            // Load your data
            IDataView dataView = mlContext.Data.LoadFromEnumerable<AnomalyRecord>(lst);

            var inputColums = new string[] { nameof(AnomalyRecord.P), nameof(AnomalyRecord.U), nameof(AnomalyRecord.I) };

            // Define the anomaly detection pipeline
            var pipelines = inputColums.Select(s => mlContext.Transforms.DetectIidSpike(outputColumnName: nameof(Prediction.Scores),
                                                               inputColumnName: s,
                                                               confidence: 95.0,
                                                               pvalueHistoryLength: 50)).ToList();

            //Train the model
            var trainedModels = pipelines.Select(p => p.Fit(dataView)).ToList();

            //// Define the anomaly detection pipeline
            //var pipeline = mlContext.Transforms.DetectIidSpike(outputColumnName: nameof(Prediction.Scores),
            //                                                   inputColumnName: nameof(AnomalyRecord.P),
            //                                                   confidence: 95.0,
            //                                                   pvalueHistoryLength: 50);

            //// Train the model
            //var trainedModel = pipeline.Fit(dataView);

            // Predict and find anomalies

            var transformedDatas = trainedModels.Select(tm => tm.Transform(dataView)).ToList();
            var allPredictions = transformedDatas.Select(td => mlContext.Data.CreateEnumerable<Prediction>(td, reuseRowObject: false)).ToList();

            //IDataView transformedData = trainedModel.Transform(dataView);
            //var predictions = mlContext.Data.CreateEnumerable<Prediction>(transformedData, reuseRowObject: false);

            for (int j = 0; j < inputColums.Length; j++)
            {
                foreach (var prediction in allPredictions[j].Where(w => w.Scores![0] != 0))
                {

                    var idx = data[prediction.Y].FindIndex(f => f.D == prediction.D);
                    var d = data[prediction.Y][idx];

                    double p = d.AS.P, u = d.AS.U, i = d.AS.I;

                    switch (inputColums[j])
                    {
                        case "P":
                            p = prediction.Scores[1];
                            Alert($"-> ({inputColums[j]}/{prediction.Y}/{prediction.D}/{prediction.D.DayOfYearLocalDate(prediction.Y).ToString("yyyy-MM-dd", null)}) Prediction score of: {p:F2}", "Anomaly", ConsoleColor.Yellow);
                            break;
                        case "U":
                            u = prediction.Scores[1];
                            Alert($"-> ({inputColums[j]}/{prediction.Y}/{prediction.D}/{prediction.D.DayOfYearLocalDate(prediction.Y).ToString("yyyy-MM-dd", null)}) Prediction score of: {u:F2}", "Anomaly", ConsoleColor.Yellow);
                            break;
                        case "I":
                            i = prediction.Scores[1] / 1000;
                            Alert($"-> ({inputColums[j]}/{prediction.Y}/{prediction.D}/{prediction.D.DayOfYearLocalDate(prediction.Y).ToString("yyyy-MM-dd", null)}) Prediction score of: {i:F2}", "Anomaly", ConsoleColor.Yellow);
                            break;
                        default:
                            // Handle default case if necessary
                            break;
                    }

                    data[prediction.Y][idx] = new BarChartData(d.D, d.P, d.U, d.I, d.J, d.S, d.MS, d.M, new(p, u, i, true), new QuarterData([], [], []));
                }
            }

            // Output the results

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
        [JsonPropertyName("x")]
        public string X { get; set; }

        [JsonPropertyName("y")]
        public double? Y { get; set; }
    }


}
