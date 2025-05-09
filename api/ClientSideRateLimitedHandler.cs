using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.RateLimiting;

namespace Stats2fa.api {
    public class ClientSideRateLimitedHandler : DelegatingHandler {
        private readonly RateLimiter _limiter;

        public ClientSideRateLimitedHandler(RateLimiter limiter)
            : base(new HttpClientHandler())
        {
            _limiter = limiter;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            using var lease = await _limiter.AcquireAsync(1, cancellationToken);

            if (lease.IsAcquired) {
                return await base.SendAsync(request, cancellationToken);
            }

            // If we couldn't acquire a lease, create a HttpResponseMessage with a 429 status code
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);
            response.ReasonPhrase = "Client-side rate limit exceeded";

            if (lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter)) {
                response.Headers.Add("Retry-After", ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo));
            }

            return response;
        }
    }
}