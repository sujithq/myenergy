# Daily Detail Page - ODS Dynamic Pricing Integration

## Overview

Integration of ODS dynamic pricing into the Daily Detail page to show real-time Belgian grid pricing alongside energy data for each 15-minute interval.

## Changes Required

### 1. DailyDetail.razor Page

#### Add ODS Service Injection
```csharp
@inject OdsPricingService OdsService
```

#### Add Dynamic Pricing UI Controls
Add after the date selection controls:

```html
<!-- Dynamic Pricing Settings -->
<div class="row mb-4">
    <div class="col-md-12">
        <div class="card">
            <div class="card-body">
                <div class="row align-items-center">
                    <div class="col-md-2">
                        <div class="form-check">
                            <input class="form-check-input" type="checkbox" 
                                   @bind="ShowDynamicPricing" 
                                   @bind:after="OnDynamicPricingToggled" 
                                   id="showDynamicPricing">
                            <label class="form-check-label fw-bold" for="showDynamicPricing">
                                <i class="bi bi-graph-up"></i> Show ODS Pricing
                            </label>
                        </div>
                    </div>
                    @if (ShowDynamicPricing)
                    {
                        <div class="col-md-3">
                            <small class="text-muted">
                                @if (!string.IsNullOrEmpty(odsPricingDateRange))
                                {
                                    <span>@odsPricingDateRange</span>
                                }
                                else
                                {
                                    <span class="text-warning">No ODS data available</span>
                                }
                            </small>
                        </div>
                        <div class="col-md-2">
                            <small class="text-muted">Fixed import: €0.30/kWh</small><br/>
                            <small class="text-muted">Fixed export: €0.05/kWh</small>
                        </div>
                        <div class="col-md-3">
                            @if (OdsService.IsDataLoaded)
                            {
                                <small class="text-success">
                                    <i class="bi bi-check-circle"></i> ODS data loaded
                                </small>
                                @if (OdsService.LastLoadTime.HasValue)
                                {
                                    <br/><small class="text-muted">
                                        Updated: @OdsService.LastLoadTime.Value.ToString("yyyy-MM-dd HH:mm")
                                    </small>
                                }
                            }
                            else
                            {
                                <small class="text-danger">
                                    <i class="bi bi-x-circle"></i> ODS data not loaded
                                </small>
                            }
                        </div>
                        <div class="col-md-2">
                            <button class="btn btn-sm btn-outline-primary" 
                                    @onclick="RefreshOdsData" 
                                    disabled="@isRefreshingOds">
                                @if (isRefreshingOds)
                                {
                                    <span class="spinner-border spinner-border-sm"></span>
                                    <span> Refreshing...</span>
                                }
                                else
                                {
                                    <i class="bi bi-arrow-repeat"></i>
                                    <span> Refresh ODS</span>
                                }
                            </button>
                        </div>
                    }
                </div>
            </div>
        </div>
    </div>
</div>
```

#### Update DetailedDayChart Component Call
```html
<DetailedDayChart DetailData="@DetailData" 
                 ShowDynamicPricing="@ShowDynamicPricing"
                 OdsPricingData="@DailyOdsPricing" />
```

