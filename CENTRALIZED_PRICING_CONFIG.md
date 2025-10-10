# Centralized Pricing Configuration

## Overview
All pages in the myenergy application now use a **centralized pricing configuration** system. Fixed import and export prices are stored in a JSON configuration file and loaded via the `PricingConfigService`.

---

## Configuration File

**Location**: `wwwroot/config/pricing.json`

```json
{
  "fixedImportPrice": 0.30,
  "fixedExportPrice": 0.05,
  "description": "Default Belgian electricity pricing (€/kWh)",
  "lastUpdated": "2025-10-10",
  "notes": {
    "importPrice": "Typical Belgian residential fixed tariff",
    "exportPrice": "Typical feed-in tariff for solar export"
  }
}
```

### Fields
- **fixedImportPrice**: Cost per kWh when importing from grid (€/kWh)
- **fixedExportPrice**: Revenue per kWh when exporting to grid (€/kWh)
- **description**: Human-readable description
- **lastUpdated**: Date when values were last updated
- **notes**: Additional context about the pricing

---

## Service Architecture

### PricingConfigService

**Location**: `Services/PricingConfigService.cs`

#### Features
- Loads configuration from JSON file at startup
- Provides readonly access to pricing values
- Falls back to safe defaults (€0.30 import, €0.05 export) if file load fails
- Singleton-like behavior (loads once per session)

#### API
```csharp
public class PricingConfigService
{
    // Properties
    public double FixedImportPrice { get; }  // Default: 0.30
    public double FixedExportPrice { get; }  // Default: 0.05
    public bool IsLoaded { get; }
    
    // Methods
    public Task LoadConfigAsync();
    public Task UpdateConfigAsync(double importPrice, double exportPrice);
}
```

#### Registration
```csharp
// Program.cs
builder.Services.AddScoped<PricingConfigService>();
```

---

## Pages Using Pricing Configuration

### 1. SmartUsageAdvisor.razor (`/smart-usage-advisor`)
**Usage**: Fixed pricing mode for device recommendations

```csharp
@inject PricingConfigService PricingConfig

protected override async Task OnInitializedAsync()
{
    await PricingConfig.LoadConfigAsync();
    // Use PricingConfig.FixedImportPrice and PricingConfig.FixedExportPrice
}
```

**What it uses**:
- `FixedImportPrice` - When dynamic pricing is disabled
- Status messages show fixed rate
- Cost calculations use fixed rate consistently

---

### 2. BatterySimulation.razor (`/battery-simulation`)
**Usage**: Fixed pricing scenario comparison

```csharp
@inject PricingConfigService PricingConfig

// Local variables for UI binding
private double fixedImportPrice = 0.30;
private double fixedExportPrice = 0.05;

protected override async Task OnInitializedAsync()
{
    await PricingConfig.LoadConfigAsync();
    
    // Initialize from config
    fixedImportPrice = PricingConfig.FixedImportPrice;
    fixedExportPrice = PricingConfig.FixedExportPrice;
}
```

**What it uses**:
- Initializes local UI-bound variables from config
- Users can still adjust values in UI
- Default values come from centralized config

---

### 3. DailyCostAnalysis.razor (`/daily-cost-analysis`)
**Usage**: Fixed pricing baseline comparison

```csharp
@inject PricingConfigService PricingConfig

private double fixedImportPrice = 0.30;
private double fixedExportPrice = 0.05;

protected override async Task OnInitializedAsync()
{
    await PricingConfig.LoadConfigAsync();
    fixedImportPrice = PricingConfig.FixedImportPrice;
    fixedExportPrice = PricingConfig.FixedExportPrice;
}
```

**What it uses**:
- Compares dynamic pricing vs fixed pricing
- Fixed pricing scenario uses config values
- Shows savings from dynamic pricing

---

### 4. RoiAnalysis.razor (`/roi-analysis`)
**Usage**: ROI calculations with fixed tariff baseline

```csharp
@inject PricingConfigService PricingConfig

private double fixedImportPrice = 0.30;
private double fixedExportPrice = 0.05;

protected override async Task OnInitializedAsync()
{
    await PricingConfig.LoadConfigAsync();
    fixedImportPrice = PricingConfig.FixedImportPrice;
    fixedExportPrice = PricingConfig.FixedExportPrice;
}
```

**What it uses**:
- Battery investment ROI calculations
- Payback period estimates
- Savings projections with fixed pricing

---

### 5. CostSavings.razor (`/cost-savings`)
**Usage**: Solar system savings calculations

```csharp
@inject PricingConfigService PricingConfig

private double importCostPerKwh = 0.30;

protected override async Task OnInitializedAsync()
{
    await PricingConfig.LoadConfigAsync();
    importCostPerKwh = PricingConfig.FixedImportPrice;
}
```

**What it uses**:
- Cost avoided by solar production
- Payback period calculations
- Monthly/yearly savings estimates

---

## Benefits of Centralized Configuration

### ✅ Consistency
- **Single source of truth** for pricing across all pages
- No more hardcoded values scattered throughout code
- All pages use same defaults automatically

