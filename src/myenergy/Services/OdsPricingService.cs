using myenergy.Models;
using System.Text.Json;

namespace myenergy.Services;

/// <summary>
/// ODS Pricing Service using JSON format from Elia API.
/// Provides access to dynamic electricity pricing data.
/// </summary>
public class OdsPricingService : IOdsPricingService
{
    private const string ELIA_API_URL = "https://opendata.elia.be/api/explore/v2.1/catalog/datasets/ods134/exports/json?lang=nl&timezone=Europe%2FBrussels";
    private const string LOCAL_FILE_PATH = "Data/ods134.json";
    
    private readonly HttpClient _http;
    private List<OdsPricing>? _pricingData;
    private Dictionary<DateTime, OdsPricing>? _pricingIndex;
    private DateTime? _lastLoadTime;
    private bool _isLoading;

    public OdsPricingService(HttpClient http)
    {
        _http = http;
    }

    public bool IsDataLoaded => _pricingData != null && _pricingData.Any();
    public DateTime? LastLoadTime => _lastLoadTime;

    /// <summary>
    /// Load ODS pricing data. Tries local file first, falls back to Elia API if needed.
    /// </summary>
    /// <param name="forceRefresh">Force download from Elia API even if local data exists</param>
    public async Task LoadDataAsync(bool forceRefresh = false)
    {
        if (_isLoading) return; // Prevent concurrent loading
        if (_pricingData != null && !forceRefresh) return; // Already loaded

        _isLoading = true;
        try
        {
            string json;
            string source;

            if (forceRefresh)
            {
                // Force download from Elia API
                Console.WriteLine("Downloading ODS pricing data from Elia API (forced refresh)...");
                json = await DownloadFromEliaAsync();
                source = "Elia API (forced refresh)";
            }
            else
            {
                // Try Elia API first
                try
                {
                    Console.WriteLine("Downloading ODS pricing data from Elia API...");
                    json = await DownloadFromEliaAsync();
                    source = "Elia API";
                }
                catch (Exception eliaEx)
                {
                    Console.WriteLine($"Elia API failed: {eliaEx.Message}");
                    Console.WriteLine("Falling back to local file...");
                    
                    try
                    {
                        json = await _http.GetStringAsync(LOCAL_FILE_PATH);
                        source = "local file (fallback)";
                    }
                    catch (Exception localEx)
                    {
                        Console.WriteLine($"Local file also failed: {localEx.Message}");
                        throw new Exception($"Unable to load ODS pricing data from either Elia API or local file. Elia: {eliaEx.Message}, Local: {localEx.Message}");
                    }
                }
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            _pricingData = JsonSerializer.Deserialize<List<OdsPricing>>(json, options) ?? new List<OdsPricing>();
            
            // Create fast lookup index
            _pricingIndex = _pricingData
                .GroupBy(p => p.DateTime)
                .ToDictionary(g => g.Key, g => g.First());
            
            _lastLoadTime = DateTime.Now;
            
            Console.WriteLine($"✅ Loaded {_pricingData.Count} ODS pricing records from {source}");
            
            // Log data range and sample prices
            if (_pricingData.Any())
            {
                var minDate = _pricingData.Min(p => p.DateTime);
                var maxDate = _pricingData.Max(p => p.DateTime);
                var avgImport = _pricingData.Average(p => p.ImportPricePerKwh);
                var avgExport = _pricingData.Average(p => p.InjectionPricePerKwh);
                
                Console.WriteLine($"📅 Date range: {minDate:yyyy-MM-dd HH:mm} to {maxDate:yyyy-MM-dd HH:mm}");
                Console.WriteLine($"💶 Average import: €{avgImport:F4}/kWh (€{avgImport * 1000:F2}/MWh)");
                Console.WriteLine($"💶 Average export: €{avgExport:F4}/kWh (€{avgExport * 1000:F2}/MWh)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading ODS pricing data: {ex.Message}");
            _pricingData = new List<OdsPricing>();
            _pricingIndex = new Dictionary<DateTime, OdsPricing>();
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Refresh data from Elia API (can be called on-demand)
    /// </summary>
    public async Task RefreshFromEliaAsync()
    {
        Console.WriteLine("🔄 Refreshing ODS data from Elia API...");
        _pricingData = null; // Clear existing data
        await LoadDataAsync(forceRefresh: true);
    }

    private async Task<string> DownloadFromEliaAsync()
    {
        try
        {
            // Download from Elia API
            // Dataset: ods134 - Operational Deviation Settlement prices (15-minute intervals)
            var response = await _http.GetAsync(ELIA_API_URL);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to download from Elia API: {ex.Message}");
            throw new Exception($"Unable to download ODS pricing data from Elia: {ex.Message}", ex);
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
