namespace June.Data
{
    public class JuneSettings
    {
        public required string username { get; set; }
        public required string grant_type { get; set; }

        private string _password;
        public string password
        {
            get => string.IsNullOrEmpty(_password) ? Environment.GetEnvironmentVariable("JUNE_PASSWORD")! : _password;
            set => _password = value;

        }
        private string _client_id;
        public string client_id
        {
            get => string.IsNullOrEmpty(_client_id) ? Environment.GetEnvironmentVariable("JUNE_CLIENT_ID")! : _client_id;
            set => _client_id = value;

        }
        private string _client_secret;
        public string client_secret
        {
            get => string.IsNullOrEmpty(_client_secret) ? Environment.GetEnvironmentVariable("JUNE_CLIENT_SECRET")! : _client_secret;
            set => _client_secret = value;

        }
    }

}
