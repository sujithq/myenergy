using Microsoft.Extensions.Options;
using Sungrow.Data.Commands;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace June.Data
{
    internal class SungrowScraper : IScraper
    {
        private static readonly string beginPublicKey = "-----BEGIN PUBLIC KEY-----";
        private static readonly string endPublicKey = "-----END PUBLIC KEY-----";

        private readonly SungrowSettings settings;

        private HttpClient client;

        public SungrowScraper(IOptions<SungrowSettings> settings)
        {
            this.settings = settings.Value;
            this.client = new HttpClient() { BaseAddress = new Uri(this.settings.gatewayUrl) };
        }

        private async Task<JsonDocument?> GetData(string url, Dictionary<string, object> data, Dictionary<string, string> addtionalHeaders, string randomKey)
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

            if (response.IsSuccessStatusCode)
            {
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();

                return DecryptHex(responseContent, randomKey);

            }
            else
            {
                Console.WriteLine($"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            }
            return default;

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
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.GenerateIV();

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var msEncrypt = new MemoryStream();
            msEncrypt.Write(aes.IV, 0, aes.IV.Length); // Prepend IV to the ciphertext
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
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.UTF8.GetBytes(key);

            using var msDecrypt = new MemoryStream(cipherText);
            byte[] iv = new byte[aes.BlockSize / 8];
            msDecrypt.Read(iv, 0, iv.Length); // Read IV from the beginning of the ciphertext
            var decryptor = aes.CreateDecryptor(aes.Key, iv);
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

        public async Task<JsonDocument?> LoginAsync()
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

        public async Task<JsonDocument?> GetData(Dictionary<string, string> config, string? date_id)
        {

            string token = config["token"];
            string user_id = config["user_id"];
            string randomKey = "web" + GenerateRandomWord(13);
            return await GetData($"v1/powerStationService/getHouseholdStoragePsReport", new Dictionary<string, object>
            {
                { "token", token },
                { "ps_id", settings.PS_ID },
                { "date_type", "1" },
                { "date_id", date_id! },
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

}
