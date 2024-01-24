using June.Data.Commands;
using MeteoStat.Data.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using myenergy.Common;
using myenergy.Common.Extensions;
using Newtonsoft.Json.Linq;
using Spectre.Console.Cli;
using Sungrow.Data.Commands;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace June.Data
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
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

            var serviceProvider = ConfigureServices(Configuration);


            var app = new CommandApp(new TypeRegistrar(serviceProvider));

            app.Configure(config =>
            {
                config.AddBranch<BaseCommandSettings>("june", add =>
                {
                    add.AddCommand<JuneRunCommand>("run");
                });
                config.AddBranch<BaseCommandSettings>("sungrow", add =>
                {
                    add.AddCommand<SungrowRunCommand>("run");
                });
                config.AddBranch<BaseCommandSettings>("meteostat", add =>
                {
                    add.AddCommand<MeteoStatRunCommand>("run");
                });
            });

            return app.Run(args);



            
        }

        //static void DetectAnomaly(Dictionary<int, List<BarChartData>> data)
        //{
        //    var lst = data
        //    .SelectMany(kv => kv.Value.Where(w => w.J && w.S && w.M).Select(bcd =>
        //        new AnomalyRecord(
        //            kv.Key,
        //            bcd.D,
        //            (float)bcd.P,
        //            (float)bcd.U,
        //            (float)bcd.I,
        //            (float)bcd.MS.tavg,
        //            (float)bcd.MS.tmin,
        //            (float)bcd.MS.tmax,
        //            (float)bcd.MS.prcp,
        //            (float)bcd.MS.snow,
        //            (float)bcd.MS.wdir,
        //            (float)bcd.MS.wspd,
        //            (float)bcd.MS.wpgt,
        //            (float)bcd.MS.pres,
        //            (float)bcd.MS.tsun
        //        )))
        //    .ToList();

        //    // Create a new ML context
        //    var mlContext = new MLContext();

        //    // Load your data
        //    IDataView dataView = mlContext.Data.LoadFromEnumerable<AnomalyRecord>(lst);

        //    var inputColums = new string[] { nameof(AnomalyRecord.P), nameof(AnomalyRecord.U), nameof(AnomalyRecord.I) };

        //    // Define the anomaly detection pipeline
        //    var pipelines = inputColums.Select(s => mlContext.Transforms.DetectIidSpike(outputColumnName: nameof(Prediction.Scores),
        //                                                       inputColumnName: s,
        //                                                       confidence: 95.0,
        //                                                       pvalueHistoryLength: 50)).ToList();

        //    //Train the model
        //    var trainedModels = pipelines.Select(p => p.Fit(dataView)).ToList();

        //    //// Define the anomaly detection pipeline
        //    //var pipeline = mlContext.Transforms.DetectIidSpike(outputColumnName: nameof(Prediction.Scores),
        //    //                                                   inputColumnName: nameof(AnomalyRecord.P),
        //    //                                                   confidence: 95.0,
        //    //                                                   pvalueHistoryLength: 50);

        //    //// Train the model
        //    //var trainedModel = pipeline.Fit(dataView);

        //    // Predict and find anomalies

        //    var transformedDatas = trainedModels.Select(tm => tm.Transform(dataView)).ToList();
        //    var allPredictions = transformedDatas.Select(td => mlContext.Data.CreateEnumerable<Prediction>(td, reuseRowObject: false)).ToList();

        //    //IDataView transformedData = trainedModel.Transform(dataView);
        //    //var predictions = mlContext.Data.CreateEnumerable<Prediction>(transformedData, reuseRowObject: false);

        //    for (int j = 0; j < inputColums.Length; j++)
        //    {
        //        foreach (var prediction in allPredictions[j].Where(w => w.Scores![0] != 0))
        //        {

        //            var idx = data[prediction.Y].FindIndex(f => f.D == prediction.D);
        //            var d = data[prediction.Y][idx];

        //            double p = d.AS.P, u = d.AS.U, i = d.AS.I;

        //            switch (inputColums[j])
        //            {
        //                case "P":
        //                    p = prediction.Scores[1];
        //                    Alert($"-> ({inputColums[j]}/{prediction.Y}/{prediction.D}/{prediction.D.DayOfYearLocalDate(prediction.Y).ToString("yyyy-MM-dd", null)}) Prediction score of: {p:F2}", "Anomaly", ConsoleColor.Yellow);
        //                    break;
        //                case "U":
        //                    u = prediction.Scores[1];
        //                    Alert($"-> ({inputColums[j]}/{prediction.Y}/{prediction.D}/{prediction.D.DayOfYearLocalDate(prediction.Y).ToString("yyyy-MM-dd", null)}) Prediction score of: {u:F2}", "Anomaly", ConsoleColor.Yellow);
        //                    break;
        //                case "I":
        //                    i = prediction.Scores[1] / 1000;
        //                    Alert($"-> ({inputColums[j]}/{prediction.Y}/{prediction.D}/{prediction.D.DayOfYearLocalDate(prediction.Y).ToString("yyyy-MM-dd", null)}) Prediction score of: {i:F2}", "Anomaly", ConsoleColor.Yellow);
        //                    break;
        //                default:
        //                    // Handle default case if necessary
        //                    break;
        //            }

        //            data[prediction.Y][idx] = new BarChartData(d.D, d.P, d.U, d.I, d.J, d.S, d.MS, d.M, new(p, u, i, true));
        //        }
        //    }

        //    // Output the results

        //}


        //static void Alert(string message, string type, ConsoleColor cc = ConsoleColor.Red)
        //{
        //    Console.ForegroundColor = cc;
        //    Console.Write($"{type}: ");
        //    Console.ResetColor();
        //    Console.WriteLine(message);
        //}


        static IServiceProvider ConfigureServices(IConfigurationRoot configuration)
        {
            IServiceCollection services = new ServiceCollection();

            //Map the implementations of your classes here ready for DI
            _ = services
                .Configure<JuneSettings>(configuration.GetSection(nameof(JuneSettings)))
                .Configure<SungrowSettings>(configuration.GetSection(nameof(SungrowSettings)))
                .Configure<MeteoStatSettings>(configuration.GetSection(nameof(MeteoStatSettings)))
                .AddSingleton<JuneRunSettings>()
                .AddSingleton<SungrowRunSettings>()
                .AddSingleton<MeteoStatRunSettings>()
                .AddSingleton<JuneRunCommand>()
                .AddSingleton<SungrowRunCommand>()
                .AddSingleton<MeteoStatRunCommand>()
                .AddOptions()
                .BuildServiceProvider();

            return services.BuildServiceProvider();
        }

    }
}
