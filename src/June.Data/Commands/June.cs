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

namespace June.Data.Commands
{
    public class BaseCommandSettings : CommandSettings
    {
    }

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
    public class JuneRunCommand : BaseRunCommand<JuneRunSettings>
    {
        public JuneRunCommand(IOptions<JuneSettings> juneSettings, IOptions<SungrowSettings> sungrowSettings, IOptions<MeteoStatSettings> meteoStatSettings):base(juneSettings, sungrowSettings, meteoStatSettings)
        {
        }
        public override int Execute(CommandContext context, JuneRunSettings settings)
        {
            var failed = false;

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

            // For SungrowProcessed = false
            var listForSungrowProcessed = data!
                .SelectMany(kvp => kvp.Value.Where(data => !data.S || data.P < data.I / 1000.0)
                                            .Select(data => (kvp.Key, data.D, data.D.DayOfYearLocalDate(kvp.Key)))
                .Where(date => date.Item3 <= currentDateInBelgium.Date)
                .Select(date => (date.Key, date.D, date.Item3.ToString("yyyyMMdd", null), date.Item3)))
                .ToList();

            if (listForSungrowProcessed.FindIndex(f => f.Key == currentDateInBelgium.Year && f.D == currentDateInBelgium.DayOfYear) == -1)
                listForSungrowProcessed.Add((currentDateInBelgium.Year, currentDateInBelgium.DayOfYear, currentDateInBelgiumString, currentDateInBelgium.Date));

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

            var meteoStatScraper = new MeteoStatScraper(meteoStatSettings);

            var mdata = meteoStatScraper.GetData(new Dictionary<string, string>() { { "start", meteoStatStart! }, { "end", meteoStatEnd! } }, default).GetAwaiter().GetResult();

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
                            value.Add(new BarChartData(item.D, 0, 0, 0, false, false, msd, false, new AnomalyData(0, 0, 0, false)));
                        }

                        var idx = value.FindIndex(f => f.D == item.D);
                        var d = value[idx];
                        value[idx] = new BarChartData(d.D, d.P, d.U, d.I, d.J, d.S, msd, item.Item3 == currentDateInBelgium.Date ? false : true, new AnomalyData(0, 0, 0, false));

                    }
                }
            }
            else
            {
                Alert("No Meteo Stat Data To Process", "Warning");
            }

            var juneScraper = new JuneScraper(juneSettings);
            var juneLogin = juneScraper.LoginAsync().GetAwaiter().GetResult();
            if (juneLogin != default)
            {
                var token_name = "access_token";
                var token = juneLogin!.RootElement.GetProperty(token_name).GetString();

                foreach (var item in listForJuneProcessed)
                {
                    var juneData = juneScraper.GetData(new Dictionary<string, string>() { { "token", token! } }, item.Item3).GetAwaiter().GetResult();

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
                            value.Add(new BarChartData(item.D, 0, 0, 0, false, false, new MeteoStatData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0), false, new AnomalyData(0, 0, 0, false)));
                        }

                        var idx = value.FindIndex(f => f.D == item.D);
                        var d = value[idx];
                        value[idx] = new BarChartData(d.D, d.P, consumption, injection, item.Item4 == currentDateInBelgium.Date ? false : true, d.S, d.MS, d.M, new AnomalyData(0, 0, 0, false));
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

            var sungrowScraper = new SungrowScraper(sungrowSettings);
            var sungrowLogin = sungrowScraper.LoginAsync().GetAwaiter().GetResult();

            if (sungrowLogin != default)
            {
                var token = sungrowLogin.RootElement.GetProperty("result_data").GetProperty("token").GetString();
                var user_id = sungrowLogin.RootElement.GetProperty("result_data").GetProperty("user_id").GetString();

                foreach (var item in listForSungrowProcessed)
                {
                    var sungrowData = sungrowScraper.GetData(new Dictionary<string, string>() { { "token", token! }, { "user_id", user_id! } }, item.Item3).GetAwaiter().GetResult();

                    if (sungrowData != default)
                    {
                        Alert($"Process Sungrow Data ({item.Item3})", "Info", ConsoleColor.Green);


                        var result_data = sungrowData.RootElement.GetProperty("result_data");
                        if (result_data.ValueKind == JsonValueKind.Null)
                        {
                            Alert($"({sungrowData.RootElement.GetProperty("result_code").GetString()}) {sungrowData.RootElement.GetProperty("result_msg").GetString()}", "Warning");
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
                            if (value.FindIndex(f => f.D == item.D) == -1)
                            {
                                value.Add(new BarChartData(item.D, 0, 0, 0, false, false, new MeteoStatData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0), false, new AnomalyData(0, 0, 0, false)));
                            }

                            var idx = value.FindIndex(f => f.D == item.D);
                            var d = value[idx];
                            value[idx] = new BarChartData(d.D, production, d.U, d.I, d.J, item.Item4 == currentDateInBelgium.Date ? false : true, d.MS, d.M, new AnomalyData(0, 0, 0, false));
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

    public abstract class BaseRunCommand<T> : Command<JuneRunSettings> where T : CommandSettings
    {
        public BaseRunCommand(IOptions<JuneSettings> juneSettings, IOptions<SungrowSettings> sungrowSettings, IOptions<MeteoStatSettings> meteoStatSettings)
        {
            this.juneSettings = juneSettings.Value;
            this.sungrowSettings = sungrowSettings.Value;
            this.meteoStatSettings = meteoStatSettings.Value;
        }

        public JuneSettings juneSettings { get; }
        public SungrowSettings sungrowSettings { get; }
        public MeteoStatSettings meteoStatSettings { get; }

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

                    data[prediction.Y][idx] = new BarChartData(d.D, d.P, d.U, d.I, d.J, d.S, d.MS, d.M, new(p, u, i, true));
                }
            }

            // Output the results

        }
    }


}
