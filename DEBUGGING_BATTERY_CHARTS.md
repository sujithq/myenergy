# Debugging Battery Simulation Empty Charts

## Changes Made

### 1. **Enhanced Lifecycle Management**
- Added `OnAfterRenderAsync` to ensure charts render after DOM is ready
- Added `shouldRenderCharts` flag to control when charts should render
- Added 200ms delay in `OnAfterRenderAsync` to ensure DOM has settled
- Explicit `StateHasChanged()` call after simulations complete

### 2. **Comprehensive C# Logging** (BatterySimulation.razor)
Added console logging at every step:
- `RunSimulationAsync`: When simulations start and complete
- `OnAfterRenderAsync`: When about to render charts
- `RenderCharts`: Function availability check, data validation, each chart render step
- Full exception details including stack trace

### 3. **JavaScript Function Availability Check**
- Checks if `renderCostComparisonChart` function exists before calling
- Retries once after 500ms if not available
- Aborts gracefully if functions still not loaded

### 4. **Enhanced JavaScript Logging** (charts.js)
- Script load confirmation
- Chart.js library availability check
- Canvas element existence verification
- Chart creation confirmation for each chart

## How to Debug

### Run the Application
From Visual Studio or Windows Terminal (not WSL):
```powershell
cd C:\Users\SujithQuintelier\source\repos\GitHub\sujithq\myenergy2\src\myenergy
dotnet run
```

### Open Browser Console
1. Open the application in browser
2. Press **F12** to open Developer Tools
3. Go to **Console** tab
4. Navigate to **Battery Simulation** page

### Expected Console Output (Success Scenario)

```
charts.js loaded successfully
Chart.js available: true
RunSimulationAsync: Starting simulations
RunSimulationAsync: Simulations complete. Results count: 365
RunSimulationAsync: Set shouldRenderCharts = true
OnAfterRenderAsync: About to render charts
RenderCharts: JavaScript functions available: true
RenderCharts: Starting with 365 daily results
Cost comparison data: 1234.56, 987.65, 1100.00, 850.00
renderCostComparisonChart called with costs: [1234.56, 987.65, 1100.00, 850.00]
createChart called for: costComparisonChart
Canvas element found: costComparisonChart, creating chart...
Chart created successfully for canvas: costComparisonChart
Cost comparison chart rendered
Savings breakdown data: 246.91, 134.56, 137.65, 384.56
renderSavingsBreakdownChart called with savings: [246.91, 134.56, 137.65, 384.56]
createChart called for: savingsBreakdownChart
Canvas element found: savingsBreakdownChart, creating chart...
Chart created successfully for canvas: savingsBreakdownChart
Savings breakdown chart rendered
Cumulative cost chart: 365 dates, 365 fixed, 365 no battery, 365 with battery
renderCumulativeCostChart called with: {labelCount: 365, fixedCount: 365, ...}
createChart called for: cumulativeCostChart
Canvas element found: cumulativeCostChart, creating chart...
Chart created successfully for canvas: cumulativeCostChart
Cumulative cost chart rendered
Battery SoC chart: 168 time points, capacity: 10 kWh
renderBatterySocChart called with: {labelCount: 168, socCount: 168, capacity: 10}
createChart called for: batterySocChart
Canvas element found: batterySocChart, creating chart...
Chart created successfully for canvas: batterySocChart
Battery SoC chart rendered
RenderCharts: All charts rendered successfully
```

### Common Error Scenarios

#### Scenario 1: Chart.js Not Loaded
**Console Output:**
```
Chart.js available: false
createChart called for: costComparisonChart
Chart.js library is not loaded!
```
**Fix:** Check that Chart.js CDN link in index.html is accessible

#### Scenario 2: Canvas Elements Not Found
**Console Output:**
```
Canvas element with id 'costComparisonChart' not found
```
**Fix:** DOM not ready, increase delay in OnAfterRenderAsync

#### Scenario 3: JavaScript Functions Not Available
**Console Output:**
```
RenderCharts: JavaScript functions available: false
RenderCharts: Chart functions not loaded yet, waiting...
RenderCharts: After retry, functions available: false
RenderCharts: Chart functions still not available, aborting
```
**Fix:** charts.js not loaded, check script tag in index.html

#### Scenario 4: No Data
**Console Output:**
```
RenderCharts: Simulation results are null
```
or
```
RenderCharts: No daily results available
```
**Fix:** Data loading issue, check BatterySimulationService

#### Scenario 5: JavaScript Interop Exception
**Console Output:**
```
RenderCharts error: [exception message]
Stack trace: [stack trace]
```
**Fix:** Check the specific error message for details

#### Scenario 6: Hanging (No Console Output)
**Possible Causes:**
- `RunSimulationAsync` never completes (check if data services are hanging)
- `OnAfterRenderAsync` never fires (check shouldRenderCharts flag)
- Infinite loop in simulation logic

**Debug Steps:**
1. Check if you see "RunSimulationAsync: Starting simulations"
2. If yes but no "Simulations complete", the simulation service is hanging
3. If "Simulations complete" but no "OnAfterRenderAsync", check StateHasChanged() call
4. If "OnAfterRenderAsync" but no "RenderCharts", check shouldRenderCharts flag

## Quick Test

To verify the setup works, you can run this in the browser console:
```javascript
// Check if Chart.js is loaded
typeof Chart !== 'undefined'  // Should return: true

// Check if our functions are available
typeof renderCostComparisonChart === 'function'  // Should return: true

// Check if canvas elements exist
document.getElementById('costComparisonChart')  // Should return: <canvas> element
```

## Next Steps After Identifying Issue

Based on the console output, the specific problem will be clear and easy to fix. The extensive logging will pinpoint:
- Timing issues
- Missing libraries
- Data problems
- DOM issues
- JavaScript errors

Once you run the app and provide the console output, I can provide the exact fix needed!
