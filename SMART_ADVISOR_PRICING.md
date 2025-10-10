# Smart Usage Advisor - Dynamic Pricing Explained

## Overview
The Smart Usage Advisor can operate in two pricing modes: **Dynamic Pricing** (default) or **Fixed Pricing**. This affects how device recommendations are calculated and what costs are displayed.

---

## Pricing Modes

### 🔄 Dynamic Pricing (Toggle ON)
**Uses real-time ODS (Elia) prices**

#### How it works:
- Import prices vary by hour (typically €0.05 - €0.25/kWh)
- Export prices vary by hour (can be negative during oversupply)
- Recommendations optimize for **lowest import prices**
- Cost calculations use **actual hourly rates**
- Savings are calculated based on **price differences** between time slots

#### Example Device Cost:
- Dishwasher (1.5 kW × 2 hours = 3 kWh)
- **Now (14:00)**: €0.18/kWh → Cost: €0.54
- **Best time (23:00)**: €0.08/kWh → Cost: €0.24
- **💰 Savings**: €0.30 if you wait!

#### Best for:
- Users with dynamic pricing contracts (e.g., Eneco, Bolt, Tibber)
- Maximizing savings through time-shifting
- Taking advantage of negative price hours
- Understanding real-time grid pricing

---

### 🔒 Fixed Pricing (Toggle OFF)
**Uses standard residential tariff**

#### How it works:
- Import price: **€0.30/kWh** (constant)
- Export price: **€0.05/kWh** (constant)
- Recommendations optimize for **solar production only**
- Cost calculations use **fixed rate**
- Savings come from **avoiding grid import**, not price differences

#### Example Device Cost:
- Dishwasher (1.5 kW × 2 hours = 3 kWh)
- **Now (14:00)**: €0.30/kWh → Cost: €0.90 (but covered by solar!)
- **Best time (23:00)**: €0.30/kWh → Cost: €0.90 (requires grid import)
- **💰 Savings**: Use during solar peak to avoid any grid import

#### Best for:
- Users with traditional fixed-rate contracts
- Focusing purely on solar self-consumption
- Avoiding grid dependency
- Simpler cost estimates

---

## What Changes When You Toggle?

| Aspect | Dynamic Pricing ON | Fixed Pricing OFF |
|--------|-------------------|-------------------|
| **Import Cost** | Varies by hour (€0.05-0.25/kWh) | Fixed at €0.30/kWh |
| **Export Revenue** | Varies by hour (negative to €0.15/kWh) | Fixed at €0.05/kWh |
| **Best Time Logic** | Lowest import price + high solar | Highest solar production |
| **Savings Calculation** | Price difference × energy used | Grid import avoided |
| **Recommendation Focus** | Time-of-use arbitrage | Solar self-consumption |
| **Status Message** | Shows current import price | Shows fixed rate |
| **Device Reasons** | Mentions prices | Mentions solar coverage |

---

## Real-World Scenarios

### Scenario 1: Sunny Afternoon (14:00)
**Dynamic Pricing ON:**
- Solar: 4.0 kWh/h production ✅
- Import price: €0.08/kWh (low due to oversupply) ✅
- **Recommendation**: START NOW (excellent conditions)
- **Reason**: High solar + low prices
- **Dishwasher cost**: €0.54 → €0.24 savings available

**Fixed Pricing OFF:**
- Solar: 4.0 kWh/h production ✅
- Import price: €0.30/kWh (fixed) 
- **Recommendation**: START NOW (excellent conditions)
- **Reason**: Optimal solar production
- **Dishwasher cost**: €0.90 (but free from solar!)

### Scenario 2: Evening Peak (19:00)
**Dynamic Pricing ON:**
- Solar: 0.2 kWh/h (sunset) ❌
- Import price: €0.22/kWh (evening peak) ❌
- **Recommendation**: WAIT until 23:00
- **Reason**: Better to wait for lower prices
- **Dishwasher cost**: €0.66 now → €0.24 at 23:00 = €0.42 savings

**Fixed Pricing OFF:**
- Solar: 0.2 kWh/h (sunset) ❌
- Import price: €0.30/kWh (fixed)
- **Recommendation**: WAIT until tomorrow's solar peak
- **Reason**: Better to wait for solar coverage
- **Dishwasher cost**: €0.90 (same any time, but solar = free)

### Scenario 3: Night Time (02:00)
**Dynamic Pricing ON:**
- Solar: 0.0 kWh/h (night) ❌
- Import price: €0.06/kWh (very low) ✅
- **Recommendation**: OK TO START (if urgent)
- **Reason**: No solar but very cheap grid power
- **Dishwasher cost**: €0.18 (cheapest time!)

**Fixed Pricing OFF:**
- Solar: 0.0 kWh/h (night) ❌
- Import price: €0.30/kWh (fixed) ❌
- **Recommendation**: WAIT until solar hours
- **Reason**: All grid import costs the same
- **Dishwasher cost**: €0.90 (no benefit to night usage)

