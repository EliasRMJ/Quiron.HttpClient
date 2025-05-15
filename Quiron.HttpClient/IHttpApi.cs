namespace Quiron.HttpClient
{
    public interface IHttpApi
    {
        Task AddBaseAddress(string domain);
        Task<T?> GetObjectAsync<T>(string endPoint, string token);
        Task<T?> PostObjectAsync<T>(string endPoint, T obj, string token = "");
        Task<T?> PutObjectAsync<T>(string endPoint, T obj, string token);
        Task<T?> PatchObjectAsync<T>(string endPoint, T obj, string token);
    }
}