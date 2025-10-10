using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace myenergy.Services;

/// <summary>
/// Centralized service for initializing all data at application startup.
/// This ensures data is loaded once and cached in the respective services.
/// </summary>
public class DataInitializationService
{
    private readonly EnergyDataService _energyService;
    private readonly OdsPricingService _odsService;
    private readonly PricingConfigService _pricingConfig;
    private bool _isInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public DataInitializationService(
        EnergyDataService energyService,
        OdsPricingService odsService,
        PricingConfigService pricingConfig)
    {
        _energyService = energyService;
        _odsService = odsService;
        _pricingConfig = pricingConfig;
    }

    /// <summary>
    /// Initializes all data sources. Safe to call multiple times (idempotent).
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized) return; // Double-check after acquiring lock

            Console.WriteLine("🚀 Starting data initialization...");
            var startTime = DateTime.Now;

            // Load all data in parallel for faster startup
            var tasks = new[]
            {
                LoadEnergyDataAsync(),
                LoadOdsPricingAsync(),
                LoadConfigAsync()
            };

            await Task.WhenAll(tasks);

            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            Console.WriteLine($"✅ Data initialization complete in {elapsed:F0}ms");

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Data initialization failed: {ex.Message}");
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task LoadEnergyDataAsync()
    {
        try
        {
            await _energyService.LoadDataAsync();
            Console.WriteLine("  ✓ Energy data loaded");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Energy data failed: {ex.Message}");
            throw;
        }
    }

    private async Task LoadOdsPricingAsync()
    {
        try
        {
            await _odsService.LoadDataAsync();
            var range = _odsService.GetDataRange();
            if (range.HasValue)
            {
                Console.WriteLine($"  ✓ ODS pricing loaded ({range.Value.start:yyyy-MM-dd} to {range.Value.end:yyyy-MM-dd})");
            }
            else
            {
                Console.WriteLine("  ⚠ ODS pricing loaded (no data available)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ ODS pricing failed: {ex.Message}");
            // Don't throw - ODS data is optional
        }
    }

    private async Task LoadConfigAsync()
    {
        try
        {
            await _pricingConfig.LoadConfigAsync();
            Console.WriteLine($"  ✓ App config loaded (Import: €{_pricingConfig.FixedImportPrice:F2}/kWh, Export: €{_pricingConfig.FixedExportPrice:F2}/kWh)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ App config failed: {ex.Message}");
            throw;
        }
    }

    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Forces re-initialization (useful for testing or manual refresh).
    /// </summary>
    public async Task RefreshAsync()
    {
        _isInitialized = false;
        await InitializeAsync();
    }
}