#### Add Code-Behind Variables and Methods
```csharp
@code {
    private DateTime SelectedDate = DateTime.Today;
    private DailyDetailData? DetailData;
    private bool Loading = false;
    private List<DateTime> AvailableDates = new();
    
    // Dynamic pricing support
    private bool ShowDynamicPricing = false;
    private bool isRefreshingOds = false;
    private string odsPricingDateRange = "";
    private List<OdsPricing> DailyOdsPricing = new();

    protected override async Task OnInitializedAsync()
    {
        // Load ODS pricing data
        await OdsService.LoadDataAsync();
        UpdateOdsPricingInfo();
        
        // Get available dates
        AvailableDates = DataService.GetAvailableDates();
        
        // Default to current date if available, otherwise most recent
        if (AvailableDates.Contains(DateTime.Today))
        {
            SelectedDate = DateTime.Today;
        }
        else if (AvailableDates.Any())
        {
            SelectedDate = AvailableDates.OrderByDescending(d => d).First();
        }
        
        await LoadDayData();
    }
    
    private void UpdateOdsPricingInfo()
    {
        var odsRange = OdsService.GetDataRange();
        if (odsRange.HasValue)
        {
            odsPricingDateRange = $"Available: {odsRange.Value.start:dd/MM/yyyy} - {odsRange.Value.end:dd/MM/yyyy}";
        }
        else
        {
            odsPricingDateRange = "";
        }
    }
    
    private async Task OnDynamicPricingToggled()
    {
        if (ShowDynamicPricing && DetailData != null)
        {
            // Load ODS pricing for the selected day
            DailyOdsPricing = OdsService.GetPricingForDay(SelectedDate);
        }
        StateHasChanged();
    }
    
    private async Task RefreshOdsData()
    {
        isRefreshingOds = true;
        StateHasChanged();
        
        try
        {
            await OdsService.RefreshFromEliaAsync();
            UpdateOdsPricingInfo();
            
            // Reload pricing data for current day
            if (ShowDynamicPricing && DetailData != null)
            {
                DailyOdsPricing = OdsService.GetPricingForDay(SelectedDate);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing ODS data: {ex.Message}");
        }
        finally
        {
            isRefreshingOds = false;
            StateHasChanged();
        }
    }

    private async Task LoadDayData()
    {
        Loading = true;
        StateHasChanged();

        await Task.Delay(50); // Small delay for UI feedback
        
        DetailData = DataService.GetDailyDetailData(SelectedDate);
        
        // Load ODS pricing for this day if enabled
        if (ShowDynamicPricing && DetailData != null)
        {
            DailyOdsPricing = OdsService.GetPricingForDay(SelectedDate);
        }
        
        Loading = false;
        StateHasChanged();
    }

    // ... rest of existing methods ...
}
```

### 2. DetailedDayChart.razor Component

#### Add Parameters
```csharp
@code {
    [Parameter] public DailyDetailData DetailData { get; set; } = default!;
    [Parameter] public bool ShowDynamicPricing { get; set; } = false;
    [Parameter] public List<OdsPricing> OdsPricingData { get; set; } = new();
    
    private string ChartCanvasId = Guid.NewGuid().ToString();
    private string PriceChartCanvasId = Guid.NewGuid().ToString(); // NEW
    private ChartView SelectedView = ChartView.All;
    private string ChartStyle = "line";
    private bool ShowSunriseSunset = true;
    // ...
}
```

