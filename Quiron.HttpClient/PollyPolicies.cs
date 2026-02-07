using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace Quiron.HttpClient
{
    public static class PollyPolicies
    {
        public static IAsyncPolicy<HttpResponseMessage> RetryPolicy(ILogger? logger = null) =>
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(500 * Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        if (logger is null) return;

                        logger.LogWarning("Retry {RetryAttempt} after {Delay}ms to {Url}",
                            retryAttempt, timespan.TotalMilliseconds, outcome.Result?.RequestMessage?.RequestUri);
                    }
                );

        public static IAsyncPolicy<HttpResponseMessage> TimeoutPolicy =>
           Policy.TimeoutAsync<HttpResponseMessage>(25);

        public static IAsyncPolicy<HttpResponseMessage> CircuitBreakerPolicy =>
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(25)
                );

        public static IAsyncPolicy<HttpResponseMessage> ResiliencePolicy(ILogger? logger = null) =>
            Policy.WrapAsync(
                RetryPolicy(logger),
                TimeoutPolicy,
                CircuitBreakerPolicy
            );
    }
}