using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static June.Data.JuneScraper;

namespace June.Data
{


    internal class Program
    {
        static async Task Main(string[] args)
        {
            var failed = false;
            var devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");

            var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) ||
                                devEnvironmentVariable.ToLower() == "development";

            //Determines the working environment as IHostingEnvironment is unavailable in a console app

            var builder = new ConfigurationBuilder();
            // tell the builder to look for the appsettings.json file
            builder
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // only add secrets in development
            if (isDevelopment)
            {
                builder
                    .AddUserSecrets<Program>();
            }

            var Configuration = builder.Build();

            IServiceCollection services = new ServiceCollection();

            //Map the implementations of your classes here ready for DI
            services
                .Configure<JuneSettings>(Configuration.GetSection(nameof(JuneSettings)))
                .Configure<SungrowSettings>(Configuration.GetSection(nameof(SungrowSettings)))
                .AddOptions()
                .BuildServiceProvider();

            var serviceProvider = services.BuildServiceProvider();

            // Get JuneSettings from DI
            var juneSettings = serviceProvider.GetService<IOptions<JuneSettings>>();
            var sungrowSettings = serviceProvider.GetService<IOptions<SungrowSettings>>();

            var juneScraper = new JuneScraper(juneSettings!.Value);
            var sungrowScraper = new SungrowScraper(sungrowSettings!.Value);

            var juneLogin = await juneScraper.LoginAsync();
            var sungrowLogin = await sungrowScraper.LoginAsync();

            var refresh_token = juneLogin!.RootElement.GetProperty("refresh_token").GetString();
            // Get token and user_id
            var token = sungrowLogin.RootElement.GetProperty("result_data").GetProperty("token").GetString();
            var user_id = sungrowLogin.RootElement.GetProperty("result_data").GetProperty("user_id").GetString();

            var data = JsonSerializer.Deserialize<Dictionary<int, List<BarChartData>>>(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Data/data.json")));

            var cultureInfo = new CultureInfo("en-US");
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            var currentDateInBelgium = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo).Date;

            // For JuneProcessed = false
            var listForJuneProcessed = data!
                .SelectMany(kvp => kvp.Value.Where(data => !data.JuneProcessed)
                                            .Select(data => (kvp.Key, data.DayNumber, DateTime.ParseExact($"{data.DayMonth}/{kvp.Key}", "d/M/yyyy", cultureInfo)))
                .Where(date => date.Item3 <= currentDateInBelgium)
                .Select(date => (date.Key, date.DayNumber, date.Item3.ToString("yyyyMMdd"), date.Item3)))
                .ToList();

            if (listForJuneProcessed.Count == 0)
                listForJuneProcessed.Add((currentDateInBelgium.Year, currentDateInBelgium.DayOfYear, currentDateInBelgium.ToString("yyyyMMdd"), currentDateInBelgium));

            // For SungrowProcessed = false
            var listForSungrowProcessed = data!
                .SelectMany(kvp => kvp.Value.Where(data => !data.SungrowProcessed)
                                            .Select(data => (kvp.Key, data.DayNumber, DateTime.ParseExact($"{data.DayMonth}/{kvp.Key}", "d/M/yyyy", cultureInfo)))
                .Where(date => date.Item3 <= currentDateInBelgium)
                .Select(date => (date.Key, date.DayNumber, date.Item3.ToString("yyyyMMdd"), date.Item3)))
                .ToList();

            if (listForSungrowProcessed.Count == 0)
                listForSungrowProcessed.Add((currentDateInBelgium.Year, currentDateInBelgium.DayOfYear, currentDateInBelgium.ToString("yyyyMMdd"), currentDateInBelgium));

            foreach (var item in listForJuneProcessed)
            {
                var juneData = await juneScraper.GetData(new Dictionary<string, string>() { { "refresh_token", refresh_token! } }, item.Item3);

                var consumption = juneData.RootElement.GetProperty("electricity").GetProperty("single").GetProperty("consumption").GetDouble();
                var injection = juneData.RootElement.GetProperty("electricity").GetProperty("single").GetProperty("injection").GetDouble() * 1000;

                if (!data.ContainsKey(item.Key))
                {
                    data.Add(item.Key, new List<BarChartData>());
                }
                if(data![item.Key].Count() < item.DayNumber)
                {
                    data![item.Key].Add(new BarChartData(item.DayNumber, item.Item4.ToString("d/M"), 0, 0, 0, false, false));
                }

                var d = data![item.Key][item.DayNumber - 1];
                data![item.Key][item.DayNumber - 1] = new BarChartData(d.DayNumber, d.DayMonth, d.Production, consumption, injection, item.Item4 == currentDateInBelgium ? false : true, d.SungrowProcessed);
            }

