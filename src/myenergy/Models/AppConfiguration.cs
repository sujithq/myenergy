namespace myenergy.Models;

public class AppConfiguration
{
    /// <summary>
    /// Enable/disable weather-related features (sunshine, precipitation, etc.)
    /// Set to true when weather data is properly populated
    /// </summary>
    public bool EnableWeatherData { get; set; } = false;

    /// <summary>
    /// Enable/disable sunshine duration features
    /// Set to true when sunshine data (tsun) is properly populated
    /// </summary>
    public bool EnableSunshineData { get; set; } = false;

    /// <summary>
    /// Enable/disable precipitation features
    /// Set to true when precipitation data (prcp) is properly populated
    /// </summary>
    public bool EnablePrecipitationData { get; set; } = false;

    /// <summary>
    /// Enable/disable temperature-based features
    /// Set to true when temperature data is properly populated
    /// </summary>
    public bool EnableTemperatureData { get; set; } = true;

    /// <summary>
    /// Enable/disable wind speed features
    /// Set to true when wind data is properly populated
    /// </summary>
    public bool EnableWindData { get; set; } = true;

    /// <summary>
    /// Enable/disable air pressure features
    /// Set to true when pressure data is properly populated
    /// </summary>
    public bool EnablePressureData { get; set; } = true;

    /// <summary>
    /// Enable/disable sunrise/sunset time display
    /// Set to true when sun times data is properly populated
    /// </summary>
    public bool EnableSunTimesDisplay { get; set; } = false;
}
