namespace myenergy.Models;

public record EnergyPoint(DateTime Ts, double Prod, double Cons, double Import, double Export);

public record BarChartData(int D, double P, double U, double I, bool J, bool S, MeteoStatData MS, bool M, AnomalyData AS, QuarterData Q, bool C = false, SunRiseSet SRS = default!);

public record AnomalyData(double P, double U, double I, bool A);

public record QuarterData(List<double> C, List<double> I, List<double> G, List<double> P, List<double> WRT, List<double> WOT, List<double> WP);

public record QuarterHourData(DateTime Time, double GridImport, double Injection, double GasUsage, double SolarProduction)
{
    // Data structure relationships (verified by comparing daily totals to quarter sums):
    // - P (daily production) = sum(Q.P) / 4  →  Q.P is in kW, needs /4 for 15-min kWh
    // - U (daily grid import) = sum(Q.C)     →  Q.C is already in kWh
    // - I (daily injection Wh) = sum(Q.I) × 1000  →  Q.I is already in kWh
    // - G (daily gas) = sum(Q.G)             →  Q.G is already in m³
    //
    // IMPORTANT: Total Consumption is NOT stored in raw data, it's calculated!
    // Energy balance: Consumption = Production + GridImport - GridExport
    
    public double ActualProduction => SolarProduction / 4.0;  // Convert kW to kWh
    public double ActualGridImport => GridImport;              // Already in kWh (from grid)
    public double ActualInjection => Injection;                // Already in kWh (to grid)
    public double ActualGasUsage => GasUsage;                  // Already in m³
    
    // CALCULATED: Total Consumption = What you produced + What you imported - What you exported
    public double TotalConsumption => ActualProduction + ActualGridImport - ActualInjection;
    public double NetGrid => ActualGridImport - ActualInjection;
};

public record DailyDetailData(DateTime Date, List<QuarterHourData> QuarterHours, SunRiseSet SunTimes, MeteoStatData Weather)
{
    public double TotalProduction => QuarterHours.Sum(q => q.ActualProduction);
    
    // CALCULATED: Consumption is not stored, it's derived from Production + Import - Export
    public double TotalConsumption => QuarterHours.Sum(q => q.TotalConsumption);
    
    public double TotalGridImport => QuarterHours.Sum(q => q.ActualGridImport);
    public double TotalInjection => QuarterHours.Sum(q => q.ActualInjection);
    public double TotalGasUsage => QuarterHours.Sum(q => q.ActualGasUsage);
    public double PeakProduction => QuarterHours.Max(q => q.SolarProduction);
    public double PeakConsumption => QuarterHours.Max(q => q.TotalConsumption);
    public double PeakGas => QuarterHours.Max(q => q.GasUsage);
}

public record SunRiseSet(DateTime R, DateTime S);

public record MeteoStatData(double tavg, double tmin, double tmax, double prcp, double snow, double wdir, double wspd, double wpgt, double pres, double tsun);

public record DailySummary(DateTime Date, double Production, double Consumption, double Import, double Export)
{
    public double SelfConsumption => Production > 0 ? (Production - Export) : 0;
    public double Autarky => Consumption > 0 ? ((Consumption - Import) / Consumption * 100) : 0;
}

public record MonthlySummary(int Year, int Month, double Production, double Consumption, double Import, double Export, int DayCount)
{
    public double AvgDailyProduction => DayCount > 0 ? Production / DayCount : 0;
    public double SelfConsumptionPercent => Production > 0 ? ((Production - Export) / Production * 100) : 0;
    public double AutarkyPercent => Consumption > 0 ? ((Consumption - Import) / Consumption * 100) : 0;
}

public record WeeklySummary(int Year, int Week, DateTime StartDate, DateTime EndDate, double Production, double Consumption, double Import, double Export, int DayCount)
{
    public double AvgDailyProduction => DayCount > 0 ? Production / DayCount : 0;
    public double SelfConsumptionPercent => Production > 0 ? ((Production - Export) / Production * 100) : 0;
    public double AutarkyPercent => Consumption > 0 ? ((Consumption - Import) / Consumption * 100) : 0;
}

public record QuarterlySummary(int Year, int Quarter, double Production, double Consumption, double Import, double Export, int DayCount)
{
    public double AvgDailyProduction => DayCount > 0 ? Production / DayCount : 0;
    public double SelfConsumptionPercent => Production > 0 ? ((Production - Export) / Production * 100) : 0;
    public double AutarkyPercent => Consumption > 0 ? ((Consumption - Import) / Consumption * 100) : 0;
    public string QuarterName => $"Q{Quarter}";
}

public record YearlySummary(int Year, double Production, double Consumption, double Import, double Export, int DayCount)
{
    public double AvgDailyProduction => DayCount > 0 ? Production / DayCount : 0;
    public double SelfConsumptionPercent => Production > 0 ? ((Production - Export) / Production * 100) : 0;
    public double AutarkyPercent => Consumption > 0 ? ((Consumption - Import) / Consumption * 100) : 0;
}

public enum PeriodType
{
    Daily,
    Weekly, 
    Monthly,
    Quarterly,
    Yearly,
    Total
}

public record PeriodDataPoint(string Label, DateTime Date, double Production, double Consumption, double Import, double Export, double AutarkyPercent, double SelfConsumptionPercent);

public record DailyDetail
{
    public DateTime Date { get; init; }
    public double Production { get; init; }
    public double Consumption { get; init; }
    public double Import { get; init; }
    public double Export { get; init; }
    public double Balance => Production - Consumption;
    public double AutarkyPercent { get; init; }
    public double SelfConsumptionPercent { get; init; }
    public double PeakProduction { get; init; }
    public double PeakConsumption { get; init; }
    public TimeSpan SunriseTime { get; init; }
    public TimeSpan SunsetTime { get; init; }
    public double DaylightHours => SunsetTime.TotalHours - SunriseTime.TotalHours;
}
