using Polly;
using Polly.Extensions.Http;

namespace Quiron.HttpClient
{
    public static class PollyPolicies
    {
        public static IAsyncPolicy<HttpResponseMessage> RetryPolicy =>
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromMilliseconds(500 * Math.Pow(2, retryAttempt))
                );
    }
}