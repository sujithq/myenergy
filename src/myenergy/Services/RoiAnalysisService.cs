using myenergy.Models;

namespace myenergy.Services;

public class RoiAnalysisService
{
    private readonly EnergyDataService _energyService;
    private readonly BatterySimulationService _simulationService;

    public RoiAnalysisService(EnergyDataService energyService, BatterySimulationService simulationService)
    {
        _energyService = energyService;
        _simulationService = simulationService;
    }

    public async Task<RoiAnalysis> CalculateRoi(
        SolarInvestment? solarInvestment,
        BatteryInvestment? batteryInvestment,
        DateTime analysisStartDate,
        DateTime analysisEndDate,
        double fixedImportPrice,
        double fixedExportPrice,
        bool useDynamicPricing)
    {
        await _energyService.LoadDataAsync();
        
        var dailyData = new List<DailyRoiData>();
        var availableDates = _energyService.GetAvailableDates()
            .Where(d => d >= analysisStartDate && d <= analysisEndDate)
            .OrderBy(d => d)
            .ToList();

        double solarCumulativeSavings = 0;
        double batteryCumulativeSavings = 0;
        
        DateTime? solarBreakEven = null;
        DateTime? batteryBreakEven = null;
        DateTime? combinedBreakEven = null;

        // Pre-run battery simulations for all years (MUCH faster than per-day)
        var batterySimulations = new Dictionary<int, SimulationResults>();
        if (batteryInvestment != null)
        {
            var years = availableDates.Select(d => d.Year).Distinct().ToList();
            foreach (var year in years)
            {
                var sim = await _simulationService.RunSimulation(
                    year,
                    batteryInvestment.CapacityKwh,
                    fixedImportPrice,
                    fixedExportPrice);
                batterySimulations[year] = sim;
            }
        }

        foreach (var date in availableDates)
        {
            var dailyDetail = _energyService.GetDailyDetailData(date);
            if (dailyDetail == null || !dailyDetail.QuarterHours.Any())
                continue;

            // Determine which investments are active on this date
            var solarActive = solarInvestment != null && date >= solarInvestment.InstallationDate;
            var batteryActive = batteryInvestment != null && date >= batteryInvestment.InstallationDate;

            // Calculate baseline cost (no solar, no battery - pure grid consumption)
            var baselineCost = CalculateBaselineCost(dailyDetail, fixedImportPrice);

            // Calculate cost with solar only (if active)
            var solarCost = solarActive 
                ? CalculateCostWithSolar(dailyDetail, fixedImportPrice, fixedExportPrice, useDynamicPricing, date)
                : baselineCost;

            // Calculate cost with solar + battery (if both active)
            var solarAndBatteryCost = solarCost; // Default to solar-only cost
            if (batteryActive && solarActive && batterySimulations.TryGetValue(date.Year, out var yearSim))
            {
                var dayResult = yearSim.DailyResults.FirstOrDefault(d => d.Date.Date == date.Date);
                if (dayResult != null)
                {
                    solarAndBatteryCost = useDynamicPricing 
                        ? dayResult.CostWithBatteryDynamic
                        : dayResult.CostWithBatteryFixed;
                }
            }

            // Daily savings
            var solarDailySavings = solarActive ? (baselineCost - solarCost) : 0;
            // Battery savings only when both solar AND battery are active (battery needs solar to work)
            var batteryDailySavings = (batteryActive && solarActive) ? (solarCost - solarAndBatteryCost) : 0;
            var totalDailySavings = baselineCost - solarAndBatteryCost;

            // DEBUG: Log when battery is active and savings are negative or zero
            if (batteryActive && solarActive && batteryDailySavings <= 0)
            {
                Console.WriteLine($"Date: {date:yyyy-MM-dd}");
                Console.WriteLine($"  Baseline Cost: €{baselineCost:F2}");
                Console.WriteLine($"  Solar Cost: €{solarCost:F2}");
                Console.WriteLine($"  Solar+Battery Cost: €{solarAndBatteryCost:F2}");
                Console.WriteLine($"  Solar Savings: €{solarDailySavings:F2}");
                Console.WriteLine($"  Battery Savings: €{batteryDailySavings:F2} {(batteryDailySavings < 0 ? "❌" : "⚠️")}");
                Console.WriteLine();
            }

            // Cumulative savings
            solarCumulativeSavings += solarDailySavings;
            batteryCumulativeSavings += batteryDailySavings;

            // Net positions (savings - investment cost)
            var solarNetPosition = solarCumulativeSavings - (solarInvestment?.Cost ?? 0);
            var batteryNetPosition = batteryCumulativeSavings - (batteryInvestment?.Cost ?? 0);
            var combinedNetPosition = (solarCumulativeSavings + batteryCumulativeSavings) - 
                                     ((solarInvestment?.Cost ?? 0) + (batteryInvestment?.Cost ?? 0));

            // Check for break-even dates
            if (solarBreakEven == null && solarNetPosition >= 0 && solarInvestment != null)
                solarBreakEven = date;
            
            if (batteryBreakEven == null && batteryNetPosition >= 0 && batteryInvestment != null)
                batteryBreakEven = date;
            
            if (combinedBreakEven == null && combinedNetPosition >= 0)
                combinedBreakEven = date;

            dailyData.Add(new DailyRoiData
            {
                Date = date,
                CostWithoutSolar = baselineCost,
                CostWithSolar = solarCost,
                CostWithSolarAndBattery = solarAndBatteryCost,
                SolarDailySavings = solarDailySavings,
                BatteryDailySavings = batteryDailySavings,
                TotalDailySavings = totalDailySavings,
                SolarCumulativeSavings = solarCumulativeSavings,
                BatteryCumulativeSavings = batteryCumulativeSavings,
                TotalCumulativeSavings = solarCumulativeSavings + batteryCumulativeSavings,
                SolarNetPosition = solarNetPosition,
                BatteryNetPosition = batteryNetPosition,
                CombinedNetPosition = combinedNetPosition
            });
        }

        // Calculate payback periods in months
        int? solarPaybackMonths = solarBreakEven.HasValue && solarInvestment != null
            ? CalculateMonthsDiff(solarInvestment.InstallationDate, solarBreakEven.Value)
            : null;

        int? batteryPaybackMonths = batteryBreakEven.HasValue && batteryInvestment != null
            ? CalculateMonthsDiff(batteryInvestment.InstallationDate, batteryBreakEven.Value)
            : null;

        int? combinedPaybackMonths = combinedBreakEven.HasValue
            ? CalculateMonthsDiff(
                solarInvestment?.InstallationDate ?? batteryInvestment!.InstallationDate,
                combinedBreakEven.Value)
            : null;

        return new RoiAnalysis
        {
            AnalysisStartDate = analysisStartDate,
            AnalysisEndDate = analysisEndDate,
            Solar = solarInvestment,
            SolarSavingsToDate = solarCumulativeSavings,
            SolarNetPosition = solarCumulativeSavings - (solarInvestment?.Cost ?? 0),
            SolarBreakEvenDate = solarBreakEven,
            SolarPaybackMonths = solarPaybackMonths,
            Battery = batteryInvestment,
            BatterySavingsToDate = batteryCumulativeSavings,
            BatteryNetPosition = batteryCumulativeSavings - (batteryInvestment?.Cost ?? 0),
            BatteryBreakEvenDate = batteryBreakEven,
            BatteryPaybackMonths = batteryPaybackMonths,
            CombinedBreakEvenDate = combinedBreakEven,
            CombinedPaybackMonths = combinedPaybackMonths,
            DailyData = dailyData
        };
    }

    private double CalculateBaselineCost(DailyDetailData dailyDetail, double importPrice)
    {
        // Baseline: No solar, must import all consumption
        var totalConsumption = dailyDetail.TotalConsumption;
        return totalConsumption * importPrice;
    }

    private double CalculateCostWithSolar(
        DailyDetailData dailyDetail, 
        double importPrice, 
        double exportPrice,
        bool useDynamic,
        DateTime date)
    {
        // With solar: Import what you need, export surplus
        var production = dailyDetail.TotalProduction;
        var consumption = dailyDetail.TotalConsumption;
        
        var gridImport = Math.Max(0, consumption - production);
        var gridExport = Math.Max(0, production - consumption);

        // For now, use fixed pricing (can enhance with dynamic later)
        return (gridImport * importPrice) - (gridExport * exportPrice);
    }

    private int CalculateMonthsDiff(DateTime start, DateTime end)
    {
        return ((end.Year - start.Year) * 12) + end.Month - start.Month;
    }
}
