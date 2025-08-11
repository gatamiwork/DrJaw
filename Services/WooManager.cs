using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DrJaw.Models;

namespace DrJaw
{
    public class WooManager
    {
        public string Domain { get; private set; } = "";
        public string ConsumerKey { get; private set; } = "";
        public string ConsumerSecret { get; private set; } = "";

        private static readonly HttpClient _httpClient = new HttpClient();
        public bool IsWooConnected { get; private set; } = false;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Domain) &&
            !string.IsNullOrWhiteSpace(ConsumerKey) &&
            !string.IsNullOrWhiteSpace(ConsumerSecret);

        public void SetConnection(string domain, string consumerKey, string consumerSecret)
        {
            Domain = domain.TrimEnd('/');
            ConsumerKey = consumerKey;
            ConsumerSecret = consumerSecret;
        }

        public static WooManager LoadFromSettings(WooConnectionSettings settings)
        {
            var manager = new WooManager();
            manager.SetConnection(settings.Domain, settings.ConsumerKey, settings.ConsumerSecret);
            return manager;
        }
        public string BuildRequestUrl(string endpoint, string method, Dictionary<string, string> parameters = null)
        {
            string storeUrl = Domain.TrimEnd('/');
            string consumerKey = ConsumerKey;
            string consumerSecret = ConsumerSecret;

            parameters ??= new Dictionary<string, string>();

            var oauthParams = new Dictionary<string, string>
            {
                { "oauth_consumer_key", consumerKey },
                { "oauth_nonce", GenerateNonce() },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_timestamp", GenerateTimestamp() },
                { "oauth_version", "1.0" }
            };

            var allParams = new Dictionary<string, string>(oauthParams);
            foreach (var param in parameters)
                allParams[param.Key] = param.Value;

            string signature = GenerateOAuthSignature(endpoint, method, allParams, storeUrl, consumerSecret);
            oauthParams["oauth_signature"] = signature;

            var queryString = string.Join("&", oauthParams.Concat(parameters)
                .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

            return $"{storeUrl}{endpoint}?{queryString}";
        }
        public string GenerateOAuthSignature(string endpoint, string method, Dictionary<string, string> parameters, string storeUrl, string consumerSecret)
        {
            var sortedParams = parameters.OrderBy(p => p.Key);
            string paramString = string.Join("&", sortedParams
                .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

            string baseString = $"{method.ToUpper()}&{Uri.EscapeDataString(storeUrl + endpoint)}&{Uri.EscapeDataString(paramString)}";
            string signingKey = $"{consumerSecret}&";

            using var hasher = new HMACSHA1(Encoding.UTF8.GetBytes(signingKey));
            byte[] signatureBytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(baseString));
            return Convert.ToBase64String(signatureBytes);
        }
        public string GenerateNonce() => Guid.NewGuid().ToString("N").Substring(0, 16);
        public string GenerateTimestamp() => ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();
        public async Task<bool> TestConnectionAsync()
        {
            if (!IsConfigured)
            {
                IsWooConnected = false;
                return false;
            }

            try
            {
                string nonce = Guid.NewGuid().ToString("N").Substring(0, 16);
                string timestamp = ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();

                string url = BuildRequestUrl("/wp-json/wc/v3/products", "GET");
                var response = await _httpClient.GetAsync(url);
                IsWooConnected = response.IsSuccessStatusCode;
                return IsWooConnected;
            }
            catch
            {
                IsWooConnected = false;
                return false;
            }
        }
    }
}
