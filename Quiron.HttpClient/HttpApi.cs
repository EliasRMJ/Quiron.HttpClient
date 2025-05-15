using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;

namespace Quiron.HttpClient
{
    public abstract class HttpApi(System.Net.Http.HttpClient httpClient) : IHttpApi
    {
        protected virtual int Timeout => 60;
        protected virtual Dictionary<string, string> Headers => new() {
            { "Accept", "application/json" },
            { "Accept-Encoding", "gzip,deflate,br" },
            { "Connection", "keep-alive" }
        };

        public async virtual Task AddBaseAddress(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return;
            httpClient.BaseAddress = new Uri(domain);
            await Task.CompletedTask;
        }

        public async virtual Task<T?> GetObjectAsync<T>(string endPoint, string token)
        {
            this.Config(token);

            var request = new HttpRequestMessage(HttpMethod.Get, endPoint);

            var response = await httpClient.SendAsync(request);
            return await TryThrowException<T>(response.StatusCode, endPoint, response);
        }

        public async virtual Task<T?> PatchObjectAsync<T>(string endPoint, T obj, string token)
        {
            this.Config(token);

            var response = await httpClient.PatchAsync(endPoint, await HttpContent<T>(obj));
            return await TryThrowException<T>(response.StatusCode, endPoint, response);
        }

        public async virtual Task<T?> PostObjectAsync<T>(string endPoint, T obj, string? token = "")
        {
            this.Config(token ?? string.Empty);

            var response = await httpClient.PostAsync(endPoint, await HttpContent<T>(obj));
            return await TryThrowException<T>(response.StatusCode, endPoint, response);
        }

        public async virtual Task<T?> PutObjectAsync<T>(string endPoint, T obj, string token)
        {
            this.Config(token);

            var response = await httpClient.PutAsync(endPoint, await HttpContent<T>(obj));
            return await TryThrowException<T>(response.StatusCode, endPoint, response);
        }

        private void Config(string token)
        {
            httpClient.DefaultRequestHeaders.Clear();
            foreach (var header in this.Headers)
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);

            if (!string.IsNullOrWhiteSpace(token))
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            httpClient.Timeout = TimeSpan.FromSeconds(this.Timeout);
        }

        private static async Task<StringContent> HttpContent<T>(T obj)
        {
            var requestJson = obj != null ? JsonConvert.SerializeObject(obj) : string.Empty;
            var httpContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
            return await Task.FromResult(httpContent);
        }

        private static async Task<T?> TryThrowException<T>(HttpStatusCode statusCode, string endPoint, HttpResponseMessage response)
        {
            if (statusCode.Equals(HttpStatusCode.OK))
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                T objectReturn = JsonConvert.DeserializeObject<T>(responseContent);
                return objectReturn;
            }

            var message = response.Content is not null ? await response.Content.ReadAsStringAsync() : string.Empty;

            throw statusCode switch
            {
                HttpStatusCode.BadRequest => new Exception($"[400] An unexpected error occurred while calling endpoint {endPoint}. Error: {message}"),
                HttpStatusCode.NotFound => new Exception($"[404] Endpoint not found. Endpoint {endPoint}"),
                HttpStatusCode.Forbidden => new Exception($"[403] Access denied to endpoint {endPoint}"),
                HttpStatusCode.Unauthorized => new Exception($"[401] Unauthorized call to endpoint {endPoint}"),
                _ => new Exception($"[500] An unexpected error occurred while executing the resource. Endpoint '{endPoint}'")
            };
        }
    }
}