#### Add Pricing Chart Section (After Weather Info)
```html
<!-- Dynamic Pricing Chart -->
@if (ShowDynamicPricing && OdsPricingData != null && OdsPricingData.Any())
{
    <div class="row mt-3">
        <div class="col-md-12">
            <div class="card">
                <div class="card-header bg-info text-white">
                    <h5 class="card-title mb-0">💶 ODS Dynamic Pricing - 15-Minute Intervals</h5>
                </div>
                <div class="card-body">
                    <div class="chart-container" style="position: relative; height: 300px;">
                        <canvas id="@PriceChartCanvasId" style="max-height: 300px;"></canvas>
                    </div>
                    <div class="row mt-3">
                        <div class="col-md-12">
                            <div class="card bg-light">
                                <div class="card-body">
                                    <h6 class="card-title">Pricing Summary</h6>
                                    <div class="row">
                                        <div class="col-md-3">
                                            <strong>Import Price:</strong><br/>
                                            <span class="text-danger">
                                                Avg: €@OdsPricingData.Average(p => p.ImportPricePerKwh).ToString("F4")/kWh
                                            </span><br/>
                                            <small class="text-muted">
                                                €@(OdsPricingData.Average(p => p.ImportPricePerKwh) * 1000).ToString("F2")/MWh
                                            </small>
                                        </div>
                                        <div class="col-md-3">
                                            <strong>Export Price:</strong><br/>
                                            <span class="text-success">
                                                Avg: €@OdsPricingData.Average(p => p.InjectionPricePerKwh).ToString("F4")/kWh
                                            </span><br/>
                                            <small class="text-muted">
                                                €@(OdsPricingData.Average(p => p.InjectionPricePerKwh) * 1000).ToString("F2")/MWh
                                            </small>
                                        </div>
                                        <div class="col-md-3">
                                            <strong>Import Range:</strong><br/>
                                            <span>
                                                €@OdsPricingData.Min(p => p.ImportPricePerKwh).ToString("F4") - 
                                                €@OdsPricingData.Max(p => p.ImportPricePerKwh).ToString("F4")/kWh
                                            </span>
                                        </div>
                                        <div class="col-md-3">
                                            <strong>Export Range:</strong><br/>
                                            <span>
                                                €@OdsPricingData.Min(p => p.InjectionPricePerKwh).ToString("F4") - 
                                                €@OdsPricingData.Max(p => p.InjectionPricePerKwh).ToString("F4")/kWh
                                            </span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
}
else if (ShowDynamicPricing && (OdsPricingData == null || !OdsPricingData.Any()))
{
    <div class="row mt-3">
        <div class="col-md-12">
            <div class="alert alert-warning">
                <i class="bi bi-exclamation-triangle"></i> 
                No ODS pricing data available for @DetailData.Date.ToString("yyyy-MM-dd"). 
                This date may be outside the available ODS data range.
            </div>
        </div>
    </div>
}
```

#### Update OnAfterRenderAsync and OnParametersSetAsync
```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await CreateChart();
        if (ShowDynamicPricing && OdsPricingData != null && OdsPricingData.Any())
        {
            await CreatePriceChart();
        }
    }
}

protected override async Task OnParametersSetAsync()
{
    await CreateChart();
    if (ShowDynamicPricing && OdsPricingData != null && OdsPricingData.Any())
    {
        await CreatePriceChart();
    }
}
```

#### Add Price Chart Creation Methods
```csharp
private async Task CreatePriceChart()
{
    if (OdsPricingData == null || !OdsPricingData.Any())
        return;

    try
    {
        var chartConfig = GeneratePriceChartConfig();
        await JSRuntime.InvokeVoidAsync("createChart", PriceChartCanvasId, chartConfig);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating price chart: {ex.Message}");
    }
}

private object GeneratePriceChartConfig()
{
    var labels = OdsPricingData.Select(p => p.DateTime.ToString("HH:mm")).ToArray();

    // Create anonymous list to avoid Razor parsing issues
    var importData = new {
        label = "Import Price (€/kWh)",
        data = OdsPricingData.Select(p => p.ImportPricePerKwh).ToArray(),
        borderColor = "rgb(220, 53, 69)",
        backgroundColor = "rgba(220, 53, 69, 0.1)",
        borderWidth = 2,
        fill = true,
        tension = 0.4,
        yAxisID = "y"
    };

    var exportData = new {
        label = "Export Price (€/kWh)",
        data = OdsPricingData.Select(p => p.InjectionPricePerKwh).ToArray(),
        borderColor = "rgb(40, 167, 69)",
        backgroundColor = "rgba(40, 167, 69, 0.1)",
        borderWidth = 2,
        fill = true,
        tension = 0.4,
        yAxisID = "y"
    };

    return new {
        type = "line",
        data = new {
            labels = labels,
            datasets = new[] { importData, exportData }
        },
        options = new {
            responsive = true,
            maintainAspectRatio = false,
            interaction = new {
                mode = "index",
                intersect = false
            },
            plugins = new {
                title = new {
                    display = true,
                    text = $"ODS Dynamic Pricing - {DetailData.Date:yyyy-MM-dd}"
                },
                legend = new {
                    display = true,
                    position = "top"
                },
                tooltip = new {
                    callbacks = new { }
                }
            },
            scales = new {
                y = new {
                    beginAtZero = false,
                    title = new {
                        display = true,
                        text = "Price (€/kWh)"
                    }
                }
            }
        }
    };
}
```

