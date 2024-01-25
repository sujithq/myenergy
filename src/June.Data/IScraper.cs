using myenergy.Common;
using System.Text.Json;

namespace June.Data
{
    public interface IScraper
    {
        Task<JsonDocument?> LoginAsync();
        Task<JsonDocument?> GetData(Dictionary<string, string> config, string? date_id);
    }

    public interface IJuneScraper : IScraper
    {
        Task<QuarterData?> GetQuarterData(Dictionary<string, string> config, string? date_id);
    }


}