### ✅ Maintainability
- **Update once, apply everywhere**
- Change config file, refresh app - all pages updated
- No code changes needed for pricing updates

### ✅ Flexibility
- **Easy to adjust** for different markets
- Belgian defaults provided, but customizable
- Can add region-specific configs in future

### ✅ Transparency
- **Clear documentation** in JSON file
- Notes explain what each price represents
- Last updated date for audit trail

### ✅ Fallback Safety
- **Graceful degradation** if file missing
- Default values ensure app never breaks
- Console logging for debugging

---

## How to Update Pricing

### Method 1: Edit JSON File
1. Open `wwwroot/config/pricing.json`
2. Update `fixedImportPrice` and/or `fixedExportPrice`
3. Update `lastUpdated` date
4. Save file
5. Refresh application

```json
{
  "fixedImportPrice": 0.35,  // Changed from 0.30
  "fixedExportPrice": 0.04,  // Changed from 0.05
  "lastUpdated": "2025-10-15"
}
```

### Method 2: Programmatic Update (Future)
```csharp
await PricingConfig.UpdateConfigAsync(0.35, 0.04);
```

Note: Currently only updates in-memory. Future enhancement would save to backend/localStorage.

---

## Migration from Old Code

### Before (Inconsistent)
```csharp
// SmartUsageAdvisor.razor
private const double FIXED_IMPORT_PRICE = 0.30;

// BatterySimulation.razor
private double fixedImportPrice = 0.30;

// RoiAnalysis.razor
private double fixedImportPrice = 0.30;

// CostSavings.razor
private double importCostPerKwh = 0.30;
```

**Problems**:
- 4 different variable names for same concept
- Hardcoded values in 5+ files
- Update required changing every file
- Risk of inconsistency

### After (Centralized)
```csharp
// One config file
wwwroot/config/pricing.json: "fixedImportPrice": 0.30

// One service
PricingConfigService.FixedImportPrice

// All pages inject and use
@inject PricingConfigService PricingConfig
await PricingConfig.LoadConfigAsync();
var price = PricingConfig.FixedImportPrice;
```

**Benefits**:
- ✅ One source of truth
- ✅ Consistent naming
- ✅ Easy updates
- ✅ No code changes needed

---

## Default Values Explained

### Import Price: €0.30/kWh
**Belgian residential fixed tariff (2024-2025 average)**
- Includes energy cost (~€0.12/kWh)
- Network costs (~€0.08/kWh)
- Taxes and levies (~€0.07/kWh)
- Supplier margin (~€0.03/kWh)

**Range**: Typically €0.28-0.35/kWh depending on provider

### Export Price: €0.05/kWh
**Typical Belgian feed-in tariff (2024-2025)**
- Based on wholesale market price
- Lower than import due to no network/tax costs
- Decreasing trend in recent years
- Negative during oversupply hours

**Range**: Typically €0.03-0.08/kWh, sometimes negative

---

## Future Enhancements

### Planned Features
1. **UI Configuration Panel**
   - Admin page to edit pricing via UI
   - Save to localStorage for persistence
   - Preview impact on all analyses

2. **Regional Presets**
   ```json
   {
     "regions": {
       "belgium": { "import": 0.30, "export": 0.05 },
       "netherlands": { "import": 0.25, "export": 0.08 },
       "germany": { "import": 0.35, "export": 0.07 }
     }
   }
   ```

3. **Time-of-Use Fixed Rates**
   ```json
   {
     "fixedRates": {
       "peak": { "import": 0.40, "hours": "17-21" },
       "offPeak": { "import": 0.20, "hours": "23-07" },
       "normal": { "import": 0.30, "hours": "07-17,21-23" }
     }
   }
   ```

4. **Historical Pricing**
   ```json
   {
     "history": [
       { "from": "2024-01-01", "import": 0.32, "export": 0.06 },
       { "from": "2024-07-01", "import": 0.30, "export": 0.05 }
     ]
   }
   ```

5. **Backend Persistence**
   - Save user-customized pricing
   - Per-user pricing profiles
   - Sync across devices

---

## Testing

### Verify Configuration Loading
1. Open browser console (F12)
2. Navigate to any page that uses pricing
3. Look for: `Pricing config loaded: Import=€0.30/kWh, Export=€0.05/kWh`

### Test Fallback Behavior
1. Rename `pricing.json` temporarily
2. Refresh app
3. Console should show: `Failed to load pricing config. Using defaults.`
4. App should still work with default values

### Test Configuration Change
1. Edit `pricing.json`: Change import to 0.35
2. Hard refresh app (Ctrl+F5)
3. Check SmartUsageAdvisor status message: Should show "€0.35/kWh"
4. Check BatterySimulation: UI should show 0.35 in import price field

---

## Summary

The pricing configuration system provides:
- **Centralized management** of fixed electricity pricing
- **Consistent values** across all 5+ pages
- **Easy updates** via JSON file
- **Safe defaults** if file unavailable
- **Clear documentation** of pricing assumptions
- **Future-proof** architecture for enhancements

All pages that perform cost calculations now reference the same source of truth, ensuring accuracy and maintainability.
