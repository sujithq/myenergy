using myenergy.Models;

namespace myenergy.Services;

public class AppConfigurationService
{
    private readonly AppConfiguration _config;

    public AppConfigurationService()
    {
        // Default configuration - update these values when data becomes available
        _config = new AppConfiguration
        {
            EnableWeatherData = true,        // Master switch for all weather features
            EnableSunshineData = false,       // Sunshine duration (tsun)
            EnablePrecipitationData = false,  // Precipitation (prcp)
            EnableTemperatureData = true,     // Temperature data (available)
            EnableWindData = true,            // Wind speed (available)
            EnablePressureData = true,        // Air pressure (available)
            EnableSunTimesDisplay = false     // Sunrise/sunset times
        };
    }

    public AppConfiguration Config => _config;

    // Helper methods for checking specific features
    public bool IsWeatherDataEnabled => _config.EnableWeatherData;
    public bool IsSunshineDataEnabled => _config.EnableWeatherData && _config.EnableSunshineData;
    public bool IsPrecipitationDataEnabled => _config.EnableWeatherData && _config.EnablePrecipitationData;
    public bool IsTemperatureDataEnabled => _config.EnableWeatherData && _config.EnableTemperatureData;
    public bool IsWindDataEnabled => _config.EnableWeatherData && _config.EnableWindData;
    public bool IsPressureDataEnabled => _config.EnableWeatherData && _config.EnablePressureData;
    public bool IsSunTimesDisplayEnabled => _config.EnableWeatherData && _config.EnableSunTimesDisplay;

    /// <summary>
    /// Get available weather factors based on configuration
    /// </summary>
    public List<(string value, string name)> GetAvailableWeatherFactors()
    {
        var factors = new List<(string value, string name)>();

        if (IsSunshineDataEnabled)
            factors.Add(("tsun", "Sunshine Duration"));

        if (IsTemperatureDataEnabled)
        {
            factors.Add(("tavg", "Temperature (Avg)"));
            factors.Add(("tmax", "Temperature (Max)"));
        }

        if (IsWindDataEnabled)
            factors.Add(("wspd", "Wind Speed"));

        if (IsPrecipitationDataEnabled)
            factors.Add(("prcp", "Precipitation"));

        if (IsPressureDataEnabled)
            factors.Add(("pres", "Air Pressure"));

        return factors;
    }

    /// <summary>
    /// Update configuration at runtime (for testing or admin panel)
    /// </summary>
    public void UpdateConfiguration(Action<AppConfiguration> updateAction)
    {
        updateAction(_config);
    }
}
