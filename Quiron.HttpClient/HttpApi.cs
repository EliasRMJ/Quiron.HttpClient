using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Quiron.HttpClient
{
    public abstract class HttpApi(System.Net.Http.HttpClient httpClient) : IHttpApi
    {
        protected virtual int Timeout => 60;
        protected virtual Dictionary<string, string> Headers => new Dictionary<string, string> {
            { "Accept", "application/json" },
            { "Accept-Encoding", "gzip,deflate,br" },
            { "Connection", "keep-alive" }
        };

        public async virtual Task<T> GetObjectAsync<T>(string endPoint, string token)
        {
            this.Config(token);

            var request = new HttpRequestMessage(HttpMethod.Get, endPoint);

            var response = await httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                T objectReturn = JsonConvert.DeserializeObject<T>(responseContent);
                return objectReturn!;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                throw new Exception($"[400] An unexpected error occurred while calling endpoint {endPoint}. Error: {await response.Content.ReadAsStringAsync()}");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new Exception("[404] Endpoint not found.");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new Exception($"[401] Unauthorized call to endpoint {endPoint}");
            }
            else
            {
                throw new Exception($"[500] An unexpected error occurred while executing the resource. Endpoint '{endPoint}' | Error: {await response.Content.ReadAsStringAsync()}");
            }
        }

        public async virtual Task<HttpResponseMessage> PatchObjectAsync<T>(string endPoint, T obj, string token)
        {
            this.Config(token);

            var requestJson = obj != null ? JsonConvert.SerializeObject(obj) : string.Empty;
            var httpContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

            return await httpClient.PatchAsync(endPoint, httpContent);
        }

        public async virtual Task<HttpResponseMessage> PostObjectAsync<T>(string endPoint, T obj, string? token = "")
        {
            this.Config(token ?? string.Empty);

            var requestJson = JsonConvert.SerializeObject(obj);
            var httpContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

            return await httpClient.PostAsync(endPoint, httpContent);
        }

        public async virtual Task<HttpResponseMessage> PutObjectAsync<T>(string endPoint, T obj, string token)
        {
            this.Config(token);

            var requestJson = JsonConvert.SerializeObject(obj);
            var httpContent = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

            return await httpClient.PutAsync(endPoint, httpContent);
        }

        private void Config(string token)
        {
            httpClient.DefaultRequestHeaders.Clear();
            for (int i = 0; i < Headers.Count; i++)
            {
                var header = Headers.ElementAt(i);
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
            if (!string.IsNullOrWhiteSpace(token))
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            httpClient.Timeout = TimeSpan.FromSeconds(Timeout);
        }
    }
}