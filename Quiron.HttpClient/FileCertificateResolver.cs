using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace Quiron.HttpClient
{
    public class FileCertificateResolver : ICertificateResolver
    {
        private readonly ConcurrentDictionary<string, X509Certificate2> _cache = new();

        public X509Certificate2 GetCertificate(string dirBase, string clientId, string clientSecret
            , string fileName)
        {
            return _cache.GetOrAdd(clientId, id =>
            {
                var path = Path.Combine(dirBase, id, fileName);
                var password = clientSecret;

                return X509CertificateLoader.LoadPkcs12(File.ReadAllBytes(path), password
                    , X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet |
                      X509KeyStorageFlags.Exportable);
            });
        }
    }
}