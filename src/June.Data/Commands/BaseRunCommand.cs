using Microsoft.Extensions.Options;
using Microsoft.ML;
using myenergy.Common;
using myenergy.Common.Extensions;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;

namespace June.Data.Commands
{
    public abstract class BaseRunCommand<T, U> : Command<T> where T : CommandSettings where U : class
    {

        protected JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions { WriteIndented = true };

        public BaseRunCommand(IOptions<U> settings)
        {
            this.Settings = settings.Value;
        }

        public U Settings { get; }

        protected static void Alert(string message, string type, ConsoleColor cc = ConsoleColor.Red)
        {
            // AnsiConsole.MarkupLine($"[bold {cc}]{type}[/]: {message}");
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
                    (float)bcd.MS.tsun,
                    bcd.C
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

            // Predict and find anomalies

            var transformedDatas = trainedModels.Select(tm => tm.Transform(dataView)).ToList();
            var allPredictions = transformedDatas.Select(td => mlContext.Data.CreateEnumerable<Prediction>(td, reuseRowObject: false)).ToList();

            for (int j = 0; j < inputColums.Length; j++)
            {
                foreach (var prediction in allPredictions[j].Where(w => w.Scores![0] != 0))
                {

                    var idx = data[prediction.Y].FindIndex(f => f.D == prediction.D);
                    var d = data[prediction.Y][idx];

                    double p = d.AS.P, u = d.AS.U, i = d.AS.I;

                    var s = prediction.Scores?[1] ?? 0;

                    switch (inputColums[j])
                    {
                        case "P":
                            p = s;
                            Alert($"-> ({inputColums[j]}/{prediction.Y}/{prediction.D}/{prediction.D.DayOfYearLocalDate(prediction.Y).ToString("yyyy-MM-dd", null)}) Prediction score of: {p:F2}", "Anomaly", ConsoleColor.Yellow);
                            break;
                        case "U":
                            u = s;
                            Alert($"-> ({inputColums[j]}/{prediction.Y}/{prediction.D}/{prediction.D.DayOfYearLocalDate(prediction.Y).ToString("yyyy-MM-dd", null)}) Prediction score of: {u:F2}", "Anomaly", ConsoleColor.Yellow);
                            break;
                        case "I":
                            i = s / 1000;
                            Alert($"-> ({inputColums[j]}/{prediction.Y}/{prediction.D}/{prediction.D.DayOfYearLocalDate(prediction.Y).ToString("yyyy-MM-dd", null)}) Prediction score of: {i:F2}", "Anomaly", ConsoleColor.Yellow);
                            break;
                        default:
                            // Handle default case if necessary
                            break;
                    }

                    data[prediction.Y][idx] = new BarChartData(d.D, d.P, d.U, d.I, d.J, d.S, d.MS, d.M, new(p, u, i, true), d.Q, d.C, d.SRS);
                }
            }

            DetectAnomalyQuarterData(data!);

        }

