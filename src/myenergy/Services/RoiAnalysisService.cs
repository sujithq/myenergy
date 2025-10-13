using myenergy.Models;

namespace myenergy.Services;

public class RoiAnalysisService
{
    private readonly EnergyDataService _energyService;
    private readonly BatterySimulationService _simulationService;
    private readonly IOdsPricingService _pricingService;

    public RoiAnalysisService(
        EnergyDataService energyService, 
        BatterySimulationService simulationService,
        IOdsPricingService pricingService)
    {
        _energyService = energyService;
        _simulationService = simulationService;
        _pricingService = pricingService;
    }

    public async Task<RoiAnalysis> CalculateRoi(
        SolarInvestment? solarInvestment,
        BatteryInvestment? batteryInvestment,
        DateTime analysisStartDate,
        DateTime analysisEndDate,
        double fixedImportPrice,
        double fixedExportPrice,
        bool useDynamicPricing,
        DateTime? dynamicPricingStartDate = null)
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
                    fixedExportPrice,
                    useDynamicPricing,
                    dynamicPricingStartDate);
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
            
            // Determine if we should use dynamic pricing for this date
            var shouldUseDynamicPricing = useDynamicPricing 
                && dynamicPricingStartDate.HasValue 
                && date >= dynamicPricingStartDate.Value;

            // Calculate baseline cost (no solar, no battery - pure grid consumption)
            // IMPORTANT: Must use same pricing model (fixed or dynamic) for fair comparison
            var baselineCost = shouldUseDynamicPricing
                ? CalculateBaselineCostDynamic(dailyDetail, fixedImportPrice)
                : CalculateBaselineCost(dailyDetail, fixedImportPrice);

            // Calculate cost with solar only (if active)
            var solarCost = solarActive 
                ? CalculateCostWithSolar(dailyDetail, fixedImportPrice, fixedExportPrice, useDynamicPricing, date, dynamicPricingStartDate)
                : baselineCost;

            // Calculate cost with solar + battery (if both active)
            var solarAndBatteryCost = solarCost; // Default to solar-only cost
            if (batteryActive && solarActive && batterySimulations.TryGetValue(date.Year, out var yearSim))
            {
                var dayResult = yearSim.DailyResults.FirstOrDefault(d => d.Date.Date == date.Date);
                if (dayResult != null)
                {
                    // Use consistent pricing model from simulation
                    solarCost = shouldUseDynamicPricing 
                        ? dayResult.CostNoBatteryDynamic
                        : dayResult.CostNoBatteryFixed;
                    
                    solarAndBatteryCost = shouldUseDynamicPricing 
                        ? dayResult.CostWithBatteryDynamic
                        : dayResult.CostWithBatteryFixed;
                }
            }

            // Daily savings
            var solarDailySavings = solarActive ? (baselineCost - solarCost) : 0;
            // Battery savings only when both solar AND battery are active (battery needs solar to work)
            var batteryDailySavings = (batteryActive && solarActive) ? (solarCost - solarAndBatteryCost) : 0;
            var totalDailySavings = baselineCost - solarAndBatteryCost;

            // DEBUG: Track battery savings statistics
            if (batteryActive && solarActive)
            {
                if (dailyData.Count == 0) // First day
                {
                    Console.WriteLine("=== Battery Savings Analysis ===");
                }
                
                if (batteryDailySavings <= 0 && dailyData.Count < 10)
                {
                    var dailyCalcCost = CalculateCostWithSolar(dailyDetail, fixedImportPrice, fixedExportPrice, useDynamicPricing, date, dynamicPricingStartDate);
                    
                    Console.WriteLine($"Date: {date:yyyy-MM-dd}");
                    Console.WriteLine($"  Baseline Cost: €{baselineCost:F2}");
                    Console.WriteLine($"  Daily Calc Solar Cost: €{dailyCalcCost:F2} (whole-day totals)");
                    Console.WriteLine($"  Simulation Solar Cost: €{solarCost:F2} (15-min intervals)");
                    Console.WriteLine($"  Simulation Solar+Battery: €{solarAndBatteryCost:F2} (15-min intervals)");
                    Console.WriteLine($"  Solar Savings: €{solarDailySavings:F2}");
                    Console.WriteLine($"  Battery Savings: €{batteryDailySavings:F2} {(batteryDailySavings < 0 ? "❌ NEGATIVE" : "⚠️ ZERO")}");
                    Console.WriteLine($"  Issue: Battery makes it {(batteryDailySavings < 0 ? "MORE expensive" : "no different")} than solar alone");
                    Console.WriteLine();
                }
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

        // Battery savings summary
        if (batteryInvestment != null)
        {
            var daysWithBattery = dailyData.Count(d => d.BatteryDailySavings != 0);
            var daysWithNegativeSavings = dailyData.Count(d => d.BatteryDailySavings < 0);
            var daysWithPositiveSavings = dailyData.Count(d => d.BatteryDailySavings > 0);
            
            Console.WriteLine("=== Battery Savings Summary ===");
            Console.WriteLine($"Total days analyzed: {dailyData.Count}");
            Console.WriteLine($"Days with battery active: {daysWithBattery}");
            Console.WriteLine($"Days with positive savings: {daysWithPositiveSavings} ({(double)daysWithPositiveSavings/daysWithBattery*100:F1}%)");
            Console.WriteLine($"Days with negative savings: {daysWithNegativeSavings} ({(double)daysWithNegativeSavings/daysWithBattery*100:F1}%)");
            Console.WriteLine($"Total battery savings: €{batteryCumulativeSavings:F2}");
            Console.WriteLine($"Average daily savings: €{batteryCumulativeSavings/daysWithBattery:F2}");
            Console.WriteLine("================================");
        }

        // DIAGNOSTIC: Summary of battery performance
        if (batteryInvestment != null && batteryCumulativeSavings < 0)
        {
            var negativeDays = dailyData.Count(d => d.BatteryDailySavings < 0);
            var positiveDays = dailyData.Count(d => d.BatteryDailySavings > 0);
            var zeroDays = dailyData.Count(d => d.BatteryDailySavings == 0);
            
            Console.WriteLine("=== BATTERY PERFORMANCE SUMMARY ===");
            Console.WriteLine($"Total Days Analyzed: {dailyData.Count}");
            Console.WriteLine($"Days with NEGATIVE battery savings: {negativeDays}");
            Console.WriteLine($"Days with POSITIVE battery savings: {positiveDays}");
            Console.WriteLine($"Days with ZERO battery savings: {zeroDays}");
            Console.WriteLine($"Total Battery Savings: €{batteryCumulativeSavings:F2}");
            Console.WriteLine($"Average daily savings: €{(batteryCumulativeSavings / dailyData.Count):F4}");
            
            // Show worst days
            var worstDays = dailyData
                .Where(d => d.BatteryDailySavings < 0)
                .OrderBy(d => d.BatteryDailySavings)
                .Take(3)
                .ToList();
            
            Console.WriteLine("Worst 3 days:");
            foreach (var day in worstDays)
            {
                Console.WriteLine($"  {day.Date:yyyy-MM-dd}: €{day.BatteryDailySavings:F2} (Solar: €{day.CostWithSolar:F2}, Solar+Bat: €{day.CostWithSolarAndBattery:F2})");
            }
            Console.WriteLine("===================================");
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
        // Baseline: No solar, must import all consumption (fixed pricing)
        var totalConsumption = dailyDetail.TotalConsumption;
        return totalConsumption * importPrice;
    }

    private double CalculateBaselineCostDynamic(DailyDetailData dailyDetail, double fallbackImportPrice)
    {
        // Baseline with dynamic pricing: No solar, must import all consumption
        // Calculate cost using 15-minute interval dynamic pricing
        double totalCost = 0;
        
        foreach (var quarter in dailyDetail.QuarterHours)
        {
            var pricing = _pricingService.GetPricingForInterval(quarter.Time);
            var quarterImportPrice = pricing?.ImportPricePerKwh ?? fallbackImportPrice;
            
            // All consumption must be imported from grid (no solar)
            totalCost += quarter.TotalConsumption * quarterImportPrice;
        }
        
        return totalCost;
    }

    private double CalculateCostWithSolar(
        DailyDetailData dailyDetail, 
        double fixedImportPrice, 
        double fixedExportPrice,
        bool useDynamicPricing,
        DateTime date,
        DateTime? dynamicPricingStartDate)
    {
        // With solar: Import what you need, export surplus
        var production = dailyDetail.TotalProduction;
        var consumption = dailyDetail.TotalConsumption;
        
        var gridImport = Math.Max(0, consumption - production);
        var gridExport = Math.Max(0, production - consumption);

        // Determine if we should use dynamic pricing for this date
        var shouldUseDynamicPricing = useDynamicPricing 
            && dynamicPricingStartDate.HasValue 
            && date >= dynamicPricingStartDate.Value;

        if (shouldUseDynamicPricing)
        {
            // Calculate cost using 15-minute interval dynamic pricing
            double totalCost = 0;
            
            foreach (var quarter in dailyDetail.QuarterHours)
            {
                var pricing = _pricingService.GetPricingForInterval(quarter.Time);
                
                var quarterImportPrice = pricing?.ImportPricePerKwh ?? fixedImportPrice;
                var quarterExportPrice = pricing?.InjectionPricePerKwh ?? fixedExportPrice;
                
                var quarterGridImport = Math.Max(0, quarter.TotalConsumption - quarter.ActualProduction);
                var quarterGridExport = Math.Max(0, quarter.ActualProduction - quarter.TotalConsumption);
                
                totalCost += (quarterGridImport * quarterImportPrice) - (quarterGridExport * quarterExportPrice);
            }
            
            return totalCost;
        }
        else
        {
            // Use fixed pricing
            return (gridImport * fixedImportPrice) - (gridExport * fixedExportPrice);
        }
    }

    private int CalculateMonthsDiff(DateTime start, DateTime end)
    {
        return ((end.Year - start.Year) * 12) + end.Month - start.Month;
    }
}
