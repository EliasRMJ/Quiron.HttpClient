## What is the Quiron.HttpClient?

Project created to group endpoint call methods to facilitate the development and consumption of APIs

## Give a Star! ⭐

If you find this project useful, please give it a star! It helps us grow and improve the community.

## Namespaces and Dependencies

- ✅ Quiron.HttpClient
- ✅ Newtonsoft.Json
- ✅ System.Net.Http

## Protected Properties

```csharp
protected const string _CERTIFICATE_HEADER = "X-Certificate";
protected const string _CLIENT_ID_HEADER = "X-Client-Id";
protected const string _CLIENT_SECRET_HEADER = "X-Client-Secret";
protected virtual string BaseDomain => string.Empty; // you need to implement your base domain here ⚠️
protected virtual int Timeout => 60;
protected virtual Dictionary<string, string> Headers => new() {
            { "Accept", "application/json" },
            { "Accept-Encoding", "gzip,deflate,br" },
            { "Connection", "keep-alive" }
        };
```

## Methods 

```csharp
Task<(byte[] content, string contentType)> DownloadAsync<T>(HttpMethod method, string endPoint, object? obj = null, string? token = "");
Task<(byte[] content, string contentType)> DownloadAsync<T>(string endPoint, string? token = "");
Task<T?> GetObjectAsync<T>(string endPoint, string token);
Task<T?> PostObjectAsync<T>(string endPoint, object obj, string token = "");
Task<T?> PutObjectAsync<T>(string endPoint, object obj, string token);
Task<T?> PatchObjectAsync<T>(string endPoint, object obj, string token);
```

Supports:

- ✅ .NET Standard 2.1  
- ✅ .NET 9 through 9 (including latest versions)  
- ⚠️ Legacy support for .NET Core 3.1 and older (with limitations)
  
## About
Quiron.HttpClient was developed by [EliasRMJ](https://www.linkedin.com/in/elias-medeiros-98232066/) under the [MIT license](LICENSE).
