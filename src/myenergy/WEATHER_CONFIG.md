# Weather Data Configuration

## Overview
The myenergy application includes weather correlation features that are currently disabled because the weather data (sunshine duration, precipitation, etc.) is not yet fully populated in the data files.

## Current Status
✅ **Enabled Features:**
- Temperature data
- Wind speed data  
- Air pressure data

⏳ **Disabled Features (Pending Data):**
- Sunshine duration (tsun) - All values currently 0
- Precipitation (prcp) - All values currently 0
- Sunrise/sunset times display - Data not populated

## How to Enable Weather Features

When your weather data becomes available, you can enable these features by editing the configuration:

### Step 1: Update Configuration Service

Edit `Services/AppConfigurationService.cs` and modify the configuration values:

```csharp
public AppConfigurationService()
{
    _config = new AppConfiguration
    {
        EnableWeatherData = true,         // ✅ Set to true when data is ready
        EnableSunshineData = true,        // ✅ Enable sunshine duration
        EnablePrecipitationData = true,   // ✅ Enable precipitation
        EnableTemperatureData = true,     // Already enabled
        EnableWindData = true,            // Already enabled
        EnablePressureData = true,        // Already enabled
        EnableSunTimesDisplay = true      // ✅ Enable sunrise/sunset display
    };
}
```

### Step 2: Rebuild the Application

```bash
dotnet build
```

### Step 3: Verify Data

The Weather Correlation page (`/weather-correlation`) will automatically activate when `EnableWeatherData = true`.

## Feature Flags

### Master Switch
- **EnableWeatherData**: Master switch that must be true for any weather features to work

### Individual Switches
- **EnableSunshineData**: Controls visibility of sunshine duration metrics
- **EnablePrecipitationData**: Controls visibility of precipitation metrics
- **EnableTemperatureData**: Controls visibility of temperature metrics
- **EnableWindData**: Controls visibility of wind speed metrics
- **EnablePressureData**: Controls visibility of air pressure metrics
- **EnableSunTimesDisplay**: Controls display of sunrise/sunset times

## Affected Components

### Pages
- **WeatherCorrelation.razor**: Shows a notice when `EnableWeatherData = false`
  - Dynamically adjusts available weather factors based on enabled flags
  - Hides sun/precip columns in tables when disabled

### Components  
- **DetailedDayChart.razor**: Hides sunrise/sunset times when `EnableSunTimesDisplay = false`

## User Experience

### When Disabled
Users visiting `/weather-correlation` will see:
```
⚙️ Weather Data Not Yet Available

Weather correlation features are currently disabled because weather 
data is not fully populated in the system.

This page will automatically activate when:
• Sunshine duration data (tsun) is properly recorded
• Precipitation data (prcp) is properly recorded
• Temperature and other weather metrics are validated
```

### When Enabled
All weather correlation features become available:
- Scatter plots showing weather vs production correlation
- Multi-factor correlation analysis
- Weather range distribution charts
- Detailed weather impact breakdowns

## Data Requirements

For full weather functionality, ensure your data files include:

```json
{
  "MS": {
    "tavg": 15.2,    // Average temperature (°C)
    "tmin": 10.1,    // Min temperature (°C)  
    "tmax": 20.3,    // Max temperature (°C)
    "prcp": 5.2,     // ✅ Precipitation (mm) - currently 0
    "snow": 0,       // Snow (mm)
    "wdir": 180,     // Wind direction (degrees)
    "wspd": 12.5,    // Wind speed (km/h)
    "wpgt": 25.3,    // Wind gust peak (km/h)
    "pres": 1013.2,  // Air pressure (hPa)
    "tsun": 480      // ✅ Sunshine duration (minutes) - currently 0
  },
  "SRS": {
    "R": "2024-01-15T07:30:00",  // ✅ Sunrise - not displayed when disabled
    "S": "2024-01-15T17:45:00"   // ✅ Sunset - not displayed when disabled
  }
}
```

## Testing

To test with sample data before full weather data is available:

1. Set `EnableWeatherData = true`
2. Set only `EnableTemperatureData = true` and `EnableWindData = true`
3. Test that sunshine and precipitation options don't appear in dropdowns
4. Verify tables don't show disabled columns

## Future Enhancements

Consider adding:
- Admin panel for runtime configuration changes
- Automatic data quality detection
- Partial feature enablement based on data availability
- Data validation warnings

---

**Last Updated**: October 6, 2025  
**Status**: Weather features ready for activation when data becomes available
