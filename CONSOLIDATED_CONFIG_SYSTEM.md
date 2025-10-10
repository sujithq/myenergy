# Consolidated Configuration System

## Overview
All configurable constants across the myenergy application are now centralized in a single JSON configuration file (`app-config.json`) and accessed via the `PricingConfigService`.

---

## Configuration File Structure

**Location**: `wwwroot/config/app-config.json`

```json
{
  "pricing": {
    "fixedImportPrice": 0.30,
    "fixedExportPrice": 0.05,
    "description": "Default Belgian electricity pricing (€/kWh)"
  },
  "battery": {
    "defaultCapacityKwh": 5.0,
    "efficiency": 0.95,
    "chargeRateFactor": 0.5,
    "dischargeRateFactor": 0.5,
    "description": "Battery defaults"
  },
  "solar": {
    "defaultSystemCapacityKwp": 5.0,
    "averagePeakSunHours": 4.0,
    "defaultSystemCostEur": 10000,
    "description": "Solar system defaults"
  },
  "lastUpdated": "2025-10-10"
}
```

---

## Configuration Categories

### 1. Pricing Configuration
**Used by**: SmartUsageAdvisor, BatterySimulation, DailyCostAnalysis, RoiAnalysis, CostSavings

| Property | Default | Description |
|----------|---------|-------------|
| `fixedImportPrice` | 0.30 | Cost per kWh when importing from grid (€/kWh) |
| `fixedExportPrice` | 0.05 | Revenue per kWh when exporting to grid (€/kWh) |

**Belgian Context**:
- Import: €0.30/kWh typical residential rate
- Export: €0.05/kWh typical feed-in tariff (decreasing trend)

---

### 2. Battery Configuration
**Used by**: BatterySimulation, DailyCostAnalysis, RoiAnalysis

| Property | Default | Description |
|----------|---------|-------------|
| `defaultCapacityKwh` | 5.0 | Default battery capacity in kWh |
| `efficiency` | 0.95 | Round-trip efficiency (95%) |
| `chargeRateFactor` | 0.5 | Charge rate as fraction of capacity (C/2) |
| `dischargeRateFactor` | 0.5 | Discharge rate as fraction of capacity (C/2) |

**Technical Details**:
- **5 kWh capacity**: Typical home battery (Tesla Powerwall: 13.5 kWh, LG Chem: 9.8 kWh)
- **95% efficiency**: Industry standard for lithium-ion batteries
- **C/2 rate**: Means a 5kWh battery charges/discharges at 2.5kW
  - Full charge/discharge takes 2 hours
  - Balances speed vs battery longevity

---

### 3. Solar Configuration
**Used by**: EfficiencyMetrics, CostSavings

| Property | Default | Description |
|----------|---------|-------------|
| `defaultSystemCapacityKwp` | 5.0 | Default solar system capacity in kWp |
| `averagePeakSunHours` | 4.0 | Average peak sun hours per day (Belgian climate) |
| `defaultSystemCostEur` | 10000 | Typical solar system installation cost in € |

**Belgian Context**:
- **5 kWp system**: Typical residential installation (12-15 panels)
- **4.0 peak sun hours**: Belgium average (varies 2.5-5.5 by season)
- **€10,000 cost**: Typical 2024-2025 installation cost for 5kWp

---

## PricingConfigService API

### Properties

#### Pricing
```csharp
public double FixedImportPrice { get; }      // Default: 0.30
public double FixedExportPrice { get; }      // Default: 0.05
```

#### Battery
```csharp
public double DefaultBatteryCapacityKwh { get; }       // Default: 5.0
public double BatteryEfficiency { get; }               // Default: 0.95
public double BatteryChargeRateFactor { get; }         // Default: 0.5
public double BatteryDischargeRateFactor { get; }      // Default: 0.5
```

#### Solar
```csharp
public double DefaultSolarSystemCapacityKwp { get; }   // Default: 5.0
public double AveragePeakSunHours { get; }             // Default: 4.0
public double DefaultSolarSystemCostEur { get; }       // Default: 10000
```

#### Status
```csharp
public bool IsLoaded { get; }
```

### Methods
```csharp
public Task LoadConfigAsync();
public Task UpdateConfigAsync(double importPrice, double exportPrice);
```

---

## Page-by-Page Usage

### SmartUsageAdvisor.razor
**Uses**: Pricing only

