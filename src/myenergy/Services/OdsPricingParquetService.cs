using myenergy.Models;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using System.IO;

namespace myenergy.Services;

/// <summary>
/// ODS Pricing Service using Parquet file format for better performance and smaller file size.
/// Parquet is a columnar storage format that's ~10x more efficient than JSON.
/// </summary>
public class OdsPricingParquetService : IOdsPricingService
{
    private const string ELIA_API_URL = "https://opendata.elia.be/api/explore/v2.1/catalog/datasets/ods134/exports/parquet?lang=en&timezone=Europe%2FBrussels";
    private const string LOCAL_FILE_PATH = "Data/ods134.parquet";
    
    private readonly HttpClient _http;
    private List<OdsPricing>? _pricingData;
    private Dictionary<DateTime, OdsPricing>? _pricingIndex;
    private DateTime? _lastLoadTime;
    private bool _isLoading;

    public OdsPricingParquetService(HttpClient http)
    {
        _http = http;
    }

    public bool IsDataLoaded => _pricingData != null && _pricingData.Any();
    public DateTime? LastLoadTime => _lastLoadTime;

    /// <summary>
    /// Load ODS pricing data from Parquet file. Tries local file first, falls back to Elia API if needed.
    /// </summary>
    /// <param name="forceRefresh">Force download from Elia API even if local data exists</param>
    public async Task LoadDataAsync(bool forceRefresh = false)
    {
        if (_isLoading) return; // Prevent concurrent loading
        if (_pricingData != null && !forceRefresh) return; // Already loaded

        _isLoading = true;
        try
        {
            byte[] parquetData;
            string source;

            if (forceRefresh)
            {
                // Force download from Elia API
                Console.WriteLine("Downloading ODS pricing data (Parquet) from Elia API (forced refresh)...");
                parquetData = await DownloadFromEliaAsync();
                source = "Elia API (forced refresh)";
            }
            else
            {
                // Try Elia API first
                try
                {
                    Console.WriteLine("Downloading ODS pricing data (Parquet) from Elia API...");
                    parquetData = await DownloadFromEliaAsync();
                    source = "Elia API";
                }
                catch (Exception eliaEx)
                {
                    Console.WriteLine($"Elia API failed: {eliaEx.Message}");
                    Console.WriteLine("Falling back to local Parquet file...");
                    
                    try
                    {
                        parquetData = await _http.GetByteArrayAsync(LOCAL_FILE_PATH);
                        source = "local file (fallback)";
                    }
                    catch (Exception localEx)
                    {
                        Console.WriteLine($"Local file also failed: {localEx.Message}");
                        throw new Exception($"Unable to load ODS pricing data from either Elia API or local file. Elia: {eliaEx.Message}, Local: {localEx.Message}");
                    }
                }
            }

            // Parse Parquet data
            _pricingData = await ParseParquetDataAsync(parquetData);
            
            // Create fast lookup index
            _pricingIndex = _pricingData
                .GroupBy(p => p.DateTime)
                .ToDictionary(g => g.Key, g => g.First());
            
            _lastLoadTime = DateTime.Now;
            
            Console.WriteLine($"✅ Loaded {_pricingData.Count} ODS pricing records from {source} (Parquet format)");
            
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
                Console.WriteLine($"📦 Parquet format is ~10x smaller than JSON!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading ODS pricing data (Parquet): {ex.Message}");
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
        Console.WriteLine("🔄 Refreshing ODS data from Elia API (Parquet)...");
        _pricingData = null; // Clear existing data
        await LoadDataAsync(forceRefresh: true);
    }

    private async Task<byte[]> DownloadFromEliaAsync()
    {
        try
        {
            // Download Parquet file from Elia API
            // Dataset: ods134 - Operational Deviation Settlement prices (15-minute intervals)
            Console.WriteLine($"Fetching from: {ELIA_API_URL}");
            var response = await _http.GetAsync(ELIA_API_URL);
            response.EnsureSuccessStatusCode();
            
            var data = await response.Content.ReadAsByteArrayAsync();
            Console.WriteLine($"Downloaded {data.Length} bytes (Parquet format)");
            return data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to download from Elia API: {ex.Message}");
            throw new Exception($"Unable to download ODS pricing data from Elia: {ex.Message}", ex);
        }
    }

    private async Task<List<OdsPricing>> ParseParquetDataAsync(byte[] parquetData)
    {
        var result = new List<OdsPricing>();
        
        try
        {
            Console.WriteLine("Parsing Parquet data...");
            
            using var memoryStream = new MemoryStream(parquetData);
            using var parquetReader = await ParquetReader.CreateAsync(memoryStream);
            
            Console.WriteLine($"Parquet file has {parquetReader.RowGroupCount} row group(s)");
            
            // Read all row groups
            for (int i = 0; i < parquetReader.RowGroupCount; i++)
            {
                using var groupReader = parquetReader.OpenRowGroupReader(i);
                var rowCount = (int)groupReader.RowCount;
                Console.WriteLine($"Reading row group {i + 1}/{parquetReader.RowGroupCount} ({rowCount} rows)");
                
                // Get schema fields - only DataFields can be read as columns
                var dataFields = parquetReader.Schema.GetDataFields();
                Console.WriteLine($"Schema has {dataFields.Length} data fields");
                
                // Read all columns
                var columnData = new Dictionary<string, DataColumn>();
                foreach (var dataField in dataFields)
                {
                    var column = await groupReader.ReadColumnAsync(dataField);
                    columnData[dataField.Name.ToLower()] = column;
                    Console.WriteLine($"  - {dataField.Name} ({dataField.ClrType})");
                }
                
                // Map column names (Elia dataset uses specific names)
                // Expected columns: datetime, mostactivatedbalancingenergypriceofferdown, mostactivatedbalancingenergypriceofferup
                var datetimeCol = columnData.FirstOrDefault(c => 
                    c.Key.Contains("datetime") || c.Key.Contains("date") || c.Key.Contains("time")).Value;
                var importCol = columnData.FirstOrDefault(c => 
                    c.Key.Contains("priceofferup") || c.Key.Contains("import") || c.Key.Contains("purchase")).Value;
                var exportCol = columnData.FirstOrDefault(c => 
                    c.Key.Contains("priceofferdown") || c.Key.Contains("export") || c.Key.Contains("injection")).Value;
                
                if (datetimeCol == null)
                {
                    Console.WriteLine("⚠️ Could not find datetime column. Available columns:");
                    foreach (var col in columnData.Keys)
                    {
                        Console.WriteLine($"  - {col}");
                    }
                    throw new Exception("Datetime column not found in Parquet file");
                }
                
                // Process rows
                for (int row = 0; row < rowCount; row++)
                {
                    try
                    {
                        var datetimeValue = datetimeCol.Data.GetValue(row);
                        DateTime dateTime;
                        
                        // Handle different datetime formats
                        if (datetimeValue is DateTime dt)
                        {
                            dateTime = dt;
                        }
                        else if (datetimeValue is DateTimeOffset dto)
                        {
                            dateTime = dto.DateTime;
                        }
                        else if (datetimeValue is string dtStr)
                        {
                            dateTime = DateTime.Parse(dtStr);
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Unexpected datetime type: {datetimeValue?.GetType().Name}");
                            continue;
                        }
                        
                        // Get price values (in €/MWh as stored in Parquet)
                        // The OdsPricing model will automatically convert to €/kWh
                        double? importPriceMwh = null;
                        double? exportPriceMwh = null;
                        
                        if (importCol != null)
                        {
                            var importValue = importCol.Data.GetValue(row);
                            if (importValue != null && double.TryParse(importValue.ToString(), out var impVal))
                            {
                                importPriceMwh = impVal; // Keep in €/MWh
                            }
                        }
                        
                        if (exportCol != null)
                        {
                            var exportValue = exportCol.Data.GetValue(row);
                            if (exportValue != null && double.TryParse(exportValue.ToString(), out var expVal))
                            {
                                exportPriceMwh = expVal; // Keep in €/MWh
                            }
                        }
                        
                        result.Add(new OdsPricing
                        {
                            DateTime = dateTime,
                            MarginalIncrementalPrice = importPriceMwh,  // Store in €/MWh
                            MarginalDecrementalPrice = exportPriceMwh   // Store in €/MWh
                            // ImportPricePerKwh and InjectionPricePerKwh are calculated automatically
                        });
                    }
                    catch (Exception rowEx)
                    {
                        Console.WriteLine($"⚠️ Error parsing row {row}: {rowEx.Message}");
                    }
                }
            }
            
            Console.WriteLine($"✅ Parsed {result.Count} pricing records from Parquet");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error parsing Parquet data: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw new Exception($"Failed to parse Parquet data: {ex.Message}", ex);
        }
        
        return result;
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
