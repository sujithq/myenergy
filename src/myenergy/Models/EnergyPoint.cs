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

// ODS (Elia Grid) Pricing Data - 15-minute intervals
public record OdsPricing
{
    public DateTime DateTime { get; init; }
    
    // Injection prices (when you export to grid) - in €/MWh
    public double? DownwardAvailableAfrrPrice { get; init; }  // aFRR injection price
    public double? DownwardAvailableMfrrPrice { get; init; }  // mFRR injection price
    
    // Grid import prices (when you use from grid) - in €/MWh
    public double? UpwardAvailableAfrrPrice { get; init; }    // aFRR import price
    public double? UpwardAvailableMfrrPrice { get; init; }    // mFRR import price
    
    public string ResolutionCode { get; init; } = "PT15M";
    
    // Best available prices (use aFRR if available, fallback to mFRR)
    public double InjectionPrice => DownwardAvailableAfrrPrice ?? DownwardAvailableMfrrPrice ?? 0;
    public double ImportPrice => UpwardAvailableAfrrPrice ?? UpwardAvailableMfrrPrice ?? 0;
    
    // Convert €/MWh to €/kWh
    public double InjectionPricePerKwh => InjectionPrice / 1000.0;
    public double ImportPricePerKwh => ImportPrice / 1000.0;
}

// Battery state and configuration
public record BatteryConfig(double CapacityKwh, double MaxChargeRateKw, double MaxDischargeRateKw, double Efficiency = 0.95)
{
    public double UsableCapacity => CapacityKwh * 0.9; // 90% DOD typical for lithium
}

public record BatteryState(double ChargeLevel, double ChargeRate, double DischargeRate)
{
    public double StateOfCharge => ChargeLevel;
}

// Simulation results for a single 15-minute interval
public record IntervalSimulation
{
    public DateTime Time { get; init; }
    public double Production { get; init; }
    public double Consumption { get; init; }
    
    // Without battery
    public double GridImportNoBattery { get; init; }
    public double GridExportNoBattery { get; init; }
    
    // With battery
    public double BatteryCharge { get; init; }        // Energy stored to battery (kWh)
    public double BatteryDischarge { get; init; }     // Energy taken from battery (kWh)
    public double BatteryLevel { get; init; }         // Current battery charge level (kWh)
    public double BatterySoC { get; init; }           // State of charge (%)
    public double GridImportWithBattery { get; init; }
    public double GridExportWithBattery { get; init; }
    
    // Pricing (dynamic)
    public double ImportPrice { get; init; }          // €/kWh
    public double ExportPrice { get; init; }          // €/kWh
    
    // Costs
    public double CostNoBatteryDynamic { get; init; }
    public double CostWithBatteryDynamic { get; init; }
    public double CostNoBatteryFixed { get; init; }
    public double CostWithBatteryFixed { get; init; }
    
    public double SavingsDynamic => CostNoBatteryDynamic - CostWithBatteryDynamic;
    public double SavingsFixed => CostNoBatteryFixed - CostWithBatteryFixed;
}

// Daily simulation summary
public record DailySimulation
{
    public DateTime Date { get; init; }
    public List<IntervalSimulation> Intervals { get; init; } = new();
    
    // Totals without battery
    public double TotalImportNoBattery => Intervals.Sum(i => i.GridImportNoBattery);
    public double TotalExportNoBattery => Intervals.Sum(i => i.GridExportNoBattery);
    
    // Totals with battery
    public double TotalImportWithBattery => Intervals.Sum(i => i.GridImportWithBattery);
    public double TotalExportWithBattery => Intervals.Sum(i => i.GridExportWithBattery);
    public double TotalBatteryCharged => Intervals.Sum(i => i.BatteryCharge);
    public double TotalBatteryDischarged => Intervals.Sum(i => i.BatteryDischarge);
    
    // Costs - Dynamic pricing
    public double CostNoBatteryDynamic => Intervals.Sum(i => i.CostNoBatteryDynamic);
    public double CostWithBatteryDynamic => Intervals.Sum(i => i.CostWithBatteryDynamic);
    public double SavingsDynamic => CostNoBatteryDynamic - CostWithBatteryDynamic;
    
    // Costs - Fixed pricing
    public double CostNoBatteryFixed => Intervals.Sum(i => i.CostNoBatteryFixed);
    public double CostWithBatteryFixed => Intervals.Sum(i => i.CostWithBatteryFixed);
    public double SavingsFixed => CostNoBatteryFixed - CostWithBatteryFixed;
    