## Features

### User Experience

1. **Checkbox Toggle**: Easy on/off switch for ODS pricing display
2. **Data Status**: Visual indicators showing if ODS data is loaded
3. **Date Range Display**: Shows available ODS pricing date range
4. **Refresh Button**: One-click update of ODS data from Elia API
5. **Loading States**: Spinner feedback during data refresh

### Pricing Chart

**Displays:**
- Import prices (red line) - What you pay for grid electricity
- Export prices (green line) - What you get for exporting to grid
- 15-minute interval resolution
- Average prices for the day
- Min/max price ranges

**Summary Statistics:**
- Average import price (€/kWh and €/MWh)
- Average export price (€/kWh and €/MWh)
- Price ranges for both import and export

### Data Handling

- **Automatic Loading**: ODS data loads on page initialization
- **Day-Specific**: Only loads pricing for the selected date
- **Fallback**: Shows warning if no data available for date
- **Real-Time Refresh**: Updates from Elia API on demand

## Benefits

### For Analysis

✅ **Compare Energy Patterns with Prices**: See when prices are high/low alongside your energy usage  
✅ **Identify Savings Opportunities**: Spot times when export prices are good or import prices are low  
✅ **Understand Grid Economics**: See real Belgian grid pricing dynamics  
✅ **Battery Optimization Insights**: Understand when charging/discharging would be profitable  

### For Users

✅ **Easy Toggle**: Simple checkbox to enable/disable pricing display  
✅ **Always Current**: Refresh button gets latest ODS data  
✅ **Visual Clarity**: Separate chart dedicated to pricing  
✅ **Detailed Stats**: Summary cards show pricing metrics  

## Testing Checklist

Once implemented:

- [ ] Checkbox toggles ODS pricing display
- [ ] Pricing chart appears when checkbox enabled
- [ ] Chart shows import and export prices correctly
- [ ] Summary statistics calculate correctly
- [ ] Refresh button downloads latest data
- [ ] Loading spinner shows during refresh
- [ ] Warning shows when no data available for date
- [ ] Date navigation updates pricing data
- [ ] Works with dates inside ODS range
- [ ] Handles dates outside ODS range gracefully

## Example Use Cases

### Use Case 1: Daily Energy Review
```
1. User selects a date
2. Reviews energy production/consumption
3. Enables "Show ODS Pricing"
4. Sees that export prices were low during peak production
5. Realizes battery storage would have been beneficial
```

### Use Case 2: Price Pattern Analysis
```
1. User navigates through multiple days
2. Keeps ODS pricing enabled
3. Notices export prices are consistently low midday
4. Understands grid oversupply during solar peaks
5. Makes informed decision about battery investment
```

### Use Case 3: Real-Time Comparison
```
1. User looks at today's energy data
2. Clicks "Refresh ODS" to get latest prices
3. Sees current pricing alongside energy usage
4. Plans energy-intensive tasks for low-price periods
```

## Implementation Status

✅ DailyDetail.razor - UI and code-behind ready  
✅ ODS Service injection added  
✅ Dynamic pricing toggle implemented  
✅ Refresh functionality complete  
⏳ DetailedDayChart.razor - Awaiting manual implementation (Razor syntax issues with automated tools)  

## Manual Steps Required

Due to Razor parser limitations with generic types in code blocks, the DetailedDayChart.razor component methods need to be added manually. Copy the code from sections above into the component file.

---

**Status**: Ready for manual implementation  
**Estimated Time**: 15-20 minutes  
**Complexity**: Medium (requires careful copy-paste of code sections)  
