using DrJaw.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DrJaw
{
    public class WooRepository
    {
        private readonly WooManager _manager;
        private static readonly HttpClient _httpClient = new HttpClient();

        public WooRepository(WooManager manager)
        {
            _manager = manager;
        }
        public async Task<List<WooProduct>> GetProductsJsonAsync(int per_page, int page, int categoryId, bool searchType, string searchText)
        {
            string endpoint = "/wp-json/wc/v3/products";
            var parameters = new Dictionary<string, string>();
            if (per_page > 0) parameters["per_page"] = per_page.ToString();
            if (page > 0) parameters["page"] = page.ToString();
            if (categoryId > 0) parameters["category"] = categoryId.ToString();
            if (searchType && !string.IsNullOrEmpty(searchText))
                parameters["search"] = searchText;
            else
                parameters["sku"] = searchText;

            string url = _manager.BuildRequestUrl(endpoint, "GET", parameters);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<WooProduct>>(json);
        }
        public async Task<List<WooVariation>> GetVariationsAsync(int productId, List<int> variationIds)
        {
            var tasks = variationIds.Select(async variationId =>
            {
                string endpoint = $"/wp-json/wc/v3/products/{productId}/variations/{variationId}";
                string url = _manager.BuildRequestUrl(endpoint, "GET");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<WooVariation>(json);
            });

            return (await Task.WhenAll(tasks)).ToList();
        }
        public async Task<string> CreateProductAsync(string productJson)
        {
            string endpoint = "/wp-json/wc/v3/products";
            string url = _manager.BuildRequestUrl(endpoint, "POST");

            var content = new StringContent(productJson, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        public async Task<string> UpdateProductAsync(int productId, string productJson)
        {
            string endpoint = $"/wp-json/wc/v3/products/{productId}";
            string url = _manager.BuildRequestUrl(endpoint, "PUT");

            var content = new StringContent(productJson, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(url, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        public async Task<string> DeleteProductAsync(int productId)
        {
            string endpoint = $"/wp-json/wc/v3/products/{productId}";
            string url = _manager.BuildRequestUrl(endpoint, "DELETE");

            var response = await _httpClient.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        public async Task<List<WooCategory>> GetAllCategoriesAsync()
        {
            var allCategories = new List<WooCategory>();
            int page = 1;
            const int pageSize = 100;

            while (true)
            {
                string endpoint = "/wp-json/wc/v3/products/categories";
                string url = _manager.BuildRequestUrl(endpoint, "GET", new Dictionary<string, string>
        {
            { "per_page", pageSize.ToString() },
            { "page", page.ToString() }
        });

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var parsed = JsonSerializer.Deserialize<List<WooCategory>>(json);

                if (parsed == null || parsed.Count == 0)
                    break;

                allCategories.AddRange(parsed);
                page++;
            }

            return allCategories;
        }
        public async Task<bool> CreateCategoryAsync(WooCategory data)
        {
            string endpoint = "/wp-json/wc/v3/products/categories";
            string url = _manager.BuildRequestUrl(endpoint, "POST");

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            return true;
        }
        public async Task<bool> UpdateCategoryAsync(int id, WooCategory data)
        {
            string endpoint = $"/wp-json/wc/v3/products/categories/{id}";
            string url = _manager.BuildRequestUrl(endpoint, "PUT");

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(url, content);
            response.EnsureSuccessStatusCode();
            return true;
        }
        public async Task<bool> DeleteCategoryAsync(int id)
        {
            string endpoint = $"/wp-json/wc/v3/products/categories/{id}";
            string url = _manager.BuildRequestUrl(endpoint, "DELETE", new Dictionary<string, string>
            {
                { "force", "true" }
            });

            var response = await _httpClient.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
            return true;
        }
        public async Task<string> GetAttributesJsonAsync()
        {
            var allJson = new List<string>();
            int page = 1;
            const int pageSize = 100;

            while (true)
            {
                string endpoint = "/wp-json/wc/v3/products/attributes";
                string url = _manager.BuildRequestUrl(endpoint, "GET", new Dictionary<string, string>
                {
                    { "per_page", pageSize.ToString() },
                    { "page", page.ToString() }
                });

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var parsed = JsonSerializer.Deserialize<List<WooAttribute>>(json);
                if (parsed == null || parsed.Count == 0)
                    break;

                allJson.Add(json);
                page++;
            }
            return $"[{string.Join(",", allJson)}]";
        }
        public async Task<BitmapImage?> GetImageAsync(string imageUrl)
        {
            try
            {
                var response = await _httpClient.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync();
                var bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze(); // безопасен для многопоточности

                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