    // Battery efficiency loss
    public double BatteryLoss => TotalBatteryCharged - TotalBatteryDischarged;
}

// Full simulation results
public record SimulationResults
{
    public int Year { get; init; }
    public BatteryConfig BatteryConfig { get; init; } = new(0, 0, 0);
    public double FixedImportPrice { get; init; }  // €/kWh
    public double FixedExportPrice { get; init; }  // €/kWh
    
    public List<DailySimulation> DailyResults { get; init; } = new();
    
    // Annual totals
    public double TotalCostNoBatteryDynamic => DailyResults.Sum(d => d.CostNoBatteryDynamic);
    public double TotalCostWithBatteryDynamic => DailyResults.Sum(d => d.CostWithBatteryDynamic);
    public double TotalSavingsDynamic => TotalCostNoBatteryDynamic - TotalCostWithBatteryDynamic;
    
    public double TotalCostNoBatteryFixed => DailyResults.Sum(d => d.CostNoBatteryFixed);
    public double TotalCostWithBatteryFixed => DailyResults.Sum(d => d.CostWithBatteryFixed);
    public double TotalSavingsFixed => TotalCostNoBatteryFixed - TotalCostWithBatteryFixed;
    
    // Dynamic vs Fixed comparison
    public double DynamicAdvantageNoBattery => TotalCostNoBatteryFixed - TotalCostNoBatteryDynamic;
    public double DynamicAdvantageWithBattery => TotalCostWithBatteryFixed - TotalCostWithBatteryDynamic;
    
    public double TotalBatteryThroughput => DailyResults.Sum(d => d.TotalBatteryCharged);
    public double TotalBatteryLoss => DailyResults.Sum(d => d.BatteryLoss);
}

// Investment tracking for ROI calculations
public record Investment
{
    public string Name { get; init; } = string.Empty;
    public DateTime InstallationDate { get; init; }
    public double Cost { get; init; }  // €
    public string Description { get; init; } = string.Empty;
}

public record SolarInvestment : Investment
{
    public double SystemSizeKw { get; init; }
    public double EstimatedAnnualProductionKwh { get; init; }
}

public record BatteryInvestment : Investment
{
    public double CapacityKwh { get; init; }
    public double MaxChargeRateKw { get; init; }
}

// ROI calculation results
public record RoiAnalysis
{
    public DateTime AnalysisStartDate { get; init; }
    public DateTime AnalysisEndDate { get; init; }
    
    // Solar investment
    public SolarInvestment? Solar { get; init; }
    public double SolarSavingsToDate { get; init; }
    public double SolarNetPosition { get; init; }  // Savings - Investment
    public DateTime? SolarBreakEvenDate { get; init; }
    public int? SolarPaybackMonths { get; init; }
    
    // Battery investment
    public BatteryInvestment? Battery { get; init; }
    public double BatterySavingsToDate { get; init; }
    public double BatteryNetPosition { get; init; }  // Savings - Investment
    public DateTime? BatteryBreakEvenDate { get; init; }
    public int? BatteryPaybackMonths { get; init; }
    
    // Combined
    public double TotalInvestment => (Solar?.Cost ?? 0) + (Battery?.Cost ?? 0);
    public double TotalSavings => SolarSavingsToDate + BatterySavingsToDate;
    public double CombinedNetPosition => TotalSavings - TotalInvestment;
    public DateTime? CombinedBreakEvenDate { get; init; }
    public int? CombinedPaybackMonths { get; init; }
    
    // Daily data for charting
    public List<DailyRoiData> DailyData { get; init; } = new();
}

// Daily ROI tracking
public record DailyRoiData
{
    public DateTime Date { get; init; }
    
    // Daily costs/savings
    public double CostWithoutSolar { get; init; }      // What you would have paid
    public double CostWithSolar { get; init; }          // What you paid with solar
    public double CostWithSolarAndBattery { get; init; } // What you paid with both
    
    // Daily savings
    public double SolarDailySavings { get; init; }
    public double BatteryDailySavings { get; init; }
    public double TotalDailySavings { get; init; }
    
    // Cumulative
    public double SolarCumulativeSavings { get; init; }
    public double BatteryCumulativeSavings { get; init; }
    public double TotalCumulativeSavings { get; init; }
    
    // Net position (cumulative savings - investment)
    public double SolarNetPosition { get; init; }
    public double BatteryNetPosition { get; init; }
    public double CombinedNetPosition { get; init; }
}
