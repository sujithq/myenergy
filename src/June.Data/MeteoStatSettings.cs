
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
        public required string Station { get; set; } = "06451";
    }
}