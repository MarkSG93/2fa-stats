using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace Stats2fa.api;

public class ClientSideRateLimitedHandler : DelegatingHandler {
    private readonly RateLimiter _limiter;

    public ClientSideRateLimitedHandler(RateLimiter limiter)
        : base(new HttpClientHandler()) {
        _limiter = limiter;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) {
        using var lease = await _limiter.AcquireAsync(cancellationToken: cancellationToken);

        if (lease.IsAcquired) return await base.SendAsync(request: request, cancellationToken: cancellationToken);

        // If we couldn't acquire a lease, create a HttpResponseMessage with a 429 status code
        var response = new HttpResponseMessage(statusCode: HttpStatusCode.TooManyRequests);
        response.ReasonPhrase = "Client-side rate limit exceeded";

        if (lease.TryGetMetadata(metadataName: MetadataName.RetryAfter, out var retryAfter)) response.Headers.Add("Retry-After", ((int)retryAfter.TotalSeconds).ToString(provider: NumberFormatInfo.InvariantInfo));

        return response;
    }
}