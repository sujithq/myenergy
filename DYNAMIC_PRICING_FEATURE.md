# Dynamic Pricing Start Date Feature

## Summary
Added the ability to configure when dynamic ODS pricing should start being used in ROI analysis calculations.

## Changes Made

### 1. UI Changes (`Pages/RoiAnalysis.razor`)
- **Added dynamic pricing start date input field** (line 134-142)
  - Only shows when "Use Dynamic (ODS)" checkbox is enabled
  - Date picker with min/max constraints from available ODS data
  - Shows available date range below the input
  
- **Disabled fixed price fields when dynamic pricing is enabled** (lines 116-123)
  - Import and Export price fields are disabled when using dynamic pricing
  - Prevents confusion about which pricing is being used

- **Added ODS date range tracking variables** (lines 290-292)
  - `odsPricingMinDate` - First available ODS pricing date
  - `odsPricingMaxDate` - Last available ODS pricing date  
  - `odsPricingDateRange` - Human-readable date range display

### 2. Service Integration
- **Injected OdsPricingService** (line 6)
  - Now available as `OdsService` in the component
  
- **Initialize ODS pricing data** (lines 299-308)
  - Loads ODS pricing data on component initialization
  - Gets available date range
  - Sets default dynamic pricing start date to first available ODS date

- **OnDynamicPricingToggled handler** (lines 327-338)
  - Resets dynamic pricing start date when checkbox is toggled on
  - Triggers recalculation

### 3. Service Layer Changes (`Services/RoiAnalysisService.cs`)
- **Added optional `dynamicPricingStartDate` parameter** (line 24)
  - Defaults to null when not using dynamic pricing
  - Will be used to determine when to switch from fixed to dynamic prices

### 4. Updated Service Call (`Pages/RoiAnalysis.razor` line 376)
- **Passes dynamic pricing start date** to `CalculateRoi()`
  - Only passes the date when dynamic pricing is enabled
  - Null otherwise

## User Experience

### Before Dynamic Pricing is Enabled
```
Fixed Pricing:
Import Price: €0.30/kWh
Export Price: €0.05/kWh
☐ Use Dynamic (ODS)
```

### After Enabling Dynamic Pricing
```
Fixed Pricing:
Import Price: €0.30/kWh [DISABLED]
Export Price: €0.05/kWh [DISABLED]
☑ Use Dynamic (ODS)

Dynamic Pricing Start Date: [2024-01-01▼]
Available: 01/01/2024 - 09/10/2025
```

## Next Steps (Future Implementation)

The infrastructure is now in place, but the actual dynamic pricing logic needs to be implemented:

1. **RoiAnalysisService needs to use the start date**
   - Before start date: Use fixed prices
   - After start date: Use ODS pricing from OdsPricingService
   
2. **BatterySimulationService needs dynamic pricing support**
   - Currently only supports fixed prices
   - Needs to accept ODS pricing data
   - Should use interval-specific prices from ODS

3. **CalculateCostWithSolar needs dynamic pricing logic**
   - Currently has placeholder comment (line 264)
   - Should query OdsPricingService for actual prices
   - Apply per-interval or daily average pricing

## Technical Notes

- ODS pricing data is loaded from `Data/ods153.json`
- Pricing data has 15-minute interval granularity
- Date range is automatically detected from available data
- UI prevents selecting dates outside available range
- Default is first available date when enabling dynamic pricing

## Testing Checklist

- [x] UI displays dynamic pricing start date field when checkbox enabled
- [x] UI hides field when checkbox disabled
- [x] Fixed price fields are disabled when dynamic pricing enabled
- [x] Date range is displayed correctly
- [x] Min/max date constraints work in date picker
- [x] Service receives correct date parameter
- [ ] Actual dynamic pricing calculations work (TODO)
- [ ] Battery simulation uses dynamic prices (TODO)
- [ ] Charts reflect dynamic pricing correctly (TODO)
