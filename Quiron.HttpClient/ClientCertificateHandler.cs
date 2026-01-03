namespace Quiron.HttpClient
{
    public sealed class ClientCertificateHandler(ICertificateResolver certificateResolver) 
        : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!request.Headers.TryGetValues("X-Certificate", out var certificateValue) ||
                !request.Headers.TryGetValues("X-Client-Id", out var clientIdValue) ||
                !request.Headers.TryGetValues("X-Client-Secret", out var clientSecretValue))
                return await base.SendAsync(request, cancellationToken);

            var certificate = certificateValue.First();
            var clientId = clientIdValue.First();
            var clientSecret = clientSecretValue.First();

            var certificateFile = certificateResolver.GetCertificate(certificate, clientId, clientSecret);

            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(certificateFile);

            using var client = new System.Net.Http.HttpClient(handler, disposeHandler: true);

            var clonedRequest = await CloneAsync(request);

            return await client.SendAsync(clonedRequest, cancellationToken);
        }

        private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            foreach (var header in request.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            if (request.Content is not null)
            {
                var ms = new MemoryStream();
                await request.Content.CopyToAsync(ms);
                ms.Position = 0;

                clone.Content = new StreamContent(ms);

                foreach (var header in request.Content.Headers)
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }
    }
}