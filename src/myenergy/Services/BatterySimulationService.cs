using myenergy.Models;

namespace myenergy.Services;

public class BatterySimulationService
{
    private readonly EnergyDataService _energyService;
    private readonly OdsPricingService _pricingService;

    public BatterySimulationService(EnergyDataService energyService, OdsPricingService pricingService)
    {
        _energyService = energyService;
        _pricingService = pricingService;
    }

    public async Task<SimulationResults> RunSimulation(
        int year,
        double batteryCapacityKwh,
        double fixedImportPrice,
        double fixedExportPrice)
    {
        await _energyService.LoadDataAsync();
        await _pricingService.LoadDataAsync();

        var batteryConfig = new BatteryConfig(
            CapacityKwh: batteryCapacityKwh,
            MaxChargeRateKw: batteryCapacityKwh / 2.0,      // C/2 charge rate (typical for home battery)
            MaxDischargeRateKw: batteryCapacityKwh / 2.0,   // C/2 discharge rate
            Efficiency: 0.95                                 // 95% round-trip efficiency
        );

        var dailyResults = new List<DailySimulation>();
        var availableDates = _energyService.GetAvailableDates()
            .Where(d => d.Year == year)
            .OrderBy(d => d)
            .ToList();

        double batteryLevel = 0; // Start with empty battery

        foreach (var date in availableDates)
        {
            var dailyDetail = _energyService.GetDailyDetailData(date);
            if (dailyDetail == null || !dailyDetail.QuarterHours.Any())
                continue;

            var dailySim = SimulateDay(date, dailyDetail, batteryConfig, ref batteryLevel, fixedImportPrice, fixedExportPrice);
            dailyResults.Add(dailySim);
        }

        return new SimulationResults
        {
            Year = year,
            BatteryConfig = batteryConfig,
            FixedImportPrice = fixedImportPrice,
            FixedExportPrice = fixedExportPrice,
            DailyResults = dailyResults
        };
    }

    private DailySimulation SimulateDay(
        DateTime date,
        DailyDetailData dailyDetail,
        BatteryConfig battery,
        ref double batteryLevel,
        double fixedImportPrice,
        double fixedExportPrice)
    {
        var intervals = new List<IntervalSimulation>();
        var dayPricing = _pricingService.GetPricingForDay(date);

        foreach (var quarter in dailyDetail.QuarterHours)
        {
            var production = quarter.ActualProduction;
            var consumption = quarter.TotalConsumption;
            var netDemand = consumption - production;

            // Get dynamic pricing for this interval
            var pricing = _pricingService.GetPricingForInterval(quarter.Time);
            var dynamicImportPrice = pricing?.ImportPricePerKwh ?? fixedImportPrice;
            var dynamicExportPrice = pricing?.InjectionPricePerKwh ?? fixedExportPrice;

            // Scenario 1: No battery
            var gridImportNoBattery = Math.Max(0, netDemand);
            var gridExportNoBattery = Math.Max(0, -netDemand);

            // Scenario 2: With battery - Smart management
            var (batteryCharge, batteryDischarge, gridImport, gridExport, newBatteryLevel) = 
                OptimizeBatteryOperation(
                    production, 
                    consumption, 
                    batteryLevel, 
                    battery, 
                    dynamicImportPrice, 
                    dynamicExportPrice);

            batteryLevel = newBatteryLevel;

            // Calculate costs
            var costNoBatteryDynamic = (gridImportNoBattery * dynamicImportPrice) - (gridExportNoBattery * dynamicExportPrice);
            var costWithBatteryDynamic = (gridImport * dynamicImportPrice) - (gridExport * dynamicExportPrice);
            var costNoBatteryFixed = (gridImportNoBattery * fixedImportPrice) - (gridExportNoBattery * fixedExportPrice);
            var costWithBatteryFixed = (gridImport * fixedImportPrice) - (gridExport * fixedExportPrice);

            intervals.Add(new IntervalSimulation
            {
                Time = quarter.Time,
                Production = production,
                Consumption = consumption,
                GridImportNoBattery = gridImportNoBattery,
                GridExportNoBattery = gridExportNoBattery,
                BatteryCharge = batteryCharge,
                BatteryDischarge = batteryDischarge,
                BatteryLevel = batteryLevel,
                BatterySoC = (batteryLevel / battery.UsableCapacity) * 100,
                GridImportWithBattery = gridImport,
                GridExportWithBattery = gridExport,
                ImportPrice = dynamicImportPrice,
                ExportPrice = dynamicExportPrice,
                CostNoBatteryDynamic = costNoBatteryDynamic,
                CostWithBatteryDynamic = costWithBatteryDynamic,
                CostNoBatteryFixed = costNoBatteryFixed,
                CostWithBatteryFixed = costWithBatteryFixed
            });
        }

        return new DailySimulation
        {
            Date = date,
            Intervals = intervals
        };
    }

