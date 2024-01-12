
namespace June.Data
{
    public class MeteoStatSettings
    {
        private string? _Key;
        public string Key
        {
            get => string.IsNullOrEmpty(_Key) ? Environment.GetEnvironmentVariable("METEOSTAT_KEY")! : _Key;
            set => _Key = value;
        }

        public required string Host { get; set; }
    }
}