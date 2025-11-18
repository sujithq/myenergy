using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using June.Data.Commands.Settings;
using Microsoft.Extensions.Options;
using Microsoft.ML.Transforms.Text;
using myenergy.Common;
using myenergy.Common.Extensions;
using NodaTime.Extensions;
using Spectre.Console.Cli;
using System.Globalization;
using System.Text.Json;

namespace June.Data.Commands
{
    public class WemSettings
    {
    }

    public class WemRunSettings : BaseCommandSettings
    {

    }

    public class WemRunCommand : BaseRunCommand<WemRunSettings, WemSettings>
    {
        public WemRunCommand(IOptions<WemSettings> settings) : base(settings)
        {
        }

        public override int Execute(CommandContext context, WemRunSettings settings, CancellationToken ct)
        {
            var dataPath = Path.Combine(AppContext.BaseDirectory, "Data/data.json");
            Alert($"Reading from {dataPath}", "Info", ConsoleColor.Green);

            var data = JsonSerializer.Deserialize<Dictionary<int, List<BarChartData>>>(File.ReadAllTextAsync(dataPath).GetAwaiter().GetResult());

            var allowedMinutes = new string[] { "00", "15", "30", "45" };


            Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Data/Wem")).ToList().ForEach(file =>
            {
                Alert($"Reading from {file}", "Info", ConsoleColor.Green);

                CsvConfiguration config = CsvConfiguration.FromAttributes<Foo>();
                using (var reader = new StreamReader(file))
                using (var csv = new CsvReader(reader, config))
                {
                    //csv.Context.RegisterClassMap<FooMap>();
                    var records = csv.GetRecords<Foo>();

                    foreach (var r in records.Where(w => allowedMinutes.Contains(w.RawTime.Split(':')[1])))
                    {


                        var d = r.Date.ToDateTime(r.Time);

                        Alert($"{d} -> {r.RoomTempHZK0} -> {r.OutTempHZK0} -> {r.PressureWE0}", "info");

                        var year = d.Year;
                        var doy = d.DayOfYear;
                        var idx = d.GetQuarterHourIndex();
                        var dd = data![year][doy];
                        
                        var lwrt = dd.Q.WRT;
                        var lwot = dd.Q.WOT;
                        var lwp = dd.Q.WP;

                        if(lwrt == null || lwrt.Count == 0)
                            lwrt = Enumerable.Repeat(0.0, 96).ToList();
                        if (lwot == null || lwot.Count == 0)
                            lwot = Enumerable.Repeat(0.0, 96).ToList();
                        if (lwp == null || lwp.Count == 0)
                            lwp = Enumerable.Repeat(0.0, 96).ToList();

                        lwrt[idx] = r.RoomTempHZK0;
                        lwot[idx] = r.OutTempHZK0;
                        lwp[idx] = r.PressureWE0;

                        var newQ = new QuarterData(dd.Q.C, dd.Q.I, dd.Q.G, dd.Q.P, lwrt, lwot, lwp);
                        var nbcd = new BarChartData(dd.D, dd.P, dd.U, dd.I, dd.J, dd.S, dd.MS, dd.M, dd.AS, newQ, dd.C, dd.SRS);

                        data[year][doy] = nbcd;

                        //    Alert($"Processing start charging session on {r.PlugInDate}", "Info", ConsoleColor.Green);
                        //    var doy = r.PlugInDate.ToLocalDateTime().DayOfYear;
                        //    var year = r.PlugInDate.Year;
                        //    var idx = data[year].FindIndex(f => f.D == doy);
                        //    if (idx != -1)
                        //    {
                        //        var d = data[year][idx];
                        //        data[year][idx] = new BarChartData(d.D, d.P, d.U, d.I, d.J, d.S, d.MS, d.M, d.AS, d.Q, true, d.SRS);
                        //        var doy2 = r.PlugInDate.Add(r.WemdTime_).ToLocalDateTime().DayOfYear;
                        //        if (doy != doy2)
                        //        {
                        //            Alert($"Processing end charging session on {r.PlugInDate.Add(r.WemdTime_)}", "Info", ConsoleColor.Green);
                        //            year = r.PlugInDate.Add(r.WemdTime_).Year;
                        //            idx = data[year].FindIndex(f => f.D == doy2);
                        //            if (idx != -1)
                        //            {
                        //                d = data[year][idx];
                        //                data[year][idx] = new BarChartData(d.D, d.P, d.U, d.I, d.J, d.S, d.MS, d.M, d.AS, d.Q, true, d.SRS);
                        //            }
                        //        }
                        //    }
                    }

                }
            });

            DetectAnomaly(data!);

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "Data/data.json"), JsonSerializer.Serialize(data, JsonSerializerOptions));

            return Environment.ExitCode;
        }


        

        //Date;Time;Ruimtetemperatuur 1 Actueel[HZK0];Buitentemperatuur Actueel[HZK0];Installatiedruk Actueel[WE0];Buitentemperatuur Actueel[SYSTEM0];
        //1-1-2024;00:00:00;18;9;1,5;9,1
        [Delimiter(";")]
        [CultureInfo("nl-BE")]
        public class Foo
        {

            //28/01/2023 23:38
            //[Index(0)]
            [Name("Date")]
            public string RawDate { get; set; }
            public DateOnly Date { get { return DateOnly.Parse(RawDate); } }
            //[Index(1)]
            [Name("Time")]
            public string RawTime { get; set; }
            public TimeOnly Time { get { return TimeOnly.Parse(RawTime); } }
            //[Index(2)]
            [Name("Ruimtetemperatuur 1 Actueel[HZK0]")]
            public string RawRoomTempHZK0 { get; set; }
            public double RoomTempHZK0 { get { return double.Parse(RawRoomTempHZK0); } }
            //[Index(3)]
            [Name("Buitentemperatuur Actueel[HZK0]")]
            public string RawOutTempHZK0 { get; set; }
            public double OutTempHZK0 { get { return double.Parse(RawOutTempHZK0); } }
            //[Index(4)]
            [Name("Installatiedruk Actueel[WE0]")]
            public string RawPressureWE0 { get; set; }
            public double PressureWE0 { get { return double.Parse(RawPressureWE0); } }
            //[Index(5)]
            [Name("Buitentemperatuur Actueel[SYSTEM0]")]
            public string RawOutSYS { get; set; }
            public double OutSYS { get { return double.Parse(RawOutSYS); } }


        }

        //public sealed class FooMap : ClassMap<Foo>
        //{
        //    public FooMap()
        //    {
        //        Map(m => m.RawDate);
        //        Map(m => m.RawTime);
        //        Map(m => m.RawRoomTempHZK0);
        //        Map(m => m.RawOutTempHZK0);
        //        Map(m => m.RawPressureWE0);
        //        Map(m => m.RawOutSYS);
        //    }
        //}


    }


}
