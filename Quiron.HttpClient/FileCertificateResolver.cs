using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace Quiron.HttpClient
{
    public class FileCertificateResolver : ICertificateResolver
    {
        private readonly ConcurrentDictionary<string, X509Certificate2> _cache = new();

        public X509Certificate2 GetCertificate(string certificate, string clientId, string clientSecret)
        {
            return _cache.GetOrAdd(clientId, id =>
            {
                return X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(certificate), clientSecret
                    , X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet |
                      X509KeyStorageFlags.Exportable);
            });
        }
    }
}