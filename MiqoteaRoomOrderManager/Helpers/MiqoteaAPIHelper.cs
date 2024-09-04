using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;

namespace MiqoteaRoomOrderManager.Helpers
{
    public class MiqoteaAPIHelper
    {
        private readonly HttpClient _httpClient;

        public MiqoteaAPIHelper(string baseUrl = "https://api.miqotea.com")
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void SetAuthorizationHeader(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // GET request
        public async Task<T> GetAsync<T>(string endpoint)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            string responseData = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseData);
        }

        // POST request
        public async Task<(HttpStatusCode StatusCode, TResponse Response)> PostAsync<TRequest, TResponse>(string endpoint, TRequest content)
        {
            string jsonContent = JsonConvert.SerializeObject(content);
            StringContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(endpoint, httpContent);

            string responseData = await response.Content.ReadAsStringAsync();
            return (response.StatusCode, JsonConvert.DeserializeObject<TResponse>(responseData));
        }

        // PUT request
        public async Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest content)
        {
            string jsonContent = JsonConvert.SerializeObject(content);
            StringContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PutAsync(endpoint, httpContent);
            response.EnsureSuccessStatusCode();

            string responseData = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TResponse>(responseData);
        }

        // DELETE request
        public async Task DeleteAsync(string endpoint)
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync(endpoint);
            response.EnsureSuccessStatusCode();
        }

        // Optionally: a method to handle custom headers
        public void AddDefaultRequestHeader(string name, string value)
        {
            _httpClient.DefaultRequestHeaders.Add(name, value);
        }
    }
}
