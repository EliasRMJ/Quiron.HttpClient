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
            await this.Config(token);

            var response = await httpClient.GetAsync(endPoint);
            return await TryThrowException<T>(response.StatusCode, endPoint, response);
        }

        public async virtual Task<T?> PatchObjectAsync<T>(string endPoint, object obj, string token)
        {
            await this.Config(token);

            var response = await httpClient.PatchAsync(endPoint, await HttpContent(obj));
            return await TryThrowException<T>(response.StatusCode, endPoint, response);
        }

        public async virtual Task<T?> PostObjectAsync<T>(string endPoint, object obj, string? token = "")
        {
            await this.Config(token ?? string.Empty);

            var response = await httpClient.PostAsync(endPoint, await HttpContent(obj));
            return await TryThrowException<T>(response.StatusCode, endPoint, response);
        }

        public async virtual Task<T?> PutObjectAsync<T>(string endPoint, object obj, string token)
        {
            await this.Config(token);

            var response = await httpClient.PutAsync(endPoint, await HttpContent(obj));
            return await TryThrowException<T>(response.StatusCode, endPoint, response);
        }

        private Task Config(string token)
        {
            httpClient.Timeout = TimeSpan.FromSeconds(this.Timeout);
            if (!string.IsNullOrWhiteSpace(token))
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            if (!httpClient.DefaultRequestHeaders.Any())
            {
                foreach (var header in this.Headers)
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            if (!string.IsNullOrWhiteSpace(this.BaseDomain) && httpClient.BaseAddress is null)
                httpClient.BaseAddress = new Uri(this.BaseDomain);

            return Task.CompletedTask;
        }

        private static async Task<StringContent> HttpContent(object obj)
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
            else if (response.Content is not null)
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