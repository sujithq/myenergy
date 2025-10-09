# ODS Pricing Data Format Update

## Date: January 2025
## Change: Switched from ods153.json to ods134.json with Belgian ODS marginal prices

## New Data Format

### Source File: `wwwroot/Data/ods134.json`

**Belgian ODS (Operational Deviation Settlement) Data Structure:**

```json
{
    "datetime": "2025-10-08T13:00:00+02:00",
    "resolutioncode": "PT15M",
    "qualitystatus": "NotValidated",
    "ace": 22.82,
    "systemimbalance": -19.669,
    "alpha": 0.0,
    "alpha_prime": 0.0,
    "marginalincrementalprice": 100.06,    // Import price (€/MWh)
    "marginaldecrementalprice": 100.0,     // Export price (€/MWh)
    "imbalanceprice": 100.06
}
```

### Key Fields Used:

- **`marginalincrementalprice`** (€/MWh): Price you PAY when importing from grid
  - This is what it costs to use grid electricity
  - Always positive (it's a cost)

- **`marginaldecrementalprice`** (€/MWh): Price you GET (or pay) when exporting to grid
  - Positive = You get paid (revenue)
  - Zero = No payment
  - Negative = You pay to export (during grid oversupply)

- **`datetime`**: 15-minute interval timestamp with timezone

### Price Calculation Formula

For each 15-minute interval:

```csharp
// Convert MWh prices to kWh prices (divide by 1000)
import_price_per_kwh = marginalincrementalprice / 1000.0
export_price_per_kwh = marginaldecrementalprice / 1000.0

// Calculate cost/revenue
import_cost  = (import_kWh) * import_price_per_kwh
export_value = (export_kWh) * export_price_per_kwh
net_cost     = import_cost - export_value
```

**Sign Convention:**
- Import cost: Always POSITIVE (you pay)
- Export value: POSITIVE = revenue, NEGATIVE = cost (you pay to export)
- Net cost: POSITIVE = you owe money, NEGATIVE = you earn money

## Model Changes

### Updated `OdsPricing` Record (EnergyPoint.cs)

```csharp
public record OdsPricing
{
    // NEW FORMAT (ods134.json) - Marginal prices
    public double? MarginalIncrementalPrice { get; init; }  // Import (€/MWh)
    public double? MarginalDecrementalPrice { get; init; }  // Export (€/MWh)
    public double? ImbalancePrice { get; init; }
    public string? QualityStatus { get; init; }
    
    // OLD FORMAT (ods153.json) - Legacy aFRR/mFRR - kept for backward compatibility
    public double? DownwardAvailableAfrrPrice { get; init; }
    public double? DownwardAvailableMfrrPrice { get; init; }
    public double? UpwardAvailableAfrrPrice { get; init; }
    public double? UpwardAvailableMfrrPrice { get; init; }
    
    // Best available prices (prefer new format, fallback to old)
    public double InjectionPrice => MarginalDecrementalPrice 
        ?? DownwardAvailableAfrrPrice 
        ?? DownwardAvailableMfrrPrice 
        ?? 0;
        
    public double ImportPrice => MarginalIncrementalPrice 
        ?? UpwardAvailableAfrrPrice 
        ?? UpwardAvailableMfrrPrice 
        ?? 0;
    
    // Convert €/MWh to €/kWh
    public double InjectionPricePerKwh => InjectionPrice / 1000.0;
    public double ImportPricePerKwh => ImportPrice / 1000.0;
}
```

**Backward Compatibility:**
- If `MarginalDecrementalPrice` exists → use it
- Else if `DownwardAvailableAfrrPrice` exists → use it (old format)
- Else if `DownwardAvailableMfrrPrice` exists → use it (old format)
- Else → default to 0

## Service Changes

### Updated `OdsPricingService.cs`

**Changed data source:**
```csharp
// OLD:
var json = await _http.GetStringAsync("Data/ods153.json");

// NEW:
var json = await _http.GetStringAsync("Data/ods134.json");
```

**Added logging:**
```csharp
Console.WriteLine($"Loaded {_pricingData.Count} ODS pricing records from ods134.json");
Console.WriteLine($"Date range: {minDate} to {maxDate}");
Console.WriteLine($"Average import price: €{avgImport:F4}/kWh");
Console.WriteLine($"Average export price: €{avgExport:F4}/kWh");
```

## Real-World ODS Pricing Examples

### Sample from October 8, 2025:

**High Export Price (Good for solar):**
```
Time: 13:00 - Midday solar peak
Import:  €100.06/MWh = €0.10006/kWh
Export:  €100.00/MWh = €0.10000/kWh
→ Exporting is profitable! Almost equal to import price.
```

**Zero Export Price (Storage recommended):**
```
Time: 00:00 - Night time
Import:  €109.22/MWh = €0.10922/kWh
Export:  €0.00/MWh = €0.00000/kWh
→ Exporting gets you nothing. Battery should store instead.
```

**Negative Export Price (Must store or consume):**
```
Time: 12:30 - Solar oversupply
Import:  €100.12/MWh = €0.10012/kWh
Export:  €78.649/MWh = €0.07865/kWh
→ Export price lower than import. Battery should charge.
```

## Impact on Battery Simulation

With the new data format, the battery simulation will:

1. **Use actual Belgian ODS settlement prices** - more accurate than aFRR/mFRR
2. **See real grid dynamics** - zero export prices at night, varying prices during day
3. **Make better decisions**:
   - Charge battery when export price is low/zero
   - Discharge battery when import price is high
   - Avoid exporting when it's unprofitable

## Benefits

✅ **More Accurate**: Using actual settlement prices instead of reserve prices
✅ **Real-Time Data**: ods134.json can be updated with latest prices
✅ **Better ROI Calculation**: Reflects actual grid economics
✅ **Backward Compatible**: Old format still works if needed

## Testing

Test scenarios:
- [ ] Load ods134.json successfully
- [ ] Prices converted correctly (€/MWh → €/kWh)
- [ ] Battery avoids exporting when price is zero
- [ ] ROI calculation uses correct marginal prices
- [ ] Date range detection works for October 2025 data

## Data Source

Belgian ODS pricing data comes from:
- **Elia Open Data Platform**: https://opendata.elia.be
- **API**: Operational Deviation Settlement prices
- **Resolution**: 15-minute intervals (PT15M)
- **Fields**: `marginalincrementalprice`, `marginaldecrementalprice`
- **Timezone**: Europe/Brussels (CET/CEST)

## Next Steps

To get historical data:
1. Download ODS pricing from Elia Open Data for your date range
2. Save as `wwwroot/Data/ods134.json`
3. Ensure JSON format matches the structure above
4. Application will automatically use the new prices

---

**Note**: The old ods153.json file (aFRR/mFRR prices) was from a different pricing mechanism. The new ods134.json uses actual settlement prices which better reflect grid economics.