```csharp
@inject PricingConfigService PricingConfig

await PricingConfig.LoadConfigAsync();

// Used when dynamic pricing is disabled
var fixedRate = PricingConfig.FixedImportPrice;
```

**Impact**: Fixed pricing mode shows consistent rate from config

---

### BatterySimulation.razor
**Uses**: Pricing + Battery

```csharp
@inject PricingConfigService PricingConfig

await PricingConfig.LoadConfigAsync();

// Initialize all from config
batteryCapacity = PricingConfig.DefaultBatteryCapacityKwh;
fixedImportPrice = PricingConfig.FixedImportPrice;
fixedExportPrice = PricingConfig.FixedExportPrice;
```

**Impact**: 
- Default battery capacity from config
- Fixed pricing scenarios use config values
- Users can still override in UI

---

### DailyCostAnalysis.razor
**Uses**: Pricing + Battery

```csharp
@inject PricingConfigService PricingConfig

await PricingConfig.LoadConfigAsync();

batteryCapacity = PricingConfig.DefaultBatteryCapacityKwh;
fixedImportPrice = PricingConfig.FixedImportPrice;
fixedExportPrice = PricingConfig.FixedExportPrice;
```

**Impact**: Fixed pricing baseline uses config, battery size default

---

### RoiAnalysis.razor
**Uses**: Pricing + Battery

```csharp
@inject PricingConfigService PricingConfig

await PricingConfig.LoadConfigAsync();

batteryCapacity = PricingConfig.DefaultBatteryCapacityKwh;
fixedImportPrice = PricingConfig.FixedImportPrice;
fixedExportPrice = PricingConfig.FixedExportPrice;
```

**Impact**: ROI calculations use consistent defaults

---

### EfficiencyMetrics.razor
**Uses**: Solar only

```csharp
@inject PricingConfigService PricingConfig

await PricingConfig.LoadConfigAsync();

systemCapacityKwp = PricingConfig.DefaultSolarSystemCapacityKwp;
```

**Impact**: 
- Default system size from config
- Peak sun hours for efficiency calculations
- Users can override in UI

---

### CostSavings.razor
**Uses**: Pricing + Solar

```csharp
@inject PricingConfigService PricingConfig

await PricingConfig.LoadConfigAsync();

importCostPerKwh = PricingConfig.FixedImportPrice;
systemCost = PricingConfig.DefaultSolarSystemCostEur;
```

**Impact**: 
- Import cost from config
- Default system cost from config
- Payback calculations consistent

---

## Constants Analysis

### ✅ Centralized (Multi-Page Usage)

| Constant | Pages Using | Config Property |
|----------|-------------|-----------------|
| Fixed Import Price (€0.30) | 5 pages | `pricing.fixedImportPrice` |
| Fixed Export Price (€0.05) | 4 pages | `pricing.fixedExportPrice` |
| Battery Capacity (5 kWh) | 3 pages | `battery.defaultCapacityKwh` |
| System Capacity (5 kWp) | 1 page | `solar.defaultSystemCapacityKwp` |
| System Cost (€10,000) | 1 page | `solar.defaultSystemCostEur` |

### ⚙️ Technical Constants (Not Configurable)

| Constant | Location | Reason Not Configurable |
|----------|----------|-------------------------|
| Battery Efficiency (95%) | BatterySimulationService | Physical property of lithium-ion |
| Charge Rate (C/2) | BatterySimulationService | Industry standard, safety |
| Peak Sun Hours (4.0) | EfficiencyMetrics | Belgian climate average |
| Quarter Hour Fraction (0.25) | BatterySimulationService | Data granularity (15 min intervals) |

**Note**: Battery efficiency is now in config but also kept in BatterySimulationService for now. Future enhancement could refactor service to use config.

---

## Migration Summary

### Before (Scattered)
```csharp
// 5+ files with hardcoded values
BatterySimulation: batteryCapacity = 5;
DailyCostAnalysis: batteryCapacity = 5;
RoiAnalysis: batteryCapacity = 5;
EfficiencyMetrics: systemCapacityKwp = 5.0;
CostSavings: systemCost = 10000;
```

### After (Centralized)
```json
// One config file
{
  "battery": { "defaultCapacityKwh": 5.0 },
  "solar": { 
    "defaultSystemCapacityKwp": 5.0,
    "defaultSystemCostEur": 10000
  }
}
```

