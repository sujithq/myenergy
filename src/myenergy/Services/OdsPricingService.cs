using myenergy.Models;
using System.Text.Json;

namespace myenergy.Services;

public class OdsPricingService
{
    private readonly HttpClient _http;
    private List<OdsPricing>? _pricingData;
    private Dictionary<DateTime, OdsPricing>? _pricingIndex;

    public OdsPricingService(HttpClient http)
    {
        _http = http;
    }

    public async Task LoadDataAsync()
    {
        if (_pricingData != null) return; // Already loaded

        try
        {
            var json = await _http.GetStringAsync("Data/ods153.json");
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            _pricingData = JsonSerializer.Deserialize<List<OdsPricing>>(json, options) ?? new List<OdsPricing>();
            
            // Create fast lookup index
            _pricingIndex = _pricingData
                .GroupBy(p => p.DateTime)
                .ToDictionary(g => g.Key, g => g.First());
            
            Console.WriteLine($"Loaded {_pricingData.Count} ODS pricing records");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading ODS pricing data: {ex.Message}");
            _pricingData = new List<OdsPricing>();
            _pricingIndex = new Dictionary<DateTime, OdsPricing>();
        }
    }

    public OdsPricing? GetPricingForInterval(DateTime time)
    {
        if (_pricingIndex == null) return null;
        
        // Try exact match first
        if (_pricingIndex.TryGetValue(time, out var pricing))
            return pricing;
        
        // Try to find closest 15-minute interval
        var roundedTime = RoundToQuarterHour(time);
        if (_pricingIndex.TryGetValue(roundedTime, out var roundedPricing))
            return roundedPricing;
        
        return null;
    }

    public List<OdsPricing> GetPricingForDay(DateTime date)
    {
        if (_pricingData == null) return new List<OdsPricing>();
        
        return _pricingData
            .Where(p => p.DateTime.Date == date.Date)
            .OrderBy(p => p.DateTime)
            .ToList();
    }

    public List<OdsPricing> GetPricingForDateRange(DateTime start, DateTime end)
    {
        if (_pricingData == null) return new List<OdsPricing>();
        
        return _pricingData
            .Where(p => p.DateTime >= start && p.DateTime <= end)
            .OrderBy(p => p.DateTime)
            .ToList();
    }

    public (double avgImport, double avgExport, double minImport, double maxImport, double minExport, double maxExport) GetPriceStatistics(int? year = null)
    {
        if (_pricingData == null || !_pricingData.Any())
            return (0, 0, 0, 0, 0, 0);
        
        var data = year.HasValue 
            ? _pricingData.Where(p => p.DateTime.Year == year.Value).ToList()
            : _pricingData;
        
        if (!data.Any())
            return (0, 0, 0, 0, 0, 0);
        
        return (
            avgImport: data.Average(p => p.ImportPricePerKwh),
            avgExport: data.Average(p => p.InjectionPricePerKwh),
            minImport: data.Min(p => p.ImportPricePerKwh),
            maxImport: data.Max(p => p.ImportPricePerKwh),
            minExport: data.Min(p => p.InjectionPricePerKwh),
            maxExport: data.Max(p => p.InjectionPricePerKwh)
        );
    }

    public List<int> GetAvailableYears()
    {
        if (_pricingData == null) return new List<int>();
        
        return _pricingData
            .Select(p => p.DateTime.Year)
            .Distinct()
            .OrderBy(y => y)
            .ToList();
    }

    public (DateTime start, DateTime end)? GetDataRange()
    {
        if (_pricingData == null || !_pricingData.Any())
            return null;
        
        return (
            start: _pricingData.Min(p => p.DateTime),
            end: _pricingData.Max(p => p.DateTime)
        );
    }

    private DateTime RoundToQuarterHour(DateTime time)
    {
        var minutes = time.Minute;
        var quarterMinutes = (minutes / 15) * 15;
        return new DateTime(time.Year, time.Month, time.Day, time.Hour, quarterMinutes, 0);
    }

    // Helper to get hourly average prices (for visualization)
    public Dictionary<int, (double avgImport, double avgExport)> GetHourlyAveragePrices(DateTime date)
    {
        var dayPricing = GetPricingForDay(date);
        
        return dayPricing
            .GroupBy(p => p.DateTime.Hour)
            .ToDictionary(
                g => g.Key,
                g => (
                    avgImport: g.Average(p => p.ImportPricePerKwh),
                    avgExport: g.Average(p => p.InjectionPricePerKwh)
                )
            );
    }

    // Get price distribution for analysis
    public (List<double> importPrices, List<double> exportPrices) GetPriceDistribution(int? year = null)
    {
        if (_pricingData == null)
            return (new List<double>(), new List<double>());
        
        var data = year.HasValue 
            ? _pricingData.Where(p => p.DateTime.Year == year.Value)
            : _pricingData;
        
        return (
            importPrices: data.Select(p => p.ImportPricePerKwh).OrderBy(p => p).ToList(),
            exportPrices: data.Select(p => p.InjectionPricePerKwh).OrderBy(p => p).ToList()
        );
    }
}
