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

        // Find first and last dates with actual quarter-hour data
        var firstValidDate = availableDates.FirstOrDefault(d => 
        {
            var detail = _energyService.GetDailyDetailData(d);
            return detail != null && detail.QuarterHours.Any();
        });
        
        var lastValidDate = availableDates.LastOrDefault(d => 
        {
            var detail = _energyService.GetDailyDetailData(d);
            return detail != null && detail.QuarterHours.Any();
        });

        // Adjust date range to available data
        if (firstValidDate != default && lastValidDate != default)
        {
            if (firstValidDate > analysisStartDate)
            {
                Console.WriteLine($"⚠️ Adjusted start date from {analysisStartDate:yyyy-MM-dd} to {firstValidDate:yyyy-MM-dd} (first date with quarter-hour data)");
                analysisStartDate = firstValidDate;
            }
            
            if (lastValidDate < analysisEndDate)
            {
                Console.WriteLine($"⚠️ Adjusted end date from {analysisEndDate:yyyy-MM-dd} to {lastValidDate:yyyy-MM-dd} (last date with quarter-hour data)");
                analysisEndDate = lastValidDate;
            }
            
            // Update available dates list with adjusted range
            availableDates = availableDates
                .Where(d => d >= analysisStartDate && d <= analysisEndDate)
                .ToList();
        }

        Console.WriteLine($"=== ROI Analysis Date Range ===");
        Console.WriteLine($"Analysis Start: {analysisStartDate:yyyy-MM-dd}");
        Console.WriteLine($"Analysis End: {analysisEndDate:yyyy-MM-dd}");
        Console.WriteLine($"Available dates in range: {availableDates.Count}");
        if (availableDates.Any())
        {
            Console.WriteLine($"First date: {availableDates.First():yyyy-MM-dd}");
            Console.WriteLine($"Last date: {availableDates.Last():yyyy-MM-dd}");
        }
        Console.WriteLine($"Dynamic Pricing: {useDynamicPricing}");
        Console.WriteLine($"Dynamic Start: {dynamicPricingStartDate:yyyy-MM-dd}");
        Console.WriteLine("==============================");

        double solarCumulativeSavings = 0;
        double batteryCumulativeSavings = 0;
        
        DateTime? solarBreakEven = null;
        DateTime? batteryBreakEven = null;
        DateTime? combinedBreakEven = null;
        
        int skippedDays = 0;

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
            {
                skippedDays++;
                if (skippedDays <= 5) // Log first 5 skipped days
                {
                    Console.WriteLine($"⚠️ Skipping {date:yyyy-MM-dd}: No quarter-hour data available");
                }
                continue;
            }

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
            var simSolarCost = solarCost; // Track simulation's solar cost separately
            
            if (batteryActive && solarActive && batterySimulations.TryGetValue(date.Year, out var yearSim))
            {
                var dayResult = yearSim.DailyResults.FirstOrDefault(d => d.Date.Date == date.Date);
                if (dayResult != null)
                {
                    // Get simulation results for consistent comparison
                    // The simulation calculates both "no battery" and "with battery" using the same methodology
                    simSolarCost = shouldUseDynamicPricing 
                        ? dayResult.CostNoBatteryDynamic
                        : dayResult.CostNoBatteryFixed;
                    
                    solarAndBatteryCost = shouldUseDynamicPricing 
                        ? dayResult.CostWithBatteryDynamic
                        : dayResult.CostWithBatteryFixed;
                }
            }

            // Daily savings
            // Solar savings: baseline vs solar (no battery involved)
            var solarDailySavings = solarActive ? (baselineCost - solarCost) : 0;
            
            // Battery savings: solar-only vs solar+battery (using simulation for apples-to-apples comparison)
            // Use simSolarCost (from simulation) for consistency with solarAndBatteryCost
            var batteryDailySavings = (batteryActive && solarActive) ? (simSolarCost - solarAndBatteryCost) : 0;
            
            // Total savings: baseline vs solar+battery
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

        Console.WriteLine($"=== ROI Processing Complete ===");
        Console.WriteLine($"Days processed: {dailyData.Count} out of {availableDates.Count} available");
        if (skippedDays > 0)
        {
            Console.WriteLine($"⚠️ Skipped {skippedDays} days with no quarter-hour data");
        }
        if (dailyData.Any())
        {
            Console.WriteLine($"First day in results: {dailyData.First().Date:yyyy-MM-dd}");
            Console.WriteLine($"Last day in results: {dailyData.Last().Date:yyyy-MM-dd}");
        }
        Console.WriteLine("==============================");

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
        int nullPricingCount = 0;
        int zeroPricingCount = 0;
        
        foreach (var quarter in dailyDetail.QuarterHours)
        {
            var pricing = _pricingService.GetPricingForInterval(quarter.Time);
            var quarterImportPrice = pricing?.ImportPricePerKwh ?? fallbackImportPrice;
            
            // Diagnostic logging
            if (pricing == null)
            {
                nullPricingCount++;
            }
            else if (pricing.ImportPricePerKwh == 0)
            {
                zeroPricingCount++;
            }
            
            // All consumption must be imported from grid (no solar)
            totalCost += quarter.TotalConsumption * quarterImportPrice;
        }
        
        // Log if we have issues
        if (nullPricingCount > 0 || zeroPricingCount > 0)
        {
            Console.WriteLine($"⚠️ Baseline Dynamic Pricing Issues on {dailyDetail.Date:yyyy-MM-dd}:");
            Console.WriteLine($"   Null pricing: {nullPricingCount}/{dailyDetail.QuarterHours.Count} intervals");
            Console.WriteLine($"   Zero pricing: {zeroPricingCount}/{dailyDetail.QuarterHours.Count} intervals");
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
            int nullPricingCount = 0;
            int zeroPricingCount = 0;
            int negativePricingCount = 0;
            double totalImportPrice = 0;
            double totalExportPrice = 0;
            int pricingCount = 0;
            
            foreach (var quarter in dailyDetail.QuarterHours)
            {
                var pricing = _pricingService.GetPricingForInterval(quarter.Time);
                
                var quarterImportPrice = pricing?.ImportPricePerKwh ?? fixedImportPrice;
                // IMPORTANT: Negative export prices mean you PAY to export (grid oversupply)
                // For ROI calculations, treat negative as zero (you don't earn, but don't pay)
                var quarterExportPrice = pricing?.InjectionPricePerKwh ?? fixedExportPrice;
                if (quarterExportPrice < 0)
                {
                    negativePricingCount++;
                    quarterExportPrice = 0; // Don't pay to export, just get nothing
                }
                
                // Diagnostic tracking
                if (pricing == null)
                {
                    nullPricingCount++;
                }
                else
                {
                    if (pricing.ImportPricePerKwh == 0) zeroPricingCount++;
                    totalImportPrice += pricing.ImportPricePerKwh;
                    // Use clamped export price for accurate diagnostic average
                    totalExportPrice += quarterExportPrice; // After clamping, not the original
                    pricingCount++;
                }
                
                var quarterGridImport = Math.Max(0, quarter.TotalConsumption - quarter.ActualProduction);
                var quarterGridExport = Math.Max(0, quarter.ActualProduction - quarter.TotalConsumption);
                
                totalCost += (quarterGridImport * quarterImportPrice) - (quarterGridExport * quarterExportPrice);
            }
            
            // Log first 3 days with pricing issues
            if ((nullPricingCount > 0 || zeroPricingCount > 0 || negativePricingCount > 0) && date.Day <= 3)
            {
                Console.WriteLine($"⚠️ Solar Dynamic Pricing Issues on {date:yyyy-MM-dd}:");
                Console.WriteLine($"   Null pricing: {nullPricingCount}/{dailyDetail.QuarterHours.Count} intervals");
                Console.WriteLine($"   Zero pricing: {zeroPricingCount}/{dailyDetail.QuarterHours.Count} intervals");
                Console.WriteLine($"   Negative export pricing: {negativePricingCount}/{dailyDetail.QuarterHours.Count} intervals");
                if (pricingCount > 0)
                {
                    Console.WriteLine($"   Avg import: €{(totalImportPrice/pricingCount):F4}/kWh");
                    Console.WriteLine($"   Avg export: €{(totalExportPrice/pricingCount):F4}/kWh");
                }
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
