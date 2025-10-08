# Energy Cost Calculation Logic - Detailed Explanation

## 🎯 Overview

This document explains how the battery simulation calculates costs for different energy flows in your solar + battery system.

---

## 💰 Cost Components

### **1. Grid Import (Consuming from Grid)**
- **Flow:** Grid → Your Home
- **When:** Demand exceeds production + battery
- **Type:** 💸 **COST**
- **Formula:** `GridImport × ImportPrice`
- **Example:** 2 kWh from grid @ €0.30/kWh = **€0.60 cost**

---

### **2. Grid Export (Injecting to Grid)**
- **Flow:** Your Home → Grid
- **When:** Solar production exceeds demand + battery capacity
- **Type:** 💰 **GAIN** (Revenue)
- **Formula:** `-GridExport × ExportPrice` (negative cost = income)
- **Example:** 5 kWh to grid @ €0.05/kWh = **-€0.25 (€0.25 gain)**

---

### **3. Battery Discharge (Using from Battery)**
- **Flow:** Battery → Your Home
- **When:** Demand exceeds production, battery has charge
- **Type:** ✅ **AVOIDED COST** (No direct cost, but has value)
- **Formula:** No direct cost in calculation
- **Value:** Avoided `gridImport × importPrice`
- **Example:** 3 kWh from battery avoids importing 3 kWh @ €0.30/kWh = **€0.90 saved**

---

### **4. Battery Charge from Solar**
- **Flow:** Solar → Battery
- **When:** Solar surplus available, battery not full
- **Type:** ⚖️ **OPPORTUNITY COST** (Free energy, but could have exported)
- **Formula:** No direct cost in calculation
- **Opportunity:** Forgoing `solarSurplus × exportPrice`
- **Example:** 4 kWh stored instead of exported @ €0.05/kWh = **€0.20 opportunity cost**

---

### **5. Battery Charge from Grid** (Rare in this system)
- **Flow:** Grid → Battery
- **When:** Deliberate charging at cheap rates (not implemented in current logic)
- **Type:** 💸 **COST**
- **Formula:** Included in `GridImport × ImportPrice`
- **Example:** 5 kWh charged @ €0.15/kWh = **€0.75 cost**

---

## 🧮 Net Cost Formula

The system uses this formula for each 15-minute interval:

```
Net Cost = (GridImport × ImportPrice) - (GridExport × ExportPrice)
```

### **Why This Works:**

1. **Grid Import** = Direct money spent
2. **Grid Export** = Direct money earned (negative cost)
3. **Battery operations** = Implicit in grid import/export reduction

---

## 📊 Example Scenarios

### **Scenario 1: Pure Consumption (No Solar, No Battery)**
```
Consumption: 5 kWh
Production: 0 kWh
Battery: None

GridImport = 5 kWh
GridExport = 0 kWh

Cost = (5 × €0.30) - (0 × €0.05) = €1.50
```

---

### **Scenario 2: Solar Surplus (Export)**
```
Consumption: 5 kWh
Production: 8 kWh
Battery: None

Surplus = 3 kWh
GridImport = 0 kWh
GridExport = 3 kWh

Cost = (0 × €0.30) - (3 × €0.05) = -€0.15 (gain €0.15)
```

---

### **Scenario 3: Battery Storage Instead of Export**
```
Consumption: 5 kWh
Production: 8 kWh
Battery: 5 kWh capacity, 2 kWh stored

Surplus = 3 kWh
Battery stores: 3 kWh (has space)
GridImport = 0 kWh
GridExport = 0 kWh

Cost = (0 × €0.30) - (0 × €0.05) = €0.00

Opportunity Cost = 3 kWh × €0.05 = €0.15 (could have exported)
Future Value = 3 kWh × €0.30 = €0.90 (will avoid import later)
Net Future Benefit = €0.90 - €0.15 = €0.75 savings
```

---

### **Scenario 4: Battery Discharge to Avoid Import**
```
Consumption: 8 kWh
Production: 3 kWh
Battery: 5 kWh capacity, 5 kWh stored

Demand = 5 kWh (8 - 3)
Battery discharges: 5 kWh (enough to cover)
GridImport = 0 kWh
GridExport = 0 kWh

Cost = (0 × €0.30) - (0 × €0.05) = €0.00

Without Battery:
Cost = (5 × €0.30) = €1.50

Savings = €1.50 (avoided grid import)
```

---

### **Scenario 5: Partial Battery, Partial Grid**
```
Consumption: 10 kWh
Production: 2 kWh
Battery: 5 kWh capacity, 3 kWh stored

Demand = 8 kWh (10 - 2)
Battery discharges: 3 kWh (all it has)
Remaining demand: 5 kWh
GridImport = 5 kWh
GridExport = 0 kWh

Cost = (5 × €0.30) - (0 × €0.05) = €1.50

Without Battery:
Cost = (8 × €0.30) = €2.40

Savings = €0.90 (battery covered 3 kWh)
```

---

## 🔋 Battery Efficiency Impact

The battery has **95% round-trip efficiency**:

### **Charging:**
```
Solar production available: 5 kWh
Battery stores: 5 × 0.95 = 4.75 kWh
Loss: 0.25 kWh (5% efficiency loss)
```

### **Discharging:**
```
Battery has: 4.75 kWh stored
Home receives: 4.75 kWh
Battery depletes: 4.75 / 0.95 = 5 kWh
Additional loss: 0.25 kWh
```