    private (double charge, double discharge, double gridImport, double gridExport, double newLevel) OptimizeBatteryOperation(
        double production,
        double consumption,
        double currentLevel,
        BatteryConfig battery,
        double importPrice,
        double exportPrice)
    {
        const double quarterHourFraction = 0.25; // 15 minutes = 0.25 hours
        
        var netDemand = consumption - production;
        double charge = 0;
        double discharge = 0;
        double gridImport = 0;
        double gridExport = 0;

        if (netDemand > 0) // Need power
        {
            // ALWAYS use battery instead of importing (battery power is free!)
            var availableFromBattery = currentLevel;
            var maxDischarge = battery.MaxDischargeRateKw * quarterHourFraction;
            var canDischarge = Math.Min(availableFromBattery, Math.Min(maxDischarge, netDemand));

            if (canDischarge > 0)
            {
                discharge = canDischarge / battery.Efficiency; // Account for efficiency loss
                var remaining = netDemand - canDischarge;
                gridImport = Math.Max(0, remaining);
            }
            else
            {
                // Battery empty, must import
                gridImport = netDemand;
            }
        }
        else // Surplus power
        {
            var surplus = -netDemand;
            
            // Strategy: Store free solar energy in battery for later use
            // Only makes sense if we'll actually use it to avoid future imports
            // Considering round-trip efficiency: store only if (importPrice * efficiency²) > exportPrice
            var spaceInBattery = battery.UsableCapacity - currentLevel;
            var maxCharge = battery.MaxChargeRateKw * quarterHourFraction;
            var canStore = Math.Min(spaceInBattery, Math.Min(maxCharge, surplus));

            // Economic threshold: Store if future import savings (after losses) exceed immediate export revenue
            // With 95% efficiency: need importPrice * 0.9025 > exportPrice
            // Example: €0.30 * 0.9025 = €0.271 > €0.05 ✅ (worth storing)
            var roundTripEfficiency = battery.Efficiency * battery.Efficiency;
            var futureValue = importPrice * roundTripEfficiency;
            
            if (canStore > 0 && futureValue > exportPrice)
            {
                charge = canStore * battery.Efficiency; // Account for charging efficiency
                var remaining = surplus - canStore;
                gridExport = Math.Max(0, remaining);
            }
            else
            {
                // Not economical to store, export all surplus immediately
                gridExport = surplus;
            }
        }

        var newLevel = currentLevel - discharge + charge;
        newLevel = Math.Max(0, Math.Min(battery.UsableCapacity, newLevel)); // Clamp to valid range

        return (charge, discharge, gridImport, gridExport, newLevel);
    }

    // Get comparison data for visualization
    public List<(DateTime date, double noBattery, double withBattery5k, double withBattery10k)> GetCumulativeCostComparison(
        SimulationResults sim5k,
        SimulationResults sim10k,
        SimulationResults noBattery)
    {
        var results = new List<(DateTime, double, double, double)>();
        
        double cumNoBattery = 0;
        double cum5k = 0;
        double cum10k = 0;

        for (int i = 0; i < noBattery.DailyResults.Count; i++)
        {
            var date = noBattery.DailyResults[i].Date;
            cumNoBattery += noBattery.DailyResults[i].CostNoBatteryDynamic;
            
            if (i < sim5k.DailyResults.Count)
                cum5k += sim5k.DailyResults[i].CostWithBatteryDynamic;
            
            if (i < sim10k.DailyResults.Count)
                cum10k += sim10k.DailyResults[i].CostWithBatteryDynamic;
            
            results.Add((date, cumNoBattery, cum5k, cum10k));
        }

        return results;
    }
}
