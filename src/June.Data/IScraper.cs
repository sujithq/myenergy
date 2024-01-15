using System.Text.Json;

namespace June.Data
{
    internal interface IScraper
    {
        Task<JsonDocument?> LoginAsync();
        Task<JsonDocument?> GetData(Dictionary<string, string> config, string? date_id);
    }

}
