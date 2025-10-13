using myenergy.Models;

namespace myenergy.Services;

/// <summary>
/// Interface for ODS pricing services that load and provide access to dynamic electricity pricing data.
/// Implementations may use different data formats (JSON, Parquet, etc.) while maintaining a consistent API.
/// </summary>
public interface IOdsPricingService
{
    /// <summary>
    /// Indicates whether pricing data has been successfully loaded.
    /// </summary>
    bool IsDataLoaded { get; }

    /// <summary>
    /// The timestamp when data was last successfully loaded.
    /// </summary>
    DateTime? LastLoadTime { get; }

    /// <summary>
    /// Loads ODS pricing data from the configured source (file or API).
    /// </summary>
    /// <param name="forceRefresh">If true, forces download from API even if local data exists.</param>
    Task LoadDataAsync(bool forceRefresh = false);

    /// <summary>
    /// Forces a refresh of data from the Elia API.
    /// </summary>
    Task RefreshFromEliaAsync();

    /// <summary>
    /// Gets the pricing for a specific 15-minute interval.
    /// </summary>
    /// <param name="time">The datetime to get pricing for.</param>
    /// <returns>The pricing record for that interval, or null if not found.</returns>
    OdsPricing? GetPricingForInterval(DateTime time);

    /// <summary>
    /// Gets all 96 pricing intervals for a specific day.
    /// </summary>
    /// <param name="date">The date to get pricing for.</param>
    /// <returns>List of pricing records for the day (should be 96 intervals).</returns>
    List<OdsPricing> GetPricingForDay(DateTime date);

    /// <summary>
    /// Gets all pricing records within a date range.
    /// </summary>
    /// <param name="startDate">Start of the range (inclusive).</param>
    /// <param name="endDate">End of the range (inclusive).</param>
    /// <returns>List of pricing records in the range.</returns>
    List<OdsPricing> GetPricingForDateRange(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Calculates price statistics for a given year (or all years if null).
    /// </summary>
    /// <param name="year">The year to analyze, or null for all data.</param>
    /// <returns>Tuple containing average, min, and max prices for import and export.</returns>
    (double avgImport, double avgExport, double minImport, double maxImport, double minExport, double maxExport) GetPriceStatistics(int? year = null);

    /// <summary>
    /// Gets the available years in the dataset.
    /// </summary>
    /// <returns>List of years for which data is available.</returns>
    List<int> GetAvailableYears();

    /// <summary>
    /// Gets the date range covered by the loaded data.
    /// </summary>
    /// <returns>Tuple with start and end dates, or null if no data loaded.</returns>
    (DateTime start, DateTime end)? GetDataRange();

    /// <summary>
    /// Gets hourly average prices for a specific date.
    /// </summary>
    /// <param name="date">The date to get hourly averages for.</param>
    /// <returns>Dictionary with hour (0-23) as key and tuple of (avgImport, avgExport) as value.</returns>
    Dictionary<int, (double avgImport, double avgExport)> GetHourlyAveragePrices(DateTime date);

    /// <summary>
    /// Gets the distribution of prices (all price values) for analysis.
    /// </summary>
    /// <param name="year">The year to analyze, or null for all data.</param>
    /// <returns>Tuple with lists of all import and export prices.</returns>
    (List<double> importPrices, List<double> exportPrices) GetPriceDistribution(int? year = null);
}
