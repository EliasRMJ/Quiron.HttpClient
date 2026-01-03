using System.Security.Cryptography.X509Certificates;

namespace Quiron.HttpClient
{
    public interface ICertificateResolver
    {
        X509Certificate2 GetCertificate(string certificate, string clientId
            , string clientSecret);
    }
}
