using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;

namespace Quiron.HttpClient
{
    public abstract class HttpApi(System.Net.Http.HttpClient httpClient) : IHttpApi
    {
        protected const string _CERTIFICATE_HEADER = "X-Certificate";
        protected const string _CLIENT_ID_HEADER = "X-Client-Id";
        protected const string _CLIENT_SECRET_HEADER = "X-Client-Secret";

        protected virtual string? BaseDomain { get; set; }
        protected virtual int Timeout => 60;
        protected virtual Dictionary<string, string> Headers => new()
        {
            { "Accept", "application/pdf" },
            { "Accept-Encoding", "gzip,deflate,br" },
            { "Connection", "keep-alive" }
        };
        protected virtual JsonSerializerSettings JsonSerializerSettings => new() { NullValueHandling = NullValueHandling.Ignore };

        private bool _isConfigured = false;
        private readonly ConcurrentDictionary<string, System.Net.Http.HttpClient> _httpClients = new();

        public void EnsureHttpClientReset()
        {
            _isConfigured = false;
        }

        public async virtual Task<(byte[] content, string contentType)> DownloadAsync<T>(string endPoint
            , string? token = "")
        {
            return await DownloadAsync<T>(HttpMethod.Get, endPoint, null, token);
        }

        public async virtual Task<(byte[] content, string contentType)> DownloadAsync<T>(HttpMethod method
            , string endPoint, object? obj = null, string? token = "")
        {
            using var request = CreateRequest(method, endPoint, token, obj, "multipart/form-data");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout));

            this.EnsureHttpClientConfigured();

            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            response.EnsureSuccessStatusCode();

            if (response.Content is null)
                throw new Exception($"Response content is null to {endPoint}.");

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/pdf";

            return (bytes, contentType);
        }

        public async virtual Task<T?> GetObjectAsync<T>(string endPoint, string token)
            => await SendAsync<T>(HttpMethod.Get, endPoint, token);

        public async virtual Task<T?> PostObjectAsync<T>(string endPoint, object obj, string? token = "")
            => await SendAsync<T>(HttpMethod.Post, endPoint, token, obj);

        public async virtual Task<T?> PatchObjectAsync<T>(string endPoint, object obj, string token)
            => await SendAsync<T>(HttpMethod.Patch, endPoint, token, obj);

        public async virtual Task<T?> PutObjectAsync<T>(string endPoint, object obj, string token)
            => await SendAsync<T>(HttpMethod.Put, endPoint, token, obj);

        private System.Net.Http.HttpClient HttpClientWithCertFactory(string certId
            , string certificate, string secret)
        {
            return this._httpClients.GetOrAdd(certId, _ =>
            {
                var x509Certificate = X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(certificate), secret
                    , X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                
                var handler = new HttpClientHandler();
                handler.ClientCertificates?.Add(x509Certificate);

                return new System.Net.Http.HttpClient(handler, disposeHandler: false);
            });
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string endPoint, string? token
            , object? body = null, string contentType = "application/json")
        {
            var certificate = this.Headers.FirstOrDefault(find => find.Key == _CERTIFICATE_HEADER).Value;
            var clientId = this.Headers.FirstOrDefault(find => find.Key == _CLIENT_ID_HEADER).Value;
            var clientSecret = this.Headers.FirstOrDefault(find => find.Key == _CLIENT_SECRET_HEADER).Value;
           
            if (!string.IsNullOrEmpty(certificate) && 
                !string.IsNullOrEmpty(clientId) && 
                !string.IsNullOrEmpty(clientSecret))
                httpClient = this.HttpClientWithCertFactory(clientId, certificate, clientSecret);

            var request = new HttpRequestMessage(method, endPoint);

            var headersExclude = this.Headers.Where(find => find.Key != _CERTIFICATE_HEADER
                                                         && find.Key != _CLIENT_ID_HEADER
                                                         && find.Key != _CLIENT_SECRET_HEADER);
            foreach (var header in headersExclude)
               request.Headers.TryAddWithoutValidation(header.Key, header.Value);

            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            if (body is not null)
                request.Content = new StringContent(!contentType.Contains("multipart") ? 
                    JsonConvert.SerializeObject(body, new JsonSerializerSettings { NullValueHandling  = NullValueHandling.Ignore }) : 
                    body?.ToString() ?? string.Empty
                    , System.Text.Encoding.UTF8, contentType);

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

            if (string.IsNullOrWhiteSpace(this.BaseDomain))
                throw new Exception("'BaseDomain' property is not configured.");

            httpClient.Timeout = TimeSpan.FromSeconds(this.Timeout);
            httpClient.BaseAddress = new Uri(this.BaseDomain);
            
            _isConfigured = true;
        }

        private static async Task<T?> TryThrowException<T>(HttpStatusCode statusCode, string endPoint
            , HttpResponseMessage response)
        {
            var responseContent = response.Content is not null ? await response.Content.ReadAsStringAsync() : null;

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