            foreach (var item in listForSungrowProcessed)
            {
                var sungrowData = await sungrowScraper.GetData(new Dictionary<string, string>() { { "token", token! }, { "user_id", user_id! } }, item.Item3);
                var result_data = sungrowData.RootElement.GetProperty("result_data");
                if(result_data.ValueKind == JsonValueKind.Null)
                {
                    await Console.Error.WriteLineAsync($"Error: ({sungrowData.RootElement.GetProperty("result_code").GetString()}) {sungrowData.RootElement.GetProperty("result_msg").GetString()}");
                    failed = true;
                }
                else
                {
                    var production = double.Parse(result_data.GetProperty("day_data").GetProperty("p83077_map_virgin").GetProperty("value").GetString()!) / 1000;
                    var d = data![item.Key][item.DayNumber - 1];
                    data![item.Key][item.DayNumber - 1] = new BarChartData(d.DayNumber, d.DayMonth, production, d.Usage, d.Injection, d.JuneProcessed, item.Item4 == currentDateInBelgium ? false : true);

                }
            }

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "Data/data.json"), JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));

            if (failed)
            {
                Environment.ExitCode = 1;
            }
        }
    }

    internal class JuneScraper
    {
        private readonly JuneSettings settings;
        public JuneScraper(JuneSettings settings)
        {
            this.settings = settings;
        }
        public async Task<JsonDocument?> LoginAsync()
        {

            // June API endpoints
            var juneBaseAddress = "https://api.june.energy/";
            var juneTokenEndpoint = "rest/oauth/token";

            // Create a new HttpClient and set the base address
            var client = new HttpClient { BaseAddress = new Uri(juneBaseAddress) };

            // Create the request body as JSON
            string json = JsonSerializer.Serialize(settings);

            var requestToken = new HttpRequestMessage(HttpMethod.Post, juneTokenEndpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Send the request to the server and wait for the response
            var response = await client.SendAsync(requestToken);

            // If the response contains content we want to read it!
            if (response.IsSuccessStatusCode)
            {
                // Read the response content as a string
                json = await response.Content.ReadAsStringAsync();

                return JsonDocument.Parse(json);
            }
            return default;
        }
        public async Task<JsonDocument> GetData(Dictionary<string, string> config, string date_id)
        {

            var from = DateOnly.ParseExact(date_id, "yyyyMMdd").ToString("yyyy-MM-dd");
            var to = DateOnly.ParseExact(date_id, "yyyyMMdd").AddDays(1).ToString("yyyy-MM-dd");
            var valueType = "ENERGY";
            var refresh_token = config["refresh_token"];

            // June API endpoints
            var juneBaseAddress = "https://api.june.energy/";
            var juneSummaryEndpoint = $"eliq/contract/18713/summary?from={from}&to={to}&valueType={valueType}";

            // Create a new HttpClient and set the base address
            var client = new HttpClient { BaseAddress = new Uri(juneBaseAddress) };

            // Create the request body as JSON
            string json = JsonSerializer.Serialize(settings);

            // Create the request body as JSON
            var requestData = new HttpRequestMessage(HttpMethod.Get, juneSummaryEndpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Add the access token to the request header
            requestData.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refresh_token);

            // Send the request to the server and wait for the response
            var dataResponse = await client.SendAsync(requestData);

            // If the response contains content we want to read it!
            if (dataResponse.IsSuccessStatusCode)
            {
                // Read the response content as a string
                var dataResponseStringContent = await dataResponse.Content.ReadAsStringAsync();

                return JsonDocument.Parse(dataResponseStringContent);

            }
            return await Task.FromResult<JsonDocument>(null);
        }
    }

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

    internal class SungrowScraper
    {
        private static readonly string beginPublicKey = "-----BEGIN PUBLIC KEY-----";
        private static readonly string endPublicKey = "-----END PUBLIC KEY-----";

        private readonly SungrowSettings settings;

        private HttpClient client;

        public SungrowScraper(SungrowSettings settings)
        {
            this.settings = settings;
            this.client = new HttpClient() { BaseAddress = new Uri(settings.gatewayUrl) };
        }

        private async Task<JsonDocument> GetData(string url, Dictionary<string, object> data, Dictionary<string, string> addtionalHeaders, string randomKey)
        {
            string dataStr = JsonSerializer.Serialize(data);
            string dataHex = EncryptHex(dataStr, randomKey);

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(dataHex, Encoding.UTF8, "application/json")
            };

            foreach (var header in addtionalHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();

            return DecryptHex(responseContent, randomKey);
        }

        // RSA encryption method
        private string EncryptRSA(string value, string publicKeyString)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyString.ToCharArray());
            var encryptedBytes = rsa.Encrypt(Encoding.UTF8.GetBytes(value), RSAEncryptionPadding.Pkcs1);
            return Convert.ToBase64String(encryptedBytes);
        }

        private byte[] EncryptAES(string data, string key)
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.UTF8.GetBytes(key);

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var msEncrypt = new MemoryStream();
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(data);
            }

            return msEncrypt.ToArray();
        }

        private string DecryptAES(byte[] cipherText, string key)
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.UTF8.GetBytes(key);

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var msDecrypt = new MemoryStream(cipherText);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            return srDecrypt.ReadToEnd();
        }

        private string EncryptHex(string dataStr, string key)
        {
            var encryptedBytes = EncryptAES(dataStr, key);
            return BitConverter.ToString(encryptedBytes).Replace("-", "").ToLowerInvariant();
        }

        private JsonDocument DecryptHex(string dataHexStr, string key)
        {
            var dataBytes = Convert.FromHexString(dataHexStr);
            var decryptedText = DecryptAES(dataBytes, key);
            return JsonDocument.Parse(decryptedText);
        }

        // Random string generator method
        private string GenerateRandomWord(int length)
        {
            var random = new Random();
            var characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var result = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                result.Append(characters[random.Next(characters.Length)]);
            }
            return result.ToString();
        }

        public async Task<JsonDocument> LoginAsync()
        {
            string randomKey = "web" + GenerateRandomWord(13);

            return await GetData($"v1/userService/login", new Dictionary<string, object>
            {
                { "user_account", settings.username },
                { "user_password", settings.password },
                { "api_key_param", new {
                        timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                        nonce = GenerateRandomWord(32),
                    } },
                { "appkey", settings.APP_KEY }
            }
            , new Dictionary<string, string>
            {
                { "sys_code", "200" },
                { "x-access-key", settings.ACCESS_KEY },
                { "x-random-secret-key", EncryptRSA(randomKey, $"{beginPublicKey}{Environment.NewLine}{settings.APP_RSA_PUBLIC_KEY.Replace("-", "+").Replace("_", "/")}{Environment.NewLine}{endPublicKey}")}
            }, randomKey);


        }

        public async Task<JsonDocument> GetData(Dictionary<string, string> config, string date_id)
        {

            string token = config["token"];
            string user_id = config["user_id"];
            string randomKey = "web" + GenerateRandomWord(13);
            return await GetData($"v1/powerStationService/getHouseholdStoragePsReport", new Dictionary<string, object>
            {
                { "token", token },
                { "ps_id", settings.PS_ID },
                { "date_type", "1" },
                { "date_id", date_id },
                { "api_key_param", new
                    {
                        timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                        nonce = GenerateRandomWord(32),
                    } },
                { "appkey", settings.APP_KEY }
            }, new Dictionary<string, string>
            {
                { "sys_code", "200" },
                { "x-access-key", settings.ACCESS_KEY },
                { "x-random-secret-key", EncryptRSA(randomKey, $"{beginPublicKey}{Environment.NewLine}{settings.APP_RSA_PUBLIC_KEY.Replace("-", "+").Replace("_", "/")}{Environment.NewLine}{endPublicKey}")},
                { "x-limit-obj", EncryptRSA(user_id, $"{beginPublicKey}{Environment.NewLine}{settings.APP_RSA_PUBLIC_KEY.Replace("-", "+").Replace("_", "/")}{Environment.NewLine}{endPublicKey}")},
            }, randomKey);


        }
    }
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
    record BarChartData(int DayNumber, string DayMonth, double Production, double Usage, double Injection, bool JuneProcessed, bool SungrowProcessed);

}
