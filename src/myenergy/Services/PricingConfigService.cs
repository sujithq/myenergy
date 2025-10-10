using System.Net.Http.Json;
using System.Text.Json;

namespace myenergy.Services;

/// <summary>
/// Centralized configuration service for pricing, battery, and solar system defaults.
/// Loads from app-config.json and provides fallback defaults.
/// </summary>
public class PricingConfigService
{
    private readonly HttpClient _httpClient;
    private AppConfig? _config;
    private bool _isLoaded;

    public PricingConfigService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Pricing Configuration
    public double FixedImportPrice => _config?.Pricing?.FixedImportPrice ?? 0.30;
    public double FixedExportPrice => _config?.Pricing?.FixedExportPrice ?? 0.05;

    // Battery Configuration
    public double DefaultBatteryCapacityKwh => _config?.Battery?.DefaultCapacityKwh ?? 5.0;
    public double BatteryEfficiency => _config?.Battery?.Efficiency ?? 0.95;
    public double BatteryChargeRateFactor => _config?.Battery?.ChargeRateFactor ?? 0.5;
    public double BatteryDischargeRateFactor => _config?.Battery?.DischargeRateFactor ?? 0.5;

    // Solar Configuration
    public double DefaultSolarSystemCapacityKwp => _config?.Solar?.DefaultSystemCapacityKwp ?? 5.0;
    public double AveragePeakSunHours => _config?.Solar?.AveragePeakSunHours ?? 4.0;
    public double DefaultSolarSystemCostEur => _config?.Solar?.DefaultSystemCostEur ?? 10000;

    public bool IsLoaded => _isLoaded;

    public async Task LoadConfigAsync()
    {
        if (_isLoaded) return;

        try
        {
            _config = await _httpClient.GetFromJsonAsync<AppConfig>("config/app-config.json");
            _isLoaded = true;
            Console.WriteLine($"App config loaded successfully");
            Console.WriteLine($"  Pricing: Import=€{FixedImportPrice:F2}/kWh, Export=€{FixedExportPrice:F2}/kWh");
            Console.WriteLine($"  Battery: {DefaultBatteryCapacityKwh:F1}kWh, Efficiency={BatteryEfficiency:P0}");
            Console.WriteLine($"  Solar: {DefaultSolarSystemCapacityKwp:F1}kWp system");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load app config: {ex.Message}. Using defaults.");
            _config = new AppConfig
            {
                Pricing = new AppPricingConfig
                {
                    FixedImportPrice = 0.30,
                    FixedExportPrice = 0.05
                },
                Battery = new AppBatteryConfig
                {
                    DefaultCapacityKwh = 5.0,
                    Efficiency = 0.95,
                    ChargeRateFactor = 0.5,
                    DischargeRateFactor = 0.5
                },
                Solar = new AppSolarConfig
                {
                    DefaultSystemCapacityKwp = 5.0,
                    AveragePeakSunHours = 4.0,
                    DefaultSystemCostEur = 10000
                }
            };
            _isLoaded = true;
        }
    }

    public async Task UpdateConfigAsync(double importPrice, double exportPrice)
    {
        if (_config == null)
        {
            _config = new AppConfig();
        }

        if (_config.Pricing == null)
        {
            _config.Pricing = new AppPricingConfig();
        }

        _config.Pricing.FixedImportPrice = importPrice;
        _config.Pricing.FixedExportPrice = exportPrice;

        // Note: In a real application, you would save this to a backend
        // For now, it only updates the in-memory config
        Console.WriteLine($"Pricing config updated: Import=€{importPrice:F2}/kWh, Export=€{exportPrice:F2}/kWh");
    }
}

public class AppConfig
{
    public AppPricingConfig? Pricing { get; set; }
    public AppBatteryConfig? Battery { get; set; }
    public AppSolarConfig? Solar { get; set; }
    public string LastUpdated { get; set; } = "";
}

public class AppPricingConfig
{
    public double FixedImportPrice { get; set; } = 0.30;
    public double FixedExportPrice { get; set; } = 0.05;
    public string Description { get; set; } = "";
}

public class AppBatteryConfig
{
    public double DefaultCapacityKwh { get; set; } = 5.0;
    public double Efficiency { get; set; } = 0.95;
    public double ChargeRateFactor { get; set; } = 0.5;
    public double DischargeRateFactor { get; set; } = 0.5;
    public string Description { get; set; } = "";
}

public class AppSolarConfig
{
    public double DefaultSystemCapacityKwp { get; set; } = 5.0;
    public double AveragePeakSunHours { get; set; } = 4.0;
    public double DefaultSystemCostEur { get; set; } = 10000;
    public string Description { get; set; } = "";
}
