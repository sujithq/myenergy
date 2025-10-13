# ROI Solar Savings Calculation Fix - Battery Overwrite Bug

## Problem

When battery investment was enabled, the Monthly Savings chart showed **negative solar savings** (yellow bars below zero), especially after June 2024. This should be impossible - solar panels should always save money compared to pure grid usage.

### Symptoms from Graph
- Negative solar savings (yellow bars below €0) from June-August 2024
- Abnormally high savings in early months (€157 in June 2023)
- Erratic pattern changes around dynamic pricing start date
- Battery savings appeared normal (green bars positive)

## Root Cause

**Bug in `RoiAnalysisService.cs` lines 154-156:**

The code was **overwriting** the solar cost with the battery simulation's result:

```csharp
// ❌ BUG: Overwrites solarCost!
solarCost = shouldUseDynamicPricing 
    ? dayResult.CostNoBatteryDynamic
    : dayResult.CostNoBatteryFixed;
```

### Why This Caused Negative Savings

1. **Initial calculation** (line 143): `solarCost` = actual cost with solar panels
2. **Overwrite** (line 154): `solarCost` = battery simulation's "no battery" cost
3. **Solar savings** (line 167): `baselineCost - solarCost`

**The problem**: The simulation's `CostNoBatteryDynamic` might be calculated using:
- Different time intervals (15-minute vs daily totals)
- Different pricing logic
- Battery optimization assumptions

When `solarCost` becomes larger than `baselineCost`, you get **negative savings**!

### Why It Was Worse with Dynamic Pricing

Dynamic pricing calculations are more complex with 15-minute intervals. The simulation uses quarter-hour optimization, while the daily calculation uses whole-day totals. This mismatch made `solarCost` incorrectly high, resulting in negative savings.

## Solution

Keep the original `solarCost` for solar savings calculations, and introduce a separate `simSolarCost` for battery comparisons:

```csharp
// Calculate cost with solar only (if active)
var solarCost = solarActive 
    ? CalculateCostWithSolar(...)
    : baselineCost;

// Track simulation's solar cost separately
var simSolarCost = solarCost;

if (batteryActive && solarActive)
{
    // Get simulation results
    simSolarCost = shouldUseDynamicPricing 
        ? dayResult.CostNoBatteryDynamic
        : dayResult.CostNoBatteryFixed;
    
    solarAndBatteryCost = shouldUseDynamicPricing 
        ? dayResult.CostWithBatteryDynamic
        : dayResult.CostWithBatteryFixed;
}

// Solar savings: baseline vs solar (using original solarCost)
var solarDailySavings = solarActive ? (baselineCost - solarCost) : 0;

// Battery savings: solar vs solar+battery (using simulation's values for consistency)
var batteryDailySavings = (batteryActive && solarActive) ? (simSolarCost - solarAndBatteryCost) : 0;
```

### Key Changes

1. **Added `simSolarCost` variable** - Tracks simulation's solar-only cost separately
2. **Keep original `solarCost`** - Used for solar savings calculation (never overwritten)
3. **Battery savings uses `simSolarCost`** - Ensures apples-to-apples comparison with `solarAndBatteryCost`

## Why This Works

### Correct Calculation Flow

**For Solar Savings (Yellow Bars):**
- Compares: `baselineCost` (no solar) vs `solarCost` (with solar)
- Uses: Daily calculation method (whole-day import/export)
- Result: Always positive (solar always reduces costs vs pure grid)

**For Battery Savings (Green Bars):**
- Compares: `simSolarCost` (solar, no battery) vs `solarAndBatteryCost` (solar + battery)
- Uses: Simulation results (both values from same 15-minute optimization)
- Result: Consistent comparison using same methodology

**For Total Savings:**
- Compares: `baselineCost` (no solar) vs `solarAndBatteryCost` (solar + battery)
- Result: Combined benefit of both investments

## Expected Results After Fix

### Monthly Savings Chart Should Show:

✅ **Yellow bars (solar) always positive** - Never below €0  
✅ **Consistent solar savings** - No dramatic swings  
✅ **Green bars (battery) mostly positive** - Small additional savings  
✅ **Smooth transitions** - No erratic changes at dynamic pricing start  

### Typical Monthly Values:

- **Solar only** (May 2023 - May 2024): €50-150/month
- **Battery addition**: €5-25/month extra
- **Total combined**: €55-175/month
- **Summer months higher**: More solar production
- **Winter months lower**: Less solar production

## Testing Scenarios

### Scenario 1: Solar Only (Battery Disabled)
- Solar savings should be consistently positive
- Based on production vs consumption
- Higher in summer, lower in winter

### Scenario 2: Solar + Battery (Battery Enabled)
- Solar savings should remain positive
- Battery adds additional savings (green bars)
- Battery optimizes import/export timing

### Scenario 3: Dynamic Pricing Enabled
- Both solar and battery savings should remain positive
- Battery savings may increase (better arbitrage opportunities)
- No negative values at pricing transition date

## Related Files

**Modified:**
- `Services/RoiAnalysisService.cs` (lines 141-179)

**Affected Pages:**
- ROI Analysis: Shows correct solar/battery savings breakdown
- Monthly Savings Chart: No more negative yellow bars
- Summary Cards: Accurate cumulative savings

## Technical Details

### Why Two Solar Cost Variables?

1. **`solarCost`** - Daily calculation method:
   - Uses whole-day production/consumption totals
   - Simple import/export calculation
   - Good for solar-only scenarios
   - Consistent with baseline calculation

2. **`simSolarCost`** - Simulation method:
   - Uses 15-minute interval optimization
   - Considers battery charging/discharging
   - Matches battery cost calculation methodology
   - Ensures apples-to-apples comparison

### Why Not Use Simulation for Everything?

Running battery simulations is expensive:
- Pre-calculates full year simulations for each battery capacity
- Uses complex 15-minute optimization algorithm
- Takes several seconds per year

Daily calculations are fast:
- Simple daily totals
- No optimization needed
- Instant calculation

We only use simulation when battery is enabled, otherwise use fast daily calculation.

## Validation

After rebuild and restart, verify:

1. **Console logs** - No "negative savings" warnings
2. **Monthly chart** - All yellow bars above €0
3. **Summary cards** - Positive net positions
4. **Cumulative chart** - Smooth upward trend

## Historical Context

This completes the ROI calculation fixes:
1. ✅ **Dynamic Pricing Baseline Fix** - Consistent pricing models
2. ✅ **Date Range Fix** - Start from May 2023 with quarter-hour data
3. ✅ **Monthly Chart Display** - Show all 30 months
4. ✅ **ODS Column Mapping** - Read Elia pricing data correctly
5. ✅ **Solar Savings Calculation** - Never overwrite solarCost (THIS FIX)

Together, these ensure accurate and reliable ROI analysis! 🎉
