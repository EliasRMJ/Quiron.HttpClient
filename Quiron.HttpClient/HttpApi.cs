using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;

namespace Quiron.HttpClient
{
    public abstract class HttpApi(System.Net.Http.HttpClient httpClient) : IHttpApi
    {
        protected virtual string BaseDomain => string.Empty;
        protected virtual int Timeout => 60;
        protected virtual Dictionary<string, string> Headers => new()
        {
            { "Accept", "application/json" },
            { "Accept-Encoding", "gzip,deflate,br" },
            { "Connection", "keep-alive" }
        };

        private bool _isConfigured = false;

        protected virtual Dictionary<string, string>? ResolveCertificateInfo()
        {
            return null; 
        }

        public void EnsureHttpClientReset()
        {
            _isConfigured = false;
        }

        public async virtual Task<T?> GetObjectAsync<T>(string endPoint, string token)
            => await SendAsync<T>(HttpMethod.Get, endPoint, token);

        public async virtual Task<T?> PostObjectAsync<T>(string endPoint, object obj, string? token = "")
            => await SendAsync<T>(HttpMethod.Post, endPoint, token, obj);

        public async virtual Task<T?> PatchObjectAsync<T>(string endPoint, object obj, string token)
            => await SendAsync<T>(HttpMethod.Patch, endPoint, token, obj);

        public async virtual Task<T?> PutObjectAsync<T>(string endPoint, object obj, string token)
            => await SendAsync<T>(HttpMethod.Put, endPoint, token, obj);

        private HttpRequestMessage CreateRequest(HttpMethod method, string endPoint, string? token
            , object? body = null)
        {
            var request = new HttpRequestMessage(method, endPoint);

            foreach (var header in Headers)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);

            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var infos = this.ResolveCertificateInfo();
            if (infos is not null && infos.Count.Equals(4))
            {
                request.Headers.Add("X-Dir-Base", infos["DirBase"]);
                request.Headers.Add("X-File-Name", infos["FileName"]);
                request.Headers.Add("X-Client-Id", infos["ClientId"]);
                request.Headers.Add("X-Client-Secret", infos["ClientSecret"]);
            }
         
            if (body is not null)
                request.Content = new StringContent(JsonConvert.SerializeObject(body)
                    , System.Text.Encoding.UTF8, "application/json");

            return request;
        }

        private async Task<T?> SendAsync<T>(HttpMethod method, string endPoint, string? token
            , object? body = null)
        {
            using var request = CreateRequest(method, endPoint, token, body);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout));

            EnsureHttpClientConfigured();

            var response = await httpClient.SendAsync(request, cts.Token);

            return await TryThrowException<T>(response.StatusCode, endPoint, response);
        }

        private void EnsureHttpClientConfigured()
        {
            if (_isConfigured) return;

            httpClient.Timeout = TimeSpan.FromSeconds(Timeout);

            if (!string.IsNullOrWhiteSpace(BaseDomain))
                httpClient.BaseAddress = new Uri(BaseDomain);

            _isConfigured = true;
        }

        private static async Task<T?> TryThrowException<T>(HttpStatusCode statusCode, string endPoint, HttpResponseMessage response)
        {
            var responseContent = response.Content is not null
                ? await response.Content.ReadAsStringAsync()
                : null;

            if (!string.IsNullOrWhiteSpace(responseContent))
                return JsonConvert.DeserializeObject<T>(responseContent);

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