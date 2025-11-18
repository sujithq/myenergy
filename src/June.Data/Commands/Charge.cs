using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using June.Data.Commands.Settings;
using Microsoft.Extensions.Options;
using myenergy.Common;
using myenergy.Common.Extensions;
using NodaTime.Extensions;
using Spectre.Console.Cli;
using System.Globalization;
using System.Text.Json;

namespace June.Data.Commands
{
    public class ChargeSettings
    {
    }

    public class ChargeRunSettings : BaseCommandSettings
    {

    }

    public class ChargeRunCommand : BaseRunCommand<ChargeRunSettings, ChargeSettings>
    {
        public ChargeRunCommand(IOptions<ChargeSettings> settings) : base(settings)
        {
        }

        public override int Execute(CommandContext context, ChargeRunSettings settings, CancellationToken ct)
        {
            var dataPath = Path.Combine(AppContext.BaseDirectory, "Data/data.json");
            Alert($"Reading from {dataPath}", "Info", ConsoleColor.Green);

            var data = JsonSerializer.Deserialize<Dictionary<int, List<BarChartData>>>(File.ReadAllTextAsync(dataPath).GetAwaiter().GetResult());


            Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Data/Charging")).ToList().ForEach(file =>
            {
                Alert($"Reading from {file}", "Info", ConsoleColor.Green);

                CsvConfiguration config = CsvConfiguration.FromAttributes<Foo>();
                using (var reader = new StreamReader(file))
                using (var csv = new CsvReader(reader, config))
                {
                    //csv.Context.RegisterClassMap<FooMap>();
                    var records = csv.GetRecords<Foo>();

                    foreach (var r in records.Where(w=>w.ChargingLocation.StartsWith("Provinciesteenweg")))
                    {
                        
                        Alert($"Processing start charging session on {r.PlugInDate}", "Info", ConsoleColor.Green);

                        var doy = r.PlugInDate.ToLocalDateTime().DayOfYear;
                        var year = r.PlugInDate.Year;
                        var idx = data![year].FindIndex(f => f.D == doy);

                        if (idx != -1)
                        {
                            var d = data[year][idx];
                            data[year][idx] = new BarChartData(d.D, d.P, d.U, d.I, d.J, d.S, d.MS, d.M, d.AS, d.Q, true, d.SRS);

                            var doy2 = r.PlugInDate.Add(r.ChargedTime_).ToLocalDateTime().DayOfYear;

                            if (doy != doy2)
                            {
                                Alert($"Processing end charging session on {r.PlugInDate.Add(r.ChargedTime_)}", "Info", ConsoleColor.Green);

                                year = r.PlugInDate.Add(r.ChargedTime_).Year;
                                idx = data[year].FindIndex(f => f.D == doy2);
                                if (idx != -1)
                                {
                                    d = data[year][idx];
                                    data[year][idx] = new BarChartData(d.D, d.P, d.U, d.I, d.J, d.S, d.MS, d.M, d.AS, d.Q, true, d.SRS);
                                }
                            }
                        }

                    }

                }
            }); 

            DetectAnomaly(data!);

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "Data/data.json"), JsonSerializer.Serialize(data, JsonSerializerOptions));

            return Environment.ExitCode;
        }

        [Delimiter(";")]
        [CultureInfo("nl-BE")]
        public class Foo
        {

            //28/01/2023 23:38
            [Index(0)]
            public DateTime PlugInDate { get; set; }

            //9.331 km
            [Index(1)]
            public string TotalMileage { get; set; }
            public int TotalMileage_ { get { return int.Parse(TotalMileage.Replace(" km", string.Empty).Replace(".", string.Empty)); } }

            //23%
            [Index(2)]
            public string SocPluggedin { get; set; }
            public int SocPluggedin_ { get { return int.Parse(SocPluggedin.Replace("%", string.Empty)); } }

            //29/01/2023 14:00
            [Index(3)]
            public DateTime UnplugDate { get; set; }

            //80%
            [Index(4)]
            public string SocUnplugged { get; set; }
            public int SocUnplugged_ { get { return int.Parse(SocUnplugged.Replace("%", string.Empty)); } }

            //~ 17.69 EUR
            //[Index(5)]
            //public string Cost { get; set; }

            //~ 45 kWh
            [Index(9)]
            public string Charged { get; set; }
            public int Charged_ { get { return int.Parse(Charged.Replace("~ ", string.Empty).Replace(" kWh", string.Empty)); } }

            //4h 13min
            [Index(12)]
            public string ChargedTime { get; set; }
            public TimeSpan ChargedTime_ { get { return ChargedTime.ToTimeSpan(); } }

            [Index(5)]
            public string ChargingLocation { get; set; }


        }

        //public sealed class FooMap : ClassMap<Foo>
        //{
        //    public FooMap()
        //    {
        //        Map(m => m.PlugInDate);
        //        Map(m => m.TotalMileage);
        //        Map(m => m.SocPluggedin);
        //        Map(m => m.UnplugDate);
        //        Map(m => m.SocUnplugged);
        //        //Map(m => m.CostRaw);
        //        Map(m => m.Charged);
        //        Map(m => m.ChargedTime);
        //    }
        //}
    }

    
}
