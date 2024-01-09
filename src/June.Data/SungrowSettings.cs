namespace June.Data
{
    public class SungrowSettings
    {
        public required string username { get; set; }
        public required string gatewayUrl { get; set; }

        private string _password;
        public string password
        {
            get => string.IsNullOrEmpty(_password) ? Environment.GetEnvironmentVariable("SUNGROW_PASSWORD")! : _password;
            set => _password = value;
        }

        private string _APP_RSA_PUBLIC_KEY;
        public string APP_RSA_PUBLIC_KEY
        {
            get => string.IsNullOrEmpty(_APP_RSA_PUBLIC_KEY) ? Environment.GetEnvironmentVariable("SUNGROW_APP_RSA_PUBLIC_KEY")! : _APP_RSA_PUBLIC_KEY;
            set => _APP_RSA_PUBLIC_KEY = value;
        }

        private string _ACCESS_KEY;
        public string ACCESS_KEY
        {
            get => string.IsNullOrEmpty(_ACCESS_KEY) ? Environment.GetEnvironmentVariable("SUNGROW_ACCESS_KEY")! : _ACCESS_KEY;
            set => _ACCESS_KEY = value;

        }

        private string _APP_KEY;
        public string APP_KEY
        {
            get => string.IsNullOrEmpty(_APP_KEY) ? Environment.GetEnvironmentVariable("SUNGROW_APP_KEY")! : _APP_KEY;
            set => _APP_KEY = value;

        }
        private string _PS_ID;
        public string PS_ID
        {
            get => string.IsNullOrEmpty(_PS_ID) ? Environment.GetEnvironmentVariable("SUNGROW_PS_ID")! : _PS_ID;
            set => _PS_ID = value;

        }
    }

}
