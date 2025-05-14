namespace Quiron.HttpClient
{
    public interface IHttpApi
    {
        Task<T> GetObjectAsync<T>(string endPoint, string token);
        Task<HttpResponseMessage> PostObjectAsync<T>(string endPoint, T obj, string token = "");
        Task<HttpResponseMessage> PutObjectAsync<T>(string endPoint, T obj, string token);
        Task<HttpResponseMessage> PatchObjectAsync<T>(string endPoint, T obj, string token);
    }
}