using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Stats2fa.logger;

namespace Stats2fa.api {
    public class RetryHandler : DelegatingHandler {
        private const int MaxRetries = 3;

        public RetryHandler(HttpMessageHandler innerHandler)
            : base(innerHandler) {
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            HttpResponseMessage response = null;
            for (int i = 0; i < MaxRetries; i++) {
                response = await base.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode) {
                    return response;
                }

                // Only retry on 429 (Too Many Requests) or 5xx (server error)
                if ((int)response.StatusCode != 429 && (int)response.StatusCode < 500) {
                    return response;
                }

                Console.WriteLine("\n" + $"[{DateTime.UtcNow:s}] Error response: {response.StatusCode}, retry {i + 1} of {MaxRetries} request {request.RequestUri} in {TimeSpan.FromSeconds(Math.Pow(2, i))} seconds");
                // Exponential backoff
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)), cancellationToken);
            }

            return response;
        }
    }
}