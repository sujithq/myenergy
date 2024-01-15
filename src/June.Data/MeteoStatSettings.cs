
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

        private string? _Lat;
        public string Lat
        {
            get => string.IsNullOrEmpty(_Lat) ? Environment.GetEnvironmentVariable("METEOSTAT_LAT")! : _Lat;
            set => _Lat = value;
        }

        private string? _Lon;
        public string Lon
        {
            get => string.IsNullOrEmpty(_Lon) ? Environment.GetEnvironmentVariable("METEOSTAT_LON")! : _Lon;
            set => _Lon = value;
        }
    }
}