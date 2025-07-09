using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;

namespace Quiron.HttpClient
{
    public abstract class HttpApi(System.Net.Http.HttpClient httpClient) : IHttpApi
    {
        protected virtual string BaseDomain => string.Empty;
        protected virtual int Timeout => 60;
        protected virtual Dictionary<string, string> Headers => new() {
            { "Accept", "application/json" },
            { "Accept-Encoding", "gzip,deflate,br" },
            { "Connection", "keep-alive" }
        };

        public async virtual Task<T?> GetObjectAsync<T>(string endPoint, string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(httpClient.BaseAddress!, endPoint));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            foreach (var header in this.Headers)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout));

            httpClient.Timeout = TimeSpan.FromSeconds(Timeout);
            var response = await httpClient.SendAsync(request, cts.Token);
            return await TryThrowException<T>(response.StatusCode, endPoint, response);
        }

        public async virtual Task<T?> PatchObjectAsync<T>(string endPoint, object obj, string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, new Uri(httpClient.BaseAddress!, endPoint));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = await HttpContent(obj);
            
            foreach (var header in this.Headers)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout));

            httpClient.Timeout = TimeSpan.FromSeconds(Timeout);
            var response = await httpClient.SendAsync(request, cts.Token);
            return await TryThrowException<T>(response.StatusCode, endPoint, response);
        }

        public async virtual Task<T?> PostObjectAsync<T>(string endPoint, object obj, string? token = "")
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(httpClient.BaseAddress!, endPoint));
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = await HttpContent(obj);

            foreach (var header in this.Headers)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout));

            httpClient.Timeout = TimeSpan.FromSeconds(Timeout);
            var response = await httpClient.SendAsync(request, cts.Token);
            return await TryThrowException<T>(response.StatusCode, endPoint, response);
        }

        public async virtual Task<T?> PutObjectAsync<T>(string endPoint, object obj, string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, new Uri(httpClient.BaseAddress!, endPoint));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = await HttpContent(obj);

            foreach (var header in this.Headers)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout));

            httpClient.Timeout = TimeSpan.FromSeconds(Timeout);
            var response = await httpClient.SendAsync(request, cts.Token);
            return await TryThrowException<T>(response.StatusCode, endPoint, response);
        }

        private static async Task<StringContent> HttpContent(object obj)
        {
            var requestJson = obj is not null ? JsonConvert.SerializeObject(obj) : string.Empty;
            var httpContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

            return await Task.FromResult(httpContent);
        }

        private static async Task<T?> TryThrowException<T>(HttpStatusCode statusCode, string endPoint, HttpResponseMessage response)
        {
            if (response.Content is not null)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                T objectReturn = JsonConvert.DeserializeObject<T>(responseContent);
                return objectReturn;
            }

            throw statusCode switch
            {
                HttpStatusCode.NotFound => new Exception($"[404] Endpoint not found. Endpoint {endPoint}"),
                HttpStatusCode.Forbidden => new Exception($"[403] Access denied to endpoint {endPoint}"),
                HttpStatusCode.Unauthorized => new Exception($"[401] Unauthorized call to endpoint {endPoint}"),
                HttpStatusCode.InternalServerError => new Exception($"[500] Internal serverError. Endpoint '{endPoint}'"),
                HttpStatusCode.ServiceUnavailable => new Exception($"[502] An unexpected error occurred while executing the resource. Endpoint '{endPoint}'"),
                _ => new Exception($"[{statusCode}] Unknown error. Endpoint '{endPoint}'")
            };
        }
    }
}