using Serilog;

namespace AspCoreApi.Handlers;

/// <summary>
/// HTTP message handler that logs outgoing requests and their responses
/// </summary>
public class LoggingHttpMessageHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHttpMessageHandler> _logger;

    public LoggingHttpMessageHandler(ILogger<LoggingHttpMessageHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Generate a correlation ID to trace the request/response pair
        var correlationId = Guid.NewGuid().ToString("N");

        // Log the outgoing request
        _logger.LogDebug("HTTP Request [{CorrelationId}]: {Method} {Uri}",
            correlationId, request.Method, request.RequestUri);

        // Log request headers if needed
        if (Log.IsEnabled(Serilog.Events.LogEventLevel.Debug))
        {
            foreach (var header in request.Headers)
            {
                // Skip sensitive headers like Authorization
                if (!header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Request Header [{CorrelationId}]: {Key}={Value}",
                        correlationId, header.Key, string.Join(", ", header.Value));
                }
                else
                {
                    _logger.LogDebug("Request Header [{CorrelationId}]: {Key}=[REDACTED]",
                        correlationId, header.Key);
                }
            }
        }

        // Log the request content if available
        if (request.Content != null)
        {
            try
            {
                var content = await request.Content.ReadAsStringAsync(cancellationToken);

                // Truncate content if it's too long
                if (content.Length > 1000)
                {
                    content = content.Substring(0, 1000) + "... [truncated]";
                }

                _logger.LogDebug("Request Content [{CorrelationId}]: {Content}",
                    correlationId, content);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read request content [{CorrelationId}]", correlationId);
            }
        }

        // Measure the time taken for the request
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Forward the request to the inner handler
            var response = await base.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            // Log the response
            _logger.LogDebug("HTTP Response [{CorrelationId}]: {StatusCode} for {Method} {Uri} in {ElapsedMs}ms",
                correlationId, (int)response.StatusCode, request.Method, request.RequestUri, stopwatch.ElapsedMilliseconds);

            // Log response headers if needed
            if (Log.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                foreach (var header in response.Headers)
                {
                    _logger.LogDebug("Response Header [{CorrelationId}]: {Key}={Value}",
                        correlationId, header.Key, string.Join(", ", header.Value));
                }
            }

            // Log the response content if available and not too large
            if (response.Content != null && response.Content.Headers.ContentLength.GetValueOrDefault() < 10000)
            {
                try
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);

                    // Truncate content if it's too long
                    if (content.Length > 1000)
                    {
                        content = content.Substring(0, 1000) + "... [truncated]";
                    }

                    _logger.LogDebug("Response Content [{CorrelationId}]: {Content}",
                        correlationId, content);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read response content [{CorrelationId}]", correlationId);
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "HTTP Request Failed [{CorrelationId}]: {Method} {Uri} after {ElapsedMs}ms",
                correlationId, request.Method, request.RequestUri, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}