        private static void DetectAnomalyQuarterData(Dictionary<int, List<BarChartData>> data)
        {
            var lists = new List<List<AnomalyQuarterRecord>>();

            lists.Add(data
            .SelectMany(kv => kv.Value//.Where(w => w.J && w.S && w.M)
            .SelectMany(bcd => bcd.Q.C.Select((v, idx) => (kv.Key, bcd, "C", v, idx))
            ))

            .Select((tuple, index) =>
            {
                var (key, bcd, listType, value, idx) = tuple;

                var dateToCheck = new DateTime(key, 1, 1).AddDays(bcd.D - 1).AddMinutes(15 * idx);
                var sunShines = bcd.SRS != null ? dateToCheck >= bcd.SRS.R && dateToCheck <= bcd.SRS.S : false;

                return new AnomalyQuarterRecord(
                    key,
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
                    (float)bcd.MS.tsun,
                    bcd.C,
                    (float)value,
                    //dateTime, 
                    idx,
                    tuple.Item3,
                    //bcd.SRS != null ? bcd.SRS.R : default,
                    //bcd.SRS != null ? bcd.SRS.S : default, 
                    sunShines
                );
            })
            .ToList());

            lists.Add(data
            .SelectMany(kv => kv.Value//.Where(w => w.J && w.S && w.M)
            .SelectMany(bcd => bcd.Q.I.Select((v, idx) => (kv.Key, bcd, "I", v, idx))
            ))

            .Select((tuple, index) =>
            {
                var (key, bcd, listType, value, idx) = tuple;

                var dateToCheck = new DateTime(key, 1, 1).AddDays(bcd.D - 1).AddMinutes(15 * idx);
                var sunShines = bcd.SRS != null ? dateToCheck >= bcd.SRS.R && dateToCheck <= bcd.SRS.S : false;

                return new AnomalyQuarterRecord(
                    key,
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
                    (float)bcd.MS.tsun,
                    bcd.C,
                    (float)value,
                    //dateTime, 
                    idx,
                    tuple.Item3,
                    //bcd.SRS != null ? bcd.SRS.R : default,
                    //bcd.SRS != null ? bcd.SRS.S : default,
                    sunShines
                );
            })
            .ToList());

            lists.Add(data
            .SelectMany(kv => kv.Value//.Where(w => w.J && w.S && w.M)
            .SelectMany(bcd => bcd.Q.G.Select((v, idx) => (kv.Key, bcd, "G", v, idx))
            ))

            .Select((tuple, index) =>
            {
                var (key, bcd, listType, value, idx) = tuple;

                var dateToCheck = new DateTime(key, 1, 1).AddDays(bcd.D - 1).AddMinutes(15 * idx);
                var sunShines = bcd.SRS != null ? dateToCheck >= bcd.SRS.R && dateToCheck <= bcd.SRS.S : false;

                return new AnomalyQuarterRecord(
                    key,
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
                    (float)bcd.MS.tsun,
                    bcd.C,
                    (float)value,
                    //dateTime, 
                    idx,
                    tuple.Item3,
                    //bcd.SRS != null ? bcd.SRS.R : default,
                    //bcd.SRS != null ? bcd.SRS.S : default,
                    sunShines
                );
            })
            .ToList());

            lists.Add(data
            .SelectMany(kv => kv.Value//.Where(w => w.J && w.S && w.M)
            .SelectMany(bcd => bcd.Q.P.Select((v, idx) => (kv.Key, bcd, "P", v, idx))
            ))

            .Select((tuple, index) =>
            {
                var (key, bcd, listType, value, idx) = tuple;

                var dateToCheck = new DateTime(key, 1, 1).AddDays(bcd.D - 1).AddMinutes(15 * idx);
                var sunShines = bcd.SRS != null ? dateToCheck >= bcd.SRS.R && dateToCheck <= bcd.SRS.S : false;

                return new AnomalyQuarterRecord(
                    key,
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
                    (float)bcd.MS.tsun,
                    bcd.C,
                    (float)value,
                    //dateTime, 
                    idx,
                    tuple.Item3,
                    //bcd.SRS != null ? bcd.SRS.R : default, 
                    //bcd.SRS != null ? bcd.SRS.S : default,
                    sunShines
                );
            })
            .ToList());


            foreach (var lst in lists)
            {
                // Create a new ML context
                var mlContext = new MLContext();

                // Load your data
                IDataView dataView = mlContext.Data.LoadFromEnumerable<AnomalyQuarterRecord>(lst);

                var inputColums = new string[] { nameof(AnomalyQuarterRecord.VV) };

                // Define the anomaly detection pipeline
                var pipelines = inputColums.Select(s => mlContext.Transforms.DetectIidSpike(outputColumnName: nameof(PredictionQuarter.Scores),
                                                                   inputColumnName: s,
                                                                   confidence: 95.0,
                                                                   pvalueHistoryLength: 50)).ToList();

                //Train the model
                var trainedModels = pipelines.Select(p => p.Fit(dataView)).ToList();

                // Predict and find anomalies

                var transformedDatas = trainedModels.Select(tm => tm.Transform(dataView)).ToList();
                var allPredictions = transformedDatas.Select(td => mlContext.Data.CreateEnumerable<PredictionQuarter>(td, reuseRowObject: false)).ToList();

                for (int j = 0; j < inputColums.Length; j++)
                {
                    foreach (var prediction in allPredictions[j].Where(w => w.Scores![0] != 0))
                    {

                        var idx = data[prediction.Y].FindIndex(f => f.D == prediction.D);
                        var d = data[prediction.Y][idx];

                        var date = new DateTime(prediction.Y, 1, 1).AddDays(prediction.D - 1).AddMinutes(15 * prediction.IDX);


                        var p = prediction.Scores?[1] ?? 0;
                        Alert($"-> ({prediction.T}/{prediction.Y}/{prediction.D}/{date.ToString("yyyy-MM-dd H:mm", null)}) Prediction score of: {p:F3} ({prediction.IDX})", "Anomaly Quarter", ConsoleColor.Yellow);
                    }
                }

            }
            // Output the results

        }
    }


}
