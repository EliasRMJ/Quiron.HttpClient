## What is the Quiron.HttpClient?

Project created to group endpoint call methods to facilitate the development and consumption of APIs

## Give a Star! ⭐

If you find this project useful, please give it a star! It helps us grow and improve the community.

## Namespaces and Dependencies

- ✅ Quiron.HttpClient
- ✅ Newtonsoft.Json
- ✅ System.Net.Http

## Methods

```csharp
Task<T> GetObjectAsync<T>(string endPoint, string token);
Task<HttpResponseMessage> PostObjectAsync<T>(string endPoint, T obj, string token = "");
Task<HttpResponseMessage> PutObjectAsync<T>(string endPoint, T obj, string token);
Task<HttpResponseMessage> PatchObjectAsync<T>(string endPoint, T obj, string token);
```

Supports:

- ✅ .NET Standard 2.1  
- ✅ .NET 9 through 9 (including latest versions)  
- ⚠️ Legacy support for .NET Core 3.1 and older (with limitations)
  
## About
Quiron.HttpClient was developed by [EliasRMJ](https://www.linkedin.com/in/elias-medeiros-98232066/) under the [MIT license](LICENSE).