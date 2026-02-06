namespace Quiron.HttpClient
{
    public abstract class HttpApiWithRetry(System.Net.Http.HttpClient httpClient) 
        : HttpApi(httpClient) { }
}