```csharp
// All pages
await PricingConfig.LoadConfigAsync();
batteryCapacity = PricingConfig.DefaultBatteryCapacityKwh;
```

---

## Benefits

### ✅ Single Source of Truth
- All defaults in one file
- No hunting through code for constants
- Easy to see all configurable values

### ✅ Regional Adaptation
Easy to customize for different markets:
```json
// Belgium (current)
{ "pricing": { "fixedImportPrice": 0.30, "fixedExportPrice": 0.05 } }

// Netherlands
{ "pricing": { "fixedImportPrice": 0.25, "fixedExportPrice": 0.08 } }

// Germany  
{ "pricing": { "fixedImportPrice": 0.35, "fixedExportPrice": 0.07 } }
```

### ✅ Consistency
- Same defaults across all pages automatically
- Changes apply everywhere instantly
- No risk of page-to-page inconsistency

### ✅ User Experience
- Sensible defaults for Belgian market
- Users can still override in UI where needed
- Clear documentation of assumptions

---

## How to Update Configuration

### Method 1: Edit JSON File
1. Open `wwwroot/config/app-config.json`
2. Modify desired values
3. Update `lastUpdated` field
4. Save file
5. Refresh application

**Example**: Increase default battery size to 10 kWh
```json
{
  "battery": {
    "defaultCapacityKwh": 10.0
  },
  "lastUpdated": "2025-10-15"
}
```

### Method 2: Programmatic (Future)
```csharp
await PricingConfig.UpdateConfigAsync(0.35, 0.06);
```

---

## Console Output

When config loads successfully:
```
App config loaded successfully
  Pricing: Import=€0.30/kWh, Export=€0.05/kWh
  Battery: 5.0kWh, Efficiency=95%
  Solar: 5.0kWp system
```

When config fails to load:
```
Failed to load app config: <error>. Using defaults.
```

---

## Future Enhancements

### 1. Per-Page Configs
For page-specific settings not used elsewhere:
```json
{
  "pageConfigs": {
    "smartUsageAdvisor": {
      "defaultForecastHours": 24,
      "devices": [
        { "name": "Dishwasher", "powerKw": 1.5, "durationHours": 2.0 }
      ]
    }
  }
}
```

### 2. Regional Presets
```json
{
  "regions": {
    "belgium": { /* current defaults */ },
    "netherlands": { /* NL-specific */ },
    "germany": { /* DE-specific */ }
  },
  "activeRegion": "belgium"
}
```

### 3. Time-of-Use Defaults
```json
{
  "pricing": {
    "timeOfUse": {
      "peak": { "hours": "17-21", "import": 0.40 },
      "offPeak": { "hours": "23-07", "import": 0.20 }
    }
  }
}
```

### 4. Battery Technology Profiles
```json
{
  "battery": {
    "profiles": {
      "lithiumIon": { "efficiency": 0.95, "degradation": 0.02 },
      "leadAcid": { "efficiency": 0.80, "degradation": 0.05 }
    }
  }
}
```

### 5. Configuration UI
- Admin page to edit all settings
- Preview impact before saving
- Export/import config files
- Reset to defaults option

---

## Testing

### Verify Loading
1. Open browser console (F12)
2. Refresh any page
3. Look for: `App config loaded successfully`

### Test Fallback
1. Rename `app-config.json`
2. Refresh application
3. Should see: `Failed to load app config. Using defaults.`
4. Application continues working with hardcoded defaults

### Test Value Changes
1. Edit `app-config.json`: Change `defaultBatteryCapacityKwh` to 10
2. Hard refresh (Ctrl+F5)
3. Open BatterySimulation page
4. Battery capacity dropdown should default to 10 kWh

---

## Summary

**Centralized**: 
- ✅ Fixed import/export prices (5 pages)
- ✅ Battery capacity default (3 pages)
- ✅ Solar system capacity (1 page)
- ✅ Solar system cost (1 page)
- ✅ Peak sun hours (available for use)
- ✅ Battery efficiency (available for use)

**Page-Specific**: 
- ℹ️ None yet - all current constants are now centralized

**Technical/Immutable**: 
- ⚙️ Quarter-hour fraction (0.25) - data granularity
- ⚙️ Other calculation constants in algorithms

The application now has a robust, extensible configuration system that makes it easy to:
- Customize defaults for different regions
- Update values without code changes
- Maintain consistency across all pages
- Document assumptions clearly