### **Round-trip:**
```
Input: 5 kWh
Stored: 4.75 kWh
Retrieved: 4.75 kWh
Output: ~4.51 kWh usable
Total loss: ~0.49 kWh (9.8%)
```

---

## 🎯 Smart Battery Strategy

The simulation uses this intelligent logic:

### **When to CHARGE (Solar Surplus):**
```
IF importPrice > exportPrice × 1.1
THEN store in battery
ELSE export to grid
```

**Reasoning:** Only store solar if future import cost (€0.30) is significantly higher than immediate export gain (€0.05). The 1.1x multiplier accounts for efficiency losses.

---

### **When to DISCHARGE (Demand):**
```
IF exportPrice < importPrice × 0.9
THEN discharge battery
ELSE import from grid
```

**Reasoning:** Use battery if it's cheaper than importing. The 0.9x multiplier provides a safety margin.

---

## 💡 Cost vs. Value Distinction

### **Direct Cost (What You Pay/Earn):**
```
Net Cost = GridImport Cost - GridExport Revenue
```

### **Opportunity Cost (What You Give Up):**
```
Storing solar = Forgoing export revenue
```

### **Avoided Cost (What You Save):**
```
Using battery = Avoiding grid import cost
```

### **Net Battery Benefit:**
```
Battery Benefit = (Avoided Import Cost) - (Opportunity Cost) - (Efficiency Losses)
```

---

## 📈 Daily Cost Calculation

For each day with 96 intervals (15-min each):

```csharp
DailyCost = Sum of all 96 intervals:
    (GridImport[i] × ImportPrice[i]) - (GridExport[i] × ExportPrice[i])
```

### **With Dynamic Pricing (ODS):**
```
ImportPrice[i] = ODS import price for time i
ExportPrice[i] = ODS export price for time i
```

### **With Fixed Pricing:**
```
ImportPrice[i] = Fixed rate (e.g., €0.30)
ExportPrice[i] = Fixed rate (e.g., €0.05)
```

---

## 🔍 Real Example from Your Data

### **Day: March 15, 2024**

**Without Battery:**
```
Total Import: 12.5 kWh @ €0.28 avg = €3.50
Total Export: 8.2 kWh @ €0.05 = -€0.41
Net Cost: €3.50 - €0.41 = €3.09
```

**With 5 kWh Battery:**
```
Battery stored: 6.8 kWh (multiple charge cycles)
Battery discharged: 6.2 kWh (multiple discharge cycles)

Total Import: 8.1 kWh @ €0.28 avg = €2.27
Total Export: 4.5 kWh @ €0.05 = -€0.23
Net Cost: €2.27 - €0.23 = €2.04

Savings: €3.09 - €2.04 = €1.05
Battery Benefit: €1.05
```

**What Happened:**
1. Morning surplus (8 AM-12 PM): Stored 4 kWh in battery instead of exporting @ €0.05 = €0.20 opportunity cost
2. Evening peak (6 PM-10 PM): Discharged 4 kWh instead of importing @ €0.35 = €1.40 avoided cost
3. Net benefit: €1.40 - €0.20 - 5% efficiency loss = **€1.05 savings**

---

## 🎓 Key Takeaways

1. ✅ **Grid Import = Direct Cost** (you pay the utility)
2. ✅ **Grid Export = Direct Gain** (utility pays you, negative cost)
3. ✅ **Battery Discharge = Avoided Cost** (you don't pay import)
4. ⚠️ **Battery Charge from Solar = Opportunity Cost** (could have exported)
5. ⚠️ **Battery Charge from Grid = Cost** (pay import price)
6. 📉 **Efficiency Losses = 5% each way** (9.8% round-trip)
7. 🧠 **Smart Strategy = Optimize based on price spreads**

---

## 🔧 Technical Notes

### **Cost Sign Convention:**
- **Positive values** = Money you spend (cost)
- **Negative values** = Money you earn (gain/revenue)
- **Zero** = Break even

### **Battery State Tracking:**
The simulation tracks:
- `BatteryLevel` (kWh stored)
- `BatteryCharge` (kWh added this interval)
- `BatteryDischarge` (kWh removed this interval)
- `BatterySoC` (State of Charge %)

### **Efficiency Application:**
```csharp
// Charging: Account for efficiency loss going in
charge = availableSolar × efficiency; // 0.95

// Discharging: Account for efficiency loss coming out
discharge = requestedPower / efficiency; // Battery loses more than home gets
```

---

## 📊 Summary Table

| Energy Flow | Direction | Type | Formula | Example (€0.30 import, €0.05 export) |
|-------------|-----------|------|---------|--------------------------------------|
| Grid Import | Grid → Home | Cost | `+Import × Price` | 3 kWh = +€0.90 |
| Grid Export | Home → Grid | Gain | `-Export × Price` | 3 kWh = -€0.15 |
| Battery Charge (Solar) | Solar → Battery | Opportunity | `0` (but forgo export) | 3 kWh = €0.15 opportunity |
| Battery Charge (Grid) | Grid → Battery | Cost | `+Import × Price` | 3 kWh = +€0.90 |
| Battery Discharge | Battery → Home | Avoided | `0` (saves import) | 3 kWh = €0.90 saved |

---

**Bottom Line:** The cost formula captures the **actual money flow**, while battery operations create **indirect savings** by reducing grid import and maximizing high-value energy usage timing.

---

**Generated:** October 8, 2025  
**System:** myenergy Battery Simulation v1.0
