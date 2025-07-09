namespace Quiron.HttpClient
{
    public interface IHttpApi
    {
        void EnsureHttpClientReset();
        Task<T?> GetObjectAsync<T>(string endPoint, string token);
        Task<T?> PostObjectAsync<T>(string endPoint, object obj, string token = "");
        Task<T?> PutObjectAsync<T>(string endPoint, object obj, string token);
        Task<T?> PatchObjectAsync<T>(string endPoint, object obj, string token);
    }
}