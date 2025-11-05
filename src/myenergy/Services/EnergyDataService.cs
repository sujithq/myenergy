using myenergy.Models;
using System.Text.Json;
using System.Globalization;

namespace myenergy.Services;

public class EnergyDataService
{
    private readonly HttpClient _http;
    private Dictionary<int, List<BarChartData>>? _rawData;
    private List<DailySummary>? _dailyData;
    private List<MonthlySummary>? _monthlyData;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    public EnergyDataService(HttpClient http)
    {
        _http = http;
    }

    public async Task LoadDataAsync(bool bypassCache = false)
    {
        if (_rawData != null) return; // Already loaded

        try
        {
            var url = "Data/data.json";
            if (bypassCache)
            {
                // Add a cache-busting query string to force CDN / browser to fetch the latest file
                url += (url.Contains('?') ? '&' : '?') + "v=" + DateTime.UtcNow.Ticks;
            }
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (bypassCache)
            {
                request.Headers.Add("Cache-Control", "no-cache");
                request.Headers.Add("Pragma", "no-cache");
            }
            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            _rawData = JsonSerializer.Deserialize<Dictionary<int, List<BarChartData>>>(json);
            
            if (_rawData != null)
            {
                ProcessData();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading data: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Ensures the in-memory data contains (at least) the requested date. If the date is newer than
    /// the currently loaded max date we force a reload of the underlying JSON file. This helps when
    /// the large raw data file is updated on the server after the WASM app was already running.
    /// </summary>
    public async Task EnsureLatestDataAsync(DateTime requiredDate)
    {
        // Fast path: data not loaded yet – normal load will pick everything up
        if (_rawData == null)
        {
            await LoadDataAsync(bypassCache: true);
            return;
        }

        var currentMax = _dailyData?.Max(d => d.Date);
        if (currentMax.HasValue && requiredDate.Date <= currentMax.Value.Date)
        {
            // Already have the date or something newer
            return;
        }

        await _reloadLock.WaitAsync();
        try
        {
            // Re-evaluate inside lock in case another thread already refreshed
            currentMax = _dailyData?.Max(d => d.Date);
            if (currentMax.HasValue && requiredDate.Date <= currentMax.Value.Date)
            {
                return; // Another thread refreshed while we waited
            }

            // Clear caches and force reload
            _rawData = null;
            _dailyData = null;
            _monthlyData = null;
            await LoadDataAsync(bypassCache: true); // Force fresh fetch when reloading newer date
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    private void ProcessData()
    {
        if (_rawData == null) return;

        // Process daily summaries
        _dailyData = _rawData
            .SelectMany(kvp => kvp.Value.Select(bar => new DailySummary(
                Date: new DateTime(kvp.Key, 1, 1).AddDays(bar.D - 1),
                Production: bar.P,
                Consumption: bar.U,
                Import: bar.I / 1000.0, // Wh to kWh
                Export: Math.Max(0, bar.P - bar.U)
            )))
            .OrderBy(d => d.Date)
            .ToList();

        // Process monthly summaries
        _monthlyData = _dailyData
            .GroupBy(d => new { d.Date.Year, d.Date.Month })
            .Select(g => new MonthlySummary(
                Year: g.Key.Year,
                Month: g.Key.Month,
                Production: g.Sum(d => d.Production),
                Consumption: g.Sum(d => d.Consumption),
                Import: g.Sum(d => d.Import),
                Export: g.Sum(d => d.Export),
                DayCount: g.Count()
            ))
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToList();
    }

    public List<DailySummary> GetDailyData() => _dailyData ?? new List<DailySummary>();
    
    public List<MonthlySummary> GetMonthlyData() => _monthlyData ?? new List<MonthlySummary>();

    public List<DailySummary> GetDailyDataForYear(int year) 
        => _dailyData?.Where(d => d.Date.Year == year).ToList() ?? new List<DailySummary>();

    public List<DailySummary> GetTopProductionDays(int count = 10)
        => _dailyData?.OrderByDescending(d => d.Production).Take(count).ToList() ?? new List<DailySummary>();

    public (double totalProduction, double totalConsumption, double totalImport, double totalExport) GetTotals(int? year = null)
    {
        var data = year.HasValue ? GetDailyDataForYear(year.Value) : GetDailyData();
        return (
            data.Sum(d => d.Production),
            data.Sum(d => d.Consumption),
            data.Sum(d => d.Import),
            data.Sum(d => d.Export)
        );
    }

    public Dictionary<int, (double production, double consumption, double import, double export)> GetYearlyTotals()
    {
        return _dailyData?
            .GroupBy(d => d.Date.Year)
            .ToDictionary(
                g => g.Key,
                g => (
                    g.Sum(d => d.Production), 
                    g.Sum(d => d.Consumption),
                    g.Sum(d => d.Import),
                    g.Sum(d => d.Export)
                )
            ) ?? new Dictionary<int, (double, double, double, double)>();
    }

    public List<(DateTime date, double surplus)> GetSurplusDays(int minSurplusKwh = 5)
    {
        return _dailyData?
            .Where(d => (d.Production - d.Consumption) >= minSurplusKwh)
            .Select(d => (d.Date, d.Production - d.Consumption))
            .OrderByDescending(x => x.Item2)
            .ToList() ?? new List<(DateTime, double)>();
    }

    public List<int> GetAvailableYears()
    {
        return _dailyData?
            .Select(d => d.Date.Year)
            .Distinct()
            .OrderBy(y => y)
            .ToList() ?? new List<int>();
    }

    public List<PeriodDataPoint> GetPeriodData(int? selectedYear, PeriodType periodType)
    {
        if (_dailyData == null) return new List<PeriodDataPoint>();

        var data = selectedYear.HasValue && periodType != PeriodType.Total 
            ? _dailyData.Where(d => d.Date.Year == selectedYear.Value)
            : _dailyData;

        return periodType switch
        {
            PeriodType.Daily => GetDailyPeriodData(data),
            PeriodType.Weekly => GetWeeklyPeriodData(data),
            PeriodType.Monthly => GetMonthlyPeriodData(data),
            PeriodType.Quarterly => GetQuarterlyPeriodData(data),
            PeriodType.Yearly => GetYearlyPeriodData(data),
            PeriodType.Total => GetTotalPeriodData(data),
            _ => new List<PeriodDataPoint>()
        };
    }

    private List<PeriodDataPoint> GetDailyPeriodData(IEnumerable<DailySummary> data)
    {
        return data.Select(d => new PeriodDataPoint(
            Label: d.Date.ToString("MMM dd"),
            Date: d.Date,
            Production: d.Production,
            Consumption: d.Consumption,
            Import: d.Import,
            Export: d.Export,
            AutarkyPercent: d.Autarky,
            SelfConsumptionPercent: d.Consumption > 0 ? ((d.Consumption - d.Import) / d.Consumption * 100) : 0
        )).ToList();
    }

    private List<PeriodDataPoint> GetWeeklyPeriodData(IEnumerable<DailySummary> data)
    {
        return data
            .GroupBy(d => new { d.Date.Year, Week = GetWeekOfYear(d.Date) })
            .Select(g =>
            {
                var weekData = g.ToList();
                var startDate = weekData.Min(d => d.Date);
                var production = weekData.Sum(d => d.Production);
                var consumption = weekData.Sum(d => d.Consumption);
                var import = weekData.Sum(d => d.Import);
                var export = weekData.Sum(d => d.Export);
                
                return new PeriodDataPoint(
                    Label: $"Week {g.Key.Week}",
                    Date: startDate,
                    Production: production,
                    Consumption: consumption,
                    Import: import,
                    Export: export,
                    AutarkyPercent: consumption > 0 ? ((consumption - import) / consumption * 100) : 0,
                    SelfConsumptionPercent: production > 0 ? ((production - export) / production * 100) : 0
                );
            })
            .OrderBy(w => w.Date)
            .ToList();
    }

    private List<PeriodDataPoint> GetMonthlyPeriodData(IEnumerable<DailySummary> data)
    {
        return data
            .GroupBy(d => new { d.Date.Year, d.Date.Month })
            .Select(g =>
            {
                var monthData = g.ToList();
                var production = monthData.Sum(d => d.Production);
                var consumption = monthData.Sum(d => d.Consumption);
                var import = monthData.Sum(d => d.Import);
                var export = monthData.Sum(d => d.Export);
                
                return new PeriodDataPoint(
                    Label: new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Date: new DateTime(g.Key.Year, g.Key.Month, 1),
                    Production: production,
                    Consumption: consumption,
                    Import: import,
                    Export: export,
                    AutarkyPercent: consumption > 0 ? ((consumption - import) / consumption * 100) : 0,
                    SelfConsumptionPercent: production > 0 ? ((production - export) / production * 100) : 0
                );
            })
            .OrderBy(m => m.Date)
            .ToList();
    }

    private List<PeriodDataPoint> GetQuarterlyPeriodData(IEnumerable<DailySummary> data)
    {
        return data
            .GroupBy(d => new { d.Date.Year, Quarter = (d.Date.Month - 1) / 3 + 1 })
            .Select(g =>
            {
                var quarterData = g.ToList();
                var production = quarterData.Sum(d => d.Production);
                var consumption = quarterData.Sum(d => d.Consumption);
                var import = quarterData.Sum(d => d.Import);
                var export = quarterData.Sum(d => d.Export);
                
                return new PeriodDataPoint(
                    Label: $"Q{g.Key.Quarter} {g.Key.Year}",
                    Date: new DateTime(g.Key.Year, (g.Key.Quarter - 1) * 3 + 1, 1),
                    Production: production,
                    Consumption: consumption,
                    Import: import,
                    Export: export,
                    AutarkyPercent: consumption > 0 ? ((consumption - import) / consumption * 100) : 0,
                    SelfConsumptionPercent: production > 0 ? ((production - export) / production * 100) : 0
                );
            })
            .OrderBy(q => q.Date)
            .ToList();
    }

    private List<PeriodDataPoint> GetYearlyPeriodData(IEnumerable<DailySummary> data)
    {
        return data
            .GroupBy(d => d.Date.Year)
            .Select(g =>
            {
                var yearData = g.ToList();
                var production = yearData.Sum(d => d.Production);
                var consumption = yearData.Sum(d => d.Consumption);
                var import = yearData.Sum(d => d.Import);
                var export = yearData.Sum(d => d.Export);
                
                return new PeriodDataPoint(
                    Label: g.Key.ToString(),
                    Date: new DateTime(g.Key, 1, 1),
                    Production: production,
                    Consumption: consumption,
                    Import: import,
                    Export: export,
                    AutarkyPercent: consumption > 0 ? ((consumption - import) / consumption * 100) : 0,
                    SelfConsumptionPercent: production > 0 ? ((production - export) / production * 100) : 0
                );
            })
            .OrderBy(y => y.Date)
            .ToList();
    }

    private List<PeriodDataPoint> GetTotalPeriodData(IEnumerable<DailySummary> data)
    {
        var allData = data.ToList();
        if (!allData.Any()) return new List<PeriodDataPoint>();
        
        var production = allData.Sum(d => d.Production);
        var consumption = allData.Sum(d => d.Consumption);
        var import = allData.Sum(d => d.Import);
        var export = allData.Sum(d => d.Export);
        
        return new List<PeriodDataPoint>
        {
            new PeriodDataPoint(
                Label: "Total",
                Date: allData.Min(d => d.Date),
                Production: production,
                Consumption: consumption,
                Import: import,
                Export: export,
                AutarkyPercent: consumption > 0 ? ((consumption - import) / consumption * 100) : 0,
                SelfConsumptionPercent: production > 0 ? ((production - export) / production * 100) : 0
            )
        };
    }

    private int GetWeekOfYear(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        var calendar = culture.Calendar;
        return calendar.GetWeekOfYear(date, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
    }

    public DailyDetailData? GetDailyDetailData(DateTime date)
    {
        if (_rawData == null) return null;

        var year = date.Year;
        if (!_rawData.ContainsKey(year)) return null;

        var dayData = _rawData[year].FirstOrDefault(d => 
            new DateTime(year, 1, 1).AddDays(d.D - 1).Date == date.Date);

        if (dayData == null) return null;

        // Extract 15-minute interval data from QuarterData
        var quarterHours = new List<QuarterHourData>();
        var startTime = date.Date;

        // Each day has 96 quarter-hour intervals (24 hours * 4)
        var quarterCount = Math.Min(
            Math.Min(dayData.Q.C.Count, dayData.Q.I.Count),
            Math.Min(dayData.Q.G.Count, dayData.Q.P.Count)
        );

        for (int i = 0; i < quarterCount; i++)
        {
            var time = startTime.AddMinutes(i * 15);
            var quarterHour = new QuarterHourData(
                Time: time,
                GridImport: dayData.Q.C[i],
                Injection: dayData.Q.I[i],
                GasUsage: dayData.Q.G[i],
                SolarProduction: dayData.Q.P[i]
            );
            quarterHours.Add(quarterHour);
        }

        return new DailyDetailData(
            Date: date,
            QuarterHours: quarterHours,
            SunTimes: dayData.SRS,
            Weather: dayData.MS
        );
    }

    public List<DateTime> GetAvailableDates()
    {
        if (_dailyData == null) return new List<DateTime>();
        return _dailyData.Select(d => d.Date).OrderByDescending(d => d).ToList();
    }
}