---

## Key Insights

### When to Use Dynamic Pricing Mode ✅
- You have a **variable tariff** contract
- You want to **shift loads** to cheap hours
- You care about **price arbitrage**
- You want to see **real savings** from time-shifting
- You're interested in **grid dynamics**

### When to Use Fixed Pricing Mode ✅
- You have a **traditional contract** (single rate)
- You want to **maximize solar usage**
- You prefer **simpler cost estimates**
- You only care about **self-consumption**
- You want to **minimize grid dependency**

---

## Technical Details

### Constants Used (when Fixed Pricing is enabled)
```csharp
FIXED_IMPORT_PRICE = €0.30/kWh  // Typical Belgian residential rate
FIXED_EXPORT_PRICE = €0.05/kWh  // Typical feed-in tariff
```

### Dynamic Pricing Data Source
- **Source**: Elia ODS (ods134 dataset)
- **Resolution**: 15-minute intervals
- **Range**: May 2024 - October 2025
- **Updated**: Daily via API
- **Fallback**: Local JSON file

### Cost Calculation Logic
```csharp
// Dynamic Pricing ON
currentPrice = hourlyPattern.AvgImportPrice;  // e.g., €0.08/kWh
bestPrice = bestHourPattern.AvgImportPrice;   // e.g., €0.06/kWh

// Fixed Pricing OFF
currentPrice = €0.30/kWh;  // Constant
bestPrice = €0.30/kWh;     // Constant (no time-of-use difference!)
```

---

## Belgium-Specific Context

### Why Dynamic Pricing Matters
- Belgium had **262+ hours of negative prices** in 2024
- Oversupply during sunny afternoons drives prices down
- Evening peaks drive prices up
- Dynamic contracts can save **€20-50/month** vs fixed rates

### Typical Belgian Price Ranges
- **Night (00:00-06:00)**: €0.05-0.10/kWh
- **Morning (06:00-09:00)**: €0.12-0.18/kWh
- **Midday (09:00-15:00)**: €0.04-0.12/kWh (solar oversupply)
- **Evening (17:00-21:00)**: €0.15-0.25/kWh (peak demand)
- **Late night (21:00-24:00)**: €0.06-0.12/kWh

### Fixed Rate Benchmark
- Typical Belgian fixed rate: **€0.28-0.35/kWh**
- Feed-in tariff: **€0.03-0.07/kWh** (decreasing)

---

## User Interface Changes

When you **toggle Dynamic Pricing OFF**, you'll see:

1. **Status Banner**: Shows "Fixed rate: €0.30/kWh" instead of hourly price
2. **Device Cards**: Reasons mention "solar coverage" instead of "prices"
3. **Cost Estimates**: Still calculated, but always at €0.30/kWh
4. **Savings**: Focus on avoiding grid import (solar usage) vs price arbitrage
5. **Timeline Chart**: Still shows patterns, but price optimization is disabled
6. **Best Time Slots**: Optimized for solar production only

---

## Recommendations

### For Most Users
**Start with Dynamic Pricing ON** to see the full picture, then:
- If you have a variable contract → **Keep it ON**
- If you have a fixed contract → **Switch it OFF** for more relevant advice

### For Battery Owners
**Always use Dynamic Pricing ON** because:
- Battery charging decisions depend on price forecasts
- Arbitrage opportunities (charge cheap, discharge expensive)
- Export timing matters (avoid negative prices)

### For Solar-Only Systems
- **Fixed Pricing OFF** may be simpler
- Focus is purely on self-consumption
- Recommendations align with solar availability

---

## FAQ

**Q: Does the toggle affect historical data?**
A: No, it only affects how recommendations are calculated and displayed. Historical data remains the same.

**Q: Which mode shows real savings?**
A: Both show real savings, but "Dynamic Pricing ON" shows **price arbitrage savings**, while "Fixed Pricing OFF" shows **grid import avoidance savings**.

**Q: Can I use dynamic mode with a fixed contract?**
A: Yes! It's educational to see what savings you *could* achieve with a dynamic contract.

**Q: Why are costs higher with fixed pricing?**
A: Fixed rates (€0.30/kWh) are typically higher than average dynamic rates (€0.10-0.15/kWh avg) to account for risk and simplicity.

**Q: Does this affect actual billing?**
A: No, this is for **advice only**. Your actual costs depend on your real contract with your energy supplier.

---

## Summary

The **Use Dynamic Pricing** toggle allows you to:
- ✅ See advice tailored to your contract type
- ✅ Compare dynamic vs fixed pricing scenarios
- ✅ Understand when time-shifting saves money
- ✅ Focus on solar self-consumption if prices don't vary
- ✅ Make informed decisions about contract types

**Default: ON** - Shows the full power of smart energy management with real-time pricing!
